using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Navisworks.Api;
using NavisMcp.Contracts;
using NavisMcp.Plugin.Navis2026.Services;
using NwApplication = Autodesk.Navisworks.Api.Application;

namespace NavisMcp.Plugin.Navis2026.Bridge
{
    internal sealed class BridgeHost : IDisposable
    {
        private readonly UiWorkDispatcher _ui;
        private readonly NavisCommandDispatcher _dispatcher;
        private readonly BridgeLogger _logger;
        private readonly object _descriptorGate = new object();
        private readonly string _sessionFile;
        private readonly string _authTokenFile;
        private readonly string _authToken;
        private readonly Timer _sessionTimer;
        private CancellationTokenSource _cts;
        private Task _listenerTask;
        private SessionDescriptor _lastDescriptor;
        private bool _disposed;

        public BridgeHost(UiWorkDispatcher ui)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            TargetId = Guid.NewGuid().ToString("N");
            _authToken = Guid.NewGuid().ToString("N");
            PipeName = NavisMcpDefaults.PipePrefix + TargetId;
            _logger = new BridgeLogger(TargetId);
            _dispatcher = new NavisCommandDispatcher(GetLastDescriptorSnapshot);

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var sessionPath = Path.Combine(localAppData, NavisMcpDefaults.AppName, NavisMcpDefaults.SessionFolderName);
            Directory.CreateDirectory(sessionPath);
            _sessionFile = Path.Combine(sessionPath, TargetId + ".json");
            _authTokenFile = _sessionFile + NavisMcpDefaults.SessionTokenFileSuffix;
            _sessionTimer = new Timer(OnSessionTimer, null, Timeout.Infinite, Timeout.Infinite);
        }

        public string TargetId { get; }
        public string PipeName { get; }
        public bool IsRunning => _cts != null && !_cts.IsCancellationRequested;

        public string Status
        {
            get
            {
                var descriptor = GetLastDescriptorSnapshot();
                return "Navis MCP bridge " + (IsRunning ? "running" : "stopped") +
                    Environment.NewLine + "targetId: " + TargetId +
                    Environment.NewLine + "pipe: " + PipeName +
                    Environment.NewLine + "document: " + (descriptor.DocumentTitle ?? "(none)") +
                    Environment.NewLine + "session: " + _sessionFile;
            }
        }

