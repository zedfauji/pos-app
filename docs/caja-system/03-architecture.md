# Caja System - Architecture Documentation

**Date:** 2025-01-27  
**Branch:** `implement-caja-system`  
**Status:** Architecture Design

## System Architecture Overview

The Caja System is integrated into the existing MagiDesk POS architecture, following the same patterns and conventions.

```mermaid
graph TB
    subgraph "Frontend (WinUI 3)"
        A[CajaPage]
        B[OpenCajaDialog]
        C[CloseCajaDialog]
        D[CajaReportsPage]
        E[CajaStatusBanner]
        F[PaymentPage]
        G[OrdersPage]
    end
    
    subgraph "Frontend Services"
        H[CajaService]
        I[PaymentService]
        J[OrderService]
    end
    
    subgraph "Backend APIs"
        K[PaymentApi]
        L[OrderApi]
        M[TablesApi]
        N[UsersApi]
    end
    
    subgraph "Backend Services"
        O[CajaService]
        P[PaymentService]
        Q[OrderService]
    end
    
    subgraph "Backend Repositories"
        R[CajaRepository]
        S[PaymentRepository]
        T[OrderRepository]
    end
    
    subgraph "Database (PostgreSQL)"
        U[(caja schema)]
        V[(pay schema)]
        W[(ord schema)]
        X[(public schema)]
        Y[(users schema)]
    end
    
    A --> H
    B --> H
    C --> H
    D --> H
    E --> H
    F --> I
    G --> J
    
    H --> K
    I --> K
    J --> L
    
    K --> O
    K --> P
    L --> Q
    
    O --> R
    P --> S
    Q --> T
    
    R --> U
    S --> V
    T --> W
    O --> X
    O --> Y
```

---

## Database Schema Architecture

### Schema: `caja`

```mermaid
erDiagram
    CAJA_SESSIONS ||--o{ CAJA_TRANSACTIONS : "has"
    CAJA_SESSIONS }o--|| USERS : "opened_by"
    CAJA_SESSIONS }o--o| USERS : "closed_by"
    
    CAJA_SESSIONS {
        uuid id PK
        text opened_by_user_id FK
        text closed_by_user_id FK
        timestamptz opened_at
        timestamptz closed_at
        numeric opening_amount
        numeric closing_amount
        numeric system_calculated_total
        numeric difference
        text status
        text notes
    }
    
    CAJA_TRANSACTIONS {
        uuid id PK
        uuid caja_session_id FK
        uuid transaction_id
        text transaction_type
        numeric amount
        timestamptz timestamp
    }
```

### Schema Relationships

```mermaid
erDiagram
    CAJA_SESSIONS ||--o{ PAYMENTS : "tracks"
    CAJA_SESSIONS ||--o{ ORDERS : "tracks"
    CAJA_SESSIONS ||--o{ REFUNDS : "tracks"
    CAJA_SESSIONS ||--o{ TABLE_SESSIONS : "validates"
    
    PAYMENTS {
        uuid payment_id PK
        uuid caja_session_id FK
        uuid session_id
        uuid billing_id
        numeric amount_paid
    }
    
    ORDERS {
        bigint order_id PK
        uuid caja_session_id FK
        uuid session_id
        text table_id
        numeric total
    }
    
    REFUNDS {
        uuid refund_id PK
        uuid caja_session_id FK
        uuid payment_id
        numeric refund_amount
    }
    
    TABLE_SESSIONS {
        uuid session_id PK
        text table_label
        text status
    }
```

---

## Backend Architecture

### Service Layer

