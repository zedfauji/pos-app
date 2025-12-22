# UI Wireframe: Table Management Page

## Layout: Tab control
The page will have two main tabs: **Tables** and **Service Configuration**.

### Tab 1: Tables (Instance Management)
A `DataGrid` listing all tables.
- **Columns**: ID (Hidden), Name, Type (Badge), Capacity, Status (Active/Inactive), Actions.
- **Top Bar**: `[+ Add New Table]` Button.
- **Row Actions**: Context Menu or Buttons -> `Edit`, `Delete`.

#### "Add/Edit Table" Dialog
- **Header**: "Create New Table" or "Edit Table 1"
- **Fields**:
  - Name: `TextBox`
  - Type: `ComboBox` (Loaded from Table Types)
  - Capacity: `NumberBox`
  - Is Active: `ToggleSwitch` (Edit Mode only)
- **Footer**: `[Cancel]` `[Save]`

### Tab 2: Service Configuration (Types)
A `DataGrid` listing Table Types.
- **Columns**: Name, Hourly Rate, Has Timer (Yes/No), Requires Server (Yes/No), Allow Orders (Yes/No), Actions.
- **Row Actions**: `Edit`. (Add/Delete disabled for now per scope).

#### "Edit Service Rules" Dialog
- **Header**: "Edit Configuration: Billiard"
- **Fields**:
  - Name: `TextBox`
  - Hourly Rate: `NumberBox` (Currency format)
  - Has Timer: `ToggleSwitch`
  - Requires Server to Start: `ToggleSwitch`
  - Allow Ordering Items: `ToggleSwitch`
- **Warning**: "Changing these rules will affect how new sessions are calculated. Active sessions maintain their current logic until stopped/moved."
- **Footer**: `[Cancel]` `[Save]`

## Access Control
This page is only accessible via the "Settings" or "Admin" menu in the Shell.
Validation check: `AuthService.IsAdmin` must be true.
