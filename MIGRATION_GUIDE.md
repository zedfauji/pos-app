# Repository Structure Migration Guide
## MagiDesk POS - Enterprise Structure Migration

**Status**: Ready for execution  
**Estimated Time**: 15-30 minutes  
**Risk Level**: Low (with backup branch)

---

## Overview

This migration reorganizes the repository from the current `solution/`-based structure to an enterprise-standard layout aligned with the DevOps audit recommendations.

### Current Structure → New Structure

```
solution/backend/TablesApi/  →  src/Backend/TablesApi/
solution/frontend/           →  src/Frontend/
solution/shared/             →  src/Shared/
solution/MagiDesk.Tests/     →  tests/MagiDesk.Tests/
solution/docs/               →  docs/
solution/archive/            →  [REMOVED]
```

---

## Prerequisites

- ✅ Git 2.30+
- ✅ PowerShell 7+
- ✅ .NET 8 SDK installed
- ✅ Clean working directory (commit or stash changes)
- ✅ Access to repository (read/write)

---

## Step-by-Step Migration

### Step 1: Preview Migration (Recommended)

```powershell
.\scripts\migration\migrate-structure.ps1 -DryRun
```

**What it does:**
- Shows all file moves that will happen
- Lists directories that will be created
- Displays solution file updates
- **No changes are made**

### Step 2: Execute Migration

```powershell
.\scripts\migration\migrate-structure.ps1
```

**What it does:**
1. Creates backup branch: `backup/pre-migration-YYYYMMDD-HHMMSS`
2. Creates new directory structure (`src/`, `tests/`, `docs/`, `infra/`)
3. Moves all backend services (9 APIs)
4. Moves frontend application
5. Moves shared libraries
6. Moves test projects
7. Moves documentation
8. Organizes infrastructure files
9. Updates `MagiDesk.sln` with new paths
10. Creates standard files (`.editorconfig`, `Directory.Build.props`)
11. Generates Terraform IaC files
12. Validates build (`dotnet build --no-restore`)
13. Runs tests (`dotnet test`)

**Interactive Prompts:**
- Confirm to continue migration
- Confirm to remove archive directory
- Confirm before pushing backup branch

### Step 3: Review Changes

```powershell
git status
git diff --stat
```

**Verify:**
- ✅ All projects moved to `src/Backend/`
- ✅ Frontend moved to `src/Frontend/`
- ✅ Solution file updated
- ✅ New standard files created (`.editorconfig`, `Directory.Build.props`)

### Step 4: Test Build

```powershell
dotnet clean
dotnet restore
dotnet build
dotnet test
```

**Expected Result:** ✅ All projects build and tests pass

### Step 5: Commit Migration

```powershell
git add .
git commit -F scripts/migration/COMMIT_MESSAGE_TEMPLATE.md
```

Or use the template:

```powershell
git commit -m "refactor: migrate to enterprise repository structure

BREAKING CHANGE: Repository structure reorganized

- Backend: solution/backend → src/Backend
- Frontend: solution/frontend → src/Frontend
- Shared: solution/shared → src/Shared
- Tests: solution/MagiDesk.Tests → tests/MagiDesk.Tests
- Docs: solution/docs → docs/
- Infrastructure: infra/terraform/ (new)
- Removed: solution/archive/

Backup branch: backup/pre-migration-YYYYMMDD-HHMMSS"
```

---

## What Gets Created

### New Files

