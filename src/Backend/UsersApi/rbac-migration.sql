-- =====================================================
-- RBAC System Migration Scripts
-- =====================================================
-- This file contains all SQL scripts needed to set up
-- the Role-Based Access Control (RBAC) system
-- =====================================================

-- =====================================================
-- 1. SCHEMA CREATION
-- =====================================================

-- Create users schema if it doesn't exist
CREATE SCHEMA IF NOT EXISTS users;

-- =====================================================
-- 2. ROLES TABLE
-- =====================================================

-- Create roles table
CREATE TABLE IF NOT EXISTS users.roles (
    role_id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE,
    description TEXT,
    is_system_role BOOLEAN NOT NULL DEFAULT false,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    is_deleted BOOLEAN NOT NULL DEFAULT false
);

-- Create indexes for roles table
CREATE INDEX IF NOT EXISTS idx_roles_name ON users.roles(name) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS idx_roles_system ON users.roles(is_system_role) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS idx_roles_active ON users.roles(is_active) WHERE is_deleted = false;

-- =====================================================
-- 3. ROLE PERMISSIONS TABLE
-- =====================================================

-- Create role_permissions table
CREATE TABLE IF NOT EXISTS users.role_permissions (
    role_id VARCHAR(50) NOT NULL,
    permission VARCHAR(100) NOT NULL,
    PRIMARY KEY (role_id, permission),
    FOREIGN KEY (role_id) REFERENCES users.roles(role_id) ON DELETE CASCADE
);

-- Create indexes for role_permissions table
CREATE INDEX IF NOT EXISTS idx_role_permissions_role ON users.role_permissions(role_id);
CREATE INDEX IF NOT EXISTS idx_role_permissions_permission ON users.role_permissions(permission);

-- =====================================================
-- 4. ROLE INHERITANCE TABLE
-- =====================================================

-- Create role_inheritance table
CREATE TABLE IF NOT EXISTS users.role_inheritance (
    child_role_id VARCHAR(50) NOT NULL,
    parent_role_id VARCHAR(50) NOT NULL,
    PRIMARY KEY (child_role_id, parent_role_id),
    FOREIGN KEY (child_role_id) REFERENCES users.roles(role_id) ON DELETE CASCADE,
    FOREIGN KEY (parent_role_id) REFERENCES users.roles(role_id) ON DELETE CASCADE,
    CHECK (child_role_id != parent_role_id)
);

-- Create indexes for role_inheritance table
CREATE INDEX IF NOT EXISTS idx_role_inheritance_child ON users.role_inheritance(child_role_id);
CREATE INDEX IF NOT EXISTS idx_role_inheritance_parent ON users.role_inheritance(parent_role_id);

-- =====================================================
-- 5. USERS TABLE (Updated for RBAC)
-- =====================================================

-- Create users table (updated to reference role names)
CREATE TABLE IF NOT EXISTS users.users (
    user_id VARCHAR(50) PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    role VARCHAR(50) NOT NULL DEFAULT 'Server',
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    is_active BOOLEAN NOT NULL DEFAULT true,
    is_deleted BOOLEAN NOT NULL DEFAULT false
);

-- Create indexes for users table
CREATE INDEX IF NOT EXISTS idx_users_username ON users.users(username) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS idx_users_role ON users.users(role) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS idx_users_active ON users.users(is_active) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS idx_users_created_at ON users.users(created_at) WHERE is_deleted = false;

-- =====================================================
-- 6. SYSTEM ROLES INITIALIZATION
-- =====================================================

-- Insert system roles
INSERT INTO users.roles (role_id, name, description, is_system_role, is_active, created_at, updated_at)
VALUES 
    ('system-owner', 'Owner', 'Full system access, business owner', true, true, NOW(), NOW()),
    ('system-administrator', 'Administrator', 'System administration and user management', true, true, NOW(), NOW()),
    ('system-manager', 'Manager', 'Store operations, staff management, reports', true, true, NOW(), NOW()),
    ('system-server', 'Server', 'Order taking, table management, customer service', true, true, NOW(), NOW()),
    ('system-cashier', 'Cashier', 'Payment processing, basic order operations', true, true, NOW(), NOW()),
    ('system-host', 'Host', 'Customer seating, reservation management', true, true, NOW(), NOW())
