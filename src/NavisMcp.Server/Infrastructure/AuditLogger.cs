using System.Text.Json;

namespace NavisMcp.Server.Infrastructure;

public sealed class AuditLogger
{
    private readonly AppPaths _paths;

    public AuditLogger(AppPaths paths)
    {
        _paths = paths;
    }

    public async Task WriteAsync(string toolName, object parameters, CancellationToken cancellationToken)
    {
        var entry = new
        {
            atUtc = DateTimeOffset.UtcNow,
            toolName,
            user = Environment.UserName,
            machine = Environment.MachineName,
            parameters
        };

        var line = JsonSerializer.Serialize(entry) + Environment.NewLine;
        await File.AppendAllTextAsync(_paths.AuditLogPath, line, cancellationToken).ConfigureAwait(false);
    }
}
