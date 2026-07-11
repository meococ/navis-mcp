# Capability matrix

Honest status of public MCP tools as of v0.1.0.

Legend: **Supported** · **Partial** · **Stub** (`errorCode=not_supported_by_api`) · **Write-like**

Default toolsets (`core`, `search`, `clash`, `report`, `write`) exclude stubs. Opt in with `--toolsets advanced`.

| Area | Tool | Status | Write-like | Notes |
|------|------|--------|------------|-------|
| Meta | `nwd_list_targets` | Supported | No | Token stripped |
| Meta | `nwd_health_check` | Supported | No | |
| Document | `nwd_get_document_info` | Supported | No | |
| Document | `nwd_get_federation_map` | Supported | No | |
| Document | `nwd_run_federated_preflight` | Supported | Yes | Report under `reports/` |
| Search | `nwd_find_items` | Supported | No | Accent-insensitive, paged |
| Search | `nwd_create_search_set` | Supported | Yes | Dynamic SelectionSet+Search |
| Search | `nwd_export_search_sets` | Supported | Yes | XML under `reports/search-sets` |
| Visual | `nwd_capture_viewport` | Supported | Framing ≠ current | Native NW image API |
| Visual | `nwd_set_section_box` | Supported | Yes | ClipPlanes box fit/clear |
| Clash | `nwd_create_clash_test` / `run` / `export` | Supported | Yes | Live Clash Detective |
| Clash | `nwd_cluster_clash_results` | Supported | No | Server-side triage |
| Clash | `nwd_update_clash_result` | Supported | Yes | Status / comment / AssignedTo |
| Evidence | `nwd_create_issue_pack` / `nwd_create_evidence_pack` | Supported | Yes | JSON+MD under `reports/` |
| Evidence | `nwd_export_bcf` | Supported | Yes | Minimal open BCF 2.1 |
| Appearance | `nwd_set_appearance_override` | Stub | Yes | `--toolsets advanced` |
| 4D | `nwd_list_timeliner_tasks` / `link` | Stub | Mixed | `--toolsets advanced` |
| QTO | `nwd_export_quantification_summary` | Stub | No | `--toolsets advanced` |

Framing aliases for snapshots: `current`, `auto`, `selection`, `model`, `top`, `front`, `back`, `right`, `left`, `iso`, `front_right_top`, `route_overview`.

See also [tool-reference.md](tool-reference.md).
