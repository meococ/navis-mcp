# Troubleshooting

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| No targets | Plug-in not loaded / NW not running | Install bundle, restart NW, check Add-Ins status |
| `unauthorized` | Missing/invalid session token | Restart NW bridge; ensure server reads sidecar `.token` |
| `writes_disabled` | Server started without `--allow-writes` | Add flag for write-like tools |
| `*_path_denied` | Path outside guarded root or through junction | Keep outputs under project-root subfolders |
| Install fails with DLL lock | Navisworks running | Close Roamer/Navisworks, retry |
| Version mismatch warning | Server and plug-in from different builds | Reinstall matching release zip / `install-dev` |
| Stub tools | Capability not mapped | Expect `not_supported_by_api`; see tool-reference |

Logs: `%LOCALAPPDATA%\NavisMcp\logs`
