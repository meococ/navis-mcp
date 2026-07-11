# Demo assets

Record a short GIF/video for the README using [docs/demo.md](../demo.md) on **Autodesk Samples** (or any public sample model), then save as:

- `docs/assets/demo.gif` (preferred for README)
- or `docs/assets/demo.mp4`

Until a recording is committed, the README links here instead of embedding a missing binary.

## Smoke checklist (Autodesk Samples)

1. `.\scripts\verify-env.ps1` then `.\scripts\install-dev.ps1`
2. Open Navisworks Manage 2026 → Autodesk Samples model
3. MCP config from `samples/mcp.cursor.example.json` with `--project-root` = a copy of `samples/project-root/`
4. `nwd_list_targets` → `nwd_health_check` → `nwd_run_federated_preflight`
5. `nwd_find_items` / `nwd_create_search_set` → `nwd_capture_viewport` (`framing=selection`)
6. Optional: clash create/run/export → `nwd_create_evidence_pack`
