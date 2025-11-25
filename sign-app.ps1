
$certName = "ThinkMineCert"
$certPassword = ConvertTo-SecureString -String "ThinkMine123" -Force -AsPlainText
$exePath = "c:\Users\DELL\.gemini\antigravity\playground\triple-observatory\ThinkMine\bin\Release\net8.0-windows\win-x64\publish\ThinkMine.exe"

# 1. Create Self-Signed Certificate
$cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject "CN=ThinkMine Publisher" -CertStoreLocation Cert:\CurrentUser\My -NotAfter (Get-Date).AddYears(5) -FriendlyName $certName

# 2. Export PFX
$pfxPath = "c:\Users\DELL\.gemini\antigravity\playground\triple-observatory\ThinkMine\ThinkMineCert.pfx"
Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $certPassword

# 3. Sign the Executable
Set-AuthenticodeSignature -FilePath $exePath -Certificate $cert

Write-Host "Created Certificate: $pfxPath" -ForegroundColor Green
Write-Host "Signed Executable: $exePath" -ForegroundColor Green