ON CONFLICT (role_id) DO NOTHING;

-- =====================================================
-- 7. SYSTEM ROLE PERMISSIONS
-- =====================================================

-- Owner gets all permissions (53 permissions)
INSERT INTO users.role_permissions (role_id, permission)
VALUES 
    ('system-owner', 'user:view'),
    ('system-owner', 'user:create'),
    ('system-owner', 'user:update'),
    ('system-owner', 'user:delete'),
    ('system-owner', 'user:manage_roles'),
    ('system-owner', 'order:view'),
    ('system-owner', 'order:create'),
    ('system-owner', 'order:update'),
    ('system-owner', 'order:delete'),
    ('system-owner', 'order:cancel'),
    ('system-owner', 'order:complete'),
    ('system-owner', 'table:view'),
    ('system-owner', 'table:manage'),
    ('system-owner', 'table:assign'),
    ('system-owner', 'table:clear'),
    ('system-owner', 'menu:view'),
    ('system-owner', 'menu:create'),
    ('system-owner', 'menu:update'),
    ('system-owner', 'menu:delete'),
    ('system-owner', 'menu:price_change'),
    ('system-owner', 'payment:view'),
    ('system-owner', 'payment:process'),
    ('system-owner', 'payment:refund'),
    ('system-owner', 'payment:void'),
    ('system-owner', 'inventory:view'),
    ('system-owner', 'inventory:update'),
    ('system-owner', 'inventory:adjust'),
    ('system-owner', 'inventory:restock'),
    ('system-owner', 'report:view'),
    ('system-owner', 'report:sales'),
    ('system-owner', 'report:inventory'),
    ('system-owner', 'report:user_activity'),
    ('system-owner', 'report:export'),
    ('system-owner', 'settings:view'),
    ('system-owner', 'settings:update'),
    ('system-owner', 'settings:system'),
    ('system-owner', 'settings:receipt'),
    ('system-owner', 'customer:view'),
    ('system-owner', 'customer:create'),
    ('system-owner', 'customer:update'),
    ('system-owner', 'customer:delete'),
    ('system-owner', 'reservation:view'),
    ('system-owner', 'reservation:create'),
    ('system-owner', 'reservation:update'),
    ('system-owner', 'reservation:cancel'),
    ('system-owner', 'vendor:view'),
    ('system-owner', 'vendor:create'),
    ('system-owner', 'vendor:update'),
    ('system-owner', 'vendor:delete'),
    ('system-owner', 'vendor:order'),
    ('system-owner', 'session:view'),
    ('system-owner', 'session:manage'),
    ('system-owner', 'session:close')
ON CONFLICT (role_id, permission) DO NOTHING;

-- Administrator permissions (53 permissions - same as Owner for now)
INSERT INTO users.role_permissions (role_id, permission)
SELECT 'system-administrator', permission FROM users.role_permissions WHERE role_id = 'system-owner'
ON CONFLICT (role_id, permission) DO NOTHING;

-- Manager permissions (39 permissions)
INSERT INTO users.role_permissions (role_id, permission)
VALUES 
    ('system-manager', 'order:view'),
    ('system-manager', 'order:create'),
    ('system-manager', 'order:update'),
    ('system-manager', 'order:complete'),
    ('system-manager', 'table:view'),
    ('system-manager', 'table:manage'),
    ('system-manager', 'table:assign'),
    ('system-manager', 'table:clear'),
    ('system-manager', 'menu:view'),
    ('system-manager', 'menu:create'),
    ('system-manager', 'menu:update'),
    ('system-manager', 'menu:price_change'),
    ('system-manager', 'payment:view'),
    ('system-manager', 'payment:process'),
    ('system-manager', 'payment:refund'),
    ('system-manager', 'inventory:view'),
    ('system-manager', 'inventory:update'),
    ('system-manager', 'inventory:adjust'),
    ('system-manager', 'inventory:restock'),
    ('system-manager', 'report:view'),
    ('system-manager', 'report:sales'),
    ('system-manager', 'report:inventory'),
    ('system-manager', 'report:export'),
    ('system-manager', 'settings:view'),
    ('system-manager', 'settings:update'),
    ('system-manager', 'customer:view'),
    ('system-manager', 'customer:create'),
    ('system-manager', 'customer:update'),
    ('system-manager', 'reservation:view'),
    ('system-manager', 'reservation:create'),
    ('system-manager', 'reservation:update'),
    ('system-manager', 'reservation:cancel'),
    ('system-manager', 'vendor:view'),
    ('system-manager', 'vendor:create'),
    ('system-manager', 'vendor:update'),
    ('system-manager', 'vendor:order'),
    ('system-manager', 'session:view'),
    ('system-manager', 'session:manage'),
    ('system-manager', 'session:close')
