using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NavisMcp.Contracts;
using NavisMcp.Server.Infrastructure;
using Xunit;

namespace NavisMcp.Server.Tests;

public sealed class FakeBridgeAuthTests
{
    [Fact]
    public async Task InvokeAsync_RejectsWhenBridgeTokenDoesNotMatch()
    {
        using var loggerFactory = LoggerFactory.Create(_ => { });
        var tempRoot = Path.Combine(Path.GetTempPath(), "navis-mcp-tests", Guid.NewGuid().ToString("N"));
        var paths = new AppPaths(Path.Combine(tempRoot, "appdata"));
        var options = ServerOptions.Parse(new[] { "--project-root", tempRoot });
        var targetId = "auth-" + Guid.NewGuid().ToString("N");
        var pipeName = NavisMcpDefaults.PipePrefix + targetId;
        var clientToken = Guid.NewGuid().ToString("N");
        var bridgeToken = Guid.NewGuid().ToString("N");
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
        File.WriteAllText(sessionFile + NavisMcpDefaults.SessionTokenFileSuffix, clientToken);

        var serverTask = ServeUnauthorizedAsync(pipeName, bridgeToken);
        try
        {
            var registry = new SessionRegistry(paths, loggerFactory.CreateLogger<SessionRegistry>());
            var client = new PipeRpcClient(registry, loggerFactory.CreateLogger<PipeRpcClient>(), options);

            var ex = await Assert.ThrowsAsync<NavisRpcException>(() =>
                client.InvokeAsync<object>("nwd_health_check", null, targetId, 5000));

            Assert.Equal("unauthorized", ex.Code);
            Assert.False(TokenComparer.EqualsConstantTime(clientToken, bridgeToken));
            await serverTask;
        }
        finally
        {
            try { File.Delete(sessionFile); } catch { }
            try { File.Delete(sessionFile + NavisMcpDefaults.SessionTokenFileSuffix); } catch { }
        }
    }

    private static async Task ServeUnauthorizedAsync(string pipeName, string expectedBridgeToken)
    {
        using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        await pipe.WaitForConnectionAsync();
        using var reader = new StreamReader(pipe, Encoding.UTF8, false, 4096, leaveOpen: true);
        using var writer = new StreamWriter(pipe, new UTF8Encoding(false), 4096, leaveOpen: true) { AutoFlush = true };

        var line = await reader.ReadLineAsync();
        var request = JsonSerializer.Deserialize<NavisRpcRequest>(line ?? string.Empty, JsonDefaults.Options)!;
        Assert.False(TokenComparer.EqualsConstantTime(expectedBridgeToken, request.Context?.AuthToken));
        var response = NavisRpcResponse.Failure(request.Id, "unauthorized", "Invalid Navis MCP bridge token.", 1);
        await writer.WriteLineAsync(JsonSerializer.Serialize(response, JsonDefaults.Options));
    }
}
