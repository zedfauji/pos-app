#!/usr/bin/env pwsh

# Migrate users from Firestore to PostgreSQL UsersApi
# Usage: .\migrate-users.ps1

param(
    [string]$UsersApiUrl = "https://magidesk-users-904541739138.northamerica-south1.run.app",
    [string]$ProjectId = "bola8pos"
)

Write-Host "🔄 Starting user migration from Firestore to PostgreSQL..." -ForegroundColor Cyan
Write-Host "Users API URL: $UsersApiUrl" -ForegroundColor Yellow
Write-Host "Project ID: $ProjectId" -ForegroundColor Yellow

# Function to get users from Firestore
function Get-FirestoreUsers {
    Write-Host "📥 Fetching users from Firestore..." -ForegroundColor Green
    
    try {
        $firestoreUsers = gcloud firestore documents list --collection=users --project=$ProjectId --format=json | ConvertFrom-Json
        
        if ($firestoreUsers -and $firestoreUsers.Count -gt 0) {
            Write-Host "✅ Found $($firestoreUsers.Count) users in Firestore" -ForegroundColor Green
            return $firestoreUsers
        } else {
            Write-Host "⚠️  No users found in Firestore" -ForegroundColor Yellow
            return @()
        }
    }
    catch {
        Write-Host "❌ Failed to fetch users from Firestore: $($_.Exception.Message)" -ForegroundColor Red
        return @()
    }
}

# Function to create user in PostgreSQL via API
function Create-PostgreSQLUser {
    param(
        [string]$Username,
        [string]$Password,
        [string]$Role,
        [bool]$IsActive = $true
    )
    
    $createRequest = @{
        Username = $Username
        Password = $Password
        Role = $Role
    } | ConvertTo-Json
    
    try {
        $response = Invoke-RestMethod -Uri "$UsersApiUrl/api/users" -Method Post -Body $createRequest -ContentType "application/json" -TimeoutSec 30
        return $response
    }
    catch {
        Write-Host "❌ Failed to create user '$Username': $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

# Function to check if user exists in PostgreSQL
function Test-PostgreSQLUser {
    param([string]$Username)
    
    try {
        $response = Invoke-RestMethod -Uri "$UsersApiUrl/api/users/username/$Username" -Method Get -TimeoutSec 30
        return $response -ne $null
    }
    catch {
        return $false
    }
}

# Main migration logic
try {
    # Test API connectivity
    Write-Host "🔍 Testing API connectivity..." -ForegroundColor Yellow
    $pingResponse = Invoke-RestMethod -Uri "$UsersApiUrl/api/users/ping" -Method Get -TimeoutSec 30
    if (-not $pingResponse) {
        throw "API ping failed"
    }
    Write-Host "✅ API is responding" -ForegroundColor Green
    
    # Get current stats
    $initialStats = Invoke-RestMethod -Uri "$UsersApiUrl/api/users/stats" -Method Get -TimeoutSec 30
    Write-Host "📊 Current PostgreSQL stats: $($initialStats.totalUsers) total users, $($initialStats.activeUsers) active" -ForegroundColor Cyan
    
    # Get Firestore users
    $firestoreUsers = Get-FirestoreUsers
    
    if ($firestoreUsers.Count -eq 0) {
        Write-Host "ℹ️  No users to migrate from Firestore" -ForegroundColor Blue
        return
    }
    
    $migratedCount = 0
    $skippedCount = 0
    $errorCount = 0
    
    foreach ($firestoreUser in $firestoreUsers) {
        try {
            # Extract user data from Firestore document
            $userData = $firestoreUser.fields
            $username = $userData.username.stringValue
            $role = if ($userData.role.stringValue) { $userData.role.stringValue } else { "employee" }
            $isActive = if ($userData.isActive.booleanValue -ne $null) { $userData.isActive.booleanValue } else { $true }
            
            Write-Host "🔄 Processing user: $username (role: $role, active: $isActive)" -ForegroundColor Yellow
            
            # Check if user already exists in PostgreSQL
            if (Test-PostgreSQLUser -Username $username) {
                Write-Host "⏭️  User '$username' already exists in PostgreSQL, skipping..." -ForegroundColor Yellow
                $skippedCount++
                continue
            }
            
            # Generate a default password (user will need to reset)
            $defaultPassword = "123456"  # Meets 6-digit requirement
            
            # Create user in PostgreSQL
            $newUser = Create-PostgreSQLUser -Username $username -Password $defaultPassword -Role $role
            
            if ($newUser) {
                Write-Host "✅ Successfully migrated user: $username" -ForegroundColor Green
                $migratedCount++
            } else {
                Write-Host "❌ Failed to migrate user: $username" -ForegroundColor Red
                $errorCount++
            }
        }
        catch {
            Write-Host "❌ Error processing user: $($_.Exception.Message)" -ForegroundColor Red
            $errorCount++
        }
    }
    
    # Get final stats
    $finalStats = Invoke-RestMethod -Uri "$UsersApiUrl/api/users/stats" -Method Get -TimeoutSec 30
    
    Write-Host "" -ForegroundColor White
    Write-Host "🎉 Migration Summary:" -ForegroundColor Cyan
    Write-Host "   Users found in Firestore: $($firestoreUsers.Count)" -ForegroundColor White
    Write-Host "   Users migrated: $migratedCount" -ForegroundColor Green
    Write-Host "   Users skipped (already exist): $skippedCount" -ForegroundColor Yellow
    Write-Host "   Errors: $errorCount" -ForegroundColor Red
    Write-Host "   Final PostgreSQL stats: $($finalStats.totalUsers) total users, $($finalStats.activeUsers) active" -ForegroundColor Cyan
    Write-Host "" -ForegroundColor White
    
    if ($errorCount -eq 0) {
        Write-Host "✅ Migration completed successfully!" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Migration completed with $errorCount errors" -ForegroundColor Yellow
    }
    
    Write-Host "📋 Next Steps:" -ForegroundColor Cyan
    Write-Host "   1. Verify migrated users can log in with default password '123456'" -ForegroundColor White
    Write-Host "   2. Inform users to change their passwords" -ForegroundColor White
    Write-Host "   3. Test the frontend with the new backend" -ForegroundColor White
    Write-Host "   4. Update frontend configuration to use new UsersApi" -ForegroundColor White
}
catch {
    Write-Host "❌ Migration failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
