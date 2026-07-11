using NavisMcp.Contracts;

namespace NavisMcp.Server.Infrastructure;

public static class PlaybookCatalog
{
    private static readonly (string Id, string FileName, string Title)[] Catalog =
    {
        ("coord.phase0_selection_qa", "coord.phase0_selection_qa.md", "Phase 0 selection QA"),
        ("coord.official_clash_batch", "coord.official_clash_batch.md", "Official clash batch"),
        ("coord.meeting_pack", "coord.meeting_pack.md", "Meeting pack"),
        ("safety.preflight", "safety.preflight.md", "Safety preflight")
    };

    public static PlaybookListResult List()
    {
        return new PlaybookListResult
        {
            Playbooks = Catalog.Select(entry => new PlaybookInfo
            {
                Id = entry.Id,
                Title = entry.Title,
                RelativePath = Path.Combine("Playbooks", entry.FileName)
            }).ToList()
        };
    }

    public static PlaybookContentResult Get(string id)
    {
        var normalized = (id ?? string.Empty).Trim();
        var entry = Catalog.FirstOrDefault(x =>
            string.Equals(x.Id, normalized, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Path.GetFileNameWithoutExtension(x.FileName), normalized, StringComparison.OrdinalIgnoreCase));

        if (entry.Id is null)
        {
            throw new NavisRpcException("playbook_not_found", $"Unknown playbook '{id}'.");
        }

        var path = ResolvePlaybookPath(entry.FileName);
        if (!File.Exists(path))
        {
            throw new NavisRpcException("playbook_missing", $"Playbook file was not found at '{path}'.");
        }

        return new PlaybookContentResult
        {
            Id = entry.Id,
            Title = entry.Title,
            RelativePath = Path.Combine("Playbooks", entry.FileName),
            Markdown = File.ReadAllText(path)
        };
    }

    public static string ResolvePlaybookDirectory()
    {
        var candidates = new List<string>
        {
            Path.Combine(AppContext.BaseDirectory, "Playbooks")
        };

        // Walk up from BaseDirectory looking for src/NavisMcp.Server/Playbooks (dev/test) or packaged Playbooks.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            candidates.Add(Path.Combine(dir.FullName, "Playbooks"));
            candidates.Add(Path.Combine(dir.FullName, "src", "NavisMcp.Server", "Playbooks"));
        }

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (Directory.Exists(candidate) && Directory.EnumerateFiles(candidate, "*.md").Any())
            {
                return candidate;
            }
        }

        return candidates[0];
    }

    private static string ResolvePlaybookPath(string fileName)
    {
        return Path.Combine(ResolvePlaybookDirectory(), fileName);
    }
}
