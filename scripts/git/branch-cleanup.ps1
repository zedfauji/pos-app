# ============================================
# MagiDesk POS - Safe Branch Hygiene Automation
# ============================================
# Purpose: Automated branch cleanup with safety checks
# Prerequisites: Git 2.30+, PowerShell 7+, Git remote 'origin' configured
# Usage: .\branch-cleanup.ps1 [-DryRun] [-SkipConfirmation] [-ArchiveOnly]

[CmdletBinding()]
param(
    [switch]$DryRun,
    [switch]$SkipConfirmation,
    [switch]$ArchiveOnly,
    [string]$BaseBranch = "main",
    [string]$ReviewOutputPath = "merge-review.md"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

# ============================================
# Configuration from Audit
# ============================================

$script:BaselineTag = "revamp-baseline-v1"
$script:ArchivePrefix = "archive"
$script:StructureBranch = "structure/restructure-v1"

# Branch Categories from Audit
$script:HighPriorityMerge = @(
    "implement-rbac-api-cursor",
    "implement-split-payment",
    "settings-restructure"
)

$script:MediumPriorityReview = @(
    "UI-revamp-cursor",
    "WinUI-Redesign-Cursor",
    "work-by-cursor-printing",
    "implement-vendors-management",
    "audit-refund-process"
)

$script:ImmediatePrune = @(
    "implement-table-layout-ui-cursor",  # Reverted on main
    "work-by-curser",                     # Typo in name
    "work-by-windsurf",                   # Too generic
    "work-by-grok",                       # Too generic
    "work-by-sonnet-windsurf"            # Too generic
)

$script:LowPriorityReview = @(
    "improving-settings-by-cursor",
    "improving-settings-by-windsurf",
    "membership-implementation-by-windsurf",
    "redesign-UI-Refactor",
    "starting-order-menu-work",
    "feature-8-sep-2025"
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
        Write-Warning "Working directory is not clean. Uncommitted changes detected:"
        Write-Host $status
        if (-not $SkipConfirmation) {
            $response = Read-Host "Continue anyway? (y/N)"
            if ($response -ne "y" -and $response -ne "Y") {
                throw "Aborted by user."
            }
        }
    }
}

function Test-GitRemoteConfigured {
    $remotes = git remote
    if (-not ($remotes -contains "origin")) {
        throw "Git remote 'origin' not configured. Cannot push changes."
    }
}

function Test-BranchExists($branchName) {
    $branches = git branch -a | ForEach-Object { $_.Trim().Replace("remotes/origin/", "").Replace("remotes/", "").Replace("* ", "") }
    return $branches -contains $branchName -or $branches -contains "origin/$branchName"
}

function Test-BranchUpToDate($branchName) {
    # Check if remote branch exists
    $remoteExists = Test-RemoteBranchExists -BranchName $branchName
    $localExists = git rev-parse --verify $branchName 2>$null
    
    $ahead = 0
    $behind = 0
    
    if ($remoteExists -and $localExists) {
        # Fetch to update remote tracking
        $null = git fetch origin $branchName 2>&1
        $countResult = git rev-list --left-right --count "origin/$branchName...$branchName" 2>$null
        if ($countResult) {
            $parts = $countResult -split "`t"
            $ahead = [int]$parts[0]
            $behind = [int]$parts[1]
        }
    }
    
    return @{
        Exists = $remoteExists
        Ahead = $ahead
        Behind = $behind
        LocalOnly = $localExists -and -not $remoteExists
        RemoteOnly = -not $localExists -and $remoteExists
    }
}

# ============================================
# Git Operations (Safe)
# ============================================

function Invoke-GitCommand {
    param(
        [string[]]$Arguments,
        [switch]$RequireConfirmation,
        [string]$Description
    )
    
    if ($DryRun) {
        Write-DryRun "Would execute: git $($Arguments -join ' ')"
        if ($Description) {
            Write-DryRun "  Purpose: $Description"
        }
        return
    }
    
    if ($RequireConfirmation -and -not $SkipConfirmation) {
        Write-Info "About to execute: git $($Arguments -join ' ')"
        Write-Info "Purpose: $Description"
        $response = Read-Host "Continue? (y/N)"
        if ($response -ne "y" -and $response -ne "Y") {
            Write-Warning "Skipped by user."
            return
        }
    }
    
    try {
        & git $Arguments 2>&1 | ForEach-Object { Write-Host $_ }
        if ($LASTEXITCODE -ne 0) {
            throw "Git command failed: git $($Arguments -join ' ') (Exit code: $LASTEXITCODE)"
        }
    }
    catch {
        Write-Error-Custom "Git operation failed: $_"
        throw
    }
}

