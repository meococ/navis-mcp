using System.ComponentModel;
using ModelContextProtocol.Server;
using NavisMcp.Contracts;
using NavisMcp.Server.Infrastructure;

namespace NavisMcp.Server.Tools;

[McpServerToolType]
[Description("Search and read tools for model tree, find, properties, selection, viewpoints, and measure.")]
public static class NavisSearchTools
{
    [McpServerTool]
    [Description("Return a bounded tree of model items. Increase maxDepth/maxNodes carefully for large NWD/NWF files.")]
    public static Task<object> nwd_get_model_tree(
        PipeRpcClient client,
        [Description("Maximum hierarchy depth to return, starting at model roots.")] int maxDepth = 3,
        [Description("Maximum number of nodes to return across all roots.")] int maxNodes = 500,
        [Description("Optional paging cursor returned by a previous model tree call.")] string? cursor = null,
        [Description("Optional root model item id to return a bounded tree under one model/subtree.")] string? scopeItemId = null,
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        return ToolInvocation.Read<ModelTreeResult>(client, "nwd_get_model_tree", new ModelTreeParams { MaxDepth = maxDepth, MaxNodes = maxNodes, Cursor = cursor, ScopeItemId = scopeItemId }, targetId);
    }

    [McpServerTool]
    [Description("Find model items by display name and optionally property text. Returns session/document-scoped item refs.")]
    public static Task<object> nwd_find_items(
        PipeRpcClient client,
        ServerOptions options,
        [Description("Case-insensitive text to match against item display names and, optionally, properties.")] string query,
        [Description("Maximum matching items to return.")] int maxResults = 50,
        [Description("Also scan item property category/property/value text. Slower on large files.")] bool includeProperties = false,
        [Description("contains or equals.")] string matchMode = "contains",
        [Description("Normalize Vietnamese accents before matching.")] bool accentInsensitive = true,
        [Description("Optional result cursor from a previous call.")] string? cursor = null,
        [Description("Optional root model item id to limit search to one model/subtree, for example mi:15.")] string? scopeItemId = null,
        [Description("Optional source model name contains filter.")] string? sourceModelContains = null,
        [Description("Optional item class contains filter.")] string? classContains = null,
        [Description("Optional item class/category contains filter.")] string? categoryContains = null,
        [Description("Optional property category contains filter.")] string? propertyCategoryContains = null,
        [Description("Optional property name contains filter.")] string? propertyNameContains = null,
        [Description("Optional property value contains filter.")] string? propertyValueContains = null,
        [Description("Optional material property contains filter.")] string? materialContains = null,
        [Description("Optional tag property contains filter.")] string? tagContains = null,
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        return ToolInvocation.Read<ItemSearchResult>(client, "nwd_find_items", new FindItemsParams
        {
            Query = query,
            MaxResults = maxResults,
            IncludeProperties = includeProperties,
            MatchMode = matchMode,
            AccentInsensitive = accentInsensitive,
            Cursor = cursor,
            ScopeItemId = scopeItemId,
            Filter = ToolInvocation.BuildSearchFilter(sourceModelContains, classContains, categoryContains, propertyCategoryContains, propertyNameContains, propertyValueContains, materialContains, tagContains)
        }, targetId, NavisMcpDefaults.DefaultTimeoutMs, result => BriefResponseHelper.MaybeTruncate(result, options.Brief));
    }

    [McpServerTool]
    [Description("Sample available property categories/properties so an agent can build precise searches.")]
    public static Task<object> nwd_list_property_schema(
        PipeRpcClient client,
        [Description("Maximum model items to sample.")] int maxItems = 2000,
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        return ToolInvocation.Read<PropertySchemaResult>(client, "nwd_list_property_schema", new PropertySchemaParams { MaxItems = maxItems }, targetId, NavisMcpDefaults.LongTimeoutMs);
    }

    [McpServerTool]
    [Description("Get property categories and properties for one model item.")]
    public static Task<object> nwd_get_item_properties(
        PipeRpcClient client,
        [Description("Item id returned by another tool. Session/document scoped.")] string? itemId = null,
        [Description("Alternative model index path from itemRef.indexPath.")] int[]? indexPath = null,
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        return ToolInvocation.Read<ItemPropertiesResult>(client, "nwd_get_item_properties", new ItemLookupParams
        {
            ItemId = itemId,
            IndexPath = indexPath?.ToList()
        }, targetId);
    }

    [McpServerTool]
    [Description("Return the current Navisworks selection as item refs.")]
    public static Task<object> nwd_get_current_selection(
        PipeRpcClient client,
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        return ToolInvocation.Read<SelectionResult>(client, "nwd_get_current_selection", null, targetId);
    }

    [McpServerTool]
    [Description("List saved selection sets and folders from the active document.")]
    public static Task<object> nwd_list_selection_sets(
        PipeRpcClient client,
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        return ToolInvocation.Read<SavedItemListResult>(client, "nwd_list_selection_sets", null, targetId);
    }

    [McpServerTool]
    [Description("Resolve a saved selection set to model item refs.")]
    public static Task<object> nwd_get_selection_set_items(
        PipeRpcClient client,
        [Description("Saved selection set id from nwd_list_selection_sets.")] string selectionSetId,
        [Description("Maximum selected model items to return.")] int maxItems = 1000,
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        return ToolInvocation.Read<SelectionResult>(client, "nwd_get_selection_set_items", new SelectionSetLookupParams
        {
            SelectionSetId = selectionSetId,
            MaxItems = maxItems
        }, targetId);
    }

    [McpServerTool]
    [Description("List saved viewpoints and viewpoint folders from the active document.")]
    public static Task<object> nwd_list_viewpoints(
        PipeRpcClient client,
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        return ToolInvocation.Read<ViewpointListResult>(client, "nwd_list_viewpoints", null, targetId);
    }

    [McpServerTool]
    [Description("Return basic camera metadata for the current Navisworks viewpoint.")]
    public static Task<object> nwd_get_current_viewpoint(
        PipeRpcClient client,
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        return ToolInvocation.Read<CurrentViewpointResult>(client, "nwd_get_current_viewpoint", null, targetId);
    }

    [McpServerTool]
    [Description("Measure selected/items bounding extent where supported.")]
    public static Task<object> nwd_measure_items(PipeRpcClient client, string[]? itemIds = null, bool useCurrentSelection = false, string? targetId = null)
    {
        return ToolInvocation.Read<MeasureResult>(client, "nwd_measure_items", new MeasureParams { ItemIds = itemIds?.ToList() ?? new List<string>(), UseCurrentSelection = useCurrentSelection }, targetId);
    }
}
