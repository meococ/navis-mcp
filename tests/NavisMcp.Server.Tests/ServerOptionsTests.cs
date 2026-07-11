using NavisMcp.Server.Infrastructure;
using Xunit;

namespace NavisMcp.Server.Tests;

public sealed class ServerOptionsTests
{
    [Fact]
    public void Parse_RecognizesAllowWritesVerboseAndProjectRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "navis-mcp-tests");
        var modelRoot = Path.Combine(root, "incoming-models");
        var options = ServerOptions.Parse(new[] { "--allow-writes", "--verbose", "--project-root", root, "--model-input-root", modelRoot });

        Assert.True(options.AllowWrites);
        Assert.True(options.Verbose);
        Assert.Equal(Path.GetFullPath(root), options.ProjectRoot);
        Assert.Equal(Path.Combine(Path.GetFullPath(root), "exports"), options.ExportsRoot);
        Assert.Equal(Path.Combine(Path.GetFullPath(root), "snapshots"), options.SnapshotsRoot);
        Assert.Equal(Path.Combine(Path.GetFullPath(root), "reports"), options.ReportsRoot);
        Assert.Equal(Path.GetFullPath(modelRoot), options.ModelInputRoot);
    }

    [Fact]
    public void Parse_RecognizesToolsetsAndBrief()
    {
        var options = ServerOptions.Parse(new[] { "--brief", "--toolsets", "core,clash,report" });
        Assert.True(options.Brief);
        Assert.Equal(new[] { "core", "clash", "report" }, options.Toolsets);
        var catalog = ToolsetCatalog.List(options);
        Assert.True(catalog.Brief);
        Assert.Equal(6, catalog.Toolsets.Count);
        Assert.Contains("core", catalog.Requested);
        Assert.False(ToolsetCatalog.IncludesAdvanced(options.Toolsets));
    }

    [Fact]
    public void DefaultIds_ExcludesAdvanced()
    {
        var options = ServerOptions.Parse(Array.Empty<string>());
        var catalog = ToolsetCatalog.List(options);

        Assert.Equal(ToolsetCatalog.DefaultIds, catalog.Requested);
        Assert.False(ToolsetCatalog.IncludesAdvanced(options.Toolsets));
        Assert.Contains("advanced", ToolsetCatalog.AllIds);
    }

    [Fact]
    public void IncludesAdvanced_ReturnsTrueOnlyWhenExplicitlyRequested()
    {
        Assert.False(ToolsetCatalog.IncludesAdvanced(Array.Empty<string>()));
        Assert.False(ToolsetCatalog.IncludesAdvanced(new[] { "core", "write" }));
        Assert.True(ToolsetCatalog.IncludesAdvanced(new[] { "core", "advanced" }));
    }
}
