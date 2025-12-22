# Execution Phases: Flexible Tables

## Phase 1: Backend Core (Safe Mode)
1.  **Schema Migration**: Run SQL to create `table_types` and populate defaults.
2.  **Entity Updates**: Update `Table.cs` and `TableStatusDto.cs` to support the new relationship.
3.  **Read Endpoint**: Implement `GET /api/table-types`.
4.  **Unit Tests**: Verify the Schema and CRUD mapping work.

## Phase 2: UI "Pre-Session" State
1.  **Frontend Logic**: Modify `TableWorkspaceViewModel` to NOT auto-start session.
2.  **UI Layout**: Implement the "Start Session Box" and "Server Name" input.
3.  **Command Binding**: Wire up "Start Session" button to `POST /start`.

## Phase 3: The Move Logic (The Hard Part)
1.  **Backend Service**: Refactor `MoveSession` to use the `BillingIntegrityMatrix`.
2.  **Frontend Dialog**: Create the "Move Table" confirmation flow.
3.  **Integration Testing**: Verify all 4 scenarios from the Matrix (Timer->Timer, Timer->Flat, etc.).

## Phase 4: Cleanup & Migration
1.  **Data Migration**: Switch all live tables to point to their new Types.
2.  **Legacy Cleanup**: Remove hardcoded "Billiard" vs "Bar" string checks in code.
