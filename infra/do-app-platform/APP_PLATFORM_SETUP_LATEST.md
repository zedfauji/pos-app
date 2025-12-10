# DigitalOcean App Platform Setup - Latest Documentation (2025)

**Updated**: December 10, 2025  
**Based on**: Official DigitalOcean App Platform Documentation

---

## 🔍 Key Changes in 2025

### What's New:
1. **App Spec Location**: Place app spec in `.do/app.yaml` (not root or custom directory)
2. **UI-First Approach**: Primary method is through the web interface
3. **Auto-Detection**: DO automatically detects Dockerfiles and components
4. **Monorepo Support**: Specify `source_dir` for each component
5. **Pay-as-You-Go Pricing**: More flexible, no tier restrictions
6. **CPU-Based Autoscaling**: Available for all services
7. **Environment Tagging**: Tag apps with Development/Staging/Production

---

## 📋 Setup Methods

### Method 1: Using App Spec (`.do/app.yaml`) - RECOMMENDED

**This is the modern approach** - DigitalOcean automatically detects `.do/app.yaml` in your repo.

#### Step 1: Create App Spec File

We've created `.do/app.yaml` in the repo root with all 9 services configured.

**Key points**:
- File must be at: `.do/app.yaml` (not `do-app.yaml` or anywhere else)
- DO automatically detects this file when you connect the repo
- All services are configured with correct source directories

#### Step 2: Connect Repository

1. Go to: https://cloud.digitalocean.com/apps
2. Click **"Create App"**
3. Select **"GitHub"** as Git provider
4. Repository: `zedfauji/pos-app`
5. Branch: `feature/revamp-2025-enterprise-ui`
6. **Source directories**: Leave **EMPTY** (DO will read from `.do/app.yaml`)
7. Click **"Next"**

#### Step 3: Review Auto-Detected Components

DigitalOcean should automatically:
- ✅ Detect `.do/app.yaml`
- ✅ Create database component (`magidesk-postgres`)
- ✅ Create all 9 service components
- ✅ Configure environment variables
- ✅ Set up source directories

**Verify**:
- 1 Database component
- 9 Service components
- All source directories correct (`src/Backend/ServiceName`)
- All environment variables set

#### Step 4: Deploy

1. Review all components
2. Click **"Create Resources"** or **"Deploy"**
3. Monitor deployment in the dashboard

---

### Method 2: Manual UI Configuration (If Auto-Detection Fails)

If `.do/app.yaml` isn't detected or you prefer manual setup:

#### Step 1: Initial Setup
- **Git Provider**: GitHub ✅
- **Repository**: `zedfauji/pos-app` ✅
- **Branch**: `feature/revamp-2025-enterprise-ui` ✅
- **Source directories**: **EMPTY** (let DO scan from root)
- Click **"Next"**

#### Step 2: Add Database Component

1. Click **"Add Resource"** or **"Add Component"**
2. Select **"Database"**
3. Configure:
   - **Database Engine**: PostgreSQL
   - **Version**: 17
   - **Plan**: **db-s-dev-database** ($15/mo)
   - **Name**: `magidesk-postgres`
   - **Production**: Unchecked (for dev)
4. Click **"Add Database"**

#### Step 3: Add Service Components (9 times)

For each service (TablesApi, OrderApi, PaymentApi, MenuApi, CustomerApi, DiscountApi, InventoryApi, SettingsApi, UsersApi):

1. Click **"Add Resource"** → **"Service"** or **"Worker"**
2. Configure:

   **Basic Settings**:
   - **Name**: `tables-api` (lowercase, hyphenated)
   - **Source Directory**: `src/Backend/TablesApi`
   - **Build Command**: (leave empty - Docker handles it)
   - **Run Command**: `dotnet TablesApi.dll`

   **Docker Settings**:
   - **Dockerfile Path**: `Dockerfile` (relative to source directory)
   - **HTTP Port**: `8080`

   **Instance Settings**:
   - **Instance Size**: `basic-xxs` (Free tier for dev)
   - **Instance Count**: `1`
   - **Autoscaling**: Disabled (for dev)

   **Environment Variables**:
   Click **"Edit"** or **"Add Variable"**, add:
   ```
   ASPNETCORE_URLS = http://0.0.0.0:8080
   ConnectionStrings__Postgres = ${magidesk-postgres.DATABASE_URL}
   ASPNETCORE_ENVIRONMENT = Development
   ```

   **Note**: `${magidesk-postgres.DATABASE_URL}` is a DO variable reference - it automatically resolves to the database connection string.

3. Click **"Add Service"** or **"Save"**

#### Step 4: Repeat for All 9 Services

