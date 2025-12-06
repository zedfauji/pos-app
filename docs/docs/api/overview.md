# API Reference Overview

MagiDesk POS provides RESTful APIs through 9 microservices. All APIs follow consistent patterns and support versioning.

## API Base URLs

### Local Development

```
UsersApi:      https://localhost:5001
SettingsApi:   https://localhost:5002
MenuApi:       https://localhost:5003
OrderApi:      https://localhost:5004
PaymentApi:    https://localhost:5005
InventoryApi:  https://localhost:5006
CustomerApi:   https://localhost:5007
DiscountApi:   https://localhost:5008
TablesApi:     https://localhost:5009
```

### Production (Cloud Run)

```
UsersApi:      https://magidesk-users-904541739138.northamerica-south1.run.app
SettingsApi:   https://magidesk-settings-904541739138.us-central1.run.app
MenuApi:       https://magidesk-menu-904541739138.northamerica-south1.run.app
OrderApi:      https://magidesk-order-904541739138.northamerica-south1.run.app
PaymentApi:    https://magidesk-payment-904541739138.northamerica-south1.run.app
InventoryApi:  https://magidesk-inventory-904541739138.northamerica-south1.run.app
CustomerApi:   https://magidesk-customer-904541739138.northamerica-south1.run.app
DiscountApi:   https://magidesk-discount-904541739138.northamerica-south1.run.app
TablesApi:     https://magidesk-tables-904541739138.northamerica-south1.run.app
```

## API Versioning

### v1 (Legacy)
- **Route:** `/api/{controller}` or `/api/v1/{controller}`
- **RBAC:** No permission checks (backward compatible)
- **Status:** Maintained for backward compatibility

### v2 (RBAC-Enabled)
- **Route:** `/api/v2/{controller}`
- **RBAC:** Permission checks required
- **Status:** Recommended for new development

## Common Patterns

### Authentication

All v2 endpoints require authentication:
- **Header:** `X-User-Id: {userId}`
- **Future:** JWT tokens (planned)

### Response Format

**Success Response:**
```json
{
  "data": { ... },
  "message": "Success"
}
```

**Error Response:**
```json
{
  "error": "Error message",
  "details": { ... }
}
```

### Pagination

Many endpoints support pagination:
```json
{
  "items": [ ... ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

### Status Codes

- `200 OK` - Success
- `201 Created` - Resource created
- `204 No Content` - Success, no content
- `400 Bad Request` - Invalid request
- `401 Unauthorized` - Not authenticated
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server error

## API Documentation

### Swagger UI

All APIs provide Swagger UI in development:
- **URL:** `https://localhost:{port}/swagger`
- **Authentication:** Not required for Swagger

### API Endpoints

- [Authentication](./authentication.md)
- [API v1](./v1/overview.md)
- [API v2](./v2/overview.md)

## Rate Limiting

Currently no rate limiting implemented. Consider implementing for production.

## CORS

CORS is configured to allow all origins in development. Configure appropriately for production.

## Next Steps

- [Authentication Guide](./authentication.md)
- [API v1 Reference](./v1/overview.md)
- [API v2 Reference](./v2/overview.md)
