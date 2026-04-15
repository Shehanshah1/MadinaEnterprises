<#
.SYNOPSIS
    Builds, signs, and stages a Madina Enterprises MSIX release with a matching
    .appinstaller file for auto-updates.

.DESCRIPTION
    Run on a Windows machine with the MAUI + Windows SDK workloads installed.
    Produces an output folder you can upload as-is to GitHub Releases, Azure
    Blob, S3, or any HTTPS host.

.PARAMETER Version
    Four-part version, e.g. 1.0.1.0. MUST be higher than the currently
    installed version for updates to apply.

.PARAMETER BaseUri
    Public URL where the .msix and .appinstaller will be hosted, with a
    trailing slash. Example: https://downloads.example.com/madina/

.PARAMETER CertThumbprint
    Thumbprint of the code-signing cert in CurrentUser\My used to sign the MSIX.
    For self-signed internal distribution the cert subject must be
    "CN=Madina Enterprises" to match Package.appxmanifest.

.PARAMETER OutputDir
    Where to place the final MSIX + .appinstaller. Defaults to ./publish.

.EXAMPLE
    ./Publish-MsixRelease.ps1 -Version 1.0.1.0 `
        -BaseUri "https://downloads.example.com/madina/" `
        -CertThumbprint "ABCD1234..."
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $Version,
    [Parameter(Mandatory = $true)] [string] $BaseUri,
    [Parameter(Mandatory = $true)] [string] $CertThumbprint,
    [string] $OutputDir = "$PSScriptRoot/../publish"
)

$ErrorActionPreference = 'Stop'
if (-not $BaseUri.EndsWith('/')) { $BaseUri += '/' }

$repoRoot    = Resolve-Path "$PSScriptRoot/.."
$csproj      = Join-Path $repoRoot 'MadinaEnterprises.csproj'
$manifest    = Join-Path $repoRoot 'Platforms/Windows/Package.appxmanifest'
$appinstTmpl = Join-Path $repoRoot 'Platforms/Windows/MadinaEnterprises.appinstaller'

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

Write-Host "==> Stamping version $Version into Package.appxmanifest" -ForegroundColor Cyan
[xml]$xml = Get-Content $manifest
$xml.Package.Identity.Version = $Version
$xml.Save($manifest)

Write-Host "==> Restoring + publishing MSIX (x64, Release)" -ForegroundColor Cyan
dotnet publish $csproj `
    -f net8.0-windows10.0.19041.0 `
    -c Release `
    -p:RuntimeIdentifierOverride=win10-x64 `
    -p:GenerateAppxPackageOnBuild=true `
    -p:AppxPackageSigningEnabled=true `
    -p:PackageCertificateThumbprint=$CertThumbprint `
    -p:AppxPackageDir="$OutputDir\\" `
    -p:AppxBundle=Never `
    -p:UapAppxPackageBuildMode=SideloadOnly

$msix = Get-ChildItem -Path $OutputDir -Recurse -Filter "MadinaEnterprises_${Version}_x64.msix" |
        Select-Object -First 1
if (-not $msix) { throw "Did not find the expected MSIX under $OutputDir" }

# Flatten: copy the MSIX to $OutputDir root so the .appinstaller URL is simple.
Copy-Item $msix.FullName -Destination (Join-Path $OutputDir $msix.Name) -Force

Write-Host "==> Writing .appinstaller with BaseUri=$BaseUri" -ForegroundColor Cyan
[xml]$ai = Get-Content $appinstTmpl
$ai.AppInstaller.Uri         = "${BaseUri}MadinaEnterprises.appinstaller"
$ai.AppInstaller.Version     = $Version
$ai.AppInstaller.MainPackage.Version = $Version
$ai.AppInstaller.MainPackage.Uri     = "${BaseUri}$($msix.Name)"
$ai.Save((Join-Path $OutputDir 'MadinaEnterprises.appinstaller'))

Write-Host ""
Write-Host "Done. Upload the contents of $OutputDir to $BaseUri" -ForegroundColor Green
Write-Host "Users install once from: ${BaseUri}MadinaEnterprises.appinstaller" -ForegroundColor Green
