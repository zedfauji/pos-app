
This document is authoritative
Payment Workspace Implementation & Debugging Synthesis
Conversation Decisions & Outcomes
Page-Based Payment Architecture

Decision: Payment flow moved entirely to 
PaymentWorkspacePage
, abandoning ContentDialog.
Rationale: Logic was too complex for a dialog; page-based flow allows for better state management and Split Payment UI.
Scope: UI / Workflow
Type: OPERATIONAL
Backend-Driven Session Totals

Decision: 
GetActiveSessionsAsync
 (Payment Hub) must calculate totals dynamically by joining order_items.
Rationale: Active sessions are not finalized; totals must reflect real-time order state.
Scope: Backend / DB
Type: FINANCIAL
WinUI Type Compatibility (double vs decimal)

Decision: 
PaymentWorkspaceViewModel
 properties bound to NumberBox (e.g., TotalDue, 
AmountTendered
) changed from decimal to double.
Rationale: WinUI NumberBox control fundamentally requires double. Two-way binding with decimal caused runtime crashes.
Scope: Client ViewModel
Type: OPERATIONAL
Route Standardization

Decision: Client 
ITableApi
 updated to call /tables/active instead of /sessions/active.
Rationale: Resolved 404 error; matched existing backend controller routing.
Scope: Client API
Type: OPERATIONAL
ViewModel Template Registration

Decision: Explicitly registered PaymentWorkspaceTemplate in 
ViewModelTemplateSelector
 and 
ShellPage
.
Rationale: Required for the navigation service to correctly render the ViewModel.
Scope: Client Infrastructure
Type: OPERATIONAL
Expected Code Impact
Decision: Backend-Driven Session Totals

Project: MagiDesk.Infrastructure
File: 
TableRepository.cs
Responsibility: Execute SQL JOIN between table_sessions, orders, and order_items to return ItemsCount and 
Total
.
Decision: WinUI Type Compatibility

Project: MagiDesk.Client
File: 
PaymentWorkspaceViewModel.cs
Responsibility: Store values as double for UI binding; explicit cast to decimal when mapping to DTOs for API calls.
Decision: Route Standardization

Project: MagiDesk.Client
File: 
ITableApi.cs
Responsibility: Define 
GetActiveSessionsAsync
 with correct route attribute [Get("/tables/active")] and return type List<SessionOverview>.
Decision: Page-Based Payment Architecture

Project: MagiDesk.Client
File: 
PaymentWorkspacePage.xaml
Responsibility: Host full UI for payment, split selection, and bill preview.
File: 
App.xaml
Responsibility: Register necessary converters (
ZeroToVisibleConverter
, 
StringEqualsToVisibilityConverter
).
Code Verification Checklist
Backend Logic

 Verify that TableRepository.GetActiveSessionsAsync uses COUNT(*) or valid column reference and correctly joins ord schema tables.
 Verify that /tables/active endpoint returns non-zero totals for tables with orders.
Client Architecture

 Verify that 
ITableApi
 uses List<SessionOverview> (typed) instead of List<object>.
 Verify that 
ViewModelTemplateSelector
 contains a case for 
PaymentWorkspaceViewModel
.
 Verify that 
ShellPage.xaml
 Resources contains PaymentWorkspaceTemplate.
UI Binding & Type Safety

 Verify that 
PaymentWorkspaceViewModel
 properties TotalDue, 
AmountTendered
, Subtotal, Tax, FinalTotal are of type double.
 Verify that partial methods 
OnAmountTenderedChanged
 and 
OnDiscountAmountChanged
 accept double.
 Verify explicit casts 
(decimal)
 exist when creating 
RegisterPaymentRequestDto
 and 
CalculateSplitRequest
.
Potential Gaps (If Verification Fails)
Gap: Precision Loss in Payment (Floating Point Math)