function New-BaselineTag {
    Write-Info "Creating baseline tag: $script:BaselineTag"
    
    # Check if tag already exists
    $tagExists = git rev-parse --verify $script:BaselineTag 2>$null
    if ($tagExists) {
        Write-Warning "Tag $script:BaselineTag already exists at: $tagExists"
        if (-not $SkipConfirmation) {
            $response = Read-Host "Overwrite? (y/N)"
            if ($response -ne "y" -and $response -ne "Y") {
                Write-Info "Skipping baseline tag creation."
                return
            }
            Invoke-GitCommand -Arguments @("tag", "-d", $script:BaselineTag) -Description "Delete existing tag"
            Invoke-GitCommand -Arguments @("push", "origin", ":refs/tags/$script:BaselineTag") -Description "Delete remote tag" -RequireConfirmation
        }
    }
    
    Invoke-GitCommand -Arguments @("checkout", $BaseBranch) -Description "Switch to base branch"
    Invoke-GitCommand -Arguments @("pull", "origin", $BaseBranch) -Description "Pull latest changes"
    
    $tagMessage = "Pre-revamp baseline: 9 microservices, manual deployment, branch chaos - Created $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    Invoke-GitCommand -Arguments @("tag", "-a", $script:BaselineTag, "-m", $tagMessage) -Description "Create baseline tag"
    Invoke-GitCommand -Arguments @("push", "origin", $script:BaselineTag) -Description "Push baseline tag" -RequireConfirmation
}

function Test-TagExists {
    param([string]$TagName)
    
    try {
        $result = git rev-parse --verify $TagName 2>$null
        return $result -ne $null -and $result.ToString().Trim().Length -gt 0
    }
    catch {
        return $false
    }
}

function New-ArchiveTag {
    param(
        [string]$BranchName,
        [string]$ArchiveMessage
    )
    
    $archiveTag = "$script:ArchivePrefix/$($BranchName -replace '/', '-')-$(Get-Date -Format 'yyyyMMdd')"
    
    # Check if tag already exists
    $tagExists = Test-TagExists -TagName $archiveTag
    if ($tagExists) {
        Write-Info "Archive tag $archiveTag already exists. Skipping tag creation (using existing tag)."
        return $archiveTag
    }
    
    Write-Info "Creating archive tag: $archiveTag"
    
    # Checkout branch first
    if (Test-BranchExists $BranchName) {
        $branchInfo = Test-BranchUpToDate $BranchName
        if ($branchInfo.RemoteOnly) {
            Write-Info "Branch $BranchName exists only on remote. Fetching..."
            Invoke-GitCommand -Arguments @("fetch", "origin", $BranchName) -Description "Fetch remote branch"
            $localBranch = "origin/$BranchName"
        }
        else {
            $currentBranch = git rev-parse --abbrev-ref HEAD
            if ($currentBranch -ne $BranchName) {
                Invoke-GitCommand -Arguments @("checkout", $BranchName) -Description "Checkout branch for archiving"
            }
            $localBranch = $BranchName
        }
        
        # Create tag
        Invoke-GitCommand -Arguments @("tag", "-a", $archiveTag, "-m", $ArchiveMessage) -Description "Create archive tag"
        
        # Push tag if not in dry-run mode
        if (-not $DryRun) {
            $remoteTagExists = git ls-remote --tags origin $archiveTag 2>$null
            if ($LASTEXITCODE -eq 0 -and $remoteTagExists -and $remoteTagExists.ToString().Trim().Length -gt 0) {
                Write-Info "Archive tag $archiveTag already exists on remote. Skipping push."
            }
            else {
                Invoke-GitCommand -Arguments @("push", "origin", $archiveTag) -Description "Push archive tag" -RequireConfirmation
            }
        }
        
        # Return to main
        $currentBranch = git rev-parse --abbrev-ref HEAD
        if ($currentBranch -ne $BaseBranch) {
            Invoke-GitCommand -Arguments @("checkout", $BaseBranch) -Description "Return to base branch"
        }
        
        return $archiveTag
    }
    else {
        Write-Warning "Branch $BranchName does not exist. Skipping archive tag creation."
        return $null
    }
}

