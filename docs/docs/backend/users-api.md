# UsersApi

The UsersApi handles user management, authentication, and Role-Based Access Control (RBAC).

## Overview

**Location:** `solution/backend/UsersApi/`  
**Port:** 5001 (local)  
**Database Schema:** `users`  
**Framework:** ASP.NET Core 8

## Key Features

- User CRUD operations
- Authentication (login)
- Role-Based Access Control (RBAC)
- Permission management
- Role management

## API Versioning

### v1 (Legacy)
- Route: `/api/users` or `/api/v1/users`
- **No RBAC enforcement** - Backward compatible
- All endpoints accessible (if authenticated)

### v2 (RBAC-Enabled)
- Route: `/api/v2/users`
- **RBAC enforcement** - Permission checks required
- All endpoints require specific permissions

## Controllers

### V2/UsersController

**Base Route:** `/api/v2/users`

#### Endpoints

**GET /api/v2/users**
- **Permission:** `user:view`
- **Description:** Get all users with pagination and filtering
- **Query Parameters:**
  - `SearchTerm` (string, optional)
  - `Role` (string, optional)
  - `IsActive` (bool?, optional)
  - `SortBy` (string, optional)
  - `SortDescending` (bool, optional)
  - `Page` (int, default: 1)
  - `PageSize` (int, default: 20)

**GET** `/api/v2/users/&#123;userId&#125;`
- **Permission:** `user:view`
- **Description:** Get user by ID

**POST** `/api/v2/users`
- **Permission:** `user:create`
- **Description:** Create a new user
- **Request Body:** `CreateUserRequest`

**PUT** `/api/v2/users/&#123;userId&#125;`
- **Permission:** `user:update`
- **Description:** Update an existing user
- **Request Body:** `UpdateUserRequest`

**DELETE** `/api/v2/users/&#123;userId&#125;`
- **Description:** Delete a user (soft delete)

**GET /api/v2/users/ping**
- **Permission:** None (health check)
- **Description:** Ping endpoint for health checks

### V2/AuthController

**Base Route:** `/api/v2/auth`

#### Endpoints

**POST /api/v2/auth/login**
- **Permission:** None (public endpoint)
- **Description:** User login
- **Request Body:** `LoginRequest`
- **Response:** `LoginResponse` (includes permissions)

### V2/RbacController

**Base Route:** `/api/v2/rbac`

#### Endpoints

**GET** `/api/v2/rbac/users/&#123;userId&#125;/permissions`
- **Permission:** `user:view`
- **Description:** Get all permissions for a user
- **Response:** `string[]` (array of permission strings)

**GET /api/v2/rbac/roles**
- **Permission:** `user:view`
- **Description:** Get all roles

**POST /api/v2/rbac/roles**
- **Permission:** `user:manage_roles`
- **Description:** Create a new role

## Services

### UsersService

**Interface:** `IUsersService`

**Key Methods:**
- `GetUsersAsync(UserSearchRequest, CancellationToken)` - Get users with filtering
- `GetUserByIdAsync(string, CancellationToken)` - Get user by ID
- `CreateUserAsync(CreateUserRequest, CancellationToken)` - Create user
- `UpdateUserAsync(string, UpdateUserRequest, CancellationToken)` - Update user
- `DeleteUserAsync(string, CancellationToken)` - Delete user
- `PingAsync(CancellationToken)` - Health check

### RbacService

**Interface:** `IRbacService`

**Key Methods:**
- `GetUserPermissionsAsync(string, CancellationToken)` - Get all user permissions
- `HasPermissionAsync(string, string, CancellationToken)` - Check single permission
- `HasAnyPermissionAsync(string, string[], CancellationToken)` - Check any permission
- `HasAllPermissionsAsync(string, string[], CancellationToken)` - Check all permissions

## Repositories

### UsersRepository

**Interface:** `IUsersRepository`

Database operations for users.

### RbacRepository

**Interface:** `IRbacRepository`

Database operations for RBAC (roles, permissions).

## Database Schema

### users.users

```sql
CREATE TABLE users.users (
    user_id UUID PRIMARY KEY,
    username TEXT NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    role TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE
);
```

### users.roles

```sql
CREATE TABLE users.roles (
    role_id UUID PRIMARY KEY,
    name TEXT NOT NULL UNIQUE,
    description TEXT,
    is_system_role BOOLEAN NOT NULL DEFAULT FALSE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE
);
```

### users.role_permissions

```sql
CREATE TABLE users.role_permissions (
    role_id UUID NOT NULL REFERENCES users.roles(role_id),
    permission TEXT NOT NULL,
    PRIMARY KEY (role_id, permission)
);
```

## Configuration

### appsettings.json

```json
{
  "Postgres": {
    "LocalConnectionString": "Host=localhost;Port=5432;Username=posapp;Password=Campus_66;Database=postgres",
    "CloudRunSocketConnectionString": "Host=/cloudsql/bola8pos:northamerica-south1:pos-app-1;Port=5432;Username=posapp;Password=Campus_66;Database=postgres;SSL Mode=Disable"
  }
}
```

## Deployment

### Cloud Run

```powershell
.\solution\backend\UsersApi\deploy-users-api.ps1
```

**Configuration:**
- Memory: 512Mi
- CPU: 1
- Min instances: 0
- Max instances: 10
- Timeout: 300s

## Permissions

All v2 endpoints require permissions:
- `user:view` - View users
- `user:create` - Create users
- `user:update` - Update users
- `user:delete` - Delete users
- `user:manage_roles` - Manage roles

## Related Documentation

- [RBAC Architecture](../../architecture/rbac-architecture.md)
- [Security - RBAC](../../security/rbac.md)
- [API Reference - Users](../../api/v2/users.md)
