# Install test certificate properly for MSIX package signing
param(
    [string]$CertificatePath = "MagiDeskTestCert.cer",
    [string]$PfxPath = "MagiDeskTestCert.pfx",
    [string]$PfxPassword = "MagiDesk123"
)

Write-Host "Installing certificate for MSIX package signing..."

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
    # Import the certificate to Personal store first
    $cert = Import-Certificate -FilePath $CertificatePath -CertStoreLocation "Cert:\CurrentUser\My"
    Write-Host "Certificate imported to Personal store: $($cert.Thumbprint)"

    # Import to Trusted Root Certification Authorities
    Import-Certificate -FilePath $CertificatePath -CertStoreLocation "Cert:\CurrentUser\Root"
    Write-Host "Certificate imported to Trusted Root Certification Authorities"

    # Import to Trusted Publishers
    Import-Certificate -FilePath $CertificatePath -CertStoreLocation "Cert:\CurrentUser\TrustedPublisher"
    Write-Host "Certificate imported to Trusted Publishers"

    # Import the PFX with private key
    $pfxPasswordSecure = ConvertTo-SecureString -String $PfxPassword -Force -AsPlainText
    Import-PfxCertificate -FilePath $PfxPath -CertStoreLocation "Cert:\CurrentUser\My" -Password $pfxPasswordSecure
    Write-Host "PFX certificate imported with private key"

    Write-Host ""
    Write-Host "Certificate installation completed successfully!"
    Write-Host "Certificate Thumbprint: $($cert.Thumbprint)"
    Write-Host "Subject: $($cert.Subject)"
    Write-Host ""
    Write-Host "You can now install the MSIX package without certificate errors."
    
    return $cert
} catch {
    Write-Error "Failed to install certificate: $($_.Exception.Message)"
    exit 1
}
