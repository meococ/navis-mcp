using NavisMcp.Server.Infrastructure;
using Xunit;

namespace NavisMcp.Server.Tests;

public sealed class ReportPathGuardTests
{
    [Fact]
    public void ResolveReportPath_AllowsRelativePathUnderReports()
    {
        var root = CreateTempRoot();
        var options = ServerOptions.Parse(new[] { "--project-root", root });

        var path = ReportPathGuard.ResolveReportPath(options, "nested/report", "json");

        Assert.StartsWith(options.ReportsRoot, path.FilePath, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(".json", path.FilePath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("json", path.Format);
        Assert.StartsWith("reports", path.RelativePath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveReportPath_DeniesParentTraversal()
    {
        var root = CreateTempRoot();
        var options = ServerOptions.Parse(new[] { "--project-root", root });

        var ex = Assert.Throws<NavisRpcException>(() => ReportPathGuard.ResolveReportPath(options, "..\\outside.json", "json"));

        Assert.Equal("report_path_denied", ex.Code);
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "navis-mcp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
