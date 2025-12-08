# Caja System - Implementation Plan

**Date:** 2025-01-27  
**Branch:** `implement-caja-system`  
**Status:** Planning Phase

## Overview

This document provides a detailed, step-by-step implementation plan for the Caja System. Each phase includes specific tasks, code locations, and acceptance criteria.

---

## Implementation Phases

### Phase 1: Database Schema & Models (Days 1-2)

#### 1.1 Create Caja Schema

**Location:** `solution/backend/PaymentApi/` (or new `CajaApi`)

**Tasks:**
- [ ] Create `Database/CajaSchema.sql` with:
  - `caja` schema creation
  - `caja_sessions` table
  - `caja_transactions` table
  - Indexes and constraints
- [ ] Create `DatabaseInitializer` extension for caja schema
- [ ] Test migration on local database
- [ ] Document rollback procedure

**Files to Create:**
```
solution/backend/PaymentApi/
├── Database/
│   └── CajaSchema.sql
└── Services/
    └── CajaDatabaseInitializer.cs
```

**Acceptance Criteria:**
- ✅ Schema created successfully
- ✅ Tables have proper constraints
- ✅ Unique constraint prevents multiple active sessions
- ✅ Foreign keys properly defined

#### 1.2 Create Caja Models

**Location:** `solution/backend/PaymentApi/Models/`

**Tasks:**
- [ ] Create `CajaDtos.cs` with:
  - `OpenCajaRequestDto`
  - `CloseCajaRequestDto`
  - `CajaSessionDto`
  - `CajaTransactionDto`
  - `CajaReportDto`
- [ ] Create `CajaModels.cs` with:
  - `CajaSession` entity
  - `CajaTransaction` entity

**Files to Create:**
```
solution/backend/PaymentApi/Models/
├── CajaDtos.cs
└── CajaModels.cs
```

**Acceptance Criteria:**
- ✅ All DTOs match database schema
- ✅ Validation attributes on request DTOs
- ✅ Proper nullable types

---

### Phase 2: Backend Service Implementation (Days 3-5)

#### 2.1 Create Caja Repository

**Location:** `solution/backend/PaymentApi/Repositories/`

**Tasks:**
- [ ] Create `ICajaRepository.cs` interface
- [ ] Create `CajaRepository.cs` implementation
- [ ] Implement methods:
  - `GetActiveSessionAsync()`
  - `OpenSessionAsync()`
  - `CloseSessionAsync()`
  - `GetSessionByIdAsync()`
  - `GetSessionsAsync()` (with pagination)
  - `AddTransactionAsync()`
  - `GetTransactionsBySessionAsync()`
  - `CalculateSystemTotalAsync()`

**Files to Create:**
```
solution/backend/PaymentApi/Repositories/
├── ICajaRepository.cs
└── CajaRepository.cs
```

**Acceptance Criteria:**
- ✅ All repository methods implemented
- ✅ Proper transaction handling
- ✅ Error handling for edge cases
- ✅ Unit tests pass

#### 2.2 Create Caja Service

**Location:** `solution/backend/PaymentApi/Services/`

**Tasks:**
- [ ] Create `ICajaService.cs` interface
- [ ] Create `CajaService.cs` implementation
- [ ] Implement business logic:
  - `OpenCajaAsync()` - Validate permissions, check for existing session
  - `CloseCajaAsync()` - Validate permissions, check open tables/payments, calculate totals
  - `GetActiveSessionAsync()` - Get current open session
  - `GetSessionReportAsync()` - Generate report for session
  - `GetSessionsHistoryAsync()` - List historical sessions
  - `ValidateActiveSessionAsync()` - Check if session exists (for middleware)

**Files to Create:**
```
solution/backend/PaymentApi/Services/
├── ICajaService.cs
└── CajaService.cs
```

**Acceptance Criteria:**
- ✅ All business rules enforced
- ✅ Proper error messages
- ✅ Permission checks implemented
- ✅ Integration tests pass