```mermaid
classDiagram
    class ICajaService {
        +OpenCajaAsync(request)
        +CloseCajaAsync(request)
        +GetActiveSessionAsync()
        +GetSessionReportAsync(id)
        +ValidateActiveSessionAsync()
    }
    
    class CajaService {
        -ICajaRepository repository
        -IRbacService rbacService
        -ITablesService tablesService
        +OpenCajaAsync(request)
        +CloseCajaAsync(request)
        +GetActiveSessionAsync()
        +GetSessionReportAsync(id)
        +ValidateActiveSessionAsync()
        -CalculateSystemTotal(sessionId)
        -ValidateOpenConditions()
        -ValidateCloseConditions()
    }
    
    class ICajaRepository {
        +GetActiveSessionAsync()
        +OpenSessionAsync(session)
        +CloseSessionAsync(id, data)
        +GetSessionByIdAsync(id)
        +GetSessionsAsync(filters)
        +AddTransactionAsync(transaction)
        +GetTransactionsBySessionAsync(sessionId)
        +CalculateSystemTotalAsync(sessionId)
    }
    
    class CajaRepository {
        -NpgsqlDataSource dataSource
        +GetActiveSessionAsync()
        +OpenSessionAsync(session)
        +CloseSessionAsync(id, data)
        +GetSessionByIdAsync(id)
        +GetSessionsAsync(filters)
        +AddTransactionAsync(transaction)
        +GetTransactionsBySessionAsync(sessionId)
        +CalculateSystemTotalAsync(sessionId)
    }
    
    ICajaService <|.. CajaService
    ICajaRepository <|.. CajaRepository
    CajaService --> ICajaRepository
    CajaService --> IRbacService
    CajaService --> ITablesService
```

### Controller Layer

```mermaid
classDiagram
    class CajaController {
        -ICajaService cajaService
        +OpenCaja(request) POST /api/caja/open
        +CloseCaja(request) POST /api/caja/close
        +GetActiveSession() GET /api/caja/active
        +GetSession(id) GET /api/caja/{id}
        +GetHistory(filters) GET /api/caja/history
        +GetReport(id) GET /api/caja/{id}/report
    }
    
    class PaymentController {
        -IPaymentService paymentService
        +RegisterPayment(request) POST /api/payments
        +ProcessRefund(id, request) POST /api/payments/{id}/refund
    }
    
    class OrderController {
        -IOrderService orderService
        +CreateOrder(request) POST /api/orders
        +UpdateOrder(id, request) PUT /api/orders/{id}
    }
    
    CajaController --> ICajaService
    PaymentController --> IPaymentService
    OrderController --> IOrderService
```

---

## Frontend Architecture

### ViewModel Layer

```mermaid
classDiagram
    class CajaViewModel {
        -ICajaService cajaService
        +ActiveSession CajaSessionDto
        +IsCajaOpen bool
        +SessionHistory ObservableCollection
        +OpenCajaCommand ICommand
        +CloseCajaCommand ICommand
        +RefreshCommand ICommand
        +LoadActiveSessionAsync()
        +LoadSessionHistoryAsync()
        +OpenCajaAsync()
        +CloseCajaAsync()
    }
    
    class PaymentViewModel {
        -IPaymentService paymentService
        -ICajaService cajaService
        +ProcessPaymentAsync()
        -ValidateCajaAsync()
    }
    
    class OrdersViewModel {
        -IOrderService orderService
        -ICajaService cajaService
        +CreateOrderAsync()
        -ValidateCajaAsync()
    }
    
    CajaViewModel --> ICajaService
    PaymentViewModel --> IPaymentService
    PaymentViewModel --> ICajaService
    OrdersViewModel --> IOrderService
    OrdersViewModel --> ICajaService
```

### Service Layer

```mermaid
classDiagram
    class ICajaService {
        +OpenCajaAsync(request) Task
        +CloseCajaAsync(request) Task
        +GetActiveSessionAsync() Task
        +GetSessionHistoryAsync(filters) Task
        +GetSessionReportAsync(id) Task
    }
    
    class CajaService {
        -HttpClient httpClient
        -string baseUrl
        +OpenCajaAsync(request)
        +CloseCajaAsync(request)
        +GetActiveSessionAsync()
        +GetSessionHistoryAsync(filters)
        +GetSessionReportAsync(id)
    }
    
    ICajaService <|.. CajaService
```

---

## Data Flow Diagrams

### Open Caja Flow

