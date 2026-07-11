using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NavisMcp.Contracts;
using NavisMcp.Server.Infrastructure;
using Xunit;

namespace NavisMcp.Server.Tests;

public sealed class PipeRpcClientTests
{
    [Fact]
    public async Task InvokeAsync_UsesSessionDescriptorAndReturnsData()
    {
        using var loggerFactory = LoggerFactory.Create(_ => { });
        var tempRoot = Path.Combine(Path.GetTempPath(), "navis-mcp-tests", Guid.NewGuid().ToString("N"));
        var paths = new AppPaths(Path.Combine(tempRoot, "appdata"));
        var options = ServerOptions.Parse(new[] { "--allow-writes", "--project-root", tempRoot });
        var targetId = "test-" + Guid.NewGuid().ToString("N");
        var pipeName = NavisMcpDefaults.PipePrefix + targetId;
        var authToken = Guid.NewGuid().ToString("N");
        var sessionFile = Path.Combine(paths.SessionsPath, targetId + ".json");
        var descriptor = new SessionDescriptor
        {
            TargetId = targetId,
            PipeName = pipeName,
            ProcessId = Process.GetCurrentProcess().Id,
            ProcessName = "test",
            LastSeenUtc = DateTimeOffset.UtcNow
        };

        File.WriteAllText(sessionFile, JsonSerializer.Serialize(descriptor, JsonDefaults.Options));
        File.WriteAllText(sessionFile + NavisMcpDefaults.SessionTokenFileSuffix, authToken);

        var serverTask = ServeOneRequestAsync(pipeName, authToken);
        try
        {
            var registry = new SessionRegistry(paths, loggerFactory.CreateLogger<SessionRegistry>());
            var client = new PipeRpcClient(registry, loggerFactory.CreateLogger<PipeRpcClient>(), options);

            var result = await client.InvokeAsync<DocumentInfo>("nwd_get_document_info", null, targetId, 5000);

            Assert.Equal("Fake Document", result.Title);
            await serverTask;
        }
        finally
        {
            try { File.Delete(sessionFile); } catch { }
            try { File.Delete(sessionFile + NavisMcpDefaults.SessionTokenFileSuffix); } catch { }
        }
    }

    private static async Task ServeOneRequestAsync(string pipeName, string authToken)
    {
        using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        await pipe.WaitForConnectionAsync();
        using var reader = new StreamReader(pipe, Encoding.UTF8, false, 4096, leaveOpen: true);
        using var writer = new StreamWriter(pipe, new UTF8Encoding(false), 4096, leaveOpen: true) { AutoFlush = true };

        var line = await reader.ReadLineAsync();
        var request = JsonSerializer.Deserialize<NavisRpcRequest>(line ?? string.Empty, JsonDefaults.Options)!;
        Assert.NotNull(request.Context);
        Assert.True(request.Context!.AllowWrites);
        Assert.Equal(authToken, request.Context.AuthToken);
        Assert.Equal("nwd_get_document_info", request.Method);
        var response = NavisRpcResponse.Success(request.Id, new DocumentInfo { Title = "Fake Document", ModelCount = 1 }, 1);
        await writer.WriteLineAsync(JsonSerializer.Serialize(response, JsonDefaults.Options));
    }
}
