using System.Text.Json;
using NavisMcp.Server.Infrastructure;
using Xunit;

namespace NavisMcp.Server.Tests;

public sealed class AuditLoggerTests
{
    [Fact]
    public async Task WriteAsync_WritesTraceableJsonLine()
    {
        var root = Path.Combine(Path.GetTempPath(), "navis-mcp-tests", Guid.NewGuid().ToString("N"));
        var paths = new AppPaths(root);
        var audit = new AuditLogger(paths);

        await audit.WriteAsync("nwd_export_items_table", new { targetId = "target-1", fileName = "items.csv" }, CancellationToken.None);

        var line = File.ReadAllText(paths.AuditLogPath);
        using var doc = JsonDocument.Parse(line);
        Assert.Equal("nwd_export_items_table", doc.RootElement.GetProperty("toolName").GetString());
        Assert.Equal("target-1", doc.RootElement.GetProperty("parameters").GetProperty("targetId").GetString());
        Assert.True(doc.RootElement.TryGetProperty("atUtc", out _));
    }
}
