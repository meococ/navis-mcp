using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NavisMcp.Contracts;

namespace NavisMcp.Server.Infrastructure;

public sealed class SessionRegistry
{
    private readonly AppPaths _paths;
    private readonly ILogger<SessionRegistry> _logger;

    public SessionRegistry(AppPaths paths, ILogger<SessionRegistry> logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public IReadOnlyList<SessionDescriptor> ListTargets()
    {
        var results = new List<SessionDescriptor>();
        foreach (var file in Directory.EnumerateFiles(_paths.SessionsPath, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var descriptor = JsonSerializer.Deserialize<SessionDescriptor>(json, JsonDefaults.Options);
                if (descriptor is null)
                {
                    continue;
                }

                if (!IsProcessAlive(descriptor.ProcessId))
                {
                    TryDelete(file);
                    TryDelete(GetTokenFilePath(file));
                    continue;
                }

                descriptor.AuthToken = ReadAuthToken(file);
                results.Add(descriptor);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Ignoring invalid session descriptor {File}", file);
            }
        }

        return results
            .OrderByDescending(x => x.LastSeenUtc)
            .ThenByDescending(x => x.CreatedUtc)
            .ToList();
    }

    public IReadOnlyList<SessionDescriptor> ListPublicTargets()
    {
        return ListTargets()
            .Select(ToPublicDescriptor)
            .ToList();
    }

    public SessionDescriptor ResolveTarget(string? targetId)
    {
        var targets = ListTargets();
        if (!string.IsNullOrWhiteSpace(targetId))
        {
            var exact = targets.FirstOrDefault(x => string.Equals(x.TargetId, targetId, StringComparison.OrdinalIgnoreCase));
            if (exact is not null)
            {
                return exact;
            }

            throw new NavisRpcException("target_not_found", $"No active Navisworks target found for '{targetId}'.");
        }

        var latest = targets.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.DocumentTitle))
            ?? targets.FirstOrDefault();
        if (latest is null)
        {
            throw new NavisRpcException("no_target", "No active Navisworks MCP plug-in session was found. Launch Navisworks Manage 2026 and ensure the dev bundle is installed.");
        }

        return latest;
    }

    private static bool IsProcessAlive(int processId)
    {
        try
        {
            _ = Process.GetProcessById(processId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void TryDelete(string file)
    {
        try { File.Delete(file); }
        catch { }
    }

    public static SessionDescriptor ToPublicDescriptor(SessionDescriptor descriptor)
    {
        return new SessionDescriptor
        {
            TargetId = descriptor.TargetId,
            PipeName = descriptor.PipeName,
            ProcessId = descriptor.ProcessId,
            ProcessName = descriptor.ProcessName,
            NavisworksVersion = descriptor.NavisworksVersion,
            PluginVersion = descriptor.PluginVersion,
            MachineName = descriptor.MachineName,
            UserName = descriptor.UserName,
            DocumentTitle = descriptor.DocumentTitle,
            DocumentPath = descriptor.DocumentPath,
            AuthToken = null,
            CreatedUtc = descriptor.CreatedUtc,
            LastSeenUtc = descriptor.LastSeenUtc
        };
    }

    private static string? ReadAuthToken(string sessionFile)
    {
        var tokenFile = GetTokenFilePath(sessionFile);
        if (!File.Exists(tokenFile))
        {
            return null;
        }

        var token = File.ReadAllText(tokenFile).Trim();
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }

    private static string GetTokenFilePath(string sessionFile)
    {
        return sessionFile + NavisMcpDefaults.SessionTokenFileSuffix;
    }
}
