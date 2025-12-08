# Quick Start: Deploy Developer Portal

## Immediate Steps

### 1. Enable GitHub Pages

1. Go to: `https://github.com/YOUR_USERNAME/Order-Tracking-By-GPT/settings/pages`
2. Under **Source**, select: **GitHub Actions**
3. Click **Save**

### 2. Update Configuration

Edit `docs/docusaurus.config.js` and replace:
- `your-username` → Your actual GitHub username
- Keep `Order-Tracking-By-GPT` as the project name (or update if different)

### 3. Trigger Workflow

The workflow should trigger automatically, but you can manually trigger it:
1. Go to **Actions** tab
2. Click **Deploy Documentation**
3. Click **Run workflow** → **Run workflow**

### 4. Wait for Deployment

- Build time: ~2-3 minutes
- Deployment time: ~1-2 minutes
- Total: ~3-5 minutes

### 5. Access Your Portal

After deployment completes:
- URL: `https://YOUR_USERNAME.github.io/Order-Tracking-By-GPT/`
- Or check **Settings** → **Pages** for the exact URL

## Need Help?

If you provide your GitHub username, I can update the config file for you automatically.