Missing: double used for calculations in ViewModel before sending to backend.
Why it matters: Standard IEEE 754 floating point issues (e.g., 0.1 + 0.2 != 0.3) could cause penny discrepancies.
Layer: Client ViewModel
Risk: MEDIUM (Financial) - Should verify backend recalculates/validates final totals.
Gap: Split Calculation Mismatch

Missing: Backend calculates split in decimal, UI receives decimal but casts to double for display.
Why it matters: Displayed split amount might differ slightly from what backend processed if rounding occurs differently.
Layer: Client -> Backend Handoff
Risk: LOW (Display only, final payment is authoritative).
Safe Next Steps
Ready for controlled implementation
The system is now building and runnable.
Major blockers (backend crashes, 404s, XAML type errors) are resolved.
Immediate next step is functional verification of the full payment flow (Full, Split, Partial) to ensure data integrity traverses the double <-> decimal boundary correctly.

Conversation Decisions & Outcomes
Architectural & workflow Decisions
Strict Data Persistence Policy
Decision: The development-mode "Database Initializer" must NOT drop the ord schema on startup.
Rationale: Preventing data loss during service restarts is critical for testing multi-step workflows (Place Order -> Restart -> Pay).
Scope: Backend (TablesApi) / Database (ord schema).
Type: OPERATIONAL.
Enforced Full Payment Model (MVP)
Decision: The system must reject any "Stop Session" command where the tendered amount matches "Cash" but is less than the total bill amount.
Rationale: The current MVP does not support "Split by Amount" or "Partial Payment" tracking. Accepting partial payments previously resulted in revenue loss (closing session with unpaid balance).
Scope: Backend (
StopSessionCommandHandler
).
Type: FINANCIAL.
Sequential Build-And-Start Workflow
Decision: The backend startup script must explicitly kill existing processes, build services synchronously, and abort on failure before launching new instances.
Rationale: Previous parallel or "fire-and-forget" startup led to port conflicts (phantom errors) and silent build failures (running old code).
Scope: Workflow (
start-backend.ps1
).
Type: OPERATIONAL.
New Invariants
Invariant: ord.orders and ord.order_items tables persist data across backend application restarts.
Invariant: A 
StopSessionCommand
 with PaymentMethod.Cash throws an exception if AmountTendered < TotalAmount.
Invariant: 
SessionsController
 returns HTTP 400 Bad Request (not 500) for payment validation errors.
Expected Code Impact
Infrastructure
File: 
start-backend.ps1
Responsibility: Clean environment (taskkill), verified serial builds, verified process launch.
File: TableRepository / 
OrderIntegrationService
Responsibility: Connection management MUST explicitly open connections before transactions (fix for InvalidOperationException).
Core Domain (MagiDesk.Core)
File: 
StopSessionCommandHandler.cs
Responsibility: Business logic validation. MUST compare AmountTendered vs TotalAmount for Cash payments.
API Layer (TablesApi)
File: 
SessionsController.cs
Responsibility: Exception mapping. MUST catch InvalidOperationException and map to BadRequest to provide UI feedback.
File: 
DatabaseInitializer.cs
Responsibility: Schema maintenance. MUST NOT contain DROP SCHEMA commands.
Code Verification Checklist
Persistence
 Verify that TablesApi does not execute DROP SCHEMA ord on startup logs.
 Verify that an Order created in Session A remains retrievable via 
GetOrderItems
 after a full backend restart.
Financial Safety
 Verify that 
StopSessionCommandHandler
 throws InvalidOperationException when AmountTendered < Total (Cash).
 Verify that 
SessionsController
 catches this specific exception and returns HTTP 400.
 Verify that the Session remains active (Status = 'active') after a failed insufficient payment attempt.
 Verify that AmountTendered >= Total successfully closes the session.
Operational Stability
 Verify that 
start-backend.ps1
 cleans up dotnet.exe processes before starting.
 Verify that OrderIntegrationService.PostOrderAsync calls OpenAsync() before BeginTransactionAsync().
