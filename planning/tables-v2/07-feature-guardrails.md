# Feature Guardrails: Flexible Table Types

## 1. Financial Invariants (The "Do Not Lose Money" Rules)

> [!IMPORTANT]
> These rules must be enforced at the **Database Transaction** level where possible, or within a `lock` block in the Application Layer.

1.  **Atomic Move Operations**: A session move across types (e.g., Timer -> Flat) is a destructive financial event. It must be wrapped in a single transaction that:
    *   Calculates the final time cost.
    *   Creates the Bill Line Item.
    *   Updates the Session State.
    *   *Failure in any step must roll back the entire move.*
2.  **No "Null" Rates**: If a Table Type has `HasTimer=true`, `HourlyRate` MUST be a valid non-negative decimal. It cannot be NULL.
3.  **Positive Time Delta**: When finalizing a timer session during a move, the `EndTime` must strictly be greater than or equal to `StartTime`. If system clock skew causes `End < Start`, default to 0 duration, never negative cost.

## 2. Operational Guardrails

> [!WARNING]
> Operational rules prevent staff from making mistakes that require manager intervention.

1.  **Server Assignment**: If `TableType.RequiresServer = true`, the API **MUST** reject any `StartSession` request that provides a null or empty `ServerId`. The UI validation is not enough; the backend must enforce this.
2.  **Orphaned Sessions**: A table cannot be deleted if it has an `Open` session. The session must be closed or moved first.
3.  **Type Modification Locking**: An Admin cannot change a Table's `Type` (e.g., from Billiard to Bar) while there is an **Active Session** on that table.
    *   *Why?* It confuses the billing engine. The session started under one set of rules and would end under another.
    *   *Fix*: Table must be empty to change its Type.

## 3. Legacy Compatibility & Data Integrity

> [!CAUTION]
> We share a database with a Legacy System we cannot touch.

1.  **The "Phantom" Type Column**: We are creating `table_types` and linking via `type_id`. However, we MUST currently maintain the legacy string `type` column in the `tables` table.
    *   *Guardrail*: Any write to `type_id` must also sync the legacy `type` string (e.g., `type_id=1 (Billiard)` -> `type='billiard'`).
    *   *Why?* The legacy frontend likely reads the string column. If we stop updating it, the old dashboard will show blank types.
2.  **Default Handling**: If a `table` has a `type_id` that points to a deleted type (integrity error), the system must fallback to "Bar" behavior (Safest state: No Timer) rather than crashing the controller.

## 4. Audit & Recovery

1.  **Move logging**: Every "Move Session" action must handle an Audit Log entry recording:
    *   `FromTable` / `ToTable`
    *   `FromType` / `ToType`
    *   `TimeCostFinalized` (if applicable)
    *   `User` who performed the move.
2.  **Session Recovery**: If the server restarts, all `HasTimer=true` sessions must correctly resume counting from their original `StartTime`. RAM-based timers are forbidden; always calculate `Now - StartTime`.
