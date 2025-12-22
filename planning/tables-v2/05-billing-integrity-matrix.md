# Billing Integrity Matrix: Session Moves

## The Problem
Different table types calculate money differently. Moving a session must not "lose" money or "create" money incorrectly.

## Matrix: Source (Row) -> Target (Column)

| Source \ Target | **HasTimer=True** (e.g. Billiard) | **HasTimer=False** (e.g. Bar) |
| :--- | :--- | :--- |
| **HasTimer=True** (Billiard) | **Scenario A: Transfer Time**<br>Timer continues. Rate *might* change if target rate differs.<br>*Risk*: High. Rate shift calculations.<br>*Rule*: Current Segment Closes. New Segment Starts. | **Scenario B: Stop & Finalize**<br>Timer stops immediately.<br>Time cost calculated up to NOW.<br>Added as `LineItem` ("Table Rent").<br>Session becomes "Flat". |
| **HasTimer=False** (Bar) | **Scenario C: Start New Timer**<br>Session was flat.<br>New Timer starts at 00:00.<br>Old orders carry over.<br>No "back pay" for previous time. | **Scenario D: Flat Move**<br>Just change Table ID.<br>Orders carry over.<br>No time implications. |

## Detailed Rules

### Scenario A: Timer -> New Timer (Different Rate)
*   **Example**: Regular Pool ($10/hr) -> VIP Pool ($20/hr).
*   **Action**: We cannot just "keep the clock running" because the math would be wrong (TotalTime * NewRate = WrongPrice).
*   **Implementation**:
    1.  Calculate Cost of Segment 1 (1 hr @ $10 = $10).
    2.  Add $10 as a "Previous Table Charge" (Hidden or Explicit).
    3.  Reset "Billable Timer" to 00:00 (Visual timer can show Total Time, but billing is split).
    4.  Start Segment 2 at $20/hr.
    *   *Alternative (Simpler)*: Calculate "Average Rate"? NO. Too complex.
    *   *Recommended*: Stop Time billing for Table 1, Add as item. Start Time billing for Table 2.

### Scenario B: Timer -> Flat
*   **Example**: Pool ($15 accrued) -> Bar.
*   **Action**:
    1.  System realizes Target `!HasTimer`.
    2.  Calculates final time: 1h 30m.
    3.  Calculates cost: $15.00.
    4.  Inserts `OrderItem` into session: "Table Rental: 1h 30m" @ $15.00.
    5.  Updates Session: `StartTime` = null? No, keep it for record, but flag as "TimerStopped".

### Scenario C: Flat -> Timer
*   **Example**: Bar -> Pool.
*   **Action**:
    1.  Session moves.
    2.  `StartTime` set to NOW.
    3.  User is NOT billed for the time they sat at the bar (obviously).
