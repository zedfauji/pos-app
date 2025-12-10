# Apply GitHub Branch Protection Rules

## Quick Setup for Main Branch

Replace `OWNER/REPO` with your repository (e.g., `zedfauji/pos-app`):

```bash
gh api repos/OWNER/REPO/branches/main/protection --method PUT --input github-branch-protection-main.json
```

## Example

```bash
gh api repos/zedfauji/pos-app/branches/main/protection --method PUT --input github-branch-protection-main.json
```

## File Structure

- **`github-branch-protection-main.json`** - Ready-to-use JSON for main branch
- **`github-branch-protection-structure.json`** - Ready-to-use JSON for structure/* branches
- **`github-branch-protection.json`** - Reference/documentation file (not for direct API use)

## Apply Protection to Other Branches

### Structure Branch
```bash
gh api repos/OWNER/REPO/branches/structure/restructure-v1/protection --method PUT --input github-branch-protection-structure.json
```

## Alternative: GitHub UI

1. Go to your repository on GitHub
2. Settings > Branches
3. Click "Add rule" or edit existing "main" rule
4. Configure settings matching `github-branch-protection-main.json`
5. Save changes

## Verify Protection

```bash
gh api repos/OWNER/REPO/branches/main/protection
```

## Troubleshooting

**Error: "enforce_admins", "required_pull_request_reviews", etc. weren't supplied**
- Make sure you're using `github-branch-protection-main.json` (not the reference file)
- All required fields must be present in the JSON

**Error: "Not Found"**
- Verify you have admin access to the repository
- Check the repository name is correct (OWNER/REPO format)

