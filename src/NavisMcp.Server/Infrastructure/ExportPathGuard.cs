namespace NavisMcp.Server.Infrastructure;

public static class ExportPathGuard
{
    public static string ResolveExportPath(ServerOptions options, string fileNameOrPath)
    {
        if (string.IsNullOrWhiteSpace(fileNameOrPath))
        {
            fileNameOrPath = $"navis-export-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.nwd";
        }

        var exportRoot = Path.GetFullPath(options.ExportsRoot);
        Directory.CreateDirectory(exportRoot);

        string candidate;
        if (Path.IsPathRooted(fileNameOrPath))
        {
            candidate = Path.GetFullPath(fileNameOrPath);
        }
        else
        {
            candidate = Path.GetFullPath(Path.Combine(exportRoot, fileNameOrPath));
        }

        if (!string.Equals(Path.GetExtension(candidate), ".nwd", StringComparison.OrdinalIgnoreCase))
        {
            candidate += ".nwd";
        }

        var normalizedRoot = exportRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new NavisRpcException("export_path_denied", $"Exports are restricted to '{exportRoot}'.");
        }

        PathGuardPolicy.EnsureNoReparsePointInPath(exportRoot, candidate, "export_path_denied", "Export");
        Directory.CreateDirectory(Path.GetDirectoryName(candidate)!);
        return candidate;
    }
}
