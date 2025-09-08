# Backend Overview (MagiDesk.Backend)

- Project: `solution/backend/MagiDesk.Backend/`
- Entry point: `Program.cs`
- Technologies: ASP.NET Core 8, Controllers, Dependency Injection, Swagger (Development only)
- Data: Google Firestore via `FirestoreService`

## Services Registered (DI)

- `IFirestoreService` → `FirestoreService`
- `ICashFlowService` → `CashFlowService`
- `IInventoryService` → `InventoryService`
- `IOrdersService` → `OrdersService`
- `IEmailService` (HttpClient) → `EmailService`
- `IUsersService` → `UsersService`
- `ISettingsService` → `SettingsService`

## Controllers

- `VendorsController`: CRUD vendors; nested item CRUD; export/import; probe endpoints.
- `ItemsController`: vendor-scoped generic item CRUD (via `vendorId` query).
- `OrdersController`: list orders; finalize; drafts; job history; notifications.
- `InventoryController`: list inventory; launch product-name sync job.
- `JobsController`: get job by id; list recent jobs.
- `CashFlowController`: list/add/update cash flow.
- `UsersController` / `AuthController`: Users CRUD; login.
- `SettingsController`: get/put frontend/backend settings.

## Configuration

`appsettings.json` Firestore section:

```json
{
  "Firestore": {
    "ProjectId": "YOUR_GCP_PROJECT_ID",
    "CredentialsPath": "config/serviceAccount.json"
  }
}
```

Env var `GOOGLE_APPLICATION_CREDENTIALS` can override path.

## CORS

Policy `AllowAll` for development. Restrict in production.