        public void Start()
        {
            if (IsRunning)
            {
                return;
            }

            _cts = new CancellationTokenSource();
            _listenerTask = Task.Run(() => ListenLoopAsync(_cts.Token));
            _sessionTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(5));
            _logger.Info("Bridge started. targetId=" + TargetId + ", pipe=" + PipeName);
        }

        private async Task ListenLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var pipe = CreateSecuredPipe();

                try
                {
                    var waitTask = pipe.WaitForConnectionAsync();
                    var cancelTask = Task.Delay(Timeout.Infinite, cancellationToken);
                    if (await Task.WhenAny(waitTask, cancelTask).ConfigureAwait(false) != waitTask)
                    {
                        pipe.Dispose();
                        break;
                    }

                    await waitTask.ConfigureAwait(false);
                    _ = Task.Run(() => HandleClientAsync(pipe, cancellationToken), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    pipe.Dispose();
                    break;
                }
                catch (Exception ex)
                {
                    pipe.Dispose();
                    _logger.Error("Pipe accept failed.", ex);
                    await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private NamedPipeServerStream CreateSecuredPipe()
        {
            var security = new PipeSecurity();
            var currentUser = WindowsIdentity.GetCurrent().User;
            if (currentUser != null)
            {
                security.AddAccessRule(new PipeAccessRule(currentUser, PipeAccessRights.FullControl, AccessControlType.Allow));
            }

            return new NamedPipeServerStream(
                PipeName,
                PipeDirection.InOut,
                4,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous,
                0,
                0,
                security);
        }

        private async Task HandleClientAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
        {
            using (pipe)
            using (var reader = new StreamReader(pipe, Encoding.UTF8, false, 4096, true))
            using (var writer = new StreamWriter(pipe, new UTF8Encoding(false), 4096, true) { AutoFlush = true })
            {
                var started = Stopwatch.StartNew();
                string requestId = string.Empty;

                try
                {
                    var line = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        throw new BridgeRpcException("invalid_request", "The bridge received an empty request.");
                    }

                    var request = JsonSerializer.Deserialize<NavisRpcRequest>(line, JsonDefaults.Options);
                    if (request == null)
                    {
                        throw new BridgeRpcException("invalid_request", "The bridge could not parse the request.");
                    }

                    requestId = request.Id;
                    var timeoutMs = request.TimeoutMs.GetValueOrDefault(NavisMcpDefaults.DefaultTimeoutMs);
                    if (timeoutMs <= 0)
                    {
                        timeoutMs = NavisMcpDefaults.DefaultTimeoutMs;
                    }

                    var dispatchTask = _ui.InvokeAsync(() => _dispatcher.Dispatch(request.Method, request.Params, request.Context));
                    var timeoutTask = Task.Delay(timeoutMs, cancellationToken);
                    var completed = await Task.WhenAny(dispatchTask, timeoutTask).ConfigureAwait(false);
                    if (completed != dispatchTask)
                    {
                        throw new BridgeRpcException("timeout", "Timed out waiting for Navisworks API method '" + request.Method + "'.");
                    }

                    var data = await dispatchTask.ConfigureAwait(false);
                    var response = NavisRpcResponse.Success(requestId, data, started.ElapsedMilliseconds);
                    await writer.WriteLineAsync(JsonSerializer.Serialize(response, JsonDefaults.Options)).ConfigureAwait(false);
                }
                catch (BridgeRpcException ex)
                {
                    var response = NavisRpcResponse.Failure(requestId, ex.Code, ex.Message, started.ElapsedMilliseconds, ex.Details);
                    await writer.WriteLineAsync(JsonSerializer.Serialize(response, JsonDefaults.Options)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.Error("Request failed.", ex);
                    var response = NavisRpcResponse.Failure(requestId, "bridge_error", ex.Message, started.ElapsedMilliseconds);
                    await writer.WriteLineAsync(JsonSerializer.Serialize(response, JsonDefaults.Options)).ConfigureAwait(false);
                }
            }
        }

        private async Task RefreshSessionDescriptorAsync()
        {
            if (!IsRunning)
            {
                return;
            }

            try
            {
                var descriptor = (SessionDescriptor)await _ui.InvokeAsync(BuildDescriptorOnUiThread).ConfigureAwait(false);
                lock (_descriptorGate)
                {
                    _lastDescriptor = descriptor;
                }

                WriteHiddenTokenFile();

                var publicDescriptor = new SessionDescriptor
                {
                    TargetId = descriptor.TargetId,
                    PipeName = descriptor.PipeName,
                    ProcessId = descriptor.ProcessId,
                    ProcessName = descriptor.ProcessName,
                    NavisworksVersion = descriptor.NavisworksVersion,
                    PluginVersion = descriptor.PluginVersion,
                    MachineName = descriptor.MachineName,
                    UserName = descriptor.UserName,
                    DocumentTitle = descriptor.DocumentTitle,
                    DocumentPath = descriptor.DocumentPath,
                    AuthToken = null,
                    CreatedUtc = descriptor.CreatedUtc,
                    LastSeenUtc = descriptor.LastSeenUtc
                };

                var json = JsonSerializer.Serialize(publicDescriptor, JsonDefaults.Options);
                File.WriteAllText(_sessionFile, json);
            }
            catch (Exception ex)
            {
                _logger.Error("Session descriptor refresh failed.", ex);
            }
        }

        private void OnSessionTimer(object state)
        {
            try
            {
                var task = RefreshSessionDescriptorAsync();
                task.ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        _logger.Error("Session descriptor refresh failed outside guarded path.", t.Exception.GetBaseException());
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
            catch (Exception ex)
            {
                _logger.Error("Session descriptor timer callback failed.", ex);
            }
        }

        private object BuildDescriptorOnUiThread()
        {
            string title = null;
            string path = null;

            try
            {
                var document = NwApplication.ActiveDocument;
                if (document != null && !document.IsClear)
                {
                    title = document.Title;
                    path = document.CurrentFileName;
                }
            }
            catch
            {
            }

            var process = Process.GetCurrentProcess();
            return new SessionDescriptor
            {
                TargetId = TargetId,
                PipeName = PipeName,
                ProcessId = process.Id,
                ProcessName = process.ProcessName,
                NavisworksVersion = SafeToString(() => NwApplication.Version),
                PluginVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                MachineName = Environment.MachineName,
                UserName = Environment.UserName,
                DocumentTitle = title,
                DocumentPath = path,
                AuthToken = _authToken,
                CreatedUtc = _lastDescriptor?.CreatedUtc ?? DateTimeOffset.UtcNow,
                LastSeenUtc = DateTimeOffset.UtcNow
            };
        }

        private SessionDescriptor GetLastDescriptorSnapshot()
        {
            lock (_descriptorGate)
            {
                return _lastDescriptor ?? (SessionDescriptor)BuildDescriptorOnUiThread();
            }
        }

        private static string SafeToString(Func<object> getter)
        {
            try
            {
                var value = getter();
                return value == null ? string.Empty : value.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void TryHideFile(string path)
        {
            try
            {
                File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden);
            }
            catch
            {
            }
        }

        private void WriteHiddenTokenFile()
        {
            try
            {
                if (File.Exists(_authTokenFile))
                {
                    File.SetAttributes(_authTokenFile, File.GetAttributes(_authTokenFile) & ~FileAttributes.Hidden);
                }

                File.WriteAllText(_authTokenFile, _authToken);
                RestrictFileToCurrentUser(_authTokenFile);
                TryHideFile(_authTokenFile);
            }
            catch (UnauthorizedAccessException)
            {
                try
                {
                    File.Delete(_authTokenFile);
                    File.WriteAllText(_authTokenFile, _authToken);
                    RestrictFileToCurrentUser(_authTokenFile);
                    TryHideFile(_authTokenFile);
                }
                catch
                {
                    throw;
                }
            }
        }

        private static void RestrictFileToCurrentUser(string path)
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                if (identity?.User == null)
                {
                    return;
                }

                var security = new FileSecurity();
                security.SetAccessRuleProtection(true, false);
                security.AddAccessRule(new FileSystemAccessRule(
                    identity.User,
                    FileSystemRights.FullControl,
                    AccessControlType.Allow));
                File.SetAccessControl(path, security);
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                _sessionTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _sessionTimer.Dispose();
            }
            catch
            {
            }

            try
            {
                _cts?.Cancel();
            }
            catch
            {
            }

            try
            {
                File.Delete(_sessionFile);
            }
            catch
            {
            }

            try
            {
                File.Delete(_authTokenFile);
            }
            catch
            {
            }

            try
            {
                _cts?.Dispose();
            }
            catch
            {
            }

            _logger.Info("Bridge stopped.");
        }
    }
}
