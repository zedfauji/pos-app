# Refund Processing Playbook

## Scenario

A customer requests a refund for a payment that was already processed. This playbook guides you through processing the refund correctly.

## Impact Assessment

**Severity:** 🟡 **MEDIUM** (Business Operation)

**Business Impact:**
- Customer satisfaction
- Financial accuracy
- Audit compliance
- Revenue adjustment

**Time Sensitivity:** 
- Should be processed promptly (within business day)
- Not urgent unless customer is waiting

## Prerequisites

**Required Permissions:**
- `payment:refund` permission
- Access to Payments page
- Manager approval may be required for large refunds

**Required Information:**
- Original payment ID or billing ID
- Refund amount
- Refund reason (optional but recommended)
- Refund method (original payment method or cash)

## Immediate Actions

### Step 1: Verify Refund Request

**What to check:**
1. **Customer Identity:** Verify customer is legitimate
2. **Original Payment:** Locate the original payment
3. **Refund Amount:** Verify refund amount is valid
4. **Refund Reason:** Understand why refund is needed
5. **Policy Compliance:** Check if refund is allowed per policy

**Questions to ask:**
- When was the original payment made?
- What was the payment amount?
- Why is the refund needed?
- What is the requested refund amount?
- Is this a full or partial refund?

### Step 2: Locate Original Payment

**How to find the payment:**

**Via Payments Page:**
1. Open "All Payments" page
2. Search by:
   - Billing ID
   - Payment ID
   - Table number
   - Date range
   - Customer name (if available)

**Via Database (if needed):**
```sql
-- Find payment by billing ID
SELECT * FROM pay.payments 
WHERE billing_id = 'billing-id-here';

-- Find payment by payment ID
SELECT * FROM pay.payments 
WHERE payment_id = 'payment-id-here';
```

**Verify payment details:**
- Payment amount
- Payment method
- Payment date
- Bill status
- Any previous refunds

### Step 3: Check Refund Eligibility

**Validation checks:**

1. **Payment Exists:**
   - ✅ Payment found
   - ❌ Payment not found → Cannot process refund

2. **Refund Amount Valid:**
   - ✅ Refund amount ≤ original payment amount
   - ✅ Refund amount > 0
   - ❌ Invalid amount → Adjust refund amount

3. **No Over-Refunding:**
   - ✅ Total refunded + new refund ≤ original payment
   - ❌ Would exceed original payment → Reduce refund amount

4. **Payment Status:**
   - ✅ Payment was successfully processed
   - ❌ Payment was already refunded → Check refund history
   - ❌ Payment was voided → Cannot refund voided payment

## Detailed Procedures

### Procedure 1: Process Full Refund

**When to use:** Customer wants complete refund of entire payment

**Steps:**

1. **Open Refund Dialog**
   - Navigate to "All Payments" page
   - Find the payment to refund
   - Click "Refund" button

2. **Enter Refund Details**
   - **Refund Amount:** Enter full payment amount (auto-filled)
   - **Refund Method:** Select "Original" (same as payment method)
   - **Refund Reason:** Enter reason (e.g., "Customer cancellation")
   - **Verify:** Check that amount equals original payment

3. **Confirm Refund**
   - Review refund details
   - Click "Process Refund"
   - Wait for confirmation

4. **Verify Refund**
   - Check refund appears in payment history
   - Verify bill status updated to "Refunded"
   - Confirm refund amount is correct

**Expected Result:**
- Refund processed successfully
- Payment status: "Fully Refunded"
- Bill status: "Refunded"
- Refund record created

### Procedure 2: Process Partial Refund

**When to use:** Customer wants refund of part of payment (e.g., one item was wrong)

**Steps:**

1. **Open Refund Dialog**
   - Navigate to "All Payments" page
   - Find the payment to refund
   - Click "Refund" button

2. **Enter Refund Details**
   - **Refund Amount:** Enter partial amount (e.g., $20 of $50 payment)
   - **Refund Method:** Select method (original or cash)
   - **Refund Reason:** Enter reason (e.g., "Item not delivered")
   - **Verify:** Check that amount < original payment

3. **Confirm Refund**
   - Review refund details
   - Click "Process Refund"
   - Wait for confirmation

4. **Verify Refund**
   - Check refund appears in payment history
   - Verify bill status updated to "Partial Refunded"
   - Confirm remaining balance is correct

**Expected Result:**
- Partial refund processed successfully
- Payment status: "Partially Refunded"
- Bill status: "Partial Refunded"
- Remaining balance: Original - Refund

### Procedure 3: Process Multiple Refunds

**When to use:** Customer wants multiple refunds from same payment (e.g., refund $10, then refund $5 later)

**Steps:**

1. **Process First Refund**
   - Follow Procedure 1 or 2
   - Record refund amount

2. **Check Remaining Refundable Amount**
   - System shows: "Remaining: $X.XX"
   - Verify: Remaining = Original - All Previous Refunds