#### 2.3 Create Caja Controller

**Location:** `solution/backend/PaymentApi/Controllers/`

**Tasks:**
- [ ] Create `CajaController.cs`
- [ ] Implement endpoints:
  - `POST /api/caja/open` - Open caja session
  - `POST /api/caja/close` - Close caja session
  - `GET /api/caja/active` - Get active session
  - `GET /api/caja/{id}` - Get session by ID
  - `GET /api/caja/history` - Get session history
  - `GET /api/caja/{id}/report` - Get session report
- [ ] Add `[RequiresPermission]` attributes
- [ ] Add proper HTTP status codes
- [ ] Add Swagger documentation

**Files to Create:**
```
solution/backend/PaymentApi/Controllers/
└── CajaController.cs
```

**Acceptance Criteria:**
- ✅ All endpoints functional
- ✅ Proper authorization
- ✅ Swagger documentation complete
- ✅ Error responses properly formatted

---

### Phase 3: Integration with Existing APIs (Days 6-8)

#### 3.1 PaymentApi Integration

**Location:** `solution/backend/PaymentApi/`

**Tasks:**
- [ ] Update `PaymentService.RegisterPaymentAsync()`:
  - Add caja session validation
  - Link payment to caja_session_id
  - Create caja transaction record
- [ ] Update `PaymentService.ProcessRefundAsync()`:
  - Add caja session validation
  - Create caja transaction record
- [ ] Update `PaymentRepository.InsertPaymentsAsync()`:
  - Add `caja_session_id` parameter
- [ ] Update database schema:
  - Add `caja_session_id` column to `pay.payments`
  - Add `caja_session_id` column to `pay.refunds`

**Files to Modify:**
```
solution/backend/PaymentApi/
├── Services/PaymentService.cs
├── Repositories/PaymentRepository.cs
└── Services/DatabaseInitializer.cs
```

**Acceptance Criteria:**
- ✅ Payments blocked without active caja
- ✅ Payments linked to caja session
- ✅ Refunds tracked in caja transactions
- ✅ Existing tests still pass

#### 3.2 OrderApi Integration

**Location:** `solution/backend/OrderApi/`

**Tasks:**
- [ ] Update `OrderService.CreateOrderAsync()`:
  - Add caja session validation
  - Link order to caja_session_id (optional, via session_id)
- [ ] Update `OrderService.UpdateOrderAsync()`:
  - Add caja session validation (if modifying totals)
- [ ] Update database schema:
  - Add `caja_session_id` column to `ord.orders` (optional)

**Files to Modify:**
```
solution/backend/OrderApi/
├── Services/OrderService.cs
└── Services/DatabaseInitializer.cs
```

**Acceptance Criteria:**
- ✅ Orders blocked without active caja
- ✅ Orders linked to caja session
- ✅ Existing tests still pass

#### 3.3 TablesApi Integration

**Location:** `solution/backend/TablesApi/`

**Tasks:**
- [ ] Update `TablesService.StopSessionAsync()`:
  - Add caja session validation before bill generation
- [ ] Add method to check for active table sessions:
  - `GetActiveSessionsCountAsync()`
- [ ] Use in caja close validation

**Files to Modify:**
```
solution/backend/TablesApi/
└── Program.cs (or separate service file)
```

**Acceptance Criteria:**
- ✅ Cannot generate bills without active caja
- ✅ Cannot close caja with active sessions
- ✅ Existing tests still pass

---

### Phase 4: Middleware & RBAC (Days 9-10)

#### 4.1 Create Caja Validation Middleware

**Location:** `solution/shared/Authorization/Middleware/`

**Tasks:**
- [ ] Create `CajaSessionValidationMiddleware.cs`
- [ ] Implement validation logic:
  - Check for active caja session
  - Skip validation for caja endpoints
  - Return proper error response
- [ ] Add configuration for protected endpoints

