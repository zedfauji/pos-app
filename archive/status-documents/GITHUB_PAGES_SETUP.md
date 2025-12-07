# GitHub Pages Setup Guide

## Steps to Deploy Developer Portal

### Step 1: Configure GitHub Pages

1. Go to your GitHub repository
2. Click **Settings** → **Pages**
3. Under **Source**, select:
   - **Source:** `GitHub Actions`
4. Click **Save**

### Step 2: Check GitHub Actions Workflow

1. Go to **Actions** tab in your repository
2. Look for "Deploy Documentation" workflow
3. If it hasn't run, you can:
   - Wait for it to trigger automatically (on next push)
   - Or manually trigger it: Click "Deploy Documentation" → "Run workflow"

### Step 3: Update Docusaurus Configuration

The `docs/docusaurus.config.js` file has placeholder values that need to be updated:

**Current placeholders:**
- `organizationName: 'your-username'` - Replace with your GitHub username
- `url: 'https://your-username.github.io'` - Replace with your GitHub Pages URL
- `editUrl: 'https://github.com/your-username/Order-Tracking-By-GPT/tree/main/docs/'` - Update path

**To update:**

1. Open `docs/docusaurus.config.js`
2. Replace `your-username` with your actual GitHub username
3. If your repository is under an organization, use the organization name
4. Commit and push the changes

### Step 4: Verify Deployment

After the GitHub Actions workflow completes:

1. Go to **Actions** tab
2. Find the completed "Deploy Documentation" workflow run
3. Click on it to see the deployment URL
4. Or go to **Settings** → **Pages** to see the published URL

The URL will typically be:
- `https://your-username.github.io/Order-Tracking-By-GPT/`

### Step 5: Test Locally (Optional)

Before waiting for GitHub Pages, you can test locally:

```powershell
cd docs
npm install
npm start
```

Visit `http://localhost:3000` to see the documentation.

## Troubleshooting

### Workflow Not Running

If the workflow doesn't trigger:
1. Check that `.github/workflows/docs.yml` exists
2. Verify the workflow file syntax is correct
3. Check repository settings → Actions → General → Workflow permissions

### Build Fails

If the build fails:
1. Check the Actions tab for error messages
2. Common issues:
   - Missing `package-lock.json` (run `npm install` in docs folder locally)
   - Node version mismatch
   - Missing dependencies

### Pages Not Showing

If Pages is configured but not showing:
1. Wait a few minutes (deployment can take 1-5 minutes)
2. Check the Actions tab for deployment status
3. Verify the workflow completed successfully
4. Clear browser cache and try again

## Quick Fix: Update Config Now

If you want to update the config file with your GitHub username, I can help. Just provide:
- Your GitHub username (or organization name)
- Your repository name (if different from "Order-Tracking-By-GPT")
