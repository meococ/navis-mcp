using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using NavisMcp.Contracts;

var targetId = args.FirstOrDefault(x => x.StartsWith("--target-id=", StringComparison.OrdinalIgnoreCase))?.Substring("--target-id=".Length)
    ?? "fake-" + Guid.NewGuid().ToString("N");
var pipeName = NavisMcpDefaults.PipePrefix + targetId;
var authToken = Guid.NewGuid().ToString("N");

var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
var sessionPath = Path.Combine(localAppData, NavisMcpDefaults.AppName, NavisMcpDefaults.SessionFolderName);
Directory.CreateDirectory(sessionPath);
var sessionFile = Path.Combine(sessionPath, targetId + ".json");
var tokenFile = sessionFile + NavisMcpDefaults.SessionTokenFileSuffix;

var descriptor = new SessionDescriptor
{
    TargetId = targetId,
    PipeName = pipeName,
    ProcessId = Process.GetCurrentProcess().Id,
    ProcessName = "NavisMcp.FakeBridge",
    NavisworksVersion = "fake",
    PluginVersion = "fake",
    DocumentTitle = "Fake Document",
    DocumentPath = "C:\\fake\\model.nwd",
    LastSeenUtc = DateTimeOffset.UtcNow
};
File.WriteAllText(sessionFile, JsonSerializer.Serialize(descriptor, JsonDefaults.Options));
File.WriteAllText(tokenFile, authToken);

Console.Error.WriteLine($"Fake bridge listening on {pipeName}");
Console.Error.WriteLine($"Session descriptor: {sessionFile}");
Console.Error.WriteLine($"Auth token sidecar: {tokenFile}");

var stopping = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    stopping.Cancel();
};

try
{
    while (!stopping.IsCancellationRequested)
    {
        using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 4, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        var waitTask = pipe.WaitForConnectionAsync();
        var completed = await Task.WhenAny(waitTask, Task.Delay(Timeout.Infinite, stopping.Token));
        if (completed != waitTask)
        {
            break;
        }

        await waitTask;
        using var writer = new StreamWriter(pipe, new UTF8Encoding(false), 4096, leaveOpen: true) { AutoFlush = true };
        using var reader = new StreamReader(pipe, Encoding.UTF8, false, 4096, leaveOpen: true);
        var line = await reader.ReadLineAsync();
        var request = JsonSerializer.Deserialize<NavisRpcRequest>(line ?? string.Empty, JsonDefaults.Options);
        NavisRpcResponse response;
        if (request == null)
        {
            response = NavisRpcResponse.Failure("", "invalid_request", "Invalid fake bridge request.", 0);
        }
        else if (request.Context == null ||
                 !TokenComparer.EqualsConstantTime(authToken, request.Context.AuthToken))
        {
            response = NavisRpcResponse.Failure(request.Id, "unauthorized", "Invalid Navis MCP bridge token.", 1);
        }
        else
        {
            response = NavisRpcResponse.Success(request.Id, Handle(request.Method), 1);
        }

        await writer.WriteLineAsync(JsonSerializer.Serialize(response, JsonDefaults.Options));
    }
}
finally
{
    try { File.Delete(sessionFile); } catch { }
    try { File.Delete(tokenFile); } catch { }
}

static object Handle(string method)
{
    switch (method)
    {
        case "nwd_health_check":
            return new HealthResult
            {
                Status = "ok",
                Document = new DocumentInfo { Title = "Fake Document", FilePath = "C:\\fake\\model.nwd", ModelCount = 1 },
                PluginVersion = "fake"
            };
        case "nwd_get_document_info":
            return new DocumentInfo { Title = "Fake Document", FilePath = "C:\\fake\\model.nwd", ModelCount = 1 };
        case "nwd_get_federation_map":
            return new FederationMapResult
            {
                DocumentTitle = "Fake Document",
                ModelCount = 1,
                Models = new List<FederationModelInfo>
                {
                    new FederationModelInfo { DisplayName = "Fake Root", SourceFileName = "model.nwd" }
                }
            };
        case "nwd_get_model_tree":
            return new ModelTreeResult
            {
                ReturnedNodeCount = 1,
                Roots = new List<ModelTreeNode>
                {
                    new ModelTreeNode
                    {
                        ItemRef = new ItemRef { Id = "mi:0", IndexPath = new List<int> { 0 }, DisplayName = "Fake Root", ModelName = "model.nwd" }
                    }
                }
            };
        case "nwd_find_items":
            return new ItemSearchResult
            {
                ReturnedCount = 1,
                Items = new List<ItemRef> { new ItemRef { Id = "mi:0", IndexPath = new List<int> { 0 }, DisplayName = "Fake Root" } }
            };
        case "nwd_capture_viewport":
            return new ViewportSnapshotResult
            {
                FilePath = "C:\\fake\\snapshots\\view.png",
                RelativePath = "snapshots\\view.png",
                Width = 1920,
                Height = 1080,
                Format = "png",
                Length = 123,
                Style = "scene_plus_overlay",
                FramingApplied = "current",
                DocumentTitle = "Fake Document",
                SelectionCount = 0,
                Viewpoint = new CurrentViewpointResult { Projection = "Perspective" },
                GeneratedUtc = DateTimeOffset.UtcNow
            };
        case "nwd_create_search_set":
            return new SavedItemInfo { Id = "ss:0", DisplayName = "Fake Search Set", Kind = "search_set" };
        case "nwd_export_search_sets":
            return new ReportResult
            {
                Written = true,
                FilePath = "C:\\fake\\reports\\search-sets.xml",
                RelativePath = "reports\\search-sets.xml",
                Format = "xml",
                ItemCount = 1,
                Message = "Exported 1 search set(s).",
                GeneratedUtc = DateTimeOffset.UtcNow
            };
        case "nwd_set_appearance_override":
        case "nwd_reset_appearance_overrides":
        case "nwd_set_section_box":
        case "nwd_update_clash_result":
        case "nwd_list_timeliner_tasks":
        case "nwd_link_timeliner_task":
        case "nwd_export_quantification_summary":
            return new NotSupportedResult
            {
                Supported = false,
                ErrorCode = "not_supported_by_api",
                Capability = method,
                Message = method + " is not supported by the fake bridge."
            };
        default:
            return new { method, fake = true };
    }
}
