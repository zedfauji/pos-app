# Render Migration Guide - GCP Cloud Run → Render

Complete migration guide for moving MagiDesk POS from GCP Cloud Run to Render for cost optimization.

## 📋 Prerequisites

1. **Render CLI** installed:
   ```powershell
   winget install RenderCLI.Render
   # Or: https://render.com/docs/cli
   ```

2. **GitHub Repository** connected to Render (auto-deploy)

3. **Database Dump** from GCP (already created: `dumps/postgres-dump-*.sql`)

4. **Render Account** with billing enabled

## 🚀 Quick Start

### 1. Dry-Run Migration (Preview)

```powershell
.\scripts\migration\render-migration.ps1 -DryRun
```

This will show you exactly what will happen without making any changes.

### 2. Execute Migration

```powershell
# Full migration
.\scripts\migration\render-migration.ps1

# With custom database dump
.\scripts\migration\render-migration.ps1 -DumpFile "dumps\your-dump.sql"
```

### 3. Configure Cost Optimization

```powershell
.\scripts\migration\render-cost-optimizer.ps1
```

Then manually:
- Enable auto-suspend in Render Dashboard (15 min idle timeout)
- Set budget alert at $50/month

### 4. Rollback (if needed)

```powershell
# Pause Render and resume GCP
.\scripts\migration\render-rollback.ps1 -PauseRender -ResumeGCP
```

## 📁 File Structure

```
├── render.yaml                      # Render blueprint (auto-creates all services)
├── scripts/migration/
│   ├── render-migration.ps1         # Main migration script
│   ├── render-cost-optimizer.ps1    # Cost optimization setup
│   ├── render-rollback.ps1          # Blue-green rollback script
│   └── render-env.json              # Environment variables template
└── dumps/
    └── postgres-dump-*.sql          # Database dump file
```

## 🔧 Manual Steps

### 1. Connect GitHub to Render

1. Go to https://dashboard.render.com
2. Click "New" → "Blueprint"
3. Connect your GitHub repository
4. Select `render.yaml` file
5. Click "Apply"

### 2. Import Database Dump

After database is created:
```powershell
# Get connection string from Render Dashboard
$env:PGPASSWORD = "your-password"
psql -h <render-db-host> -p 5432 -U <user> -d postgres -f dumps\postgres-dump-*.sql
```

### 3. Update Frontend Configuration

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
  }
  // ... update all API endpoints
}
```

## 💰 Cost Comparison

### Render (with auto-suspend)
- **9 Web Services (Starter)**: $7 × 9 = $63/month
- **1 Database (Basic-1GB)**: $7/month
- **Estimated Total**: $70/month
- **With Auto-Suspend**: ~$35-50/month (estimated 50% savings)

### GCP Cloud Run (current)
- **Cloud Run**: ~$40-80/month (based on requests/memory)
- **Cloud SQL (db-f1-micro)**: ~$10-20/month
- **Estimated Total**: $80-120/month

### Savings
- **Estimated**: $30-70/month (37-58% reduction)
- **Annual**: $360-840/year

## 🔄 Blue-Green Deployment Strategy

1. **Deploy to Render** (green environment)
2. **Test thoroughly** on Render URLs
3. **Gradually shift traffic** via DNS/load balancer
4. **Monitor both environments** for 1-2 weeks
5. **Complete cutover** once verified
6. **Keep GCP paused** for 30 days as backup
7. **Decommission GCP** after successful migration

## 🚨 Rollback Procedure

If issues occur:

```powershell
# 1. Pause Render services
.\scripts\migration\render-rollback.ps1 -PauseRender

# 2. Resume GCP services
.\scripts\migration\render-rollback.ps1 -ResumeGCP

# 3. Update DNS back to GCP
# (Manual step - update DNS records)

# 4. Verify GCP services responding
```

## 📊 Monitoring

### Render Dashboard
- Service health: https://dashboard.render.com
- Logs: View real-time logs for each service
- Metrics: CPU, memory, request rates

### Cost Monitoring
- Billing: https://dashboard.render.com/account/billing
- Set budget alerts at $50/month
- Review weekly

## ⚙️ Environment Variables

All services automatically get:
- `ASPNETCORE_URLS=http://0.0.0.0:10000`
- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__Postgres` (from database service reference)

Service-specific variables are in `render-env.json`.

## 🔍 Troubleshooting

### Services not deploying
- Check GitHub repository is connected
- Verify `render.yaml` syntax is correct
- Check build logs in Render Dashboard

### Database connection issues
- Verify database service reference in `render.yaml`
- Check connection string format
- Ensure database is in same region as services

### Auto-suspend not working
- Enable via Render Dashboard (Settings → Auto-Suspend)
- CLI may not support this feature yet

## 📝 Notes

- **Auto-suspend**: Services resume automatically on first request (10-30 second delay)
- **Database**: PITR (Point-in-Time Recovery) enabled for backups
- **Health Checks**: All services have `/health` endpoint configured
- **Region**: All services deployed to `oregon` (can be changed in `render.yaml`)

## 🆘 Support

- Render Docs: https://render.com/docs
- Render Status: https://status.render.com
- GCP Cloud Run Docs: https://cloud.google.com/run/docs

