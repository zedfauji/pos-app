# API Contract

This document describes the request/response schemas and status codes for the backend APIs, based on the shared DTOs in `solution/shared/` and controller logic in `solution/backend/MagiDesk.Backend/Controllers/`.

Note: Authentication/authorization is currently minimal; refine as needed.

## Conventions

- Content-Type: application/json unless stated otherwise.
- Errors: controllers often return `400 BadRequest`, `404 NotFound`, `409 Conflict`, or `500` with `{ error: string }` body.
- Long-running tasks: some endpoints return `202 Accepted` with `{ jobId: string }`.

## Schemas (from `MagiDesk.Shared`)

- ItemDto
```json
{
  "id": "string|null",
  "sku": "string",
  "name": "string",
  "price": 0.0,
  "stock": 0
}
```

- VendorDto
```json
{
  "id": "string|null",
  "name": "string",
  "contactInfo": "string|null",
  "status": "active|inactive",
  "budget": 0.0,
  "reminder": "string|null",
  "reminderEnabled": true
}
```

- OrderItemDto (see `shared/DTOs/Orders/OrderItemDto.cs`)
- OrderDto
```json
{
  "orderId": "string",
  "vendorId": "string",
  "vendorName": "string",
  "items": [ { /* OrderItemDto */ } ],
  "totalAmount": 0.0,
  "status": "draft|submitted",
  "createdAt": "2024-01-01T00:00:00Z"
}
```

- CartDraftDto
```json
{
  "cartId": "string",
  "vendorId": "string",
  "vendorName": "string",
  "items": [ { /* OrderItemDto */ } ],
  "totalAmount": 0.0,
  "status": "draft|submitted",
  "createdAt": "2024-01-01T00:00:00Z"
}
```

- CashFlow (see `shared/DTOs/CashFlow.cs`)
- JobStatusDto (see `shared/DTOs/JobStatusDto.cs`)
- OrdersJobDto, OrderNotificationDto (see `shared/DTOs/Orders/*`)

- Auth
```json
// CreateUserRequest
{ "username": "string", "password": "string", "role": "string" }
// UpdateUserRequest
{ "username": "string", "password": "string|null", "role": "string", "isActive": true }
// LoginRequest
{ "username": "string", "password": "string" }
// LoginResponse
{ "userId": "string", "username": "string", "role": "string", "lastLoginAt": "2024-01-01T00:00:00Z" }
```

- Settings (from backend services): `FrontendSettings`, `BackendSettings`, `AppSettings` (see SettingsApi for `AppSettings`).

## Endpoint Contracts

- Vendors
  - GET `/api/vendors` → 200 `[VendorDto]`
  - GET `/api/vendors/{id}` → 200 `VendorDto` | 404
  - POST `/api/vendors` (body: `VendorDto{name}` required) → 201 `VendorDto`
  - PUT `/api/vendors/{id}` (body: `VendorDto`) → 204 | 404
  - DELETE `/api/vendors/{id}` → 204 | 404
  - GET `/api/vendors/{vendorId}/items` → 200 `[ItemDto]`
  - GET `/api/vendors/{vendorId}/items/{itemId}` → 200 `ItemDto` | 404
  - POST `/api/vendors/{vendorId}/items` (body: `ItemDto`) → 201 `ItemDto`
  - PUT `/api/vendors/{vendorId}/items/{itemId}` (body: `ItemDto`) → 204 | 404
  - DELETE `/api/vendors/{vendorId}/items/{itemId}` → 204 | 404
  - GET `/api/vendors/probe/dump/all` → 200 object
  - GET `/api/vendors/probe/{vendorId}/details` → 200 object
  - GET `/api/vendors/probe/heineken` → 200 object
  - GET `/api/vendors/{vendorId}/export?format=json|yaml|csv` → 200 file content
  - POST `/api/vendors/{vendorId}/import?format=json|yaml|csv` (raw body) → 204

- Items (generic; requires `vendorId` query)
  - GET `/api/items?vendorId=` → 200 `[ItemDto]` | 400
  - GET `/api/items/{id}?vendorId=` → 200 `ItemDto` | 404 | 400
  - POST `/api/items?vendorId=` (body: `ItemDto`) → 201 `ItemDto` | 400
  - PUT `/api/items/{id}?vendorId=` (body: `ItemDto`) → 204 | 404 | 400
  - DELETE `/api/items/{id}?vendorId=` → 204 | 404 | 400

- Orders
  - GET `/api/orders` → 200 `[OrderDto]`
  - POST `/api/orders` (body: `OrderDto`) → 202 `{ success: true, jobId: string, orderId: string }` | 500
  - POST `/api/orders/cart-draft` (body: `CartDraftDto`) → 200 `CartDraftDto` | 500
  - GET `/api/orders/cart-draft/{draftId}` → 200 `CartDraftDto` | 404
  - GET `/api/orders/cart-draft?limit=` → 200 `[CartDraftDto]`
  - GET `/api/orders/job-history?limit=` → 200 `[OrdersJobDto]`
  - GET `/api/orders/notifications?limit=` → 200 `[OrderNotificationDto]`

- Inventory
  - GET `/api/inventory` → 200 `[InventoryItem]`
  - POST `/api/inventory/syncProductNames/launch` → 202 `{ success: true, jobId: string }` | 500

- Jobs
  - GET `/api/jobs/{jobId}` → 200 `JobStatusDto` | 404
  - GET `/api/jobs?limit=` → 200 `[JobStatusDto]`

- Cash Flow
  - GET `/api/cashflow` → 200 `[CashFlow]`
  - POST `/api/cashflow` (body: `CashFlow`) → 201 `CashFlow` | 500
  - PUT `/api/cashflow` (body: `CashFlow`) → 204 | 500

- Users
  - GET `/api/users` → 200 `[UserDto]` (password hashes blanked)
  - GET `/api/users/{id}` → 200 `UserDto` | 404
  - POST `/api/users` (body: `CreateUserRequest`) → 201 `UserDto` | 400 | 409
  - PUT `/api/users/{id}` (body: `UpdateUserRequest`) → 204 | 400 | 404
  - DELETE `/api/users/{id}` → 204 | 404

- Auth
  - POST `/api/auth/login` (body: `LoginRequest`) → 200 `LoginResponse` | 400 | 401

- Settings (MagiDesk.Backend)
  - GET `/api/settings/frontend?host=` → 200 `FrontendSettings` | 404
  - PUT `/api/settings/frontend?host=` → 204 | 400
  - GET `/api/settings/backend?host=` → 200 `BackendSettings` | 404
  - PUT `/api/settings/backend?host=` → 204 | 400

- SettingsApi (separate project)
  - GET `/api/settings/frontend?host=` → 200 `FrontendSettings` | 404
  - PUT `/api/settings/frontend?host=` → 204 | 400
  - GET `/api/settings/backend?host=` → 200 `BackendSettings` | 404
  - PUT `/api/settings/backend?host=` → 204 | 400
  - GET `/api/settings/app?host=` → 200 `AppSettings` | 404
  - PUT `/api/settings/app?host=` → 204 | 400
