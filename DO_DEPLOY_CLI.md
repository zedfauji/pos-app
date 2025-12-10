# DigitalOcean App Platform - Deploy via CLI (Works Guaranteed)

**Skip the broken UI - Deploy directly with `doctl` CLI**

---

## Step 1: Install `doctl`

### Windows (PowerShell as Admin):

```powershell
# Option 1: Using Chocolatey (if installed)
choco install doctl

# Option 2: Using winget
winget install DigitalOcean.doctl

# Option 3: Manual download
# Go to: https://github.com/digitalocean/doctl/releases/latest
# Download: doctl-x.x.x-windows-amd64.zip
# Extract and add to PATH
```

### Verify Installation:

```powershell
doctl version
```

---

## Step 2: Authenticate with DigitalOcean

```powershell
doctl auth init
```

This will:
1. Ask for your DigitalOcean API token
2. Get token from: https://cloud.digitalocean.com/account/api/tokens
3. Click "Generate New Token"
4. Give it a name (e.g., "App Platform Deploy")
5. Set expiration (or no expiration)
6. Copy the token
7. Paste it into `doctl auth init`

---

## Step 3: Validate Your App Spec

First, let's make sure the spec is valid:

```powershell
# Navigate to repo root
cd C:\Users\giris\Documents\Code\Order-Tracking-By-GPT

# Validate the spec
doctl apps spec validate --spec .do/app.yaml
```

**If you get errors**, fix them before proceeding.

---

## Step 4: Deploy Your App

```powershell
# Create and deploy the app
doctl apps create --spec .do/app.yaml
```

This will:
- Create all 9 services
- Create the database
- Configure all environment variables
- Start the deployment

**Output will show**:
- App ID
- Live URLs for each service
- Deployment status

---

## Step 5: Monitor Deployment

```powershell
# Get your app ID from the output above
$APP_ID = "your-app-id-here"

# Check deployment status
doctl apps get $APP_ID

# View logs
doctl apps logs $APP_ID --type=build
doctl apps logs $APP_ID --type=run

# List all your apps
doctl apps list
```

---

## Step 6: Update Deployments (Future)

When you push code changes:

```powershell
# If auto-deploy is enabled, it deploys automatically
# Or manually trigger:
doctl apps create-deployment $APP_ID
```

---

## Troubleshooting

### "doctl: command not found"
- Add doctl to your PATH
- Restart PowerShell

### "invalid API token"
- Generate new token: https://cloud.digitalocean.com/account/api/tokens
- Run `doctl auth init` again

### "spec validation failed"
- Check `.do/app.yaml` syntax
- Run `doctl apps spec validate --spec .do/app.yaml` for details

### "database already exists"
- Delete existing app first: `doctl apps delete $APP_ID`
- Or change database name in spec

---

## Complete Command Reference

```powershell
# Authenticate
doctl auth init

# Validate spec
doctl apps spec validate --spec .do/app.yaml

# Create app
doctl apps create --spec .do/app.yaml

# List apps
doctl apps list

# Get app details
doctl apps get $APP_ID

# View logs
doctl apps logs $APP_ID

# Delete app (if needed)
doctl apps delete $APP_ID

# Update app spec
doctl apps update $APP_ID --spec .do/app.yaml
```

---

**This CLI method bypasses ALL UI issues. It's the official, reliable way to deploy.**

