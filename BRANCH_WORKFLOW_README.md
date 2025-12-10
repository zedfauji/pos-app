# Branch Workflow & Naming Conventions
## MagiDesk POS - Git Branch Management Guide

**Last Updated**: 2024-12-08  
**Based on**: DevOps Audit & Branch Hygiene Automation

---

## 📋 Branch Naming Convention

### Standard Prefixes

| Prefix | Purpose | Example | Lifecycle |
|--------|---------|---------|-----------|
| `feature/` | New features or enhancements | `feature/rbac-implementation` | Merge → Delete after merge |
| `bugfix/` | Bug fixes | `bugfix/payment-validation-error` | Merge → Delete after merge |
| `hotfix/` | Critical production fixes | `hotfix/critical-session-crash` | Merge → Delete after merge |
| `refactor/` | Code refactoring | `refactor/extract-auth-service` | Merge → Delete after merge |
| `docs/` | Documentation updates | `docs/api-documentation-update` | Merge → Delete after merge |
| `structure/` | Repository restructuring | `structure/migrate-to-new-layout` | Long-lived (restructuring work) |
| `release/` | Release preparation | `release/v1.2.0` | Merge → Keep tag, delete branch |

### Format Rules

- **Use kebab-case**: All lowercase, words separated by hyphens
- **Be descriptive**: Include enough context to understand the branch purpose
- **Keep it short**: Maximum 50 characters (excluding prefix)
- **No special characters**: Only letters, numbers, and hyphens

### ❌ Prohibited Patterns

**Never use these patterns:**
- `work-by-*` (too generic)
- `*-by-cursor`, `*-by-windsurf`, `*-by-grok` (tool-specific, not descriptive)
- `temp-*`, `tmp-*`, `test-branch`, `wip-*`, `draft-*` (temporary branches)
- Dates only: `feature-8-sep-2025` (use descriptive names)
- Typos: `work-by-curser` (should be `feature/print-receipt-improvements`)

### ✅ Good Examples

```
feature/rbac-implementation
bugfix/payment-validation-error
hotfix/critical-session-crash
refactor/extract-auth-service
docs/api-documentation-update
structure/migrate-to-new-layout
release/v1.2.0
feature/split-payment-ui
bugfix/table-session-recovery
```

---

## 🔄 Branch Workflow

### Creating a New Branch

```powershell
# 1. Ensure you're on main and up-to-date
git checkout main
git pull origin main

# 2. Create and checkout new branch
git checkout -b feature/your-feature-name

# 3. Push to remote and set upstream
git push -u origin feature/your-feature-name
```

### Working on a Branch

```powershell
# Make your changes, commit regularly
git add .
git commit -m "feat: add new feature description"

# Push updates
git push origin feature/your-feature-name
```

### Merging to Main

**Always use Pull Requests (PRs):**

1. Push your branch: `git push origin feature/your-feature-name`
2. Create PR on GitHub (or your Git platform)
3. Wait for CI checks to pass (lint, test, build)
4. Get required approvals (1 for features, 2 for releases)
5. Merge via PR (no direct pushes to main)

**After merge:**
- Delete local branch: `git branch -d feature/your-feature-name`
- Delete remote branch: `git push origin --delete feature/your-feature-name`

---

## 🛡️ Branch Protection Rules

### Main Branch

- ✅ Requires PR review (1 approval minimum)
- ✅ Requires all CI checks to pass
- ✅ No force pushes allowed
- ✅ No direct commits (must use PR)
- ✅ Conversation resolution required

### Structure Branches

- ⚠️ Lenient rules (allows force pushes during restructuring)
- ✅ Still requires lint check

### Feature Branches

- ✅ Requires lint and test checks
- ✅ Requires 1 approval
- ✅ Allows force pushes (for rebasing)

### Release Branches

- ✅ Strict protection (2 approvals, code owner reviews)
- ✅ All CI checks required
- ✅ No force pushes

---

## 🧹 Branch Hygiene Automation

### Automated Cleanup Script

Use the provided PowerShell script for safe branch cleanup:

