# API Routes

Base path: `https://<host>/`
All routes below are relative to the base path.

## MagiDesk.Backend

- Vendors
  - GET    `/api/vendors`
  - GET    `/api/vendors/{id}`
  - POST   `/api/vendors`
  - PUT    `/api/vendors/{id}`
  - DELETE `/api/vendors/{id}`
  - GET    `/api/vendors/{vendorId}/items`
  - GET    `/api/vendors/{vendorId}/items/{itemId}`
  - POST   `/api/vendors/{vendorId}/items`
  - PUT    `/api/vendors/{vendorId}/items/{itemId}`
  - DELETE `/api/vendors/{vendorId}/items/{itemId}`
  - GET    `/api/vendors/probe/dump/all`
  - GET    `/api/vendors/probe/{vendorId}/details`
  - GET    `/api/vendors/probe/heineken`
  - GET    `/api/vendors/{vendorId}/export?format=json|yaml|csv`
  - POST   `/api/vendors/{vendorId}/import?format=json|yaml|csv` (raw body)

- Items (generic; requires `vendorId` query parameter)
  - GET    `/api/items?vendorId={vendorId}`
  - GET    `/api/items/{id}?vendorId={vendorId}`
  - POST   `/api/items?vendorId={vendorId}`
  - PUT    `/api/items/{id}?vendorId={vendorId}`
  - DELETE `/api/items/{id}?vendorId={vendorId}`

- Orders
  - GET    `/api/orders`
  - POST   `/api/orders` (finalize order) → 202 Accepted with `{ jobId, orderId }`
  - POST   `/api/orders/cart-draft`
  - GET    `/api/orders/cart-draft/{draftId}`
  - GET    `/api/orders/cart-draft?limit={n}`
  - GET    `/api/orders/job-history?limit={n}`
  - GET    `/api/orders/notifications?limit={n}`

- Inventory
  - GET    `/api/inventory`
  - POST   `/api/inventory/syncProductNames/launch` → 202 Accepted with `{ jobId }`

- Jobs
  - GET    `/api/jobs/{jobId}`
  - GET    `/api/jobs?limit={n}`

- Cash Flow
  - GET    `/api/cashflow`
  - POST   `/api/cashflow`
  - PUT    `/api/cashflow`

- Users & Auth
  - GET    `/api/users`
  - GET    `/api/users/{id}`
  - POST   `/api/users`
  - PUT    `/api/users/{id}`
  - DELETE `/api/users/{id}`
  - POST   `/api/auth/login`

- Settings
  - GET    `/api/settings/frontend?host={host?}`
  - PUT    `/api/settings/frontend?host={host?}`
  - GET    `/api/settings/backend?host={host?}`
  - PUT    `/api/settings/backend?host={host?}`

## SettingsApi (separate project)

- Settings
  - GET    `/api/settings/frontend?host={host?}`
  - PUT    `/api/settings/frontend?host={host?}`
  - GET    `/api/settings/backend?host={host?}`
  - PUT    `/api/settings/backend?host={host?}`
  - GET    `/api/settings/app?host={host?}`
  - PUT    `/api/settings/app?host={host?}`
