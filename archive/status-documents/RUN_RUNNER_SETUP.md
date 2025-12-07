# Run GitHub Runner Setup

## ⚠️ Security Warning

**You've shared your GitHub token in this conversation. After setup, you should:**
1. Go to GitHub → Settings → Developer settings → Personal access tokens
2. Revoke the token: `AAKACBI5PLUO435LC2VXPKDJGRXLI`
3. Generate a new token for future use

## Run Setup Script

**Open PowerShell as Administrator** and run:

```powershell
cd C:\Users\giris\Documents\Code\Order-Tracking-By-GPT

.\setup-github-runner.ps1 `
    -RunnerName "windows-runner-01" `
    -GitHubToken "AAKACBI5PLUO435LC2VXPKDJGRXLI" `
    -GitHubRepo "zedfauji/pos-app" `
    -RunnerVersion "2.329.0" `
    -InstallPath "C:\actions-runner"
```

## What the Script Will Do

1. ✅ Create installation directory (`C:\actions-runner`)
2. ✅ Download runner (version 2.329.0)
3. ✅ Verify checksum (using your provided hash)
4. ✅ Extract runner files
5. ✅ Configure runner with your repository
6. ⚠️ Ask if you want to install as Windows Service (recommended: Yes)
7. ⚠️ Ask if you want to start the service (recommended: Yes)

## After Setup

1. **Verify in GitHub:**
   - Go to: `https://github.com/zedfauji/pos-app/settings/actions/runners`
   - You should see "windows-runner-01" listed

2. **Revoke the token** (security best practice)

3. **Test the runner:**
   - Create a test workflow or use the example workflow
   - The runner should pick up jobs automatically

## Troubleshooting

If the script fails:
- Make sure you're running as Administrator
- Check internet connection
- Verify the token has `repo` scope
- Check Windows Defender/firewall isn't blocking
