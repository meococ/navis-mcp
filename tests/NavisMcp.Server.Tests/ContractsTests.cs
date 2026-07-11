using System.Text.Json;
using NavisMcp.Contracts;
using Xunit;

namespace NavisMcp.Server.Tests;

public sealed class ContractsTests
{
    [Fact]
    public void RpcRequest_RoundTrips_WithCamelCaseJson()
    {
        var request = new NavisRpcRequest
        {
            Id = "abc",
            Method = "nwd_get_model_tree",
            TargetId = "target",
            Params = JsonSerializer.SerializeToElement(new ModelTreeParams { MaxDepth = 2, MaxNodes = 10, ScopeItemId = "mi:15" }, JsonDefaults.Options)
        };

        var json = JsonSerializer.Serialize(request, JsonDefaults.Options);
        var copy = JsonSerializer.Deserialize<NavisRpcRequest>(json, JsonDefaults.Options);

        Assert.NotNull(copy);
        Assert.Equal("abc", copy!.Id);
        Assert.Equal("nwd_get_model_tree", copy.Method);
        Assert.Equal(2, copy.Params.GetProperty("maxDepth").GetInt32());
        Assert.Equal("mi:15", copy.Params.GetProperty("scopeItemId").GetString());
    }

    [Fact]
    public void ViewportSnapshotParams_RoundTrips_WithCamelCaseJson()
    {
        var parameters = new ViewportSnapshotParams
        {
            FilePath = "C:\\snapshots\\view.png",
            RelativePath = "snapshots\\view.png",
            Width = 800,
            Height = 600,
            Format = "png",
            Style = "scene_plus_overlay",
            Framing = "current",
            EnableSectioning = false,
            MaxTimeHintSeconds = 1.5,
            RestoreView = true
        };

        var json = JsonSerializer.Serialize(parameters, JsonDefaults.Options);
        var copy = JsonSerializer.Deserialize<ViewportSnapshotParams>(json, JsonDefaults.Options);

        Assert.Contains("\"maxTimeHintSeconds\":1.5", json);
        Assert.NotNull(copy);
        Assert.Equal("current", copy!.Framing);
        Assert.False(copy.EnableSectioning);
        Assert.True(copy.RestoreView);
    }

    [Fact]
    public void AdvancedRequestAndReportDtos_RoundTrip_WithCamelCaseJson()
    {
        var request = new NavisRpcRequest
        {
            Id = "report-1",
            Method = "nwd_export_items_table",
            TargetId = "target-1",
            Context = new RequestContext
            {
                AuthToken = "token",
                AllowWrites = true,
                ExportsRoot = "C:\\project\\exports",
                SnapshotsRoot = "C:\\project\\snapshots",
                ReportsRoot = "C:\\project\\reports",
                ModelInputRoot = "C:\\project\\models"
            },
            Params = JsonSerializer.SerializeToElement(new ItemsTableExportParams
            {
                FilePath = "C:\\project\\reports\\items.csv",
                RelativePath = "reports\\items.csv",
                Query = "cau",
                ScopeItemId = "mi:15",
                Filter = new SearchFilter
                {
                    SourceModelContains = "bridge",
                    PropertyNameContains = "material",
                    PropertyValueContains = "concrete"
                }
            }, JsonDefaults.Options)
        };

        var json = JsonSerializer.Serialize(request, JsonDefaults.Options);
        var copy = JsonSerializer.Deserialize<NavisRpcRequest>(json, JsonDefaults.Options);
        var exportParams = copy!.Params.Deserialize<ItemsTableExportParams>(JsonDefaults.Options);

        Assert.Contains("\"allowWrites\":true", json);
        Assert.Equal("token", copy.Context!.AuthToken);
        Assert.Equal("C:\\project\\reports", copy.Context.ReportsRoot);
        Assert.Equal("mi:15", exportParams!.ScopeItemId);
        Assert.Equal("bridge", exportParams!.Filter!.SourceModelContains);

        var report = new ReportResult
        {
            Written = true,
            FilePath = "C:\\project\\reports\\items.csv",
            RelativePath = "reports\\items.csv",
            Format = "csv",
            Length = 42,
            DocumentTitle = "model.nwf",
            ItemCount = 2,
            Message = "ok"
        };

        var reportJson = JsonSerializer.Serialize(report, JsonDefaults.Options);
        var reportCopy = JsonSerializer.Deserialize<ReportResult>(reportJson, JsonDefaults.Options);

        Assert.Contains("\"relativePath\":\"reports\\\\items.csv\"", reportJson);
        Assert.True(reportCopy!.Written);
        Assert.Equal(2, reportCopy.ItemCount);
    }

    [Fact]
    public void ClashReportDtos_RoundTrip_WithDetailedColumns()
    {
        var result = new ClashResultInfo
        {
            Id = "clash-1",
            DisplayName = "Clash1",
            Status = "New",
            Distance = "-0.372",
            Description = "Hard",
            ClashPoint = "x:610780.108, y:1198300.289, z:4.835",
            ImageRelativePath = "clash-report_images/clash-0001.png",
            Item1Detail = new ClashItemDetail { ItemId = "Element ID: A", Layer = "Undefined", ItemName = "CBV-N4", ItemType = "IfcSlab" },
            Item2Detail = new ClashItemDetail { ItemId = "Entity Handle: 70FC", Layer = "Thoatnuoc", ItemName = "Thoat nuoc", ItemType = "Pipe" }
        };

        var parameters = new ClashReportExportParams
        {
            TestId = "test-1",
            RelativePath = "reports\\ckn.html",
            IncludeImages = true,
            ImageWidth = 240,
            ImageHeight = 160
        };

        var json = JsonSerializer.Serialize(new { result, parameters }, JsonDefaults.Options);
        var copy = JsonSerializer.Deserialize<JsonElement>(json, JsonDefaults.Options);

        Assert.Contains("\"item1Detail\"", json);
        Assert.Contains("\"includeImages\":true", json);
        Assert.Equal("Hard", copy.GetProperty("result").GetProperty("description").GetString());
        Assert.Equal(240, copy.GetProperty("parameters").GetProperty("imageWidth").GetInt32());
    }
}
