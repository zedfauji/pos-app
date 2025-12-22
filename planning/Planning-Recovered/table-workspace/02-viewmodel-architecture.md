# 02 - ViewModel & State Design

## Overview

All ViewModels follow strict MVVM. **No business logic in ViewModels**. ViewModels are bridges between Views and Backend APIs.

---

## 1. TableWorkspaceViewModel (Main)

**Scope**: Orchestrates the entire Table Workspace page.

### Responsibilities
- Coordinates child ViewModels
- Handles page navigation parameters
- Manages page lifecycle (initialize, dispose)

### Inputs (DTOs)
| Source | DTO | Purpose |
|--------|-----|---------|
| Navigation | `string TableLabel` | Identifies the table |

### Properties (Bindings)
```csharp
// Child ViewModels
public TableStatusViewModel TableStatus { get; }
public CurrentOrderViewModel CurrentOrder { get; }
public OrderedItemsViewModel OrderedItems { get; }

// Menu State
public ObservableCollection<MenuCategoryDto> Categories { get; }
public ObservableCollection<MenuItemDto> MenuItems { get; }
public string SelectedCategory { get; set; }

// Page State
public bool IsLoading { get; }
public string ErrorMessage { get; }
```

### Commands
| Command | Action |
|---------|--------|
| `LoadCommand` | Initialize all data from APIs |
| `SelectCategoryCommand` | Filter menu items |
| `NavigateBackCommand` | Return to Table Map |

### Forbidden Logic
- ❌ Calculating totals
- ❌ Summing item prices
- ❌ Timer arithmetic
- ❌ Session duration calculation

---

## 2. TableStatusViewModel

**Scope**: Displays the Table Status Header.

### Responsibilities
- Display table operational state
- Refresh status from backend periodically

### Inputs (DTOs)
| DTO | Fields Used |
|-----|-------------|
| `TableStatusDto` | Label, Type, Occupied, CurrentSessionId, StartTime, Server |

### Properties (Bindings)
```csharp
public string TableLabel { get; }          // "Bar 8"
public string TableType { get; }           // "Billiards"
public string StatusBadge { get; }         // "OPEN" / "PAUSED" / "PAYING"
public string ElapsedTime { get; }         // "01:42:18" (display only, backend-calculated)
public string StartedAt { get; }           // "7:18 PM"
public string StartedBy { get; }           // "Juan"
public Guid? SessionId { get; }            // For API calls
public bool IsOccupied { get; }            // Visual state
```

### Commands
| Command | Action |
|---------|--------|
| `RefreshCommand` | Reload from `GET /tables` |
| `PauseCommand` | Intent: Pause session (if backend supports) |
| `TransferCommand` | Navigate to transfer flow |
| `NotesCommand` | Open notes sidebar |

### Forbidden Logic
- ❌ Calculating elapsed time (use `StartTime` + UI clock for display only)
- ❌ Deriving status from multiple fields

---

## 3. CurrentOrderViewModel

**Scope**: Manages the draft order for the current interaction.

### Responsibilities
- Hold items queued to be sent
- Quantity adjustments
- Submit order to backend

### Inputs (DTOs)
| DTO | Purpose |
|-----|---------|
| `MenuItemDto` | Source for adding items |

### Properties (Bindings)
```csharp
public ObservableCollection<DraftOrderItem> Items { get; }
public string TableLabel { get; }
public bool HasItems => Items.Any();  // Simple computed, allowed
public bool CanSend => HasItems;      // Simple gating, allowed
```

### DraftOrderItem (Display Model)
```csharp
public class DraftOrderItem
{
    public string ItemId { get; set; }
    public string Name { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }  // For display only
}
```

### Commands
| Command | Action |
|---------|--------|
| `AddItemCommand(MenuItemDto)` | Add to draft |
| `IncrementCommand(DraftOrderItem)` | Quantity++ |
| `DecrementCommand(DraftOrderItem)` | Quantity-- or remove |
| `RemoveCommand(DraftOrderItem)` | Remove from draft |
| `SendOrderCommand` | POST to `/tables/{label}/order` |
| `CancelDraftCommand` | Clear all draft items |

### Forbidden Logic
- ❌ Calculating order totals
- ❌ Applying discounts
- ❌ Tax calculation

---

## 4. OrderedItemsViewModel

**Scope**: Read-only view of all items ordered in the session.

### Responsibilities
- Fetch and display all sent orders
- Group by batch/time
- Show item status

### Inputs (DTOs)
| DTO | Purpose |
|-----|---------|
| `ItemLine` | Backend returns ordered items |

### Properties (Bindings)
```csharp
public ObservableCollection<OrderedItemGroup> ItemGroups { get; }
public bool IsLoading { get; }
public bool HasItems => ItemGroups.Any();
public string TableLabel { get; }
```

### OrderedItemGroup (Display Model)
```csharp
public class OrderedItemGroup
{
    public string BatchLabel { get; set; }   // "Sent 7:25 PM"
    public ObservableCollection<OrderedItem> Items { get; set; }
}

public class OrderedItem
{
    public string Name { get; set; }
    public int Quantity { get; set; }
    public string Status { get; set; }  // "Sent" / "Pending" / "Cancelled"
}
```

### Commands
| Command | Action |
|---------|--------|
| `LoadCommand` | GET `/tables/{label}/items` |
| `RefreshCommand` | Reload items |

### Forbidden Logic
- ❌ Calculating totals
- ❌ Deriving batch groupings from timestamps (backend should provide)
- ❌ Any status derivation logic

---

## Data Flow Diagram

```mermaid
graph TD
    A[TableWorkspacePage] --> B[TableWorkspaceViewModel]
    B --> C[TableStatusViewModel]
    B --> D[CurrentOrderViewModel]
    B --> E[OrderedItemsViewModel]
    
    C -->|GET /tables| F[TablesApi]
    D -->|POST /tables/{label}/order| F
    E -->|GET /tables/{label}/items| F
    
    G[IMenuApi] -->|GET /api/menu/items| B
```

---

## ViewModels NOT on this Page

| ViewModel | Location | Why Excluded |
|-----------|----------|--------------|
| PaymentWorkspaceViewModel | Payment Workspace | Different page |
| SplitPaymentViewModel | Payment Workspace | Payment logic |
| BillLedgerViewModel | Payment Workspace | Running totals |