3. **Process Additional Refund**
   - Open refund dialog again
   - Enter new refund amount (must be ≤ remaining)
   - Process refund

4. **Verify All Refunds**
   - Check refund history shows all refunds
   - Verify total refunded ≤ original payment
   - Confirm final status is correct

**Expected Result:**
- Multiple refunds processed successfully
- All refunds recorded
- Total refunded tracked correctly
- Final status reflects total refunds

## Verification

### Post-Refund Checklist

- [ ] Refund processed successfully
- [ ] Refund amount is correct
- [ ] Refund method is correct
- [ ] Refund reason is recorded
- [ ] Payment status updated correctly
- [ ] Bill status updated correctly
- [ ] Refund appears in payment history
- [ ] Customer notified (if applicable)
- [ ] Receipt generated (if needed)

### Success Criteria

✅ **Refund Processed Successfully**
- Refund recorded in system
- Payment status updated
- Bill status updated
- Audit trail created

⚠️ **Refund Processed with Warnings**
- Refund processed but some verification failed
- Review and fix issues
- Document warnings

🔴 **Refund Failed**
- Error occurred during processing
- Review error message
- Fix issue and retry
- See [Troubleshooting](#troubleshooting)

## Troubleshooting

### Issue: "Refund Amount Exceeds Original Payment"

**Cause:** Trying to refund more than was paid

**Solution:**
1. Check original payment amount
2. Check previous refunds (if any)
3. Calculate remaining refundable amount
4. Adjust refund amount to be ≤ remaining

**Example:**
- Original payment: $100
- Previous refunds: $20
- Remaining refundable: $80
- New refund must be ≤ $80

### Issue: "Payment Not Found"

**Cause:** Payment ID or billing ID is incorrect

**Solution:**
1. Verify payment ID/billing ID is correct
2. Check if payment exists in database
3. Search by other criteria (date, table, amount)
4. Verify payment wasn't deleted

### Issue: "Payment Already Fully Refunded"

**Cause:** Trying to refund a payment that was already fully refunded

**Solution:**
1. Check refund history
2. Verify total refunded = original payment
3. If customer needs more refund, may need manual adjustment
4. Contact technical support if needed

### Issue: "Refund Processing Failed"

**Cause:** System error during refund processing

**Solution:**
1. Check error message
2. Review PaymentApi logs
3. Verify database connectivity
4. Check PaymentApi health
5. Retry refund
6. If persists, contact technical support

### Issue: "Bill Status Not Updated"

**Cause:** Refund processed but bill status didn't update

**Solution:**
1. Verify refund was processed successfully
2. Check bill status manually
3. May need to sync bill status (TablesApi)
4. Contact technical support if needed

## Post-Refund Actions

### Immediate (Within 1 Hour)

1. **Notify Customer**
   - Inform customer refund is processed
   - Provide refund confirmation
   - Explain refund method and timing

2. **Update Records**
   - Document refund in manual records (if kept)
   - Update any physical records
   - Note refund reason

3. **Generate Receipt**
   - Print refund receipt (if requested)
   - Email refund receipt (if applicable)
   - Keep copy for records

### Short-Term (Within 24 Hours)

1. **Review Refund**
   - Verify refund was correct
   - Check for any issues
   - Document any problems

2. **Update Reports**
   - Verify refund appears in reports
   - Check financial totals are correct
   - Review daily sales report

### Long-Term (Within 1 Week)

1. **Analyze Refund Patterns**
   - Review refund reasons
   - Identify common issues
   - Implement improvements

2. **Update Procedures**
   - Document lessons learned
   - Update refund policy if needed
   - Train staff on improvements

## Prevention

### Best Practices

1. **Verify Before Processing**
   - Always verify payment details
   - Check refund eligibility
   - Confirm refund amount

2. **Document Everything**
   - Record refund reasons
   - Keep refund receipts
   - Maintain audit trail

3. **Follow Policy**
   - Adhere to refund policy
   - Get approvals when needed
   - Document exceptions

4. **Train Staff**
   - Ensure staff know refund procedures
   - Provide clear guidelines
   - Regular training updates

## Related Playbooks

- [Payment Processing Outage Playbook](./payment-outage.md)
- [Data Integrity Playbook](./data-integrity.md)

## Related Runbooks

- [Payment Processing Issues Runbook](../runbooks/payment-processing-issues.md)
- [Health Check Runbook](../runbooks/health-check.md)

## Additional Resources

- [Payment Processing Documentation](../../features/payments.md)
- [Refund Implementation Guide](../../archive/status-documents/REFUND_IMPLEMENTATION_PROGRESS.md)
- [PaymentApi Reference](../../backend/payment-api.md)

---

**Last Updated:** 2025-01-27  
**Maintained By:** Operations Team  
**Review Frequency:** Quarterly
