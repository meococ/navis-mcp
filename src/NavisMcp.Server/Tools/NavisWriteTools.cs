using System.ComponentModel;
using ModelContextProtocol.Server;
using NavisMcp.Contracts;
using NavisMcp.Server.Infrastructure;

namespace NavisMcp.Server.Tools;

[McpServerToolType]
[Description("Write tools for selection, viewpoints, visibility, export, and append model.")]
public static class NavisWriteTools
{
    [McpServerTool]
    [Description("Create a static selection set from item ids or the current selection. Requires --allow-writes.")]
    public static Task<object> nwd_create_selection_set(
        PipeRpcClient client,
        WriteGate writeGate,
        AuditLogger audit,
        [Description("Display name for the new selection set.")] string displayName,
        [Description("Item ids to include. Leave empty with useCurrentSelection=true.")] string[]? itemIds = null,
        [Description("Use the current Navisworks selection as the source.")] bool useCurrentSelection = false,
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        var parameters = new SelectionSetMutationParams { DisplayName = displayName, ItemIds = itemIds?.ToList() ?? new List<string>(), UseCurrentSelection = useCurrentSelection };
        return ToolInvocation.Write<SavedItemInfo>(client, writeGate, audit, "nwd_create_selection_set", parameters, targetId);
    }

    [McpServerTool]
    [Description("Delete a saved selection set. Requires --allow-writes.")]
    public static Task<object> nwd_delete_selection_set(
        PipeRpcClient client,
        WriteGate writeGate,
        AuditLogger audit,
        [Description("Saved selection set id from nwd_list_selection_sets.")] string selectionSetId,
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        return ToolInvocation.Write<WriteResult>(client, writeGate, audit, "nwd_delete_selection_set", new SavedItemMutationParams { Id = selectionSetId }, targetId);
    }

