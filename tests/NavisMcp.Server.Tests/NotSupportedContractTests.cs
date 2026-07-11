using NavisMcp.Contracts;
using Xunit;

namespace NavisMcp.Server.Tests;

public sealed class NotSupportedContractTests
{
    [Fact]
    public void NotSupportedResult_Defaults_To_NotSupportedByApi_ErrorCode()
    {
        var result = new NotSupportedResult
        {
            Supported = false,
            Capability = "nwd_create_search_set",
            Message = "not mapped"
        };

        Assert.Equal("not_supported_by_api", result.ErrorCode);
        Assert.False(result.Supported);
    }
}
