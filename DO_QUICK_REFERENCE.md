# DigitalOcean Setup - Quick Reference Card

## ⚠️ IGNORE "No components detected" - Just Add Manually

---

## 1️⃣ Initial Form

- Branch: `feature/revamp-2025-enterprise-ui`
- Source directories: **EMPTY**
- Click "Next"

---

## 2️⃣ Add Database

**Component Name**: `magidesk-postgres`  
**Plan**: `db-s-dev-database` ($15/mo)  
**PostgreSQL 17**

---

## 3️⃣ Add 9 Services (Same Settings for All)

**For Each Service**:

```
Name: [service]-api (lowercase, hyphenated)
Source Directory: src/Backend/[ServiceName]
Dockerfile Path: Dockerfile
HTTP Port: 8080
Instance Size: basic-xxs
Run Command: dotnet [ServiceName]Api.dll

Environment Variables:
  ASPNETCORE_URLS = http://0.0.0.0:8080
  ConnectionStrings__Postgres = ${magidesk-postgres.DATABASE_URL}
  ASPNETCORE_ENVIRONMENT = Development
```

**Service Names**:
- `tables-api` → `src/Backend/TablesApi` → `dotnet TablesApi.dll`
- `order-api` → `src/Backend/OrderApi` → `dotnet OrderApi.dll`
- `payment-api` → `src/Backend/PaymentApi` → `dotnet PaymentApi.dll`
- `menu-api` → `src/Backend/MenuApi` → `dotnet MenuApi.dll`
- `customer-api` → `src/Backend/CustomerApi` → `dotnet CustomerApi.dll`
- `discount-api` → `src/Backend/DiscountApi` → `dotnet DiscountApi.dll`
- `inventory-api` → `src/Backend/InventoryApi` → `dotnet InventoryApi.dll`
- `settings-api` → `src/Backend/SettingsApi` → `dotnet SettingsApi.dll`
- `users-api` → `src/Backend/UsersApi` → `dotnet UsersApi.dll`

---

## 4️⃣ Deploy

Click "Create Resources" or "Deploy"

---

**That's it. No app spec needed. Just manual UI configuration.**

