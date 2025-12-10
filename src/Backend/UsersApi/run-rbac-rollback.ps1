# =====================================================
# RBAC Rollback Script
# =====================================================
# This script removes the RBAC system from the database
# WARNING: This will delete all RBAC data!
# =====================================================

param(
    [string]$ProjectId = "bola8pos",
    [string]$Region = "northamerica-south1",
    [string]$InstanceId = "pos-app-1",
    [string]$Database = "postgres",
    [string]$Username = "posapp",
    [switch]$Force
)

Write-Host "⚠️ RBAC ROLLBACK SCRIPT" -ForegroundColor Red
Write-Host "This will remove the entire RBAC system from the database!" -ForegroundColor Red
Write-Host ""

if (-not $Force) {
    $confirmation = Read-Host "Are you sure you want to proceed? Type 'YES' to continue"
    if ($confirmation -ne "YES") {
        Write-Host "❌ Rollback cancelled by user" -ForegroundColor Yellow
        exit 0
    }
}

Write-Host "🚀 Running RBAC Rollback Script..." -ForegroundColor Red
Write-Host "Project: $ProjectId" -ForegroundColor Cyan
Write-Host "Region: $Region" -ForegroundColor Cyan
Write-Host "Instance: $InstanceId" -ForegroundColor Cyan
Write-Host "Database: $Database" -ForegroundColor Cyan

# Set the GCP project
Write-Host "Setting GCP project..." -ForegroundColor Yellow
gcloud config set project $ProjectId

# Get the Cloud SQL instance connection name
$ConnectionName = "$ProjectId`:$Region`:$InstanceId"

Write-Host "Connection Name: $ConnectionName" -ForegroundColor Cyan

# Check if the rollback file exists
$RollbackFile = "rbac-rollback.sql"
if (-not (Test-Path $RollbackFile)) {
    Write-Host "❌ Rollback file not found: $RollbackFile" -ForegroundColor Red
    exit 1
}

Write-Host "📄 Found rollback file: $RollbackFile" -ForegroundColor Green

# Run the rollback
Write-Host "🔄 Running RBAC rollback..." -ForegroundColor Red
Write-Host "This will remove all RBAC tables and data..." -ForegroundColor Red

try {
    # Execute the rollback SQL
    gcloud sql connect $InstanceId --user=$Username --database=$Database --region=$Region --file=$RollbackFile
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ RBAC rollback completed successfully!" -ForegroundColor Green
    } else {
        Write-Host "❌ Rollback failed with exit code: $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ Error running rollback: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Verify the rollback
Write-Host "🔍 Verifying rollback..." -ForegroundColor Yellow

$VerificationQuery = @"
-- Check if RBAC tables still exist
SELECT 
    schemaname,
    tablename,
    tableowner
FROM pg_tables 
WHERE schemaname = 'users' 
ORDER BY tablename;
"@

# Save verification query to a temporary file
$TempFile = "temp-rollback-verification.sql"
$VerificationQuery | Out-File -FilePath $TempFile -Encoding UTF8

try {
    Write-Host "Running verification query..." -ForegroundColor Yellow
    gcloud sql connect $InstanceId --user=$Username --database=$Database --region=$Region --file=$TempFile
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Rollback verification completed!" -ForegroundColor Green
    } else {
        Write-Host "⚠️ Verification queries failed, but rollback may have succeeded" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠️ Verification failed: $($_.Exception.Message)" -ForegroundColor Yellow
} finally {
    # Clean up temporary file
    if (Test-Path $TempFile) {
        Remove-Item $TempFile
    }
}

Write-Host "🎉 RBAC Rollback Process Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 What was removed:" -ForegroundColor Cyan
Write-Host "   ❌ users.roles table" -ForegroundColor Red
Write-Host "   ❌ users.role_permissions table" -ForegroundColor Red
Write-Host "   ❌ users.role_inheritance table" -ForegroundColor Red
Write-Host "   ❌ All RBAC data" -ForegroundColor Red
Write-Host ""
Write-Host "🔗 Next Steps:" -ForegroundColor Cyan
Write-Host "   1. Restart the Users API service" -ForegroundColor White
Write-Host "   2. The system will revert to basic user management" -ForegroundColor White
Write-Host "   3. Run migration script again if you want to restore RBAC" -ForegroundColor White
Write-Host ""
Write-Host "🌐 Service URL: https://magidesk-users-23sbzjsxaq-pv.a.run.app" -ForegroundColor Cyan
