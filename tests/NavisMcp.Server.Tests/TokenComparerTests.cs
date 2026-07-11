using NavisMcp.Contracts;
using Xunit;

namespace NavisMcp.Server.Tests;

public sealed class TokenComparerTests
{
    [Fact]
    public void EqualsConstantTime_MatchesIdenticalTokens()
    {
        Assert.True(TokenComparer.EqualsConstantTime("abc123", "abc123"));
    }

    [Fact]
    public void EqualsConstantTime_RejectsMismatchAndEmpty()
    {
        Assert.False(TokenComparer.EqualsConstantTime("abc123", "abc124"));
        Assert.False(TokenComparer.EqualsConstantTime("abc123", "abc12"));
        Assert.False(TokenComparer.EqualsConstantTime(null, "abc"));
        Assert.False(TokenComparer.EqualsConstantTime("abc", null));
        Assert.False(TokenComparer.EqualsConstantTime("", "abc"));
        Assert.False(TokenComparer.EqualsConstantTime("abc", ""));
    }
}
