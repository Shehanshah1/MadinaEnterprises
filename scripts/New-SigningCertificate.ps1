<#
.SYNOPSIS
    One-time: generate a self-signed code-signing cert for internal MSIX
    distribution and export both the PFX (for signing) and CER (for user trust).

.DESCRIPTION
    Run this ONCE on a Windows machine with admin rights. Produces:

      certs/MadinaEnterprises.pfx   - private key + cert, used by the build to
                                      sign the MSIX. Protected by -Password.
      certs/MadinaEnterprises.cer   - public cert only, shipped to users so
                                      Windows will trust the MSIX signature.

    Keep the PFX secret. Commit ONLY the CER if you want to; it's public.

    For CI:
      1. Base64-encode the PFX:
           [Convert]::ToBase64String([IO.File]::ReadAllBytes(".\certs\MadinaEnterprises.pfx")) |
               Set-Clipboard
      2. Paste into a GitHub repository secret named SIGNING_CERT_PFX_BASE64.
      3. Store the PFX password in another secret named SIGNING_CERT_PASSWORD.

.PARAMETER Password
    Password used to protect the PFX. Required.

.PARAMETER Subject
    Cert subject. MUST match the Publisher in Package.appxmanifest.
    Default: "CN=Madina Enterprises"

.PARAMETER OutputDir
    Where to drop the PFX and CER. Default: ./certs

.EXAMPLE
    ./New-SigningCertificate.ps1 -Password (Read-Host -AsSecureString "PFX password")
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [SecureString] $Password,
    [string] $Subject    = "CN=Madina Enterprises",
    [string] $OutputDir  = "$PSScriptRoot/../certs",
    [int]    $YearsValid = 5
)

$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

Write-Host "==> Creating self-signed code-signing cert: $Subject" -ForegroundColor Cyan
$cert = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject $Subject `
    -KeyUsage DigitalSignature `
    -FriendlyName "Madina Enterprises MSIX Signing" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter (Get-Date).AddYears($YearsValid) `
    -TextExtension @(
        "2.5.29.37={text}1.3.6.1.5.5.7.3.3",  # Code Signing EKU
        "2.5.29.19={text}"                    # Basic Constraints: not a CA
    )

$pfxPath = Join-Path $OutputDir 'MadinaEnterprises.pfx'
$cerPath = Join-Path $OutputDir 'MadinaEnterprises.cer'

Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $Password | Out-Null
Export-Certificate    -Cert $cert -FilePath $cerPath                      | Out-Null

Write-Host ""
Write-Host "Cert created and exported:" -ForegroundColor Green
Write-Host "  PFX (KEEP SECRET): $pfxPath"
Write-Host "  CER (ship to users): $cerPath"
Write-Host "  Thumbprint: $($cert.Thumbprint)"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  - Add certs/ to .gitignore (the PFX must never be committed)."
Write-Host "  - Base64-encode the PFX and set GitHub secrets:"
Write-Host "      SIGNING_CERT_PFX_BASE64"
Write-Host "      SIGNING_CERT_PASSWORD"
Write-Host "  - You can upload the CER as a release asset so users can trust it."
