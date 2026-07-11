# Smoke testing (Navisworks required)

CI does **not** run these steps. Maintainers run them on a licensed Navisworks Manage 2026 machine.

## Checklist

1. Close Navisworks.
2. `.\scripts\verify-env.ps1`
3. `.\scripts\install-dev.ps1`
4. Start Navisworks Manage 2026 and open a model (Autodesk Samples is fine).
5. Configure MCP from `samples/mcp.cursor.example.json` (or `.mcp.json.example`) using placeholders — set `--project-root` to a BIM job folder (see `samples/project-root/`), not necessarily this repo.
6. `nwd_list_targets` → pick a target with `documentTitle`.
7. `nwd_health_check`
8. `nwd_get_document_info` / `nwd_run_federated_preflight`
9. `nwd_capture_viewport` with `framing=auto` → file under `snapshots/`
10. `nwd_create_search_set` with a known name fragment → appears in `nwd_list_selection_sets` as `search_set`
11. Optional write path: selection set → clash test → `nwd_export_clash_report` → `nwd_create_evidence_pack`

Restart Navisworks after plug-in DLL changes.
