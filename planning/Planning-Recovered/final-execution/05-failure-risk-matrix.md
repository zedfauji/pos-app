# 05 - Failure & Risk Matrix

## P0: Reporting API & Z-Report

| Failure Mode | Impact | Detection | Mitigation | Rollback |
| :--- | :--- | :--- | :--- | :--- |
| **Logic Error**: Z-Report sums include "Open" orders. | **Critical**: Revenue inflated. Tax liability wrong. | **Integration Test**: Create 1 open, 1 paid order. Verify sum = paid only. | Filter `Status == Paid` explictly. | Revert API deployment. |
| **Timezone Bug**: Orders after midnight appear on previous day. | **High**: Cashier drawer won't balance. | **Manual Test**: Set server time to 23:59, fire order. Set to 00:01, fire order. Check split. | Use `UTC` internally, convert to `Local` for Report Query boundaries. | None (Data fix required). |
| **Double Counting**: Split bills counted as 2 full orders. | **Critical**: Revenue doubled. | **Data Review**: Inspect `Orders` table for parent/child relationship. | Ensure only "Child" or "Final" orders are summed, or tracked by Payment Transaction ID. | Fix query logic. |

## P1: Split Bill UI

| Failure Mode | Impact | Detection | Mitigation | Rollback |
| :--- | :--- | :--- | :--- | :--- |
| **Rounding Error**: $10 / 3 = $3.33 + $3.33 + $3.33 ($9.99 total). | **Medium**: Penny lost. | **Unit Test**: Split logic must adhere to "allocate remainder" rule. | Implement "Penny Allocator" algorithm in Backend. | Revert to old UI. |
| **Orphaned Items**: Item dragged to new bill disappears. | **High**: Free food. | **UI Test**: Count items before and after split. | Transactional Split API (All or Nothing). | Revert UI. |

## P2: Printer Hardware

| Failure Mode | Impact | Detection | Mitigation | Rollback |
| :--- | :--- | :--- | :--- | :--- |
| **Printer Offline**: App hangs waiting for printer. | **Medium**: Bad UX. | **Manual Timeout Test**. | Async print operation with timeout + "Retry" dialog. | None. |
