# Ruthless Repository Cleanup V2 - Complete cleanup for DigitalOcean migration
# Removes ALL garbage, dead code, debug statements, and unused dependencies

param(
    [switch]$DryRun,
    [switch]$SkipBuildVerification
)

$ErrorActionPreference = "Stop"

$script:DeletedItems = @()
$script:MovedItems = @()
$script:Stats = @{
    FilesDeleted = 0
    FoldersDeleted = 0
    FilesMoved = 0
    ServicesRemoved = 0
    DebugStatementsRemoved = 0
}

function Write-Action {
    param([string]$Message, [string]$Type = "Info")
    $color = switch ($Type) {
        "Delete" { "Red" }
        "Move" { "Yellow" }
        "Success" { "Green" }
        default { "Cyan" }
    }
    Write-Host $Message -ForegroundColor $color
}

function Remove-Item-Safe {
    param([string]$Path, [string]$Reason)
    
    if (-not (Test-Path $Path)) { return }
    
    if ($DryRun) {
        Write-Action "[DRY RUN] Would delete: $Path ($Reason)" "Delete"
        $script:Stats.FilesDeleted++
    } else {
        try {
            Remove-Item -Path $Path -Recurse -Force -ErrorAction Stop
            Write-Action "✓ Deleted: $Path ($Reason)" "Delete"
            $script:DeletedItems += @{ Path = $Path; Reason = $Reason }
            $script:Stats.FilesDeleted++
        } catch {
            Write-Action "⚠ Failed to delete $Path`: $_" "Move"
        }
    }
}

function Move-Item-Safe {
    param([string]$Path, [string]$Destination, [string]$Reason)
    
    if (-not (Test-Path $Path)) { return }
    
    $destDir = Split-Path -Parent $Destination -ErrorAction SilentlyContinue
    if ($destDir -and -not (Test-Path $destDir)) {
        New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    }
    
    if ($DryRun) {
        Write-Action "[DRY RUN] Would move: $Path → $Destination ($Reason)" "Move"
        $script:Stats.FilesMoved++
    } else {
        try {
            Move-Item -Path $Path -Destination $Destination -Force -ErrorAction Stop
            Write-Action "✓ Moved: $Path → $Destination ($Reason)" "Move"
            $script:MovedItems += @{ Path = $Path; Destination = $Destination; Reason = $Reason }
            $script:Stats.FilesMoved++
        } catch {
            Write-Action "⚠ Failed to move $Path`: $_" "Move"
        }
    }
}

Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   RUTHLESS CLEANUP V2 - DIGITALOCEAN MIGRATION PREP      ║" -ForegroundColor Red
Write-Host "╚════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

if ($DryRun) {
    Write-Host "🔍 DRY RUN MODE - No files will be deleted or moved`n" -ForegroundColor Yellow
}

$legacyDir = "legacy-reference"
if (-not $DryRun -and -not (Test-Path $legacyDir)) {
    New-Item -ItemType Directory -Path $legacyDir -Force | Out-Null
}

# ============================================================================
# PHASE 1: Remove Debug/MessageBox Statements
# ============================================================================
Write-Host "`n🐛 PHASE 1: Removing Debug Statements" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray

$debugPatterns = @(
    'MessageBox\.Show',
    'Debug\.Write',
    'Debug\.WriteLine',
    'Console\.Write',
    'Console\.WriteLine',
    '#if DEBUG',
    'System\.Diagnostics\.Debug'
)

Get-ChildItem -Path "src" -Recurse -Include *.cs -ErrorAction SilentlyContinue | ForEach-Object {
    $file = $_.FullName
    $content = Get-Content $file -Raw -ErrorAction SilentlyContinue
    
    if ($content) {
        $modified = $false
        $newContent = $content
        
        foreach ($pattern in $debugPatterns) {
            if ($newContent -match $pattern) {
                # Remove lines with debug statements (simple approach)
                $lines = $newContent -split "`n"
                $filtered = $lines | Where-Object { $_ -notmatch $pattern }
                $newContent = $filtered -join "`n"
                $modified = $true
            }
        }
        
        if ($modified -and -not $DryRun) {
            Set-Content -Path $file -Value $newContent -NoNewline -ErrorAction SilentlyContinue
            $script:Stats.DebugStatementsRemoved++
        } elseif ($modified) {
            Write-Action "[DRY RUN] Would clean debug statements in: $file" "Delete"
        }
    }
}

# ============================================================================
# PHASE 2: Remove Root-Level Clutter
# ============================================================================
Write-Host "`n🗑️  PHASE 2: Removing Root-Level Clutter" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray

$rootClutter = @(
    "create-frontend-installer.ps1",
    "db-interact.ps1",
    "query-db.ps1",
    "start-mcp-server.ps1",
    "test-mcp-connection.ps1",
    "test-payment-flow.ps1",
    "run-settings-tests.ps1",
    "run-settings-tests.sh",
    "receipt_*.pdf",
    "payment-flow-refactor-plan.md",
    "WEB_VERSION_ESTIMATE.md",
    "WPF_TO_WINUI3_MIGRATION_REPORT.md",
    "ENTERPRISE_READINESS_AUDIT.json",
    "ENTERPRISE_READINESS_AUDIT.md",
    "FINOPS_COST_COMPARISON.csv",
    "github-branch-protection*.json",
    "Order-Tracking-By-GPT.code-workspace",
    "postgres-mcp",
    "MagiDesk-Frontend-Installer"
)

