# Manual Trigger Guide for Developer Portal

## Why Manual Trigger?

The workflow may not trigger automatically if:
- Self-hosted runner is offline
- Path filters don't match recent changes
- GitHub Actions needs initial setup

## Method 1: GitHub Actions UI (Recommended)

### Steps:

1. **Go to GitHub Repository**
   ```
   https://github.com/zedfauji/pos-app
   ```

2. **Open Actions Tab**
   - Click "Actions" in the top navigation

3. **Select Workflow**
   - Click on "Deploy Documentation" in the left sidebar

4. **Run Workflow**
   - Click "Run workflow" button (top right)
   - Select branch: `implement-rbac-api-cursor` (or your current branch)
   - Optionally check "Force rebuild" if you want to rebuild everything
   - Click green "Run workflow" button

5. **Monitor Progress**
   - Watch the workflow run in real-time
   - Check each step for errors
   - Wait for "Deploy to GitHub Pages" step to complete

## Method 2: PowerShell Script

Run the trigger script:

```powershell
.\trigger-docs-deployment.ps1
```

This script:
- Creates a trigger file in `docs/`
- Commits and pushes the change
- Forces the workflow to run

## Method 3: Empty Commit

Create an empty commit to trigger:

```powershell
git commit --allow-empty -m "chore: trigger documentation deployment"
git push
```

## Method 4: Touch a File

Modify any file in `docs/` directory:

```powershell
# Add a comment or whitespace to any file in docs/
# Then commit and push
git add docs/
git commit -m "chore: update docs to trigger deployment"
git push
```

## Verification

After triggering:

1. **Check Actions Tab**
   - Go to: https://github.com/zedfauji/pos-app/actions
   - Verify workflow is running or completed

2. **Check Portal**
   - Wait 2-5 minutes after workflow completes
   - Visit: https://zedfauji.github.io/pos-app/
   - Verify portal is accessible

3. **Check Logs**
   - Click on the workflow run
   - Review build and deploy steps
   - Check for any errors

## Troubleshooting

### Workflow Not Appearing

- Verify workflow file exists: `.github/workflows/docs.yml`
- Check if workflow file has correct syntax
- Ensure you're on the correct branch

### Workflow Fails

- Check self-hosted runner is online
- Verify Node.js 18+ is installed on runner
- Check build logs for specific errors
- Verify `docs/package.json` is correct

### Portal Not Updating

- Wait a few minutes for GitHub Pages to propagate
- Clear browser cache
- Check repository Settings → Pages for deployment status

## Quick Reference

**Workflow URL**: https://github.com/zedfauji/pos-app/actions/workflows/docs.yml  
**Portal URL**: https://zedfauji.github.io/pos-app/  
**Trigger Script**: `.\trigger-docs-deployment.ps1`
