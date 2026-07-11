using NavisMcp.Server.Infrastructure;
using NavisMcp.Server.Tools;
using Xunit;

namespace NavisMcp.Server.Tests;

public sealed class WriteGateTests
{
    [Fact]
    public void EnsureAllowed_ThrowsWithoutAllowWrites()
    {
        var gate = new WriteGate(ServerOptions.Parse(Array.Empty<string>()));

        var ex = Assert.Throws<NavisRpcException>(() => gate.EnsureAllowed());

        Assert.Equal("writes_disabled", ex.Code);
    }

    [Fact]
    public void EnsureAllowed_AllowsWhenFlagIsPresent()
    {
        var gate = new WriteGate(ServerOptions.Parse(new[] { "--allow-writes" }));

        gate.EnsureAllowed();
    }

    [Theory]
    [InlineData("current", false)]
    [InlineData("auto", true)]
    [InlineData("selection", true)]
    [InlineData("model", true)]
    [InlineData("front_right_top", true)]
    public void SnapshotRequiresWrites_DependsOnFraming(string framing, bool expected)
    {
        Assert.Equal(expected, ToolInvocation.SnapshotRequiresWrites(framing));
    }
}
