# Repository Cleanup Execution Guide

## Quick Start

### 1. Render Blueprint Reconnection

**Before cleanup**, safely disconnect Render from `main` and reconnect to `develop`:

```powershell
# Set your Render API key
$env:RENDER_API_KEY = "your-api-key-here"

# Run the reconnection script
.\scripts\cleanup\render-reconnect.ps1

# Verify in Render dashboard: https://dashboard.render.com
```

**Manual Steps** (if script fails):
1. Go to Render Dashboard → Each service → Settings
2. Pause auto-deploy
3. Change branch from `main` to `develop`
4. Re-enable auto-deploy
5. Verify health endpoints: `curl https://tablesapi.onrender.com/health`

### 2. Execute Cleanup

```powershell
# Dry run first (recommended)
.\scripts\cleanup\ruthless-cleanup.ps1 -DryRun

# Review output, then run for real
.\scripts\cleanup\ruthless-cleanup.ps1

# Verify build
dotnet clean solution/MagiDesk.sln
dotnet build solution/MagiDesk.sln --configuration Release
dotnet test solution/MagiDesk.sln --configuration Release
```

### 3. Verify Results

```powershell
# Check cleanup report
cat cleanup-report.md

# Verify solution integrity
dotnet sln solution/MagiDesk.sln list

# Check for any remaining issues
grep -r "InventoryProxy" src/ tests/
```

## What Gets Cleaned

### Removed
- ✅ Unused services (InventoryProxy)
- ✅ Stale files (*old*, *backup*, *temp*, *deprecated*)
- ✅ Build artifacts (bin/, obj/, *.log, publish/)
- ✅ Duplicate files
- ✅ Dead tests

### Moved (Legacy Reference)
- 📦 Unsure items → `legacy-reference/` for review

### Kept
- ✅ All 9 active APIs (verified usage)
- ✅ Frontend (WinUI 3)
- ✅ Shared libraries
- ✅ Test suite
- ✅ Infrastructure files

## Rollback (If Needed)

If cleanup breaks something:

```powershell
# Restore from git
git checkout feature/revamp-2025-enterprise-ui
git reset --hard origin/feature/revamp-2025-enterprise-ui

# Or restore specific files
git checkout HEAD -- src/Backend/InventoryProxy/
```

## Post-Cleanup Checklist

- [ ] Build succeeds: `dotnet build solution/MagiDesk.sln`
- [ ] Tests pass: `dotnet test solution/MagiDesk.sln`
- [ ] Render services connected to `develop` branch
- [ ] Production (`main`) untouched
- [ ] Review `legacy-reference/` for any needed items
- [ ] Update team documentation if needed

## Support

- **Cleanup Script**: `scripts/cleanup/ruthless-cleanup.ps1`
- **Render Script**: `scripts/cleanup/render-reconnect.ps1`
- **Report**: `cleanup-report.md`
- **Issues**: Create GitHub issue or check `legacy-reference/`

---

**Safe Cleanup**: Production (`main`) is never touched. All changes happen on `feature/revamp-2025-enterprise-ui` branch.

