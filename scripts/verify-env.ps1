param(
    [string]$NavisworksPath = "C:\Program Files\Autodesk\Navisworks Manage 2026"
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

function Add-Check {
    param(
        [string]$Name,
        [bool]$Ok,
        [string]$Detail
    )

    $status = if ($Ok) { "OK" } else { "MISSING" }
    Write-Host ("[{0}] {1} - {2}" -f $status, $Name, $Detail)
    if (-not $Ok) {
        $script:failures.Add($Name) | Out-Null
    }
}

$apiDll = Join-Path $NavisworksPath "Autodesk.Navisworks.Api.dll"
$clashDll = Join-Path $NavisworksPath "Autodesk.Navisworks.Clash.dll"
$navisOk = (Test-Path $apiDll) -and (Test-Path $clashDll)
$navisDetail = $NavisworksPath
if (Test-Path $apiDll) {
    $navisDetail += " / API " + [Reflection.AssemblyName]::GetAssemblyName($apiDll).Version.ToString()
}
Add-Check "Navisworks Manage 2026 API" $navisOk $navisDetail

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
$sdkList = @()
if ($dotnet) {
    $sdkList = & dotnet --list-sdks 2>$null
}
$hasSdk = $sdkList.Count -gt 0
$hasNet8Sdk = $sdkList | Where-Object { $_ -match "^(8|9|10)\." } | Select-Object -First 1
Add-Check ".NET SDK" $hasSdk ($(if ($hasSdk) { $sdkList -join "; " } else { "dotnet SDK not found" }))
Add-Check ".NET 8+ SDK" ([bool]$hasNet8Sdk) ($(if ($hasNet8Sdk) { $hasNet8Sdk } else { "Install .NET 8 SDK or newer" }))

$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuildPaths = @()
if (Test-Path $vswhere) {
    $installPath = & $vswhere -latest -requires Microsoft.Component.MSBuild -property installationPath 2>$null
    if ($installPath) {
        $msbuildPaths += Join-Path $installPath "MSBuild\Current\Bin\MSBuild.exe"
    }
}
$msbuildPaths += Get-ChildItem "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022" -Recurse -Filter MSBuild.exe -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -like "*\MSBuild\Current\Bin\MSBuild.exe" } |
    Select-Object -ExpandProperty FullName
$msbuildPath = $msbuildPaths | Where-Object { Test-Path $_ } | Select-Object -First 1
Add-Check "Visual Studio 2022 Build Tools / MSBuild" ([bool]$msbuildPath) ($(if ($msbuildPath) { $msbuildPath } else { "Install VS 2022 Build Tools with .NET desktop build tools" }))

$netFxRef = "${env:ProgramFiles(x86)}\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
$netFxSdkReg = "HKLM:\SOFTWARE\Microsoft\Microsoft SDKs\NETFXSDK\4.8"
$hasNetFxTargeting = (Test-Path $netFxRef) -or (Test-Path $netFxSdkReg)
Add-Check ".NET Framework 4.8 targeting/developer pack" $hasNetFxTargeting ($(if ($hasNetFxTargeting) { $netFxRef } else { "Install .NET Framework 4.8 Developer Pack" }))

if ($failures.Count -gt 0) {
    Write-Host ""
    Write-Host "Environment is not ready. Missing:"
    foreach ($failure in $failures) {
        Write-Host " - $failure"
    }
    exit 1
}

Write-Host ""
Write-Host "Environment is ready."
