using System.ComponentModel;
using ModelContextProtocol.Server;
using NavisMcp.Contracts;
using NavisMcp.Server.Infrastructure;

namespace NavisMcp.Server.Tools;

[McpServerToolType]
[Description("Advanced stub tools for appearance, TimeLiner, and quantification (opt-in via --toolsets advanced).")]
public static class NavisAdvancedTools
{
    [McpServerTool]
    [Description("Apply temporary color/transparency overrides to items when supported. Requires --allow-writes.")]
    public static Task<object> nwd_set_appearance_override(
        PipeRpcClient client,
        WriteGate writeGate,
        AuditLogger audit,
        string[] itemIds,
        string? color = null,
        double? transparency = null,
        string? targetId = null)
    {
        return ToolInvocation.Write<NotSupportedResult>(client, writeGate, audit, "nwd_set_appearance_override", new AppearanceOverride { ItemIds = itemIds?.ToList() ?? new List<string>(), Color = color, Transparency = transparency }, targetId);
    }

    [McpServerTool]
    [Description("Reset temporary appearance overrides when supported. Requires --allow-writes.")]
    public static Task<object> nwd_reset_appearance_overrides(PipeRpcClient client, WriteGate writeGate, AuditLogger audit, string? targetId = null)
    {
        return ToolInvocation.Write<NotSupportedResult>(client, writeGate, audit, "nwd_reset_appearance_overrides", null, targetId);
    }

    [McpServerTool]
    [Description("List TimeLiner tasks when Navisworks exposes them.")]
    public static Task<object> nwd_list_timeliner_tasks(PipeRpcClient client, int maxResults = 500, string? targetId = null)
    {
        return ToolInvocation.Read<NotSupportedResult>(client, "nwd_list_timeliner_tasks", new { maxResults }, targetId, NavisMcpDefaults.LongTimeoutMs);
    }

    [McpServerTool]
    [Description("Link model items or a search set to a TimeLiner task when supported. Requires --allow-writes.")]
    public static Task<object> nwd_link_timeliner_task(PipeRpcClient client, WriteGate writeGate, AuditLogger audit, string taskId, string[]? itemIds = null, string? searchSetId = null, string? targetId = null)
    {
        return ToolInvocation.Write<NotSupportedResult>(client, writeGate, audit, "nwd_link_timeliner_task", new TimeLinerLinkParams { TaskId = taskId, ItemIds = itemIds?.ToList() ?? new List<string>(), SearchSetId = searchSetId }, targetId, NavisMcpDefaults.LongTimeoutMs);
    }

    [McpServerTool]
    [Description("Export quantification summary when Navisworks exposes quantification data.")]
    public static Task<object> nwd_export_quantification_summary(PipeRpcClient client, string? targetId = null)
    {
        return ToolInvocation.Read<NotSupportedResult>(client, "nwd_export_quantification_summary", null, targetId, NavisMcpDefaults.LongTimeoutMs);
    }
}
