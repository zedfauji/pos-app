# System Architecture

This document provides a comprehensive overview of the MagiDesk POS system architecture, including component interactions, data flows, and design decisions.

## High-Level Architecture

```mermaid
graph TB
    subgraph "Client Layer"
        A[WinUI 3 Desktop App]
        B[User Interface]
        C[ViewModels]
        D[Services]
    end
    
    subgraph "API Gateway Layer"
        E[Load Balancer]
    end
    
    subgraph "Microservices Layer"
        F[UsersApi]
        G[MenuApi]
        H[OrderApi]
        I[PaymentApi]
        J[InventoryApi]
        K[SettingsApi]
        L[CustomerApi]
        M[DiscountApi]
        N[TablesApi]
    end
    
    subgraph "Data Layer"
        O[(PostgreSQL<br/>Cloud SQL)]
    end
    
    subgraph "External Services"
        P[Google Cloud Run]
        Q[Cloud SQL Proxy]
    end
    
    A --> B
    B --> C
    C --> D
    D --> E
    E --> F
    E --> G
    E --> H
    E --> I
    E --> J
    E --> K
    E --> L
    E --> M
    E --> N
    
    F --> O
    G --> O
    H --> O
    I --> O
    J --> O
    K --> O
    L --> O
    M --> O
    N --> O
    
    F --> P
    G --> P
    H --> P
    I --> P
    J --> P
    K --> P
    L --> P
    M --> P
    N --> P
    
    O --> Q
    Q --> P
```

## Architecture Principles

### 1. Microservices Architecture

**Independent Services**
- Each API is independently deployable
- Services communicate via RESTful HTTP/JSON
- No direct service-to-service dependencies (except UsersApi for RBAC)

**Technology Consistency**
- All services use ASP.NET Core 8
- Consistent patterns across services
- Shared authorization library (`MagiDesk.Shared`)

**Database per Service**
- Each service has its own schema in PostgreSQL
- Services don't access other services' schemas directly
- Data consistency maintained via application logic

### 2. Frontend-Backend Separation

**Desktop Application**
- WinUI 3 for rich, native Windows UI
- MVVM pattern for separation of concerns
- Local state management with caching

**RESTful Communication**
- Standard HTTP/JSON APIs
- Stateless requests
- No server-side session state

**Permission Caching**
- Frontend caches user permissions after login
- UI elements show/hide based on permissions
- Backend always validates permissions (source of truth)

### 3. Security Architecture

**Role-Based Access Control (RBAC)**
- 47 granular permissions
- 6 system roles with predefined permissions
- Custom roles supported
- Role inheritance for permission composition

**Backend Enforcement**
- All v2 endpoints require permissions
- `[RequiresPermission]` attribute on controllers
- `PermissionRequirementHandler` validates access
- `UserIdExtractionMiddleware` extracts user context

**API Versioning**
- v1 endpoints: Legacy, no RBAC (backward compatible)
- v2 endpoints: RBAC-enabled (recommended)
- Gradual migration path

## Component Communication

### Request Flow

```mermaid
sequenceDiagram
    participant U as User
    participant F as Frontend
    participant API as Backend API
    participant RBAC as RBAC Service
    participant DB as Database
    
    U->>F: User Action
    F->>F: Check Permission Cache
    alt Has Permission (UI)
        F->>API: HTTP Request (X-User-Id header)
        API->>API: Extract User ID
        API->>RBAC: Check Permission
        RBAC->>DB: Query User Permissions
        DB-->>RBAC: Permissions
        alt Has Permission (Backend)
            RBAC-->>API: Authorized
            API->>DB: Execute Business Logic
            DB-->>API: Result
            API-->>F: Success Response
            F-->>U: Update UI
        else No Permission
            RBAC-->>API: Forbidden
            API-->>F: 403 Forbidden
            F-->>U: Show Error
        end
    else No Permission (UI)
        F-->>U: Hide/Disable UI Element
    end
```

### Authentication Flow

