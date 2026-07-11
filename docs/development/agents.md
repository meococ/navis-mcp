# Agent Guide: NavisMcp

## Mission

Operate and improve the local MCP bridge for Autodesk Navisworks Manage 2026 so an AI agent can inspect, analyze, snapshot, report on, and safely control local Navisworks models.

Primary outcome: make the agent useful inside the user's BIM/Navisworks workflow — model understanding, search, selection, viewport snapshots, issue finding, reporting, QA checks, and coordination/clash workflows.

## Agent operating model

Agents in this repo act as **team lead**: coordinate sub-agents, implement and verify here, and do **not** hand off work as external coding prompts.

## System shape

- **Product root:** this repository (source, docs, samples). Do **not** use the clone as `--project-root` for real BIM jobs.
- **BIM job root:** a separate folder (see `samples/project-root/`) passed as `--project-root` for `exports/`, `snapshots/`, `reports/`, optional `models/`.
- Target: Autodesk Navisworks Manage 2026 only (`Nw23`).
- Navisworks API path (build-time): `C:\Program Files\Autodesk\Navisworks Manage 2026`
- Architecture: MCP stdio → `NavisMcp.Server` → named pipe → plug-in → Navisworks UI thread
- Logs/audit/session: `%LOCALAPPDATA%\NavisMcp`
- Local agent ops (gitignored): `SKILL/`, `RULE/`, `.cursor/`

## Core commands

```powershell
scripts\verify-env.cmd
scripts\install-dev.cmd
scripts\uninstall-dev.cmd
dotnet build NavisMcp.sln -c Debug
dotnet test tests\NavisMcp.Server.Tests\NavisMcp.Server.Tests.csproj -c Debug
```

Run server (point `--project-root` at a BIM job folder, not necessarily this repo):

```powershell
dotnet run --project src\NavisMcp.Server\NavisMcp.Server.csproj -- --project-root "PATH\TO\YOUR\BIM\PROJECT" --allow-writes
```

Close Navisworks before `install-dev`.

## Model analysis workflow

1. `nwd_list_targets` — prefer a target with `documentTitle`.
2. `nwd_health_check` and `nwd_get_document_info`.
3. `nwd_capture_viewport(width=1600,height=900,framing="auto")`.
4. `nwd_get_model_tree(maxDepth=0,maxNodes=200)`.
5. `nwd_list_property_schema` when building property-aware queries.
6. Scoped/paged `nwd_find_items` (accent-insensitive). Use domain ontology packs / prompts for discipline terms.
7. For each group: `nwd_select_by_search` → `nwd_capture_viewport(framing="selection", restoreView=true)` → sample properties.
8. Write tables/QA/issue packs under `reports/`.
9. Clash: selection sets → `nwd_create_clash_test` → `nwd_run_clash_test` → `nwd_export_clash_report` (default project tolerance `0.100 m` unless specified). Use Clearance when the task is proximity, not hard intersection.
10. Summarize what is visually visible, property-supported, and uncertain.

Prefer MCP prompts (`coord.phase0_selection_qa`, `coord.official_clash_batch`, `coord.meeting_pack`, `safety.preflight`) over ad-hoc tool thrash.

## Safety rules

- All Navisworks API work on the UI-thread dispatcher.
- Item ids (`mi:*`) are session/document scoped.
- Camera framing for snapshots is write-like.
- Token + guarded roots on every bridge request; never expose the token in public target/health fields.
- No arbitrary code/script/shell tools.
- Keep export/snapshot/report/model-input path guards strict; reject junctions/symlinks.

## Local project notes

Job-specific search terms, clash SOPs, and model observations belong in gitignored `RULE/` / `SKILL/` (or the BIM job folder), not in this product guide. Re-query each live document; do not treat prior session notes as universal product truth.

## Definition of done (capability changes)

- DTOs in `NavisMcp.Contracts`
- Server tool registered
- Plug-in dispatcher maps the method
- UI-thread only
- Path/write/audit policy explicit
- Unit tests for registration/serialization/guards/auth
- Manual smoke notes (whether NW restart is required)