foreach ($item in $rootClutter) {
    $path = Get-Item -Path $item -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($path) {
        Remove-Item-Safe -Path $path.FullName -Reason "Root-level clutter"
    }
}

# Move docs to legacy if large
if (Test-Path "docs" -PathType Container) {
    $docSize = (Get-ChildItem -Path "docs" -Recurse -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum / 1MB
    if ($docSize -gt 50) {
        Move-Item-Safe -Path "docs" -Destination "legacy-reference/docs" -Reason "Large documentation archive ($([math]::Round($docSize, 2)) MB)"
    }
}

# ============================================================================
# PHASE 3: Remove Test/Portable Builds
# ============================================================================
Write-Host "`n📦 PHASE 3: Removing Test/Portable Builds" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray

$testBuilds = @(
    "solution/TestWinUI3",
    "solution/MagiDesk-Frontend-Portable",
    "solution/MagiDesk-Frontend-Installer.zip",
    "solution/TestResults"
)

foreach ($item in $testBuilds) {
    if (Test-Path $item) {
        Remove-Item-Safe -Path $item -Reason "Test/portable build artifact"
    }
}

# ============================================================================
# PHASE 4: Clean Solution Folder
# ============================================================================
Write-Host "`n🔧 PHASE 4: Cleaning Solution Folder" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray

$solutionClutter = @(
    "solution/backend",  # Old structure
    "solution/test-billing-integration.ps1",
    "solution/migrate-users.ps1",
    "solution/testEnvironments.json",
    "solution/Dockerfile.UsersApi",
    "solution/HIERARCHICAL_SETTINGS_IMPLEMENTATION.md"
)

foreach ($item in $solutionClutter) {
    if (Test-Path $item) {
        Remove-Item-Safe -Path $item -Reason "Solution folder cleanup"
    }
}

# Move solution/docs if still exists
if (Test-Path "solution/docs") {
    Move-Item-Safe -Path "solution/docs" -Destination "legacy-reference/solution-docs" -Reason "Solution documentation archive"
}

# ============================================================================
# PHASE 5: Remove Unused Scripts
# ============================================================================
Write-Host "`n📜 PHASE 5: Cleaning Scripts" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray

# Keep only essential scripts
$keepScripts = @(
    "scripts/cleanup/",
    "scripts/db/create-postgres-dump.ps1",
    "scripts/db/verify-dump.ps1"
)

# Move old deploy scripts to legacy
if (Test-Path "scripts/deploy") {
    Move-Item-Safe -Path "scripts/deploy" -Destination "legacy-reference/scripts-deploy" -Reason "Old GCP deployment scripts (migrating to DO)"
}

# ============================================================================
# PHASE 6: Fix Project References
# ============================================================================
Write-Host "`n🔗 PHASE 6: Fixing Project References" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray

# Fix InventoryApi and SettingsApi to use correct Shared reference
$servicesToFix = @("InventoryApi", "SettingsApi", "UsersApi")

foreach ($service in $servicesToFix) {
    $csproj = "src/Backend/$service/$service.csproj"
    if (Test-Path $csproj) {
        if (-not $DryRun) {
            $content = Get-Content $csproj -Raw
            # Replace MSBuildThisFileDirectory with relative path
            $content = $content -replace '\$\(MSBuildThisFileDirectory\)\.\.\\\.\.\\\.\.\\Shared\\shared\\', '../../Shared/shared/'
            Set-Content -Path $csproj -Value $content -NoNewline
            Write-Action "✓ Fixed project reference in $service" "Success"
        } else {
            Write-Action "[DRY RUN] Would fix project reference in $service" "Delete"
        }
    }
}

# ============================================================================
# PHASE 7: Remove All Build Artifacts
# ============================================================================
Write-Host "`n🏗️  PHASE 7: Removing Build Artifacts" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray

Get-ChildItem -Path . -Include bin,obj,TestResults,publish -Recurse -Directory -ErrorAction SilentlyContinue | ForEach-Object {
    Remove-Item-Safe -Path $_.FullName -Reason "Build artifact"
}

Get-ChildItem -Path . -Include *.log,*.binlog,*.tmp,*.cache -Recurse -File -ErrorAction SilentlyContinue | ForEach-Object {
    $exclude = $false
    foreach ($dir in @(".git", "node_modules", "legacy-reference")) {
        if ($_.FullName -like "*\$dir\*") { $exclude = $true; break }
    }
    if (-not $exclude) {
        Remove-Item-Safe -Path $_.FullName -Reason "Build artifact"
    }
}

# ============================================================================
# SUMMARY
# ============================================================================
Write-Host "`n╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                    CLEANUP SUMMARY                         ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

Write-Host "📊 Statistics:" -ForegroundColor Yellow
Write-Host "   Files Deleted: $($script:Stats.FilesDeleted)" -ForegroundColor White
Write-Host "   Folders Deleted: $($script:Stats.FoldersDeleted)" -ForegroundColor White
Write-Host "   Files Moved: $($script:Stats.FilesMoved)" -ForegroundColor White
Write-Host "   Debug Statements Removed: $($script:Stats.DebugStatementsRemoved)" -ForegroundColor White

Write-Host "`n✅ Cleanup Complete!" -ForegroundColor Green

