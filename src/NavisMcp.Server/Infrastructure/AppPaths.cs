using NavisMcp.Contracts;

namespace NavisMcp.Server.Infrastructure;

public sealed class AppPaths
{
    public string BasePath { get; }
    public string SessionsPath { get; }
    public string LogsPath { get; }
    public string AuditLogPath { get; }

    public AppPaths()
        : this(null)
    {
    }

    public AppPaths(string? basePath)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        BasePath = string.IsNullOrWhiteSpace(basePath)
            ? Path.Combine(localAppData, NavisMcpDefaults.AppName)
            : Path.GetFullPath(basePath);
        SessionsPath = Path.Combine(BasePath, NavisMcpDefaults.SessionFolderName);
        LogsPath = Path.Combine(BasePath, NavisMcpDefaults.LogFolderName);
        AuditLogPath = Path.Combine(LogsPath, "audit.jsonl");

        Directory.CreateDirectory(SessionsPath);
        Directory.CreateDirectory(LogsPath);
    }
}