| Service | Component Name | Source Directory | Run Command |
|---------|---------------|------------------|-------------|
| TablesApi | `tables-api` | `src/Backend/TablesApi` | `dotnet TablesApi.dll` |
| OrderApi | `order-api` | `src/Backend/OrderApi` | `dotnet OrderApi.dll` |
| PaymentApi | `payment-api` | `src/Backend/PaymentApi` | `dotnet PaymentApi.dll` |
| MenuApi | `menu-api` | `src/Backend/MenuApi` | `dotnet MenuApi.dll` |
| CustomerApi | `customer-api` | `src/Backend/CustomerApi` | `dotnet CustomerApi.dll` |
| DiscountApi | `discount-api` | `src/Backend/DiscountApi` | `dotnet DiscountApi.dll` |
| InventoryApi | `inventory-api` | `src/Backend/InventoryApi` | `dotnet InventoryApi.dll` |
| SettingsApi | `settings-api` | `src/Backend/SettingsApi` | `dotnet SettingsApi.dll` |
| UsersApi | `users-api` | `src/Backend/UsersApi` | `dotnet UsersApi.dll` |

---

## 🔧 Important Configuration Details

### Docker Configuration

Each service needs:
- **Source Directory**: `src/Backend/ServiceName`
- **Dockerfile Path**: `Dockerfile` (relative to source directory)
- **Docker Context**: Automatically set to source directory
- **HTTP Port**: `8080`

### Environment Variables (All Services)

```env
ASPNETCORE_URLS=http://0.0.0.0:8080
ConnectionStrings__Postgres=${magidesk-postgres.DATABASE_URL}
ASPNETCORE_ENVIRONMENT=Development
```

**Critical**: The database variable `${magidesk-postgres.DATABASE_URL}` must match the exact database component name you created.

### Instance Sizes

- **Development**: `basic-xxs` (Free tier - $0/mo)
- **Production**: `basic-xs` ($5/mo) or `basic-s` ($12/mo)

### Database Plans

- **Development**: `db-s-dev-database` ($15/mo) - 1 vCPU, 1GB RAM, 10GB SSD
- **Production**: `db-s-1vcpu-1gb` ($15/mo) or higher

---

## 💰 Cost Breakdown

### Current Setup (Development)
- **Database**: db-s-dev-database = **$15/mo**
- **9 Services**: basic-xxs × 9 = **$0/mo** (free tier)
- **Total**: **$15/mo**

### Comparison to Render
- **Render**: $86.50/mo (9 services × $7 + DB $20)
- **DigitalOcean**: $15/mo
- **Savings**: **$71.50/mo (83% reduction)**

---

## 🚀 Post-Deployment: Import Database

After deployment:

1. **Get Database Connection String**:
   - Go to App Platform dashboard
   - Click on `magidesk-postgres` database
   - Copy the connection string (format: `postgresql://user:pass@host:port/db`)

2. **Import Dump**:
   ```bash
   # Using psql
   psql "YOUR_DATABASE_CONNECTION_STRING" < dumps/postgres-dump-20251209-210039.sql
   
   # Or using DO's web SQL console (if available)
   ```

---

## 🔍 Troubleshooting

### "No components detected"

**Cause**: DO is looking in wrong directory or can't find `.do/app.yaml`

**Fix**:
1. Verify `.do/app.yaml` exists in repo root
2. Clear source directories (leave empty)
3. DO should auto-detect `.do/app.yaml`
4. If not, use Method 2 (Manual Configuration)

### Services can't connect to database

**Cause**: Environment variable not resolving or wrong database name

**Fix**:
1. Ensure database component is named exactly `magidesk-postgres`
2. Use `${magidesk-postgres.DATABASE_URL}` (exact match)
3. Database must be created before services

### Build fails with "Dockerfile not found"

**Cause**: Dockerfile path incorrect

**Fix**:
1. Verify Dockerfile exists in `src/Backend/ServiceName/Dockerfile`
2. Set **Source Directory** to `src/Backend/ServiceName`
3. Set **Dockerfile Path** to `Dockerfile` (relative to source directory)

### Port binding errors

**Cause**: Wrong port configuration

**Fix**:
1. Ensure `ASPNETCORE_URLS=http://0.0.0.0:8080`
2. Set HTTP Port to `8080` in component settings
3. Verify Dockerfile exposes port 8080

---

## 📚 Official Resources

- **App Platform Documentation**: https://docs.digitalocean.com/docs/app-platform
- **Quickstart Guide**: https://docs.digitalocean.com/products/app-platform/getting-started/quickstart/
- **Reference Documentation**: https://docs.digitalocean.com/products/app-platform/reference/
- **Creating Apps Guide**: https://docs.digitalocean.com/products/app-platform/how-to/create-apps/

---

## ✅ Verification Checklist

After setup, verify:

- [ ] Database component created and running
- [ ] All 9 service components created
- [ ] All services building successfully
- [ ] Environment variables set correctly
- [ ] Database connection string resolving
- [ ] Health endpoints responding (e.g., `/health`)
- [ ] Auto-deploy enabled (if desired)

---

## 🎯 Next Steps

1. **Deploy**: Follow Method 1 (App Spec) or Method 2 (Manual)
2. **Import Database**: Use connection string from DO dashboard
3. **Test Services**: Verify all 9 APIs are responding
4. **Update Frontend**: Point frontend to new DO URLs
5. **Monitor**: Use DO dashboard to monitor deployments and logs

---

**Status**: ✅ App spec created at `.do/app.yaml`  
**Branch**: `feature/revamp-2025-enterprise-ui`  
**Ready for**: Deployment via DigitalOcean App Platform