```mermaid
sequenceDiagram
    participant User
    participant Frontend
    participant PaymentApi
    participant CajaService
    participant CajaRepository
    participant RbacService
    participant Database
    
    User->>Frontend: Click "Open Caja"
    Frontend->>Frontend: Show OpenCajaDialog
    User->>Frontend: Enter opening amount, notes
    Frontend->>PaymentApi: POST /api/caja/open
    PaymentApi->>RbacService: Check permission (caja:open)
    RbacService-->>PaymentApi: Permission granted
    PaymentApi->>CajaService: OpenCajaAsync(request)
    CajaService->>CajaRepository: GetActiveSessionAsync()
    CajaRepository->>Database: SELECT * FROM caja_sessions WHERE status = 'open'
    Database-->>CajaRepository: No active session
    CajaRepository-->>CajaService: null
    CajaService->>CajaRepository: OpenSessionAsync(session)
    CajaRepository->>Database: INSERT INTO caja_sessions ...
    Database-->>CajaRepository: Session created
    CajaRepository-->>CajaService: CajaSessionDto
    CajaService-->>PaymentApi: CajaSessionDto
    PaymentApi-->>Frontend: 201 Created
    Frontend->>Frontend: Update UI, show status banner
    Frontend-->>User: Caja opened successfully
```

### Process Payment with Caja Validation

```mermaid
sequenceDiagram
    participant User
    participant Frontend
    participant PaymentApi
    participant CajaService
    participant PaymentService
    participant CajaRepository
    participant Database
    
    User->>Frontend: Process payment
    Frontend->>PaymentApi: POST /api/payments
    PaymentApi->>CajaService: ValidateActiveSessionAsync()
    CajaService->>CajaRepository: GetActiveSessionAsync()
    CajaRepository->>Database: SELECT * FROM caja_sessions WHERE status = 'open'
    Database-->>CajaRepository: Active session found
    CajaRepository-->>CajaService: CajaSessionDto
    CajaService-->>PaymentApi: Validation passed
    PaymentApi->>PaymentService: RegisterPaymentAsync(request)
    PaymentService->>PaymentRepository: InsertPaymentsAsync(...)
    PaymentRepository->>Database: INSERT INTO pay.payments (..., caja_session_id)
    PaymentService->>CajaRepository: AddTransactionAsync(transaction)
    CajaRepository->>Database: INSERT INTO caja_transactions ...
    PaymentService-->>PaymentApi: PaymentDto
    PaymentApi-->>Frontend: 201 Created
    Frontend-->>User: Payment processed
```

### Close Caja Flow

```mermaid
sequenceDiagram
    participant User
    participant Frontend
    participant PaymentApi
    participant CajaService
    participant CajaRepository
    participant TablesService
    participant Database
    
    User->>Frontend: Click "Close Caja"
    Frontend->>Frontend: Show CloseCajaDialog
    User->>Frontend: Enter closing amount
    Frontend->>PaymentApi: POST /api/caja/close
    PaymentApi->>CajaService: CloseCajaAsync(request)
    CajaService->>CajaRepository: GetActiveSessionAsync()
    CajaRepository->>Database: SELECT * FROM caja_sessions WHERE status = 'open'
    Database-->>CajaRepository: Active session
    CajaService->>TablesService: GetActiveSessionsCountAsync()
    TablesService-->>CajaService: 0 (no active sessions)
    CajaService->>CajaRepository: CalculateSystemTotalAsync(sessionId)
    CajaRepository->>Database: SELECT SUM(amount) FROM caja_transactions ...
    Database-->>CajaRepository: System total
    CajaService->>CajaService: Calculate difference
    CajaService->>CajaRepository: CloseSessionAsync(id, data)
    CajaRepository->>Database: UPDATE caja_sessions SET status='closed', ...
    Database-->>CajaRepository: Session closed
    CajaRepository-->>CajaService: CajaSessionDto
    CajaService-->>PaymentApi: CajaSessionDto
    PaymentApi-->>Frontend: 200 OK
    Frontend->>Frontend: Update UI, hide status banner
    Frontend-->>User: Caja closed successfully
```

---

## Middleware Architecture

### Request Pipeline

```mermaid
graph LR
    A[HTTP Request] --> B[UserIdExtractionMiddleware]
    B --> C[Authentication]
    C --> D[Authorization]
    D --> E{Caja Validation<br/>Middleware}
    E -->|Caja Endpoints| F[Skip Validation]
    E -->|Protected Endpoints| G{Active Caja?}
    G -->|Yes| H[Continue]
    G -->|No| I[Return 403]
    H --> J[Controller]
    I --> K[Error Response]
    J --> L[Service]
    L --> M[Repository]
    M --> N[Database]
```

