using System;
using System.Text.Json;
using NavisMcp.Contracts;
using NavisMcp.Plugin.Navis2026.Bridge;

namespace NavisMcp.Plugin.Navis2026.Services
{
    internal sealed class NavisCommandDispatcher
    {
        private readonly Func<SessionDescriptor> _descriptorProvider;
        private readonly NavisDocumentService _documentService = new NavisDocumentService();
        private readonly NavisClashService _clashService = new NavisClashService();
        private readonly NavisViewportSnapshotService _snapshotService = new NavisViewportSnapshotService();

        public NavisCommandDispatcher(Func<SessionDescriptor> descriptorProvider)
        {
            _descriptorProvider = descriptorProvider;
        }

        public object Dispatch(string method, JsonElement parameters, RequestContext context)
        {
            var descriptor = _descriptorProvider();
            switch (method)
            {
                case "nwd_health_check":
                    BridgeRequestPolicy.EnsureTrusted(context, descriptor);
                    return _documentService.Health(descriptor);
                case "nwd_get_document_info":
                    BridgeRequestPolicy.EnsureTrusted(context, descriptor);
                    return _documentService.GetDocumentInfo();
                case "nwd_get_federation_map":
                    BridgeRequestPolicy.EnsureTrusted(context, descriptor);
                    return _documentService.GetFederationMap();
                case "nwd_get_model_tree":
                    BridgeRequestPolicy.EnsureTrusted(context, descriptor);
                    return _documentService.GetModelTree(ReadParams<ModelTreeParams>(parameters));
                case "nwd_find_items":
                    BridgeRequestPolicy.EnsureTrusted(context, descriptor);
                    return _documentService.FindItems(ReadParams<FindItemsParams>(parameters));
                case "nwd_list_property_schema":
                    BridgeRequestPolicy.EnsureTrusted(context, descriptor);
                    return _documentService.ListPropertySchema(ReadParams<PropertySchemaParams>(parameters));
                case "nwd_get_item_properties":
                    BridgeRequestPolicy.EnsureTrusted(context, descriptor);
                    return _documentService.GetItemProperties(ReadParams<ItemLookupParams>(parameters));
                case "nwd_get_current_selection":
                    BridgeRequestPolicy.EnsureTrusted(context, descriptor);
                    return _documentService.GetCurrentSelection();
                case "nwd_list_selection_sets":
                    BridgeRequestPolicy.EnsureTrusted(context, descriptor);
                    return _documentService.ListSelectionSets();
                case "nwd_get_selection_set_items":
                    BridgeRequestPolicy.EnsureTrusted(context, descriptor);
                    return _documentService.GetSelectionSetItems(ReadParams<SelectionSetLookupParams>(parameters));
                case "nwd_list_viewpoints":
                    BridgeRequestPolicy.EnsureTrusted(context, descriptor);
                    return _documentService.ListViewpoints();
                case "nwd_get_current_viewpoint":
                    BridgeRequestPolicy.EnsureTrusted(context, descriptor);
                    return _documentService.GetCurrentViewpoint();
                case "nwd_capture_viewport":
                    var snapshotParams = ReadParams<ViewportSnapshotParams>(parameters);
                    BridgeRequestPolicy.EnsureTrusted(context, descriptor);
                    BridgeRequestPolicy.EnsurePathUnderRoot(snapshotParams.FilePath, context.SnapshotsRoot, "snapshot_path_denied", "Snapshot");
                    if (!string.Equals((snapshotParams.Framing ?? string.Empty).Trim(), "current", StringComparison.OrdinalIgnoreCase))
                    {
                        BridgeRequestPolicy.EnsureWrites(context, descriptor);
                    }
                    return _snapshotService.Capture(_documentService.GetRequiredDocument(), snapshotParams);
                case "nwd_select_items":
                    BridgeRequestPolicy.EnsureWrites(context, descriptor);
                    return _documentService.SelectItems(ReadParams<ItemsLookupParams>(parameters));
                case "nwd_select_by_search":
                    BridgeRequestPolicy.EnsureWrites(context, descriptor);
                    return _documentService.SelectBySearch(ReadParams<SelectBySearchParams>(parameters));
                case "nwd_clear_selection":
                    BridgeRequestPolicy.EnsureWrites(context, descriptor);
                    return _documentService.ClearSelection();
                case "nwd_goto_viewpoint":
                    BridgeRequestPolicy.EnsureWrites(context, descriptor);
                    return _documentService.GoToViewpoint(ReadParams<ViewpointLookupParams>(parameters));
                case "nwd_save_viewpoint":
                    BridgeRequestPolicy.EnsureWrites(context, descriptor);
                    return _documentService.SaveViewpoint(ReadParams<SaveViewpointParams>(parameters));
                case "nwd_update_viewpoint":
                    BridgeRequestPolicy.EnsureWrites(context, descriptor);
                    return _documentService.UpdateViewpoint(ReadParams<SavedItemMutationParams>(parameters));
                case "nwd_delete_viewpoint":
                    BridgeRequestPolicy.EnsureWrites(context, descriptor);
                    return _documentService.DeleteViewpoint(ReadParams<SavedItemMutationParams>(parameters));
                case "nwd_create_selection_set":
                    BridgeRequestPolicy.EnsureWrites(context, descriptor);
                    return _documentService.CreateSelectionSet(ReadParams<SelectionSetMutationParams>(parameters));
                case "nwd_delete_selection_set":
                    BridgeRequestPolicy.EnsureWrites(context, descriptor);
                    return _documentService.DeleteSelectionSet(ReadParams<SavedItemMutationParams>(parameters));
                case "nwd_create_search_set":
                    BridgeRequestPolicy.EnsureWrites(context, descriptor);
                    return _documentService.CreateSearchSet(ReadParams<SearchSetDefinition>(parameters));
                case "nwd_export_search_sets":
                    BridgeRequestPolicy.EnsureWrites(context, descriptor);
                    var searchSetExport = ReadParams<SearchSetImportExportParams>(parameters);
                    BridgeRequestPolicy.EnsurePathUnderRoot(searchSetExport.FilePath, context.ReportsRoot, "report_path_denied", "Report");
                    return _documentService.ExportSearchSets(searchSetExport);
                case "nwd_set_hidden":
                    BridgeRequestPolicy.EnsureWrites(context, descriptor);
                    return _documentService.SetHidden(ReadParams<SetHiddenParams>(parameters));
                case "nwd_unhide_all":
                    BridgeRequestPolicy.EnsureWrites(context, descriptor);
                    return _documentService.UnhideAll();
                case "nwd_set_appearance_override":
                case "nwd_reset_appearance_overrides":
                    BridgeRequestPolicy.EnsureWrites(context, descriptor);
                    return Unsupported(method, "Appearance overrides require additional Navisworks API mapping.");
                case "nwd_set_section_box":
                    BridgeRequestPolicy.EnsureWrites(context, descriptor);
                    return _documentService.SetSectionBox(ReadParams<SectionBoxParams>(parameters));
                case "nwd_export_nwd":
                    BridgeRequestPolicy.EnsureWrites(context, descriptor);
                    var exportParams = ReadParams<ExportNwdParams>(parameters);
                    BridgeRequestPolicy.EnsurePathUnderRoot(exportParams.FilePath, context.ExportsRoot, "export_path_denied", "Export");
                    return _documentService.ExportNwd(exportParams);
                case "nwd_append_model":
                    BridgeRequestPolicy.EnsureWrites(context, descriptor);
                    var appendParams = ReadParams<AppendModelParams>(parameters);
                    BridgeRequestPolicy.EnsurePathUnderRoot(appendParams.FilePath, context.ModelInputRoot, "model_path_denied", "Model input");
                    return _documentService.AppendModel(appendParams);
                case "nwd_export_items_table":
                    BridgeRequestPolicy.EnsureWrites(context, descriptor);
                    var itemsExport = ReadParams<ItemsTableExportParams>(parameters);
                    BridgeRequestPolicy.EnsurePathUnderRoot(itemsExport.FilePath, context.ReportsRoot, "report_path_denied", "Report");
                    return _documentService.ExportItemsTable(itemsExport);
                case "nwd_create_issue_summary":
                    BridgeRequestPolicy.EnsureWrites(context, descriptor);
                    var issueSummary = ReadParams<IssueSummaryParams>(parameters);
                    BridgeRequestPolicy.EnsurePathUnderRoot(issueSummary.FilePath, context.ReportsRoot, "report_path_denied", "Report");
                    return _documentService.CreateIssueSummary(issueSummary);
                case "nwd_run_qa_checks":
                    var qaParams = ReadParams<QaCheckParams>(parameters);
                    BridgeRequestPolicy.EnsureTrusted(context, descriptor);
                    if (!string.IsNullOrWhiteSpace(qaParams.FilePath))
                    {
                        BridgeRequestPolicy.EnsureWrites(context, descriptor);
                        BridgeRequestPolicy.EnsurePathUnderRoot(qaParams.FilePath, context.ReportsRoot, "report_path_denied", "Report");
                    }
                    return _documentService.RunQaChecks(qaParams);
                case "nwd_list_clash_tests":
                    BridgeRequestPolicy.EnsureTrusted(context, descriptor);
                    return _clashService.ListTests(_documentService.GetRequiredDocument());
                case "nwd_get_clash_results":
                    BridgeRequestPolicy.EnsureTrusted(context, descriptor);
                    return _clashService.GetResults(_documentService.GetRequiredDocument(), ReadParams<ClashTestLookupParams>(parameters));
                case "nwd_run_clash_test":
                    BridgeRequestPolicy.EnsureWrites(context, descriptor);
                    return _clashService.RunTest(_documentService.GetRequiredDocument(), ReadParams<ClashTestLookupParams>(parameters));
                case "nwd_create_clash_test":
                    BridgeRequestPolicy.EnsureWrites(context, descriptor);
                    return _clashService.CreateTest(_documentService.GetRequiredDocument(), ReadParams<ClashTestDefinition>(parameters));
                case "nwd_update_clash_result":
                    BridgeRequestPolicy.EnsureWrites(context, descriptor);
                    return _clashService.UpdateResult(_documentService.GetRequiredDocument(), ReadParams<ClashResultUpdate>(parameters));
                case "nwd_link_timeliner_task":
                    BridgeRequestPolicy.EnsureWrites(context, descriptor);
                    return Unsupported(method, "This Navisworks write workflow is not exposed by the current .NET API implementation.");
                case "nwd_export_clash_report":
                    BridgeRequestPolicy.EnsureWrites(context, descriptor);
                    var clashReport = ReadParams<ClashReportExportParams>(parameters);
                    BridgeRequestPolicy.EnsurePathUnderRoot(clashReport.FilePath, context.ReportsRoot, "report_path_denied", "Report");
                    return _clashService.ExportReport(_documentService.GetRequiredDocument(), clashReport);
                case "nwd_list_timeliner_tasks":
                    BridgeRequestPolicy.EnsureTrusted(context, descriptor);
                    return Unsupported(method, "TimeLiner task enumeration is not exposed by this local bridge yet.");
                case "nwd_measure_items":
                    BridgeRequestPolicy.EnsureTrusted(context, descriptor);
                    return _documentService.MeasureItems(ReadParams<MeasureParams>(parameters));
                case "nwd_export_quantification_summary":
                    BridgeRequestPolicy.EnsureTrusted(context, descriptor);
                    return Unsupported(method, "Quantification summary export is not exposed by this local bridge yet.");
                default:
                    throw new BridgeRpcException("method_not_found", "Unknown Navis MCP bridge method '" + method + "'.");
            }
        }

        private static object Unsupported(string capability, string message)
        {
            throw new BridgeRpcException(
                "not_supported_by_api",
                capability + ": " + message,
                new NotSupportedResult
                {
                    Supported = false,
                    ErrorCode = "not_supported_by_api",
                    Capability = capability,
                    Message = message
                });
        }

        private static T ReadParams<T>(JsonElement parameters) where T : new()
        {
            if (parameters.ValueKind == JsonValueKind.Undefined || parameters.ValueKind == JsonValueKind.Null)
            {
                return new T();
            }

            var value = parameters.Deserialize<T>(JsonDefaults.Options);
            return value == null ? new T() : value;
        }
    }
}
