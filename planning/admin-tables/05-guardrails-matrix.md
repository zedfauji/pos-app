# Guardrails & Risk Matrix: Admin Table CRUD

## Guardrails (System Invariants)

| Action | Constraint | Failure Mode | Validation Layer |
| :--- | :--- | :--- | :--- |
| **Create Table** | Name must be unique (optional, but good practice) | `400 Bad Request` | Repository/DB |
| **Delete Table** | **CRITICAL**: Cannot delete if `CurrentSessionId` is not null. | `409 Conflict` | repository `DeleteTableAsync` |
| **Deactivate Table** | Cannot deactivate if `Occupied`. | `409 Conflict` | repository `UpdateTableAsync` |
| **Edit Type** | Rate/Timer changes do not retroactively alter closed bills. | N/A (Business Logic) | System Architecture |
| **Move Table** | Moving an active session to a NEWLY deactivated table | `400 Bad Request` | `MoveSessionAsync` (Existing) |

## Risk Analysis

### Risk 1: Phantom Sessions
**Scenario**: Admin forces a delete on a table index that has an orphaned session (not correctly linked).
**Mitigation**: The system relies on `table_sessions.table_label` or `tables.current_session_id`. We must check the standard "Occupied" flag.

### Risk 2: Shift Integity
**Scenario**: Admin changes a "Bar" table to "Billiard" mid-shift while it is occupied.
**Impact**: The `Move logic` or `Stop logic` relies on the "Current Type" of the table. If type changes *underneath* a live session, the `StopSession` handler might calculate the bill using the *new* type's logic (e.g., adding a timer cost where none existed).
**Decision**: Allow this, but warn. It is a feature, not a bug, to be able to fix a wrong type assignment.
**Warning**: "This table has an active session. Changing its type will apply the NEW rules (Rates/Timers) when the session ends."

## Recovery Logic
If an Admin accidentally deletes a table (that was empty), they can just re-create it. Active sessions are protected, so no data loss there. Only configuration loss.
