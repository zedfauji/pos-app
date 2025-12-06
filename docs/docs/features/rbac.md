# RBAC Feature Documentation

This document provides comprehensive documentation for the Role-Based Access Control (RBAC) feature in MagiDesk POS.

## Overview

The RBAC system provides fine-grained access control across the entire application, ensuring users can only access features and data they're authorized to use.

## Key Features

- **47 Granular Permissions** across 11 categories
- **6 System Roles** with predefined permissions
- **Custom Roles** with permission assignment
- **Role Inheritance** for permission composition
- **Backend Enforcement** for security
- **Frontend Shadow** for UX

## Permission Categories

See [RBAC Architecture](../architecture/rbac-architecture) for complete permission list.

## System Roles

### Owner
- **Full Access**: All 47 permissions
- **Cannot be Modified**: System role
- **Use Case**: Business owner, system administrator

### Administrator
- **Operational Access**: Most permissions except system-level settings
- **User Management**: Can manage users and roles
- **Use Case**: IT administrator, operations manager

### Manager
- **Day-to-Day Operations**: View, order, payment, inventory, reports
- **No User Management**: Cannot manage users
- **Use Case**: Shift manager, floor manager

### Server
- **Table Management**: Table viewing, assignment, order creation
- **Limited Access**: Cannot process payments or manage inventory
- **Use Case**: Wait staff, table servers

### Cashier
- **Payment Processing**: Payment viewing and processing
- **Order Viewing**: Can view orders
- **Use Case**: Cashier, payment processor

### Host
- **Table Assignment**: Table viewing and assignment
- **Reservations**: Reservation management
- **Use Case**: Host, receptionist

## Usage

### Backend Usage

#### Protecting Endpoints

```csharp
[HttpGet]
[RequiresPermission(Permissions.USER_VIEW)]
public async Task<IActionResult> GetUsers()
{
    // Implementation
}
```

#### Checking Permissions Programmatically

```csharp
var hasPermission = await _rbacService.HasPermissionAsync(userId, Permissions.ORDER_CREATE);
if (hasPermission)
{
    // Allow operation
}
```

### Frontend Usage

#### Checking Permissions

```csharp
public bool CanCreateOrder => 
    _permissionManager.HasPermission(Permissions.ORDER_CREATE);
```

#### UI Visibility

```xml
<Button 
    Visibility="{x:Bind ViewModel.CanCreateOrder, Mode=OneWay}"
    Content="Create Order" />
```

## API Endpoints

### Get User Permissions

```http
GET /api/v2/rbac/users/{userId}/permissions
```

**Response**:
```json
[
  "user:view",
  "order:create",
  "payment:process"
]
```

### Get Role Permissions

```http
GET /api/v2/rbac/roles/{roleId}/permissions
```

**Response**:
```json
[
  "user:view",
  "user:create",
  "order:view",
  "order:create"
]
```

### Assign Permissions to Role

```http
POST /api/v2/rbac/roles/{roleId}/permissions
Content-Type: application/json

{
  "permissions": ["user:view", "user:create"]
}
```

## Best Practices

1. **Always Check Backend**: Frontend checks are for UX only
2. **Fail Closed**: Deny access if permission check fails
3. **Cache Permissions**: Cache permissions after login for performance
4. **Use Specific Permissions**: Use granular permissions, not broad roles

## Troubleshooting

See [Common Issues](../troubleshooting/common-issues#permission-errors) for troubleshooting permission issues.

---

**Last Updated**: 2025-01-02

