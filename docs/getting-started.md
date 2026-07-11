# Getting started

## Prerequisites

- Autodesk Navisworks Manage 2026
- .NET 8 SDK
- Visual Studio 2022 Build Tools (desktop)
- .NET Framework 4.8 targeting pack

## Install from source

```powershell
.\scripts\verify-env.ps1
# Close Navisworks first
.\scripts\install-dev.ps1
```

Restart Navisworks Manage 2026. The event watcher starts the bridge automatically. Check **Add-Ins → Navis MCP Bridge Status**.

## Configure an MCP client

Copy [samples/mcp.cursor.example.json](../samples/mcp.cursor.example.json) and replace:

- `PATH/TO/navis-mcp` — this repository (or a published server DLL)
- `PATH/TO/YOUR/BIM/PROJECT` — the folder that should receive `exports/`, `snapshots/`, `reports/`

`--project-root` is **not** the git clone of NavisMcp unless you intentionally want runtime folders inside the repo.

## First calls

1. `nwd_list_targets` — pick a target with `documentTitle`
2. `nwd_health_check`
3. `nwd_get_document_info`
4. `nwd_capture_viewport` with `framing=auto` (requires `--allow-writes` for non-`current` framing)
5. `nwd_find_items` with a short query and paging

## Uninstall

```powershell
.\scripts\uninstall-dev.ps1
```

## Troubleshooting

See [troubleshooting.md](troubleshooting.md).
