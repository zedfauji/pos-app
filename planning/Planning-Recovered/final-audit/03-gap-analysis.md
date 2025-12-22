# 03 - Gap Analysis (Legacy → New)

| Legacy Feature | Exists in New System | Status | Migration Decision |
| :--- | :--- | :--- | :--- |
| **Table Map** | Yes | Complete | **Port** (Improved UI) |
| **Ordering** | Yes | Complete | **Port** (API Driven) |
| **End-of-Day Report** | **No** | **Missing** | **MUST HAVE** (Critical for ops) |
| **Shift Reports** | **No** | **Missing** | **MUST HAVE** |
| **Offline Writes** | No | Dropped | **Drop** (Accepted: "Read-Only" Mode) |
| **Split Bill** | Yes (API) | Partial | **Port** (UI needs validation) |
| **Void/Refund** | Unclear | Unknown | **Validation Required** |
| **Cash Drawer Kick** | Implicit | Partial | **Port** (Via Printer Driver) |

## Gap Explanation

### 1. Reporting (Critical)
**Impact**: High. Managers cannot close the day or reconcile cash.
**Decision**: Omission is **NOT ACCEPTABLE**. Must be implemented before full production.

### 2. Offline Mode (Architecture Change)
**Impact**: High. Internet loss = Operations halt (except read).
**Decision**: **Acceptable**. This was an explicit architectural choice to avoid "Sync Conflict" hell. Waiters resort to paper during downtime.

### 3. Voids / Refunds
**Impact**: Medium. Mistakes happen.
**Decision**: Need to confirm if `StopSession` or a separate `RefundApi` handles corrections. Currently exposed APIs do not clearly show "Refund".
