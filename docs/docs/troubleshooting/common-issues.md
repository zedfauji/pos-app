# Common Issues

This document covers frequently encountered issues and their solutions.

## Frontend Issues

### Application Won't Start

**Symptoms**:
- Application crashes on startup
- Blank screen
- Error dialog appears

**Solutions**:

1. **Check Logs**
   ```
   Location: %LocalAppData%\MagiDesk\crash-debug.log
   ```

2. **Verify .NET 8 Runtime**
   ```powershell
   dotnet --version
   # Should be 8.0.x or higher
   ```

3. **Clear App Data**
   ```
   Delete: %LocalAppData%\MagiDesk\
   ```

4. **Reinstall Application**
   - Uninstall current version
   - Download fresh installer
   - Install again

### API Connection Errors

**Symptoms**:
- "Unable to connect to API" errors
- Timeout errors
- 404 Not Found errors

**Solutions**:

1. **Check API URLs**
   - Verify `appsettings.json` has correct API URLs
   - Check if APIs are running

2. **Check Network**
   ```powershell
   # Test API connectivity
   Invoke-WebRequest -Uri "https://magidesk-users-904541739138.northamerica-south1.run.app/health"
   ```

3. **Check Firewall**
   - Ensure firewall allows outbound HTTPS
   - Check proxy settings

4. **Verify User ID Header**
   - Ensure `X-User-Id` header is being sent
   - Check `UserIdHeaderHandler` is configured

### Permission Errors

**Symptoms**:
- "403 Forbidden" errors
- UI elements missing
- "Access Denied" messages

**Solutions**:

1. **Check User Permissions**
   - Verify user has role assigned
   - Check role has required permissions
   - Re-login to refresh permissions

2. **Check API Version**
   - Ensure using v2 endpoints (RBAC-enabled)
   - Verify `X-User-Id` header is included

3. **Clear Permission Cache**
   - Logout and login again
   - Or restart application

### XAML Compilation Errors

**Symptoms**:
- Build errors related to XAML
- "Unknown member" errors
- Style not found errors

**Solutions**:

1. **Check WinUI 3 Compatibility**
   - Ensure using WinUI 3 APIs (not UWP/WPF)
   - Verify namespace is `Microsoft.UI.Xaml`

2. **Clean and Rebuild**
   ```powershell
   dotnet clean
   dotnet build
   ```

3. **Check Visual Studio**
   - Use Visual Studio 2022 for XAML debugging
   - Verify WinUI 3 workload is installed

## Backend Issues

### Database Connection Errors

**Symptoms**:
- "Unable to connect to database" errors
- Timeout errors
- Connection pool exhaustion

**Solutions**:

1. **Check Connection String**
   ```json
   {
     "Postgres": {
       "CloudRunSocketConnectionString": "Host=/cloudsql/..."
     }
   }
   ```

2. **Verify Cloud SQL Proxy**
   - Ensure Cloud SQL Proxy is running (Cloud Run handles this)
   - Check instance connection name is correct

3. **Check Database Status**
   ```sql
   SELECT version();
   SELECT current_database();
   ```

4. **Connection Pool Settings**
   - Increase pool size if needed
   - Check for connection leaks

### Permission Check Failures

**Symptoms**:
- All requests return 403 Forbidden
- Permission checks always fail
- "User ID not found" errors

**Solutions**:

1. **Check UserIdExtractionMiddleware**
   - Verify middleware is registered
   - Check `X-User-Id` header is present

2. **Check RbacService**
   - Verify `IRbacService` is registered
   - Check `UsersApi:BaseUrl` is configured

3. **Check Database**
   ```sql
   -- Verify user has role
   SELECT u.username, u.role 
   FROM users.users u 
   WHERE u.user_id = 'USER_ID';
   
   -- Verify role has permissions
   SELECT r.name, rp.permission
   FROM users.roles r
   JOIN users.role_permissions rp ON r.role_id = rp.role_id
   WHERE r.name = 'ROLE_NAME';
   ```

### API Deployment Failures

**Symptoms**:
- Deployment script fails
- Service won't start
- Health check fails

**Solutions**:

1. **Check Docker Build**
   ```powershell
   docker build -t test-image .
   docker run test-image
   ```

2. **Check Environment Variables**
   - Verify all required env vars are set
   - Check for typos in variable names

3. **Check Cloud Run Logs**
   ```powershell
   gcloud run services logs read magidesk-menu --region northamerica-south1 --limit 50
   ```

