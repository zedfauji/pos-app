# Create test certificate for MSIX package signing
param(
    [string]$CertificateName = "MagiDeskTestCert",
    [string]$OutputPath = "."
)

Write-Host "Creating test certificate for MSIX package signing..."

# Create a self-signed certificate for testing
$cert = New-SelfSignedCertificate -Type Custom -Subject "CN=MagiDesk Test Certificate" -KeyUsage DigitalSignature -FriendlyName $CertificateName -CertStoreLocation "Cert:\CurrentUser\My" -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")

if ($cert) {
    Write-Host "Certificate created successfully!"
    Write-Host "Thumbprint: $($cert.Thumbprint)"
    Write-Host "Subject: $($cert.Subject)"
    
    # Export the certificate to a .cer file
    $certPath = Join-Path $OutputPath "MagiDeskTestCert.cer"
    Export-Certificate -Cert $cert -FilePath $certPath -Type CERT
    Write-Host "Certificate exported to: $certPath"
    
    # Export the private key to a .pfx file
    $pfxPath = Join-Path $OutputPath "MagiDeskTestCert.pfx"
    $password = ConvertTo-SecureString -String "MagiDesk123" -Force -AsPlainText
    Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $password
    Write-Host "Private key exported to: $pfxPath"
    Write-Host "Password: MagiDesk123"
    
    Write-Host ""
    Write-Host "Next steps:"
    Write-Host "1. Install the certificate: Install the .cer file by double-clicking it"
    Write-Host "2. Update the package project to use this certificate"
    Write-Host "3. Rebuild the MSIX package"
    
    return $cert
} else {
    Write-Error "Failed to create certificate"
    exit 1
}