```powershell
# Preview changes (dry run)
.\scripts\git\branch-cleanup.ps1 -DryRun

# Execute cleanup (with confirmations)
.\scripts\git\branch-cleanup.ps1

# Skip confirmations (use with caution)
.\scripts\git\branch-cleanup.ps1 -SkipConfirmation

# Archive branches only (no merges/deletes)
.\scripts\git\branch-cleanup.ps1 -ArchiveOnly
```

### Manual Cleanup Checklist

1. **Archive valuable branches** before deletion
2. **Merge high-priority branches** to main
3. **Review medium-priority branches** (see `merge-review.md`)
4. **Prune stale/generic branches** after archiving

---

## 📊 Branch Status Definitions

| Status | Description | Action |
|--------|-------------|--------|
| **Active** | Recent commits, up-to-date with main | Keep working or merge |
| **Stale** | No commits in 90+ days, behind main | Review → Merge or Prune |
| **Merged** | Already merged to main | Delete branch |
| **Remote Only** | Exists only on remote | Fetch → Review → Delete |
| **Local Only** | Exists only locally | Push or delete |
| **Conflicted** | Has merge conflicts with main | Resolve conflicts or rebase |

---

## 🔍 Branch Audit & Review

### Review Report

After running branch cleanup, review `merge-review.md` for:
- Medium-priority branches requiring manual review
- Diff statistics (commits, files changed, lines)
- Merge recommendations

### Conflict Resolution

If merge conflicts occur:

```powershell
# 1. Check status
git status

# 2. Resolve conflicts manually or use mergetool
git mergetool

# 3. Complete merge
git commit

# 4. Push
git push origin main
```

---

## 🚀 Quick Reference

### Common Commands

```powershell
# List all branches
git branch -a

# Check branch status
git status

# See commits ahead/behind main
git rev-list --left-right --count main...feature/your-branch

# Fetch all remote branches
git fetch --all --prune

# Delete merged branches (local)
git branch --merged main | Where-Object { $_ -notmatch 'main|structure' } | ForEach-Object { git branch -d $_.Trim() }

# Delete merged branches (remote)
git branch -r --merged main | Where-Object { $_ -notmatch 'main|structure' } | ForEach-Object { $branch = $_.Trim().Replace('origin/', ''); git push origin --delete $branch }
```

---

## 📝 Commit Message Guidelines

Follow conventional commits:

- `feat:` New feature
- `fix:` Bug fix
- `docs:` Documentation changes
- `style:` Code style changes (formatting)
- `refactor:` Code refactoring
- `test:` Adding/updating tests
- `chore:` Maintenance tasks

Example: `feat: add RBAC implementation with permission-based UI visibility`

---

## ⚠️ Best Practices

1. **Always create branches from main**: `git checkout main && git pull`
2. **Keep branches small and focused**: One feature/fix per branch
3. **Commit often**: Small, logical commits
4. **Update from main regularly**: `git fetch origin && git rebase origin/main`
5. **Delete merged branches**: Clean up after merge
6. **Use descriptive names**: Future you will thank you
7. **Don't use tool names in branch names**: Focus on what the change does

---

## 🆘 Troubleshooting

### "Branch is behind main"

```powershell
# Rebase onto main
git checkout feature/your-branch
git fetch origin
git rebase origin/main

# Resolve conflicts if any
git mergetool
git rebase --continue

# Force push (only for feature branches)
git push --force-with-lease origin feature/your-branch
```

### "Remote branch deleted but local still exists"

```powershell
# Prune remote tracking branches
git fetch --prune

# Delete local branch
git branch -d feature/your-branch
```

### "Cannot delete branch (not fully merged)"

```powershell
# Force delete (use with caution)
git branch -D feature/your-branch
```

---

## 📚 Additional Resources

- [Git Branch Cleanup Script](./scripts/git/branch-cleanup.ps1)
- [Branch Strategy YAML](./git-branch-strategy.yaml)
- [GitHub Branch Protection](./github-branch-protection.json)
- [Merge Review Report](./merge-review.md)
- [DevOps Audit Report](./DEVOPS_AUDIT_REPORT.md)

---

**Questions?** Open an issue or contact the repository maintainer.