1. **`.editorconfig`** - Code formatting standards (UTF-8, 4-space indent for C#, etc.)
2. **`Directory.Build.props`** - Shared MSBuild properties (LangVersion, Nullable, etc.)
3. **`infra/terraform/main.tf`** - GCP infrastructure (Cloud Run + Cloud SQL)
4. **`infra/terraform/variables.tf`** - Terraform variables
5. **`infra/terraform/outputs.tf`** - Terraform outputs
6. **`infra/terraform/modules/cloudrun-service/main.tf`** - Reusable Cloud Run module

### Updated Files

1. **`MagiDesk.sln`** - Project paths updated
2. **`.gitignore`** - Infrastructure patterns added

---

## Rollback Procedure

If migration fails or needs to be undone:

```powershell
# Option 1: Checkout backup branch
git checkout backup/pre-migration-YYYYMMDD-HHMMSS

# Option 2: Reset main to backup (destructive)
git checkout main
git reset --hard backup/pre-migration-YYYYMMDD-HHMMSS

# Option 3: Restore specific files
git checkout backup/pre-migration-YYYYMMDD-HHMMSS -- solution/backend/TablesApi/
```

---

## Post-Migration Tasks

### Immediate (Required)

- [ ] Verify all projects build: `dotnet build`
- [ ] Run all tests: `dotnet test`
- [ ] Update CI/CD workflows (GitHub Actions paths)
- [ ] Update deployment scripts (if they reference old paths)

### Short-term (Week 1)

- [ ] Update README.md with new structure
- [ ] Update developer onboarding docs
- [ ] Notify team of new structure
- [ ] Update IDE workspace files (if using)

### Medium-term (Weeks 2-4)

- [ ] Migrate CI/CD to use new paths
- [ ] Update Terraform with actual values
- [ ] Test Terraform provisioning in dev environment
- [ ] Document Terraform usage

---

## Troubleshooting

### Build Fails After Migration

```powershell
# 1. Clean and restore
dotnet clean
dotnet restore

# 2. Check solution file
dotnet sln list

# 3. Verify project references
# Check .csproj files for relative paths
```

### Solution File Issues

```powershell
# Manually regenerate solution
dotnet sln remove src/Backend/*/*.csproj
dotnet sln add src/Backend/TablesApi/TablesApi.csproj
# ... repeat for all projects
```

### Project References Broken

Check `.csproj` files for `<ProjectReference>` paths. Update if needed:

```xml
<!-- Old -->
<ProjectReference Include="..\..\shared\MagiDesk.Shared.csproj" />

<!-- New -->
<ProjectReference Include="..\..\..\src\Shared\MagiDesk.Shared.csproj" />
```

---

## Migration Checklist

### Pre-Migration

- [ ] Backup current state (script does this automatically)
- [ ] Review dry-run output
- [ ] Ensure clean working directory
- [ ] Notify team (if working with others)

### During Migration

- [ ] Run dry-run first
- [ ] Review interactive prompts
- [ ] Monitor script output for errors

### Post-Migration

- [ ] Verify directory structure
- [ ] Test solution opens in Visual Studio
- [ ] Run build (`dotnet build`)
- [ ] Run tests (`dotnet test`)
- [ ] Review git status
- [ ] Commit changes
- [ ] Push backup branch to remote

---

## Expected Results

### Directory Structure After Migration

```
MagiDesk-POS/
├── src/
│   ├── Backend/
│   │   ├── TablesApi/
│   │   ├── OrdersApi/
│   │   ├── PaymentApi/
│   │   ├── MenuApi/
│   │   ├── CustomerApi/
│   │   ├── DiscountApi/
│   │   ├── InventoryApi/
│   │   ├── SettingsApi/
│   │   └── UsersApi/
│   ├── Frontend/
│   │   ├── Views/
│   │   ├── ViewModels/
│   │   ├── Services/
│   │   └── ...
│   └── Shared/
│       ├── DTOs/
│       └── ...
├── tests/
│   └── MagiDesk.Tests/
├── docs/
├── infra/
│   ├── terraform/
│   │   ├── main.tf
│   │   ├── variables.tf
│   │   └── modules/
│   └── cloudbuild/
├── scripts/
│   ├── deploy/
│   ├── dev/
│   └── migration/
├── .editorconfig
├── Directory.Build.props
└── MagiDesk.sln (updated paths)
```

---

## Support

If migration fails:

1. Check script output for specific errors
2. Review backup branch: `git show backup/pre-migration-YYYYMMDD-HHMMSS`
3. Check solution file paths manually
4. Verify .NET SDK version: `dotnet --version` (should be 8.x)

---

**Ready to migrate?** Start with: `.\scripts\migration\migrate-structure.ps1 -DryRun`

