# System Patterns: MagiDesk POS System

## System Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    WinUI 3 Client                            │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │  Views   │→ │ViewModels│→ │  APIs    │→ │   DTOs   │   │
│  │  (XAML)  │  │  (MVVM)  │  │  (Refit) │  │ (Shared) │   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │
└─────────────────────────────────────────────────────────────┘
                          │ HTTP/JSON
                          ▼
┌─────────────────────────────────────────────────────────────┐
│              ASP.NET Core Microservices                      │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │TablesApi │  │OrderApi  │  │PaymentApi│  │ MenuApi  │   │
│  │  :53503  │  │  :53504  │  │  :5002   │  │  :5227   │   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │UsersApi  │  │SettingsApi│ │Inventory │  │CustomerApi│   │
│  │  :55162  │  │           │  │   Api    │  │           │   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│              PostgreSQL Database                             │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │  public  │  │   ord    │  │   pay    │  │  audit   │   │
│  │ (tables, │  │ (orders, │  │(payments,│  │  (logs)  │   │
│  │ shifts)  │  │  items)  │  │  ledger) │  │          │   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### Client Architecture (Clean Architecture Layers)

```
MagiDesk.Client/
├── Views/              # XAML pages (presentation only)
├── ViewModels/         # MVVM view models (state + commands)
├── Services/           # Infrastructure services (DI)
│   ├── IAuthService
│   ├── INavigationService
│   ├── IPrinterService
│   └── IDialogService
└── Converters/         # Value converters for XAML

MagiDesk.Shared/
└── DTOs/              # Data transfer objects (contracts)
    ├── Orders/
    ├── Payments/
    ├── Tables/
    └── Menu/
```

### Backend Architecture (Microservices)

```
Backend/
├── MagiDesk.Core/           # Domain entities, interfaces
├── MagiDesk.Infrastructure/ # Repositories, DB access
├── TablesApi/              # Tables, Sessions, Shifts
├── OrderApi/               # Orders, Order Items
├── PaymentApi/             # Payments, Ledger
├── MenuApi/                # Menu Items, Categories
├── InventoryApi/           # Stock, Vendors
├── SettingsApi/            # System Settings
├── UsersApi/               # Authentication
└── CustomerApi/            # Customers, Loyalty
```

## Key Technical Decisions

### 1. Client-Server Separation
- **Decision**: Zero business logic in client, all logic in backend
- **Rationale**: Enables scalability, testability, and multi-platform support
- **Enforcement**: Architecture tests block database dependencies

### 2. Refit for API Clients
- **Decision**: Use Refit to generate type-safe HTTP clients
- **Rationale**: Compile-time safety, less boilerplate, automatic serialization
- **Pattern**: Define interface, Refit generates implementation

### 3. MVVM with CommunityToolkit
- **Decision**: Use CommunityToolkit.Mvvm for ObservableObject and RelayCommand
- **Rationale**: Reduces boilerplate, standard patterns, source generators
- **Pattern**: `[ObservableProperty]` and `[RelayCommand]` attributes

### 4. Polly for Resilience
- **Decision**: Automatic retry with exponential backoff
- **Rationale**: Network failures are common, retry improves reliability
- **Pattern**: Configured per API client in DI container

### 5. Serilog for Logging
- **Decision**: Structured logging to file and console
- **Rationale**: Better debugging, production diagnostics
- **Pattern**: Configured in App.xaml.cs, injected via DI

### 6. Shift-Based Gating
- **Decision**: All financial operations require open shift
- **Rationale**: Financial audit trail, prevents unauthorized operations
- **Pattern**: `[RequireOpenShift]` action filter on controllers

### 7. Page-Based Payment Flow
- **Decision**: Payment workflow in dedicated page, not dialog
- **Rationale**: Complex state management, better UX for split payments
- **Pattern**: PaymentWorkspacePage with PaymentWorkspaceViewModel

### 8. Backend Financial Authority
- **Decision**: All calculations and validations server-side
- **Rationale**: Precision, security, single source of truth
- **Pattern**: UI sends intent, backend calculates and validates

## Design Patterns in Use

### 1. Dependency Injection
- **Pattern**: Constructor injection throughout
- **Implementation**: Microsoft.Extensions.DependencyInjection
- **Usage**: All ViewModels, Services, API clients registered in App.xaml.cs

