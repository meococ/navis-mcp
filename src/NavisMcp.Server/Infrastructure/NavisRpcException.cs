namespace NavisMcp.Server.Infrastructure;

public sealed class NavisRpcException : Exception
{
    public string Code { get; }

    public NavisRpcException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}
