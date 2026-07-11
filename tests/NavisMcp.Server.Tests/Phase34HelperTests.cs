using System.IO.Compression;
using System.Text.Json;
using NavisMcp.Contracts;
using NavisMcp.Server.Infrastructure;
using NavisMcp.Server.Playbooks;
using Xunit;

namespace NavisMcp.Server.Tests;

public sealed class Phase34HelperTests
{
    [Fact]
    public void DomainOntology_IncludesCoreEnAndViPacks()
    {
        var packs = DomainOntology.ListPacks().Packs;
        Assert.Contains(packs, p => p.Id == "en.structure");
        Assert.Contains(packs, p => p.Id == "en.mep");
        Assert.Contains(packs, p => p.Id == "en.architecture");
        Assert.Contains(packs, p => p.Id == "en.civil");
        Assert.Contains(packs, p => p.Id == "vi.cau");
        Assert.Contains(packs, p => p.Id == "vi.bmc");
        Assert.Contains(packs, p => p.Id == "vi.khe");
        Assert.Contains(packs, p => p.Id == "vi.lan_can");
        Assert.All(packs.Where(p => p.Id.StartsWith("en.", StringComparison.Ordinal)), p => Assert.Equal("en", p.Locale));
        Assert.All(packs.Where(p => p.Id.StartsWith("vi.", StringComparison.Ordinal)), p => Assert.Equal("vi", p.Locale));
        Assert.All(packs, p => Assert.NotEmpty(p.FalsePositiveNotes));
        Assert.All(packs, p => Assert.NotEmpty(p.SearchQueries));
    }

    [Fact]
    public void ClashClustering_GroupsByRoundedPointModelPairAndDistance()
    {
        var results = new List<ClashResultInfo>
        {
            new()
            {
                Id = "a",
                ClashPoint = "1.04, 2.01, 3.02",
                Distance = "0.02",
                Item1 = new ItemRef { ModelName = "Struct" },
                Item2 = new ItemRef { ModelName = "MEP" }
            },
            new()
            {
                Id = "b",
                ClashPoint = "1.05, 2.02, 3.01",
                Distance = "0.015",
                Item1 = new ItemRef { ModelName = "MEP" },
                Item2 = new ItemRef { ModelName = "Struct" }
            },
            new()
            {
                Id = "c",
                ClashPoint = "10, 20, 30",
                Distance = "0.8",
                Item1 = new ItemRef { ModelName = "Civil" },
                Item2 = new ItemRef { ModelName = "Struct" }
            }
        };

        var clustered = ClashClustering.Cluster(new ClashClusterParams
        {
            TestId = "test-1",
            PointPrecision = 0.1,
            DistanceBuckets = new List<double> { 0.05, 0.1, 0.5, 1.0 }
        }, results);

        Assert.Equal("test-1", clustered.TestId);
        Assert.Equal(3, clustered.SourceResultCount);
        Assert.Equal(2, clustered.ClusterCount);
        var primary = clustered.Clusters.Single(c => c.Count == 2);
        Assert.Equal(2, primary.ResultIds.Count);
        Assert.Contains("a", primary.ResultIds);
        Assert.Contains("b", primary.ResultIds);
        Assert.Equal("MEP x Struct", primary.ModelPair);
        Assert.Equal("<=0.05", primary.DistanceBucket);
    }

    [Fact]
    public void BcfExport_CreatesMinimalZipFromIssuePack()
    {
        var root = Path.Combine(Path.GetTempPath(), "navis-mcp-bcf-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var packPath = Path.Combine(root, "issue.json");
            File.WriteAllText(packPath, JsonSerializer.Serialize(new
            {
                title = "Pipe vs Beam",
                status = "open",
                description = "Clearance clash near abutment"
            }, JsonDefaults.Options));

            var output = Path.Combine(root, "issue.bcfzip");
            var result = BcfExport.CreateFromIssuePack(packPath, output, "reports/issue.bcfzip");

            Assert.True(result.Written);
            Assert.True(File.Exists(output));
            using var zip = ZipFile.OpenRead(output);
            Assert.NotNull(zip.GetEntry("bcf.version"));
            Assert.Contains(zip.Entries, e => e.FullName.EndsWith("markup.bcf", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(zip.Entries, e => e.FullName.EndsWith("viewpoint.bcfv", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void PlaybookCatalog_ListsAndLoadsMarkdown()
    {
        var list = PlaybookCatalog.List();
        Assert.Equal(4, list.Playbooks.Count);
        Assert.Contains(list.Playbooks, p => p.Id == "coord.phase0_selection_qa");
        Assert.Contains(list.Playbooks, p => p.Id == "safety.preflight");

        var content = PlaybookCatalog.Get("coord.official_clash_batch");
        Assert.Equal("coord.official_clash_batch", content.Id);
        Assert.Contains("nwd_create_clash_test", content.Markdown);
    }

    [Fact]
    public void BriefResponseHelper_TruncatesFindAndClashLists()
    {
        var find = new ItemSearchResult
        {
            Items = Enumerable.Range(0, 50).Select(i => new ItemRef { Id = "mi:" + i }).ToList(),
            ReturnedCount = 50
        };
        var truncatedFind = BriefResponseHelper.MaybeTruncate(find, brief: true, limit: 10);
        Assert.True(truncatedFind.Truncated);
        Assert.Equal(10, truncatedFind.Items.Count);

        var clash = new ClashResultsResult
        {
            Results = Enumerable.Range(0, 40).Select(i => new ClashResultInfo { Id = "c" + i }).ToList()
        };
        var truncatedClash = BriefResponseHelper.MaybeTruncate(clash, brief: true, limit: 5);
        Assert.True(truncatedClash.Truncated);
        Assert.Equal(5, truncatedClash.Results.Count);
    }

    [Fact]
    public void IssuePackWriter_WritesJsonAndMarkdown()
    {
        var root = Path.Combine(Path.GetTempPath(), "navis-mcp-issue-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var json = Path.Combine(root, "pack.json");
            var md = Path.Combine(root, "pack.md");
            var result = IssuePackWriter.Write(new IssuePackParams
            {
                FilePath = json,
                RelativePath = "reports/pack.json",
                MarkdownFilePath = md,
                MarkdownRelativePath = "reports/pack.md",
                Title = "Test Issue",
                Status = "open",
                ItemIds = new List<string> { "mi:1", "mi:2" },
                ClashTestId = "test",
                Description = "Evidence pack"
            });

            Assert.True(result.Written);
            Assert.Equal(2, result.ItemCount);
            Assert.True(File.Exists(json));
            Assert.True(File.Exists(md));
            Assert.Contains("Test Issue", File.ReadAllText(md));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
