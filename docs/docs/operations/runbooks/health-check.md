# Health Check Runbook

## Purpose

This runbook guides you through checking the health of all MagiDesk POS system components to ensure everything is working correctly.

## Prerequisites

- Access to PowerShell or command line
- Internet connectivity
- Basic knowledge of system components

## Estimated Time

5-10 minutes

## Steps

### Step 1: Check All API Health Endpoints

**What this does:** Verifies that all backend services are running and responding.

**Command:**
```powershell
.\test-health-endpoints.ps1
```

**Expected Result:**
```
✅ UsersApi: Healthy (200 OK)
✅ OrderApi: Healthy (200 OK)
✅ PaymentApi: Healthy (200 OK)
✅ MenuApi: Healthy (200 OK)
✅ InventoryApi: Healthy (200 OK)
✅ TablesApi: Healthy (200 OK)
✅ SettingsApi: Healthy (200 OK)
```

**If you see errors:**
- Note which API is failing
- Proceed to [API Not Responding Runbook](./api-not-responding.md)

### Step 2: Check Database Connectivity

**What this does:** Verifies that the database is accessible and responding.

**Command:**
```powershell
.\test-db-connection.ps1
```

**Expected Result:**
```
✅ Database Connection: Success
✅ Database Version: PostgreSQL 17.x
✅ Connection Time: 45ms
```

**If you see errors:**
- Note the error message
- Proceed to [Database Connection Issues Runbook](./database-connection-issues.md)

### Step 3: Check Payment Flow

**What this does:** Tests the complete payment processing flow end-to-end.

**Command:**
```powershell
.\test-payment-flow.ps1
```

**Expected Result:**
```
✅ Payment Flow Test: Success
✅ Order Creation: Success
✅ Payment Processing: Success
✅ Bill Generation: Success
```

**If you see errors:**
- Note which step failed
- Proceed to [Payment Processing Issues Runbook](./payment-processing-issues.md)

### Step 4: Check Frontend Application

**What this does:** Verifies the desktop application is working.

**Manual Check:**
1. Open MagiDesk application
2. Try to log in
3. Navigate to different pages
4. Check for error messages

**Expected Result:**
- Application opens without errors
- Login works
- Pages load correctly
- No error dialogs

**If you see errors:**
- Note the error message
- Check application logs: `logs/frontend.log`
- Review [Troubleshooting Guide](../troubleshooting-guide.md)

### Step 5: Check System Performance

**What this does:** Verifies system is performing within acceptable limits.

**Command:**
```powershell
# Check API response times
$apis = @(
    "https://magidesk-order-904541739138.northamerica-south1.run.app/health",
    "https://magidesk-payment-904541739138.northamerica-south1.run.app/health"
)

foreach ($api in $apis) {
    $response = Measure-Command { Invoke-WebRequest -Uri $api -UseBasicParsing }
    Write-Host "$api : $($response.TotalMilliseconds)ms"
}
```

**Expected Result:**
- All APIs respond in < 500ms
- No timeouts

**If response times are high:**
- Note which API is slow
- Proceed to [Performance Issues Runbook](./performance-issues.md)

## Verification

### Complete Health Check Checklist

- [ ] All APIs return 200 OK
- [ ] Database connection successful
- [ ] Payment flow test passes
- [ ] Frontend application works
- [ ] Response times < 500ms
- [ ] No error messages in logs

### Success Criteria

✅ **All systems healthy** - All checks pass, proceed with normal operations

⚠️ **Some issues found** - Review failed checks, follow appropriate runbook

🔴 **Critical issues** - System may be down, follow emergency procedures

## Troubleshooting

### Issue: API Returns 404 Not Found

**Possible Causes:**
- Service not deployed
- Wrong URL
- Service crashed

**Solution:**
1. Check Google Cloud Console for service status
2. Verify service URL is correct
3. Check service logs
4. See [API Not Responding Runbook](./api-not-responding.md)

### Issue: Database Connection Timeout

**Possible Causes:**
- Network issues
- Database server down
- Firewall blocking connection
- Wrong credentials

**Solution:**
1. Check network connectivity
2. Verify database is running in Cloud SQL
3. Check firewall rules
4. Verify connection string
5. See [Database Connection Issues Runbook](./database-connection-issues.md)

### Issue: Payment Flow Test Fails

**Possible Causes:**
- PaymentApi not responding
- OrderApi not responding
- Database issues
- Invalid test data

**Solution:**
1. Check PaymentApi health
2. Check OrderApi health
3. Verify database connectivity
4. Review test logs
5. See [Payment Processing Issues Runbook](./payment-processing-issues.md)

## Related Runbooks

- [API Not Responding Runbook](./api-not-responding.md)
- [Database Connection Issues Runbook](./database-connection-issues.md)
- [Payment Processing Issues Runbook](./payment-processing-issues.md)
- [Performance Issues Runbook](./performance-issues.md)

## Additional Resources

- [Operational Handbook](../operational-handbook.md)
- [Monitoring Guide](../monitoring.md)
- [Troubleshooting Guide](../troubleshooting-guide.md)

---

**Last Updated:** 2025-01-27  
**Maintained By:** Operations Team
