# MagiDesk POS - Operational Handbook

## What is This Document?

This handbook is your **complete guide to running and maintaining the MagiDesk Point of Sale system**. Think of it as the instruction manual for keeping your restaurant/billiard hall POS system running smoothly day-to-day.

**Who should read this:**
- System administrators
- IT support staff
- Store managers
- Operations teams
- Anyone responsible for keeping the system running

---

## Table of Contents

1. [System Overview](#system-overview)
2. [Daily Operations](#daily-operations)
3. [Common Tasks](#common-tasks)
4. [Monitoring & Health Checks](#monitoring--health-checks)
5. [Troubleshooting Guide](#troubleshooting-guide)
6. [Emergency Procedures](#emergency-procedures)
7. [Maintenance Schedules](#maintenance-schedules)

---

## System Overview

### What is MagiDesk POS?

**In Simple Terms:** MagiDesk is a computer system that helps restaurants and billiard halls manage:
- **Tables** - Which tables are occupied, how long customers have been there
- **Orders** - What customers ordered (food, drinks, pool time)
- **Payments** - How customers paid (cash, card, mobile)
- **Inventory** - What items you have in stock
- **Menu** - What items you sell and their prices
- **Customers** - Customer information and loyalty programs
- **Reports** - Sales reports, inventory reports, financial reports

### System Architecture (Simple Explanation)

Think of MagiDesk like a restaurant with different departments:

1. **Frontend (Desktop App)** - The computer screen servers use to take orders
   - Like the order pad a server carries
   - Shows tables, menu items, orders
   - Runs on Windows computers

2. **Backend APIs (9 Services)** - The "brain" that processes everything
   - **UsersApi** - Manages who can log in and what they can do
   - **MenuApi** - Manages menu items (pizza, drinks, etc.)
   - **OrderApi** - Processes orders from customers
   - **PaymentApi** - Handles payments and refunds
   - **InventoryApi** - Tracks what's in stock
   - **SettingsApi** - Stores system settings
   - **CustomerApi** - Manages customer information
   - **DiscountApi** - Handles discounts and promotions
   - **TablesApi** - Manages table sessions and bills

3. **Database (PostgreSQL)** - The filing cabinet that stores all information
   - All orders, payments, inventory, customers
   - Like a giant spreadsheet that never loses data

4. **Cloud Infrastructure (Google Cloud)** - Where everything runs
   - Like renting office space in the cloud
   - Services run 24/7 automatically
   - Handles backups and security

### How Data Flows (Simple Example)

**Customer Orders a Pizza:**
1. Server clicks "Pizza" on the desktop app (Frontend)
2. Frontend sends order to OrderApi (Backend)
3. OrderApi checks if pizza is available in InventoryApi
4. OrderApi saves order to Database
5. OrderApi deducts pizza from inventory
6. Server sees "Order Confirmed" on screen
7. When customer pays, PaymentApi processes payment
8. PaymentApi updates Database with payment info
9. Bill is generated and can be printed

---

## Daily Operations

### Morning Startup Checklist

**Before Opening:**
- [ ] Verify all APIs are running (check health endpoints)
- [ ] Check database connectivity
- [ ] Verify printer is connected and working
- [ ] Test login for at least one user
- [ ] Check inventory levels (low stock alerts)
- [ ] Verify payment processing is working
- [ ] Check for any overnight errors in logs

**How to Check:**
```powershell
# Check all API health endpoints
.\test-health-endpoints.ps1
```

### During Business Hours

**Monitor:**
- System performance (slow responses?)
- Error messages on screens
- Payment processing issues
- Printer connectivity
- Network connectivity

**Common Issues:**
- Slow order processing → Check API response times
- Payment failures → Check PaymentApi logs
- Printer not working → Check printer connection
- Can't log in → Check UsersApi

### End of Day Checklist

**Before Closing:**
- [ ] Process all pending payments
- [ ] Close all open table sessions
- [ ] Generate end-of-day reports
- [ ] Backup database (automatic, but verify)
- [ ] Check for any unresolved errors
- [ ] Review cash flow reports

---

## Common Tasks

### Starting a Table Session

**What it does:** Tracks when a customer sits at a table and starts the timer

**Steps:**
1. Open TablesPage in the app
2. Click on an available table
3. Select "Start Session"
4. Choose the server name
5. System creates a session and starts tracking time

**Behind the scenes:**
- TablesApi creates a session record
- System generates a unique session ID
- Timer starts counting minutes
- Table status changes to "Occupied"

### Processing a Payment

**What it does:** Records how a customer paid for their bill

**Steps:**
1. Server stops the table session
2. System calculates total (time + items)
3. Server selects payment method (cash/card/mobile)
4. Enter payment amount
5. System processes payment
6. Receipt is generated

**Behind the scenes:**
- PaymentApi creates payment record
- Bill ledger is updated
- Payment status changes to "Paid"
- Receipt PDF is generated

### Managing Inventory

**What it does:** Tracks what items you have in stock

**Adding Stock:**
1. Go to Inventory Management page
2. Select an item
3. Click "Adjust Stock"
4. Enter quantity to add
5. System updates inventory count

**Behind the scenes:**
- InventoryApi updates stock level
- Transaction is logged for audit
- Low stock alerts are checked
- Reports are updated

### Processing a Refund

**What it does:** Returns money to a customer

**Steps:**
1. Go to Payments page
2. Find the payment to refund
3. Click "Refund"
4. Enter refund amount
5. Select refund method (original/cash)
6. Enter reason (optional)
7. Confirm refund

**Behind the scenes:**
- PaymentApi creates refund record
- Bill ledger is updated (reduces total_paid)
- Payment status changes to "Refunded"
- Refund is logged for audit

---

## Monitoring & Health Checks

### What to Monitor

**System Health:**
- API response times (should be < 500ms)
- Database connection status
- Error rates
- Active sessions count
- Payment processing success rate

**Business Metrics:**
- Daily sales totals
- Average order value
- Table turnover rate
- Popular menu items
- Inventory levels

### Health Check Commands

**Check All APIs:**
```powershell
.\test-health-endpoints.ps1
```

**Check Database:**
```powershell
.\test-db-connection.ps1
```

**Check Payment Flow:**
```powershell
.\test-payment-flow.ps1
```

### Monitoring Tools

**Google Cloud Console:**
- View API logs
- Check service status
- Monitor resource usage
- View error rates

**Application Logs:**
- Frontend logs: `logs/frontend.log`
- Backend logs: Cloud Logging
- Error logs: `logs/errors.log`

---

## Troubleshooting Guide

### Problem: Can't Log In

**Symptoms:**
- Login page shows "Invalid credentials"
- User exists but password doesn't work

**Solutions:**
1. Verify username and password are correct
2. Check UsersApi is running: `GET /health`
3. Check database connectivity
4. Reset password if needed (admin only)

**Check:**
```powershell
# Test UsersApi
Invoke-WebRequest -Uri "https://magidesk-backend-904541739138.us-central1.run.app/health"
```

### Problem: Orders Not Processing

**Symptoms:**
- Orders stuck in "Pending"
- Error message when creating order
- Items not appearing in orders

**Solutions:**
1. Check OrderApi health: `GET /health`
2. Check InventoryApi connectivity
3. Verify items are available in inventory
4. Check database connection
5. Review OrderApi logs

**Check:**
```powershell
# Test OrderApi
Invoke-WebRequest -Uri "https://magidesk-order-904541739138.northamerica-south1.run.app/health"
```

### Problem: Payments Failing

**Symptoms:**
- Payment button doesn't work
- Payment shows as "Failed"
- Money not recorded

**Solutions:**
1. Check PaymentApi health
2. Verify billing ID is valid
3. Check payment amount doesn't exceed total
4. Review PaymentApi logs
5. Check database connectivity

### Problem: Printer Not Working

**Symptoms:**
- Receipts not printing
- Printer not found
- Print jobs failing

**Solutions:**
1. Check printer is connected (USB/Network)
2. Verify printer is powered on
3. Check printer drivers are installed
4. Test print from Windows
5. Check printer name in settings
6. Verify PDF generation is working

### Problem: Slow Performance

**Symptoms:**
- App takes long to load
- Orders process slowly
- Reports take forever

**Solutions:**
1. Check API response times
2. Check database query performance
3. Verify network connectivity
4. Check for high CPU/memory usage
5. Review recent changes/deployments
6. Check for database locks

---

## Emergency Procedures

### System Down - All APIs Offline

**Immediate Actions:**
1. **Don't Panic** - Most issues are temporary
2. Check Google Cloud Console for service status
3. Verify database is accessible
4. Check network connectivity
5. Review recent deployments

**Recovery Steps:**
1. Restart affected services (Cloud Run auto-restarts)
2. Check service logs for errors
3. Verify database connectivity
4. Test health endpoints
5. Notify team if issue persists

**Fallback:**
- Use manual order taking (paper)
- Process payments manually
- Record everything for later entry

### Database Connection Lost

**Symptoms:**
- All operations fail
- "Database error" messages
- Can't save any data

**Immediate Actions:**
1. Check Cloud SQL status in Google Cloud Console
2. Verify network connectivity
3. Check database credentials
4. Review connection string

**Recovery:**
1. Database usually auto-recovers
2. If not, contact Google Cloud support
3. Verify backups are current
4. Test connection once restored

### Payment Processing Down

**Immediate Actions:**
1. Accept cash payments only
2. Record card payments manually
3. Process payments later when system is back
4. Keep detailed records

**Recovery:**
1. Check PaymentApi health
2. Verify database connectivity
3. Test payment endpoint
4. Process backlog of payments

### Data Loss Suspected

**Immediate Actions:**
1. **STOP** making changes
2. Document what data is missing
3. Check recent backups
4. Review audit logs
5. Contact technical team immediately

**Recovery:**
1. Restore from latest backup
2. Verify data integrity
3. Review what caused loss
4. Implement prevention measures

---

## Maintenance Schedules

### Daily Maintenance

**Automated:**
- Database backups (every 6 hours)
- Log rotation
- Health checks

**Manual Checks:**
- Review error logs
- Check system performance
- Verify backups completed

### Weekly Maintenance

**Tasks:**
- Review system performance metrics
- Check disk space usage
- Review security logs
- Update documentation
- Test backup restoration

### Monthly Maintenance

**Tasks:**
- Review and archive old logs
- Performance optimization review
- Security audit
- Update dependencies
- Review and update runbooks

### Quarterly Maintenance

**Tasks:**
- Full system audit
- Disaster recovery drill
- Performance benchmarking
- Capacity planning review
- Documentation updates

---

## Key Metrics Explained (Layman Terms)

### Response Time
**What it means:** How fast the system responds when you click something
**Good:** Less than 500 milliseconds (half a second)
**Bad:** More than 2 seconds

### Uptime
**What it means:** Percentage of time the system is working
**Good:** 99.9% (system down less than 1 hour per month)
**Bad:** Less than 95%

### Error Rate
**What it means:** How many operations fail out of 100
**Good:** Less than 0.1% (1 error per 1000 operations)
**Bad:** More than 1%

### Throughput
**What it means:** How many orders/payments the system can handle per minute
**Good:** 100+ operations per minute
**Bad:** Less than 10 operations per minute

---

## Support Contacts

### Technical Support
- **Email:** support@magidesk.com
- **Phone:** [Your Support Number]
- **Hours:** 24/7 for critical issues

### Escalation Path
1. **Level 1:** Check this handbook
2. **Level 2:** Contact technical support
3. **Level 3:** Escalate to development team
4. **Level 4:** Contact Google Cloud support (for infrastructure)

---

## Quick Reference

### API Health Check URLs
- UsersApi: `https://magidesk-backend-904541739138.us-central1.run.app/health`
- OrderApi: `https://magidesk-order-904541739138.northamerica-south1.run.app/health`
- PaymentApi: `https://magidesk-payment-904541739138.northamerica-south1.run.app/health`
- MenuApi: `https://magidesk-menu-904541739138.northamerica-south1.run.app/health`
- InventoryApi: `https://magidesk-inventory-904541739138.northamerica-south1.run.app/health`
- TablesApi: `https://magidesk-tables-904541739138.northamerica-south1.run.app/health`
- SettingsApi: `https://magidesk-settings-904541739138.northamerica-south1.run.app/health`

### Database Connection
- **Instance:** `bola8pos:northamerica-south1:pos-app-1`
- **Database:** `postgres`
- **Region:** `northamerica-south1`

### Common PowerShell Commands
```powershell
# Health check
.\test-health-endpoints.ps1

# Database test
.\test-db-connection.ps1

# Payment flow test
.\test-payment-flow.ps1

# View logs
Get-Content logs\*.log -Tail 50
```

---

**Last Updated:** 2025-01-27  
**Version:** 1.0  
**Maintained By:** Operations Team
