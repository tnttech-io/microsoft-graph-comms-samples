@echo off
echo "Starting certificate conversion and installation..."

REM Define certificate environment variables
set CERT_PATH=/app/certs
set CERT_PFX_PATH=%CERT_PATH%/bot-cert.pfx
set CERT_PASSWORD=your_cert_password_here  REM Set this securely

REM Convert and install certificates
echo "Checking for certificate at %CERT_PFX_PATH%..."
if exist "%CERT_PFX_PATH%" (
    echo "Certificate found! Installing..."
    powershell -Command "& {
        $pfxPath = '%CERT_PFX_PATH%'
        $password = ConvertTo-SecureString '%CERT_PASSWORD%' -AsPlainText -Force
        $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2
        $cert.Import($pfxPath, $password, [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::MachineKeySet)
        $store = New-Object System.Security.Cryptography.X509Certificates.X509Store 'My', 'LocalMachine'
        $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
        $store.Add($cert)
        $store.Close()
        Write-Output 'Certificate successfully installed.'
    }"
) else (
    echo "No certificate found at %CERT_PFX_PATH%! Skipping installation..."
)

REM Start HueBot
echo "Starting HueBot..."
dotnet HueBot.dll
