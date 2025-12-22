# UI Interaction Model: Table Workspace V2

## 1. Goal
Decouple "Viewing a Table" from "Starting a Session". Prevent accidental billing starts.

## 2. The "Pre-Session" State
**Entry**: User clicks an **Empty** table on the Map.
**View**: `TableWorkspacePage` opens.

### Visual Elements:
1.  **Header**: "Table: {Label} ({Type})" - e.g., "Table: Pool-1 (Billiard)".
2.  **Status Indicator**: Grey/Neutral "Session Not Started".
3.  **Main Content Area**:
    *   **NO** Menu Grid (Disabled).
    *   **Large Box**: "Start New Session".
    *   **Form inside Box**:
        *   `Server Name`: Searchable Dropdown or Text Input (Based on settings).
    *   **Button**: "START SESSION" (Green). Disabled until Server Name is valid.

## 3. The "Active Session" State
**Entry**: User clicks "START SESSION" above, OR clicks an **Occupied** table.
**View**: `TableWorkspacePage` refreshes to Active mode.

### Visual Elements:
1.  **Header**: Same.
2.  **Status Indicator**:
    *   If `Type.HasTimer`: Green "Active Check: 01:23:45".
    *   If `!Type.HasTimer`: Blue "Active Check".
3.  **Main Content Area**:
    *   Menu Grid is Visible.
    *   Order Lines are Visible.

## 4. The "Move Table" Flow
**Trigger**: Button "Move Table" in Action Bar.

### Interaction:
1.  **Dialog Opens**: "Select Target Table".
2.  **User Selects Target**: e.g., "Bar-5".
3.  **System Check**: Compares Source Type vs. Target Type.
4.  **Confirmation Dialog** (If rules change):
    *   *Warning*: "Moving from Billiard to Bar will STOP the timer. Correct?"
    *   *Confirm*: User clicks "Yes, Move & Stop Timer".
5.  **Execution**: API called. Workspace refreshes to new table label.
