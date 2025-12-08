# Installation Guide

This guide walks you through installing and setting up MagiDesk POS for local development.

## Step 1: Clone the Repository

```powershell
git clone https://github.com/your-username/Order-Tracking-By-GPT.git
cd Order-Tracking-By-GPT
```

## Step 2: Restore Dependencies

```powershell
cd solution
dotnet restore
```

This will restore all NuGet packages for the solution.

## Step 3: Set Up Local Database

### Option A: Using Docker (Recommended)

```powershell
docker run -d `
  --name magidesk-postgres `
  -e POSTGRES_PASSWORD=Campus_66 `
  -e POSTGRES_USER=posapp `
  -e POSTGRES_DB=postgres `
  -p 5432:5432 `
  postgres:17
```

### Option B: Using Local PostgreSQL Installation

1. Install PostgreSQL 17
2. Create a database:
   ```sql
   CREATE DATABASE postgres;
   CREATE USER posapp WITH PASSWORD 'Campus_66';
   GRANT ALL PRIVILEGES ON DATABASE postgres TO posapp;
   ```

## Step 4: Configure Connection Strings

Update connection strings in backend API `appsettings.json` files:

```json
{
  "Postgres": {
    "LocalConnectionString": "Host=localhost;Port=5432;Username=posapp;Password=Campus_66;Database=postgres"
  }
}
```

## Step 5: Initialize Database Schemas

Each API has a database initializer that creates required schemas. Run the APIs once to initialize:

```powershell
# Terminal 1 - UsersApi
cd solution/backend/UsersApi
dotnet run

# Terminal 2 - MenuApi
cd solution/backend/MenuApi
dotnet run

# Repeat for other APIs...
```

Or use the migration scripts in each API's `Database/` folder.

## Step 6: Build the Solution

```powershell
cd solution
dotnet build MagiDesk.sln -c Debug
```

## Step 7: Configure Frontend

Update `solution/frontend/appsettings.json`:

```json
{
  "Api": {
    "BaseUrl": "https://localhost:5001"
  },
  "SettingsApi": {
    "BaseUrl": "https://localhost:5002"
  },
  "MenuApi": {
    "BaseUrl": "https://localhost:5003"
  }
  // ... other APIs
}
```

## Step 8: Run Backend APIs

Start each API in a separate terminal:

```powershell
# UsersApi (Port 5001)
cd solution/backend/UsersApi
dotnet run

# MenuApi (Port 5003)
cd solution/backend/MenuApi
dotnet run

# OrderApi (Port 5004)
cd solution/backend/OrderApi
dotnet run

# PaymentApi (Port 5005)
cd solution/backend/PaymentApi
dotnet run

# InventoryApi (Port 5006)
cd solution/backend/InventoryApi
dotnet run

# SettingsApi (Port 5002)
cd solution/backend/SettingsApi
dotnet run

# CustomerApi (Port 5007)
cd solution/backend/CustomerApi
dotnet run

# DiscountApi (Port 5008)
cd solution/backend/DiscountApi
dotnet run

# TablesApi (Port 5009)
cd solution/backend/TablesApi
dotnet run
```

## Step 9: Run Frontend

```powershell
cd solution/frontend
dotnet run
```

Or use Visual Studio:
1. Open `solution/MagiDesk.sln`
2. Set `MagiDesk.Frontend` as startup project
3. Press F5

## Step 10: Verify Installation

### Check Backend APIs

Visit Swagger UI for each API:
- UsersApi: https://localhost:5001/swagger
- MenuApi: https://localhost:5003/swagger
- OrderApi: https://localhost:5004/swagger
- etc.

### Check Frontend

1. Launch the WinUI 3 application
2. You should see the login page
3. Default credentials (if configured):
   - Username: `admin`
   - Password: `admin` (change on first login)

### Check Database

```sql
-- Verify schemas exist
SELECT schema_name 
FROM information_schema.schemata 
WHERE schema_name IN ('users', 'inventory', 'ord', 'customers', 'discounts', 'settings');

-- Verify tables exist
SELECT table_schema, table_name 
FROM information_schema.tables 
WHERE table_schema IN ('users', 'inventory', 'ord', 'customers', 'discounts', 'settings')
ORDER BY table_schema, table_name;
```

## Troubleshooting

### Port Already in Use

If a port is already in use:

1. Find the process:
   ```powershell
   netstat -ano | findstr :5001
   ```

2. Kill the process:
   ```powershell
   taskkill /PID <PID> /F
   ```

3. Or change the port in `launchSettings.json`

### Database Connection Failed

1. Verify PostgreSQL is running:
   ```powershell
   # Docker
   docker ps
   
   # Windows Service
   Get-Service postgresql*
   ```

2. Check connection string format
3. Verify firewall allows port 5432

### Build Errors

1. Clean and rebuild:
   ```powershell
   dotnet clean
   dotnet restore
   dotnet build
   ```

2. Delete `bin/` and `obj/` folders
3. Restore packages again

### Frontend Won't Start

1. Verify Windows App SDK is installed
2. Check Windows version (Windows 10 1809+)
3. Run as Administrator if needed
4. Check Visual Studio workloads

## Next Steps

- [Quick Start Guide](./quick-start.md) - Run your first operation
- [Architecture Overview](../architecture/overview.md) - Understand the system
- [Developer Guide](../dev-guide/coding-standards.md) - Start developing

## Development Scripts

### PowerShell Scripts

The repository includes helpful PowerShell scripts:

```powershell
# Deploy to Cloud Run
.\solution\backend\UsersApi\deploy-users-api.ps1

# Test API connection
.\test-mcp-connection.ps1

# Database interaction
.\db-interact.ps1
```

## Production Deployment

For production deployment, see:
- [Cloud Run Deployment](../deployment/cloud-run.md)
- [Production Setup](../deployment/production.md)
