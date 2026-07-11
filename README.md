# NavisMcp

[![CI Server](https://github.com/meococ/navis-mcp/actions/workflows/ci-server.yml/badge.svg)](https://github.com/meococ/navis-mcp/actions/workflows/ci-server.yml)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)

Local MCP bridge for **Autodesk Navisworks Manage 2026**. An AI agent can inspect federated models, search and select items, capture native viewport snapshots, run Clash Detective workflows, and write guarded reports — without arbitrary code execution.

**Positioning:** Official Autodesk MCP today is Revit-read. Cloud “clash” wrappers are not Clash Detective. NavisMcp is the open, safety-first bridge for **live Navisworks coordination**: Clash Detective, selection evidence, and path-guarded reports.

Requires a licensed Navisworks Manage 2026 install (Windows). This project does not redistribute Autodesk DLLs. Not affiliated with Autodesk.

```text
MCP client (Cursor / Claude)
        │ stdio JSON-RPC
        ▼
NavisMcp.Server (.NET 8)
        │ named pipe + session token
        ▼
NavisMcp.Plugin (NW 2026 UI thread)
        ▼
Federated model / Clash Detective / viewport
```

## Quick start

1. Prerequisites: .NET 8 SDK, VS 2022 Build Tools, .NET Framework 4.8 targeting pack, Navisworks Manage 2026.
2. Verify environment:

```powershell
.\scripts\verify-env.ps1
```

3. Close Navisworks, then install the plug-in bundle:

```powershell
.\scripts\install-dev.ps1
```

4. Restart Navisworks and open a model (Autodesk Samples is fine).
5. Generate an MCP client snippet (or copy [samples/mcp.cursor.example.json](samples/mcp.cursor.example.json)):

```powershell
.\scripts\generate-mcp-config.ps1 -Client cursor -ProjectRoot "D:\bim-jobs\my-project" -AllowWrites
```

Set `--project-root` to a **BIM job folder** (see [samples/project-root](samples/project-root/)) for `exports/`, `snapshots/`, and `reports/` — not necessarily this git clone.

Omit `--allow-writes` for read-only mode. Stub tools (appearance / TimeLiner / QTO) stay hidden unless you pass `--toolsets advanced`.

## Safety

- Write-like tools require `--allow-writes` and are audited under `%LOCALAPPDATA%\NavisMcp\logs`.
- Every bridge request carries a per-session token. The token is not exposed by `nwd_list_targets`.
- Exports, snapshots, reports, and model append inputs are path-guarded; junctions/symlinks are rejected.
- Named pipes and token files are ACL-restricted to the current Windows user.
- No shell, C#, or script execution tools.

## Docs

- [Getting started](docs/getting-started.md)
- [Architecture](docs/architecture.md)
- [Security](docs/security.md)
- [Tool reference](docs/tool-reference.md) / [Capability matrix](docs/capability-matrix.md)
- [Smoke testing](docs/smoke-testing.md) / [Demo script](docs/demo.md)
- [Marketplace & distribution](docs/marketplace.md)
- [Commercial packs (optional)](docs/commercial-packs.md)
- [Changelog](CHANGELOG.md)
- [Code of Conduct](CODE_OF_CONDUCT.md) · [Maintainers](MAINTAINERS.md)

## License

Apache-2.0. See [LICENSE](LICENSE) and [NOTICE](NOTICE).
