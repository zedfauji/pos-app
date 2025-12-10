# Ruthless Repository Cleanup Script
# Removes stale files, unused services, dead code, and build artifacts
# Moves unsure items to /legacy-reference/ for review

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
    
    if (-not (Test-Path $Path)) {
        return
    }
    
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
    
    if (-not (Test-Path $Path)) {
        return
    }
    
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
Write-Host "║      RUTHLESS REPOSITORY CLEANUP - MAGIDESK POS          ║" -ForegroundColor Red
Write-Host "╚════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

if ($DryRun) {
    Write-Host "🔍 DRY RUN MODE - No files will be deleted or moved`n" -ForegroundColor Yellow
}

# Create legacy-reference directory for unsure items
$legacyDir = "legacy-reference"
if (-not $DryRun -and -not (Test-Path $legacyDir)) {
    New-Item -ItemType Directory -Path $legacyDir -Force | Out-Null
    Write-Action "Created $legacyDir directory for unsure items" "Success"
}

# ============================================================================
# PHASE 1: Remove Unused Services
# ============================================================================
Write-Host "`n📦 PHASE 1: Removing Unused Services" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray

# InventoryProxy has 0 frontend references - candidate for removal
$unusedServices = @(
    @{ Name = "InventoryProxy"; Path = "src\Backend\InventoryProxy"; Reason = "No frontend references, minimal usage" }
)

foreach ($service in $unusedServices) {
    if (Test-Path $service.Path) {
        Write-Action "Removing unused service: $($service.Name)" "Delete"
        Remove-Item-Safe -Path $service.Path -Reason $service.Reason
        $script:Stats.ServicesRemoved++
    }
}

# ============================================================================
# PHASE 2: Remove Stale/Garbage Files
# ============================================================================
Write-Host "`n🗑️  PHASE 2: Removing Stale/Garbage Files" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray

# Patterns to search for stale files
$stalePatterns = @(
    "*old*", "*backup*", "*temp*", "*deprecated*", "*cursor-old*",
    "*_v2*", "*_V2*", "*v2.*", "*V2.*", "*copy*", "*Copy*",
    "*.bak", "*.tmp", "*.swp", "*.swo", "*~"
)

$excludeDirs = @("node_modules", ".git", "bin", "obj", "legacy-reference")

function Get-StaleFiles {
    param([string]$RootPath)
    
    $files = @()
    Get-ChildItem -Path $RootPath -Recurse -File -ErrorAction SilentlyContinue | ForEach-Object {
        $relPath = $_.FullName.Replace((Resolve-Path $RootPath).Path + "\", "")
        $shouldExclude = $false
        
        foreach ($dir in $excludeDirs) {
            if ($relPath -like "*\$dir\*" -or $relPath -like "$dir\*") {
                $shouldExclude = $true
                break
            }
        }
        
        if (-not $shouldExclude) {
            foreach ($pattern in $stalePatterns) {
                if ($_.Name -like $pattern) {
                    $files += $_.FullName
                    break
                }
            }
        }
    }
    return $files
}

$staleFiles = Get-StaleFiles -RootPath "."
foreach ($file in $staleFiles) {
    Remove-Item-Safe -Path $file -Reason "Stale/garbage file pattern match"
}

# Remove root-level Program.cs (orphaned)
if (Test-Path "Program.cs") {
    Remove-Item-Safe -Path "Program.cs" -Reason "Orphaned root-level Program.cs"
}

# ============================================================================
# PHASE 3: Remove Build Artifacts
# ============================================================================
Write-Host "`n🏗️  PHASE 3: Removing Build Artifacts" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray

$buildArtifacts = @(
    "**/bin/**",
    "**/obj/**",
    "**/*.log",
    "**/*.binlog",
    "**/*.tmp",
    "**/TestResults/**",
    "**/publish/**",
    "**/.vs/**",
    "**/.vscode/**"
)

foreach ($pattern in $buildArtifacts) {
    Get-ChildItem -Path . -Include $pattern -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
        $exclude = $false
        foreach ($excludeDir in $excludeDirs) {
            if ($_.FullName -like "*\$excludeDir\*") {
                $exclude = $true
                break
            }
        }
        if (-not $exclude) {
            Remove-Item-Safe -Path $_.FullName -Reason "Build artifact"
        }
    }
}

# ============================================================================
# PHASE 4: Remove Duplicate Files
# ============================================================================
Write-Host "`n📋 PHASE 4: Removing Duplicate Files" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray

# Find duplicate DTOs/ViewModels
$potentialDuplicates = @{
    "ReopenSessionResult" = @(
        "src\Frontend\frontend\Shared\DTOs\Tables\ReopenSessionResult.cs",  # Already deleted, but check if exists
        "src\Shared\shared\DTOs\Tables\SessionDtos.cs"  # Keep this one
    )
}

