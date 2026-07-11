# Contributing

Thanks for contributing to NavisMcp.

## Prerequisites

- Autodesk Navisworks Manage 2026 (for plugin builds)
- .NET 8 SDK
- Visual Studio 2022 Build Tools with .NET desktop workload
- .NET Framework 4.8 targeting pack

## Build

```powershell
dotnet restore NavisMcp.sln
dotnet build src\NavisMcp.Server\NavisMcp.Server.csproj -c Debug
dotnet test tests\NavisMcp.Server.Tests\NavisMcp.Server.Tests.csproj -c Debug
```

Plugin build (Windows, Navisworks installed):

```powershell
dotnet build src\NavisMcp.Plugin.Navis2026\NavisMcp.Plugin.Navis2026.csproj -c Debug -p:Platform=x64 -p:NavisworksInstallDir="C:\Program Files\Autodesk\Navisworks Manage 2026"
```

Close Navisworks before `scripts\install-dev.ps1` — it locks bundle DLLs.

## Pull requests

- Do not commit BIM files (`*.nwd`, `*.nwf`), clash reports, snapshots, or personal absolute paths.
- Keep Autodesk DLLs out of the tree.
- Add/adjust unit tests for contracts, guards, auth, and tool registration.
- Update `CHANGELOG.md` and `docs/tool-reference.md` when public tools change.
- Prefer honest `not_supported_by_api` over fake success for unfinished API mappings.

See [docs/contributing.md](docs/contributing.md) for more detail.
