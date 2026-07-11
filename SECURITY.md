# Security Policy

## Supported versions

Security fixes are accepted for the latest published `0.x` / `1.x` release line.

## Reporting a vulnerability

**Preferred:** open a private [GitHub Security Advisory](https://github.com/meococ/navis-mcp/security/advisories/new) on this repository.

**Email:** `security@navis-mcp.dev` (see [MAINTAINERS.md](MAINTAINERS.md)).

Do not file public issues for token, path-guard, or local IPC bypasses.

## Threat model (summary)

NavisMcp is a **local** bridge: MCP stdio client → .NET server → named pipe → Navisworks plug-in.

- Assumes a same-user Windows desktop. It is not a remote multi-tenant service.
- Write-like tools require `--allow-writes` and are audited under `%LOCALAPPDATA%\NavisMcp\logs`.
- File writes are restricted to guarded roots (`exports/`, `snapshots/`, `reports/`, model-input root). Junctions and symbolic links in those paths are rejected.
- Bridge requests require a per-session auth token stored in a local sidecar file (not returned by `nwd_list_targets`).
- Named pipes are ACL-restricted to the current Windows user.
- There is **no** arbitrary C#, script, or shell execution tool.

See [docs/security.md](docs/security.md) for the full model.
