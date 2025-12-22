# 01 - UX & Layout Design

## Page Purpose

The **Table Workspace** is the operational command center for managing a single table session. It answers four critical questions at all times:

1. **What is the table status?** - OPEN, PAUSED, or PAYING
2. **Who started it and when?** - Server name and timestamp
3. **What has already been ordered?** - Complete history of sent orders
4. **What actions are available now?** - Context-sensitive controls

> [!IMPORTANT]
> This page is **NOT** a payment page. All payment workflows happen on the separate Payment Workspace.

---

## Section Responsibilities

### 1. Table Status Header (Top Bar)
**Purpose**: Persistent status display for operational awareness

| Element | Source | Notes |
|---------|--------|-------|
| Table Name | `TableStatusDto.Label` | e.g., "Bar 8" |
| Table Type | `TableStatusDto.Type` | Bar / Billiards / Dining |
| Status Badge | Backend-derived | OPEN, PAUSED, PAYING |
| Live Timer | Backend `StartTime` + display only | No calculation in UI |
| Started At | `TableStatusDto.StartTime` | Formatted timestamp |
| Started By | `TableStatusDto.Server` | Staff name |
| Quick Actions | Icons only | Pause, Transfer, Notes |

### 2. Primary Action Button (Header Area)
**Purpose**: Immediate access to order history

- Large, prominent "View All Ordered Items" button
- Opens read-only Ordered Items panel/page
- Shows all items grouped by send batch
- Displays item status (sent, pending, cancelled)

### 3. Menu Browsing Area (Left/Center ~70%)
**Purpose**: Browse and add items to current draft order

- Category tabs (Appetizers, Entrees, Beer, Cocktails, etc.)
- Touch-optimized menu cards with images
- "Add" button sends intent only to draft panel
- **NO**: Totals, discounts, payment info, hidden logic

### 4. Current Order Panel (Right ~30%)
**Purpose**: Manage items queued for the current interaction

| Element | Purpose |
|---------|---------|
| Item List | Items added in THIS interaction only |
| Quantity +/- | Adjust quantities |
| Remove Button | Remove item from draft |
| "Send Order" | Submit draft to backend |
| "Cancel Draft" | Clear current draft |

**NO**: Final totals, session close, payment acceptance

---

## User Mental Model

```
┌─────────────────────────────────────────────────────────────────┐
│  TABLE STATUS HEADER (ALWAYS VISIBLE)                          │
│  [OPEN] Bar 8 • Billiards • 01:42:18 • Started 7:18PM by Juan  │
│                                    [View All Ordered Items ➜]  │
├─────────────────────────────────────┬───────────────────────────┤
│  MENU BROWSING AREA                 │  CURRENT ORDER PANEL      │
│                                     │                           │
│  [All] [Apps] [Entrees] [Beer]...   │  Draft Order              │
│                                     │  ─────────────────        │
│  ┌──────┐ ┌──────┐ ┌──────┐        │  • French Fries   - 1 +   │
│  │ 🍟   │ │ 🍔   │ │ 🍺   │        │  • Heineken       - 1 +   │
│  │Fries │ │Burger│ │ Beer │        │                           │
│  │ +Add │ │ +Add │ │ +Add │        │                           │
│  └──────┘ └──────┘ └──────┘        │  [Send Order]             │
│                                     │  [Cancel Draft]           │
└─────────────────────────────────────┴───────────────────────────┘
```

---

## What is Intentionally NOT Shown

| Excluded Element | Reason |
|------------------|--------|
| Subtotals / Totals | Calculated by backend only, belongs to Payment Workspace |
| Tax calculations | Backend responsibility |
| Tip entry | Payment Workspace only |
| "Settle & Close Session" | Payment workflow, not order workflow |
| Payment method selection | Payment Workspace only |
| Discount application | Payment Workspace only |
| Change due | Payment Workspace only |
| Bill preview | Belongs to Payment Hub / Payment Workspace |

---

## Visual Design Principles

1. **Status Header is Always Visible** - Never scrolls, never hides
2. **"View All Ordered Items" is Prominent** - Large button, accent color
3. **Touch-First Design** - Large tap targets (44px minimum)
4. **Clear Visual Hierarchy** - Status > Actions > Browse > Draft
5. **No Clutter** - Only operational information, nothing computational
