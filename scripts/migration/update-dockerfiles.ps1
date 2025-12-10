# Update Dockerfiles to match new directory structure
# Changes: backend/ → src/Backend/, shared/ → src/Shared/shared/

$ErrorActionPreference = "Stop"

Write-Host "=== Updating Dockerfiles for New Directory Structure ===" -ForegroundColor Cyan
Write-Host ""

$services = @(
    "TablesApi", "OrderApi", "PaymentApi", "MenuApi",
    "CustomerApi", "DiscountApi", "InventoryApi",
    "SettingsApi", "UsersApi"
)

foreach ($service in $services) {
    $dockerfilePath = "src\Backend\$service\Dockerfile"
    
    if (-not (Test-Path $dockerfilePath)) {
        Write-Host "⚠ Skipping $service - Dockerfile not found" -ForegroundColor Yellow
        continue
    }
    
    Write-Host "Updating $service..." -ForegroundColor Cyan
    
    $content = Get-Content $dockerfilePath -Raw
    
    # Replace old paths with new paths
    $content = $content -replace 'COPY backend/', 'COPY src/Backend/'
    $content = $content -replace 'COPY shared/', 'COPY src/Shared/shared/'
    $content = $content -replace 'WORKDIR /src/backend/', 'WORKDIR /src/src/Backend/'
    $content = $content -replace 'backend/TablesApi', 'src/Backend/TablesApi'
    $content = $content -replace 'backend/OrderApi', 'src/Backend/OrderApi'
    $content = $content -replace 'backend/PaymentApi', 'src/Backend/PaymentApi'
    $content = $content -replace 'backend/MenuApi', 'src/Backend/MenuApi'
    $content = $content -replace 'backend/CustomerApi', 'src/Backend/CustomerApi'
    $content = $content -replace 'backend/DiscountApi', 'src/Backend/DiscountApi'
    $content = $content -replace 'backend/InventoryApi', 'src/Backend/InventoryApi'
    $content = $content -replace 'backend/SettingsApi', 'src/Backend/SettingsApi'
    $content = $content -replace 'backend/UsersApi', 'src/Backend/UsersApi'
    
    # Fix double src/src if it occurred
    $content = $content -replace 'src/src/', 'src/'
    
    Set-Content -Path $dockerfilePath -Value $content -NoNewline
    Write-Host "  ✓ Updated $service" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== Dockerfile Update Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Note: Review the Dockerfiles to ensure paths are correct." -ForegroundColor Yellow
Write-Host "Some Dockerfiles may have different structures." -ForegroundColor Yellow

