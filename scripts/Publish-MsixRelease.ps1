<#
.SYNOPSIS
    Builds, signs, and stages a Madina Enterprises MSIX release configured for
    GitHub Releases hosting.

.DESCRIPTION
    Run on a Windows machine with the .NET MAUI + Windows SDK workloads
    installed, OR from the release GitHub Actions workflow.

    Produces ./publish/ containing:
      - MadinaEnterprises_<Version>_x64.msix   (signed app package)
      - MadinaEnterprises.appinstaller         (update manifest, stamped)
      - MadinaEnterprises.cer                  (public cert, if provided)

    Upload the whole folder to a GitHub Release tagged v<Version>.

.PARAMETER Version
    Four-part version, e.g. 1.0.1.0. MUST be strictly higher than the last
    published version for Windows to apply the update.

.PARAMETER GitHubRepo
    Owner/repo slug, e.g. Shehanshah1/MadinaEnterprises. Used to build the
    release asset URLs embedded in the .appinstaller file.

.PARAMETER CertThumbprint
    Thumbprint of the cert in CurrentUser\My to sign with. Use this for local
    builds. Mutually exclusive with -PfxPath.

.PARAMETER PfxPath
    Path to a PFX file to sign with. Use this in CI. Requires -PfxPassword.

.PARAMETER PfxPassword
    Password (SecureString or plain string) for the PFX. Required with -PfxPath.

.PARAMETER CerPath
    Optional: path to a .cer file to copy into the publish folder so users can
    trust it once.

.PARAMETER OutputDir
    Where to place final artefacts. Default: ./publish

.EXAMPLE
    # Local build, signing with a cert already in the store
    ./Publish-MsixRelease.ps1 -Version 1.0.1.0 `
        -GitHubRepo "Shehanshah1/MadinaEnterprises" `
        -CertThumbprint "ABCD1234..."

.EXAMPLE
    # CI build, signing with a PFX decoded from a secret
    ./Publish-MsixRelease.ps1 -Version 1.0.1.0 `
        -GitHubRepo "Shehanshah1/MadinaEnterprises" `
        -PfxPath "./signing.pfx" -PfxPassword $env:PFX_PASSWORD `
        -CerPath "./certs/MadinaEnterprises.cer"
#>

[CmdletBinding(DefaultParameterSetName = 'Thumbprint')]
param(
    [Parameter(Mandatory = $true)] [string] $Version,
    [Parameter(Mandatory = $true)] [string] $GitHubRepo,

    [Parameter(ParameterSetName = 'Thumbprint', Mandatory = $true)]
    [string] $CertThumbprint,

    [Parameter(ParameterSetName = 'Pfx', Mandatory = $true)]
    [string] $PfxPath,

    [Parameter(ParameterSetName = 'Pfx', Mandatory = $true)]
    [object] $PfxPassword,

    [string] $CerPath,
    [string] $OutputDir = "$PSScriptRoot/../publish"
)

$ErrorActionPreference = 'Stop'

if ($Version -notmatch '^\d+\.\d+\.\d+\.\d+$') {
    throw "Version must be four-part (e.g. 1.0.1.0). Got: '$Version'"
}

$repoRoot    = Resolve-Path "$PSScriptRoot/.."
$csproj      = Join-Path $repoRoot 'MadinaEnterprises.csproj'
$manifest    = Join-Path $repoRoot 'Platforms/Windows/Package.appxmanifest'
$appinstTmpl = Join-Path $repoRoot 'Platforms/Windows/MadinaEnterprises.appinstaller'

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
$OutputDir = (Resolve-Path $OutputDir).Path

# --- URLs for GitHub Releases hosting ---------------------------------------
# .appinstaller served from the STABLE "latest/download" URL, which redirects
# to whichever release is tagged Latest. The MSIX inside is pinned to this
# release's tag.
$tag               = "v$Version"
$appInstallerUri   = "https://github.com/$GitHubRepo/releases/latest/download/MadinaEnterprises.appinstaller"
$msixAssetName     = "MadinaEnterprises_${Version}_x64.msix"
$msixUri           = "https://github.com/$GitHubRepo/releases/download/$tag/$msixAssetName"

# --- Stamp manifest ----------------------------------------------------------
Write-Host "==> Stamping version $Version into Package.appxmanifest" -ForegroundColor Cyan
[xml]$xml = Get-Content $manifest
$xml.Package.Identity.Version = $Version
$xml.Save($manifest)

# --- Build signing args ------------------------------------------------------
$signArgs = @()
if ($PSCmdlet.ParameterSetName -eq 'Thumbprint') {
    $signArgs += "-p:PackageCertificateThumbprint=$CertThumbprint"
} else {
    if (-not (Test-Path $PfxPath)) { throw "PFX not found: $PfxPath" }
    $pfxFull = (Resolve-Path $PfxPath).Path
    $pwdPlain = if ($PfxPassword -is [SecureString]) {
        [Runtime.InteropServices.Marshal]::PtrToStringAuto(
            [Runtime.InteropServices.Marshal]::SecureStringToBSTR($PfxPassword))
    } else { [string]$PfxPassword }

    $signArgs += "-p:PackageCertificateKeyFile=$pfxFull"
    $signArgs += "-p:PackageCertificatePassword=$pwdPlain"
}

# --- Publish -----------------------------------------------------------------
Write-Host "==> dotnet publish (Release, win10-x64)" -ForegroundColor Cyan
$publishArgs = @(
    'publish', $csproj,
    '-f', 'net8.0-windows10.0.19041.0',
    '-c', 'Release',
    '-p:RuntimeIdentifierOverride=win10-x64',
    '-p:GenerateAppxPackageOnBuild=true',
    '-p:AppxPackageSigningEnabled=true',
    "-p:AppxPackageDir=$OutputDir\",
    '-p:AppxBundle=Never',
    '-p:UapAppxPackageBuildMode=SideloadOnly'
) + $signArgs

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

# --- Locate the MSIX the publish just produced and flatten to OutputDir -----
$msix = Get-ChildItem -Path $OutputDir -Recurse -Filter $msixAssetName |
        Select-Object -First 1
if (-not $msix) { throw "Expected $msixAssetName under $OutputDir — not found." }
$flatMsix = Join-Path $OutputDir $msixAssetName
if ($msix.FullName -ne $flatMsix) {
    Copy-Item $msix.FullName -Destination $flatMsix -Force
}

# --- Stamp the .appinstaller ------------------------------------------------
Write-Host "==> Writing .appinstaller" -ForegroundColor Cyan
[xml]$ai = Get-Content $appinstTmpl
$ai.AppInstaller.Uri                 = $appInstallerUri
$ai.AppInstaller.Version             = $Version
$ai.AppInstaller.MainPackage.Version = $Version
$ai.AppInstaller.MainPackage.Uri     = $msixUri
$ai.Save((Join-Path $OutputDir 'MadinaEnterprises.appinstaller'))

# --- Optional: copy public cert ---------------------------------------------
if ($CerPath) {
    if (-not (Test-Path $CerPath)) { throw "CER not found: $CerPath" }
    Copy-Item $CerPath -Destination (Join-Path $OutputDir 'MadinaEnterprises.cer') -Force
}

Write-Host ""
Write-Host "Release staged in $OutputDir" -ForegroundColor Green
Write-Host "  App Installer URI: $appInstallerUri"
Write-Host "  MSIX URI:          $msixUri"
Write-Host "  Tag to create:     $tag"
