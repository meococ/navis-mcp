param(
    [string]$NavisworksPath = "C:\Program Files\Autodesk\Navisworks Manage 2026",
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$bundleSource = Join-Path $root "packaging\NavisMcp.bundle"
$installRoot = Join-Path $env:APPDATA "Autodesk\ApplicationPlugins"
$bundleTarget = Join-Path $installRoot "NavisMcp.Local.bundle"
$contentsTarget = Join-Path $bundleTarget "Contents\v23"

& (Join-Path $PSScriptRoot "verify-env.ps1") -NavisworksPath $NavisworksPath

$runningNavisworks = Get-Process -Name "Roamer" -ErrorAction SilentlyContinue
if ($runningNavisworks) {
    throw "Navisworks Manage is currently running and locks the dev bundle DLLs. Close Navisworks, then run scripts\install-dev.cmd again."
}

Push-Location $root
try {
    dotnet restore (Join-Path $root "NavisMcp.sln")
    dotnet build (Join-Path $root "src\NavisMcp.Server\NavisMcp.Server.csproj") -c $Configuration --no-restore
    dotnet build (Join-Path $root "src\NavisMcp.Plugin.Navis2026\NavisMcp.Plugin.Navis2026.csproj") -c $Configuration --no-restore -p:Platform=x64 -p:NavisworksInstallDir="$NavisworksPath"

    $pluginDll = Get-ChildItem (Join-Path $root "src\NavisMcp.Plugin.Navis2026\bin") -Recurse -Filter "NavisMcp.Plugin.Navis2026.dll" |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if (-not $pluginDll) {
        throw "Could not find built NavisMcp.Plugin.Navis2026.dll under src\NavisMcp.Plugin.Navis2026\bin."
    }

    $pluginOutput = $pluginDll.Directory.FullName
    New-Item -ItemType Directory -Force $installRoot | Out-Null
    if (Test-Path $bundleTarget) {
        Remove-Item -LiteralPath $bundleTarget -Recurse -Force
    }

    New-Item -ItemType Directory -Force $contentsTarget | Out-Null
    Copy-Item -LiteralPath (Join-Path $bundleSource "PackageContents.xml") -Destination (Join-Path $bundleTarget "PackageContents.xml") -Force

    Get-ChildItem -LiteralPath $pluginOutput -File |
        Where-Object { $_.Extension -in ".dll", ".pdb", ".json", ".config" } |
        Copy-Item -Destination $contentsTarget -Force

    Write-Host ""
    Write-Host "Installed dev bundle:"
    Write-Host "  $bundleTarget"
    Write-Host ""
    Write-Host "Restart Navisworks Manage 2026, then configure MCP:"
    Write-Host "  .\scripts\generate-mcp-config.ps1 -Client cursor -ProjectRoot `"<BIM-JOB-FOLDER>`" -AllowWrites"
    Write-Host "  or copy samples\mcp.cursor.example.json"
}
finally {
    Pop-Location
}