```mermaid
sequenceDiagram
    participant U as User
    participant F as Frontend
    participant Auth as AuthController
    participant RBAC as RBAC Service
    participant DB as Database
    
    U->>F: Enter Credentials
    F->>Auth: POST /api/v2/auth/login
    Auth->>DB: Validate Credentials
    DB-->>Auth: User Data
    Auth->>RBAC: Get User Permissions
    RBAC->>DB: Query Permissions
    DB-->>RBAC: Permissions Array
    RBAC-->>Auth: Permissions
    Auth-->>F: LoginResponse (with Permissions)
    F->>F: Store Session (UserId, Permissions)
    F-->>U: Navigate to Dashboard
```

## Database Architecture

### Schema Organization

```mermaid
erDiagram
    users ||--o{ users : "has"
    users ||--o{ roles : "assigned"
    roles ||--o{ role_permissions : "has"
    roles ||--o{ role_inheritance : "inherits"
    
    menu ||--o{ menu_items : "contains"
    menu_items ||--o{ modifiers : "has"
    
    ord ||--o{ orders : "contains"
    orders ||--o{ order_items : "has"
    
    payments ||--o{ payments : "processes"
    payments ||--o{ refunds : "has"
    
    inventory ||--o{ inventory_items : "manages"
    inventory_items ||--o{ inventory_stock : "tracks"
    
    customers ||--o{ customers : "manages"
    customers ||--o{ loyalty : "has"
    
    discounts ||--o{ campaigns : "contains"
    discounts ||--o{ vouchers : "has"
    
    public ||--o{ tables : "manages"
    public ||--o{ table_sessions : "tracks"
```

### Schema Details

| Schema | Purpose | Key Tables |
|--------|---------|------------|
| `users` | User management, RBAC | `users`, `roles`, `role_permissions`, `role_inheritance` |
| `menu` | Menu items, modifiers, combos | `menu_items`, `modifiers`, `modifier_options`, `combos` |
| `ord` | Order processing | `orders`, `order_items`, `order_logs` |
| `payments` | Payment processing | `payments`, `refunds`, `payment_logs` |
| `inventory` | Inventory management | `inventory_items`, `inventory_stock`, `inventory_transactions`, `vendors` |
| `settings` | System settings | `app_settings`, `setting_categories` |
| `customers` | Customer management | `customers`, `customer_segments`, `loyalty_programs`, `wallets` |
| `discounts` | Discount management | `campaigns`, `vouchers`, `combo_offers` |
| `public` | Tables and sessions | `tables`, `table_status`, `table_sessions`, `bills` |

## Deployment Architecture

### Cloud Run Deployment

```mermaid
graph TB
    subgraph "Google Cloud Platform"
        A[Cloud Load Balancer]
        B[Cloud Run Service 1<br/>UsersApi]
        C[Cloud Run Service 2<br/>MenuApi]
        D[Cloud Run Service 3<br/>OrderApi]
        E[Cloud Run Service 4<br/>PaymentApi]
        F[Cloud Run Service 5<br/>InventoryApi]
        G[Cloud Run Service 6<br/>SettingsApi]
        H[Cloud Run Service 7<br/>CustomerApi]
        I[Cloud Run Service 8<br/>DiscountApi]
        J[Cloud Run Service 9<br/>TablesApi]
        K[Cloud SQL<br/>PostgreSQL]
        L[Cloud SQL Proxy]
    end
    
    A --> B
    A --> C
    A --> D
    A --> E
    A --> F
    A --> G
    A --> H
    A --> I
    A --> J
    
    B --> L
    C --> L
    D --> L
    E --> L
    F --> L
    G --> L
    H --> L
    I --> L
    J --> L
    
    L --> K
```

### Deployment Characteristics

**Scalability**
- Each service scales independently
- Auto-scaling based on request volume
- Horizontal scaling (multiple instances)

**Reliability**
- Health check endpoints (`/health`)
- Automatic restarts on failure
- Request timeout handling

