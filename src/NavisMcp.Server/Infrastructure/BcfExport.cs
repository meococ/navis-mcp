using System.IO.Compression;
using System.Text;
using System.Text.Json;
using NavisMcp.Contracts;

namespace NavisMcp.Server.Infrastructure;

public static class BcfExport
{
    public static BcfExportResult CreateFromIssuePack(string issuePackPath, string outputPath, string relativePath, string? titleOverride = null)
    {
        if (!File.Exists(issuePackPath))
        {
            throw new NavisRpcException("issue_pack_not_found", $"Issue pack JSON was not found at '{issuePackPath}'.");
        }

        using var stream = File.OpenRead(issuePackPath);
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;

        var title = FirstNonEmpty(
            titleOverride,
            TryGetString(root, "title"),
            Path.GetFileNameWithoutExtension(issuePackPath),
            "NavisMcp Issue");
        var description = TryGetString(root, "description") ?? string.Empty;
        var status = TryGetString(root, "status") ?? "open";
        var topicGuid = Guid.NewGuid().ToString("D");
        var viewpointGuid = Guid.NewGuid().ToString("D");

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        using (var zip = ZipFile.Open(outputPath, ZipArchiveMode.Create))
        {
            WriteEntry(zip, "bcf.version",
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine +
                "<Version VersionId=\"2.1\" xmlns=\"http://www.buildingsmart-tech.org/bcf/version\">" + Environment.NewLine +
                "  <DetailedVersion>2.1</DetailedVersion>" + Environment.NewLine +
                "</Version>" + Environment.NewLine);

            var markup = new StringBuilder();
            markup.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            markup.AppendLine("<Markup xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">");
            markup.AppendLine("  <Topic Guid=\"" + topicGuid + "\" TopicType=\"Issue\" TopicStatus=\"" + Xml(status) + "\">");
            markup.AppendLine("    <Title>" + Xml(title) + "</Title>");
            markup.AppendLine("    <CreationDate>" + DateTimeOffset.UtcNow.ToString("o") + "</CreationDate>");
            markup.AppendLine("    <CreationAuthor>NavisMcp</CreationAuthor>");
            if (!string.IsNullOrWhiteSpace(description))
            {
                markup.AppendLine("    <Description>" + Xml(description) + "</Description>");
            }

            markup.AppendLine("  </Topic>");
            markup.AppendLine("  <Viewpoints Guid=\"" + viewpointGuid + "\">");
            markup.AppendLine("    <Viewpoint>viewpoint.bcfv</Viewpoint>");
            markup.AppendLine("  </Viewpoints>");
            markup.AppendLine("</Markup>");
            WriteEntry(zip, topicGuid + "/markup.bcf", markup.ToString());

            var viewpoint = new StringBuilder();
            viewpoint.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            viewpoint.AppendLine("<VisualizationInfo Guid=\"" + viewpointGuid + "\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">");
            viewpoint.AppendLine("  <Components>");
            viewpoint.AppendLine("    <Selection />");
            viewpoint.AppendLine("    <Visibility DefaultVisibility=\"true\" />");
            viewpoint.AppendLine("  </Components>");
            viewpoint.AppendLine("  <PerspectiveCamera>");
            viewpoint.AppendLine("    <CameraViewPoint><X>0</X><Y>0</Y><Z>0</Z></CameraViewPoint>");
            viewpoint.AppendLine("    <CameraDirection><X>0</X><Y>0</Y><Z>-1</Z></CameraDirection>");
            viewpoint.AppendLine("    <CameraUpVector><X>0</X><Y>1</Y><Z>0</Z></CameraUpVector>");
            viewpoint.AppendLine("    <FieldOfView>60</FieldOfView>");
            viewpoint.AppendLine("  </PerspectiveCamera>");
            viewpoint.AppendLine("</VisualizationInfo>");
            WriteEntry(zip, topicGuid + "/viewpoint.bcfv", viewpoint.ToString());
        }

        var info = new FileInfo(outputPath);
        return new BcfExportResult
        {
            Written = info.Exists,
            FilePath = outputPath,
            RelativePath = relativePath,
            IssuePackPath = issuePackPath,
            Length = info.Exists ? info.Length : null,
            Message = "Minimal BCF 2.1 zip written from issue pack."
        };
    }

    private static void WriteEntry(ZipArchive zip, string entryName, string content)
    {
        var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
    }

    private static string? TryGetString(JsonElement root, string name)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        JsonElement property = default;
        var found = root.TryGetProperty(name, out property);
        if (!found)
        {
            foreach (var candidate in root.EnumerateObject())
            {
                if (string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    property = candidate.Value;
                    found = true;
                    break;
                }
            }
        }

        if (!found)
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return "NavisMcp Issue";
    }

    private static string Xml(string value)
    {
        return (value ?? string.Empty)
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}
