# Workflow Troubleshooting

## Issue: Workflow Not Triggering

### Changes Made:

1. ✅ **Removed Path Filters** - Now triggers on ANY push
2. ✅ **Switched to GitHub-Hosted Runner** - Changed from `self-hosted` to `ubuntu-latest`

### Why It Wasn't Triggering:

**Most Likely Cause**: Self-hosted runner was offline or unavailable

**Solution**: Switched to `ubuntu-latest` which is always available

## Current Configuration

- **Trigger**: Any push to any branch
- **Runner**: `ubuntu-latest` (GitHub-hosted)
- **Manual Trigger**: Available via workflow_dispatch

## Verify Workflow is Active

1. **Check Workflow File Exists**
   - ✅ `.github/workflows/docs.yml` exists

2. **Check GitHub Actions is Enabled**
   - Go to: Repository Settings → Actions → General
   - Ensure "Allow all actions and reusable workflows" is selected

3. **Check GitHub Pages is Enabled**
   - Go to: Repository Settings → Pages
   - Source should be "GitHub Actions"

## Test the Workflow

### Method 1: Manual Trigger (Recommended)

1. Go to: https://github.com/zedfauji/pos-app/actions
2. Click "Deploy Documentation"
3. Click "Run workflow"
4. Select branch and run

### Method 2: Push a Change

Since path filters are removed, ANY push will trigger:

```powershell
echo "test" >> test.txt
git add test.txt
git commit -m "test: trigger workflow"
git push
```

## Expected Behavior

After pushing:
- Workflow should appear in Actions tab within 1-2 minutes
- Status will show "Queued" then "In progress"
- Build should complete in 5-10 minutes

## If Still Not Working

1. **Check Repository Permissions**
   - Ensure you have write access
   - Check if Actions are enabled for the repository

2. **Check Branch Protection**
   - Some branches may have restrictions
   - Try pushing to a different branch

3. **Check Workflow Syntax**
   - Validate YAML syntax
   - Check for any errors in GitHub Actions tab

4. **Contact Repository Admin**
   - May need to enable GitHub Actions
   - May need to configure GitHub Pages

## Quick Test

Run this to force trigger:

```powershell
git commit --allow-empty -m "test: force trigger workflow"
git push
```

Then check: https://github.com/zedfauji/pos-app/actions

---

**Last Updated**: 2025-01-27  
**Status**: Switched to GitHub-hosted runner  
**Next**: Monitor Actions tab after next push
