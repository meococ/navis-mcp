param(
    [int]$TimeoutSec = 20
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$fakeProj = Join-Path $root "tests\NavisMcp.FakeBridge\NavisMcp.FakeBridge.csproj"
$outDir = Join-Path $root "artifacts\fake-bridge-smoke"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

dotnet build $fakeProj -c Release -o $outDir | Out-Host
$dll = Join-Path $outDir "NavisMcp.FakeBridge.dll"
$runtimeConfig = Join-Path $outDir "NavisMcp.FakeBridge.runtimeconfig.json"
if (-not (Test-Path $dll)) {
    throw "FakeBridge build output not found: $dll"
}
if (-not (Test-Path $runtimeConfig)) {
    throw "FakeBridge runtimeconfig not found: $runtimeConfig"
}

$errLog = Join-Path $outDir "fake-bridge.err.log"
$outLog = Join-Path $outDir "fake-bridge.out.log"
Remove-Item $errLog, $outLog -ErrorAction SilentlyContinue

$proc = Start-Process -FilePath "dotnet" `
    -ArgumentList @(
        "exec",
        "--runtimeconfig",
        "`"$runtimeConfig`"",
        "`"$dll`""
    ) `
    -WorkingDirectory $outDir `
    -PassThru -WindowStyle Hidden `
    -RedirectStandardError $errLog `
    -RedirectStandardOutput $outLog

try {
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    $sessionRoot = Join-Path $env:LOCALAPPDATA "NavisMcp\sessions"
    $found = $false
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 300
        if (Test-Path $sessionRoot) {
            $sessions = Get-ChildItem $sessionRoot -Filter "fake-*.json" -ErrorAction SilentlyContinue |
                Where-Object { $_.LastWriteTime -gt (Get-Date).AddMinutes(-2) }
            if ($sessions) {
                $found = $true
                Write-Host "FakeBridge session detected: $($sessions[0].Name)"
                break
            }
        }
        if ($proc.HasExited) {
            $stderr = if (Test-Path $errLog) { Get-Content $errLog -Raw } else { "" }
            throw "FakeBridge exited early with code $($proc.ExitCode). stderr=$stderr"
        }
    }
    if (-not $found) {
        throw "Timed out waiting for FakeBridge session descriptor under $sessionRoot"
    }
    Write-Host "FakeBridge smoke OK"
}
finally {
    if ($proc -and -not $proc.HasExited) {
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }
}