foreach ($key in $potentialDuplicates.Keys) {
    $duplicates = $potentialDuplicates[$key]
    $canonical = $duplicates[-1]  # Last one is canonical
    for ($i = 0; $i -lt $duplicates.Length - 1; $i++) {
        if (Test-Path $duplicates[$i]) {
            Remove-Item-Safe -Path $duplicates[$i] -Reason "Duplicate of canonical file: $canonical"
        }
    }
}

# ============================================================================
# PHASE 5: Move Unsure Items to Legacy
# ============================================================================
Write-Host "`n📦 PHASE 5: Moving Unsure Items to Legacy" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray

$unsureItems = @(
    @{ Path = "solution\docs"; Reason = "Documentation archive - review needed" }
)

foreach ($item in $unsureItems) {
    if (Test-Path $item.Path) {
        $destPath = Join-Path $legacyDir (Split-Path -Leaf $item.Path)
        Move-Item-Safe -Path $item.Path -Destination $destPath -Reason $item.Reason
    }
}

# ============================================================================
# PHASE 6: Clean Solution File
# ============================================================================
Write-Host "`n🔧 PHASE 6: Cleaning Solution File" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray

if (Test-Path "solution\MagiDesk.sln") {
    if (-not $DryRun) {
        # Remove InventoryProxy from solution
        $slnPath = "solution\MagiDesk.sln"
        $slnContent = Get-Content $slnPath -Raw
        
        if ($slnContent -match "InventoryProxy") {
            Write-Action "Removing InventoryProxy from solution file" "Delete"
            # Use dotnet sln remove
            $inventoryProxyProj = "src\Backend\InventoryProxy\InventoryProxy.csproj"
            if (Test-Path $inventoryProxyProj) {
                dotnet sln $slnPath remove $inventoryProxyProj 2>&1 | Out-Null
            }
        }
    } else {
        Write-Action "[DRY RUN] Would clean solution file" "Delete"
    }
}

# ============================================================================
# PHASE 7: Remove Dead Tests
# ============================================================================
Write-Host "`n🧪 PHASE 7: Cleaning Dead Tests" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray

# Remove test files referencing deleted services
$testFiles = Get-ChildItem -Path "tests" -Recurse -Include *.cs -ErrorAction SilentlyContinue
foreach ($testFile in $testFiles) {
    $content = Get-Content $testFile.FullName -Raw -ErrorAction SilentlyContinue
    if ($content) {
        if ($content -match "InventoryProxy") {
            Remove-Item-Safe -Path $testFile.FullName -Reason "References deleted InventoryProxy service"
        }
    }
}

# ============================================================================
# PHASE 8: Clean Dockerfiles
# ============================================================================
Write-Host "`n🐳 PHASE 8: Cleaning Dockerfiles" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray

# Remove InventoryProxy Dockerfile
if (Test-Path "src\Backend\InventoryProxy\Dockerfile") {
    Remove-Item-Safe -Path "src\Backend\InventoryProxy\Dockerfile" -Reason "Unused service Dockerfile"
}

# ============================================================================
# PHASE 9: Update .gitignore
# ============================================================================
Write-Host "`n📝 PHASE 9: Updating .gitignore" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray

if (-not $DryRun) {
    $gitignorePath = ".gitignore"
    if (Test-Path $gitignorePath) {
        $gitignoreContent = Get-Content $gitignorePath -Raw
        
        $additions = @(
            "# Legacy reference folder",
            "legacy-reference/",
            "# Build artifacts",
            "**/publish/",
            "**/TestResults/"
        )
        
        $alreadyHas = $false
        foreach ($line in $additions) {
            if ($gitignoreContent -notmatch [regex]::Escape($line)) {
                Add-Content -Path $gitignorePath -Value "`n$line"
                Write-Action "Added to .gitignore: $line" "Success"
            }
        }
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
Write-Host "   Services Removed: $($script:Stats.ServicesRemoved)" -ForegroundColor White

Write-Host "`n✅ Cleanup Complete!" -ForegroundColor Green

if ($DryRun) {
    Write-Host "`n⚠️  This was a DRY RUN. No files were actually modified." -ForegroundColor Yellow
    Write-Host "   Run without -DryRun to perform actual cleanup." -ForegroundColor Yellow
} else {
    Write-Host "`n📋 Next Steps:" -ForegroundColor Cyan
    Write-Host "   1. Review moved items in: legacy-reference/" -ForegroundColor White
    Write-Host "   2. Run: dotnet clean solution/MagiDesk.sln" -ForegroundColor White
    Write-Host "   3. Run: dotnet build solution/MagiDesk.sln" -ForegroundColor White
    Write-Host "   4. Fix any build errors" -ForegroundColor White
    Write-Host "   5. Run tests: dotnet test solution/MagiDesk.sln" -ForegroundColor White
}

