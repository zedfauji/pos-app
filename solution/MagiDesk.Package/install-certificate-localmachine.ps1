# Install test certificate in Local Machine store for MSIX package signing
param(
    [string]$CertificatePath = "MagiDeskTestCert.cer",
    [string]$PfxPath = "MagiDeskTestCert.pfx",
    [string]$PfxPassword = "MagiDesk123"
)

Write-Host "Installing certificate in Local Machine store for MSIX package signing..."
Write-Host "Note: This requires Administrator privileges."

# Check if running as administrator
$currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
$isAdmin = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Error "This script must be run as Administrator to install certificates in Local Machine store."
    Write-Host "Please run PowerShell as Administrator and try again."
    exit 1
}

# Check if certificate files exist
if (!(Test-Path $CertificatePath)) {
    Write-Error "Certificate file not found: $CertificatePath"
    exit 1
}

if (!(Test-Path $PfxPath)) {
    Write-Error "PFX file not found: $PfxPath"
    exit 1
}

try {
    # Import the certificate to Local Machine stores
    Write-Host "Installing certificate to Local Machine certificate stores..."
    
    # Import to Local Machine Trusted Root Certification Authorities
    Import-Certificate -FilePath $CertificatePath -CertStoreLocation "Cert:\LocalMachine\Root"
    Write-Host "Certificate imported to Local Machine Trusted Root Certification Authorities"

    # Import to Local Machine Trusted Publishers
    Import-Certificate -FilePath $CertificatePath -CertStoreLocation "Cert:\LocalMachine\TrustedPublisher"
    Write-Host "Certificate imported to Local Machine Trusted Publishers"

    # Import to Local Machine Personal store
    Import-Certificate -FilePath $CertificatePath -CertStoreLocation "Cert:\LocalMachine\My"
    Write-Host "Certificate imported to Local Machine Personal store"

    # Import the PFX with private key to Local Machine
    $pfxPasswordSecure = ConvertTo-SecureString -String $PfxPassword -Force -AsPlainText
    Import-PfxCertificate -FilePath $PfxPath -CertStoreLocation "Cert:\LocalMachine\My" -Password $pfxPasswordSecure
    Write-Host "PFX certificate imported to Local Machine with private key"

    Write-Host ""
    Write-Host "Certificate installation to Local Machine completed successfully!"
    Write-Host ""
    Write-Host "You can now install the MSIX package without certificate errors."
    
} catch {
    Write-Error "Failed to install certificate: $($_.Exception.Message)"
    exit 1
}


