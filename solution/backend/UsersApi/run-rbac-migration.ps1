# =====================================================
# RBAC Migration Script
# =====================================================
# This script runs the RBAC migration SQL against
# the Cloud SQL PostgreSQL database
# =====================================================

param(
    [string]$ProjectId = "bola8pos",
    [string]$Region = "northamerica-south1",
    [string]$InstanceId = "pos-app-1",
    [string]$Database = "postgres",
    [string]$Username = "posapp"
)

Write-Host "🚀 Running RBAC Migration Script..." -ForegroundColor Green
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

# Check if the migration file exists
$MigrationFile = "rbac-migration.sql"
if (-not (Test-Path $MigrationFile)) {
    Write-Host "❌ Migration file not found: $MigrationFile" -ForegroundColor Red
    exit 1
}

Write-Host "📄 Found migration file: $MigrationFile" -ForegroundColor Green

# Run the migration
Write-Host "🔄 Running RBAC migration..." -ForegroundColor Yellow
Write-Host "This will create/update the RBAC schema and system roles..." -ForegroundColor Yellow

try {
    # Execute the migration SQL
    gcloud sql connect $InstanceId --user=$Username --database=$Database --region=$Region --file=$MigrationFile
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ RBAC migration completed successfully!" -ForegroundColor Green
    } else {
        Write-Host "❌ Migration failed with exit code: $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ Error running migration: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Verify the migration
Write-Host "🔍 Verifying migration..." -ForegroundColor Yellow

$VerificationQueries = @"
-- Check system roles
SELECT 'System Roles' as check_type, COUNT(*) as count FROM users.roles WHERE is_system_role = true AND is_deleted = false;

-- Check total permissions
SELECT 'Total Permissions' as check_type, COUNT(DISTINCT permission) as count FROM users.role_permissions;

-- Check role permissions
SELECT 
    r.name as role_name,
    COUNT(rp.permission) as permission_count
FROM users.roles r 
LEFT JOIN users.role_permissions rp ON r.role_id = rp.role_id 
WHERE r.is_deleted = false 
GROUP BY r.role_id, r.name 
ORDER BY r.is_system_role DESC, r.name;
"@

# Save verification queries to a temporary file
$TempFile = "temp-verification.sql"
$VerificationQueries | Out-File -FilePath $TempFile -Encoding UTF8

try {
    Write-Host "Running verification queries..." -ForegroundColor Yellow
    gcloud sql connect $InstanceId --user=$Username --database=$Database --region=$Region --file=$TempFile
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Verification completed!" -ForegroundColor Green
    } else {
        Write-Host "⚠️ Verification queries failed, but migration may have succeeded" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠️ Verification failed: $($_.Exception.Message)" -ForegroundColor Yellow
} finally {
    # Clean up temporary file
    if (Test-Path $TempFile) {
        Remove-Item $TempFile
    }
}

Write-Host "🎉 RBAC Migration Process Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 What was created:" -ForegroundColor Cyan
Write-Host "   ✅ users.roles table" -ForegroundColor Green
Write-Host "   ✅ users.role_permissions table" -ForegroundColor Green
Write-Host "   ✅ users.role_inheritance table" -ForegroundColor Green
Write-Host "   ✅ users.users table (updated)" -ForegroundColor Green
Write-Host "   ✅ 6 System roles with permissions" -ForegroundColor Green
Write-Host "   ✅ Default admin user" -ForegroundColor Green
Write-Host ""
Write-Host "🔗 Next Steps:" -ForegroundColor Cyan
Write-Host "   1. Restart the Users API service" -ForegroundColor White
Write-Host "   2. Test the RBAC endpoints" -ForegroundColor White
Write-Host "   3. Update frontend to use new RBAC system" -ForegroundColor White
Write-Host ""
Write-Host "🌐 Service URL: https://magidesk-users-23sbzjsxaq-pv.a.run.app" -ForegroundColor Cyan
