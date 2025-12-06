# Quick Start Guide

Get up and running with MagiDesk POS in 5 minutes.

## Prerequisites Check

Ensure you have:
- ✅ Visual Studio 2022 with .NET 8 SDK
- ✅ PostgreSQL running locally
- ✅ Repository cloned

## 1. Start Backend APIs

Open PowerShell and run:

```powershell
# Start all APIs (run each in separate terminal)
cd solution/backend/UsersApi && dotnet run
cd solution/backend/MenuApi && dotnet run
cd solution/backend/OrderApi && dotnet run
# ... etc
```

Or use the provided scripts to start all at once.

## 2. Verify APIs

Check Swagger UI:
- UsersApi: https://localhost:5001/swagger
- MenuApi: https://localhost:5003/swagger

## 3. Launch Frontend

```powershell
cd solution/frontend
dotnet run
```

## 4. Login

Default credentials (if configured):
- Username: `admin`
- Password: `admin`

## 5. Explore Features

- **Dashboard** - View system overview
- **Orders** - Create and manage orders
- **Menu** - Manage menu items
- **Inventory** - Track inventory
- **Settings** - Configure system

## Next Steps

- [Architecture Overview](../architecture/overview.md)
- [API Reference](../api/overview.md)
- [Developer Guide](../dev-guide/coding-standards.md)
