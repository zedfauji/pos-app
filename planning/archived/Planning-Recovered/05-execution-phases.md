# Execution Phases Breakdown

## Phase 1: The Iron Foundation (Weeks 1-2)
**Objective**: Establish the architecture and "Walking Skeleton".
- **Entry Criteria**: Empty `rewrite/ui-thin-client` branch.
- **Scope**:
    - Usage of `NetArchTest` to seal the blast doors.
    - Set up `Refit` client + `AuthService`.
    - Build `ShellPage` (Main Navigation).
- **Validation**:
    - `ArchTests` pass.
    - User can Login.
    - User sees empty Dashboard.
- **Exit Criteria**: `MagiDesk.Client` compiles, runs, logs in, and bans `Npgsql`.

## Phase 2: Read-Only Features (Weeks 3-4)
**Objective**: Connect the "Eyes" of the system.
- **Entry Criteria**: Phase 1 Complete.
- **Scope**:
    - `TableMapPage` (Live Polling).
    - `MenuPage` (Inventory Display).
- **Risk**: Backend Swagger might be stale.
- **Validation**:
    - Can see Grid of Tables.
    - Can receive Table Status updates from Backend.
- **Exit Criteria**: Waiters can see which tables are occupied.

## Phase 3: The Transactional Core (Weeks 5-7)
**Objective**: Connect the "Hands" of the system.
- **Entry Criteria**: Phase 2 Complete.
- **Scope**:
    - `OrderViewModel` (Add Items).
    - `PaymentViewModel` (Billing).
    - `RawPrinterService` (Receipts).
- **Risk**: Printer hardware integration details unknown.
- **Validation**:
    - Can create Order via API.
    - Can Split Bill via API.
    - Can Print Receipt via Raw Bytes.
- **Exit Criteria**: Full Happy Path (Order -> Eat -> Pay).

## Phase 4: Production Hardening (Week 8)
**Objective**: Handle the "Rainy Day".
- **Entry Criteria**: Phase 3 Complete.
- **Scope**:
    - Global Exception Handling (Offline Overlays).
    - Retry Policies (`Polly`).
    - Installer Creation (MSIX).
- **Validation**:
    - Pull Ethernet Cable -> App shows "Offline".
    - Reconnect -> App recovers.
- **Exit Criteria**: QA Sign-off.

## Risk Assessment Matrix

| Phase | Risk | Impact | Mitigation |
|-------|------|--------|------------|
| **1** | Architecture Leaks | High | Automated `ArchTests` run on every build. |
| **2** | Backend Lag | Medium | Use `MockApi` if real Backend is slow to develop. |
| **3** | Printer Drivers | High | Abstract `IPrinter` early; test on hardware ASAP. |
| **4** | Offline UX | Medium | Keep logic simple (Block UI), don't try complex sync. |
