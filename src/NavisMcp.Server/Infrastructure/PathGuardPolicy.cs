namespace NavisMcp.Server.Infrastructure;

public static class PathGuardPolicy
{
    public static void EnsureNoReparsePointInPath(string root, string candidate, string errorCode, string label)
    {
        var rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var candidateFull = Path.GetFullPath(candidate);
        var directory = Directory.Exists(candidateFull) ? candidateFull : Path.GetDirectoryName(candidateFull);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        CheckDirectory(rootFull, errorCode, label);
        var relative = Path.GetRelativePath(rootFull, directory);
        if (relative == "." || string.IsNullOrWhiteSpace(relative))
        {
            return;
        }

        var current = rootFull;
        foreach (var segment in relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if (Directory.Exists(current))
            {
                CheckDirectory(current, errorCode, label);
            }
        }
    }

    private static void CheckDirectory(string path, string errorCode, string label)
    {
        var attributes = File.GetAttributes(path);
        if ((attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
        {
            throw new NavisRpcException(errorCode, $"{label} paths cannot pass through a junction or symbolic link.");
        }
    }
}
