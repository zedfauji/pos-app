# Stopping a Table Session

This guide explains how to stop a table session when customers are ready to pay.

---

## When to Stop a Session

**Stop a session when:**
- Customers are ready to pay
- Customers are leaving
- You need to free the table
- Session needs to be closed

**Why it's important:**
- Stops the timer (calculates final time cost)
- Generates the bill
- Allows you to process payment
- Frees the table for next customers

---

## Step-by-Step Instructions

### Step 1: Find the Occupied Table

**Look for:**
- Red table (occupied)
- Table with active session
- Table customers are at

**On the dashboard, you'll see:**
- Red tables show as occupied
- Server name is displayed
- Duration is shown (e.g., "45 minutes")

### Step 2: Click on the Table

**Click on the red table** you want to stop

**What you'll see:**
- Table details panel
- Current order information
- Session duration
- Current total
- Action buttons

### Step 3: Stop the Session

**Click "Stop Session" or "Bill" button**

**The system will:**
- Calculate time cost (minutes × rate per minute)
- Calculate items cost (sum of all items)
- Generate total bill
- Show bill summary

### Step 4: Review the Bill

**You'll see a bill summary:**

**Time Cost:**
- Duration: X minutes
- Rate: $Y per minute
- Time Cost: $Z

**Items Cost:**
- List of all items ordered
- Quantity and price for each
- Subtotal of items

**Total:**
- Time Cost + Items Cost
- **Total Amount Due**

**Important:** Always review the bill before processing payment!

### Step 5: Verify Bill is Correct

**Check:**
- ✅ All items are correct
- ✅ Quantities are correct
- ✅ Prices are correct
- ✅ Time calculation is accurate
- ✅ Total amount is correct

**If something is wrong:**
- Don't process payment yet
- Fix the order first
- Then process payment

### Step 6: Process Payment

**Click "Process Payment" button**

**This takes you to the payment screen** where you can:
- Select payment method
- Enter payment amount
- Add tip
- Apply discount
- Process the payment

**After payment is processed:**
- ✅ Table becomes available (green)
- ✅ Session is closed
- ✅ Receipt is generated
- ✅ Payment is recorded

---

## What Happens When You Stop a Session

### Automatic Calculations

**The system calculates:**
1. **Time Cost:**
   - Duration = End time - Start time
   - Minutes = Rounded up (e.g., 45.5 minutes = 46 minutes)
   - Time Cost = Minutes × Rate per minute

2. **Items Cost:**
   - Sum of all item prices
   - Includes quantities
   - Includes modifiers (if any)

3. **Total:**
   - Time Cost + Items Cost
   - This is the bill total

### Bill Generation

**A bill is created with:**
- Bill ID (unique number)
- Table number
- Server name
- Start and end times
- Duration
- Items list
- Time cost
- Items cost
- Total amount
- Status: "Awaiting Payment"

### Table Status Update

**The table:**
- Changes from red (occupied) to green (available)
- Can be used for new customers
- Session is closed and saved

---

## Important Notes

### ⚠️ Before Stopping

- **Verify customers are ready to pay**
- **Check all items are delivered**
- **Review the order is complete**
- **Confirm customers are satisfied**

### ✅ Best Practices

- Stop session when customers are ready
- Review bill carefully before processing payment
- Verify all items are correct
- Check time calculation is reasonable
- Process payment promptly after stopping

### ❌ Common Mistakes

- Stopping session too early (before customers are ready)
- Not reviewing the bill
- Processing payment with errors
- Forgetting to stop session when customers leave

---

## Troubleshooting

### Problem: "Can't Stop Session"

**Possible causes:**
- System error
- Network issue
- Session is locked

**Solutions:**
1. Try again (may be temporary)
2. Refresh the page
3. Log out and log back in
4. Contact manager if issue persists

### Problem: "Bill Amount Seems Wrong"

**What to check:**
- Time calculation (verify duration)
- Items list (verify all items)
- Prices (verify prices are correct)
- Quantities (verify quantities)

**If wrong:**
- Don't process payment
- Fix the order first
- Then stop session again

### Problem: "Table Won't Free Up"

**What to do:**
- Verify payment was processed
- Check if session was actually stopped
- Refresh the page
- Contact manager if needed

---

## Special Situations

### Customers Want to Add More Items

**If customers want to add items after you've stopped:**
- You can still add items before processing payment
- Or restart session (but this creates a new bill)
- Best to add items before stopping

### Customers Dispute the Bill

**If customers question the bill:**
1. Review bill with customer
2. Explain time cost and items
3. Check for errors
4. Fix if needed
5. Process payment with corrected amount

### Customers Leave Without Paying

**If customers leave:**
- Stop the session anyway
- Note the situation
- Process payment later if possible
- Report to manager

---

## Quick Reference

```
1. Find occupied table (red)
2. Click on table
3. Click "Stop Session"
4. Review the bill
5. Verify bill is correct
6. Click "Process Payment"
```

**After payment, table becomes available!**

---

## Practice Exercise

**Try this:**
1. Start a test session
2. Add some items
3. Wait a few minutes
4. Stop the session
5. Review the bill
6. See how time and items are calculated

**Practice until you're comfortable!**

---

## Related Guides

- [Starting a Session](./starting-session.md)
- [Processing Payment](../payments/processing-payment.md)
- [Managing Tables](./managing-tables.md)

---

**Remember:** Stopping a session generates the bill. Always review it carefully before processing payment!

---

**Last Updated:** 2025-01-27  
**For:** Servers
