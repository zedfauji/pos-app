# DatabaseInitializer Fix - TablesApi

**Date**: 2025-12-23  
**Issue**: TablesApi failing to start due to column reference error

## Problem

**Error**: `column "table_session_id" does not exist`  
**Location**: `TablesApi/Services/DatabaseInitializer.cs:133`  
**Root Cause**: Index creation referenced non-existent column `table_session_id`

## Root Cause Analysis

1. **Actual Database Schema**: `public.bills` has `session_id` (uuid), not `table_session_id` (integer)
2. **DatabaseInitializer Code**: Tried to create index on `table_session_id` which doesn't exist
3. **Table Already Exists**: The `public.bills` table already exists with different structure, so `CREATE TABLE IF NOT EXISTS` doesn't recreate it, but index creation fails

## Fix Applied

**File**: `solution/backend/TablesApi/Services/DatabaseInitializer.cs`

**Changed**:
- Line 125: `table_session_id INTEGER` → `session_id UUID`
- Line 125: Reference changed from `public."TableSessions"("Id")` → `public."TableSessions"(session_id)`
- Line 133: Index name changed from `idx_bills_table_session_id` → `idx_bills_session_id`
- Line 133: Column changed from `table_session_id` → `session_id`

**Before**:
```sql
table_session_id INTEGER NOT NULL REFERENCES public."TableSessions"("Id"),
...
CREATE INDEX IF NOT EXISTS idx_bills_table_session_id ON public.bills(table_session_id);
```

**After**:
```sql
session_id UUID REFERENCES public."TableSessions"(session_id),
...
CREATE INDEX IF NOT EXISTS idx_bills_session_id ON public.bills(session_id);
```

## Verification

- ✅ TablesApi builds successfully
- ✅ Index creation now references correct column
- ✅ Matches actual database schema structure

## Notes

- `public.bills` is deprecated in favor of `billing.bills`
- The DatabaseInitializer creates this table for backward compatibility
- The actual table structure differs from what the initializer tries to create, but `CREATE TABLE IF NOT EXISTS` prevents conflicts
- The index creation was the only part that failed because it referenced a non-existent column

---

**Status**: ✅ Fixed - TablesApi should now start successfully

