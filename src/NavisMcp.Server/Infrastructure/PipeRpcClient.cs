using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NavisMcp.Contracts;

namespace NavisMcp.Server.Infrastructure;

public sealed class PipeRpcClient
{
    private readonly SessionRegistry _sessions;
    private readonly ILogger<PipeRpcClient> _logger;
    private readonly ServerOptions _options;

    public PipeRpcClient(SessionRegistry sessions, ILogger<PipeRpcClient> logger, ServerOptions options)
    {
        _sessions = sessions;
        _logger = logger;
        _options = options;
    }

    public Task<T> InvokeAsync<T>(string method, object? parameters = null, string? targetId = null, int timeoutMs = NavisMcpDefaults.DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
        return InvokeAsync<T>(method, parameters, targetId, TimeSpan.FromMilliseconds(timeoutMs), cancellationToken);
    }

    public async Task<T> InvokeAsync<T>(string method, object? parameters, string? targetId, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var target = _sessions.ResolveTarget(targetId);
        var request = new NavisRpcRequest
        {
            Id = Guid.NewGuid().ToString("N"),
            Method = method,
            TargetId = target.TargetId,
            TimeoutMs = (int)timeout.TotalMilliseconds,
            Context = new RequestContext
            {
                AuthToken = target.AuthToken ?? string.Empty,
                AllowWrites = _options.AllowWrites,
                ExportsRoot = _options.ExportsRoot,
                SnapshotsRoot = _options.SnapshotsRoot,
                ReportsRoot = _options.ReportsRoot,
                ModelInputRoot = _options.ModelInputRoot
            },
            Params = JsonSerializer.SerializeToElement(parameters ?? new { }, JsonDefaults.Options)
        };

        var json = JsonSerializer.Serialize(request, JsonDefaults.Options);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(timeout);

        try
        {
            using var pipe = new NamedPipeClientStream(".", target.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipe.ConnectAsync((int)timeout.TotalMilliseconds, linkedCts.Token).ConfigureAwait(false);

            using var writer = new StreamWriter(pipe, new UTF8Encoding(false), 4096, leaveOpen: true) { AutoFlush = true };
            using var reader = new StreamReader(pipe, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);

            await writer.WriteLineAsync(json).ConfigureAwait(false);
            var line = await reader.ReadLineAsync().WaitAsync(timeout, linkedCts.Token).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(line))
            {
                throw new NavisRpcException("empty_response", "The Navisworks bridge returned an empty response.");
            }

            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            var ok = root.TryGetProperty("ok", out var okElement) && okElement.GetBoolean();
            if (!ok)
            {
                var code = "bridge_error";
                var message = "The Navisworks bridge returned an error.";
                if (root.TryGetProperty("error", out var error))
                {
                    if (error.TryGetProperty("code", out var codeElement))
                    {
                        code = codeElement.GetString() ?? code;
                    }

                    if (error.TryGetProperty("message", out var messageElement))
                    {
                        message = messageElement.GetString() ?? message;
                    }
                }

                throw new NavisRpcException(code, message);
            }

            if (!root.TryGetProperty("data", out var data))
            {
                return default!;
            }

            var result = data.Deserialize<T>(JsonDefaults.Options);
            return result!;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new NavisRpcException("timeout", $"Timed out waiting for Navisworks bridge method '{method}'.");
        }
        catch (TimeoutException)
        {
            throw new NavisRpcException("timeout", $"Timed out connecting to Navisworks bridge '{target.PipeName}'.");
        }
        catch (IOException ex)
        {
            _logger.LogDebug(ex, "Pipe IO failed for method {Method}", method);
            throw new NavisRpcException("pipe_io_error", ex.Message);
        }
    }
}
