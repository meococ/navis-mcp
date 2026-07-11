# Security model

## Trust boundary

NavisMcp runs entirely on the user's Windows machine. The MCP client, server process, and Navisworks process are assumed to share the same interactive user.

## Controls

| Control | Behavior |
|---------|----------|
| Session token | Required on **every** bridge method. Stored in a user-ACL sidecar file; never returned by `nwd_list_targets`. |
| Named pipe ACL | Pipe created with current-user ACL only. |
| Write gate | `--allow-writes` required for mutations and non-`current` camera framing. |
| Path guards | Writes limited to `exports/`, `snapshots/`, `reports/`, and model-input root. |
| Reparse points | Paths through junctions/symlinks are rejected. |
| Root pinning | First guarded roots seen by the plug-in are pinned for the session. |
| Audit | Write-like calls append to `%LOCALAPPDATA%\NavisMcp\logs\audit.jsonl`. |
| Non-goals | No shell, no arbitrary C#, no unrestricted filesystem tools. |

## Prompt injection

Treat model content and clash descriptions as untrusted text. Agents should not follow instructions embedded in BIM properties that ask to disable guards or write outside project roots.

## Reporting

See [SECURITY.md](../SECURITY.md).
