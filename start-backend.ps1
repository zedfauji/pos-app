$ErrorActionPreference = "Stop"
$host.ui.RawUI.WindowTitle = "MagiDesk Backend Launcher"

# Service Configuration
# Note: Profile names must match keys in Properties/launchSettings.json
$services = @(
    @{ Name = "UsersApi"; Path = "solution\backend\UsersApi"; Profile = "UsersApi"; Url = "http://localhost:55162/swagger/index.html" },
    @{ Name = "SettingsApi"; Path = "solution\backend\SettingsApi"; Profile = "SettingsApi"; Url = "http://localhost:53504/swagger/index.html" },
    @{ Name = "InventoryApi"; Path = "solution\backend\InventoryApi"; Profile = "http"; Url = "http://localhost:5117/swagger/index.html" },
    @{ Name = "MenuApi"; Path = "solution\backend\MenuApi"; Profile = "http"; Url = "http://localhost:5227/swagger/index.html" },
    @{ Name = "DiscountApi"; Path = "solution\backend\DiscountApi"; Profile = "http"; Url = "http://localhost:5229/swagger/index.html" },
    @{ Name = "CustomerApi"; Path = "solution\backend\CustomerApi"; Profile = "CustomerApi"; Url = "http://localhost:51372/swagger/index.html" },
    @{ Name = "TablesApi"; Path = "solution\backend\TablesApi"; Profile = "TablesApi"; Url = "http://localhost:53505/swagger/index.html" },
    @{ Name = "OrderApi"; Path = "solution\backend\OrderApi"; Profile = "http"; Url = "http://localhost:5256/swagger/index.html" },
    @{ Name = "PaymentApi"; Path = "solution\backend\PaymentApi"; Profile = "PaymentApi"; Url = "http://localhost:55463/swagger/index.html" },
    @{ Name = "ReportingApi"; Path = "solution\backend\ReportingApi"; Profile = "ReportingApi"; Url = "http://localhost:5228/swagger/index.html" }
)

Function Check-Service-Health {
    param ( [string]$Url, [int]$TimeoutSeconds = 30 )
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $uri = [Uri]$Url
    $port = $uri.Port
    
    Write-Host "   Waiting for port $port..." -NoNewline
    
    while ($sw.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
        $tcp = New-Object System.Net.Sockets.TcpClient
        try {
            # Try to connect with a short timeout
            $connect = $tcp.BeginConnect("localhost", $port, $null, $null)
            $success = $connect.AsyncWaitHandle.WaitOne(500, $false)
            
            if ($success) {
                # Verify connection is truly established
                try {
                    $tcp.EndConnect($connect)
                    Write-Host " OK!" -ForegroundColor Green
                    $tcp.Close()
                    $tcp.Dispose()
                    return $true
                }
                catch {
                    Write-Host "." -NoNewline
                }
            }
            else {
                Write-Host "." -NoNewline
            }
        }
        catch {
            Write-Host "." -NoNewline
        }
        finally {
            if ($tcp.Connected) { $tcp.Close() }
            $tcp.Dispose()
        }
        Start-Sleep -Milliseconds 500
    }
    
    Write-Host " TIMEOUT!" -ForegroundColor Red
    return $false
}

Function Start-Service-Verified {
    param (
        [string]$ProjectName,
        [string]$ProjectPath,
        [string]$LaunchProfile,
        [string]$HealthUrl
    )

    Write-Host "`n--------------------------------------------------" -ForegroundColor Gray
    Write-Host "STEP: $ProjectName" -ForegroundColor Cyan
    
    # 1. Build
    Write-Host "   Building..." -ForegroundColor DarkGray
    dotnet build $ProjectPath -v q
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "!!! BUILD FAILED for $ProjectName. Aborting. !!!" -ForegroundColor Red
        exit 1
    }

    # 2. Start (New Window)
    Write-Host "   Starting process..." -ForegroundColor DarkGray
    $proc = Start-Process dotnet -ArgumentList "run --project $ProjectPath --launch-profile $LaunchProfile" -PassThru
    
    # 3. Verify
    if (-not (Check-Service-Health -Url $HealthUrl)) {
        Write-Host "!!! SERVICE FAILED TO START: $ProjectName !!!" -ForegroundColor Red
        Write-Host "   Killing process..."
        if (-not $proc.HasExited) { Stop-Process -InputObject $proc -Force }
        exit 1
    }
}

# --- Main Execution ---

# Cleanup
Write-Host "Cleaning up old processes..." -ForegroundColor Yellow
taskkill /F /IM dotnet.exe 2>$null
$global:Error.Clear()
Start-Sleep -Seconds 2

# Loop through services
foreach ($svc in $services) {
    Start-Service-Verified -ProjectName $svc.Name -ProjectPath $svc.Path -LaunchProfile $svc.Profile -HealthUrl $svc.Url
}

Write-Host "`n--------------------------------------------------" -ForegroundColor Gray
Write-Host "ALL SERVICES STARTED & VERIFIED." -ForegroundColor Green
Write-Host "Press any key to exit launcher (processes will continue)..."
Read-Host