**Files to Create:**
```
solution/shared/Authorization/Middleware/
└── CajaSessionValidationMiddleware.cs
```

**Acceptance Criteria:**
- ✅ Middleware blocks requests without active caja
- ✅ Proper error messages
- ✅ Configurable endpoint exclusion

#### 4.2 Update RBAC Permissions

**Location:** `solution/shared/DTOs/Users/`

**Tasks:**
- [ ] Add caja permissions to `Permissions.cs`:
  - `CAJA_OPEN = "caja:open"`
  - `CAJA_CLOSE = "caja:close"`
  - `CAJA_VIEW = "caja:view"`
  - `CAJA_VIEW_HISTORY = "caja:view_history"`
- [ ] Update `UserRoles.cs` with role permissions:
  - Admin: All caja permissions
  - Manager: All caja permissions
  - Cashier: `caja:close`, `caja:view`
  - Server: None

**Files to Modify:**
```
solution/shared/DTOs/Users/
├── Permissions.cs
└── UserRoles.cs
```

**Acceptance Criteria:**
- ✅ Permissions added to system
- ✅ Roles updated with permissions
- ✅ RBAC tests pass

---

### Phase 5: Frontend Core (Days 11-14)

#### 5.1 Create Caja Service (API Client)

**Location:** `solution/frontend/Services/`

**Tasks:**
- [ ] Create `ICajaService.cs` interface
- [ ] Create `CajaService.cs` implementation
- [ ] Implement methods:
  - `OpenCajaAsync()`
  - `CloseCajaAsync()`
  - `GetActiveSessionAsync()`
  - `GetSessionHistoryAsync()`
  - `GetSessionReportAsync()`

**Files to Create:**
```
solution/frontend/Services/
├── ICajaService.cs
└── CajaService.cs
```

**Acceptance Criteria:**
- ✅ All API methods implemented
- ✅ Proper error handling
- ✅ Uses HttpClient correctly

#### 5.2 Create Caja ViewModel

**Location:** `solution/frontend/ViewModels/`

**Tasks:**
- [ ] Create `CajaViewModel.cs`
- [ ] Implement properties:
  - `ActiveSession` (observable)
  - `IsCajaOpen` (computed)
  - `SessionHistory` (observable collection)
- [ ] Implement commands:
  - `OpenCajaCommand`
  - `CloseCajaCommand`
  - `RefreshCommand`
- [ ] Implement methods:
  - `LoadActiveSessionAsync()`
  - `LoadSessionHistoryAsync()`

**Files to Create:**
```
solution/frontend/ViewModels/
└── CajaViewModel.cs
```

**Acceptance Criteria:**
- ✅ ViewModel implements INotifyPropertyChanged
- ✅ Commands properly bound
- ✅ Error handling implemented

#### 5.3 Create Caja UI Pages

**Location:** `solution/frontend/Views/`

**Tasks:**
- [ ] Create `CajaPage.xaml` and `CajaPage.xaml.cs`:
  - Show active session status
  - Show session history
  - Buttons for open/close
- [ ] Create `OpenCajaDialog.xaml`:
  - Opening amount input
  - Notes input
  - User info display
- [ ] Create `CloseCajaDialog.xaml`:
  - Closing amount input
  - System calculated total display
  - Difference display
  - Notes input
  - Manager override option

**Files to Create:**
```
solution/frontend/
├── Views/
│   └── CajaPage.xaml
│   └── CajaPage.xaml.cs
└── Dialogs/
    ├── OpenCajaDialog.xaml
    ├── OpenCajaDialog.xaml.cs
    ├── CloseCajaDialog.xaml
    └── CloseCajaDialog.xaml.cs
```

**Acceptance Criteria:**
- ✅ All UI components functional
- ✅ Proper WinUI 3 styling
- ✅ Error messages displayed
- ✅ Loading states handled

#### 5.4 Create Caja Reports Page

**Location:** `solution/frontend/Views/`

