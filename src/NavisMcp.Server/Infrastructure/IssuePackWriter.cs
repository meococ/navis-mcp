using System.Text;
using System.Text.Json;
using NavisMcp.Contracts;

namespace NavisMcp.Server.Infrastructure;

public static class IssuePackWriter
{
    public static IssuePackResult Write(IssuePackParams parameters)
    {
        var title = string.IsNullOrWhiteSpace(parameters.Title) ? "NavisMcp Issue Pack" : parameters.Title.Trim();
        var status = string.IsNullOrWhiteSpace(parameters.Status) ? "open" : parameters.Status.Trim();
        var items = parameters.Items ?? new List<ItemRef>();
        if (items.Count == 0 && parameters.ItemIds != null)
        {
            items = parameters.ItemIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => new ItemRef { Id = id.Trim() })
                .ToList();
        }

        var payload = new Dictionary<string, object?>
        {
            ["title"] = title,
            ["status"] = status,
            ["assignee"] = parameters.Assignee,
            ["priority"] = parameters.Priority,
            ["description"] = parameters.Description,
            ["generatedUtc"] = DateTimeOffset.UtcNow,
            ["itemIds"] = items.Select(i => i.Id).ToList(),
            ["items"] = items,
            ["snapshotPath"] = parameters.SnapshotPath,
            ["snapshotRelativePath"] = parameters.SnapshotRelativePath,
            ["clashTestId"] = parameters.ClashTestId,
            ["clashResultId"] = parameters.ClashResultId,
            ["clashPoint"] = parameters.ClashPoint,
            ["distance"] = parameters.Distance,
            ["metadata"] = parameters.Metadata ?? new Dictionary<string, string>()
        };

        Directory.CreateDirectory(Path.GetDirectoryName(parameters.FilePath)!);
        File.WriteAllText(parameters.FilePath, JsonSerializer.Serialize(payload, JsonDefaults.Options));

        if (!string.IsNullOrWhiteSpace(parameters.MarkdownFilePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(parameters.MarkdownFilePath)!);
            File.WriteAllText(parameters.MarkdownFilePath, BuildMarkdown(title, status, parameters, items), Encoding.UTF8);
        }

        return new IssuePackResult
        {
            Written = File.Exists(parameters.FilePath),
            FilePath = parameters.FilePath,
            RelativePath = parameters.RelativePath,
            MarkdownFilePath = parameters.MarkdownFilePath ?? string.Empty,
            MarkdownRelativePath = parameters.MarkdownRelativePath ?? string.Empty,
            Title = title,
            Status = status,
            ItemCount = items.Count,
            GeneratedUtc = DateTimeOffset.UtcNow,
            Message = "Issue pack written under reports/."
        };
    }

    private static string BuildMarkdown(string title, string status, IssuePackParams parameters, List<ItemRef> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# " + title);
        sb.AppendLine();
        sb.AppendLine("- Status: " + status);
        if (!string.IsNullOrWhiteSpace(parameters.Assignee))
        {
            sb.AppendLine("- Assignee: " + parameters.Assignee);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Priority))
        {
            sb.AppendLine("- Priority: " + parameters.Priority);
        }

        if (!string.IsNullOrWhiteSpace(parameters.ClashTestId))
        {
            sb.AppendLine("- Clash test: " + parameters.ClashTestId);
        }

        if (!string.IsNullOrWhiteSpace(parameters.ClashResultId))
        {
            sb.AppendLine("- Clash result: " + parameters.ClashResultId);
        }

        if (!string.IsNullOrWhiteSpace(parameters.SnapshotRelativePath) || !string.IsNullOrWhiteSpace(parameters.SnapshotPath))
        {
            sb.AppendLine("- Snapshot: " + (parameters.SnapshotRelativePath ?? parameters.SnapshotPath));
        }

        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(parameters.Description))
        {
            sb.AppendLine("## Description");
            sb.AppendLine();
            sb.AppendLine(parameters.Description);
            sb.AppendLine();
        }

        sb.AppendLine("## Items");
        sb.AppendLine();
        if (items.Count == 0)
        {
            sb.AppendLine("_No item ids provided._");
        }
        else
        {
            foreach (var item in items)
            {
                sb.AppendLine("- `" + item.Id + "` " + (item.DisplayName ?? string.Empty));
            }
        }

        sb.AppendLine();
        sb.AppendLine("## Evidence JSON");
        sb.AppendLine();
        sb.AppendLine("See companion JSON issue pack for machine-readable fields.");
        return sb.ToString();
    }
}
