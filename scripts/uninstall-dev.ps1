$ErrorActionPreference = "Stop"

$bundleTarget = Join-Path $env:APPDATA "Autodesk\ApplicationPlugins\NavisMcp.Local.bundle"

if (Test-Path $bundleTarget) {
    Remove-Item -LiteralPath $bundleTarget -Recurse -Force
    Write-Host "Removed dev bundle:"
    Write-Host "  $bundleTarget"
}
else {
    Write-Host "Dev bundle is not installed:"
    Write-Host "  $bundleTarget"
}
