using NavisMcp.Server.Infrastructure;
using Xunit;

namespace NavisMcp.Server.Tests;

public sealed class ModelInputPathGuardTests
{
    [Fact]
    public void ResolveModelInputPath_AllowsExistingModelUnderInputRoot()
    {
        var root = CreateTempRoot();
        var options = ServerOptions.Parse(new[] { "--project-root", root });
        var modelPath = Path.Combine(options.ModelInputRoot, "sample.nwd");
        Directory.CreateDirectory(options.ModelInputRoot);
        File.WriteAllText(modelPath, "fake");

        var resolved = ModelInputPathGuard.ResolveModelInputPath(options, "sample.nwd");

        Assert.Equal(Path.GetFullPath(modelPath), resolved);
    }

    [Fact]
    public void ResolveModelInputPath_DeniesParentTraversal()
    {
        var root = CreateTempRoot();
        var options = ServerOptions.Parse(new[] { "--project-root", root });

        var ex = Assert.Throws<NavisRpcException>(() => ModelInputPathGuard.ResolveModelInputPath(options, "..\\outside.nwd"));

        Assert.Equal("model_path_denied", ex.Code);
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "navis-mcp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
