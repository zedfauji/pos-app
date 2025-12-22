# Feature Scope: Admin Table CRUD & Management

## 1. Problem Statement
The restaurant manager needs a way to modify the arrangement of tables, add new ones (e.g., for events), and adjust pricing/business rules for table types (e.g., changing billiard rates) without requesting technical database intervention.

## 2. In-Scope Functionalities
### A. Table Management
- **List All Tables**: View all tables including inactive ones.
- **Create Table**: Add a new table with Name, Type, and Capacity.
- **Update Table**: Rename, Change Type, Change Capacity.
- **Delete Table**: Soft-delete (mark inactive). *Strictly blocked if Occupied.*

### B. Table Type Management
- **List Types**: View standard types (Bar, Billiard) and their rules.
- **Update Type Config**:
  - `HourlyRate`: Cost per hour.
  - `HasTimer`: Whether sessions track duration.
  - `RequiresServer`: Whether a server name is mandatory to start.
  - `AllowOrders`: Whether POS items can be added.

## 3. Out-of-Scope
- **Creating New Config Properties**: We cannot add new columns like "HasPoolCues" dynamically.
- **Visual Map Editor**: No drag-and-drop layout editing. Purely list-based management.
- **Deleting Types**: Types are hard-linked to logic/Reporting. Deletion is too risky for this iteration.

## 4. User Roles
- **Administrator**: Full Access.
- **Manager**: Full Access (same as Admin for now).
- **Server/Employee**: No Access (Hidden from Menu).
