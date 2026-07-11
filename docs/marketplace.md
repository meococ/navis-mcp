# Marketplace and distribution

## Today (OSS alpha)

| Channel | Status |
|---------|--------|
| GitHub Releases (`NavisMcp-*-win-x64.zip`) | Primary — must include plugin DLL |
| Cursor / Claude Desktop MCP config | Manual via `samples/` or `scripts/generate-mcp-config.ps1` |
| cursor.directory / community MCP lists | Submit after first public tag |
| Autodesk App Store / Design & Make MCP listing | Planned — requires signed package + safety docs |

## Release requirements

1. Build on a machine with Navisworks Manage 2026 **or** pass `-PluginDllPath` to `packaging/build-release.ps1`.
2. Zip must contain `server/`, `bundle/NavisMcp.bundle/Contents/v23/NavisMcp.Plugin.Navis2026.dll`, `scripts/`, `samples/`.
3. Capability matrix and SECURITY.md must match the shipped surface.

### GitHub Actions (public runners)

Hosted runners do **not** have Navisworks. Seed a reusable plugin cache once from a licensed machine:

```powershell
.\packaging\build-release.ps1
# also produces dist\plugin-navis2026.zip (plugin DLLs only; no Autodesk DLLs)
gh release create plugin-cache dist\plugin-navis2026.zip --title "Plugin cache (Navisworks 2026)" --notes "CI input for tag releases. Rebuild when the plugin changes."
gh release create vX.Y.Z dist\NavisMcp-X.Y.Z-win-x64.zip dist\plugin-navis2026.zip --generate-notes
```

Subsequent `v*` tag pushes download `plugin-navis2026.zip` from `plugin-cache` (or the latest release that has it) and pack a full zip.

## Autodesk Design & Make / Assistant readiness (Phase C)

When submitting as a third-party MCP for Autodesk Assistant:

- Document read vs write modes and `--allow-writes`
- No arbitrary code/shell tools (already true)
- Honest capability matrix (Supported / Partial / Stub)
- Contact: `security@navis-mcp.dev` / GitHub Security Advisories
- Version matrix: Manage 2026 (`Nw23`) only until multi-year CI exists — see [version-matrix.md](version-matrix.md)

## Not in scope for marketplace v1

- Cloud-hosted remote MCP (local named pipe only)
- Redistributing Autodesk DLLs
- ACC Issues two-way sync
