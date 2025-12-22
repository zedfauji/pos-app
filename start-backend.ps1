$ErrorActionPreference = "Stop"
$host.ui.RawUI.WindowTitle = "MagiDesk Backend Launcher"

Function Start-Service-Safe {
    param (
        [string]$ProjectName,
        [string]$ProjectPath,
        [string]$LaunchProfile
    )

    Write-Host "--------------------------------------------------" -ForegroundColor Gray
    Write-Host "BUILDING: $ProjectName..." -ForegroundColor Cyan
    
    # Run build directly (simpler, usually avoids hangs)
    dotnet build $ProjectPath
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "!!! BUILD FAILED for $ProjectName. Aborting. !!!" -ForegroundColor Red
        exit 1
    }

    Write-Host "STARTING: $ProjectName..." -ForegroundColor Cyan
    # Run in background (NoNewWindow lets us see logs, but doesn't block script)
    Start-Process dotnet -ArgumentList "run --project $ProjectPath --launch-profile $LaunchProfile" -NoNewWindow
    
    Start-Sleep -Seconds 2
}

# 1. Kill existing
Write-Host "Step 1: Cleaning up old processes..." -ForegroundColor Yellow
taskkill /F /IM dotnet.exe 2>$null
# Reset error state from taskkill (it errors if no process found, which is fine)
$global:Error.Clear()
Start-Sleep -Seconds 2

# 2. Start Services
Start-Service-Safe -ProjectName "TablesApi" -ProjectPath "solution\backend\TablesApi" -LaunchProfile "TablesApi"
Start-Service-Safe -ProjectName "MenuApi" -ProjectPath "solution\backend\MenuApi" -LaunchProfile "http"
Start-Service-Safe -ProjectName "InventoryApi" -ProjectPath "solution\backend\InventoryApi" -LaunchProfile "http"
Start-Service-Safe -ProjectName "UsersApi" -ProjectPath "solution\backend\UsersApi" -LaunchProfile "UsersApi"
Start-Service-Safe -ProjectName "ReportingApi" -ProjectPath "solution\backend\ReportingApi" -LaunchProfile "ReportingApi"

Write-Host "--------------------------------------------------" -ForegroundColor Gray
Write-Host "ALL SERVICES STARTED." -ForegroundColor Green
Write-Host "TablesApi Swagger: https://localhost:53503/swagger"
Write-Host "MenuApi Swagger: http://localhost:5227/swagger"
Write-Host "Press any key to exit launcher (processes will continue)..."
Read-Host
