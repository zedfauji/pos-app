# Payment Processing Issues Runbook

## Purpose

This runbook helps you diagnose and fix issues with payment processing in MagiDesk POS.

## Prerequisites

- Access to Payments page
- Basic knowledge of payment flow
- Access to logs (optional but helpful)

## Estimated Time

10-30 minutes (depending on issue complexity)

## When to Use This Runbook

**Use this runbook when:**
- Payments are failing
- Payment button doesn't work
- Payment shows as "Failed"
- Payment not recorded in system
- Payment processing is slow

## Steps

### Step 1: Identify the Issue

**What exactly is failing?**

**Common Issues:**
1. **Payment Button Not Working**
   - Button doesn't respond when clicked
   - No error message shown

2. **Payment Shows as "Failed"**
   - Payment attempted but failed
   - Error message displayed

3. **Payment Not Recorded**
   - Payment seems to process but doesn't appear in system
   - Money taken but not recorded

4. **Payment Processing Slow**
   - Payment takes too long to process
   - System appears frozen

5. **Split Payment Issues**
   - Split payment not working
   - Amounts don't add up correctly

### Step 2: Check PaymentApi Health

**Verify PaymentApi is running:**

```powershell
# Check PaymentApi health
$response = Invoke-WebRequest -Uri "https://magidesk-payment-904541739138.northamerica-south1.run.app/health" -UseBasicParsing
Write-Host "Status: $($response.StatusCode)"
```

**Expected Result:**
- Status Code: 200 OK
- Response time: < 500ms

**If PaymentApi is down:**
- See [Service Restart Runbook](./service-restart.md)
- Check Google Cloud Console for service status

### Step 3: Check Database Connectivity

**Verify database is accessible:**

```powershell
# Test database connection
.\test-db-connection.ps1
```

**Expected Result:**
- Database connection: Success
- Response time: < 100ms

**If database is down:**
- See [Database Connection Issues Runbook](./database-connection-issues.md)
- Check Cloud SQL instance status

### Step 4: Review Payment Logs

**Check for errors in logs:**

**Via Google Cloud Console:**
1. Navigate to Cloud Run
2. Select `magidesk-payment` service
3. Go to "Logs" tab
4. Filter for errors
5. Review recent error messages

**Look for:**
- Database connection errors
- Validation errors
- Payment amount errors
- Billing ID errors

### Step 5: Verify Payment Details

**Check payment information:**

**Common Issues:**
1. **Invalid Billing ID**
   - Billing ID doesn't exist
   - Billing ID format is wrong

2. **Invalid Payment Amount**
   - Amount is 0 or negative
   - Amount exceeds bill total
   - Amount format is wrong

3. **Invalid Payment Method**
   - Payment method not supported
   - Payment method is null/empty

4. **Bill Already Paid**
   - Trying to pay already paid bill
   - Bill status is "paid"

**How to verify:**
1. Check billing ID exists in database
2. Verify payment amount is valid
3. Confirm payment method is supported
4. Check bill status

### Step 6: Test Payment Endpoint

**Test payment API directly:**

```powershell
# Test payment endpoint (example - adjust values)
$body = @{
    sessionId = "session-id-here"
    billingId = "billing-id-here"
    totalDue = 100.00
    lines = @(
        @{
            amountPaid = 100.00
            paymentMethod = "cash"
            tipAmount = 0
        }
    )
    serverId = "user-id-here"
} | ConvertTo-Json

$response = Invoke-WebRequest -Uri "https://magidesk-payment-904541739138.northamerica-south1.run.app/api/payments" `
    -Method POST `
    -Body $body `
    -ContentType "application/json" `
    -UseBasicParsing

Write-Host "Status: $($response.StatusCode)"
Write-Host "Response: $($response.Content)"
```

**Expected Result:**
- Status Code: 201 Created
- Response contains bill ledger information

**If test fails:**
- Review error message
- Check request body is correct
- Verify all required fields are present

## Common Issues & Solutions

### Issue: "Payment Amount Exceeds Bill Total"

**Cause:** Trying to pay more than the bill total

**Solution:**
1. Check bill total
2. Verify payment amount
3. Adjust payment amount to be ≤ bill total
4. For split payments, ensure total of all splits = bill total

### Issue: "Billing ID Not Found"

**Cause:** Billing ID doesn't exist or is incorrect

**Solution:**
1. Verify billing ID is correct
2. Check if bill exists in database
3. Verify bill wasn't deleted
4. Check bill status

### Issue: "Payment Already Processed"

**Cause:** Trying to pay a bill that's already paid

**Solution:**
1. Check bill status
2. Verify payment was already processed
3. If refund needed, process refund instead
4. If duplicate payment, contact support

### Issue: "Database Error"

**Cause:** Database connection or query error

**Solution:**
1. Check database connectivity
2. Verify database is running
3. Check database logs
4. See [Database Connection Issues Runbook](./database-connection-issues.md)

### Issue: "Payment Processing Timeout"

**Cause:** Payment takes too long to process

**Solution:**
1. Check PaymentApi response time
2. Verify database performance
3. Check network connectivity
4. Retry payment
5. If persists, see [Performance Issues Runbook](./performance-issues.md)

### Issue: "Split Payment Total Doesn't Match"

**Cause:** Split payment amounts don't add up to bill total

**Solution:**
1. Verify all split amounts
2. Ensure total of splits = bill total
3. Use "Auto-Fill Remaining" if available
4. Adjust amounts to match total

## Verification

### Payment Success Checklist

- [ ] Payment processed successfully
- [ ] Payment appears in payment history
- [ ] Bill status updated to "Paid"
- [ ] Bill ledger updated correctly
- [ ] Receipt generated (if applicable)
- [ ] No errors in logs

### Success Criteria

✅ **Payment Processed Successfully**
- Payment recorded in system
- Bill status updated
- Receipt generated
- No errors

⚠️ **Payment Processed with Warnings**
- Payment processed but some issues
- Review warnings
- Fix if needed

🔴 **Payment Failed**
- Error occurred
- Review error message
- Fix issue and retry

## Prevention

### Best Practices

1. **Validate Before Processing**
   - Verify payment amount
   - Check billing ID
   - Confirm payment method

2. **Monitor Payment Success Rate**
   - Track payment failures
   - Identify patterns
   - Fix recurring issues

3. **Keep Systems Updated**
   - Update PaymentApi regularly
   - Monitor for known issues
   - Apply patches promptly

4. **Test Regularly**
   - Test payment flow weekly
   - Verify after deployments
   - Test edge cases

## Related Runbooks

- [Health Check Runbook](./health-check.md)
- [Service Restart Runbook](./service-restart.md)
- [Database Connection Issues Runbook](./database-connection-issues.md)
- [Performance Issues Runbook](./performance-issues.md)

## Related Playbooks

- [Payment Processing Outage Playbook](../playbooks/payment-outage.md)
- [Refund Processing Playbook](../playbooks/refund-processing.md)

## Additional Resources

- [Payment Processing Documentation](../../features/payments.md)
- [PaymentApi Reference](../../backend/payment-api.md)
- [Split Payments Guide](../../features/split-payments.md)

---

**Last Updated:** 2025-01-27  
**Maintained By:** Operations Team
