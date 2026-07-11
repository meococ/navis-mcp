using System.Reflection;
using NavisMcp.Server.Tools;
using Xunit;

namespace NavisMcp.Server.Tests;

public sealed class ToolRegistrationTests
{
    [Theory]
    [InlineData(typeof(NavisMetaTools), new[]
    {
        "nwd_list_targets", "nwd_health_check", "nwd_get_document_info", "nwd_get_federation_map",
        "nwd_list_playbooks", "nwd_get_playbook", "nwd_list_toolsets", "nwd_list_domain_ontology",
        "nwd_run_federated_preflight"
    })]
    [InlineData(typeof(NavisSearchTools), new[]
    {
        "nwd_get_model_tree", "nwd_find_items", "nwd_list_property_schema", "nwd_get_item_properties",
        "nwd_get_current_selection", "nwd_list_selection_sets", "nwd_get_selection_set_items",
        "nwd_list_viewpoints", "nwd_get_current_viewpoint", "nwd_measure_items"
    })]
    [InlineData(typeof(NavisClashTools), new[]
    {
        "nwd_list_clash_tests", "nwd_get_clash_results", "nwd_run_clash_test", "nwd_create_clash_test",
        "nwd_update_clash_result", "nwd_export_clash_report", "nwd_cluster_clash_results"
    })]
    [InlineData(typeof(NavisReportTools), new[]
    {
        "nwd_export_items_table", "nwd_create_issue_summary", "nwd_create_issue_pack", "nwd_create_evidence_pack",
        "nwd_export_bcf", "nwd_run_qa_checks", "nwd_run_phase0_selection_qa", "nwd_visual_qa_batch",
        "nwd_capture_viewport"
    })]
    [InlineData(typeof(NavisWriteTools), new[]
    {
        "nwd_select_items", "nwd_select_by_search", "nwd_clear_selection",
        "nwd_create_selection_set", "nwd_delete_selection_set", "nwd_create_search_set", "nwd_export_search_sets",
        "nwd_goto_viewpoint", "nwd_save_viewpoint", "nwd_update_viewpoint", "nwd_delete_viewpoint",
        "nwd_set_hidden", "nwd_unhide_all", "nwd_set_section_box", "nwd_export_nwd", "nwd_append_model"
    })]
    public void DefaultToolTypes_ExposeExpectedMcpToolNames(Type toolType, string[] expected)
    {
        var actual = GetMcpToolNames(toolType);
        Assert.Equal(expected.OrderBy(name => name), actual);
    }

    [Fact]
    public void NavisAdvancedTools_ExposeExpectedStubToolNames()
    {
        var expected = new[]
        {
            "nwd_set_appearance_override",
            "nwd_reset_appearance_overrides",
            "nwd_list_timeliner_tasks",
            "nwd_link_timeliner_task",
            "nwd_export_quantification_summary"
        };

        var actual = GetMcpToolNames(typeof(NavisAdvancedTools));
        Assert.Equal(expected.OrderBy(name => name), actual);
    }

    private static string[] GetMcpToolNames(Type toolType)
    {
        return toolType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.GetCustomAttributes().Any(attr => attr.GetType().Name == "McpServerToolAttribute"))
            .Select(method => method.Name)
            .OrderBy(name => name)
            .ToArray();
    }
}
