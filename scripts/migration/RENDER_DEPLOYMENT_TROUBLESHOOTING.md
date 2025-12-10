# Render Deployment Troubleshooting Guide

## 🔴 Current Issues & Solutions

### Issue 1: "secret name already in use: docker-hub-pull-config"

**Problem**: Some services show this error, indicating a Docker Hub pull secret conflict.

**Solution**:
1. Go to Render Dashboard → **Account** → **Secrets**
2. Check if `docker-hub-pull-config` exists
3. Either:
   - **Option A**: Remove the secret if not needed (if using public images)
   - **Option B**: Rename it to be service-specific (e.g., `docker-hub-pull-config-tablesapi`)
   - **Option C**: Use it only for services that need it

**Note**: If you're using public .NET base images (`mcr.microsoft.com/dotnet/*`), you don't need Docker Hub secrets.

### Issue 2: "deploy failed"

**Problem**: Services failed to deploy, likely due to Docker build errors.

**Solution**: 
1. **Check Build Logs**:
   - Go to Render Dashboard → Service → **Logs** tab
   - Look for build errors (usually path-related)

2. **Verify Dockerfile Paths**:
   - All Dockerfiles now use `COPY ./ ./` pattern
   - Build context is repo root (`dockerContext: .`)
   - Paths inside container: `/src/src/Backend/<Service>`

3. **Common Build Errors**:
   - **"file not found"**: Check if `src/Backend/<Service>/<Service>.csproj` exists
   - **"directory not found"**: Verify `src/Shared/shared/` exists
   - **"restore failed"**: Check if `Directory.Build.props` is accessible

4. **Manual Fix**:
   ```powershell
   # Test Docker build locally
   docker build -f src/Backend/TablesApi/Dockerfile -t test-tablesapi .
   ```

### Issue 3: Database Connection

**Problem**: Services can't connect to database.

**Solution**:
1. Verify database is created: `magidesk-pos`
2. Check environment variable: `ConnectionStrings__Postgres`
3. Ensure database is linked to service:
   - Service → **Environment** → **Link Database** → Select `magidesk-pos`
4. Verify connection string format matches your code expectations

## 🔧 Manual Deployment Steps

If automatic deployment fails:

### Step 1: Create Services Manually

For each service:
1. Go to Render Dashboard → **New** → **Web Service**
2. Connect GitHub repository
3. Configure:
   - **Name**: `TablesApi` (or service name)
   - **Runtime**: `Docker`
   - **Dockerfile Path**: `src/Backend/TablesApi/Dockerfile`
   - **Docker Build Context**: `.` (repo root)
   - **Plan**: `Starter`
   - **Region**: `Oregon`

### Step 2: Set Environment Variables

For each service, add:
- `ASPNETCORE_URLS` = `http://0.0.0.0:10000`
- `ASPNETCORE_ENVIRONMENT` = `Production`
- `ConnectionStrings__Postgres` = (link database or paste connection string)

### Step 3: Link Database

1. Go to service → **Environment** tab
2. Click **"Link Database"**
3. Select `magidesk-pos`
4. This auto-sets `DATABASE_URL` environment variable

### Step 4: Import Database

After database is created:
```powershell
# Get connection string from Render Dashboard
# Format: postgres://user:pass@host:port/dbname
$env:PGPASSWORD = "your-password"
psql -h <render-db-host> -p 5432 -U <user> -d magideskdb -f dumps\postgres-dump-*.sql
```

## 📋 Verification Checklist

- [ ] All 9 services created in Render
- [ ] Database `magidesk-pos` created
- [ ] All services linked to database
- [ ] Environment variables set correctly
- [ ] Database dump imported
- [ ] Services deploying successfully (check Logs)
- [ ] Health endpoints responding (`/health`)
- [ ] Auto-suspend enabled (15 min timeout)
- [ ] Budget alert set ($50/month)

## 🚨 Common Errors & Fixes

### Error: "Cannot find project file"
**Fix**: Verify `dockerContext: .` is set in render.yaml and Dockerfile paths are correct

### Error: "Connection refused" (database)
**Fix**: Ensure database is linked and connection string is correct

### Error: "Build timeout"
**Fix**: Increase build timeout in service settings or optimize Dockerfile

### Error: "Port already in use"
**Fix**: Ensure `ASPNETCORE_URLS=http://0.0.0.0:10000` is set (Render uses port 10000)

## 📞 Next Steps

1. **Trigger New Deployments**:
   - Go to each service → **Manual Deploy** → **Deploy latest commit**
   - Or push a new commit to trigger auto-deploy

2. **Monitor Build Logs**:
   - Watch the first deployment carefully
   - Fix any errors that appear

3. **Test Endpoints**:
   - Once deployed, test: `https://<service-name>.onrender.com/health`
   - Verify all services are responding

4. **Update Frontend**:
   - Update `appsettings.json` with Render URLs
   - Test frontend connectivity

## 💡 Tips

- **Build Context**: Always use `.` (repo root) for dockerContext
- **Port**: Render uses port 10000 internally, but expose 8080 in Dockerfile
- **Health Checks**: Ensure `/health` endpoint exists and returns 200
- **Logs**: Check Render logs frequently during initial deployment
- **Secrets**: Avoid Docker Hub secrets if using public images

