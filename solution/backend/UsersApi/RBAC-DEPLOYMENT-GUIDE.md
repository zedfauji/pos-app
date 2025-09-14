# RBAC System Deployment Guide

## Overview
This guide covers the deployment and management of the Role-Based Access Control (RBAC) system for the MagiDesk Users API.

## Files Created

### Database Scripts
- `rbac-migration.sql` - Complete RBAC schema and data migration
- `rbac-rollback.sql` - Rollback script to remove RBAC system
- `run-rbac-migration.ps1` - PowerShell script to run migration
- `run-rbac-rollback.ps1` - PowerShell script to run rollback

### API Components
- `Permissions.cs` - Permission definitions and constants
- `IRbacService.cs` - RBAC service interface
- `RbacService.cs` - RBAC business logic implementation
- `IRbacRepository.cs` - RBAC repository interface
- `RbacRepository.cs` - RBAC data access implementation
- `RbacController.cs` - REST API endpoints for RBAC
- `DatabaseInitializer.cs` - Updated with RBAC schema creation

## Deployment Steps

### 1. Database Migration

#### Option A: Using PowerShell Script (Recommended)
```powershell
# Navigate to the UsersApi directory
cd solution/backend/UsersApi

# Run the migration script
./run-rbac-migration.ps1
```

#### Option B: Manual SQL Execution
```bash
# Connect to Cloud SQL and run the migration
gcloud sql connect pos-app-1 --user=posapp --database=postgres --region=northamerica-south1 --file=rbac-migration.sql
```

### 2. Service Deployment

The RBAC system is already deployed with the Users API. If you need to redeploy:

```powershell
# Deploy the Users API with RBAC
./deploy-users-api.ps1
```

### 3. Verification

#### Check Database Schema
```sql
-- Verify tables exist
SELECT tablename FROM pg_tables WHERE schemaname = 'users';

-- Check system roles
SELECT name, description FROM users.roles WHERE is_system_role = true;

-- Check permissions
SELECT COUNT(DISTINCT permission) FROM users.role_permissions;
```

#### Test API Endpoints
```powershell
# Test permissions endpoint
Invoke-RestMethod -Uri "https://magidesk-users-23sbzjsxaq-pv.a.run.app/api/rbac/permissions" -Method Get

# Test system roles endpoint
Invoke-RestMethod -Uri "https://magidesk-users-23sbzjsxaq-pv.a.run.app/api/rbac/roles/system" -Method Get

# Test role stats
Invoke-RestMethod -Uri "https://magidesk-users-23sbzjsxaq-pv.a.run.app/api/rbac/roles/stats" -Method Get
```

## RBAC System Features

### System Roles
1. **Owner** - Full system access (53 permissions)
2. **Administrator** - System administration (53 permissions)
3. **Manager** - Store operations (39 permissions)
4. **Server** - Order taking and table management (15 permissions)
5. **Cashier** - Payment processing (11 permissions)
6. **Host** - Customer seating and reservations (11 permissions)

### Permission Categories
- User Management (5 permissions)
- Order Management (6 permissions)
- Table Management (4 permissions)
- Menu Management (5 permissions)
- Payment Management (4 permissions)
- Inventory Management (4 permissions)
- Reports & Analytics (5 permissions)
- System Settings (4 permissions)
- Customer Management (4 permissions)
- Reservation Management (4 permissions)
- Vendor Management (5 permissions)
- Session Management (3 permissions)

### API Endpoints

#### Role Management
- `GET /api/rbac/roles` - Get all roles (paginated)
- `GET /api/rbac/roles/{roleId}` - Get role by ID
- `GET /api/rbac/roles/name/{roleName}` - Get role by name
- `POST /api/rbac/roles` - Create custom role
- `PUT /api/rbac/roles/{roleId}` - Update custom role
- `DELETE /api/rbac/roles/{roleId}` - Delete custom role
- `GET /api/rbac/roles/stats` - Get role statistics
- `GET /api/rbac/roles/system` - Get system roles
- `GET /api/rbac/roles/custom` - Get custom roles

#### Permission Management
- `GET /api/rbac/permissions` - Get all permissions
- `GET /api/rbac/permissions/category/{category}` - Get permissions by category
- `GET /api/rbac/permissions/categories` - Get all categories
- `GET /api/rbac/users/{userId}/permissions` - Get user permissions
- `GET /api/rbac/roles/{roleId}/permissions` - Get role permissions
- `GET /api/rbac/roles/{roleId}/effective-permissions` - Get effective permissions
- `POST /api/rbac/users/{userId}/permissions/check` - Check single permission
- `POST /api/rbac/users/{userId}/permissions/check-any` - Check any permissions
- `POST /api/rbac/users/{userId}/permissions/check-all` - Check all permissions

#### Role Composition
- `POST /api/rbac/roles/validate-composition` - Validate role composition
- `POST /api/rbac/roles/{roleId}/can-inherit` - Check inheritance validity

## Troubleshooting

### Common Issues

#### 1. API Endpoints Return 500 Errors
**Cause**: Database schema not properly initialized
**Solution**: Run the migration script and restart the service

#### 2. Permission Checks Fail
**Cause**: User role not properly assigned
**Solution**: Verify user has correct role in database

#### 3. Custom Role Creation Fails
**Cause**: Invalid permissions or circular inheritance
**Solution**: Check permission names and inheritance chain

### Debugging Commands

```sql
-- Check user roles
SELECT u.username, u.role FROM users.users u WHERE u.is_active = true;

-- Check role permissions
SELECT r.name, COUNT(rp.permission) as permission_count
FROM users.roles r
LEFT JOIN users.role_permissions rp ON r.role_id = rp.role_id
GROUP BY r.role_id, r.name;

-- Check effective permissions for a user
WITH user_permissions AS (
    SELECT DISTINCT rp.permission
    FROM users.users u
    JOIN users.roles r ON u.role = r.name
    LEFT JOIN users.role_permissions rp ON r.role_id = rp.role_id
    WHERE u.username = 'admin'
)
SELECT permission FROM user_permissions ORDER BY permission;
```

## Rollback Procedure

If you need to remove the RBAC system:

```powershell
# Run the rollback script
./run-rbac-rollback.ps1 -Force
```

**Warning**: This will delete all RBAC data permanently!

## Maintenance

### Adding New Permissions
1. Add permission constant to `Permissions.cs`
2. Update permission descriptions
3. Run database migration to add new permissions
4. Update system roles as needed

### Creating Custom Roles
Use the API endpoints or create a custom migration script:

```sql
-- Example: Create a custom role
INSERT INTO users.roles (role_id, name, description, is_system_role, is_active)
VALUES ('custom-role-id', 'CustomRole', 'Custom role description', false, true);

-- Add permissions
INSERT INTO users.role_permissions (role_id, permission)
VALUES 
    ('custom-role-id', 'user:view'),
    ('custom-role-id', 'order:view');
```

## Security Considerations

1. **System Roles**: Cannot be modified or deleted
2. **Permission Validation**: All permissions are validated against predefined list
3. **Circular Inheritance**: Prevented by database constraints
4. **Soft Delete**: Roles are soft-deleted, not permanently removed
5. **Audit Trail**: All role changes are logged with timestamps

## Performance Notes

- All queries use proper indexes for optimal performance
- Role inheritance is resolved using recursive CTEs
- Permission checks are cached at the application level
- Database connections use connection pooling

## Support

For issues or questions:
1. Check the logs in Cloud Run console
2. Verify database schema using the verification queries
3. Test individual API endpoints
4. Check user permissions and role assignments
