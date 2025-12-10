# Render Migration - Dashboard Guide (Windows Compatible)

Since Render CLI has limited Windows support, this guide uses the Render Dashboard.

## 🔑 Prerequisites

1. **Render Account**: Sign up at https://render.com
2. **GitHub Repository**: Your code must be on GitHub
3. **Database Dump**: Have your `postgres-dump-*.sql` file ready

## 📋 Step-by-Step Migration

### Step 1: Connect GitHub to Render

1. Go to https://dashboard.render.com
2. Click **"New"** → **"Blueprint"**
3. Click **"Connect GitHub"**
4. Authorize Render to access your repositories
5. Select your repository: `Order-Tracking-By-GPT`
6. Click **"Connect"**

### Step 2: Deploy from Blueprint

1. In the Blueprint screen, Render should detect `render.yaml`
2. Review the services that will be created:
   - 9 Web Services (TablesApi, OrderApi, etc.)
   - 1 PostgreSQL Database
3. Click **"Apply"**
4. Wait for services to be created (5-10 minutes)

### Step 3: Configure Database

1. Go to **Dashboard** → **Databases**
2. Find `magidesk-pos` database
3. Click on it to view connection details
4. Note the **Internal Database URL** (format: `postgres://user:pass@host:port/dbname`)

### Step 4: Import Database Dump

**Option A: Using psql (if available)**
```powershell
# Get connection string from Render Dashboard
$env:PGPASSWORD = "your-password"
psql -h <render-db-host> -p 5432 -U <user> -d postgres -f dumps\postgres-dump-*.sql
```

**Option B: Using pgAdmin or DBeaver**
1. Download pgAdmin or DBeaver
2. Connect using database connection string from Render
3. Use Import/Restore feature to import the SQL dump

**Option C: Using Render Dashboard (if available)**
- Some Render plans include backup/restore via dashboard

### Step 5: Link Database to Services

For each service (TablesApi, OrderApi, etc.):

1. Go to **Dashboard** → **Web Services** → Select service
2. Go to **Environment** tab
3. Click **"Link Database"** → Select `magidesk-pos`
4. This automatically sets `DATABASE_URL` environment variable

### Step 6: Configure Environment Variables

For each service, add these environment variables:

1. Go to service → **Environment** tab
2. Add variables:
   - `ASPNETCORE_URLS` = `http://0.0.0.0:10000`
   - `ASPNETCORE_ENVIRONMENT` = `Production`
   - `ConnectionStrings__Postgres` = (use `${db.magidesk-pos.DATABASE_URL}` or paste connection string)

### Step 7: Enable Auto-Suspend (Cost Optimization)

For each service:

1. Go to service → **Settings** tab
2. Scroll to **"Auto-Suspend"**
3. Enable with **15 minutes** idle timeout
4. Click **"Save Changes"**

### Step 8: Set Budget Alert

1. Go to **Account** → **Billing**
2. Click **"Set Budget Alert"**
3. Set amount: **$50 USD/month**
4. Add email for alerts
5. Click **"Save"**

### Step 9: Verify Deployment

1. Go to each service → Check **"Logs"** tab
2. Verify deployment succeeded
3. Check service URL (format: `https://<service-name>.onrender.com`)
4. Test health endpoint: `https://<service-name>.onrender.com/health`

### Step 10: Update Frontend Configuration

Update `src/Frontend/frontend/appsettings.json`:

```json
{
  "Api": {
    "BaseUrl": "https://tablesapi.onrender.com"
  },
  "MenuApi": {
    "BaseUrl": "https://menuapi.onrender.com"
  },
  "OrderApi": {
    "BaseUrl": "https://orderapi.onrender.com"
  },
  "PaymentApi": {
    "BaseUrl": "https://paymentapi.onrender.com"
  },
  "CustomerApi": {
    "BaseUrl": "https://customerapi.onrender.com"
  },
  "DiscountApi": {
    "BaseUrl": "https://discountapi.onrender.com"
  },
  "InventoryApi": {
    "BaseUrl": "https://inventoryapi.onrender.com"
  },
  "SettingsApi": {
    "BaseUrl": "https://settingsapi.onrender.com"
  },
  "UsersApi": {
    "BaseUrl": "https://usersapi.onrender.com"
  }
}
```

## 🔄 Rollback Procedure

If you need to rollback to GCP:

1. **Pause Render Services**:
   - Go to each service → **Settings** → **Suspend Service**

2. **Resume GCP Services**:
   ```powershell
   # For each service
   gcloud run services update magidesk-tables --project bola8pos --region northamerica-south1 --min-instances 1
   # Repeat for all services
   ```

3. **Update DNS** (if using custom domain):
   - Point DNS back to GCP Cloud Run URLs

## 📊 Service URLs Pattern

After deployment, your services will be at:
- `https://<service-name>-<random-id>.onrender.com`
- Or if you set custom domains: `https://<service-name>.yourdomain.com`

## 🆘 Troubleshooting

### Services not deploying
- Check **Logs** tab for build errors
- Verify `render.yaml` syntax is correct
- Ensure GitHub repo is connected

### Database connection issues
- Verify database is linked to service
- Check environment variable names (case-sensitive)
- Test connection string separately

### Auto-suspend not working
- Verify it's enabled in Settings
- Check service is on Starter plan or higher
- First request after suspend may take 10-30 seconds

## 💡 Tips

1. **Use Blueprint**: Deploying from `render.yaml` is fastest
2. **Monitor Logs**: Watch first deployment in Logs tab
3. **Test Health Endpoints**: Use `/health` to verify services
4. **Save Connection Strings**: Keep database connection info safe
5. **Enable PITR**: Point-in-time recovery is enabled by default

## 📞 Support

- Render Docs: https://render.com/docs
- Render Status: https://status.render.com
- Community: https://community.render.com