### Caja Validation Middleware

```csharp
public class CajaSessionValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICajaService _cajaService;
    private readonly HashSet<string> _excludedPaths;

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip validation for caja endpoints
        if (_excludedPaths.Contains(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Check for active caja session
        var hasActiveSession = await _cajaService.ValidateActiveSessionAsync();
        
        if (!hasActiveSession)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Caja cerrada",
                message = "Debe abrir la caja antes de continuar."
            });
            return;
        }

        await _next(context);
    }
}
```

---

## Security Architecture

### RBAC Integration

```mermaid
graph TB
    A[User Request] --> B[Extract User ID]
    B --> C[Get User Role]
    C --> D{Check Permission}
    D -->|Has Permission| E[Allow]
    D -->|No Permission| F[Deny 403]
    
    G[Caja Open] --> H{Has caja:open?}
    H -->|Admin/Manager| I[Allow]
    H -->|Other| F
    
    J[Caja Close] --> K{Has caja:close?}
    K -->|Admin/Manager/Cashier| I
    K -->|Other| F
    
    L[View Caja] --> M{Has caja:view?}
    M -->|Admin/Manager/Cashier| I
    M -->|Other| F
```

### Permission Matrix

| Role | caja:open | caja:close | caja:view | caja:view_history |
|------|-----------|------------|-----------|-------------------|
| Owner | ✅ | ✅ | ✅ | ✅ |
| Admin | ✅ | ✅ | ✅ | ✅ |
| Manager | ✅ | ✅ | ✅ | ✅ |
| Cashier | ❌ | ✅ | ✅ | ❌ |
| Server | ❌ | ❌ | ❌ | ❌ |
| Host | ❌ | ❌ | ❌ | ❌ |

---

## Error Handling Architecture

### Error Response Format

```json
{
  "error": "Caja cerrada",
  "message": "Debe abrir la caja antes de continuar.",
  "code": "CAJA_CLOSED",
  "details": {
    "requiredAction": "open_caja",
    "lastClosedAt": "2025-01-27T10:30:00Z"
  }
}
```

### Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `CAJA_CLOSED` | 403 | No active caja session |
| `CAJA_ALREADY_OPEN` | 409 | Caja already open |
| `CAJA_CANNOT_CLOSE` | 400 | Cannot close (open tables/payments) |
| `CAJA_PERMISSION_DENIED` | 403 | Insufficient permissions |
| `CAJA_NOT_FOUND` | 404 | Caja session not found |

---

## Performance Considerations

### Caching Strategy

- **Active Session Cache**: Cache active caja session in memory (5-minute TTL)
- **Session History Cache**: Cache recent sessions (10-minute TTL)
- **Invalidation**: Invalidate on open/close operations

### Database Optimization

- **Indexes**: 
  - `idx_caja_sessions_status` on `status`
  - `idx_caja_sessions_active` unique on `status` where `status = 'open'`
  - `idx_caja_transactions_session` on `caja_session_id`

- **Query Optimization**:
  - Use `SELECT FOR UPDATE` for active session checks
  - Aggregate queries for totals calculation

---

## Deployment Architecture

### Cloud Run Services

```
PaymentApi (Cloud Run)
├── CajaController
├── CajaService
├── CajaRepository
└── Database (Cloud SQL)
    └── caja schema
```

### Environment Variables

```bash
# Caja Configuration
CAJA_VALIDATION_ENABLED=true
CAJA_SESSION_TIMEOUT_MINUTES=480  # 8 hours
CAJA_CACHE_TTL_SECONDS=300
```

---

## Monitoring & Logging

### Key Metrics

- Caja open/close frequency
- Average session duration
- Transaction count per session
- Difference amounts (variance)
- Validation failures

### Logging Events

- `CajaOpened` - Session opened
- `CajaClosed` - Session closed
- `CajaValidationFailed` - Validation blocked operation
- `CajaCalculationError` - Total calculation error

---

**Next Document:** [04-integration-guide.md](./04-integration-guide.md)
