# Service Restart Runbook

## Purpose

This runbook guides you through restarting a MagiDesk POS backend service when it's not responding or needs to be restarted.

## Prerequisites

- Access to Google Cloud Console
- Permissions to manage Cloud Run services
- Basic knowledge of service names

## Estimated Time

5-15 minutes (depending on service startup time)

## When to Use This Runbook

**Use this runbook when:**
- Service is not responding to health checks
- Service is returning errors
- Service needs to be restarted after configuration changes
- Service is experiencing performance issues

**Do NOT use this runbook when:**
- Multiple services are down (use [System Outage Playbook](../playbooks/system-outage.md))
- Database is down (fix database first)
- Network issues (fix network first)

## Steps

### Step 1: Identify the Service

**What service needs restarting?**

**Available Services:**
- **UsersApi** - User management and authentication
- **OrderApi** - Order processing
- **PaymentApi** - Payment processing
- **MenuApi** - Menu management
- **InventoryApi** - Inventory management
- **TablesApi** - Table management
- **SettingsApi** - Settings management
- **CustomerApi** - Customer management
- **DiscountApi** - Discount management

**How to identify:**
- Check error messages
- Review health check results
- Check service logs

### Step 2: Verify Service Status

**Check current service status:**

**Via Google Cloud Console:**
1. Log into Google Cloud Console
2. Navigate to Cloud Run
3. Find the service (e.g., `magidesk-order`)
4. Check service status

**Via Command Line:**
```powershell
# Check service status
gcloud run services describe magidesk-order \
  --region northamerica-south1 \
  --project bola8pos \
  --format="value(status.conditions)"
```

**Expected Status:**
- **Ready:** Service is running
- **Not Ready:** Service needs restart
- **Unknown:** Service status unclear

### Step 3: Review Service Logs (Optional but Recommended)

**Check recent errors before restarting:**

**Via Google Cloud Console:**
1. Navigate to Cloud Run
2. Click on the service
3. Go to "Logs" tab
4. Review recent errors

**Via Command Line:**
```powershell
# View recent logs
gcloud logging read "resource.type=cloud_run_revision AND resource.labels.service_name=magidesk-order" \
  --limit 50 \
  --format json
```

**Look for:**
- Error messages
- Crash reports
- Out-of-memory errors
- Database connection errors

### Step 4: Restart the Service

**Method 1: Via Google Cloud Console (Recommended)**

1. Log into Google Cloud Console
2. Navigate to **Cloud Run**
3. Find the service (e.g., `magidesk-order`)
4. Click on the service name
5. Click **"EDIT & DEPLOY NEW REVISION"**
6. Click **"DEPLOY"** (no changes needed, this restarts the service)
7. Wait for deployment to complete (2-5 minutes)

**Method 2: Via Command Line**

```powershell
# Restart service by updating (triggers new deployment)
gcloud run services update magidesk-order \
  --region northamerica-south1 \
  --project bola8pos \
  --no-traffic
```

**Wait for service to be ready:**
- Service will show "Deploying..." status
- Usually takes 2-5 minutes
- Monitor in Cloud Console

### Step 5: Verify Service is Running

**Check health endpoint:**

```powershell
# Test health endpoint
$serviceUrl = "https://magidesk-order-904541739138.northamerica-south1.run.app/health"
$response = Invoke-WebRequest -Uri $serviceUrl -UseBasicParsing
Write-Host "Status: $($response.StatusCode)"
```

**Expected Result:**
- Status Code: 200 OK
- Response time: < 500ms

**If still not working:**
- Wait 2 more minutes (service may still be starting)
- Check logs for errors
- See [Troubleshooting](#troubleshooting) section

### Step 6: Test Service Functionality

**Test a basic operation:**

**For OrderApi:**
```powershell
# Test order creation endpoint
Invoke-WebRequest -Uri "https://magidesk-order-904541739138.northamerica-south1.run.app/api/orders" -Method GET
```

**For PaymentApi:**
```powershell
# Test payment endpoint
Invoke-WebRequest -Uri "https://magidesk-payment-904541739138.northamerica-south1.run.app/health"
```

**Expected Result:**
- Endpoint responds (may return 404 for invalid requests, but should not timeout)
- Response time is reasonable

## Verification

### Complete Restart Checklist

- [ ] Service status shows "Ready" in Cloud Console
- [ ] Health endpoint returns 200 OK
- [ ] Response time is < 500ms
- [ ] No errors in recent logs
- [ ] Basic functionality test passes

### Success Criteria

✅ **Service Restarted Successfully** - Service is running and responding

⚠️ **Service Restarted but Issues Remain** - Review logs, may need further troubleshooting

🔴 **Service Failed to Restart** - Check configuration, may need to contact support

## Troubleshooting

### Issue: Service Won't Start

**Possible Causes:**
- Configuration error
- Database connection issue
- Resource limits exceeded
- Code error

**Solutions:**
1. Check service logs for errors
2. Verify configuration is correct
3. Check database connectivity
4. Review resource limits (CPU, memory)
5. Check for code deployment errors

### Issue: Service Starts but Returns Errors

**Possible Causes:**
- Database connection issue
- Missing environment variables
- Invalid configuration
- Dependency service down

**Solutions:**
1. Check service logs
2. Verify environment variables
3. Check database connectivity
4. Verify dependent services are running
5. Review configuration

### Issue: Service Takes Too Long to Start

**Possible Causes:**
- Cold start (first request after inactivity)
- Resource constraints
- Database connection slow
- Large application size

**Solutions:**
1. Wait 5-10 minutes (cold starts can be slow)
2. Check resource allocation
3. Verify database connectivity
4. Consider increasing resources if consistently slow

### Issue: Service Restarts Repeatedly

**Possible Causes:**
- Application crash
- Out of memory
- Configuration error
- Database connection failure

**Solutions:**
1. Check logs for crash reports
2. Review memory usage
3. Verify configuration
4. Check database connectivity
5. May need code fix or configuration change

## Related Runbooks

- [Health Check Runbook](./health-check.md)
- [Database Connection Issues Runbook](./database-connection-issues.md)
- [Performance Issues Runbook](./performance-issues.md)

## Related Playbooks

- [System Outage Playbook](../playbooks/system-outage.md)
- [API Not Responding Playbook](../playbooks/api-not-responding.md)

## Additional Resources

- [Google Cloud Run Documentation](https://cloud.google.com/run/docs)
- [Service Management Guide](../service-management.md)
- [Troubleshooting Guide](../troubleshooting-guide.md)

---

**Last Updated:** 2025-01-27  
**Maintained By:** Operations Team