4. **Check Health Endpoint**
   ```powershell
   Invoke-WebRequest -Uri "https://magidesk-menu-.../health"
   ```

## Database Issues

### Schema Migration Failures

**Symptoms**:
- Migration scripts fail
- Tables not created
- Constraint errors

**Solutions**:

1. **Check Migration Scripts**
   - Verify SQL syntax is correct
   - Check for idempotent migrations

2. **Check Permissions**
   ```sql
   -- Verify user has CREATE privileges
   SELECT has_schema_privilege('posapp', 'users', 'CREATE');
   ```

3. **Manual Migration**
   - Run migration scripts manually
   - Check for errors in output

### Data Consistency Issues

**Symptoms**:
- Duplicate records
- Missing relationships
- Orphaned records

**Solutions**:

1. **Check Foreign Keys**
   ```sql
   -- Find orphaned records
   SELECT * FROM table1 t1
   LEFT JOIN table2 t2 ON t1.fk_id = t2.id
   WHERE t2.id IS NULL;
   ```

2. **Check Constraints**
   ```sql
   -- Verify constraints are enforced
   SELECT conname, contype 
   FROM pg_constraint 
   WHERE conrelid = 'table_name'::regclass;
   ```

3. **Run Consistency Checks**
   - Use diagnostic queries
   - Fix inconsistencies manually

## Performance Issues

### Slow API Responses

**Symptoms**:
- API requests take long time
- Timeout errors
- High latency

**Solutions**:

1. **Check Database Queries**
   ```sql
   -- Find slow queries
   SELECT query, mean_exec_time
   FROM pg_stat_statements
   ORDER BY mean_exec_time DESC
   LIMIT 10;
   ```

2. **Check Indexes**
   ```sql
   -- Verify indexes exist
   SELECT indexname, tablename
   FROM pg_indexes
   WHERE schemaname = 'users';
   ```

3. **Check Connection Pool**
   - Monitor connection pool usage
   - Increase pool size if needed

### Frontend Performance

**Symptoms**:
- UI is slow to respond
- High memory usage
- Application freezes

**Solutions**:

1. **Check Memory Usage**
   - Use Task Manager to monitor
   - Check for memory leaks

2. **Optimize Data Binding**
   - Use `x:Bind` instead of `Binding` where possible
   - Reduce number of bindings

3. **Check Async Operations**
   - Ensure all I/O is async
   - Don't block UI thread

## Authentication Issues

### Login Failures

**Symptoms**:
- "Invalid credentials" errors
- Login button doesn't work
- Session not created

**Solutions**:

1. **Check Credentials**
   - Verify username and password
   - Check for typos

2. **Check API Endpoint**
   ```powershell
   # Test login endpoint
   $body = @{
       username = "admin"
       password = "123456"
   } | ConvertTo-Json
   
   Invoke-RestMethod -Uri "https://magidesk-users-.../api/v2/auth/login" `
       -Method POST `
       -Body $body `
       -ContentType "application/json"
   ```

3. **Check Database**
   ```sql
   -- Verify user exists
   SELECT username, is_active, is_deleted
   FROM users.users
   WHERE username = 'admin';
   ```

### Session Expiration

**Symptoms**:
- "Session expired" errors
- Forced to re-login frequently
- Permissions lost

**Solutions**:

1. **Check Session Storage**
   - Verify session file exists
   - Check file permissions

2. **Extend Session Timeout**
   - Update session timeout settings
   - Or implement refresh token

## Common Error Messages

### "No authenticationScheme was specified"

**Cause**: Authentication middleware not configured

**Solution**:
```csharp
builder.Services.AddAuthentication("Bearer")
    .AddScheme<AuthenticationSchemeOptions, NoOpAuthenticationHandler>(
        "Bearer", options => { });
```

### "User ID not found in request"

**Cause**: `X-User-Id` header missing

**Solution**:
- Ensure frontend includes `X-User-Id` header
- Check `UserIdHeaderHandler` is configured

### "Permission check timed out"

**Cause**: RBAC service unavailable or slow

**Solution**:
- Check `UsersApi` is running
- Verify `UsersApi:BaseUrl` is correct
- Check network connectivity

### "Catastrophic failure" (COM Exception)

**Cause**: XAML binding or control initialization issue

**Solution**:
- Check XAML for binding errors
- Verify controls are properly initialized
- Use try-catch for COM exceptions

---

**Last Updated**: 2025-01-02

