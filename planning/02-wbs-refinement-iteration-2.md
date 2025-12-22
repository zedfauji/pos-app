# WBS Refinement - Iteration 2 (Prerequisite Check)

## 1. Prerequisite Scan

| Task | Prerequisite | Status Assumed | Risk |
|------|--------------|----------------|------|
| **1.3. API Client** | Backend Swagger/OpenAPI Spec | **MISSING** | **High**. If Backend doesn't expose Swagger, we have to write clients manually. |
| **2.1. Auth Service** | Backend Auth Endpoint (`/api/auth/login`) | **Exists** | Low. (Observed in `UsersApi`). |
| **3.2. Table Map UI** | Assets (Table Icons, etc.) | **Exists** | Low. (In legacy `Assets` folder). |
| **4.2.1. Printing** | Backend Receipt Generator Endpoint | **MISSING** | **Critical**. The current backend relies on the Client generating the PDF. |

## 2. Blocking Issues Identified
- **The Printing Blocker**: The legacy app generates PDFs locally (`ReceiptBuilder.cs`). The new "Thin Client" rule says "Backend owns logic".
- **Resolution Required**: We need a task to **port `ReceiptBuilder` logic to the Backend API**. The Client cannot be "Thin" if it has to know how to layout a receipt.

## 3. Refined WBS Tasks (Delta)

### 1.0. Backend Preparation (New Section)
- **1.0.1. Expose Swagger**
    - **Action**: Ensure `Swashbuckle` is enabled in Backend.
- **1.0.2. Port Receipt Logic**
    - **Action**: Move `ReceiptBuilder.cs` logic to `OrderApi`.
    - **Endpoint**: `GET /api/orders/{id}/receipt-preview` (Returns PDF/Image/HTML).
    - **Endpoint**: `GET /api/orders/{id}/print-data` (Returns ESC/POS bytes).

### 5. Offline Handling (Refined)
- **5.1. Resilience Policy**
    - **New**: Use `Polly` for transient retries (408, 503).
    - **Constraint**: Retry logic lives in `Program.cs` / DI config, not in ViewModels.
