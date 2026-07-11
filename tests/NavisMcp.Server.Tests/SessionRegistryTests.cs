using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NavisMcp.Contracts;
using NavisMcp.Server.Infrastructure;
using Xunit;

namespace NavisMcp.Server.Tests;

public sealed class SessionRegistryTests
{
    [Fact]
    public void ListTargets_ReadsTokenSidecar_AndPublicTargetsHideToken()
    {
        using var loggerFactory = LoggerFactory.Create(_ => { });
        var tempRoot = Path.Combine(Path.GetTempPath(), "navis-mcp-tests", Guid.NewGuid().ToString("N"));
        var paths = new AppPaths(tempRoot);
        var targetId = "target-" + Guid.NewGuid().ToString("N");
        var sessionFile = Path.Combine(paths.SessionsPath, targetId + ".json");
        var token = Guid.NewGuid().ToString("N");
        var descriptor = new SessionDescriptor
        {
            TargetId = targetId,
            PipeName = NavisMcpDefaults.PipePrefix + targetId,
            ProcessId = Process.GetCurrentProcess().Id,
            DocumentTitle = "model.nwf",
            LastSeenUtc = DateTimeOffset.UtcNow
        };

        File.WriteAllText(sessionFile, JsonSerializer.Serialize(descriptor, JsonDefaults.Options));
        File.WriteAllText(sessionFile + NavisMcpDefaults.SessionTokenFileSuffix, token);

        var registry = new SessionRegistry(paths, loggerFactory.CreateLogger<SessionRegistry>());

        var internalTarget = Assert.Single(registry.ListTargets());
        var publicTarget = Assert.Single(registry.ListPublicTargets());

        Assert.Equal(token, internalTarget.AuthToken);
        Assert.Null(publicTarget.AuthToken);
    }
}
