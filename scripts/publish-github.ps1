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
$visibility = if ($Private) { "--private" } else { "--public" }

# Create org/user repo if missing
$exists = $true
gh repo view "$Owner/$Repo" 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) {
    $exists = $false
}

if (-not $exists) {
    Write-Host "Creating GitHub repo $Owner/$Repo ..."
    gh repo create "$Owner/$Repo" $visibility --source . --remote origin --description "Local MCP bridge for Autodesk Navisworks Manage 2026 — Clash Detective, evidence packs, path-guarded reports."
}
else {
    $url = "https://github.com/$Owner/$Repo.git"
    $remote = git remote get-url origin 2>$null
    if (-not $remote) {
        git remote add origin $url
    }
}

git push -u origin HEAD
git push origin v0.1.0
Write-Host "Published: https://github.com/$Owner/$Repo"
Write-Host "Tag: v0.1.0 (create a GitHub Release after plugin zip is built on a NW machine)."
