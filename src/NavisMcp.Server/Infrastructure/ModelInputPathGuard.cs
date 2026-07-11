namespace NavisMcp.Server.Infrastructure;

public static class ModelInputPathGuard
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".nwd",
        ".nwf",
        ".nwc",
        ".rvt",
        ".ifc",
        ".dwg",
        ".dxf",
        ".fbx"
    };

    public static string ResolveModelInputPath(ServerOptions options, string fileNameOrPath)
    {
        if (string.IsNullOrWhiteSpace(fileNameOrPath))
        {
            throw new NavisRpcException("invalid_model_path", "Model file path is required.");
        }

        var inputRoot = Path.GetFullPath(options.ModelInputRoot);
        Directory.CreateDirectory(inputRoot);

        string candidate;
        if (Path.IsPathRooted(fileNameOrPath))
        {
            candidate = Path.GetFullPath(fileNameOrPath);
        }
        else
        {
            candidate = Path.GetFullPath(Path.Combine(inputRoot, fileNameOrPath));
        }

        var extension = Path.GetExtension(candidate);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new NavisRpcException("invalid_model_format", "Model input must be nwd, nwf, nwc, rvt, ifc, dwg, dxf, or fbx.");
        }

        var normalizedRoot = inputRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new NavisRpcException("model_path_denied", $"Model inputs are restricted to '{inputRoot}'.");
        }

        PathGuardPolicy.EnsureNoReparsePointInPath(inputRoot, candidate, "model_path_denied", "Model input");
        if (!File.Exists(candidate))
        {
            throw new NavisRpcException("model_not_found", $"Model input '{candidate}' was not found.");
        }

        return candidate;
    }
}