    [McpServerTool]
    [Description("Create a dynamic Navisworks search set (SelectionSet with Search). Default condition: Item/Name contains displayName. Pass conditionsJson for custom category/property/operator/value rules. Requires --allow-writes.")]
    public static Task<object> nwd_create_search_set(
        PipeRpcClient client,
        WriteGate writeGate,
        AuditLogger audit,
        [Description("Search set display name.")] string displayName,
        [Description("Optional JSON array of conditions: [{category,property,operator,value,negate}]. Operators: equals, contains, wildcard.")] string conditionsJson = "",
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        var definition = new SearchSetDefinition { DisplayName = displayName };
        if (!string.IsNullOrWhiteSpace(conditionsJson))
        {
            try
            {
                var conditions = System.Text.Json.JsonSerializer.Deserialize<List<SearchConditionDefinition>>(conditionsJson, JsonDefaults.Options);
                if (conditions != null)
                {
                    definition.Conditions = conditions;
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                return Task.FromResult<object>(ToolInvocation.ToolEnvelope.Fail("invalid_params", "conditionsJson is invalid JSON: " + ex.Message));
            }
        }

        return ToolInvocation.Write<SavedItemInfo>(client, writeGate, audit, "nwd_create_search_set", definition, targetId);
    }

    [McpServerTool]
    [Description("Export dynamic search sets as XML under reports/search-sets. Requires --allow-writes.")]
    public static Task<object> nwd_export_search_sets(
        PipeRpcClient client,
        WriteGate writeGate,
        AuditLogger audit,
        ServerOptions options,
        [Description("File name under reports/search-sets.")] string fileName = "",
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        return ToolInvocation.Run(async () =>
        {
            writeGate.EnsureAllowed();
            var path = ReportPathGuard.ResolveReportPath(options, string.IsNullOrWhiteSpace(fileName) ? Path.Combine("search-sets", "search-sets.xml") : Path.Combine("search-sets", fileName), "xml", "search-sets");
            var parameters = new SearchSetImportExportParams { FilePath = path.FilePath, RelativePath = path.RelativePath };
            await audit.WriteAsync("nwd_export_search_sets", new { targetId, parameters }, CancellationToken.None).ConfigureAwait(false);
            var data = await client.InvokeAsync<ReportResult>("nwd_export_search_sets", parameters, targetId, NavisMcpDefaults.LongTimeoutMs).ConfigureAwait(false);
            return ToolInvocation.ToolEnvelope.Success(data);
        });
    }

    [McpServerTool]
    [Description("Update saved viewpoint metadata when supported. Requires --allow-writes.")]
    public static Task<object> nwd_update_viewpoint(
        PipeRpcClient client,
        WriteGate writeGate,
        AuditLogger audit,
        string viewpointId,
        string displayName,
        string? targetId = null)
    {
        return ToolInvocation.Write<WriteResult>(client, writeGate, audit, "nwd_update_viewpoint", new SavedItemMutationParams { Id = viewpointId, DisplayName = displayName }, targetId);
    }

    [McpServerTool]
    [Description("Delete a saved viewpoint when supported. Requires --allow-writes.")]
    public static Task<object> nwd_delete_viewpoint(
        PipeRpcClient client,
        WriteGate writeGate,
        AuditLogger audit,
        string viewpointId,
        string? targetId = null)
    {
        return ToolInvocation.Write<WriteResult>(client, writeGate, audit, "nwd_delete_viewpoint", new SavedItemMutationParams { Id = viewpointId }, targetId);
    }

    [McpServerTool]
    [Description("Append a model from the guarded model input folder. Requires --allow-writes.")]
    public static Task<object> nwd_append_model(PipeRpcClient client, WriteGate writeGate, AuditLogger audit, ServerOptions options, string fileName, string? targetId = null)
    {
        return ToolInvocation.Run(async () =>
        {
            writeGate.EnsureAllowed();
            var path = ModelInputPathGuard.ResolveModelInputPath(options, fileName);
            var parameters = new AppendModelParams { FilePath = path };
            await audit.WriteAsync("nwd_append_model", new { targetId, parameters }, CancellationToken.None).ConfigureAwait(false);
            var data = await client.InvokeAsync<WriteResult>("nwd_append_model", parameters, targetId, NavisMcpDefaults.LongTimeoutMs).ConfigureAwait(false);
            return ToolInvocation.ToolEnvelope.Success(data);
        });
    }

    [McpServerTool]
    [Description("Replace or extend the current Navisworks selection with item refs. Requires --allow-writes.")]
    public static Task<object> nwd_select_items(
        PipeRpcClient client,
        WriteGate writeGate,
        AuditLogger audit,
        [Description("Item ids returned by read/query tools.")] string[] itemIds,
        [Description("If true, append to the current selection instead of replacing it.")] bool preserveExistingSelection = false,
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        var parameters = new ItemsLookupParams
        {
            ItemIds = itemIds?.ToList() ?? new List<string>(),
            PreserveExistingSelection = preserveExistingSelection
        };
        return ToolInvocation.Write<SelectionResult>(client, writeGate, audit, "nwd_select_items", parameters, targetId);
    }

    [McpServerTool]
    [Description("Search items and select matches in Navisworks. Requires --allow-writes.")]
    public static Task<object> nwd_select_by_search(
        PipeRpcClient client,
        WriteGate writeGate,
        AuditLogger audit,
        [Description("Case-insensitive text to match against item display names.")] string query,
        [Description("Maximum items to select.")] int maxResults = 100,
        [Description("Optional root model item id to limit selection search to one model/subtree.")] string? scopeItemId = null,
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        var parameters = new SelectBySearchParams { Query = query, MaxResults = maxResults, ScopeItemId = scopeItemId };
        return ToolInvocation.Write<SelectionResult>(client, writeGate, audit, "nwd_select_by_search", parameters, targetId);
    }

    [McpServerTool]
    [Description("Clear the current Navisworks selection. Requires --allow-writes.")]
    public static Task<object> nwd_clear_selection(
        PipeRpcClient client,
        WriteGate writeGate,
        AuditLogger audit,
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        return ToolInvocation.Write<WriteResult>(client, writeGate, audit, "nwd_clear_selection", null, targetId);
    }

    [McpServerTool]
    [Description("Set the current Navisworks viewpoint from a saved viewpoint. Requires --allow-writes.")]
    public static Task<object> nwd_goto_viewpoint(
        PipeRpcClient client,
        WriteGate writeGate,
        AuditLogger audit,
        [Description("Saved viewpoint id from nwd_list_viewpoints.")] string viewpointId,
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        var parameters = new ViewpointLookupParams { ViewpointId = viewpointId };
        return ToolInvocation.Write<WriteResult>(client, writeGate, audit, "nwd_goto_viewpoint", parameters, targetId);
    }

    [McpServerTool]
    [Description("Save the current camera as a new saved viewpoint. Requires --allow-writes.")]
    public static Task<object> nwd_save_viewpoint(
        PipeRpcClient client,
        WriteGate writeGate,
        AuditLogger audit,
        [Description("Display name for the new saved viewpoint.")] string displayName,
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        var parameters = new SaveViewpointParams { DisplayName = displayName };
        return ToolInvocation.Write<ViewpointInfo>(client, writeGate, audit, "nwd_save_viewpoint", parameters, targetId);
    }

    [McpServerTool]
    [Description("Hide or unhide specific model items. Requires --allow-writes.")]
    public static Task<object> nwd_set_hidden(
        PipeRpcClient client,
        WriteGate writeGate,
        AuditLogger audit,
        [Description("Item ids returned by read/query tools.")] string[] itemIds,
        [Description("True to hide, false to unhide.")] bool hidden = true,
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        var parameters = new SetHiddenParams
        {
            ItemIds = itemIds?.ToList() ?? new List<string>(),
            Hidden = hidden
        };
        return ToolInvocation.Write<WriteResult>(client, writeGate, audit, "nwd_set_hidden", parameters, targetId);
    }

    [McpServerTool]
    [Description("Clear all temporary hidden state in the active document. Requires --allow-writes.")]
    public static Task<object> nwd_unhide_all(
        PipeRpcClient client,
        WriteGate writeGate,
        AuditLogger audit,
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        return ToolInvocation.Write<WriteResult>(client, writeGate, audit, "nwd_unhide_all", null, targetId);
    }

    [McpServerTool]
    [Description("Fit or clear a section box on the current viewpoint (ClipPlanes box mode). Requires --allow-writes.")]
    public static Task<object> nwd_set_section_box(
        PipeRpcClient client,
        WriteGate writeGate,
        AuditLogger audit,
        [Description("clear to disable; fit/box/selection/items to fit a box around itemIds or the current selection.")] string mode = "clear",
        string[]? itemIds = null,
        string? targetId = null)
    {
        return ToolInvocation.Write<WriteResult>(client, writeGate, audit, "nwd_set_section_box", new SectionBoxParams { Mode = mode, ItemIds = itemIds?.ToList() ?? new List<string>() }, targetId);
    }

    [McpServerTool]
    [Description("Export an NWD file under the project exports folder. Requires --allow-writes.")]
    public static Task<object> nwd_export_nwd(
        PipeRpcClient client,
        WriteGate writeGate,
        AuditLogger audit,
        ServerOptions options,
        [Description("File name or path under the project exports folder. .nwd is appended when omitted.")] string fileName = "",
        [Description("Exclude hidden model items from the exported NWD.")] bool excludeHiddenItems = true,
        [Description("Embed externally referenced files where Navisworks supports it.")] bool embedXrefs = true,
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        return ToolInvocation.Run(async () =>
        {
            writeGate.EnsureAllowed();
            var path = ExportPathGuard.ResolveExportPath(options, fileName);
            var parameters = new ExportNwdParams
            {
                FilePath = path,
                ExcludeHiddenItems = excludeHiddenItems,
                EmbedXrefs = embedXrefs
            };

            await audit.WriteAsync("nwd_export_nwd", new { targetId, parameters }, CancellationToken.None).ConfigureAwait(false);
            var data = await client.InvokeAsync<ExportResult>("nwd_export_nwd", parameters, targetId, NavisMcpDefaults.LongTimeoutMs).ConfigureAwait(false);
            return ToolInvocation.ToolEnvelope.Success(data);
        });
    }
}
