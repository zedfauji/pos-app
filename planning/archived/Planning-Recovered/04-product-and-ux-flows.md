# Product & UX Flow Planning

## 1. Screen Inventory (The New Hierarchy)

### A. Authentication
- **Login Screen**: Simple User/Pass entry.
    - *Success*: Navigate to Dashboard (if Admin) or PinPad (if Waiter).
    - *Failure*: Show "Invalid Credentials" Toast.
- **PinPad Screen**: Quick access for waiters.

### B. Table Map (Home)
- **Visuals**: Grid of tables.
- **States**: Free (Green), Occupied (Red + Timer), Payment Pending (Yellow).
- **Interactions**: Click -> Open Order Details.

### C. Order Details (Transactional Core)
- **Left Panel**: Inventory Category List.
- **Center Grid**: Inventory Items (Click to Add).
- **Right Panel**: Current Bill (List of Items, Taxes, Total).
- **Buttons**: [Fire Order], [Split Bill], [Pay], [Print].

### D. Payment Modal
- **States**: Select Payment Method (Cash/Card) -> Enter Amount -> Confirm.
- **Result**: Close Modal -> Show Receipt Options -> Navigate back to Table Map.

## 2. Critical User Flows

### Flow 1: The "Digital Waiter" (Happy Path)
1.  **Start**: Waiter logs in via PIN.
2.  **Select**: Taps "Table 5" (Empty).
3.  **Action**: System auto-creates "Draft Order" via API (or holds in memory until first item added). *Decision: Hold in Memory.*
4.  **Add**: Taps "Burger", "Coke".
5.  **Fire**: Taps "Send to Kitchen".
6.  **System**: Calls `POST /api/orders`.
7.  **Visual**: Table 5 turns Red.

### Flow 2: The "Split Bill" (Complex)
1.  **Start**: Open Table 5.
2.  **Action**: Tap "Split Bill".
3.  **Visual**: Show "Original Bill" on Left, "New Bill 1" on Right.
4.  **Action**: Drag "Burger" to "New Bill 1".
5.  **Confirm**: Tap "Save Split".
6.  **System**: Calls `POST /api/billings/{id}/split`.
7.  **Result**: UI reloads to show 2 open bills for Table 5.

## 3. State Transitions & "The Truth"

| UI State | Source of Truth |
|----------|-----------------|
| **Table List** | `GET /api/tables` (Polled every 5s) |
| **Menu Items** | `GET /api/menu` (Cached on Login) |
| **Cart (Unsent)** | Local `ObservableCollection` (Transient) |
| **Bill Total** | `GET /api/orders/{id}/calculate` (Server Math) |

## 4. Error Scenarios & Offline Behavior

### A. "The Tunnel" (Network Loss)
- **Scenario**: Waiter taps "Send to Kitchen" but WiFi drops.
- **Response**:
    1.  Top Bar shows "⚠️ Offline".
    2.  Screen locks or disables "Write" buttons.
    3.  Spinner runs for 10s -> "Connection Failed. Retrying...".
    4.  **No Local Save**: We do NOT save the order to a local DB. The waiter must write it on paper if the system is down.

### B. "The Sync Conflict"
- **Scenario**: Waiter A opens Table 5. Waiter B opens Table 5 on another device and pays.
- **Response**:
    1.  Waiter A tries to add item.
    2.  API returns `409 Conflict` (Order Closed).
    3.  Client catches 409 -> Shows "Order Modified by Another User" -> Reloads.
