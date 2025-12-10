# DigitalOcean App Platform - Manual Setup (UI-Only)

**No App Spec bullshit - Pure manual UI setup that actually works**

---

## Step 1: Initial Setup (Fix "No components detected")

### In DigitalOcean Setup Form:

1. **Git Provider**: GitHub ✅
2. **Repository**: `zedfauji/pos-app` ✅
3. **Branch**: `feature/revamp-2025-enterprise-ui` ✅
4. **Source directories**: **EMPTY** (remove any entries)
5. **Autodeploy**: Checked ✅

### Click "Next" or "Continue"

**Even if it says "No components detected" - that's fine! You'll add them manually.**

---

## Step 2: Skip Auto-Detection, Go to Components Page

**You should now see a page where you can add components manually.**

If you don't see this, look for:
- "Add Resource" button
- "Add Component" button
- "Resources" tab/section
- Or click "Skip" / "Continue" until you see component management

---

## Step 3: Add Database First

1. Click **"Add Resource"** or **"Add Component"**
2. Select **"Database"**
3. Configure:
   - **Database Engine**: PostgreSQL
   - **Version**: 17
   - **Plan**: **db-s-dev-database** ($15/mo)
   - **Name**: `magidesk-postgres`
   - **Production**: Leave unchecked
4. Click **"Add Database"** or **"Create"**

**Note the exact database name** - you'll need it for environment variables.

---

## Step 4: Add Each Service Component (9 times)

### For Each Service:

**Service List** (in order):
1. TablesApi → `tables-api`
2. OrderApi → `order-api`
3. PaymentApi → `payment-api`
4. MenuApi → `menu-api`
5. CustomerApi → `customer-api`
6. DiscountApi → `discount-api`
7. InventoryApi → `inventory-api`
8. SettingsApi → `settings-api`
9. UsersApi → `users-api`

### Configuration Template:

Click **"Add Resource"** → **"Service"** (or **"Worker"**)

**Basic Settings**:
- **Name**: `tables-api` (lowercase, hyphenated)
- **Source Directory**: `src/Backend/TablesApi` ⚠️ **THIS IS CRITICAL**
- **Build Command**: *(Leave empty - Docker handles it)*
- **Run Command**: `dotnet TablesApi.dll`

**Docker Settings**:
- **Dockerfile Path**: `Dockerfile` (relative to source directory)
- **HTTP Port**: `8080`

**Instance Settings**:
- **Instance Size**: `basic-xxs` (Free tier - $0/mo)
- **Instance Count**: `1`
- **Autoscaling**: Disabled (for dev)

**Environment Variables** (Click "Edit" or "Add Variable"):

Add these 3 variables:

```
ASPNETCORE_URLS = http://0.0.0.0:8080
ConnectionStrings__Postgres = ${magidesk-postgres.DATABASE_URL}
ASPNETCORE_ENVIRONMENT = Development
```

**Important**: 
- Use `${magidesk-postgres.DATABASE_URL}` (matches your database name exactly)
- This is a DO variable reference - it auto-resolves to the connection string

**Click "Add Service" or "Save"**

---

## Step 5: Repeat for All 9 Services

Use this table to configure each one:

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

**All services use the same environment variables** (copy-paste them).

---

## Step 6: Review and Deploy

1. Verify you have:
   - ✅ 1 Database component
   - ✅ 9 Service components
   - ✅ All source directories correct
   - ✅ All environment variables set

2. Click **"Create Resources"** or **"Deploy"**

3. Monitor deployment in the dashboard

---

## Why This Works

**The Problem**: DigitalOcean scans the **root directory** for Dockerfiles. Your Dockerfiles are in `src/Backend/ServiceName/`, so it can't find them.

**The Solution**: By setting **Source Directory** to `src/Backend/ServiceName` for each service, you tell DO exactly where to find each Dockerfile.

---

## Troubleshooting

### Still says "No components detected"?
- **Just ignore it** - click through to the components/add resource page
- You're adding everything manually anyway

### Can't find "Add Resource" button?
- Look for "Resources" tab
- Or "Components" section
- Or try clicking "Next" / "Continue" until you see component management

### Build fails?
- Verify Source Directory is exactly `src/Backend/ServiceName`
- Verify Dockerfile exists in that directory
- Check Dockerfile path is `Dockerfile` (not `src/Backend/ServiceName/Dockerfile`)

### Database connection fails?
- Ensure database name matches exactly: `magidesk-postgres`
- Use `${magidesk-postgres.DATABASE_URL}` (with the exact name)
- Database must be created BEFORE services

---

## Alternative: Use CLI (If UI is too frustrating)

If the UI is too annoying, use `doctl` CLI:

```bash
# Install doctl
# Windows: choco install doctl
# Or download from: https://github.com/digitalocean/doctl/releases

# Authenticate
doctl auth init

# Create app from spec
doctl apps create --spec .do/app.yaml
```

But honestly, **manual UI is probably faster** for 9 services.

---

**Bottom Line**: Ignore the "No components detected" warning. Just click through and manually add 1 database + 9 services with the settings above.