**Security**
- Cloud SQL Proxy for secure database connections
- Environment variables for secrets
- IAM-based access control

## Data Flow Patterns

### Order Processing Flow

```mermaid
sequenceDiagram
    participant U as User
    participant F as Frontend
    participant O as OrderApi
    participant M as MenuApi
    participant P as PaymentApi
    participant T as TablesApi
    participant DB as Database
    
    U->>F: Create Order
    F->>O: POST /api/v2/orders
    O->>M: Validate Menu Items
    M-->>O: Menu Item Data
    O->>T: Check Table Status
    T-->>O: Table Available
    O->>DB: Create Order
    DB-->>O: Order Created
    O-->>F: Order Response
    F-->>U: Order Confirmed
    
    U->>F: Process Payment
    F->>P: POST /api/v2/payments
    P->>O: Get Order Details
    O-->>P: Order Data
    P->>DB: Process Payment
    DB-->>P: Payment Processed
    P->>T: Update Table Status
    T-->>P: Table Updated
    P-->>F: Payment Confirmed
    F-->>U: Payment Success
```

## Design Decisions

### Why Microservices?

1. **Independent Deployment** - Deploy services without affecting others
2. **Technology Flexibility** - Can use different technologies per service (currently all ASP.NET Core)
3. **Team Autonomy** - Different teams can own different services
4. **Scalability** - Scale services independently based on load

### Why WinUI 3?

1. **Native Performance** - Direct access to Windows APIs
2. **Rich UI** - Modern Fluent Design System
3. **Offline Capability** - Desktop app can work offline
4. **User Experience** - Better UX than web apps for POS systems

### Why PostgreSQL?

1. **ACID Compliance** - Strong consistency guarantees
2. **JSON Support** - Native JSON/JSONB for flexible schemas
3. **Performance** - Excellent for complex queries
4. **Cloud SQL** - Managed service with automatic backups

### Why API Versioning?

1. **Backward Compatibility** - Existing clients continue working
2. **Gradual Migration** - Migrate to v2 at own pace
3. **Feature Flags** - Enable/disable v2 per environment
4. **Easy Rollback** - Revert to v1 if issues arise

## Performance Considerations

### Caching Strategy

- **Frontend**: Permission cache, menu cache, settings cache
- **Backend**: No caching (stateless services)
- **Database**: PostgreSQL query cache

### Connection Pooling

- **Npgsql DataSource**: Connection pooling per service
- **Cloud SQL Proxy**: Connection multiplexing
- **Connection Limits**: Configured per service

### Async/Await

- All I/O operations are async
- No blocking calls in request pipeline
- Parallel processing where possible

## Monitoring and Observability

### Logging

- **Structured Logging**: JSON format logs
- **Log Levels**: Debug, Information, Warning, Error
- **Cloud Logging**: Integrated with Google Cloud Logging

### Health Checks

- **Endpoint**: `/health` on all services
- **Database Check**: Verifies database connectivity
- **Dependency Check**: Verifies external service availability

### Metrics

- Request count, latency, error rate
- Database query performance
- Permission check performance

## Security Architecture

### Authentication

- Currently: `X-User-Id` header (for v2 APIs)
- Future: JWT tokens (planned)

### Authorization

- **Policy-Based**: Each permission is a policy
- **Handler-Based**: `PermissionRequirementHandler` validates
- **Middleware**: `UserIdExtractionMiddleware` extracts context

### Data Protection

- **Encryption in Transit**: HTTPS/TLS
- **Encryption at Rest**: Cloud SQL encryption
- **Secrets Management**: Environment variables

## Future Enhancements

### Planned Improvements

1. **JWT Authentication** - Replace header-based auth
2. **API Gateway** - Centralized routing and rate limiting
3. **Event Sourcing** - For audit trails
4. **CQRS** - Separate read/write models
5. **GraphQL** - Alternative to REST APIs
6. **WebSocket** - Real-time updates

---

**Last Updated**: 2025-01-02  
**Architecture Version**: 1.0.0

