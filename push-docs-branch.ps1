# Script to push documentation branch to remote
Write-Host "=== Git Branch Push Diagnostic ===" -ForegroundColor Cyan

# Check current branch
Write-Host "`n1. Current branch:" -ForegroundColor Yellow
$branch = git rev-parse --abbrev-ref HEAD
Write-Host "   Branch: $branch" -ForegroundColor Green

# Check remote
Write-Host "`n2. Remote configuration:" -ForegroundColor Yellow
$remote = git remote get-url origin
Write-Host "   Remote: $remote" -ForegroundColor Green

# Check if branch exists locally
Write-Host "`n3. Local branches:" -ForegroundColor Yellow
git branch | ForEach-Object { Write-Host "   $_" }

# Check commits
Write-Host "`n4. Recent commits:" -ForegroundColor Yellow
git log --oneline -5 | ForEach-Object { Write-Host "   $_" }

# Check if docs files are tracked
Write-Host "`n5. Checking if docs files are tracked:" -ForegroundColor Yellow
if (Test-Path "docs\package.json") {
    Write-Host "   docs/package.json exists" -ForegroundColor Green
    $tracked = git ls-files docs/package.json
    if ($tracked) {
        Write-Host "   docs/package.json is tracked by git" -ForegroundColor Green
    } else {
        Write-Host "   docs/package.json is NOT tracked - needs to be added" -ForegroundColor Red
    }
} else {
    Write-Host "   docs/package.json NOT FOUND" -ForegroundColor Red
}

# Check uncommitted changes
Write-Host "`n6. Uncommitted changes:" -ForegroundColor Yellow
$status = git status --porcelain
if ($status) {
    Write-Host "   There are uncommitted changes:" -ForegroundColor Yellow
    $status | ForEach-Object { Write-Host "   $_" }
} else {
    Write-Host "   No uncommitted changes" -ForegroundColor Green
}

# Check if remote branch exists
Write-Host "`n7. Checking remote branches:" -ForegroundColor Yellow
$remoteBranches = git ls-remote --heads origin
if ($remoteBranches -match "implement-rbac-api-cursor") {
    Write-Host "   Branch exists on remote" -ForegroundColor Green
} else {
    Write-Host "   Branch does NOT exist on remote" -ForegroundColor Red
}

# Check commits ahead
Write-Host "`n8. Commits to push:" -ForegroundColor Yellow
$commitsAhead = git log origin/$branch..HEAD --oneline 2>&1
if ($commitsAhead -and $commitsAhead -notmatch "fatal") {
    Write-Host "   Commits ahead of remote:" -ForegroundColor Yellow
    $commitsAhead | ForEach-Object { Write-Host "   $_" }
} else {
    Write-Host "   No commits to push (or remote branch doesn't exist)" -ForegroundColor Yellow
}

# Attempt push
Write-Host "`n9. Attempting to push..." -ForegroundColor Yellow
Write-Host "   Command: git push -u origin $branch" -ForegroundColor Cyan

try {
    $pushOutput = git push -u origin $branch 2>&1
    Write-Host "   Push output:" -ForegroundColor Cyan
    $pushOutput | ForEach-Object { Write-Host "   $_" }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✅ Push successful!" -ForegroundColor Green
    } else {
        Write-Host "`n❌ Push failed with exit code: $LASTEXITCODE" -ForegroundColor Red
    }
} catch {
    Write-Host "`n❌ Error during push: $_" -ForegroundColor Red
}

Write-Host "`n=== Diagnostic Complete ===" -ForegroundColor Cyan
