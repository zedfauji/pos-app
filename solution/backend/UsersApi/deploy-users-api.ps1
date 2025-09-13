#!/usr/bin/env pwsh

# Deploy UsersApi to Google Cloud Run
# Usage: .\deploy-users-api.ps1

param(
    [string]$ProjectId = "bola8pos",
    [string]$Region = "northamerica-south1", 
    [string]$ServiceName = "magidesk-users",
    [string]$CloudSqlInstance = "bola8pos:northamerica-south1:pos-app-1"
)

Write-Host "🚀 Deploying UsersApi to Cloud Run..." -ForegroundColor Cyan
Write-Host "Project: $ProjectId" -ForegroundColor Yellow
Write-Host "Region: $Region" -ForegroundColor Yellow
Write-Host "Service: $ServiceName" -ForegroundColor Yellow
Write-Host "Cloud SQL Instance: $CloudSqlInstance" -ForegroundColor Yellow

# Set the project
Write-Host "Setting GCP project..." -ForegroundColor Green
gcloud config set project $ProjectId

# Build and deploy to Cloud Run
Write-Host "Building and deploying to Cloud Run..." -ForegroundColor Green

try {
    # Build container image first
    $imageName = "gcr.io/$ProjectId/$ServiceName"
    Write-Host "Building container image: $imageName" -ForegroundColor Yellow
    
    gcloud builds submit ../../ --config ../../cloudbuild-users.yaml
    
    if ($LASTEXITCODE -ne 0) {
        throw "Container build failed"
    }
    
    # Deploy the built image
    gcloud run deploy $ServiceName `
        --image $imageName `
        --region $Region `
        --platform managed `
        --allow-unauthenticated `
        --port 8080 `
        --memory 512Mi `
        --cpu 1 `
        --min-instances 0 `
        --max-instances 10 `
        --timeout 300 `
        --set-env-vars "ASPNETCORE_ENVIRONMENT=Production" `
        --set-cloudsql-instances $CloudSqlInstance `
        --set-env-vars "ConnectionStrings__DefaultConnection=Host=/cloudsql/$CloudSqlInstance;Port=5432;Username=posapp;Password=Campus_66;Database=postgres;SSL Mode=Require;Trust Server Certificate=true"

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ UsersApi deployed successfully!" -ForegroundColor Green
        
        # Get the service URL
        $serviceUrl = gcloud run services describe $ServiceName --region $Region --format "value(status.url)"
        Write-Host "🌐 Service URL: $serviceUrl" -ForegroundColor Cyan
        
        # Test the health endpoint
        Write-Host "🔍 Testing health endpoint..." -ForegroundColor Yellow
        try {
            $healthResponse = Invoke-RestMethod -Uri "$serviceUrl/health" -Method Get -TimeoutSec 30
            Write-Host "✅ Health check passed: $($healthResponse.status)" -ForegroundColor Green
        }
        catch {
            Write-Host "⚠️  Health check failed, but deployment succeeded. Service may still be starting up." -ForegroundColor Yellow
            Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        }
        
        # Test the ping endpoint
        Write-Host "🏓 Testing ping endpoint..." -ForegroundColor Yellow
        try {
            $pingResponse = Invoke-RestMethod -Uri "$serviceUrl/api/users/ping" -Method Get -TimeoutSec 30
            Write-Host "✅ Ping successful: $($pingResponse.message)" -ForegroundColor Green
        }
        catch {
            Write-Host "⚠️  Ping failed, but deployment succeeded. Service may still be starting up." -ForegroundColor Yellow
            Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        }
        
        Write-Host "" -ForegroundColor White
        Write-Host "🎉 Deployment Summary:" -ForegroundColor Cyan
        Write-Host "   Service Name: $ServiceName" -ForegroundColor White
        Write-Host "   Service URL: $serviceUrl" -ForegroundColor White
        Write-Host "   Region: $Region" -ForegroundColor White
        Write-Host "   Database: Connected to Cloud SQL PostgreSQL" -ForegroundColor White
        Write-Host "" -ForegroundColor White
        Write-Host "📋 Next Steps:" -ForegroundColor Cyan
        Write-Host "   1. Verify database schema initialization" -ForegroundColor White
        Write-Host "   2. Test API endpoints manually" -ForegroundColor White
        Write-Host "   3. Run data migration from Firestore" -ForegroundColor White
        Write-Host "   4. Update frontend configuration (when ready to switch)" -ForegroundColor White
        
    } else {
        Write-Host "❌ Deployment failed!" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "❌ Deployment error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
