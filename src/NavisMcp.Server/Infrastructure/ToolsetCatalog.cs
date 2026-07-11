using NavisMcp.Contracts;

namespace NavisMcp.Server.Infrastructure;

public static class ToolsetCatalog
{
    public static readonly string[] DefaultIds = { "core", "search", "clash", "report", "write" };

    public static readonly string[] AllIds = { "core", "search", "clash", "report", "write", "advanced" };

    public static bool IncludesAdvanced(IReadOnlyList<string> toolsets)
    {
        return toolsets.Any(id => string.Equals(id, "advanced", StringComparison.OrdinalIgnoreCase));
    }

    public static ToolsetListResult List(ServerOptions options)
    {
        return new ToolsetListResult
        {
            Requested = options.Toolsets.Count == 0 ? DefaultIds.ToList() : options.Toolsets.ToList(),
            Brief = options.Brief,
            Toolsets = new List<ToolsetInfo>
            {
                new()
                {
                    Id = "core",
                    Description = "Targets, health, document info, federation map, playbooks, toolsets.",
                    Tools = new List<string>
                    {
                        "nwd_list_targets", "nwd_health_check", "nwd_get_document_info", "nwd_get_federation_map",
                        "nwd_list_playbooks", "nwd_get_playbook", "nwd_list_toolsets", "nwd_list_domain_ontology",
                        "nwd_run_federated_preflight"
                    }
                },
                new()
                {
                    Id = "search",
                    Description = "Model tree, find, properties, selection reads, ontology-assisted search.",
                    Tools = new List<string>
                    {
                        "nwd_get_model_tree", "nwd_find_items", "nwd_list_property_schema", "nwd_get_item_properties",
                        "nwd_get_current_selection", "nwd_list_selection_sets", "nwd_get_selection_set_items",
                        "nwd_list_viewpoints", "nwd_get_current_viewpoint", "nwd_measure_items"
                    }
                },
                new()
                {
                    Id = "clash",
                    Description = "Clash Detective list/run/create/export, status update, and clustering.",
                    Tools = new List<string>
                    {
                        "nwd_list_clash_tests", "nwd_get_clash_results", "nwd_run_clash_test", "nwd_create_clash_test",
                        "nwd_update_clash_result", "nwd_export_clash_report", "nwd_cluster_clash_results"
                    }
                },
                new()
                {
                    Id = "report",
                    Description = "Reports, issue packs, BCF, Phase 0 QA, visual QA batch.",
                    Tools = new List<string>
                    {
                        "nwd_export_items_table", "nwd_create_issue_summary", "nwd_create_issue_pack", "nwd_create_evidence_pack", "nwd_export_bcf",
                        "nwd_run_qa_checks", "nwd_run_phase0_selection_qa", "nwd_visual_qa_batch", "nwd_capture_viewport"
                    }
                },
                new()
                {
                    Id = "write",
                    Description = "Selection mutation, viewpoints, visibility, section box, export, append model.",
                    Tools = new List<string>
                    {
                        "nwd_select_items", "nwd_select_by_search", "nwd_clear_selection",
                        "nwd_create_selection_set", "nwd_delete_selection_set", "nwd_create_search_set", "nwd_export_search_sets",
                        "nwd_goto_viewpoint", "nwd_save_viewpoint", "nwd_update_viewpoint", "nwd_delete_viewpoint",
                        "nwd_set_hidden", "nwd_unhide_all", "nwd_set_section_box", "nwd_export_nwd", "nwd_append_model"
                    }
                },
                new()
                {
                    Id = "advanced",
                    Description = "Stub tools for appearance, TimeLiner, and quantification (opt-in).",
                    Tools = new List<string>
                    {
                        "nwd_set_appearance_override", "nwd_reset_appearance_overrides",
                        "nwd_list_timeliner_tasks", "nwd_link_timeliner_task",
                        "nwd_export_quantification_summary"
                    }
                }
            }
        };
    }

    public static IReadOnlyList<string> ParseToolsets(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return Array.Empty<string>();
        }

        return csv.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Trim().ToLowerInvariant())
            .Where(part => part.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
