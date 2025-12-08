# RBAC Deployment Issues - Database Connection

**Date:** 2025-01-27  
**Status:** 🔴 Critical Issue - Database Connection Failing

## Issue Summary

UsersApi is deployed but cannot connect to the PostgreSQL database. This is causing all login endpoints to return 500 errors.

## Error Details

```
Database Status:
{
  "configured": true,
  "canConnect": false,
  "error": "Failed to connect to /cloudsql/bola8pos:northamerica-south1:pos-app-1/.s.PGSQL.5432"
}
```

## Root Cause

The Cloud SQL Unix socket connection is failing. Possible causes:
1. Cloud SQL instance not properly attached to Cloud Run service
2. Service account lacks Cloud SQL Client permissions
3. Connection string format issue
4. Cloud SQL instance not accessible from the region

## Fixes Applied

1. ✅ Added PostgreSQL connection string to deployment script
2. ✅ Updated connection string format to use Unix socket (`/cloudsql/...`)
3. ✅ Added fallback connection string configuration in `Program.cs`
4. ✅ Changed SSL mode to `Disable` for Unix socket connections

## Next Steps

1. **Verify Cloud SQL Instance Attachment**
   - Check Cloud Run service configuration
   - Ensure `--add-cloudsql-instances` is working correctly
   - Verify service account has Cloud SQL Client role

2. **Test Connection String Format**
   - Try alternative connection string formats
   - Check if TCP connection works (with public IP)
   - Verify instance connection name format

3. **Check Service Account Permissions**
   - Ensure Cloud Run service account has `roles/cloudsql.client`
   - Verify IAM bindings are correct

4. **Alternative: Use TCP Connection**
   - If Unix socket fails, try TCP connection with public IP
   - Requires Cloud SQL instance to have public IP enabled
   - Use SSL connection with certificate

## Current Configuration

- **Connection String Format**: `Host=/cloudsql/bola8pos:northamerica-south1:pos-app-1;Port=5432;Username=posapp;Password=Campus_66;Database=postgres;SSL Mode=Disable`
- **Cloud SQL Instance**: `bola8pos:northamerica-south1:pos-app-1`
- **Region**: `northamerica-south1`

## Impact

- ❌ All login endpoints returning 500 errors
- ❌ RBAC cannot function without database access
- ❌ All v2 endpoints with `[RequiresPermission]` failing

## Resolution Priority

**HIGH** - This blocks all RBAC functionality and user authentication.

