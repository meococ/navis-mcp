namespace NavisMcp.Server.Infrastructure;

public sealed class WriteGate
{
    private readonly ServerOptions _options;

    public WriteGate(ServerOptions options)
    {
        _options = options;
    }

    public bool AllowWrites => _options.AllowWrites;

    public void EnsureAllowed()
    {
        if (!_options.AllowWrites)
        {
            throw new NavisRpcException("writes_disabled", "This MCP server was started without --allow-writes.");
        }
    }
}