function Merge-BranchToMain {
    param(
        [string]$BranchName,
        [string]$MergeMessage
    )
    
    Write-Info "Merging branch: $BranchName -> $BaseBranch"
    
    # Pre-flight checks
    $branchInfo = Test-BranchUpToDate $BranchName
    if ($branchInfo.RemoteOnly) {
        Write-Info "Branch exists only on remote. Creating local tracking branch..."
        Invoke-GitCommand -Arguments @("checkout", "-b", $BranchName, "origin/$BranchName") -Description "Create local tracking branch"
    }
    elseif (-not (Test-BranchExists $BranchName)) {
        Write-Warning "Branch $BranchName does not exist. Skipping merge."
        return $false
    }
    
    # Ensure we're on main
    Invoke-GitCommand -Arguments @("checkout", $BaseBranch) -Description "Switch to base branch"
    Invoke-GitCommand -Arguments @("pull", "origin", $BaseBranch) -Description "Pull latest changes"
    
    # Try merge with --no-ff
    try {
        Invoke-GitCommand -Arguments @("merge", "--no-ff", $BranchName, "-m", $MergeMessage) -Description "Merge branch (no fast-forward)" -RequireConfirmation
        
        # Check for merge conflicts
        $conflictStatus = git diff --check
        if ($LASTEXITCODE -ne 0 -or (git status --porcelain | Select-String "^UU|^AA|^DD")) {
            Write-Error-Custom "Merge conflicts detected in branch: $BranchName"
            Write-Warning "Resolve conflicts manually:"
            Write-Host "  1. Review conflicts: git status"
            Write-Host "  2. Resolve: git mergetool (or manual edit)"
            Write-Host "  3. Complete merge: git commit"
            Write-Host "  4. Push: git push origin $BaseBranch"
            throw "Merge conflict resolution required for: $BranchName"
        }
        
        return $true
    }
    catch {
        Write-Error-Custom "Merge failed for $BranchName : $_"
        # Abort merge if it's in progress
        if (Test-Path .git/MERGE_HEAD) {
            Write-Warning "Aborting merge..."
            Invoke-GitCommand -Arguments @("merge", "--abort") -Description "Abort merge"
        }
        return $false
    }
}

function Test-RemoteBranchExists {
    param([string]$BranchName)
    
    try {
        $result = git ls-remote --heads origin $BranchName 2>&1
        if ($LASTEXITCODE -eq 0 -and $result -and $result.ToString().Trim().Length -gt 0) {
            return $true
        }
        return $false
    }
    catch {
        return $false
    }
}

function Remove-Branch {
    param(
        [string]$BranchName,
        [switch]$Remote
    )
    
    Write-Info "Removing branch: $BranchName (Remote: $Remote)"
    
    if ($Remote) {
        # Check if remote branch exists using ls-remote
        $remoteExists = Test-RemoteBranchExists -BranchName $BranchName
        
        if ($remoteExists) {
            Invoke-GitCommand -Arguments @("push", "origin", "--delete", $BranchName) -Description "Delete remote branch" -RequireConfirmation
        }
        else {
            Write-Info "Remote branch origin/$BranchName does not exist. Skipping remote deletion (already deleted or never existed)."
        }
    }
    else {
        # Check if local branch exists
        $localExists = git rev-parse --verify $BranchName 2>$null
        
        if ($localExists) {
            # Switch away from branch if currently on it
            $currentBranch = git rev-parse --abbrev-ref HEAD
            if ($currentBranch -eq $BranchName) {
                Invoke-GitCommand -Arguments @("checkout", $BaseBranch) -Description "Switch away from branch before deletion"
            }
            
            Invoke-GitCommand -Arguments @("branch", "-D", $BranchName) -Description "Force delete local branch" -RequireConfirmation
        }
        else {
            Write-Info "Local branch $BranchName does not exist. Skipping local deletion (already deleted)."
        }
    }
}

function Get-BranchDiffStats {
    param(
        [string]$BranchName,
        [string]$BaseBranch = "main"
    )
    
    $branchInfo = Test-BranchUpToDate $BranchName
    if ($branchInfo.RemoteOnly) {
        Invoke-GitCommand -Arguments @("fetch", "origin", $BranchName) -Description "Fetch remote branch for diff"
        $compareBranch = "origin/$BranchName"
    }
    elseif (Test-BranchExists $BranchName) {
        $compareBranch = $BranchName
    }
    else {
        return $null
    }
    
    $diffStats = @{
        Branch = $BranchName
        Commits = git rev-list --count "$BaseBranch..$compareBranch"
        FilesChanged = (git diff --name-only "$BaseBranch..$compareBranch" | Measure-Object -Line).Lines
        Insertions = 0
        Deletions = 0
        Ahead = $branchInfo.Ahead
        Behind = $branchInfo.Behind
        LastCommit = git log -1 --format="%h - %s (%ar)" $compareBranch
        DiffSummary = git diff --stat "$BaseBranch..$compareBranch"
    }
    
    # Count insertions/deletions
    $diffLines = git diff --numstat "$BaseBranch..$compareBranch"
    foreach ($line in $diffLines) {
        if ($line -match "^\d+\s+\d+\s+") {
            $parts = $line -split "`t"
            $diffStats.Insertions += [int]$parts[0]
            $diffStats.Deletions += [int]$parts[1]
        }
    }
    
    return $diffStats
}

