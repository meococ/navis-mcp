# Architecture

```text
MCP client (Cursor / Claude / VS Code)
        | stdio JSON-RPC
NavisMcp.Server (.NET 8)
        | named pipe + session token + guarded roots
NavisMcp.Plugin.Navis2026 (.NET Framework 4.8 x64)
        | UI-thread dispatcher
Autodesk Navisworks Manage 2026 API
```

## Components

| Project | Role |
|---------|------|
| `NavisMcp.Contracts` | Shared DTOs and RPC contracts (`netstandard2.0`) |
| `NavisMcp.Server` | MCP stdio host, path guards, write gate, audit, pipe client |
| `NavisMcp.Plugin.Navis2026` | In-process plug-in, named-pipe host, UI-thread API calls |

## Session model

On start, the plug-in writes a session descriptor under `%LOCALAPPDATA%\NavisMcp\sessions\` and a sidecar `.token` file. The server discovers targets from session files, loads the token privately, and attaches `RequestContext` to every bridge call.

## Why UI-thread only

Navisworks API objects are not safe from background threads. All API work goes through `UiWorkDispatcher`.

## Versioning

Server, plug-in, and contracts share one product SemVer. `nwd_health_check` reports versions and warns on mismatch.
