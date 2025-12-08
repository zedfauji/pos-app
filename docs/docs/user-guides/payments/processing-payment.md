# Processing a Payment

This guide explains how to process a payment after customers are ready to pay.

---

## When to Process Payment

**Process payment when:**
- Customers are ready to pay
- You've stopped the table session
- Bill has been generated
- Customer has chosen payment method

**The payment process:**
1. Stop table session (generates bill)
2. Review bill with customer
3. Process payment
4. Print receipt

---

## Step-by-Step Instructions

### Step 1: Stop the Table Session

**First, stop the session to generate the bill:**

1. Click on the occupied table
2. Click "Stop Session"
3. System calculates:
   - Time cost
   - Items cost
   - Total bill

**See:** [Stopping a Session](../table-management/stopping-session.md) for details

### Step 2: Review the Bill

**The bill summary shows:**

**Time Cost:**
- Duration: X minutes
- Rate: $Y per minute
- **Time Cost: $Z**

**Items:**
- List of all items ordered
- Quantity and price for each
- Subtotal

**Total:**
- Time Cost + Items Cost
- **Total Amount: $XX.XX**

**Important:** Always review the bill before processing payment!

### Step 3: Present Bill to Customer

**Show customer the bill:**
- Explain time cost (if applicable)
- Show items ordered
- Point out total amount
- Answer any questions

**If customer has questions:**
- Explain charges clearly
- Show itemized breakdown
- Verify items are correct
- Make corrections if needed

### Step 4: Open Payment Screen

**Click "Process Payment" button**

**Payment screen appears with:**
- Bill total displayed prominently
- Payment method options
- Amount input field
- Tip input (optional)
- Discount option (if approved)
- Action buttons

### Step 5: Select Payment Method

**Choose how customer wants to pay:**

**Cash:**
- Select "Cash"
- Enter amount received
- System calculates change (if applicable)

**Card:**
- Select "Card"
- Enter amount (usually equals bill total)
- Process card payment (if integrated)

**Mobile Payment:**
- Select "Mobile Payment"
- Enter amount
- Process mobile payment (if integrated)

**Digital Wallet:**
- Select "Digital Wallet"
- Enter amount
- Process wallet payment

**Split Payment:**
- Select "Split Payment"
- Enter amounts for each method
- See [Split Payments](./split-payments.md) for details

### Step 6: Enter Payment Amount

**Enter the amount customer is paying:**

**For full payment:**
- Amount = Bill total
- System may auto-fill this

**For partial payment:**
- Enter amount less than total
- Bill will show as "Partially Paid"
- Process remaining amount later

**For overpayment (cash):**
- Enter amount received
- System calculates change
- Give change to customer

### Step 7: Add Tip (Optional)

**If customer wants to add tip:**

**Option 1: Dollar Amount**
- Enter tip amount (e.g., $5.00)

**Option 2: Percentage**
- Enter percentage (e.g., 15%)
- System calculates tip amount

**Tip is added to total:**
- Bill total: $50.00
- Tip: $5.00
- **Total to pay: $55.00**

### Step 8: Apply Discount (If Applicable)

**If discount is approved:**

1. Click "Apply Discount"
2. Enter discount:
   - **Amount:** $X.XX
   - **Percentage:** X%
3. Enter reason (optional)
4. Click "Apply"

**Updated total shows:**
- Original total
- Discount amount
- **New total**

**Note:** Manager approval may be required for discounts.

### Step 9: Review Payment Summary

**Before processing, review:**

- ✅ Payment method is correct
- ✅ Payment amount is correct
- ✅ Tip is correct (if added)
- ✅ Discount is correct (if applied)
- ✅ Total to pay is correct

**Payment summary shows:**
- Subtotal
- Discount (if any)
- Tip (if any)
- **Total to Pay**

### Step 10: Process Payment

**Click "Process Payment" button**