### 2. Repository Pattern
- **Pattern**: Data access abstraction
- **Implementation**: Interfaces in Core, implementations in Infrastructure
- **Usage**: ITableRepository, IOrderRepository, IShiftRepository

### 3. Command Pattern
- **Pattern**: Encapsulate operations as commands
- **Implementation**: StopSessionCommand, OpenShiftCommand
- **Usage**: Domain commands with handlers in Core

### 4. Service Layer Pattern
- **Pattern**: Business logic in services, not repositories
- **Implementation**: IShiftService, IBillingService
- **Usage**: Services orchestrate repositories and enforce business rules

### 5. DTO Pattern
- **Pattern**: Data transfer objects for API contracts
- **Implementation**: Shared DTOs in MagiDesk.Shared
- **Usage**: All API communication uses DTOs, no entity exposure

### 6. Factory Pattern
- **Pattern**: Create objects through factories
- **Implementation**: ViewModelTemplateSelector for ViewModel creation
- **Usage**: Navigation service uses factory to create ViewModels

### 7. Observer Pattern
- **Pattern**: Property change notifications
- **Implementation**: INotifyPropertyChanged via ObservableObject
- **Usage**: All ViewModel properties notify UI of changes

## Component Relationships

### Client Navigation Flow
```
ShellPage (Shell)
    ├── LoginPage → LoginViewModel
    ├── TableMapPage → TableViewModel
    ├── OrderPage → OrderViewModel
    ├── PaymentWorkspacePage → PaymentWorkspaceViewModel
    ├── MenuEditorPage → MenuEditorViewModel
    └── SettingsPage → SettingsViewModel
```

### API Client Dependencies
```
ViewModels
    ↓ (depends on)
Refit Interfaces (ITableApi, IOrderApi, etc.)
    ↓ (configured with)
HttpClient + Polly + Serilog
    ↓ (calls)
Backend APIs
```

### Backend Service Dependencies
```
Controllers
    ↓ (depends on)
Services (IShiftService, IBillingService)
    ↓ (depends on)
Repositories (IShiftRepository, ITableRepository)
    ↓ (depends on)
PostgreSQL Database
```

## Critical Implementation Paths

### 1. Order-to-Payment Flow
```
1. User selects table → StartSession API call
2. User browses menu → GetMenuItems API call
3. User adds items → PostOrder API call
4. User requests payment → GetActiveSessions API call
5. User processes payment → RegisterPayment API call
6. Backend validates → StopSession API call
7. Receipt printed → PrinterService
```

### 2. Shift Management Flow
```
1. Manager opens shift → OpenShift API call
2. [RequireOpenShift] filter checks shift status
3. All operations tagged with shift_id
4. Manager closes shift → CloseShift API call
5. System validates no active sessions
6. Cash reconciliation → DeclaredCash vs ExpectedCash
7. Shift immutable after close
```

### 3. Payment Processing Flow
```
1. PaymentWorkspaceViewModel loads active sessions
2. User selects session → LoadSessionDetails
3. Backend calculates totals (authoritative)
4. User enters payment method and amount
5. UI casts double → decimal for API
6. Backend validates payment (amount, method)
7. Payment registered → Session closed
8. Receipt generated → PrinterService
```

### 4. Authentication Flow
```
1. User enters credentials → LoginViewModel
2. AuthService calls IAuthApi.LoginAsync
3. Backend validates → Returns JWT token
4. Token stored in ITokenService
5. Token included in all API calls (via HttpClient handler)
6. TokenService validates expiration
7. Auto-logout on token expiry
```

## Architectural Guardrails

### Enforced Rules
1. **No Database in Client**: Architecture tests block Npgsql, System.Data.SqlClient
2. **No Business Logic in UI**: Code-behind only for UI events
3. **No ViewModel → View References**: ViewModels cannot reference UI types
4. **Always Use DI**: No service locator pattern
5. **Always Use Refit**: No direct HttpClient usage
6. **Always Use MVVM**: No code-behind logic

### Violation Consequences
- **Build Failure**: Architecture tests run on every build
- **Code Review Rejection**: Manual review checks for violations
- **ADR Required**: Breaking guardrails requires Architecture Decision Record

