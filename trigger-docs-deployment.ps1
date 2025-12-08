# Script to trigger GitHub Actions documentation deployment
# This creates an empty commit to trigger the workflow

Write-Host "Triggering GitHub Actions documentation deployment..." -ForegroundColor Green

# Create a trigger file that will cause the workflow to run
$triggerFile = "docs/.trigger-deploy"
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

# Write timestamp to trigger file
Set-Content -Path $triggerFile -Value "Last deployment trigger: $timestamp"

# Stage and commit
git add $triggerFile
git commit -m "chore: trigger documentation deployment

- Updated trigger file to force workflow run
- Timestamp: $timestamp"

Write-Host "`nPushing to trigger workflow..." -ForegroundColor Yellow
git push

Write-Host "`n✅ Trigger sent! Check GitHub Actions tab for workflow status." -ForegroundColor Green
Write-Host "Workflow URL: https://github.com/zedfauji/pos-app/actions" -ForegroundColor Cyan
