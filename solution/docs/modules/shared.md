# Shared DTOs Overview (MagiDesk.Shared)

- Project: `solution/shared/MagiDesk.Shared.csproj`
- Purpose: Strongly-typed contracts between backend APIs and the WinUI frontend.

## Namespaces

- `MagiDesk.Shared.DTOs`
  - `ItemDto`
  - `InventoryItem`
  - `VendorDto`
  - `CashFlow`
  - `JobStatusDto`
- `MagiDesk.Shared.DTOs.Orders`
  - `OrderItemDto`
  - `OrderDto`
  - `CartDraftDto`
  - `OrdersJobDto`
  - `OrderNotificationDto`
- `MagiDesk.Shared.DTOs.Auth`
  - `CreateUserRequest`
  - `UpdateUserRequest`
  - `LoginRequest`
  - `LoginResponse`
  - `SessionDto`

## Notes

- DTOs are simple POCOs designed for JSON serialization.
- Keep DTO changes backward-compatible where possible; version APIs when breaking.
- Frontend binds to these types to avoid duplication of contract definitions.
