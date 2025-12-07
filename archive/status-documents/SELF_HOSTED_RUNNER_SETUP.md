# Self-Hosted Runner Setup for GitHub Pages

## ✅ Workflow Updated

The `.github/workflows/docs.yml` has been updated to use your self-hosted Windows runner (`windows-runner-01`).

## Prerequisites on Windows Runner

Your Windows runner needs:

1. **Node.js 18+** installed
   ```powershell
   # Check if Node.js is installed
   node --version
   
   # If not installed, download from: https://nodejs.org/
   # Or use Chocolatey: choco install nodejs
   ```

2. **npm** (comes with Node.js)
   ```powershell
   npm --version
   ```

3. **Git** (should already be installed)
   ```powershell
   git --version
   ```

## Verify Runner is Online

1. Go to: `https://github.com/zedfauji/pos-app/settings/actions/runners`
2. Check that `windows-runner-01` shows as **Online** (green dot)

## Trigger the Workflow

The workflow will automatically trigger when:
- You push changes to `docs/**` files
- You push changes to `.github/workflows/docs.yml`

Or manually trigger:
1. Go to **Actions** tab
2. Click **Deploy Documentation**
3. Click **Run workflow** → **Run workflow**

## Workflow Steps on Self-Hosted Runner

1. ✅ **Checkout** - Gets code from repository
2. ✅ **Setup Node.js** - Ensures Node.js 18 is available
3. ✅ **Install dependencies** - Runs `npm ci` in `docs/` folder
4. ✅ **Build documentation** - Runs `npm run build`
5. ✅ **Upload artifact** - Uploads build to GitHub
6. ✅ **Deploy to GitHub Pages** - Deploys the built site

## Troubleshooting

### Runner Not Picking Up Jobs

1. Check runner is online in GitHub Settings
2. Check runner service is running:
   ```powershell
   Get-Service actions.runner.*
   ```
3. Restart runner service:
   ```powershell
   cd C:\actions-runner
   .\svc.cmd restart
   ```

### Node.js Not Found

If workflow fails with "Node.js not found":
1. Install Node.js 18+ on the Windows machine
2. Restart the runner service
3. Verify: `node --version` in PowerShell

### Build Fails

Check the workflow logs in GitHub Actions tab for specific errors.

Common issues:
- Missing `package-lock.json` - Run `npm install` in `docs/` folder locally first
- Path issues - Windows paths should work, but check logs
- Permissions - Runner service needs write permissions

### Deploy Fails

GitHub Pages deployment might have issues on self-hosted runners. If it fails:
- Check GitHub Pages settings (must be set to "GitHub Actions")
- Verify repository has Pages enabled
- Check deployment logs in Actions tab

## Manual Build Test

To test locally on the runner machine:

```powershell
cd C:\actions-runner\_work\pos-app\pos-app
cd docs
npm install
npm run build
```

The built files will be in `docs/build/` directory.

## Next Steps

1. ✅ Ensure Node.js 18+ is installed on Windows runner
2. ✅ Verify runner is online
3. ✅ Push a change to trigger the workflow
4. ✅ Monitor the workflow in Actions tab
