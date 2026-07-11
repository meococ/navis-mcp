param(
    [string]$Owner = "meococ",
    [string]$Repo = "navis-mcp",
    [switch]$Private
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" +
    [System.Environment]::GetEnvironmentVariable("Path", "User")

gh auth status

$visibility = if ($Private) { "private" } else { "public" }
$fullName = "$Owner/$Repo"

$exists = $false
try {
    gh repo view $fullName 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) { $exists = $true }
}
catch {
    $exists = $false
}

if (-not $exists) {
    Write-Host "Creating GitHub repo $fullName ..."
    gh repo create $fullName --$visibility --source . --remote origin --description "Local MCP bridge for Autodesk Navisworks Manage 2026. Clash Detective, evidence packs, path-guarded reports."
}
else {
    $url = "https://github.com/$fullName.git"
    $remote = git remote get-url origin 2>$null
    if (-not $remote) {
        git remote add origin $url
    }
}

git push -u origin HEAD
git push origin v0.1.0 --force
Write-Host "Published: https://github.com/$fullName"
Write-Host "Tag: v0.1.0"
