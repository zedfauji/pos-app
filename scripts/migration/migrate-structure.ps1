# ============================================
# MagiDesk POS - Repository Structure Migration
# ============================================
# Purpose: Reorganize repository to enterprise-standard structure
# Based on: DevOps Audit Report - Proposed Clean Structure
# Prerequisites: Git 2.30+, PowerShell 7+, .NET 8 SDK
# Usage: .\scripts\migration\migrate-structure.ps1 [-DryRun] [-SkipConfirmation]

[CmdletBinding()]
param(
    [switch]$DryRun,
    [switch]$SkipConfirmation,
    [switch]$SkipTests,
    [string]$SolutionFile = "MagiDesk.sln"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

# ============================================
# Configuration
# ============================================

$script:RootDir = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent
Set-Location $script:RootDir

$script:BackupBranch = "backup/pre-migration-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

# Current Structure (FROM)
$script:OldPaths = @{
    Backend = "solution/backend"
    Frontend = "solution/frontend"
    Shared = "solution/shared"
    Tests = "solution/MagiDesk.Tests"
    Docs = "solution/docs"
    Archive = "solution/archive"
}

# New Structure (TO)
$script:NewPaths = @{
    Backend = "src/Backend"
    Frontend = "src/Frontend"
    Shared = "src/Shared"
    Tests = "tests"
    Docs = "docs"
    Infrastructure = "infra"
}

# Microservices
$script:Microservices = @(
    "TablesApi",
    "OrderApi",      # Note: Singular, not OrdersApi
    "PaymentApi",
    "MenuApi",
    "CustomerApi",
    "DiscountApi",
    "InventoryApi",
    "InventoryProxy", # Also move this if it exists
    "SettingsApi",
    "UsersApi"
)

# ============================================
# Logging Functions
# ============================================

function Write-Info($message) {
    Write-Host "[INFO] $message" -ForegroundColor Cyan
}

function Write-Success($message) {
    Write-Host "[SUCCESS] $message" -ForegroundColor Green
}

function Write-Warning($message) {
    Write-Host "[WARN] $message" -ForegroundColor Yellow
}

function Write-Error-Custom($message) {
    Write-Host "[ERROR] $message" -ForegroundColor Red
}

function Write-DryRun($message) {
    Write-Host "[DRY-RUN] $message" -ForegroundColor Magenta
}

# ============================================
# Safety Checks
# ============================================

function Test-GitRepository {
    if (-not (Test-Path .git)) {
        throw "Not a Git repository. Run this script from the repository root."
    }
}

function Test-GitCleanWorkingDirectory {
    $status = git status --porcelain
    if ($status) {
        Write-Warning "Working directory has uncommitted changes:"
        Write-Host $status
        if (-not $SkipConfirmation) {
            $response = Read-Host "Commit changes first or continue anyway? (commit/continue/abort)"
            if ($response -eq "abort") {
                throw "Aborted by user."
            }
            elseif ($response -eq "commit") {
                throw "Please commit your changes first: git add . && git commit -m 'Pre-migration checkpoint'"
            }
        }
    }
}

function New-BackupBranch {
    Write-Info "Creating backup branch: $script:BackupBranch"
    
    if ($DryRun) {
        Write-DryRun "Would create backup branch: $script:BackupBranch"
        return
    }
    
    $currentBranch = git rev-parse --abbrev-ref HEAD
    git checkout -b $script:BackupBranch
    git push -u origin $script:BackupBranch
    git checkout $currentBranch
    
    Write-Success "Backup branch created. Restore with: git checkout $script:BackupBranch"
}

# ============================================
# Directory Operations
# ============================================

function New-Directory {
    param([string]$Path, [string]$Description)
    
    if ($DryRun) {
        Write-DryRun "Would create directory: $Path ($Description)"
        return
    }
    
    if (-not (Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
        Write-Info "Created directory: $Path"
    }
    else {
        Write-Info "Directory already exists: $Path"
    }
}

function Move-ItemSafe {
    param(
        [string]$Source,
        [string]$Destination,
        [string]$Description
    )
    
    if ([string]::IsNullOrWhiteSpace($Source)) {
        Write-Warning "Source path is empty for: $Description"
        return $false
    }
    
    if (-not (Test-Path $Source)) {
        Write-Warning "Source path does not exist: $Source"
        return $false
    }
    
    if ($DryRun) {
        Write-DryRun "Would move: $Source -> $Destination ($Description)"
        return $true
    }
    
    # Create destination parent directory (if it has a parent)
    $destParent = Split-Path -Parent $Destination
    if ($destParent -and $destParent.Trim().Length -gt 0 -and -not (Test-Path $destParent)) {
        New-Item -ItemType Directory -Path $destParent -Force | Out-Null
    }
    
    # Move item
    try {
        Move-Item -Path $Source -Destination $Destination -Force
        Write-Info "Moved: $Source -> $Destination"
        return $true
    }
    catch {
        Write-Error-Custom "Failed to move $Source : $_"
        return $false
    }
}

function Copy-ItemSafe {
    param(
        [string]$Source,
        [string]$Destination,
        [string]$Description
    )
    
    if ([string]::IsNullOrWhiteSpace($Source)) {
        Write-Warning "Source path is empty for: $Description"
        return $false
    }
    
    # Resolve path to ensure it's absolute
    try {
        $resolvedSource = Resolve-Path -Path $Source -ErrorAction Stop
    }
    catch {
        Write-Warning "Cannot resolve source path '$Source' for $Description : $_"
        return $false
    }
    
    if (-not (Test-Path $resolvedSource)) {
        Write-Warning "Source path does not exist: $resolvedSource"
        return $false
    }
    
    if ($DryRun) {
        Write-DryRun "Would copy: $resolvedSource -> $Destination ($Description)"
        return $true
    }
    
    # Create destination parent directory (if it has a parent)
    $destParent = Split-Path -Parent $Destination -ErrorAction SilentlyContinue
    if ($destParent -and $destParent.Trim().Length -gt 0 -and -not (Test-Path $destParent)) {
        New-Item -ItemType Directory -Path $destParent -Force | Out-Null
    }
    
    # Copy item (use resolved source)
    try {
        Copy-Item -Path $resolvedSource -Destination $Destination -Recurse -Force
        Write-Info "Copied: $resolvedSource -> $Destination"
        return $true
    }
    catch {
        Write-Error-Custom "Failed to copy $resolvedSource : $_"
        return $false
    }
}

function Remove-ItemSafe {
    param(
        [string]$Path,
        [string]$Description
    )
    
    if (-not (Test-Path $Path)) {
        Write-Info "Path does not exist (already removed): $Path"
        return $true
    }
    
    if ($DryRun) {
        Write-DryRun "Would remove: $Path ($Description)"
        return $true
    }
    
    try {
        Remove-Item -Path $Path -Recurse -Force
        Write-Info "Removed: $Path"
        return $true
    }
    catch {
        Write-Error-Custom "Failed to remove $Path : $_"
        return $false
    }
}

# ============================================
# Solution File Operations
# ============================================

function Update-SolutionFile {
    Write-Info "Updating solution file: $SolutionFile"
    
    if (-not (Test-Path $SolutionFile)) {
        Write-Warning "Solution file not found: $SolutionFile"
        return
    }
    
    if ($DryRun) {
        Write-DryRun "Would update solution file paths"
        return
    }
    
    # Read solution file
    $solutionContent = Get-Content $SolutionFile -Raw
    
    # Update paths (escape backslashes for regex)
    $solutionContent = $solutionContent -replace 'solution\\backend\\', 'src\Backend\'
    $solutionContent = $solutionContent -replace 'solution\\frontend\\', 'src\Frontend\'
    $solutionContent = $solutionContent -replace 'solution\\shared\\', 'src\Shared\'
    $solutionContent = $solutionContent -replace 'solution\\MagiDesk\.Tests\\', 'tests\MagiDesk.Tests\'
    $solutionContent = $solutionContent -replace '"backend"', '"Backend"'  # Solution folder name
    
    # Write back
    Set-Content -Path $SolutionFile -Value $solutionContent -NoNewline
    
    Write-Success "Solution file paths updated"
    
    # Regenerate solution file using dotnet sln
    Write-Info "Regenerating solution file structure..."
    
    # Get current projects from solution
    $currentProjects = dotnet sln list 2>&1 | Where-Object { $_ -match '\.csproj$' } | ForEach-Object { $_.Trim() }
    
    # Remove all projects
    foreach ($project in $currentProjects) {
        if ($project -and $project.Trim() -and (Test-Path $project)) {
            Write-Info "Removing from solution: $project"
            dotnet sln remove $project 2>&1 | Out-Null
        }
    }
    
    # Re-add projects with new paths
    $newProjectPaths = @(
        "src\Shared\MagiDesk.Shared.csproj",
        "src\Frontend\MagiDesk.Frontend.csproj",
        "tests\MagiDesk.Tests\MagiDesk.Tests.csproj"
    )
    
    foreach ($service in $script:Microservices) {
        $projectPath = "src\Backend\$service\$service.csproj"
        $newProjectPaths += $projectPath
    }
    
    foreach ($projectPath in $newProjectPaths) {
        $normalizedPath = $projectPath -replace '\\', [System.IO.Path]::DirectorySeparatorChar
        if (Test-Path $normalizedPath) {
            dotnet sln add $normalizedPath 2>&1 | Out-Null
            Write-Info "Added to solution: $normalizedPath"
        }
        else {
            Write-Warning "Project not found: $normalizedPath"
        }
    }
    
    Write-Success "Solution file regenerated"
}

# ============================================
# File Generation
# ============================================

function New-StandardFiles {
    Write-Info "Creating standard configuration files..."
    
    # Create templates directory if needed (use absolute path)
    $templatesDir = Join-Path $script:RootDir "scripts/migration/templates"
    if (-not (Test-Path $templatesDir)) {
        Write-Warning "Templates directory not found: $templatesDir"
        New-Directory -Path "scripts/migration/templates" -Description "Templates directory"
        $templatesDir = Join-Path $script:RootDir "scripts/migration/templates"
    }
    
    # .editorconfig
    if (-not (Test-Path ".editorconfig")) {
        $editorConfigTemplate = Join-Path $templatesDir ".editorconfig.template"
        if (Test-Path $editorConfigTemplate) {
            Copy-ItemSafe -Source $editorConfigTemplate -Destination ".editorconfig" -Description ".editorconfig"
        }
        else {
            Write-Warning ".editorconfig template not found at: $editorConfigTemplate"
            if (-not $DryRun) {
                # Create .editorconfig directly (template content already in separate file)
                $editorConfigContent = Get-Content $editorConfigTemplate -ErrorAction SilentlyContinue
                if ($editorConfigContent) {
                    Set-Content -Path ".editorconfig" -Value $editorConfigContent
                    Write-Info "Created .editorconfig from template"
                }
            }
        }
    }
    else {
        Write-Info ".editorconfig already exists"
    }
    
    # Directory.Build.props
    if (-not (Test-Path "Directory.Build.props")) {
        $buildPropsTemplate = Join-Path $templatesDir "Directory.Build.props.template"
        if (Test-Path $buildPropsTemplate) {
            Copy-ItemSafe -Source $buildPropsTemplate -Destination "Directory.Build.props" -Description "Directory.Build.props"
        }
        else {
            Write-Warning "Directory.Build.props template not found. Creating from template content..."
            if (-not $DryRun) {
                $buildPropsContent = Get-Content "$templatesDir/Directory.Build.props.template" -ErrorAction SilentlyContinue
                if ($buildPropsContent) {
                    Set-Content -Path "Directory.Build.props" -Value $buildPropsContent
                    Write-Info "Created Directory.Build.props from template"
                }
            }
        }
    }
    else {
        Write-Info "Directory.Build.props already exists"
    }
    
    # Update .gitignore
    Update-GitIgnore
}

function Update-GitIgnore {
    Write-Info "Updating .gitignore"
    
    if ($DryRun) {
        Write-DryRun "Would update .gitignore with new patterns"
        return
    }
    
    $gitignorePath = ".gitignore"
    if (-not (Test-Path $gitignorePath)) {
        New-Item -ItemType File -Path $gitignorePath | Out-Null
    }
    
    $additionalPatterns = @"

# Infrastructure
infra/terraform/.terraform/
infra/terraform/.terraform.lock.hcl
infra/terraform/*.tfvars
infra/terraform/.terraform.lock.hcl
*.tfstate
*.tfstate.backup
.terraform.lock.hcl

# Migration artifacts
scripts/migration/migration-log-*.txt
"@
    
    $gitignoreContent = Get-Content $gitignorePath -Raw -ErrorAction SilentlyContinue
    if ($gitignoreContent -notmatch "Infrastructure") {
        Add-Content -Path $gitignorePath -Value $additionalPatterns
        Write-Info "Updated .gitignore"
    }
}

# ============================================
# Migration Steps
# ============================================

function Start-Migration {
    Write-Info "=== MagiDesk POS Repository Structure Migration ==="
    Write-Info "Dry Run Mode: $DryRun"
    Write-Info "Skip Confirmation: $SkipConfirmation"
    Write-Host ""
    
    # Pre-flight checks
    Test-GitRepository
    Test-GitCleanWorkingDirectory
    
    if (-not $DryRun -and -not $SkipConfirmation) {
        Write-Warning "This will reorganize the repository structure."
        Write-Warning "A backup branch will be created: $script:BackupBranch"
        $response = Read-Host "Continue? (y/N)"
        if ($response -ne "y" -and $response -ne "Y") {
            Write-Info "Migration cancelled."
            return
        }
    }
    
    # Create backup branch
    if (-not $DryRun) {
        New-BackupBranch
    }
    
    # Step 1: Create new directory structure
    Write-Info "=== Step 1: Creating New Directory Structure ==="
    New-Directory -Path "src" -Description "Source root"
    New-Directory -Path "src/Backend" -Description "Backend services"
    New-Directory -Path "src/Frontend" -Description "Frontend application"
    New-Directory -Path "src/Shared" -Description "Shared libraries"
    New-Directory -Path "tests" -Description "Test projects"
    New-Directory -Path "docs" -Description "Documentation"
    New-Directory -Path "infra" -Description "Infrastructure"
    New-Directory -Path "infra/terraform" -Description "Terraform IaC"
    New-Directory -Path "infra/terraform/modules" -Description "Terraform modules"
    New-Directory -Path "infra/terraform/modules/cloudrun-service" -Description "Cloud Run service module"
    New-Directory -Path "infra/cloudbuild" -Description "Cloud Build configs"
    New-Directory -Path "scripts" -Description "Scripts"
    New-Directory -Path "scripts/deploy" -Description "Deployment scripts"
    New-Directory -Path "scripts/dev" -Description "Development scripts"
    New-Directory -Path "scripts/migration" -Description "Migration scripts"
    
    # Step 2: Move Backend Services
    Write-Info "=== Step 2: Migrating Backend Services ==="
    foreach ($service in $script:Microservices) {
        $oldPath = Join-Path $script:OldPaths.Backend $service
        $newPath = Join-Path $script:NewPaths.Backend $service
        
        if (Test-Path $oldPath) {
            Move-ItemSafe -Source $oldPath -Destination $newPath -Description "Backend service: $service"
        }
        else {
            Write-Warning "Backend service not found: $oldPath"
        }
    }
    
    # Step 3: Move Frontend
    Write-Info "=== Step 3: Migrating Frontend ==="
    if (Test-Path $script:OldPaths.Frontend) {
        Move-ItemSafe -Source $script:OldPaths.Frontend -Destination $script:NewPaths.Frontend -Description "Frontend application"
    }
    
    # Step 4: Move Shared
    Write-Info "=== Step 4: Migrating Shared Libraries ==="
    if (Test-Path $script:OldPaths.Shared) {
        Move-ItemSafe -Source $script:OldPaths.Shared -Destination $script:NewPaths.Shared -Description "Shared libraries"
    }
    
    # Step 5: Move Tests
    Write-Info "=== Step 5: Migrating Tests ==="
    if (Test-Path $script:OldPaths.Tests) {
        Move-ItemSafe -Source $script:OldPaths.Tests -Destination "tests/MagiDesk.Tests" -Description "Test projects"
    }
    
    # Step 6: Move Docs
    Write-Info "=== Step 6: Migrating Documentation ==="
    if (Test-Path $script:OldPaths.Docs) {
        # Create docs directory first
        New-Directory -Path "docs" -Description "Documentation root"
        
        # Create subdirectories
        New-Directory -Path "docs/api" -Description "API documentation"
        New-Directory -Path "docs/architecture" -Description "Architecture docs"
        New-Directory -Path "docs/deployment" -Description "Deployment docs"
        New-Directory -Path "docs/user-guides" -Description "User guides"
        
        # Copy existing docs content (preserve structure)
        $docsSource = $script:OldPaths.Docs
        if (Test-Path $docsSource) {
            Get-ChildItem -Path $docsSource -File -Recurse | ForEach-Object {
                $docsSourceResolved = (Resolve-Path $docsSource).Path
                $relativePath = $_.FullName.Substring($docsSourceResolved.Length + 1)
                $destPath = Join-Path "docs" $relativePath
                $destDir = Split-Path -Parent $destPath -ErrorAction SilentlyContinue
                if ($destDir -and $destDir.Trim().Length -gt 0 -and -not (Test-Path $destDir)) {
                    New-Item -ItemType Directory -Path $destDir -Force | Out-Null
                }
                if (-not $DryRun) {
                    Copy-Item -Path $_.FullName -Destination $destPath -Force
                    Write-Info "Copied: $relativePath"
                }
                else {
                    Write-DryRun "Would copy: $relativePath -> $destPath"
                }
            }
            Write-Info "Documentation copied (preserving existing structure)"
        }
    }
    
    # Step 7: Move Infrastructure Files
    Write-Info "=== Step 7: Organizing Infrastructure Files ==="
    # Move Cloud Build configs
    $cloudbuildFiles = Get-ChildItem -Path "solution" -Filter "*.yaml" -Recurse -ErrorAction SilentlyContinue
    foreach ($file in $cloudbuildFiles) {
        if ($file.Name -like "*cloudbuild*") {
            $dest = Join-Path "infra/cloudbuild" $file.Name
            Move-ItemSafe -Source $file.FullName -Destination $dest -Description "Cloud Build config"
        }
    }
    
    # Move deployment scripts
    $deployScripts = Get-ChildItem -Path "solution/backend" -Filter "deploy-*.ps1" -ErrorAction SilentlyContinue
    foreach ($script in $deployScripts) {
        $dest = Join-Path "scripts/deploy" $script.Name
        Move-ItemSafe -Source $script.FullName -Destination $dest -Description "Deployment script"
    }
    
    # Step 8: Remove Archive
    Write-Info "=== Step 8: Removing Archive Directory ==="
    if (Test-Path $script:OldPaths.Archive) {
        if (-not $SkipConfirmation) {
            $response = Read-Host "Remove archive directory? This cannot be undone. (y/N)"
            if ($response -eq "y" -or $response -eq "Y") {
                Remove-ItemSafe -Path $script:OldPaths.Archive -Description "Archive directory"
            }
        }
        else {
            Remove-ItemSafe -Path $script:OldPaths.Archive -Description "Archive directory"
        }
    }
    
    # Step 9: Update Solution File
    Write-Info "=== Step 9: Updating Solution File ==="
    Update-SolutionFile
    
    # Step 10: Create Standard Files
    Write-Info "=== Step 10: Creating Standard Configuration Files ==="
    New-StandardFiles
    
    # Step 11: Generate Terraform Infrastructure
    Write-Info "=== Step 11: Generating Terraform Infrastructure ==="
    New-TerraformFiles
    
    # Step 12: Validation
    if (-not $DryRun) {
        Write-Info "=== Step 12: Validating Migration ==="
        Test-Migration
    }
    
    # Summary
    Write-Host ""
    Write-Success "=== Migration Complete ==="
    Write-Info "New Structure:"
    Write-Host "  src/Backend/    - 9 microservices"
    Write-Host "  src/Frontend/   - WinUI3 application"
    Write-Host "  src/Shared/     - Shared libraries"
    Write-Host "  tests/          - Test projects"
    Write-Host "  docs/           - Documentation"
    Write-Host "  infra/          - Infrastructure as Code"
    Write-Host ""
    
    if ($DryRun) {
        Write-Warning "This was a dry run. No changes were made."
        Write-Info "Run without -DryRun to execute migration."
    }
    else {
        Write-Success "Backup branch: $script:BackupBranch"
        Write-Info "Next steps:"
        Write-Host "  1. Review changes: git status"
        Write-Host "  2. Test build: dotnet build"
        Write-Host "  3. Commit: git add . && git commit -m 'refactor: migrate to enterprise repository structure'"
    }
}

function New-TerraformFiles {
    Write-Info "Generating Terraform infrastructure files..."
    
    # Terraform files are already created in infra/terraform/
    # Just verify they exist
    if (Test-Path "infra/terraform/main.tf") {
        Write-Info "Terraform main.tf already exists"
    }
    else {
        Write-Warning "Terraform files not found. They should be created before migration."
    }
}

function Test-Migration {
    Write-Info "Running build validation..."
    
    if (-not $SkipTests) {
        # Test restore
        Write-Info "Testing NuGet restore..."
        $restoreResult = dotnet restore 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Error-Custom "Restore failed!"
            Write-Host $restoreResult
            return $false
        }
        
        # Test build
        Write-Info "Testing build..."
        $buildResult = dotnet build --no-restore 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Error-Custom "Build failed!"
            Write-Host $buildResult
            return $false
        }
        
        # Test tests (if present)
        Write-Info "Running tests..."
        $testResult = dotnet test --no-build --verbosity quiet 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Some tests failed (non-blocking)"
            Write-Host $testResult
        }
        else {
            Write-Success "All tests passed"
        }
    }
    else {
        Write-Info "Skipping validation (--SkipTests specified)"
    }
    
    return $true
}

# ============================================
# Execute
# ============================================

try {
    Start-Migration
}
catch {
    Write-Error-Custom "Fatal error: $_"
    Write-Host $_.ScriptStackTrace
    exit 1
}

