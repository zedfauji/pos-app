# Repository Structure Migration

This directory contains scripts and templates for migrating the MagiDesk POS repository to an enterprise-standard structure.

## Quick Start

### 1. Preview Migration (Dry Run)

```powershell
.\scripts\migration\migrate-structure.ps1 -DryRun
```

### 2. Execute Migration

```powershell
.\scripts\migration\migrate-structure.ps1
```

### 3. Review and Commit

```powershell
git status
git add .
git commit -F scripts/migration/COMMIT_MESSAGE_TEMPLATE.md
```

## What Gets Migrated

### File Moves

| From | To | Description |
|------|-----|-------------|
| `solution/backend/TablesApi/` | `src/Backend/TablesApi/` | Backend microservices |
| `solution/backend/OrdersApi/` | `src/Backend/OrdersApi/` | (9 services total) |
| `solution/frontend/` | `src/Frontend/` | WinUI3 application |
| `solution/shared/` | `src/Shared/` | Shared libraries |
| `solution/MagiDesk.Tests/` | `tests/MagiDesk.Tests/` | Test projects |
| `solution/docs/` | `docs/` | Documentation |
| `solution/*.yaml` | `infra/cloudbuild/` | Cloud Build configs |
| `solution/backend/deploy-*.ps1` | `scripts/deploy/` | Deployment scripts |

### New Files Created

- `.editorconfig` - Code formatting standards
- `Directory.Build.props` - Shared MSBuild properties
- `infra/terraform/main.tf` - GCP infrastructure
- `infra/terraform/variables.tf` - Terraform variables
- `infra/terraform/modules/cloudrun-service/` - Reusable Cloud Run module

### Removed

- `solution/archive/` - Deprecated code (with confirmation)

## Safety Features

- **Dry-run mode**: Preview changes without executing
- **Backup branch**: Automatic backup branch creation
- **Build validation**: Tests build after migration
- **Interactive confirmations**: Confirm destructive actions

## Rollback

If migration fails or needs rollback:

```powershell
# Checkout backup branch
git checkout backup/pre-migration-YYYYMMDD-HHMMSS

# Or restore from backup
git checkout main
git reset --hard backup/pre-migration-YYYYMMDD-HHMMSS
```

## Post-Migration Checklist

- [ ] Review `git status` for all changes
- [ ] Verify `MagiDesk.sln` loads correctly
- [ ] Run `dotnet build` (should succeed)
- [ ] Run `dotnet test` (should pass)
- [ ] Update CI/CD workflows with new paths
- [ ] Update deployment scripts if needed
- [ ] Update documentation links
- [ ] Commit migration: `git commit -m "refactor: migrate to enterprise repo structure"`

## Troubleshooting

### Build Fails After Migration

1. Restore NuGet packages: `dotnet restore`
2. Clean build: `dotnet clean && dotnet build`
3. Check project paths in solution file

### Solution File Issues

The script automatically updates the solution file, but if issues occur:

```powershell
# Remove all projects
dotnet sln remove src/Backend/*/*.csproj
dotnet sln remove src/Frontend/*.csproj

# Re-add projects
dotnet sln add src/Backend/*/*.csproj
dotnet sln add src/Frontend/MagiDesk.Frontend.csproj
dotnet sln add src/Shared/MagiDesk.Shared.csproj
dotnet sln add tests/MagiDesk.Tests/MagiDesk.Tests.csproj
```

## Files in This Directory

- `migrate-structure.ps1` - Main migration script
- `templates/.editorconfig.template` - EditorConfig template
- `templates/Directory.Build.props.template` - MSBuild props template
- `COMMIT_MESSAGE_TEMPLATE.md` - Commit message template
- `README.md` - This file

