param(
    [string]$Configuration = "Release",
    [string]$Version = "",
    [string]$NavisworksPath = "C:\Program Files\Autodesk\Navisworks Manage 2026",
    [string]$PluginDllPath = "",
    [switch]$AllowMissingPlugin
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
if (-not $Version) {
    $props = Get-Content (Join-Path $root "Directory.Build.props") -Raw
    if ($props -match "<Version>([^<]+)</Version>") {
        $Version = $Matches[1]
    }
    else {
        $Version = "0.1.0"
    }
}

$dist = Join-Path $root "dist"
$stage = Join-Path $dist "stage"
$zipName = "NavisMcp-$Version-win-x64.zip"
$zipPath = Join-Path $dist $zipName

Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $stage | Out-Null
New-Item -ItemType Directory -Force -Path $dist | Out-Null

function Sync-PackageContentsVersion {
    param([string]$XmlPath, [string]$AppVersion)
    if (-not (Test-Path $XmlPath)) {
        throw "PackageContents.xml not found: $XmlPath"
    }
    [xml]$xml = Get-Content -LiteralPath $XmlPath
    $xml.ApplicationPackage.AppVersion = $AppVersion
    $xml.ApplicationPackage.FriendlyVersion = $AppVersion
    $entry = $xml.ApplicationPackage.Components.ComponentEntry
    if ($entry) {
        $entry.Version = $AppVersion
    }
    $xml.Save($XmlPath)
}

Push-Location $root
try {
    dotnet restore (Join-Path $root "NavisMcp.sln")
    $publishDir = Join-Path $stage "server"
    New-Item -ItemType Directory -Force -Path $publishDir | Out-Null
    dotnet publish (Join-Path $root "src\NavisMcp.Server\NavisMcp.Server.csproj") `
        -c $Configuration `
        -o $publishDir `
        --no-restore `
        -p:Version=$Version

    $bundleStage = Join-Path $stage "bundle\NavisMcp.bundle"
    $contents = Join-Path $bundleStage "Contents\v23"
    New-Item -ItemType Directory -Force -Path $contents | Out-Null
    $packageXmlStage = Join-Path $bundleStage "PackageContents.xml"
    Copy-Item (Join-Path $root "packaging\NavisMcp.bundle\PackageContents.xml") $packageXmlStage -Force
    Sync-PackageContentsVersion -XmlPath $packageXmlStage -AppVersion $Version

    $pluginCopied = $false
    if ($PluginDllPath) {
        if (-not (Test-Path $PluginDllPath)) {
            throw "PluginDllPath not found: $PluginDllPath"
        }
        $pluginDir = Split-Path -Parent $PluginDllPath
        Get-ChildItem -LiteralPath $pluginDir -File |
            Where-Object { $_.Extension -in ".dll", ".pdb", ".json", ".config" } |
            Copy-Item -Destination $contents -Force
        $pluginCopied = Test-Path (Join-Path $contents "NavisMcp.Plugin.Navis2026.dll")
    }
    else {
        $apiDll = Join-Path $NavisworksPath "Autodesk.Navisworks.Api.dll"
        if (Test-Path $apiDll) {
            dotnet build (Join-Path $root "src\NavisMcp.Plugin.Navis2026\NavisMcp.Plugin.Navis2026.csproj") `
                -c $Configuration `
                --no-restore `
                -p:Platform=x64 `
                -p:NavisworksInstallDir="$NavisworksPath" `
                -p:Version=$Version
            $pluginDll = Get-ChildItem (Join-Path $root "src\NavisMcp.Plugin.Navis2026\bin") -Recurse -Filter "NavisMcp.Plugin.Navis2026.dll" |
                Sort-Object LastWriteTime -Descending |
                Select-Object -First 1
            if ($pluginDll) {
                Get-ChildItem -LiteralPath $pluginDll.Directory.FullName -File |
                    Where-Object { $_.Extension -in ".dll", ".pdb", ".json", ".config" } |
                    Copy-Item -Destination $contents -Force
                $pluginCopied = $true
            }
        }
    }

    if (-not $pluginCopied) {
        $msg = @"
Release zip requires NavisMcp.Plugin.Navis2026.dll.

Options:
  1. Install Navisworks Manage 2026 and re-run (builds plugin from API refs).
  2. Pass -PluginDllPath to a pre-built plugin DLL from a maintainer machine.
  3. Pass -AllowMissingPlugin only for local debugging (not for GitHub Releases).
"@
        if ($AllowMissingPlugin) {
            Write-Warning $msg
            Set-Content (Join-Path $contents "README.txt") "INCOMPLETE BUNDLE: plugin DLL missing. Do not ship this zip."
        }
        else {
            throw $msg
        }
    }

    Copy-Item (Join-Path $root "LICENSE") $stage -Force
    Copy-Item (Join-Path $root "NOTICE") $stage -Force
    Copy-Item (Join-Path $root "CHANGELOG.md") $stage -Force
    Copy-Item (Join-Path $root "README.md") (Join-Path $stage "README.md") -Force
    Copy-Item (Join-Path $root "scripts") (Join-Path $stage "scripts") -Recurse -Force
    Copy-Item (Join-Path $root "samples") (Join-Path $stage "samples") -Recurse -Force
    Copy-Item (Join-Path $root "packaging\build-release.ps1") (Join-Path $stage "scripts\pack-release.ps1") -Force -ErrorAction SilentlyContinue

    @"
NavisMcp $Version

1. Install .NET 8 runtime/SDK.
2. Install Autodesk Navisworks Manage 2026 (user-owned license).
3. Close Navisworks, then run scripts\install-dev.ps1 from this package (or copy bundle\NavisMcp.bundle to %APPDATA%\Autodesk\ApplicationPlugins\).
4. Restart Navisworks and open a model.
5. Point your MCP client at server\NavisMcp.Server.dll with --project-root set to your BIM project folder (not this zip root unless you intend that).
"@ | Set-Content (Join-Path $stage "README.txt")

    if (Test-Path $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }
    Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $zipPath -Force
    Write-Host "Created $zipPath"
}
finally {
    Pop-Location
}
