param(
    [ValidateSet("cursor", "vscode", "claude-desktop")]
    [string]$Client = "cursor",
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot,
    [string]$RepoRoot = "",
    [switch]$AllowWrites,
    [string]$Toolsets = "",
    [string]$OutFile = ""
)

$ErrorActionPreference = "Stop"
if (-not $RepoRoot) {
    $RepoRoot = Split-Path -Parent $PSScriptRoot
}

$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
$RepoRoot = [System.IO.Path]::GetFullPath($RepoRoot)
$serverProject = Join-Path $RepoRoot "src\NavisMcp.Server\NavisMcp.Server.csproj"
if (-not (Test-Path $serverProject)) {
    throw "Server project not found: $serverProject"
}

$argsList = @(
    "run",
    "--project",
    ($serverProject -replace '\\', '\\'),
    "--",
    "--project-root",
    ($ProjectRoot -replace '\\', '\\')
)
if ($AllowWrites) {
    $argsList += "--allow-writes"
}
if ($Toolsets) {
    $argsList += "--toolsets"
    $argsList += $Toolsets
}

$mcp = [ordered]@{
    mcpServers = [ordered]@{
        "navisworks-local" = [ordered]@{
            type = "stdio"
            command = "dotnet"
            args = $argsList
        }
    }
}

$json = $mcp | ConvertTo-Json -Depth 8
if ($OutFile) {
    $dir = Split-Path -Parent $OutFile
    if ($dir) {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }
    Set-Content -LiteralPath $OutFile -Value $json -Encoding UTF8
    Write-Host "Wrote $OutFile"
}
else {
    Write-Output $json
}

Write-Host ""
Write-Host "Client profile: $Client"
Write-Host "Paste into your MCP client config, or pass -OutFile to write a file."
Write-Host "Remember: --project-root should be a BIM job folder, not necessarily the git clone."
