# Create a new test certificate with proper extensions for MSIX
param(
    [string]$CertificateName = "MagiDeskPOS",
    [string]$OutputPath = "."
)

Write-Host "Creating new test certificate for MSIX package signing..."

# Create a self-signed certificate with proper extensions for code signing
$cert = New-SelfSignedCertificate -Type Custom -Subject "CN=MagiDesk POS System" -KeyUsage DigitalSignature -FriendlyName $CertificateName -CertStoreLocation "Cert:\CurrentUser\My" -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3,1.3.6.1.4.1.311.10.3.13", "2.5.29.19={text}")

if ($cert) {
    Write-Host "Certificate created successfully!"
    Write-Host "Thumbprint: $($cert.Thumbprint)"
    Write-Host "Subject: $($cert.Subject)"
    
    # Export the certificate to a .cer file
    $certPath = Join-Path $OutputPath "MagiDeskPOS.cer"
    Export-Certificate -Cert $cert -FilePath $certPath -Type CERT
    Write-Host "Certificate exported to: $certPath"
    
    # Export the private key to a .pfx file
    $pfxPath = Join-Path $OutputPath "MagiDeskPOS.pfx"
    $password = ConvertTo-SecureString -String "MagiDesk123" -Force -AsPlainText
    Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $password
    Write-Host "Private key exported to: $pfxPath"
    Write-Host "Password: MagiDesk123"
    
    # Install to Trusted Root and Trusted Publishers
    Import-Certificate -FilePath $certPath -CertStoreLocation "Cert:\CurrentUser\Root"
    Import-Certificate -FilePath $certPath -CertStoreLocation "Cert:\CurrentUser\TrustedPublisher"
    Write-Host "Certificate installed to Trusted Root and Trusted Publishers"
    
    Write-Host ""
    Write-Host "New certificate created and installed!"
    Write-Host "Update the package project to use thumbprint: $($cert.Thumbprint)"
    
    return $cert
} else {
    Write-Error "Failed to create certificate"
    exit 1
}


