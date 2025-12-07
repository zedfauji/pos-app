# Manager Training Manual - Complete Guide

**For:** Bar managers and supervisors  
**Estimated Training Time:** 3-4 hours  
**Last Updated:** 2025-01-27

This comprehensive manual covers everything managers need to know to effectively manage operations using MagiDesk POS.

---

## Table of Contents

1. [Manager Overview](#manager-overview)
2. [User Management](#user-management)
3. [Menu Management](#menu-management)
4. [Inventory Management](#inventory-management)
5. [Reports & Analytics](#reports--analytics)
6. [Payment Management](#payment-management)
7. [Settings & Configuration](#settings--configuration)
8. [Daily Management Tasks](#daily-management-tasks)
9. [Troubleshooting](#troubleshooting)
10. [Best Practices](#best-practices)

---

## Manager Overview

### Manager Access & Permissions

**What managers can do:**
- View all reports and analytics
- Manage menu items and prices
- Manage inventory and stock levels
- Process refunds and approve discounts
- Manage user accounts (servers, cashiers, etc.)
- Configure system settings
- View all transactions
- Access audit logs

**Manager Dashboard:**
- **Today's Sales:** Total revenue for today
- **Active Tables:** Number of occupied tables
- **Pending Orders:** Orders waiting to be prepared
- **Low Stock Alerts:** Items that need restocking
- **Recent Payments:** Latest transactions
- **Top Selling Items:** Most popular items today
- **Average Order Value:** Average amount per order

### Manager Navigation

**Additional menu items for managers:**
- 👥 **Users** - Staff management
- 📊 **Reports** - Sales, inventory, customer reports
- 🍔 **Menu Management** - Manage menu and prices
- 📦 **Inventory** - Stock management
- 💰 **Cash Flow** - Financial tracking
- ⚙️ **Settings** - System configuration
- 🔍 **Audit Reports** - Activity logs

---

## User Management

### Adding a New Server

**Step-by-Step:**

1. **Click "Users"** in navigation
2. **Click "Add User"** button
3. **Enter user details:**
   - **Username:** Choose a username (e.g., "john.smith")
   - **Full Name:** Enter full name
   - **Email:** Enter email (optional)
   - **Phone:** Enter phone (optional)
4. **Select Role:**
   - **Server** - Can take orders and process payments
   - **Cashier** - Can only process payments
   - **Host** - Can only manage tables
   - **Manager** - Full access
5. **Set initial password:**
   - System generates temporary password
   - Or you can set custom password
6. **Click "Create User"**
7. **Give credentials to new user:**
   - Username
   - Temporary password
   - User must change password on first login

**User Roles Explained:**
- **Server:** Full service access (orders, payments, tables)
- **Cashier:** Payment processing only
- **Host:** Table management only
- **Manager:** Everything + management features

### Resetting a Password

**When server forgets password:**

1. **Click "Users"**
2. **Find the user**
3. **Click "Reset Password"**
4. **New temporary password is generated**
5. **Give password to user**
6. **User must change password on first login**

**Security Tips:**
- Never share your manager password
- Reset passwords regularly
- Use strong passwords
- Don't write passwords down

### Managing User Permissions

**To change user role:**

1. **Click "Users"**
2. **Find the user**
3. **Click "Edit"**
4. **Change role** from dropdown
5. **Click "Save"**

**To deactivate a user:**

1. **Click "Users"**
2. **Find the user**
3. **Click "Edit"**
4. **Toggle "Active" to "No"**
5. **Click "Save"**

**What happens:**
- User cannot log in
- User's past transactions remain in system
- Can reactivate user later

### Viewing User Activity

**To see what users are doing:**

1. **Click "Users"**
2. **Click on a user**
3. **View "Activity" tab:**
   - Last login time
   - Recent transactions
   - Orders processed
   - Payments processed

**Why this matters:**
- Monitor staff activity
- Identify training needs
- Track performance
- Security auditing

---

## Menu Management

### Adding a Menu Item

**Step-by-Step:**

1. **Click "Menu Management"**
2. **Click "Add Item"**
3. **Enter item details:**
   - **Name:** Item name (e.g., "Margherita Pizza")
   - **Description:** Item description (optional)
   - **Category:** Select category (Food, Drinks, etc.)
   - **Price:** Enter price (e.g., 12.99)
   - **SKU:** Item SKU/barcode (optional)
4. **Link to inventory** (if applicable):
   - Select inventory item
   - System checks stock automatically
5. **Set availability:**
   - **Available:** Item shows in menu
   - **Unavailable:** Item hidden from menu
6. **Add modifiers** (optional):
   - Size options (Small, Medium, Large)
   - Toppings
   - Customizations
7. **Click "Save"**

**What happens:**
- Item appears in menu immediately
- Servers can add item to orders
- Item is linked to inventory (if configured)

### Updating Prices

**Step-by-Step:**

1. **Click "Menu Management"**
2. **Find the item**
3. **Click "Edit"**
4. **Change the price**
5. **Enter reason** (optional but recommended):
   - "Price increase"
   - "Promotion"
   - "Cost adjustment"
6. **Click "Save"**

**What happens:**
- New price takes effect immediately
- Old price is saved in history
- All new orders use new price
- Existing orders keep old price

**Price History:**
- System tracks all price changes
- View history in item details
- Useful for reporting and audits

### Marking Items as Out of Stock

**When item runs out:**

1. **Click "Menu Management"**
2. **Find the item**
3. **Click "Edit"**
4. **Toggle "Available" to "No"**
5. **Click "Save"**

**What happens:**
- Item disappears from menu
- Servers cannot add item to orders
- Item shows as "Out of Stock" if servers try to add it
- Can mark available again when stock arrives

**Alternative method:**
- System automatically marks items out of stock when inventory reaches zero
- You can override manually

### Creating Combos

**Step-by-Step:**

1. **Click "Menu Management"**
2. **Click "Combos" tab**
3. **Click "Add Combo"**
4. **Enter combo details:**
   - **Name:** Combo name (e.g., "Burger Combo")
   - **Price:** Combo price
   - **Description:** What's included
5. **Add items to combo:**
   - Select items from menu
   - Add quantities for each item
6. **Set availability**
7. **Click "Save"**

**Combo Benefits:**
- Encourages upselling
- Simplifies ordering
- Can offer discounts
- Tracks combo sales separately

### Managing Modifiers

**Modifiers are options for items (size, toppings, etc.):**

1. **Click "Menu Management"**
2. **Click "Modifiers" tab**
3. **Click "Add Modifier"**
4. **Enter modifier details:**
   - **Name:** Modifier name (e.g., "Size")
   - **Type:** Single choice or multiple choice
5. **Add options:**
   - Small (+$0.00)
   - Medium (+$1.00)
   - Large (+$2.00)
6. **Link to menu items**
7. **Click "Save"**

---

## Inventory Management

### Checking Stock Levels

**To view current inventory:**

1. **Click "Inventory"**
2. **View inventory list:**
   - Item name
   - Current quantity
   - Reorder threshold
   - Status (In Stock, Low Stock, Out of Stock)

**Stock Status Colors:**
- 🟢 **Green:** In stock (above threshold)
- 🟡 **Yellow:** Low stock (below threshold)
- 🔴 **Red:** Out of stock (zero quantity)

### Updating Stock Levels

**When you receive a delivery:**

1. **Click "Inventory"**
2. **Find the item**
3. **Click "Adjust Stock"**
4. **Enter adjustment:**
   - **Method 1:** Enter new total quantity
   - **Method 2:** Enter adjustment amount (+50, -10, etc.)
5. **Select reason:**
   - "Delivery received"
   - "Waste/Spillage"
   - "Manual count"
   - "Theft/Loss"
6. **Enter notes** (optional)
7. **Click "Save"**

**What happens:**
- Stock level updates immediately
- Adjustment is logged for audit
- Low stock alerts update
- Menu availability updates (if linked)

### Setting Reorder Thresholds

**To set when to reorder:**

1. **Click "Inventory"**
2. **Find the item**
3. **Click "Edit"**
4. **Enter "Reorder Threshold"** (e.g., 10 units)
5. **Click "Save"**

**What happens:**
- System alerts when stock falls below threshold
- Helps prevent running out
- Can generate reorder reports

### Low Stock Alerts

**System automatically alerts when stock is low:**

1. **Check dashboard** for low stock alerts
2. **Click "Inventory"** to see all low stock items
3. **Review items** that need restocking
4. **Place orders** with vendors
5. **Update stock** when delivery arrives

**Alert Settings:**
- Can configure alert thresholds
- Can set up email alerts
- Can view alerts in reports

---

## Reports & Analytics

### Daily Sales Report

**To view today's sales:**

1. **Click "Reports"**
2. **Click "Sales Reports"**
3. **Select "Today"** from date range
4. **View report:**
   - Total sales
   - Number of transactions
   - Average order value
   - Payment methods breakdown
   - Top selling items
   - Hourly sales breakdown

**Report Details:**
- **Total Sales:** Sum of all payments
- **Transactions:** Number of orders/payments
- **Average Order Value:** Total sales ÷ transactions
- **Payment Methods:** Cash, card, mobile breakdown
- **Top Items:** Best selling items today

### Weekly/Monthly Reports

**To view longer periods:**

1. **Click "Reports"**
2. **Click "Sales Reports"**
3. **Select date range:**
   - This week
   - This month
   - Custom range
4. **View trends:**
   - Sales trends over time
   - Day-by-day comparison
   - Peak hours analysis
   - Seasonal patterns

### Inventory Reports

**To view inventory analytics:**

1. **Click "Reports"**
2. **Click "Inventory Reports"**
3. **View:**
   - Current stock levels
   - Low stock items
   - Items sold (quantity)
   - Inventory value
   - Turnover rates
   - Reorder recommendations

**Inventory Metrics:**
- **Stock Value:** Total value of inventory
- **Turnover Rate:** How fast items sell
- **Days of Stock:** How long current stock will last
- **Reorder Points:** When to reorder each item

### Customer Reports

**To view customer analytics:**

1. **Click "Reports"**
2. **Click "Customer Reports"**
3. **View:**
   - Total customers
   - Repeat customers
   - Average customer value
   - Customer segments
   - Loyalty program status
   - Customer lifetime value

### Exporting Reports

**To export reports:**

1. **View any report**
2. **Click "Export"** button
3. **Select format:**
   - PDF (for printing)
   - Excel (for analysis)
   - CSV (for data import)
4. **Click "Download"**
5. **File downloads** to your computer

---

## Payment Management

### Viewing All Payments

**To see payment history:**

1. **Click "Payments"**
2. **View payment list:**
   - Date and time
   - Table number
   - Server name
   - Amount
   - Payment method
   - Status

**Filter Options:**
- By date range
- By server
- By payment method
- By amount range
- By status

### Processing Refunds

**When customer needs refund:**

1. **Click "Payments"**
2. **Find the payment** to refund
3. **Click "Refund"**
4. **Enter refund details:**
   - **Amount:** Full or partial
   - **Method:** Original method or cash
   - **Reason:** Why refund is needed
5. **Review refund summary**
6. **Click "Process Refund"**
7. **Confirm refund**

**Refund Rules:**
- Can refund full or partial amount
- Can refund multiple times (up to original)
- All refunds are logged
- Manager approval required for large refunds

### Approving Discounts

**When server requests discount:**

1. **Server requests discount** (via system or verbally)
2. **Review request:**
   - Is discount legitimate?
   - Is amount reasonable?
   - Does it follow policy?
3. **If approved:**
   - Go to payment screen
   - Apply discount
   - Enter approval reason
   - Process payment
4. **If denied:**
   - Explain reason to server
   - Process payment without discount

**Discount Policy:**
- Set discount limits
- Require reasons for discounts
- Track discount usage
- Review discount reports regularly

---

## Settings & Configuration

### Business Settings

**To update business information:**

1. **Click "Settings"**
2. **Click "Business Settings"**
3. **Update:**
   - Business name
   - Address
   - Phone number
   - Email
   - Tax ID
4. **Click "Save"**

**What this affects:**
- Appears on receipts
- Used in reports
- Customer communications

### POS Settings

**To configure POS behavior:**

1. **Click "Settings"**
2. **Click "POS Settings"**
3. **Configure:**
   - Time rate per minute (for tables)
   - Tax rates
   - Rounding rules
   - Default payment methods
4. **Click "Save"**

### Receipt Settings

**To customize receipts:**

1. **Click "Settings"**
2. **Click "Receipt Settings"**
3. **Configure:**
   - Receipt format (58mm or 80mm)
   - Printer selection
   - Receipt content
   - Logo (if applicable)
4. **Click "Save"**

### Printer Settings

**To configure printers:**

1. **Click "Settings"**
2. **Click "Printer Settings"**
3. **Select printer:**
   - Choose from available printers
   - Test print
   - Configure paper size
4. **Click "Save"**

---

## Daily Management Tasks

### Morning Tasks

**Before opening:**

- [ ] Review previous day's sales
- [ ] Check inventory levels
- [ ] Review low stock alerts
- [ ] Update menu (if needed)
- [ ] Check system status
- [ ] Verify all servers can log in
- [ ] Review any overnight issues

### During Service

**While business is open:**

- [ ] Monitor active tables
- [ ] Check order statuses
- [ ] Approve discounts as needed
- [ ] Handle customer issues
- [ ] Monitor system performance
- [ ] Support servers as needed

### End of Day Tasks

**After closing:**

- [ ] Review daily sales report
- [ ] Verify all payments processed
- [ ] Check for any issues
- [ ] Review server performance
- [ ] Update inventory (if deliveries received)
- [ ] Generate end-of-day report
- [ ] Reconcile cash (if applicable)
- [ ] Plan for next day

### Weekly Tasks

**Once per week:**

- [ ] Review weekly sales report
- [ ] Analyze trends
- [ ] Review inventory turnover
- [ ] Check customer reports
- [ ] Review staff performance
- [ ] Update menu (if needed)
- [ ] Plan promotions

### Monthly Tasks

**Once per month:**

- [ ] Review monthly sales
- [ ] Analyze profitability
- [ ] Review inventory costs
- [ ] Customer analysis
- [ ] Staff performance reviews
- [ ] System maintenance
- [ ] Plan improvements

---

## Troubleshooting

### Server Can't Log In

**Problem:** Server reports login issues

**Solutions:**
1. Verify username and password are correct
2. Check if user account is active
3. Reset password if needed
4. Check if system is down
5. Contact IT support if issue persists

### Payment Processing Issues

**Problem:** Payments failing or not processing

**Solutions:**
1. Check PaymentApi is running
2. Verify database connectivity
3. Check payment amount is correct
4. Review payment logs
5. Try processing payment again
6. Contact IT support if needed

### Inventory Discrepancies

**Problem:** Stock levels don't match reality

**Solutions:**
1. Perform physical inventory count
2. Compare with system counts
3. Adjust stock levels
4. Investigate discrepancies
5. Review transaction history
6. Update procedures if needed

### System Performance Issues

**Problem:** System is slow or unresponsive

**Solutions:**
1. Check internet connection
2. Restart the application
3. Check system resources
4. Review recent changes
5. Contact IT support
6. Use manual backup if needed

---

## Best Practices

### User Management

✅ **Do:**
- Create users promptly for new staff
- Use clear, consistent usernames
- Reset passwords regularly
- Deactivate users who leave
- Review user activity regularly

❌ **Don't:**
- Share manager credentials
- Create users without proper roles
- Leave inactive users active
- Ignore security alerts

### Menu Management

✅ **Do:**
- Keep menu updated
- Update prices promptly
- Mark items out of stock immediately
- Review menu regularly
- Track price changes

❌ **Don't:**
- Leave outdated items in menu
- Forget to update prices
- Ignore inventory links
- Make changes without testing

### Inventory Management

✅ **Do:**
- Update stock when deliveries arrive
- Set appropriate reorder thresholds
- Review low stock alerts daily
- Perform regular inventory counts
- Track inventory adjustments

❌ **Don't:**
- Ignore low stock alerts
- Skip inventory updates
- Forget to link items to inventory
- Ignore discrepancies

### Reporting

✅ **Do:**
- Review reports daily
- Analyze trends regularly
- Export reports for records
- Share insights with team
- Use reports for decision-making

❌ **Don't:**
- Ignore reports
- Make decisions without data
- Skip regular reviews
- Ignore anomalies

---

## Getting Help

### When to Contact IT Support

**Contact IT for:**
- System errors
- Login issues
- Payment processing failures
- Database errors
- Network issues
- Printer problems

### When to Contact Owner/Head Office

**Contact owner for:**
- Policy questions
- Large refunds
- Major discounts
- Staff issues
- Business decisions
- Financial questions

---

## Summary

**Key Manager Responsibilities:**
- Manage staff and user accounts
- Maintain menu and prices
- Monitor inventory levels
- Review reports and analytics
- Process refunds and approve discounts
- Configure system settings
- Support servers during service
- Ensure system is working properly

**Remember:** Your role is to ensure smooth operations and support your team. Use the system tools to make informed decisions and keep everything running efficiently.

---

**Training Complete!** You're now ready to manage operations effectively.

**Next Steps:**
- Practice with test scenarios
- Review reports regularly
- Train your servers
- Ask questions when needed

**Good luck managing your bar!** 🎉

---

**Last Updated:** 2025-01-27  
**Training Time:** 3-4 hours  
**Difficulty:** Intermediate to Advanced
