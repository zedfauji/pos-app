# Get Cloud Run Logs for RBAC Diagnostics
# This script fetches recent logs from Cloud Run services to diagnose 500 errors

param(
    [string]$ServiceName = "",
    [string]$Region = "northamerica-south1",
    [int]$Lines = 50
)

$ErrorActionPreference = "Stop"

function Get-CloudRunLogs {
    param(
        [string]$Service,
        [string]$Reg,
        [int]$LogLines = 50
    )
    
    Write-Host "`n=== Logs for $Service ===" -ForegroundColor Cyan
    Write-Host "Region: $Reg" -ForegroundColor Gray
    Write-Host ""
    
    try {
        $logs = gcloud.cmd logging read "resource.type=cloud_run_revision AND resource.labels.service_name=$Service AND resource.labels.location=$Reg" --limit $LogLines --format json 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            $logEntries = $logs | ConvertFrom-Json
            
            if ($logEntries.Count -eq 0) {
                Write-Host "  No recent logs found" -ForegroundColor Yellow
                return
            }
            
            foreach ($entry in $logEntries) {
                $timestamp = $entry.timestamp
                $severity = $entry.severity
                $textPayload = $entry.textPayload
                $jsonPayload = $entry.jsonPayload
                
                $color = switch ($severity) {
                    "ERROR" { "Red" }
                    "WARNING" { "Yellow" }
                    "INFO" { "Cyan" }
                    default { "White" }
                }
                
                Write-Host "[$timestamp] [$severity]" -ForegroundColor $color -NoNewline
                
                if ($textPayload) {
                    Write-Host " $textPayload" -ForegroundColor White
                } elseif ($jsonPayload) {
                    $message = $jsonPayload.message
                    $exception = $jsonPayload.exception
                    $error = $jsonPayload.error
                    
                    if ($message) {
                        Write-Host " $message" -ForegroundColor White
                    }
                    if ($exception) {
                        Write-Host "  Exception: $exception" -ForegroundColor Red
                    }
                    if ($error) {
                        Write-Host "  Error: $error" -ForegroundColor Red
                    }
                    if ($jsonPayload -and -not $message -and -not $exception -and -not $error) {
                        Write-Host " $($jsonPayload | ConvertTo-Json -Compress)" -ForegroundColor Gray
                    }
                }
            }
        } else {
            Write-Host "  Failed to fetch logs: $logs" -ForegroundColor Red
        }
    } catch {
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "=== Cloud Run Logs Diagnostic ===" -ForegroundColor Cyan
Write-Host "Fetching last $Lines log entries for each service..."
Write-Host ""

if ($ServiceName) {
    # Get logs for specific service
    Get-CloudRunLogs -Service $ServiceName -Reg $Region -LogLines $Lines
} else {
    # Get logs for all RBAC-related services
    $services = @(
        @{ Name = "magidesk-settings"; Region = "northamerica-south1" },
        @{ Name = "magidesk-menu"; Region = "northamerica-south1" },
        @{ Name = "magidesk-order"; Region = "northamerica-south1" },
        @{ Name = "magidesk-payment"; Region = "northamerica-south1" },
        @{ Name = "magidesk-inventory"; Region = "northamerica-south1" },
        @{ Name = "magidesk-users"; Region = "northamerica-south1" }
    )
    
    foreach ($svc in $services) {
        Get-CloudRunLogs -Service $svc.Name -Reg $svc.Region -LogLines $Lines
    }
}

Write-Host "`n=== Log Fetch Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "To get logs for a specific service:" -ForegroundColor Yellow
Write-Host "  pwsh -File get-cloudrun-logs.ps1 -ServiceName 'magidesk-settings' -Lines 100" -ForegroundColor White