Potential Gaps (If Verification Fails)
Non-Cash Partial Payments
Missing: Validation currently checks PaymentMethod.Cash. Card/Online payments might inadvertently allow partial usage if logic assumes full authorization.
Risk: MEDIUM (Financial).
Layer: Domain (
StopSessionCommandHandler
).
Legacy Data Synchronization
Missing: The system reads from both SQL (ord) and Legacy JSON (table_sessions). If legacy code writes only to JSON, new SQL-based readers might miss it (though currently merged).
Risk: LOW (Operational).
Layer: Infrastructure (
OrderIntegrationService
).
Printer Integration on Failure
Missing: If payment validation fails, ensure no receipt is printed prematurely.
Risk: LOW (Operational).
Layer: Client (Command Logic).
Safe Next Steps
Recommendation: Ready for controlled implementation

The critical data loss bug (schema drop) is resolved, enabling reliable testing.
The immediate financial risk (partial payment loophole) is blocked.
Startup tooling is stabilized.
Proceed to Final Pilot Testing using the stabilized environment.
Conversation Decisions & Outcomes
Architectural Decisions
Backend-First Gating: Financial gating is enforced at the API controller level using Action Filters ([RequireOpenShift]), not just in the UI. This ensures robust security even if the UI is bypassed.
Micro-Monolith Alignment: Shift logic is hosted within the TablesApi service (port 53503) as it directly orchestrates table sessions and billing, which are the primary blockers for shift closure.
Service Layer Pattern: Logic is encapsulated in ShiftService (Infrastructure) and exposed via IShiftService (Core), adhering to Clean Architecture principles.
Invariants & Rules
No Shift = No Operations: System must return 423 Locked for any attempt to Start Session, Post Order, or Accept Payment if no shift is open.
Atomic Closure: A shift CANNOT be closed if ANY active table sessions or unsettled bills exist. This is a hard blocker, not a warning.
Immutability: Once a shift is CLOSED, it cannot be reopened, edited, or deleted. It becomes a read-only historical record.
Scope: Defines "Financial Operations" as: Starting a session (revenue initiation), Ordering (revenue accretion), and Payment (revenue realization). Operational tasks like viewing menus or history are not gated.
Terminology
"Blockers": Specific conditions that prevent shift closure (e.g., "Active Table: Table 5", "Unsettled Bill: $45.00").
"Variance": The difference between Declared Cash (user input) and Expected Cash (system calculated).
"Shift Number": A sequential, user-friendly identifier (e.g., #104) distinct from the ShiftId (GUID).
Expected Code Impact
Backend: TablesApi
Controllers:
ShiftsController.cs
: NEW - Handles Open/Close, History, Reports, and Validation.
SessionsController.cs
: MODIFIED - Added [RequireOpenShift] attribute to 
StartSession
, 
PostOrder
, and 
StopSession
 endpoints.
Filters:
RequireOpenShiftAttribute.cs
: NEW - Intercepts requests, checks DB for open shift, returns 423 or injects CurrentShiftId.
Configuration:
Program.cs
: MODIFIED - Registered IShiftRepository, IShiftService, and ShiftsController.
Backend: Core & Infrastructure
Entities:
Shift.cs
: NEW - Domain entity for shift state.
Interfaces:
IShiftRepository.cs
: NEW - Persistence contract.
IShiftService.cs
: NEW - Business logic contract.
Repositories:
ShiftRepository.cs
: NEW - Dapper implementation for PostgreSQL.
Services:
ShiftService.cs
: NEW - Implements gating logic, cash calculation, and closure validation.
Database
Schema:
public.shifts: NEW - Central table for shift data.
table_sessions, bills, ord.orders, pay.payments: MODIFIED - Added shift_id (UUID) foreign key column.
Code Verification Checklist
Database Structure
Verify that table public.shifts exists with columns: shift_id, status, opened_at, closed_at, expected_cash, declared_cash.
Verify that a unique index exists on public.shifts to enforce only ONE row with status = 'open'.
Verify that columns shift_id exist on tables: public.table_sessions, public.bills, ord.orders, pay.payments.
Backend Implementation
Verify ShiftsController is registered in the DI container in 
TablesApi/Program.cs
.
Verify 
StartSession
 method in 
SessionsController
 is decorated with [RequireOpenShift].
Verify 
PostOrder
 method in 
SessionsController
 is decorated with [RequireOpenShift].
Verify 
StopSession
 method in 
SessionsController
 is decorated with [RequireOpenShift].
Verify ShiftService.CloseShiftAsync throws an exception if GetShiftBlockersAsync returns any active tables or unsettled bills.
Verify ShiftRepository.GetOpenShiftAsync calls the database with status = 'open'.
Potential Gaps (If Verification Fails)
1. Database Schema Mismatch (HIGH RISK)
Description: The automated migration attempt encountered schema errors (missing ord or pay schemas in the target DB connection).
Impact: If shift_id columns are missing, [RequireOpenShift] may crash or fail to tag transactions, breaking the audit trail.
Layer: Database / SQL Migration.
2. UI Gating Missing (MEDIUM RISK)
Description: No frontend implementation for ShiftControllerPage or navigation gating was started.
Impact: Users will receive raw 423 Locked API errors instead of a friendly "Open Shift" prompt.
Layer: Frontend (Client).
3. Z-Report Printing (LOW RISK)
Description: PrintZReport endpoint is currently a stub returning success without printing.
Impact: Physical proof of shift close is missing.
Layer: Backend (ShiftsController).
Safe Next Steps
Recommendation: Requires database alignment

Verify Database Target: The presence of schema errors suggests the tool might have connected to a different database instance than the application. Confirm the connection string matches the application's runtime environment.
Manual Migration: Execute 
solution/backend/shift-migration.sql
 against the verified database to ensure the schema is consistent with the code.
Backend Restart: Ensure the backend service is restarted to compile the new RequireOpenShift attributes and ShiftsController.
Proceed to UI: Only after the backend returns expected 423 Locked responses should the UI work (Phase 5) begin.
A. Architectural Baseline (Non-Negotiable)

Across all conversations, these invariants are consistent and stable:

Page-based workflows

Payment logic lives in PaymentWorkspacePage

No ContentDialog for payments

Backend is the financial authority

Totals, validation, session closure

UI is display + intent only

OPERATIONAL vs FINANCIAL separation

OPERATIONAL:

Start / end session

Shift open / close

Timer stop

FINANCIAL:

Payment acceptance

Settlement

Split logic

This separation is explicitly documented and enforced

Shift (Caja) is a hard gate

No shift → no start session, no order, no payment

Enforced at API level ([RequireOpenShift])

UI gating is secondary, not primary

These four points appear in all three summaries, which means they are solid.

💰 B. Financial Safety Rules (Merged)

From the payment + stop session conversations:

StopSessionCommand:

Cash payments must reject AmountTendered < Total

Must throw a domain exception

Controller must return HTTP 400, not 500

Session must remain ACTIVE after failure

Partial payments:

Explicitly NOT supported in MVP

Any partial acceptance is a bug

Precision handling:

UI uses double only for binding

Backend recalculates in decimal

Backend is final authority

This is internally consistent. No contradiction.

🗄️ C. Persistence & Stability Decisions

From the second conversation:

❌ No DROP SCHEMA ord in dev startup

Orders and order_items must survive restarts

Backend startup must be:

serial

fail-fast

no phantom processes

These are environment stability rules, not features — and they must be preserved.

🧾 D. Shift Controller Canonical Model

From the third summary:

Shift is:

backend-owned

immutable once closed

sequentially numbered (user-facing)

Only ONE open shift allowed

Shift blocks:

active table sessions

unsettled bills

DB schema expectations:

public.shifts

shift_id FK everywhere relevant

Gating enforced via:

RequireOpenShiftAttribute

returns 423 Locked

This is a clean, coherent model. No mixing with legacy if implemented correctly.