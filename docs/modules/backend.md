# Backend Overview (MagiDesk.Backend)

- Project: `solution/backend/MagiDesk.Backend/`
- Entry point: `Program.cs`
- Technologies: ASP.NET Core 8, Controllers, Dependency Injection, Swagger (Development only)
- Data: Historically used Google Firestore. Core services are now PostgreSQL-only. See dedicated service docs.

## Related Modules

- [MenuApi](./menu-api.md): PostgreSQL-backed menu module (items, modifiers, combos, history), with Cloud Run deployment details.
- [OrderApi](./order-api.md): PostgreSQL-backed ordering module (pricing, validation, logs, totals), with Cloud Run deployment details.

## Services Registered (DI)

This app previously aggregated multiple concerns. Inventory and Orders moved to separate services. Refer to service-specific docs for current DI setups.

## Controllers

Legacy overview retained for context. Active controllers now live per microservice.

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
