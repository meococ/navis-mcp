# Contributing (extended)

See root [CONTRIBUTING.md](../CONTRIBUTING.md).

## Project layout

- `src/` — Contracts, Server, Plugin
- `tests/` — Server.Tests, FakeBridge
- `docs/` — user documentation
- `samples/` — MCP configs and empty project-root scaffolds
- `packaging/` — Autodesk `.bundle` template and release scripts
- `scripts/` — verify / install / uninstall / pack

## Coding notes

- Keep stdout pure MCP JSON-RPC on the server.
- All Navisworks API calls on the UI-thread dispatcher.
- Prefer throwing `BridgeRpcException("not_supported_by_api", ...)` for unfinished mappings.
