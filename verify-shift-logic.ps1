
# verification-shift-logic.ps1
$ErrorActionPreference = "Stop"

$Headers = @{ "Content-Type" = "application/json" }

function Test-Endpoint {
    param($Method, $Url, $Body = $null, $ExpectedStatus)
    Write-Host "TEST: $Method $Url" -NoNewline
    try {
        if ($Body) {
            $resp = Invoke-WebRequest -Method $Method -Uri $Url -Body $Body -Headers $Headers -ErrorAction Stop
        }
        else {
            $resp = Invoke-WebRequest -Method $Method -Uri $Url -Headers $Headers -ErrorAction Stop
        }
        $status = $resp.StatusCode
    }
    catch {
        if ($_.Exception.Response) {
            $status = $_.Exception.Response.StatusCode
        }
        else {
            Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
            return $false
        }
    }

    if ($status -eq $ExpectedStatus) {
        Write-Host "   PASSED ($status)" -ForegroundColor Green
        return $true
    }
    else {
        Write-Host "   FAILED (Got $status, Expected $ExpectedStatus)" -ForegroundColor Red
        return $false
    }
}

Write-Host "--- VERIFYING SHIFT LOGIC ---" -ForegroundColor Cyan

# 1. Check Current Shift
$shiftUrl = "http://localhost:53505/shifts"
try {
    $current = Invoke-RestMethod -Uri "$shiftUrl/current" -Method Get -ErrorAction Stop
}
catch {
    # If 204 No Content, current is null
    $current = $null
}

if ($current) {
    Write-Host "Found Open Shift: $($current.shiftNumber). Closing it..."
    # Ensure properties are properly named for JSON deserializer (PascalCase often preferred if defaults used)
    # But C# model properties are DeclaredCash, Note.
    $body = @{ DeclaredCash = 0; Note = "Auto-closed by verification script" } | ConvertTo-Json
    try {
        Invoke-WebRequest -Method Post -Uri "$shiftUrl/$($current.shiftId)/close" -Body $body -Headers $Headers
        Write-Host "Closed existing shift." -ForegroundColor Green
    }
    catch {
        Write-Host "Failed to close existing shift: $($_.Exception.Message)" -ForegroundColor Red
        # Print response body if available
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader $_.Exception.Response.GetResponseStream()
            Write-Host $reader.ReadToEnd()
        }
        exit 1
    }
}

# 2. Verify Gating (Start Session without Shift)
$sessionUrl = "http://localhost:53505/tables/T99_VERIFY/start"
$sessionBody = @{ ServerId = "1"; ServerName = "Tester" } | ConvertTo-Json

# Expect 423 Locked
Test-Endpoint -Method Post -Url $sessionUrl -Body $sessionBody -ExpectedStatus 423

# 3. Open Shift
Write-Host "Opening New Shift..."
$openBody = @{ StartingCash = 100.00; Note = "Opening via script" } | ConvertTo-Json
try {
    $openResp = Invoke-RestMethod -Method Post -Uri "$shiftUrl/open" -Body $openBody -Headers $Headers
    $shiftId = $openResp.shiftId
    Write-Host "Shift Opened: $shiftId" -ForegroundColor Green
}
catch {
    Write-Host "Failed to open shift: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader $_.Exception.Response.GetResponseStream()
        Write-Host $reader.ReadToEnd()
    }
    exit 1
}

# 4. Verify Gating (Start Session WITH Shift)
$success = Test-Endpoint -Method Post -Url $sessionUrl -Body $sessionBody -ExpectedStatus 200

if ($success) {
    # Clean up session
    # We need the session ID from step 4 output.
    # Re-run request to get ID (idempotent-ish if forces new session? no, start session might fail if already active)
    # Check active sessions to find T99_VERIFY
    $active = Invoke-RestMethod -Uri "http://localhost:53505/tables/active" -Method Get
    $mySess = $active | Where-Object { $_.tableLabel -eq "T99_VERIFY" }
    
    if ($mySess) {
        $sessId = $mySess.sessionId
        # End Session
        Invoke-WebRequest -Method Post -Uri "http://localhost:53505/tables/$sessId/end" -Headers $Headers
        Write-Host "Session Ended."
    }
}

# 5. Clean up Shift
try {
    Invoke-WebRequest -Method Post -Uri "$shiftUrl/$shiftId/close" -Body (@{ DeclaredCash = 100; Note = "Cleanup" } | ConvertTo-Json) -Headers $Headers
    Write-Host "Shift Closed." -ForegroundColor Green
}
catch {
    Write-Host "Failed to close shift: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader $_.Exception.Response.GetResponseStream()
        Write-Host $reader.ReadToEnd()
    }
}

Write-Host "--- VERIFICATION COMPLETE ---" -ForegroundColor Cyan
