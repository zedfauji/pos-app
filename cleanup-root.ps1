# Cleanup Root Directory - Move Stale Files to Archive
# Run this script from the repository root

Write-Host "Starting cleanup of root directory..." -ForegroundColor Green

# Ensure archive directories exist
$archiveDirs = @(
    "archive\status-documents",
    "archive\test-scripts", 
    "archive\old-code"
)

foreach ($dir in $archiveDirs) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Host "Created: $dir" -ForegroundColor Yellow
    }
}

# Move RBAC MD files
Write-Host "`nMoving RBAC documentation files..." -ForegroundColor Cyan
Get-ChildItem -Path . -Filter "RBAC_*.md" -File -ErrorAction SilentlyContinue | ForEach-Object {
    $dest = "archive\status-documents\$($_.Name)"
    Move-Item -Path $_.FullName -Destination $dest -Force -ErrorAction SilentlyContinue
    if (Test-Path $dest) {
        Write-Host "  ✓ $($_.Name)" -ForegroundColor Green
    }
}

# Move other status MD files
Write-Host "`nMoving other status documents..." -ForegroundColor Cyan
$statusFiles = @(
    "CLOUD_RUN_VS_GKE_ANALYSIS.md",
    "DEPLOYMENT_COMPLETE.md",
    "DEPLOYMENT_STATUS.md",
    "DEVELOPER_PORTAL_CONTENT_COMPLETE.md",
    "DEVELOPER_PORTAL_PLAN.md",
    "DEVELOPER_PORTAL_SETUP_COMPLETE.md",
    "GITHUB_PAGES_SETUP.md",
    "payment-flow-refactor-plan.md",
    "REFUND_IMPLEMENTATION_PROGRESS.md",
    "REFUND_PROCESS_AUDIT.md",
    "RUN_RUNNER_SETUP.md",
    "SELF_HOSTED_RUNNER_SETUP.md",
    "setup-github-runner-guide.md",
    "WEB_VERSION_ESTIMATE.md",
    "WPF_TO_WINUI3_MIGRATION_REPORT.md"
)

foreach ($file in $statusFiles) {
    if (Test-Path $file) {
        Move-Item -Path $file -Destination "archive\status-documents\" -Force -ErrorAction SilentlyContinue
        if (-not (Test-Path $file)) {
            Write-Host "  ✓ $file" -ForegroundColor Green
        }
    }
}

# Move test scripts
Write-Host "`nMoving test scripts..." -ForegroundColor Cyan
$testScripts = @(
    "test-apis-detailed.ps1",
    "test-db-connection.ps1",
    "test-health-endpoints.ps1",
    "test-payment-flow.ps1",
    "test-rbac-apis-detailed.ps1",
    "test-rbac-apis.ps1",
    "test-rbac-diagnostics.ps1",
    "test-rbac-error-handling.ps1",
    "test-rbac-final.ps1",
    "test-rbac-verification.ps1",
    "test-usersapi-simple.ps1",
    "verify-all-apis-build.ps1",
    "run-settings-tests.ps1",
    "fix-rbac-issues.ps1",
    "get-cloudrun-logs.ps1",
    "db-interact.ps1",
    "query-db.ps1",
    "push-docs-branch.ps1"
)

foreach ($script in $testScripts) {
    if (Test-Path $script) {
        Move-Item -Path $script -Destination "archive\test-scripts\" -Force -ErrorAction SilentlyContinue
        if (-not (Test-Path $script)) {
            Write-Host "  ✓ $script" -ForegroundColor Green
        }
    }
}

# Move shell scripts
Write-Host "`nMoving shell scripts..." -ForegroundColor Cyan
if (Test-Path "run-settings-tests.sh") {
    Move-Item -Path "run-settings-tests.sh" -Destination "archive\test-scripts\" -Force -ErrorAction SilentlyContinue
    if (-not (Test-Path "run-settings-tests.sh")) {
        Write-Host "  ✓ run-settings-tests.sh" -ForegroundColor Green
    }
}

# Move old code
Write-Host "`nMoving old code files..." -ForegroundColor Cyan
if (Test-Path "Program.cs") {
    Move-Item -Path "Program.cs" -Destination "archive\old-code\" -Force -ErrorAction SilentlyContinue
    if (-not (Test-Path "Program.cs")) {
        Write-Host "  ✓ Program.cs" -ForegroundColor Green
    }
}

# Move PDF receipts
Write-Host "`nMoving PDF receipts..." -ForegroundColor Cyan
Get-ChildItem -Path . -Filter "receipt_*.pdf" -File -ErrorAction SilentlyContinue | ForEach-Object {
    Move-Item -Path $_.FullName -Destination "archive\" -Force -ErrorAction SilentlyContinue
    Write-Host "  ✓ $($_.Name)" -ForegroundColor Green
}

Write-Host "`nCleanup complete! Review the changes and commit." -ForegroundColor Green
Write-Host "Remaining files in root:" -ForegroundColor Yellow
Get-ChildItem -Path . -File | Where-Object { $_.Name -notlike ".*" } | Select-Object Name | Format-Table -AutoSize