**Tasks:**
- [ ] Create `CajaReportsPage.xaml`:
  - Session selection
  - Report display:
    - Opening amount
    - Sales total
    - Refunds total
    - Tips total
    - Cash movements
    - Closing amount
    - Difference
  - Export buttons (PDF, CSV)

**Files to Create:**
```
solution/frontend/Views/
└── CajaReportsPage.xaml
└── CajaReportsPage.xaml.cs
```

**Acceptance Criteria:**
- ✅ Reports display correctly
- ✅ Export functionality works
- ✅ Proper formatting

---

### Phase 6: Frontend Integration (Days 15-17)

#### 6.1 Update Navigation

**Location:** `solution/frontend/Views/MainPage.xaml`

**Tasks:**
- [ ] Add "Caja" menu item to navigation
- [ ] Add submenu items:
  - Open Caja
  - Close Caja
  - Reports
- [ ] Add permission-based visibility

**Files to Modify:**
```
solution/frontend/Views/
└── MainPage.xaml
```

**Acceptance Criteria:**
- ✅ Menu items appear correctly
- ✅ Navigation works
- ✅ Permission-based visibility

#### 6.2 Add Caja Status Indicator

**Location:** `solution/frontend/Views/`

**Tasks:**
- [ ] Create `CajaStatusBanner.xaml` user control
- [ ] Display:
  - "Caja Abierta" indicator
  - Opened time
  - Opened by name
- [ ] Add to `MainPage.xaml` or `TablesPage.xaml`

**Files to Create:**
```
solution/frontend/Views/
└── Controls/
    ├── CajaStatusBanner.xaml
    └── CajaStatusBanner.xaml.cs
```

**Acceptance Criteria:**
- ✅ Banner displays when caja is open
- ✅ Updates in real-time
- ✅ Proper styling

#### 6.3 Update Existing Pages

**Location:** `solution/frontend/Views/`

**Tasks:**
- [ ] Update `PaymentPage.xaml.cs`:
  - Check caja status before payment
  - Show error dialog if closed
- [ ] Update `OrdersManagementPage.xaml.cs`:
  - Check caja status before order creation
  - Show error dialog if closed
- [ ] Update `TablesPage.xaml.cs`:
  - Check caja status before bill generation
  - Show error dialog if closed

**Files to Modify:**
```
solution/frontend/Views/
├── PaymentPage.xaml.cs
├── OrdersManagementPage.xaml.cs
└── TablesPage.xaml.cs
```

**Acceptance Criteria:**
- ✅ All pages validate caja status
- ✅ Error dialogs shown when needed
- ✅ User-friendly error messages

---

### Phase 7: Testing (Days 18-19)

#### 7.1 Unit Tests

**Location:** `solution/backend/PaymentApi.Tests/` (or create new)

**Tasks:**
- [ ] Test `CajaService.OpenCajaAsync()`:
  - Success case
  - Permission denied
  - Already open
- [ ] Test `CajaService.CloseCajaAsync()`:
  - Success case
  - Permission denied
  - Open tables exist
  - Pending payments exist
- [ ] Test `CajaService.CalculateSystemTotalAsync()`:
  - Correct calculation
  - Empty session
  - Multiple transaction types

**Files to Create:**
```
solution/backend/PaymentApi.Tests/
└── CajaServiceTests.cs
```

**Acceptance Criteria:**
- ✅ All unit tests pass
- ✅ Code coverage > 80%

#### 7.2 Integration Tests

**Location:** `solution/backend/PaymentApi.Tests/`

**Tasks:**
- [ ] Test complete flow:
  - Open caja
  - Process payment
  - Process order
  - Close caja
- [ ] Test error scenarios:
  - Payment without caja
  - Order without caja
  - Close with open tables

**Files to Create:**
```
solution/backend/PaymentApi.Tests/
└── CajaIntegrationTests.cs
```

**Acceptance Criteria:**
- ✅ All integration tests pass
- ✅ Edge cases covered

#### 7.3 UI Tests

