# GitHub Actions Self-Hosted Runner Setup Guide

## Overview

This guide helps you set up a GitHub Actions self-hosted runner on Windows x64 for your repository.

## Prerequisites

- Windows x64 machine
- Administrator privileges
- PowerShell 5.1 or later
- Internet connection
- GitHub Personal Access Token (PAT) with `repo` scope

## Quick Setup

### Step 1: Generate GitHub Token

1. Go to GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic)
2. Click "Generate new token (classic)"
3. Name: "Runner Setup Token"
4. Select scopes: `repo` (full control)
5. Click "Generate token"
6. **Copy the token immediately** (you won't see it again)

### Step 2: Run Setup Script

```powershell
# Run as Administrator
cd C:\Users\giris\Documents\Code\Order-Tracking-By-GPT

# Run the setup script
.\setup-github-runner.ps1 `
    -RunnerName "windows-runner-01" `
    -GitHubToken "YOUR_GITHUB_TOKEN" `
    -GitHubRepo "zedfauji/pos-app" `
    -RunnerVersion "2.329.0" `
    -InstallPath "C:\actions-runner"
```

### Step 3: Verify Installation

1. Go to your GitHub repository
2. Settings → Actions → Runners
3. You should see your runner listed

## Manual Setup (Alternative)

If you prefer to set up manually:

### 1. Download Runner

```powershell
$version = "2.329.0"
$zipFile = "actions-runner-win-x64-$version.zip"
$zipPath = "$env:TEMP\$zipFile"
$url = "https://github.com/actions/runner/releases/download/v$version/$zipFile"

Invoke-WebRequest -Uri $url -OutFile $zipPath
```

### 2. Verify Checksum

```powershell
$expectedHash = "f60be5ddf373c52fd735388c3478536afd12bfd36d1d0777c6b855b758e70f25"
$actualHash = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash.ToUpper()

if ($actualHash -ne $expectedHash.ToUpper()) {
    throw "Checksum verification failed!"
}
Write-Host "Checksum verified"
```

### 3. Extract and Configure

```powershell
$installPath = "C:\actions-runner"
Expand-Archive -Path $zipPath -DestinationPath $installPath -Force
Set-Location $installPath

# Get registration token from GitHub
# Go to: Settings → Actions → Runners → New self-hosted runner
# Copy the registration token

.\config.cmd --url https://github.com/zedfauji/pos-app --token YOUR_TOKEN --name windows-runner-01
```

### 4. Install as Service

```powershell
.\svc.cmd install
.\svc.cmd start
```

## Runner Management

### Start Runner

```powershell
cd C:\actions-runner
.\run.cmd
```

### Stop Runner

Press `Ctrl+C` in the runner window, or:

```powershell
cd C:\actions-runner
.\svc.cmd stop
```

### Remove Runner

```powershell
cd C:\actions-runner
.\config.cmd remove --token YOUR_TOKEN
```

### Uninstall Service

```powershell
cd C:\actions-runner
.\svc.cmd uninstall
```

## Using Self-Hosted Runner in Workflows

Update your workflow files to use the self-hosted runner:

```yaml
jobs:
  build:
    runs-on: self-hosted  # Use self-hosted runner
    # or
    runs-on: [self-hosted, windows, x64]  # With labels
```

## Labels

You can add labels to your runner for better organization:

```powershell
cd C:\actions-runner
.\config.cmd --url https://github.com/zedfauji/pos-app --token YOUR_TOKEN --name windows-runner-01 --labels windows,x64,dotnet
```

Then use in workflows:

```yaml
runs-on: [self-hosted, windows, x64, dotnet]
```

## Security Considerations

1. **Token Security**: Never commit tokens to repository
2. **Network**: Consider firewall rules for runner
3. **Permissions**: Runner has access to repository code
4. **Isolation**: Consider running in VM or container
5. **Updates**: Keep runner updated regularly

## Troubleshooting

### Runner Not Appearing

1. Check runner is running: `Get-Service actions.runner.*`
2. Check logs: `C:\actions-runner\_diag\Runner_*.log`
3. Verify token is valid
4. Check network connectivity

### Workflow Not Using Runner

1. Verify `runs-on: self-hosted` in workflow
2. Check runner is online in GitHub Settings
3. Verify runner has required labels

### Runner Offline

1. Check service status: `Get-Service actions.runner.*`
2. Restart service: `.\svc.cmd restart`
3. Check logs for errors
4. Verify GitHub connectivity

## Updating Runner

```powershell
cd C:\actions-runner
.\config.cmd remove --token YOUR_TOKEN
# Download new version
# Extract and configure again
```

## Resources

- [GitHub Actions Runner Documentation](https://docs.github.com/en/actions/hosting-your-own-runners)
- [Runner Releases](https://github.com/actions/runner/releases)
- [Runner Configuration](https://docs.github.com/en/actions/hosting-your-own-runners/managing-self-hosted-runners)