**What happens:**
- ✅ Payment is processed
- ✅ Payment is recorded in system
- ✅ Bill status changes to "Paid"
- ✅ Receipt is generated
- ✅ Receipt prints automatically
- ✅ Table becomes available (green)

**You'll see:**
- Success message
- Receipt preview
- Option to print receipt again
- Option to email receipt (if enabled)

---

## Payment Confirmation

### What Gets Recorded

**Payment record includes:**
- Payment ID (unique number)
- Bill ID
- Table number
- Server name (you)
- Payment amount
- Payment method
- Tip amount (if any)
- Discount amount (if any)
- Date and time
- Status: "Paid"

### Receipt Contents

**Receipt includes:**
- Business name and address
- Receipt number
- Date and time
- Table number
- Server name
- Items ordered with prices
- Time cost
- Items cost
- Subtotal
- Discount (if any)
- Tip (if any)
- **Total paid**
- Payment method
- Thank you message

---

## Important Notes

### ⚠️ Before Processing

- **Always verify bill total**
- **Confirm payment amount**
- **Check payment method**
- **Review with customer** (optional but recommended)

### ✅ Best Practices

- Process payment promptly
- Verify amount before processing
- Count cash carefully
- Verify card payments
- Always print receipt
- Thank customer for tip

### ❌ Common Mistakes

- Processing wrong amount
- Selecting wrong payment method
- Forgetting to add tip
- Not printing receipt
- Rushing and making errors

---

## Special Situations

### Customer Pays Exact Amount

**If customer pays exact bill total:**
- Enter amount equal to bill
- No change needed
- Process payment

### Customer Pays More (Cash)

**If customer pays more than bill:**
- Enter amount received
- System calculates change
- Give change to customer
- Process payment

**Example:**
- Bill: $47.50
- Customer pays: $50.00
- Change: $2.50
- Give $2.50 to customer

### Customer Wants to Split Bill

**Use split payment feature:**
- See [Split Payments](./split-payments.md) for details
- Enter amounts for each method
- Verify total equals bill
- Process split payment

### Customer Disputes Amount

**If customer questions the bill:**
1. Review bill with customer
2. Check for errors
3. Correct if needed
4. Process payment with corrected amount
5. Get manager if needed

---

## Troubleshooting

### Problem: "Payment Amount Doesn't Match Bill"

**What it means:** Amount entered doesn't equal bill total

**Solution:**
- Verify payment amount
- Check bill total
- Ensure amounts match
- For split payments, verify all splits add up

### Problem: "Payment Method Not Available"

**What it means:** Selected payment method isn't working

**Solution:**
- Try different payment method
- Check if method is enabled
- Use cash as backup
- Contact manager if needed

### Problem: "Payment Processing Failed"

**What it means:** System error during processing

**Solution:**
1. Check error message
2. Verify all information is correct
3. Try again
4. Wait a moment and retry
5. Contact manager if issue persists

### Problem: "Receipt Won't Print"

**What it means:** Printer issue

**Solution:**
- Check printer is on and has paper
- Try printing manually
- Can email receipt if needed
- Contact manager if issue persists

---

## Quick Reference

```
1. Stop table session
2. Review bill
3. Click "Process Payment"
4. Select payment method
5. Enter amount
6. Add tip (optional)
7. Apply discount (if approved)
8. Review summary
9. Click "Process Payment"
10. Give receipt to customer
```

**Payment is complete!**

---

## Practice Exercise

**Try this:**
1. Start a test session
2. Add some items
3. Stop session
4. Process a payment
5. Verify receipt prints
6. Check payment appears in history

**Practice until you're comfortable!**

---

## Related Guides

- [Stopping a Session](../table-management/stopping-session.md)
- [Split Payments](./split-payments.md)
- [Applying Discounts](./applying-discounts.md)
- [Printing Receipts](./printing-receipts.md)

---

**Remember:** Always verify the amount before processing. Accuracy is more important than speed!

---

**Last Updated:** 2025-01-27  
**For:** Servers
