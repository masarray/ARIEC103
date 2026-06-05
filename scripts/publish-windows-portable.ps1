# Copyright 2026 Ari Sulistiono
# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
  Builds a Windows x64 portable release package for ArIEC103.

.DESCRIPTION
  This script creates a portable ZIP containing the WPF desktop app, CLI tools,
  sample mapping profile, licenses, and quick-start documents. It is intended
  for local release preparation and for GitHub Actions release packaging.

.EXAMPLE
  pwsh ./scripts/publish-windows-portable.ps1 -Version 1.3.0
#>
[CmdletBinding()]
param(
    [string]$Version = "1.3.0",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SkipTests,
    [switch]$FrameworkDependent
)

$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$ArtifactRoot = Join-Path $RepoRoot "artifacts"
$ReleaseRoot = Join-Path $ArtifactRoot "release"
$StagingRoot = Join-Path $ReleaseRoot "ArIEC103-v$Version-$Runtime-portable"
$DesktopOut = Join-Path $StagingRoot "app"
$CliOut = Join-Path $StagingRoot "cli"
$PackageZip = Join-Path $ReleaseRoot "ArIEC103-v$Version-$Runtime-portable.zip"
$ChecksumFile = Join-Path $ReleaseRoot "SHA256SUMS.txt"
$SelfContained = if ($FrameworkDependent) { "false" } else { "true" }

Write-Host "ArIEC103 portable release packaging" -ForegroundColor Cyan
Write-Host "Repository : $RepoRoot"
Write-Host "Version    : $Version"
Write-Host "Runtime    : $Runtime"
Write-Host "Config     : $Configuration"
Write-Host "Self-contained: $SelfContained"

Push-Location $RepoRoot
try {
    if (Test-Path $ReleaseRoot) {
        Remove-Item $ReleaseRoot -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $DesktopOut, $CliOut | Out-Null

    dotnet restore ArIEC103.sln
    dotnet build ArIEC103.sln --configuration $Configuration --no-restore

    if (-not $SkipTests) {
        dotnet run --project tests/ArIEC103.Protocol.Tests/ArIEC103.Protocol.Tests.csproj --configuration $Configuration --no-build
    }

    dotnet publish src/ArIEC103.Desktop/ArIEC103.Desktop.csproj `
        --configuration $Configuration `
        --runtime $Runtime `
        --self-contained:$SelfContained `
        -p:PublishSingleFile=false `
        -p:EnableCompressionInSingleFile=true `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        --output $DesktopOut

    dotnet publish src/ArIEC103.Cli/ArIEC103.Cli.csproj `
        --configuration $Configuration `
        --runtime $Runtime `
        --self-contained:$SelfContained `
        -p:PublishSingleFile=false `
        -p:EnableCompressionInSingleFile=true `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        --output $CliOut

    $DocsOut = Join-Path $StagingRoot "docs"
    $SamplesOut = Join-Path $StagingRoot "samples"
    New-Item -ItemType Directory -Force -Path $DocsOut, $SamplesOut | Out-Null

    Copy-Item LICENSE, NOTICE, THIRD_PARTY_NOTICES.md, README.md -Destination $StagingRoot -Force
    Copy-Item docs/QUICK_START.md, docs/TROUBLESHOOTING.md, docs/VALIDATION_MATRIX.md, docs/RELEASE_PACKAGING.md -Destination $DocsOut -Force
    Copy-Item samples/mapping-profiles -Destination $SamplesOut -Recurse -Force

    $LaunchDesktop = @"
@echo off
setlocal
cd /d "%~dp0app"
start "ArIEC103" "ArIEC103.Desktop.exe"
"@
    Set-Content -Path (Join-Path $StagingRoot "Start-ArIEC103.bat") -Value $LaunchDesktop -Encoding ASCII

    $CliHelp = @"
@echo off
setlocal
cd /d "%~dp0cli"
"ArIEC103.Cli.exe" --help
pause
"@
    Set-Content -Path (Join-Path $StagingRoot "Open-CLI-Help.bat") -Value $CliHelp -Encoding ASCII

    $PortableReadme = @"
ArIEC103 v$Version Windows portable package

Start desktop app:
  Start-ArIEC103.bat

Open CLI help:
  Open-CLI-Help.bat

Included folders:
  app\      Windows desktop application
  cli\      Command-line tools
  docs\     Quick start, troubleshooting, validation matrix, packaging notes
  samples\  Example user mapping profile

Recommended first check:
  1. Start the desktop app.
  2. Open Setup.
  3. Select COM port, baudrate, parity, link address, and common address.
  4. Start with Reset FCB enabled.
  5. Run General Interrogation when a startup snapshot is needed.
  6. Export evidence after the session.

Do not share exported evidence externally before reviewing project/customer names,
serial settings, and raw protocol evidence.
"@
    Set-Content -Path (Join-Path $StagingRoot "README-PORTABLE.txt") -Value $PortableReadme -Encoding UTF8

    if (Test-Path $PackageZip) {
        Remove-Item $PackageZip -Force
    }
    Compress-Archive -Path (Join-Path $StagingRoot "*") -DestinationPath $PackageZip -CompressionLevel Optimal

    $Hash = Get-FileHash -Algorithm SHA256 $PackageZip
    "{0}  {1}" -f $Hash.Hash.ToLowerInvariant(), (Split-Path $PackageZip -Leaf) | Set-Content -Path $ChecksumFile -Encoding ASCII

    Write-Host "Package created:" -ForegroundColor Green
    Write-Host "  $PackageZip"
    Write-Host "Checksum:" -ForegroundColor Green
    Get-Content $ChecksumFile | Write-Host
}
finally {
    Pop-Location
}