ON CONFLICT (role_id, permission) DO NOTHING;

-- Server permissions (15 permissions)
INSERT INTO users.role_permissions (role_id, permission)
VALUES 
    ('system-server', 'order:view'),
    ('system-server', 'order:create'),
    ('system-server', 'order:update'),
    ('system-server', 'order:complete'),
    ('system-server', 'table:view'),
    ('system-server', 'table:manage'),
    ('system-server', 'table:assign'),
    ('system-server', 'menu:view'),
    ('system-server', 'customer:view'),
    ('system-server', 'customer:create'),
    ('system-server', 'customer:update'),
    ('system-server', 'reservation:view'),
    ('system-server', 'reservation:create'),
    ('system-server', 'reservation:update'),
    ('system-server', 'session:view')
ON CONFLICT (role_id, permission) DO NOTHING;

-- Cashier permissions (11 permissions)
INSERT INTO users.role_permissions (role_id, permission)
VALUES 
    ('system-cashier', 'order:view'),
    ('system-cashier', 'order:update'),
    ('system-cashier', 'order:complete'),
    ('system-cashier', 'menu:view'),
    ('system-cashier', 'payment:view'),
    ('system-cashier', 'payment:process'),
    ('system-cashier', 'payment:refund'),
    ('system-cashier', 'customer:view'),
    ('system-cashier', 'customer:create'),
    ('system-cashier', 'customer:update'),
    ('system-cashier', 'session:view')
ON CONFLICT (role_id, permission) DO NOTHING;

-- Host permissions (11 permissions)
INSERT INTO users.role_permissions (role_id, permission)
VALUES 
    ('system-host', 'table:view'),
    ('system-host', 'table:manage'),
    ('system-host', 'table:assign'),
    ('system-host', 'customer:view'),
    ('system-host', 'customer:create'),
    ('system-host', 'customer:update'),
    ('system-host', 'reservation:view'),
    ('system-host', 'reservation:create'),
    ('system-host', 'reservation:update'),
    ('system-host', 'reservation:cancel'),
    ('system-host', 'session:view')
ON CONFLICT (role_id, permission) DO NOTHING;

-- =====================================================
-- 8. DEFAULT ADMIN USER
-- =====================================================

-- Create default admin user if no users exist
INSERT INTO users.users (user_id, username, password_hash, role, created_at, updated_at, is_active, is_deleted)
SELECT 
    'admin-user-id',
    'admin',
    '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', -- BCrypt hash for '123456'
    'Administrator',
    NOW(),
    NOW(),
    true,
    false
WHERE NOT EXISTS (SELECT 1 FROM users.users WHERE username = 'admin' AND is_deleted = false);

-- =====================================================
-- 9. VERIFICATION QUERIES
-- =====================================================

-- Verify system roles
SELECT 'System Roles' as check_type, COUNT(*) as count FROM users.roles WHERE is_system_role = true AND is_deleted = false;

-- Verify permissions
SELECT 'Total Permissions' as check_type, COUNT(DISTINCT permission) as count FROM users.role_permissions;

-- Verify role permissions
SELECT 
    r.name as role_name,
    COUNT(rp.permission) as permission_count
FROM users.roles r 
LEFT JOIN users.role_permissions rp ON r.role_id = rp.role_id 
WHERE r.is_deleted = false 
GROUP BY r.role_id, r.name 
ORDER BY r.is_system_role DESC, r.name;

-- =====================================================
-- END OF RBAC MIGRATION SCRIPT
-- =====================================================
