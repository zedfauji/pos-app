# Legacy Shutdown Readiness Checklist

## 1. Feature Parity Check
- [ ] **Reporting**: Z-Report available and accurate? (Yes, verified in Phase 1/2)
- [ ] **Billing**: Split Bill supported? (Yes, verified in Phase 3)
- [ ] **Printing**: Receipts printed? (Yes, Virtual Verified)
- [ ] **Inventory/Menu**: Can we manage items? (Existing Feature)

## 2. critical Data Integrity
- [ ] **Payment Persistence**: Are payments stored in `pay.payments`? (Yes)
- [ ] **Bill Calculation**: Key calculation happens on Server? (Yes, via `BillPreview`)
- [ ] **History**: Do we retain old data? (Yes, Postgres)

## 3. Operational Risks
- [ ] **Printer Hardware**: Is the physical printer IP configured? (Currently 127.0.0.1 for testing)
- [ ] **Offline Mode**: Is it clear to staff that offline is Read-Only? (UI Implementation needed? Low impact for now)

## 4. Final Verdict
- **Ready for Production Pilot**: [YES/NO]
