# Safety preflight

Run at the start of every Navisworks MCP session.

## Checklist

1. `nwd_list_targets` — confirm local Manage 2026 bridge is reachable.
2. `nwd_health_check` — note `writesEnabled`, `serverVersion`, `pluginVersion`, `versionMismatch`.
3. `nwd_run_federated_preflight` — record units, linked models, and clash tolerance convention.
4. Confirm `--project-root` points at the intended BIM project (exports/snapshots/reports).
5. Confirm whether `--allow-writes` is intentional before any framing/selection/clash mutation.
6. `nwd_list_toolsets` — respect requested toolsets (`core,search,clash,report,write`).
7. Remember: item ids (`mi:*`) are session/document scoped; do not reuse across documents.
8. Never expose session auth tokens in user-facing summaries.

## Hard rules

- All Navisworks API work stays on the UI-thread dispatcher (plugin).
- Camera framing for snapshots is write-like.
- Prefer playbooks over ad-hoc tool thrash.
