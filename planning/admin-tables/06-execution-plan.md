# Execution Plan: Admin Table CRUD

## Phase 1: Backend Implementation
1.  **Repository Layer (`TableRepository`)**:
    - Implement `AddTableAsync(Table table)`
    - Implement `UpdateTableAsync(Table table)`
    - Implement `DeleteTableAsync(Guid tableId)`
    - Implement `UpdateTableTypeAsync(TableType type)`
2.  **Controller Layer (`TablesController`)**:
    - Add `POST /tables` -> Calls Repo Add
    - Add `PUT /tables/{id}` -> Calls Repo Update
    - Add `DELETE /tables/{id}` -> Calls Repo Delete
    - Add `PUT /tables/types/{id}` -> Calls Repo UpdateType

## Phase 2: Frontend Service & ViewModel
1.  **Refit API (`ITableApi`)**:
    - Add the 4 new endpoints.
2.  **`TableManagementViewModel`**:
    - `ObservableCollection<TableStatusDto> Tables`
    - `ObservableCollection<TableTypeDto> Types`
    - `LoadDataAsync()`: Fetches both lists.
    - `AddTableCommand`: Shows Dialog -> Calls API -> Reloads.
    - `EditTableCommand`: Shows Dialog (Pre-filled) -> Calls API -> Reloads.
    - `DeleteTableCommand`: Confirmation -> Calls API -> Reloads.
    - `EditTypeCommand`: Shows Dialog -> Calls API -> Reloads.

## Phase 3: Frontend UI (`TableManagementPage.xaml`)
1.  **Structure**:
    - `Pivot` Control with 2 Items: "Tables" and "Types".
2.  **Tables Tab**:
    - `DataGrid` (CommunityToolkit or standard Grid) listing tables.
    - "Add" Button in top right.
3.  **Types Tab**:
    - `DataGrid` listing types.
    - "Edit" Button in row.
4.  **Navigation**:
    - Add "Admin: Tables" to `ShellPage` navigation menu (conditional on Admin role).

## Phase 4: Verification
1.  Manual Test: Add a table "Test 1". Check Main Map.
2.  Manual Test: Edit "Test 1" to "Test 2". Check Map.
3.  Manual Test: Start session on "Test 2". Try to Delete. Expect Error.
4.  Manual Test: Edit Rate of Type "Billiard". Verify logic.
