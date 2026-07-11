namespace NavisMcp.Server.Infrastructure;

public sealed class ServerOptions
{
    public bool AllowWrites { get; private set; }
    public bool Verbose { get; private set; }
    public bool Brief { get; private set; }
    public string ProjectRoot { get; private set; } = Directory.GetCurrentDirectory();
    public string? ModelInputRootOverride { get; private set; }
    public IReadOnlyList<string> Toolsets { get; private set; } = Array.Empty<string>();

    public string ExportsRoot => Path.GetFullPath(Path.Combine(ProjectRoot, "exports"));
    public string SnapshotsRoot => Path.GetFullPath(Path.Combine(ProjectRoot, "snapshots"));
    public string ReportsRoot => Path.GetFullPath(Path.Combine(ProjectRoot, "reports"));
    public string ModelInputRoot => Path.GetFullPath(ModelInputRootOverride ?? Path.Combine(ProjectRoot, "models"));

    public static ServerOptions Parse(string[] args)
    {
        var options = new ServerOptions();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.Equals(arg, "--allow-writes", StringComparison.OrdinalIgnoreCase))
            {
                options.AllowWrites = true;
            }
            else if (string.Equals(arg, "--verbose", StringComparison.OrdinalIgnoreCase))
            {
                options.Verbose = true;
            }
            else if (string.Equals(arg, "--brief", StringComparison.OrdinalIgnoreCase))
            {
                options.Brief = true;
            }
            else if (string.Equals(arg, "--project-root", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                options.ProjectRoot = Path.GetFullPath(args[++i]);
            }
            else if (string.Equals(arg, "--model-input-root", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                options.ModelInputRootOverride = Path.GetFullPath(args[++i]);
            }
            else if (string.Equals(arg, "--toolsets", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                options.Toolsets = ToolsetCatalog.ParseToolsets(args[++i]);
            }
        }

        return options;
    }
}
