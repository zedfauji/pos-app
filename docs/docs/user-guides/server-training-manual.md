# Server Training Manual - Complete Guide

**For:** Bar servers and waitstaff  
**Estimated Training Time:** 2-3 hours  
**Last Updated:** 2025-01-27

This comprehensive manual covers everything you need to know to use MagiDesk POS effectively as a server.

---

## Table of Contents

1. [Getting Started](#getting-started)
2. [Table Management](#table-management)
3. [Taking Orders](#taking-orders)
4. [Processing Payments](#processing-payments)
5. [Handling Special Situations](#handling-special-situations)
6. [Daily Procedures](#daily-procedures)
7. [Troubleshooting](#troubleshooting)
8. [Best Practices](#best-practices)

---

## Getting Started

### Logging In

**First Time Login:**
1. Open MagiDesk application
2. Enter username (provided by manager)
3. Enter temporary password (provided by manager)
4. System will prompt you to create a new password
5. Enter new password (must be at least 8 characters)
6. Confirm new password
7. Click "Save"

**Regular Login:**
1. Open MagiDesk application
2. Enter your username
3. Enter your password
4. Click "Login"

**Security Tips:**
- Never share your password
- Log out when you're done
- Change password if you suspect it's compromised
- Use a strong password (mix of letters, numbers, symbols)

### Understanding the Interface

**Main Dashboard:**
- **Left Panel:** Navigation menu
- **Center:** Table layout (billiard and bar tables)
- **Top Right:** Your name, time, logout button

**Table Status Colors:**
- 🟢 **Green:** Table is available (no customers)
- 🔴 **Red:** Table is occupied (has active session)
- 🟡 **Yellow:** Table needs attention (rare, system alert)

**Navigation Menu:**
- **Dashboard:** Main screen with tables
- **Tables:** Detailed table management
- **Orders:** View all orders
- **Payments:** Payment history
- **Settings:** Your personal settings

---

## Table Management

### Starting a Table Session

**When to Start:**
- When customers sit down at a table
- When you assign a table to customers
- Before taking any orders for that table

**Step-by-Step:**

1. **Locate the table** on the dashboard
2. **Click on the table** (it should be green/available)
3. **Right-click** or click the **menu button** (three dots)
4. **Select "Start Session"**
5. **Select your name** from the server dropdown
6. **Click "Start"**

**What Happens:**
- Table status changes to "Occupied" (red)
- System creates a session record
- Timer starts automatically
- You can now add items to this table

**Important Notes:**
- Each table can only have one active session at a time
- Timer calculates bill based on time spent
- Session continues until you stop it

### Adding Items to a Table Session

**Step-by-Step:**

1. **Click on the occupied table** (red table)
2. **Click "Add Items"** or "View Order" button
3. **Menu appears** with categories:
   - Food
   - Drinks
   - Snacks
   - Combos
4. **Browse or search** for items
5. **Click on an item** to select it
6. **Enter quantity** (default is 1)
7. **Add modifiers** if available (size, toppings, etc.)
8. **Click "Add to Order"**
9. **Click "Confirm Order"**

**What Happens:**
- Items are added to the order
- Order is sent to kitchen/bar (if integrated)
- Items appear in the order list
- Total updates automatically
- Inventory is checked (out of stock items show warning)

**Tips:**
- You can add multiple items before confirming
- You can modify items before confirming
- Check item availability before adding
- Items show price as you add them

### Viewing Table Details

**To see table information:**

1. **Click on the table**
2. **View shows:**
   - Table number
   - Server name (you)
   - Session start time
   - Duration (how long customers have been there)
   - Current order total
   - Items ordered
   - Estimated bill (time + items)

**Useful Information:**
- **Duration:** Helps estimate time cost
- **Order Total:** Current items cost
- **Estimated Total:** Time cost + items cost

### Stopping a Table Session

**When to Stop:**
- When customers are ready to pay
- When customers are leaving
- When you need to free the table

**Step-by-Step:**

1. **Click on the occupied table** (red table)
2. **Click "Stop Session"** or "Bill" button
3. **System calculates:**
   - Time cost (minutes × rate per minute)
   - Items cost (sum of all items)
   - **Total bill**
4. **Review the bill:**
   - Time: X minutes at $Y per minute = $Z
   - Items: List of items with prices
   - **Total: $XX.XX**
5. **Click "Process Payment"** to continue to payment

**What Happens:**
- Session is closed
- Bill is generated
- Table becomes available (green)
- You proceed to payment screen

**Important:**
- Always review the bill before processing payment
- Verify all items are correct
- Check time calculation is accurate
- Confirm total amount

---

## Taking Orders

### Creating a New Order

**Step-by-Step:**

1. **Start a table session** (if not already started)
2. **Click "Add Items"** on the table
3. **Browse menu** or search for items
4. **Select items** you want to add
5. **Enter quantities** for each item
6. **Add modifiers** (size, toppings, etc.) if needed
7. **Review your order** in the cart
8. **Click "Confirm Order"**

**Order Confirmation:**
- Order is created
- Items are sent to kitchen/bar
- Order appears in "Orders" page
- Inventory is deducted automatically

### Modifying an Existing Order

**To Add More Items:**

1. **Click on the table**
2. **Click "Add Items"**
3. **Select new items**
4. **Confirm order**
5. New items are added to existing order

**To Remove Items:**

1. **Click on the table**
2. **Click "View Order"**
3. **Find the item** to remove
4. **Click "Remove"** or "Cancel Item"
5. **Confirm removal**
6. Item is removed from order

**To Change Quantities:**

1. **Click on the table**
2. **Click "View Order"**
3. **Find the item**
4. **Click "Edit"**
5. **Change quantity**
6. **Save changes**

**Important:**
- You can modify orders before they're delivered
- Some items may not be cancellable if already being prepared
- Check with kitchen/bar before removing items

### Viewing Order Status

**To check order status:**

1. **Click on the table**
2. **Click "View Order"**
3. **See order details:**
   - Order number
   - Items ordered
   - Status of each item:
     - ⏳ **Pending:** Order received, waiting to be prepared
     - 👨‍🍳 **Preparing:** Kitchen/bar is working on it
     - ✅ **Ready:** Item is ready for delivery
     - 🚚 **Delivered:** Item has been delivered to table

**Order Status Colors:**
- **Blue:** Pending
- **Yellow:** Preparing
- **Green:** Ready/Delivered
- **Red:** Cancelled

### Marking Items as Delivered

**When items are ready:**

1. **Click on the table**
2. **Click "View Order"**
3. **Find the ready item**
4. **Click "Mark as Delivered"**
5. **Item status changes to "Delivered"**

**Why this matters:**
- Tracks which items have been delivered
- Helps with billing accuracy
- Shows kitchen/bar what's still pending

---

## Processing Payments

### Basic Payment Processing

**Step-by-Step:**

1. **Stop the table session** (see "Stopping a Table Session")
2. **Review the bill:**
   - Time cost
   - Items cost
   - **Total amount**
3. **Click "Process Payment"**
4. **Payment screen appears:**
   - Bill total displayed
   - Payment method options
   - Amount input
5. **Select payment method:**
   - Cash
   - Card
   - Mobile Payment
   - Digital Wallet
6. **Enter payment amount** (usually equals bill total)
7. **Add tip** (optional):
   - Enter tip amount
   - Or select percentage
8. **Apply discount** (if applicable):
   - Click "Apply Discount"
   - Enter discount amount or percentage
   - Enter reason (optional)
9. **Review payment summary:**
   - Subtotal
   - Discount (if any)
   - Tip (if any)
   - **Total to pay**
10. **Click "Process Payment"**

**What Happens:**
- Payment is processed
- Payment is recorded in system
- Bill status changes to "Paid"
- Receipt is generated
- Receipt prints automatically (if printer connected)
- Table becomes available

**Payment Confirmation:**
- Success message appears
- Receipt is shown
- You can print receipt again if needed
- Payment appears in payment history

### Split Payments

**When customers want to pay separately:**

**Step-by-Step:**

1. **Process payment** as normal
2. **Select "Split Payment"** option
3. **Enter payment amounts:**
   - Example: $50 card, $30 cash, $20 mobile
   - System shows remaining balance
4. **Select payment method** for each amount
5. **Verify total** equals bill amount
6. **Add tips** (can split tip or add to one method)
7. **Click "Process Split Payment"**

**Split Payment Tips:**
- System validates that split total = bill total
- You can split by amount or percentage
- Each payment method is recorded separately
- Receipt shows breakdown of all payments

**Common Split Scenarios:**
- **Two people:** 50/50 split
- **Three people:** Equal thirds
- **Custom amounts:** Each person pays different amount
- **One person pays card, other pays cash**

### Applying Discounts

**When to apply discounts:**
- Manager-approved discounts
- Happy hour specials
- Customer loyalty discounts
- Promotional discounts

**Step-by-Step:**

1. **In payment screen**, click "Apply Discount"
2. **Enter discount:**
   - **Amount:** Enter dollar amount (e.g., $10)
   - **Percentage:** Enter percentage (e.g., 10%)
3. **Enter reason** (optional but recommended):
   - "Happy Hour"
   - "Loyalty Customer"
   - "Manager Approved"
   - "Promotion"
4. **Click "Apply"**
5. **Review updated total:**
   - Original total
   - Discount amount
   - **New total**
6. **Process payment** with discounted amount

**Discount Rules:**
- Discounts reduce the bill total
- Discounts are recorded for reporting
- Manager approval may be required for large discounts
- Discount reason helps with reporting

### Processing Refunds

**When customer needs a refund:**

**Step-by-Step:**

1. **Go to "Payments" page**
2. **Find the payment** to refund:
   - Search by table number
   - Search by date
   - Search by amount
3. **Click on the payment**
4. **Click "Refund" button**
5. **Enter refund details:**
   - **Refund amount:** Full or partial
   - **Refund method:** Original payment method or cash
   - **Reason:** Why refund is needed (optional)
6. **Review refund summary:**
   - Original payment amount
   - Refund amount
   - Remaining balance (if partial)
7. **Click "Process Refund"**
8. **Confirm refund**

**Refund Rules:**
- Can refund full or partial amount
- Can refund multiple times (up to original payment)
- Refund method can be original method or cash
- Refunds are recorded for audit
- Manager approval may be required

**Important:**
- Always verify refund is legitimate
- Enter reason for refund (helps with reporting)
- Check refund amount is correct
- Confirm with manager for large refunds

### Printing Receipts

**Automatic Printing:**
- Receipts print automatically after payment
- Printer must be connected and on
- Receipt includes all payment details

**Manual Printing:**

1. **After payment**, receipt screen appears
2. **Click "Print Receipt"** button
3. **Select printer** (if multiple printers)
4. **Receipt prints**

**Receipt Contents:**
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
- Payment method(s)
- Thank you message

**Email Receipt (if enabled):**
1. **After payment**, click "Email Receipt"
2. **Enter customer email**
3. **Click "Send"**
4. Receipt is emailed to customer

---

## Handling Special Situations

### Customer Wants to Move Tables

**Step-by-Step:**

1. **Stop current table session** (if needed)
2. **Note the current order** and items
3. **Start new session** on new table
4. **Add existing items** to new table's order
5. **Continue with new table**

**Or use Move Table feature:**

1. **Click on current table**
2. **Click "Move Table"** or "Transfer"
3. **Select destination table**
4. **Confirm move**
5. Session and order transfer to new table

### Customer Wants to Add Time

**If customer wants to extend their session:**

- **Don't stop the session** - just let it continue
- Timer keeps running automatically
- Bill will include extended time when they pay

**If you accidentally stopped session:**

1. **Start new session** on same table
2. **Add any new items** to order
3. **Time continues** from new session start
4. **Note:** Previous time is already billed

### Item is Out of Stock

**When adding items:**

1. **System shows warning** if item is out of stock
2. **Item appears grayed out** or shows "Out of Stock"
3. **You cannot add** out of stock items
4. **Suggest alternatives** to customer

**If item becomes out of stock after order:**

1. **Kitchen/bar will notify** you
2. **Remove item** from order
3. **Inform customer**
4. **Offer alternatives**

### Payment Processing Error

**If payment fails:**

1. **Error message appears**
2. **Read the error message:**
   - "Amount exceeds bill total" → Check amount
   - "Payment method not available" → Try different method
   - "System error" → Try again or contact support
3. **Try again** with corrected information
4. **If still fails**, contact manager or IT support

**Common Errors:**
- **Amount mismatch:** Verify payment amount equals bill total
- **Invalid payment method:** Select valid payment method
- **System unavailable:** Wait a moment and try again
- **Network error:** Check internet connection

### Customer Disputes Bill

**If customer questions the bill:**

1. **Review bill with customer:**
   - Show time cost breakdown
   - Show items ordered
   - Show total calculation
2. **Check for errors:**
   - Wrong items?
   - Wrong quantities?
   - Wrong time calculation?
3. **If error found:**
   - Correct the order
   - Recalculate bill
   - Process payment with corrected amount
4. **If no error:**
   - Explain charges clearly
   - Show itemized breakdown
   - Contact manager if needed

---

## Daily Procedures

### Opening Shift Checklist

**Before starting your shift:**

- [ ] Log into the system
- [ ] Check all tables are available (green)
- [ ] Verify printer is working (print test receipt)
- [ ] Check menu items are available
- [ ] Review any specials or promotions
- [ ] Check your assigned tables (if applicable)

### During Shift

**While serving customers:**

- [ ] Start session when customers sit down
- [ ] Take orders accurately
- [ ] Check order status regularly
- [ ] Mark items as delivered when ready
- [ ] Process payments promptly
- [ ] Print receipts for customers
- [ ] Stop sessions when customers leave

### Closing Shift Checklist

**Before ending your shift:**

- [ ] Process all pending payments
- [ ] Stop all active table sessions
- [ ] Verify all tables are available (green)
- [ ] Review your sales for the day
- [ ] Log out of the system
- [ ] Report any issues to manager

---

## Troubleshooting

### Can't Log In

**Problem:** Username or password not working

**Solutions:**
1. Check username and password are correct
2. Check Caps Lock is off
3. Try typing password in notepad first, then copy/paste
4. Ask manager to reset password
5. Contact IT support if issue persists

### Table Won't Start

**Problem:** Can't start a table session

**Solutions:**
1. Check table is available (green, not red)
2. Check if table already has active session
3. Try refreshing the page
4. Log out and log back in
5. Contact manager if issue persists

### Items Not Showing in Menu

**Problem:** Can't find items in menu

**Solutions:**
1. Check if items are out of stock (will show as unavailable)
2. Try searching for item name
3. Check different menu categories
4. Ask manager if item was removed from menu
5. Contact manager if item should be available

### Payment Won't Process

**Problem:** Payment button doesn't work or payment fails

**Solutions:**
1. Verify payment amount equals bill total
2. Check payment method is selected
3. Verify internet connection
4. Try different payment method
5. Wait a moment and try again
6. Contact manager if issue persists

### Receipt Won't Print

**Problem:** Receipt doesn't print after payment

**Solutions:**
1. Check printer is on and connected
2. Check printer has paper
3. Check printer is selected correctly
4. Try printing receipt manually (click "Print Receipt")
5. Check printer settings
6. Contact manager if issue persists

### System is Slow

**Problem:** App is running slowly

**Solutions:**
1. Check internet connection
2. Close other applications
3. Refresh the page
4. Log out and log back in
5. Restart the computer
6. Contact IT support if issue persists

---

## Best Practices

### Order Taking

✅ **Do:**
- Take orders accurately
- Verify items with customer
- Check item availability before ordering
- Confirm quantities
- Double-check order before confirming

❌ **Don't:**
- Guess item names
- Add items without checking availability
- Skip quantity confirmation
- Rush through order taking

### Payment Processing

✅ **Do:**
- Always verify bill total before processing
- Count cash carefully
- Verify card payments
- Print receipts for all payments
- Keep payment area secure

❌ **Don't:**
- Process payment without verifying amount
- Skip receipt printing
- Leave payment screen unattended
- Share payment information

### Table Management

✅ **Do:**
- Start sessions immediately when customers sit
- Stop sessions when customers leave
- Keep tables updated accurately
- Check table status regularly

❌ **Don't:**
- Forget to start sessions
- Leave sessions running after customers leave
- Start multiple sessions on same table
- Ignore table status

### General

✅ **Do:**
- Log out when done
- Keep your password secure
- Ask for help when needed
- Report issues promptly
- Follow procedures accurately

❌ **Don't:**
- Share your login
- Leave system logged in
- Ignore error messages
- Skip steps in procedures
- Make up procedures

---

## Getting Help

### When to Ask for Help

**Ask your manager for:**
- Password resets
- Discount approvals
- Refund approvals
- Menu changes
- Inventory questions
- Policy questions

**Ask IT support for:**
- Login issues
- System errors
- Printer problems
- Network issues
- Technical problems

### How to Report Issues

**When reporting an issue, include:**
- What you were trying to do
- What happened instead
- Error message (if any)
- Time it occurred
- Table number (if applicable)
- Your name

---

## Practice Exercises

### Exercise 1: Basic Table Service

**Scenario:** Two customers sit at Table 5

**Tasks:**
1. Start a session for Table 5
2. Add 2 beers and 1 pizza to order
3. Mark items as delivered
4. Stop session and process $50 cash payment
5. Print receipt

### Exercise 2: Split Payment

**Scenario:** Four customers at Table 8, bill is $100

**Tasks:**
1. Stop session
2. Process split payment: $40 card, $30 cash, $20 mobile, $10 card
3. Verify total equals $100
4. Process payment
5. Print receipt

### Exercise 3: Order Modification

**Scenario:** Customer at Table 3 wants to add items and remove one item

**Tasks:**
1. View current order
2. Add 2 more beers
3. Remove 1 pizza (not yet prepared)
4. Confirm changes
5. Verify order is correct

---

## Summary

**Key Takeaways:**
- Always start sessions when customers sit down
- Take orders accurately and verify with customers
- Process payments carefully and verify amounts
- Keep tables updated (start/stop sessions)
- Ask for help when needed
- Follow procedures consistently

**Remember:** The system is here to help you serve customers better. Take your time, be accurate, and don't hesitate to ask questions!

---

**Training Complete!** You're now ready to use MagiDesk POS effectively.

**Next Steps:**
- Practice with test scenarios
- Shadow an experienced server
- Review this manual as needed
- Ask questions when unsure

**Good luck!** 🎉

---

**Last Updated:** 2025-01-27  
**Training Time:** 2-3 hours  
**Difficulty:** Beginner to Intermediate
