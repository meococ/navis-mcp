using System.ComponentModel;
using ModelContextProtocol.Server;
using NavisMcp.Contracts;
using NavisMcp.Server.Infrastructure;

namespace NavisMcp.Server.Tools;

[McpServerToolType]
[Description("Clash Detective tools for list, run, create, export, and cluster.")]
public static class NavisClashTools
{
    [McpServerTool]
    [Description("List Clash Detective tests when available in Navisworks Manage.")]
    public static Task<object> nwd_list_clash_tests(
        PipeRpcClient client,
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        return ToolInvocation.Read<ClashTestListResult>(client, "nwd_list_clash_tests", null, targetId, NavisMcpDefaults.LongTimeoutMs);
    }

    [McpServerTool]
    [Description("Return clash result summaries for a Clash Detective test.")]
    public static Task<object> nwd_get_clash_results(
        PipeRpcClient client,
        ServerOptions options,
        [Description("Clash test id from nwd_list_clash_tests.")] string testId,
        [Description("Maximum clash results to return.")] int maxResults = 100,
        [Description("Optional result cursor from a previous call.")] string? cursor = null,
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        return ToolInvocation.Read<ClashResultsResult>(client, "nwd_get_clash_results", new ClashTestLookupParams
        {
            TestId = testId,
            MaxResults = maxResults,
            Cursor = cursor
        }, targetId, NavisMcpDefaults.LongTimeoutMs, result => BriefResponseHelper.MaybeTruncate(result, options.Brief));
    }

    [McpServerTool]
    [Description("Run one Clash Detective test and return updated result summaries. Requires --allow-writes.")]
    public static Task<object> nwd_run_clash_test(
        PipeRpcClient client,
        WriteGate writeGate,
        AuditLogger audit,
        [Description("Clash test id from nwd_list_clash_tests.")] string testId,
        [Description("Maximum clash results to return after running.")] int maxResults = 100,
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        var parameters = new ClashTestLookupParams { TestId = testId, MaxResults = maxResults };
        return ToolInvocation.Write<ClashResultsResult>(client, writeGate, audit, "nwd_run_clash_test", parameters, targetId, NavisMcpDefaults.LongTimeoutMs);
    }

    [McpServerTool]
    [Description("Create a Clash Detective test when supported. Requires --allow-writes.")]
    public static Task<object> nwd_create_clash_test(
        PipeRpcClient client,
        WriteGate writeGate,
        AuditLogger audit,
        string displayName,
        string selectionA = "",
        string selectionB = "",
        string clashType = "hard",
        double tolerance = 0,
        bool compositeObjectClashing = true,
        string? targetId = null)
    {
        var parameters = new ClashTestDefinition { DisplayName = displayName, SelectionA = selectionA, SelectionB = selectionB, ClashType = clashType, Tolerance = tolerance, CompositeObjectClashing = compositeObjectClashing };
        return ToolInvocation.Write<ClashTestInfo>(client, writeGate, audit, "nwd_create_clash_test", parameters, targetId, NavisMcpDefaults.LongTimeoutMs);
    }

    [McpServerTool]
    [Description("Update clash result status, comment, and/or assignment. Requires --allow-writes.")]
    public static Task<object> nwd_update_clash_result(
        PipeRpcClient client,
        WriteGate writeGate,
        AuditLogger audit,
        string testId,
        string resultId,
        string? status = null,
        string? comment = null,
        string? assignedTo = null,
        string? targetId = null)
    {
        var parameters = new ClashResultUpdate { TestId = testId, ResultId = resultId, Status = status, Comment = comment, AssignedTo = assignedTo };
        return ToolInvocation.Write<ClashResultInfo>(client, writeGate, audit, "nwd_update_clash_result", parameters, targetId, NavisMcpDefaults.LongTimeoutMs);
    }

    [McpServerTool]
    [Description("Export clash results report under reports/. Requires --allow-writes.")]
    public static Task<object> nwd_export_clash_report(
        PipeRpcClient client,
        WriteGate writeGate,
        AuditLogger audit,
        ServerOptions options,
        string testId,
        string fileName = "",
        string format = "html",
        int maxResults = 500,
        bool includeImages = true,
        int imageWidth = 220,
        int imageHeight = 140,
        string? targetId = null)
    {
        return ToolInvocation.Run(async () =>
        {
            writeGate.EnsureAllowed();
            var path = ReportPathGuard.ResolveReportPath(options, fileName, format, "clash-report");
            var parameters = new ClashReportExportParams { TestId = testId, FilePath = path.FilePath, RelativePath = path.RelativePath, Format = path.Format, MaxResults = maxResults, IncludeImages = includeImages, ImageWidth = imageWidth, ImageHeight = imageHeight };
            await audit.WriteAsync("nwd_export_clash_report", new { targetId, parameters }, CancellationToken.None).ConfigureAwait(false);
            var data = await client.InvokeAsync<ReportResult>("nwd_export_clash_report", parameters, targetId, NavisMcpDefaults.LongTimeoutMs).ConfigureAwait(false);
            return ToolInvocation.ToolEnvelope.Success(data);
        });
    }

    [McpServerTool]
    [Description("Cluster clash results by rounded clash point, model pair, and distance bucket. Fetches results when none are supplied.")]
    public static Task<object> nwd_cluster_clash_results(
        PipeRpcClient client,
        ServerOptions options,
        [Description("Clash test id from nwd_list_clash_tests.")] string testId,
        [Description("Maximum clash results to fetch when results are not supplied.")] int maxResults = 500,
        [Description("Rounding precision for clash point coordinates.")] double pointPrecision = 0.1,
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        return ToolInvocation.Run(async () =>
        {
            var parameters = new ClashClusterParams
            {
                TestId = testId,
                MaxResults = maxResults,
                PointPrecision = pointPrecision
            };
            var source = await client.InvokeAsync<ClashResultsResult>(
                "nwd_get_clash_results",
                new ClashTestLookupParams { TestId = testId, MaxResults = maxResults },
                targetId,
                NavisMcpDefaults.LongTimeoutMs).ConfigureAwait(false);
            var clustered = ClashClustering.Cluster(parameters, source.Results);
            clustered.Truncated = source.Truncated || clustered.Truncated;
            return ToolInvocation.ToolEnvelope.Success(BriefResponseHelper.MaybeTruncate(clustered, options.Brief));
        });
    }
}
