using NavisMcp.Server.Infrastructure;
using Xunit;

namespace NavisMcp.Server.Tests;

public sealed class ExportPathGuardTests
{
    [Fact]
    public void ResolveExportPath_AllowsRelativePathUnderExports()
    {
        var root = CreateTempRoot();
        var options = ServerOptions.Parse(new[] { "--project-root", root });

        var path = ExportPathGuard.ResolveExportPath(options, "nested/model");

        Assert.StartsWith(options.ExportsRoot, path, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(".nwd", path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveExportPath_DeniesParentTraversal()
    {
        var root = CreateTempRoot();
        var options = ServerOptions.Parse(new[] { "--project-root", root });

        var ex = Assert.Throws<NavisRpcException>(() => ExportPathGuard.ResolveExportPath(options, "..\\outside.nwd"));

        Assert.Equal("export_path_denied", ex.Code);
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "navis-mcp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
