-- =====================================================
-- RBAC System Rollback Script
-- =====================================================
-- This script removes the RBAC system from the database
-- WARNING: This will delete all RBAC data!
-- =====================================================

-- =====================================================
-- 1. REMOVE RBAC TABLES
-- =====================================================

-- Drop role inheritance table (must be first due to foreign keys)
DROP TABLE IF EXISTS users.role_inheritance CASCADE;

-- Drop role permissions table
DROP TABLE IF EXISTS users.role_permissions CASCADE;

-- Drop roles table
DROP TABLE IF EXISTS users.roles CASCADE;

-- =====================================================
-- 2. REVERT USERS TABLE (Optional)
-- =====================================================
-- Uncomment the following lines if you want to revert
-- the users table to its original structure

-- ALTER TABLE users.users DROP COLUMN IF EXISTS role;
-- ALTER TABLE users.users ADD COLUMN role VARCHAR(20) NOT NULL DEFAULT 'employee';

-- =====================================================
-- 3. REMOVE SCHEMA (Optional)
-- =====================================================
-- Uncomment the following line if you want to remove
-- the entire users schema

-- DROP SCHEMA IF EXISTS users CASCADE;

-- =====================================================
-- 4. VERIFICATION
-- =====================================================

-- Check if RBAC tables still exist
SELECT 
    schemaname,
    tablename,
    tableowner
FROM pg_tables 
WHERE schemaname = 'users' 
ORDER BY tablename;

-- =====================================================
-- END OF RBAC ROLLBACK SCRIPT
-- =====================================================