# ============================================
# Main Workflow
# ============================================

function Start-BranchHygiene {
    Write-Info "=== MagiDesk POS Branch Hygiene Automation ==="
    Write-Info "Dry Run Mode: $DryRun"
    Write-Info "Skip Confirmation: $SkipConfirmation"
    Write-Info "Archive Only: $ArchiveOnly"
    Write-Host ""
    
    # Pre-flight checks
    Test-GitRepository
    Test-GitCleanWorkingDirectory
    Test-GitRemoteConfigured
    
    # Fetch all remote branches
    Write-Info "Fetching all remote branches..."
    Invoke-GitCommand -Arguments @("fetch", "--all", "--prune") -Description "Fetch and prune remote branches"
    
    # Step 1: Create baseline tag
    if (-not $ArchiveOnly) {
        New-BaselineTag
    }
    
    # Step 2: Archive high-priority branches before merging
    $archivedBranches = @()
    foreach ($branch in $script:HighPriorityMerge) {
        if (Test-BranchExists $branch) {
            $archiveMessage = "Pre-merge archive: $branch - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
            $archiveTag = New-ArchiveTag -BranchName $branch -ArchiveMessage $archiveMessage
            if ($archiveTag) {
                $archivedBranches += @{ Branch = $branch; Tag = $archiveTag }
            }
        }
        else {
            Write-Warning "Branch $branch does not exist. Skipping archive."
        }
    }
    
    if ($ArchiveOnly) {
        $archivedCount = if ($archivedBranches) { $archivedBranches.Count } else { 0 }
        Write-Success "Archive-only mode complete. Archived $archivedCount branches."
        return
    }
    
    # Step 3: Merge high-priority branches
    Write-Info "=== High-Priority Merges ==="
    $script:mergeResults = @()
    foreach ($branch in $script:HighPriorityMerge) {
        $mergeMessage = "Merge: $branch (Automated cleanup)"
        $success = Merge-BranchToMain -BranchName $branch -MergeMessage $mergeMessage
        $script:mergeResults += @{ Branch = $branch; Success = $success }
        
        if ($success -and -not $DryRun) {
            # Push merge to remote
            Invoke-GitCommand -Arguments @("push", "origin", $BaseBranch) -Description "Push merge to remote" -RequireConfirmation
            
            # Delete merged branch
            Remove-Branch -BranchName $branch -Remote
            Remove-Branch -BranchName $branch
        }
    }
    
    # Step 4: Generate review report for medium-priority branches
    Write-Info "=== Generating Review Report for Medium-Priority Branches ==="
    $reviewReport = @"
# Branch Review Report
Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
Base Branch: $BaseBranch

## Medium-Priority Branches (Require Review)

"@
    
    foreach ($branch in $script:MediumPriorityReview) {
        if (Test-BranchExists $branch) {
            Write-Info "Analyzing branch: $branch"
            $diffStats = Get-BranchDiffStats -BranchName $branch -BaseBranch $BaseBranch
            if ($diffStats) {
                $reviewReport += @"

### $branch

- **Status**: $($diffStats.Ahead) commits ahead, $($diffStats.Behind) commits behind $BaseBranch
- **Commits**: $($diffStats.Commits) new commits
- **Files Changed**: $($diffStats.FilesChanged)
- **Changes**: +$($diffStats.Insertions) / -$($diffStats.Deletions) lines
- **Last Commit**: $($diffStats.LastCommit)

#### Diff Summary
``````
$($diffStats.DiffSummary)
``````

#### Recommendation
Review changes and decide: MERGE, EXTRACT (specific commits), or PRUNE

---
"@
            }
        }
        else {
            Write-Warning "Branch $branch does not exist. Skipping review."
        }
    }
    
    # Save review report
    if (-not $DryRun) {
        $reviewReport | Out-File -FilePath $ReviewOutputPath -Encoding UTF8
        Write-Success "Review report saved to: $ReviewOutputPath"
    }
    else {
        Write-DryRun "Would save review report to: $ReviewOutputPath"
    }
    
    # Step 5: Prune immediate branches (with archive)
    Write-Info "=== Pruning Immediate Branches ==="
    foreach ($branch in $script:ImmediatePrune) {
        $branchInfo = Test-BranchUpToDate $branch
        $existsLocally = git rev-parse --verify $branch 2>$null
        $existsRemotely = Test-RemoteBranchExists -BranchName $branch
        
        if ($existsLocally -or $existsRemotely) {
            # Archive before pruning (only if branch has commits)
            $archiveMessage = "Pre-prune archive: $branch - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
            if ($existsLocally) {
                $archiveTag = New-ArchiveTag -BranchName $branch -ArchiveMessage $archiveMessage
            }
            elseif ($existsRemotely) {
                Write-Info "Branch $branch exists only on remote. Fetching for archive..."
                Invoke-GitCommand -Arguments @("fetch", "origin", $branch) -Description "Fetch remote branch for archiving"
                $archiveTag = New-ArchiveTag -BranchName $branch -ArchiveMessage $archiveMessage
            }
            
            # Prune (both remote and local)
            if ($existsRemotely) {
                Remove-Branch -BranchName $branch -Remote
            }
            if ($existsLocally) {
                Remove-Branch -BranchName $branch
            }
        }
        else {
            Write-Info "Branch $branch does not exist locally or remotely. Skipping prune (already cleaned up)."
        }
    }
    
    # Step 6: Update README with branch naming conventions
    Write-Info "=== Updating README with Branch Naming Conventions ==="
    if (-not $DryRun) {
        $readmePath = "BRANCH_WORKFLOW_README.md"
        if (-not (Test-Path $readmePath)) {
            Write-Warning "README file not found at $readmePath. Skipping README update."
        }
        else {
            Write-Info "README already exists at $readmePath. Manual update may be needed."
        }
    }
    else {
        Write-DryRun "Would check/update README with branch naming conventions"
    }
    
    # Step 7: Generate GitHub Branch Protection JSON
    Write-Info "=== Generating GitHub Branch Protection Configuration ==="
    if (-not $DryRun) {
        $protectionJsonPath = "github-branch-protection.json"
        if (-not (Test-Path $protectionJsonPath)) {
            Write-Warning "Branch protection JSON not found at $protectionJsonPath. Creating from template..."
            # Template already created in separate file
            Write-Info "Branch protection template available at: $protectionJsonPath"
        }
        Write-Info "GitHub Branch Protection JSON available at: $protectionJsonPath"
        Write-Info "Apply using: gh api repos/:owner/:repo/branches/main/protection --method PUT --input $protectionJsonPath"
    }
    else {
        Write-DryRun "Would generate/verify GitHub branch protection JSON"
    }
    
    # Step 8: Summary
    Write-Host ""
    Write-Success "=== Branch Hygiene Complete ==="
    
    # Initialize counts safely
    $archivedCount = if ($archivedBranches) { $archivedBranches.Count } else { 0 }
    $mergedCount = 0
    if ($script:mergeResults -and $script:mergeResults.Count -gt 0) {
        $mergedCount = (@($script:mergeResults | Where-Object { $_.Success -eq $true })).Count
    }
    
    Write-Info "Archived Branches: $archivedCount"
    Write-Info "Merged Branches: $mergedCount"
    Write-Info "Review Report: $ReviewOutputPath"
    Write-Info "Branch Workflow README: BRANCH_WORKFLOW_README.md"
    Write-Info "GitHub Protection JSON: github-branch-protection.json"
    Write-Info "Dry Run Mode: $DryRun"
    
    if ($DryRun) {
        Write-Warning "This was a dry run. No changes were made. Run without -DryRun to execute."
    }
    else {
        Write-Success "Next steps:"
        Write-Host "  1. Review $ReviewOutputPath for medium-priority branches"
        Write-Host "  2. Apply branch protection: gh api repos/:owner/:repo/branches/main/protection --method PUT --input github-branch-protection.json"
        Write-Host "  3. Share BRANCH_WORKFLOW_README.md with team"
    }
}

# ============================================
# Execute
# ============================================

try {
    Start-BranchHygiene
}
catch {
    Write-Error-Custom "Fatal error: $_"
    Write-Host $_.ScriptStackTrace
    exit 1
}

