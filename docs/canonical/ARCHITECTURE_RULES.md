# Architecture Rules

**Status**: ACTIVE  
**Owner**: Principal Software Architect  
**Last Reviewed**: 2025-12-22  

---

## Core Principles

### 1. API-First Architecture

The backend is the **single source of truth** for all business logic and data.

```
┌──────────────┐      HTTP/JSON       ┌──────────────┐
│   WinUI 3    │  ──────────────────► │   Backend    │
│   Client     │  ◄──────────────────  │   APIs       │
└──────────────┘                      └──────────────┘
      │                                      │
      │ Display Only                         │ Business Logic
      ▼                                      ▼
   User sees                           PostgreSQL DB
```

### 2. No Direct Database Access from Client

❌ **NEVER** use `NpgsqlConnection` or any DB driver in the client  
❌ **NEVER** embed SQL in UI code  
✅ **ALWAYS** call backend APIs for data operations

### 3. Backend Authoritative Pattern

| Responsibility | Backend | Client |
|:---------------|:-------:|:------:|
| Calculate totals | ✅ | ❌ |
| Validate payments | ✅ | ❌ |
| Business rules | ✅ | ❌ |
| Data persistence | ✅ | ❌ |
| Display data | ❌ | ✅ |
| User input | ❌ | ✅ |
| Navigation | ❌ | ✅ |

### 4. Clean Architecture Layers

```
solution/backend/
├── MagiDesk.Core/          # Domain entities, interfaces
├── MagiDesk.Infrastructure/ # Repositories, DB access
├── *Api/                    # HTTP controllers per domain
```

### 5. Microservices Boundaries

| API | Port | Domain |
|:----|:-----|:-------|
| TablesApi | 53503 | Tables, Sessions, Bills, Shifts |
| OrderApi | 53504 | Orders, Order Items |
| PaymentApi | 53505 | Payments, Ledger |
| MenuApi | 53506 | Menu Items, Modifiers, Combos |
| InventoryApi | 53507 | Inventory, Vendors |
| SettingsApi | 53508 | App Settings |
| UsersApi | 53509 | Users, Auth, RBAC |
| CustomerApi | 53510 | Customers, Loyalty |
| DiscountApi | 53511 | Discounts, Vouchers |

---

## Invariants (Non-Negotiable)

1. **No financial calculations in UI** - Backend computes all money
2. **No direct DB writes from client** - Always via API
3. **Immutable billing IDs** - Once created, cannot change
4. **Shift gating enforced at API** - `[RequireOpenShift]` attribute
5. **Order data persists across restart** - No DROP SCHEMA in dev

---

## Anti-Patterns (BANNED)

| Pattern | Why Bad |
|:--------|:--------|
| `new NpgsqlConnection()` in UI | Tight coupling, security risk |
| SQL strings in ViewModel | Business logic leak |
| Money calculation in XAML converter | Precision loss |
| Hardcoded API URLs | Deployment failure |
| Parallel DB writes without transaction | Data corruption |

---

**DO NOT MODIFY** without Principal Architect approval.
