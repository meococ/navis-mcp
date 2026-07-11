namespace NavisMcp.Server.Infrastructure;

public static class ReportPathGuard
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".csv",
        ".json",
        ".html",
        ".htm",
        ".md",
        ".xml",
        ".zip",
        ".bcfzip"
    };

    public static GuardedPath ResolveReportPath(ServerOptions options, string fileNameOrPath, string format, string defaultPrefix = "navis-report")
    {
        var reportsRoot = Path.GetFullPath(options.ReportsRoot);
        Directory.CreateDirectory(reportsRoot);

        var normalizedFormat = NormalizeFormat(format);
        if (string.IsNullOrWhiteSpace(fileNameOrPath))
        {
            fileNameOrPath = $"{defaultPrefix}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss-fff}.{normalizedFormat}";
        }

        string candidate;
        if (Path.IsPathRooted(fileNameOrPath))
        {
            candidate = Path.GetFullPath(fileNameOrPath);
        }
        else
        {
            candidate = Path.GetFullPath(Path.Combine(reportsRoot, fileNameOrPath));
        }

        var extension = Path.GetExtension(candidate);
        if (string.IsNullOrWhiteSpace(extension))
        {
            candidate += "." + normalizedFormat;
            extension = Path.GetExtension(candidate);
        }

        if (!AllowedExtensions.Contains(extension))
        {
            throw new NavisRpcException("invalid_report_format", "Report format must be csv, json, html, md, xml, zip, or bcfzip.");
        }

        normalizedFormat = NormalizeFormat(extension.TrimStart('.'));
        var normalizedRoot = reportsRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new NavisRpcException("report_path_denied", $"Reports are restricted to '{reportsRoot}'.");
        }

        PathGuardPolicy.EnsureNoReparsePointInPath(reportsRoot, candidate, "report_path_denied", "Report");
        Directory.CreateDirectory(Path.GetDirectoryName(candidate)!);
        return new GuardedPath(candidate, Path.GetRelativePath(options.ProjectRoot, candidate), normalizedFormat);
    }

    private static string NormalizeFormat(string format)
    {
        var value = (format ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant();
        if (value.Length == 0)
        {
            value = "json";
        }

        if (value == "htm")
        {
            value = "html";
        }

        if (value != "csv" && value != "json" && value != "html" && value != "md" && value != "xml" && value != "zip" && value != "bcfzip")
        {
            throw new NavisRpcException("invalid_report_format", "Report format must be csv, json, html, md, xml, zip, or bcfzip.");
        }

        return value;
    }
}
