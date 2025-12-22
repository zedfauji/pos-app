# 02 - Task Breakdown per Phase

## Phase 1: Reporting Backend

### Task 1.1: Define Reporting DTOs
- **Component**: `MagiDesk.Shared`
- **Output**: `ZReportDto`, `DailySalesDto`
- **Responsibility**: Define contract for Z-Report.
- **Fields**: `TotalSales`, `TotalCash`, `TotalCard`, `TotalOrders`, `StartTime`, `EndTime`.

### Task 1.2: Implement Reporting Service (Core)
- **Component**: `MagiDesk.Core` / `MagiDesk.Infrastructure`
- **Responsibility**: Query **Order** and **Payment** tables to aggregate data.
- **Logic**: 
    - Sum `TotalAmount` where `Status = 'Paid'` and `Date = Today`.
    - Group by `PaymentMethod`.
- **Constraint**: READ-ONLY access to Order/Payment data.

### Task 1.3: Expose Reporting API
- **Component**: `ReportingApi` (New Project or Folder in Backend)
- **Responsibility**: Endpoint `GET /api/reports/z-report`.
- **Auth**: Admin/Manager Role ONLY.

## Phase 2: Reporting UI & Print

### Task 2.1: Add Client API Client
- **Component**: `MagiDesk.Client`
- **Responsibility**: Add `IReportingApi` to Refit definition.

### Task 2.2: Implement `DayCloseViewModel`
- **Component**: `MagiDesk.Client`
- **Responsibility**: 
    - Call API to get `ZReportDto`.
    - Format String for Printer (Header, Body, Totals, Footer).
    - Send to `PrinterService`.

### Task 2.3: Add "Day Close" Button
- **Component**: `ShellPage` or `SettingsPage`
- **Responsibility**: Entry point restricted to Admin.

## Phase 3: Split Bill Refinement

### Task 3.1: Backend Bill Preview
- **Component**: `BillingApi`
- **Responsibility**: `POST /api/billing/preview-split`.
- **Input**: List of Items to split.
- **Output**: `BillPreviewDto` (Total, Tax, Remaining Balance).
- **Goal**: Prevent client-side math errors.

### Task 3.2: Refactor Split UI
- **Component**: `TableViewModel` (or new `BillingViewModel`)
- **Responsibility**: Use `BillPreviewDto` instead of local `Sum()`.

## Phase 4: Final Validation

### Task 4.1: Run Legacy Readiness Checklist
- **Component**: Audit
- **Action**: Perform full E2E test of closing a shift.
