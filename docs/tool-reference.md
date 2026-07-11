# Tool reference

Status legend: **Supported** · **Partial** · **Stub** (`not_supported_by_api`) · **Write-like**

## Meta

| Tool | Status | Write-like | Notes |
|------|--------|------------|-------|
| `nwd_list_targets` | Supported | No | Token stripped from public response |
| `nwd_health_check` | Supported | No | Version mismatch warning |

## Document / search

| Tool | Status | Write-like | Notes |
|------|--------|------------|-------|
| `nwd_get_document_info` | Supported | No | |
| `nwd_get_model_tree` | Supported | No | Bound with `maxDepth` / `maxNodes` |
| `nwd_find_items` | Supported | No | Paged, scoped, accent-insensitive |
| `nwd_list_property_schema` | Supported | No | |
| `nwd_get_item_properties` | Supported | No | |
| `nwd_get_federation_map` | Supported | No | Linked model inventory |
| `nwd_list_domain_ontology` | Supported | No | Domain search packs |

## Selection / sets / viewpoints

| Tool | Status | Write-like | Notes |
|------|--------|------------|-------|
| `nwd_get_current_selection` | Supported | No | |
| `nwd_select_items` | Supported | Yes | |
| `nwd_select_by_search` | Supported | Yes | |
| `nwd_clear_selection` | Supported | Yes | |
| `nwd_list_selection_sets` | Supported | No | |
| `nwd_get_selection_set_items` | Supported | No | |
| `nwd_create_selection_set` | Supported | Yes | |
| `nwd_delete_selection_set` | Supported | Yes | |
| `nwd_create_search_set` | Supported | Yes | Dynamic search; optional `conditionsJson` |
| `nwd_export_search_sets` | Supported | Yes | XML under `reports/search-sets` |
| `nwd_list_viewpoints` | Supported | No | |
| `nwd_get_current_viewpoint` | Supported | No | |
| `nwd_goto_viewpoint` | Supported | Yes | |
| `nwd_save_viewpoint` | Supported | Yes | |
| `nwd_update_viewpoint` | Supported | Yes | |
| `nwd_delete_viewpoint` | Supported | Yes | |

## Visibility / export

| Tool | Status | Write-like | Notes |
|------|--------|------------|-------|
| `nwd_set_hidden` | Supported | Yes | |
| `nwd_unhide_all` | Supported | Yes | |
| `nwd_set_appearance_override` | Stub | Yes | `--toolsets advanced` |
| `nwd_reset_appearance_overrides` | Stub | Yes | `--toolsets advanced` |
| `nwd_set_section_box` | Supported | Yes | ClipPlanes box fit/clear |
| `nwd_export_nwd` | Supported | Yes | Under `exports/` |
| `nwd_append_model` | Supported | Yes | Under model-input root |

## Visual

| Tool | Status | Write-like | Notes |
|------|--------|------------|-------|
| `nwd_capture_viewport` | Supported | Framing ≠ `current` | Framing: `current`, `auto`, `model`, `selection`, `top`, `front`, `back`, `right`, `left`, `iso`, `front_right_top`, `route_overview` |
| `nwd_visual_qa_batch` | Supported | Yes | Batch framed snapshots + checklist scaffold |

## Clash / coordination

| Tool | Status | Write-like | Notes |
|------|--------|------------|-------|
| `nwd_list_clash_tests` | Supported | No | |
| `nwd_get_clash_results` | Supported | No | |
| `nwd_run_clash_test` | Supported | Yes | |
| `nwd_create_clash_test` | Supported | Yes | From selection sets or `mi:*` ids |
| `nwd_update_clash_result` | Supported | Yes | Status / comment / AssignedTo |
| `nwd_export_clash_report` | Supported | Yes | |
| `nwd_cluster_clash_results` | Supported | No | Post-process clustering |
| `nwd_create_issue_pack` | Supported | Yes | Local evidence bundle under `reports/` |
| `nwd_create_evidence_pack` | Supported | Yes | Alias of issue pack |
| `nwd_export_bcf` | Supported | Yes | Open BCF zip from issue packs |
| `nwd_run_federated_preflight` | Supported | Yes | Units + model inventory + tolerance record |

## Reports / QA / measure

| Tool | Status | Write-like | Notes |
|------|--------|------------|-------|
| `nwd_export_items_table` | Supported | Yes | |
| `nwd_create_issue_summary` | Supported | Yes | |
| `nwd_run_qa_checks` | Supported | File path ⇒ yes | |
| `nwd_run_phase0_selection_qa` | Supported | Yes | Selection hygiene report |
| `nwd_measure_items` | Supported | No | |
| `nwd_list_timeliner_tasks` | Stub | No | `--toolsets advanced` |
| `nwd_link_timeliner_task` | Stub | Yes | `--toolsets advanced` |
| `nwd_export_quantification_summary` | Stub | No | `--toolsets advanced` |

## MCP prompts / resources

| Name | Purpose |
|------|---------|
| `coord.phase0_selection_qa` | Selection hygiene before official clash |
| `coord.official_clash_batch` | Pair matrix + hard/clearance + export |
| `coord.meeting_pack` | Clustered agenda + evidence |
| `safety.preflight` | Target, writes mode, roots, id scope |

Toolsets (server option / resource): `core`, `search`, `clash`, `report`, `write`.
