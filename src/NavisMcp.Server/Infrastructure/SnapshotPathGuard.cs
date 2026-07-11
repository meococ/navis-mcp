namespace NavisMcp.Server.Infrastructure;

public static class SnapshotPathGuard
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg"
    };

    public static GuardedPath ResolveSnapshotPath(ServerOptions options, string fileNameOrPath, string format)
    {
        var snapshotRoot = Path.GetFullPath(options.SnapshotsRoot);
        Directory.CreateDirectory(snapshotRoot);

        var normalizedFormat = NormalizeFormat(format);
        if (string.IsNullOrWhiteSpace(fileNameOrPath))
        {
            fileNameOrPath = $"navis-snapshot-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss-fff}.{normalizedFormat}";
        }

        string candidate;
        if (Path.IsPathRooted(fileNameOrPath))
        {
            candidate = Path.GetFullPath(fileNameOrPath);
        }
        else
        {
            candidate = Path.GetFullPath(Path.Combine(snapshotRoot, fileNameOrPath));
        }

        var extension = Path.GetExtension(candidate);
        if (string.IsNullOrWhiteSpace(extension))
        {
            candidate += "." + normalizedFormat;
            extension = Path.GetExtension(candidate);
        }

        if (!AllowedExtensions.Contains(extension))
        {
            throw new NavisRpcException("invalid_snapshot_format", "Snapshot format must be png, jpg, or jpeg.");
        }

        normalizedFormat = NormalizeFormat(extension.TrimStart('.'));
        var normalizedRoot = snapshotRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new NavisRpcException("snapshot_path_denied", $"Snapshots are restricted to '{snapshotRoot}'.");
        }

        PathGuardPolicy.EnsureNoReparsePointInPath(snapshotRoot, candidate, "snapshot_path_denied", "Snapshot");
        Directory.CreateDirectory(Path.GetDirectoryName(candidate)!);
        return new GuardedPath(candidate, Path.GetRelativePath(options.ProjectRoot, candidate), normalizedFormat);
    }

    private static string NormalizeFormat(string format)
    {
        var value = (format ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant();
        if (value.Length == 0)
        {
            value = "png";
        }

        if (value == "jpeg")
        {
            value = "jpg";
        }

        if (value != "png" && value != "jpg")
        {
            throw new NavisRpcException("invalid_snapshot_format", "Snapshot format must be png, jpg, or jpeg.");
        }

        return value;
    }
}
