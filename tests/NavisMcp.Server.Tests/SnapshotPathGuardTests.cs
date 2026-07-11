using NavisMcp.Server.Infrastructure;
using Xunit;

namespace NavisMcp.Server.Tests;

public sealed class SnapshotPathGuardTests
{
    [Fact]
    public void ResolveSnapshotPath_AllowsRelativePathUnderSnapshots()
    {
        var root = CreateTempRoot();
        var options = ServerOptions.Parse(new[] { "--project-root", root });

        var path = SnapshotPathGuard.ResolveSnapshotPath(options, "nested/view", "png");

        Assert.StartsWith(options.SnapshotsRoot, path.FilePath, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(".png", path.FilePath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("png", path.Format);
        Assert.StartsWith("snapshots", path.RelativePath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveSnapshotPath_DeniesParentTraversal()
    {
        var root = CreateTempRoot();
        var options = ServerOptions.Parse(new[] { "--project-root", root });

        var ex = Assert.Throws<NavisRpcException>(() => SnapshotPathGuard.ResolveSnapshotPath(options, "..\\outside.png", "png"));

        Assert.Equal("snapshot_path_denied", ex.Code);
    }

    [Fact]
    public void ResolveSnapshotPath_DeniesUnsupportedExtension()
    {
        var root = CreateTempRoot();
        var options = ServerOptions.Parse(new[] { "--project-root", root });

        var ex = Assert.Throws<NavisRpcException>(() => SnapshotPathGuard.ResolveSnapshotPath(options, "view.bmp", "png"));

        Assert.Equal("invalid_snapshot_format", ex.Code);
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "navis-mcp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