**Location:** `solution/MagiDesk.Tests/UITests/`

**Tasks:**
- [ ] Test open caja dialog
- [ ] Test close caja dialog
- [ ] Test status indicator
- [ ] Test blocked operations

**Files to Create:**
```
solution/MagiDesk.Tests/UITests/
└── CajaUITests.cs
```

**Acceptance Criteria:**
- ✅ All UI tests pass
- ✅ Visual verification complete

---

### Phase 8: Documentation (Day 20)

#### 8.1 Developer Documentation

**Location:** `docs/caja-system/`

**Tasks:**
- [ ] Create `03-architecture.md`:
  - System architecture
  - Database schema
  - API endpoints
  - Flow diagrams
- [ ] Create `04-integration-guide.md`:
  - How to add caja validation
  - How to link transactions
  - Error handling patterns
- [ ] Create `05-testing-guide.md`:
  - Test scenarios
  - Test data setup
  - Edge cases

**Files to Create:**
```
docs/caja-system/
├── 03-architecture.md
├── 04-integration-guide.md
└── 05-testing-guide.md
```

**Acceptance Criteria:**
- ✅ All documentation complete
- ✅ Code examples included
- ✅ Diagrams clear

#### 8.2 User Documentation

**Location:** `docs/docs/user-guides/`

**Tasks:**
- [ ] Create `caja-operations.md`:
  - How to open caja
  - How to close caja
  - How to view reports
- [ ] Create `caja-troubleshooting.md`:
  - Common issues
  - Error messages
  - Recovery procedures

**Files to Create:**
```
docs/docs/user-guides/
├── caja-operations.md
└── caja-troubleshooting.md
```

**Acceptance Criteria:**
- ✅ User-friendly language
- ✅ Screenshots included
- ✅ Step-by-step instructions

---

## Risk Mitigation

### High-Risk Areas

1. **Database Migration**
   - Risk: Breaking existing data
   - Mitigation: Test migrations on copy of production data

2. **Transaction Blocking**
   - Risk: Blocking legitimate operations
   - Mitigation: Comprehensive testing, graceful error messages

3. **Performance Impact**
   - Risk: Additional database queries
   - Mitigation: Proper indexing, caching active session

4. **RBAC Integration**
   - Risk: Permission conflicts
   - Mitigation: Test all role combinations

### Rollback Plan

1. **Database Rollback**
   - Keep migration scripts reversible
   - Test rollback on staging

2. **Code Rollback**
   - Feature flags for caja validation
   - Gradual rollout

---

## Success Criteria

### Functional Requirements

- ✅ Can open caja session (Admin/Manager only)
- ✅ Can close caja session (Admin/Manager/Cashier)
- ✅ Cannot process payments without active caja
- ✅ Cannot create orders without active caja
- ✅ Cannot close caja with open tables
- ✅ System calculates totals correctly
- ✅ Reports show accurate data

### Non-Functional Requirements

- ✅ Performance: < 100ms for caja validation
- ✅ Reliability: 99.9% uptime
- ✅ Security: Proper RBAC enforcement
- ✅ Usability: Clear error messages

---

## Timeline Summary

| Phase | Duration | Dependencies |
|-------|----------|--------------|
| Phase 1: Database | 2 days | None |
| Phase 2: Backend Service | 3 days | Phase 1 |
| Phase 3: API Integration | 3 days | Phase 2 |
| Phase 4: Middleware & RBAC | 2 days | Phase 2 |
| Phase 5: Frontend Core | 4 days | Phase 2, 4 |
| Phase 6: Frontend Integration | 3 days | Phase 5 |
| Phase 7: Testing | 2 days | All phases |
| Phase 8: Documentation | 1 day | All phases |
| **Total** | **20 days** | |

---

## Next Steps

1. Review and approve this implementation plan
2. Set up development environment
3. Create feature branch
4. Begin Phase 1 implementation

---

**Next Document:** [03-architecture.md](./03-architecture.md)
