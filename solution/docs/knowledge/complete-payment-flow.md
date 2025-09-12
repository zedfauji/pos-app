# Complete Payment Flow Documentation

## Overview
This document traces the complete flow from table start to payment settlement using real database operations via MCP tool. The flow demonstrates how data moves through multiple schemas and tables in the MagiDesk POS system.

## Database Schema Overview

### Core Tables by Schema:

**`public` Schema:**
- `table_status` - Current state of all tables
- `table_sessions` - Active/inactive table sessions
- `bills` - Generated bills awaiting or completed payment
- `table_session_moves` - Table transfer history

**`ord` Schema (Orders):**
- `orders` - Order records linked to sessions
- `order_items` - Individual items within orders
- `order_logs` - Order activity logs

**`pay` Schema (Payments):**
- `payments` - Payment records with method and amounts
- `payment_logs` - Payment activity audit trail
- `bill_ledger` - Financial ledger for billing tracking

## Complete Flow Trace

### Session Details
- **Table**: Billiard 3
- **Server**: PREETI
- **Session ID**: `a4ada3a6-8dc3-4661-8cf2-836ce1342805`
- **Billing ID**: `ae4f5f20-3c7b-4ab2-9d10-3f3b02ff0b2d`
- **Payment ID**: `39`

---

## Step 1: Server Starts Table
`
### Database Changes:
1. **Create Session Record** (`public.table_sessions`):
   ```sql
   INSERT INTO public.table_sessions (
     session_id, table_label, server_id, server_name, 
     start_time, status, items, billing_id, last_heartbeat
   )
   VALUES (
     'a4ada3a6-8dc3-4661-8cf2-836ce1342805'::uuid,
     'Billiard 3', 'PREETI', 'PREETI',
     '2025-09-12 19:20:20.405814+00'::timestamptz,
     'active', '[]'::jsonb, 
     'ae4f5f20-3c7b-4ab2-9d10-3f3b02ff0b2d'::uuid,
     '2025-09-12 19:20:20.405814+00'::timestamptz
   );
   ```

2. **Update Table Status** (`public.table_status`):
   ```sql
   UPDATE public.table_status 
   SET occupied = true, start_time = now(), server = 'PREETI', updated_at = now()
   WHERE label = 'Billiard 3';
   ```

### Result:
- Table marked as occupied
- Session created with `active` status
- Billing ID generated for future payment tracking

---

## Step 2: Add Items to Table

### Database Changes:
1. **Create Order** (`ord.orders`):
   ```sql
   INSERT INTO ord.orders (
     session_id, billing_id, table_id, server_id, server_name,
     status, subtotal, total, delivery_status
   )
   VALUES (
     'a4ada3a6-8dc3-4661-8cf2-836ce1342805'::uuid,
     'ae4f5f20-3c7b-4ab2-9d10-3f3b02ff0b2d'::uuid,
     'Billiard 3', 'PREETI', 'PREETI',
     'open', 0.00, 0.00, 'pending'
   );
   ```

2. **Add Order Items** (`ord.order_items`):
   ```sql
   -- Premium Beer (2x $5.50 = $11.00)
   INSERT INTO ord.order_items (
     order_id, quantity, base_price, vendor_price, 
     line_total, profit, snapshot_name, snapshot_sku, snapshot_category
   ) VALUES (29, 2, 5.50, 3.50, 11.00, 4.00, 'Premium Beer', 'BEER001', 'Beverages');

   -- Margherita Pizza (1x $12.99 = $12.99)
   INSERT INTO ord.order_items (
     order_id, quantity, base_price, vendor_price, 
     line_total, profit, snapshot_name, snapshot_sku, snapshot_category
   ) VALUES (29, 1, 12.99, 8.99, 12.99, 4.00, 'Margherita Pizza', 'PIZZA001', 'Food');

   -- Nachos (1x $8.50 = $8.50)
   INSERT INTO ord.order_items (
     order_id, quantity, base_price, vendor_price, 
     line_total, profit, snapshot_name, snapshot_sku, snapshot_category
   ) VALUES (29, 1, 8.50, 5.50, 8.50, 3.00, 'Nachos', 'SNACK001', 'Snacks');
   ```

3. **Update Order Totals**:
   ```sql
   UPDATE ord.orders 
   SET subtotal = 32.49, total = 32.49, updated_at = now()
   WHERE order_id = 29;
   ```

4. **Update Session Items** (`public.table_sessions`):
   ```sql
   UPDATE public.table_sessions 
   SET items = '[
     {"name": "Premium Beer", "quantity": 2, "price": 5.50, "total": 11.00},
     {"name": "Margherita Pizza", "quantity": 1, "price": 12.99, "total": 12.99},
     {"name": "Nachos", "quantity": 1, "price": 8.50, "total": 8.50}
   ]'::jsonb
   WHERE session_id = 'a4ada3a6-8dc3-4661-8cf2-836ce1342805'::uuid;
   ```

### Result:
- Order created with 3 items totaling $32.49
- Session updated with item details
- All items marked as `confirmed` status

---

## Step 3: Kitchen Orders Delivered

### Database Changes:
1. **Update Delivery Status** (`ord.order_items`):
   ```sql
   -- Beer: 1 out of 2 delivered (partial)
   UPDATE ord.order_items SET delivered_quantity = 1 WHERE order_item_id = 50;
   
   -- Pizza: Fully delivered
   UPDATE ord.order_items SET delivered_quantity = 1 WHERE order_item_id = 51;
   
   -- Nachos: Fully delivered
   UPDATE ord.order_items SET delivered_quantity = 1 WHERE order_item_id = 52;
   ```

2. **Update Order Delivery Status** (`ord.orders`):
   ```sql
   UPDATE ord.orders 
   SET delivery_status = 'partial', updated_at = now()
   WHERE order_id = 29;
   ```

### Result:
- 2 items fully delivered, 1 partially delivered
- Order status updated to `partial` delivery
- System tracks delivery progress for billing accuracy

---

## Step 4: Server Stops Table & Bill Generation

### Database Changes:
1. **Calculate Session Costs**:
   - Session Duration: ~47 seconds (0.64 minutes)
   - Time Cost: $1.27 (at $2/minute)
   - Items Cost: $32.49
   - **Total Amount: $33.76**

2. **Create Bill** (`public.bills`):
   ```sql
   INSERT INTO public.bills (
     bill_id, billing_id, session_id, table_label, server_id, server_name,
     start_time, end_time, total_time_minutes, items, time_cost, items_cost, 
     total_amount, status, is_settled, payment_state
   )
   VALUES (
     gen_random_uuid(),
     'ae4f5f20-3c7b-4ab2-9d10-3f3b02ff0b2d'::uuid,
     'a4ada3a6-8dc3-4661-8cf2-836ce1342805'::uuid,
     'Billiard 3', 'PREETI', 'PREETI',
     '2025-09-12 19:20:20.405814+00'::timestamptz,
     now(), 1, -- rounded minutes
     '[{"name": "Premium Beer", "quantity": 2, "price": 5.50, "total": 11.00}, ...]'::jsonb,
     1.27, 32.49, 33.76,
     'awaiting_payment', false, 'not-paid'
   );
   ```

3. **Close Session** (`public.table_sessions`):
   ```sql
   UPDATE public.table_sessions 
   SET end_time = now(), status = 'closed', last_heartbeat = now()
   WHERE session_id = 'a4ada3a6-8dc3-4661-8cf2-836ce1342805'::uuid;
   ```

4. **Free Table** (`public.table_status`):
   ```sql
   UPDATE public.table_status 
   SET occupied = false, start_time = NULL, server = NULL, updated_at = now()
   WHERE label = 'Billiard 3';
   ```

5. **Close Order** (`ord.orders`):
   ```sql
   UPDATE ord.orders 
   SET status = 'closed', closed_at = now(), updated_at = now()
   WHERE order_id = 29;
   ```

### Result:
- Bill generated for $33.76 awaiting payment
- Session closed and table freed
- Order marked as closed
- All financial calculations completed

---

## Step 5: Payment Selection on Payment Page

### Database Operations:
1. **Query Available Bills**:
   ```sql
   SELECT bill_id, billing_id, table_label, server_name, 
          total_amount, status, payment_state, created_at
   FROM public.bills 
   WHERE billing_id = 'ae4f5f20-3c7b-4ab2-9d10-3f3b02ff0b2d'::uuid;
   ```

2. **Display Bill Details**:
   - Table: Billiard 3
   - Server: PREETI
   - Total: $33.76
   - Items: 3 items (Beer, Pizza, Nachos)
   - Status: `awaiting_payment`

### Result:
- Manager can view complete bill details
- Payment page shows all transaction information
- System ready for payment processing

---

## Step 6: Payment Settlement & Payment ID Generation

### Database Changes:
1. **Create Payment Record** (`pay.payments`):
   ```sql
   INSERT INTO pay.payments (
     session_id, billing_id, amount_paid, currency, payment_method,
     discount_amount, tip_amount, external_ref, meta, created_by
   )
   VALUES (
     'a4ada3a6-8dc3-4661-8cf2-836ce1342805'::uuid,
     'ae4f5f20-3c7b-4ab2-9d10-3f3b02ff0b2d'::uuid,
     33.76, 'USD', 'cash', 0.00, 5.00, -- $5 tip
     'CASH_PAYMENT_' || EXTRACT(EPOCH FROM now())::bigint,
     '{"manager": "MANAGER001", "location": "main_bar", "receipt_printed": true}'::jsonb,
     'MANAGER001'
   );
   ```

2. **Update Bill Status** (`public.bills`):
   ```sql
   UPDATE public.bills 
   SET status = 'closed', closed_at = now(), is_settled = true, payment_state = 'paid'
   WHERE billing_id = 'ae4f5f20-3c7b-4ab2-9d10-3f3b02ff0b2d'::uuid;
   ```

3. **Create Payment Log** (`pay.payment_logs`):
   ```sql
   INSERT INTO pay.payment_logs (
     billing_id, session_id, action, old_value, new_value, server_id
   )
   VALUES (
     'ae4f5f20-3c7b-4ab2-9d10-3f3b02ff0b2d',
     'a4ada3a6-8dc3-4661-8cf2-836ce1342805',
     'payment_completed',
     '{"status": "awaiting_payment", "payment_state": "not-paid"}'::jsonb,
     '{"status": "closed", "payment_state": "paid", "amount_paid": 33.76, "tip": 5.00, "payment_method": "cash"}'::jsonb,
     'MANAGER001'
   );
   ```

4. **Update Bill Ledger** (`pay.bill_ledger`):
   ```sql
   INSERT INTO pay.bill_ledger (
     billing_id, session_id, total_due, total_discount, 
     total_paid, total_tip, status
   )
   VALUES (
     'ae4f5f20-3c7b-4ab2-9d10-3f3b02ff0b2d'::uuid,
     'a4ada3a6-8dc3-4661-8cf2-836ce1342805'::uuid,
     33.76, 0.00, 33.76, 5.00, 'paid'
   );
   ```

### Result:
- **Payment ID Generated**: `39`
- Payment recorded: $33.76 + $5.00 tip = $38.76 total
- Bill marked as settled and paid
- Complete audit trail created
- Financial ledger updated

---

## Final State Summary

### Complete Transaction Record:
- **Session ID**: `a4ada3a6-8dc3-4661-8cf2-836ce1342805`
- **Billing ID**: `ae4f5f20-3c7b-4ab2-9d10-3f3b02ff0b2d`
- **Payment ID**: `39`
- **Table**: Billiard 3 (now free)
- **Server**: PREETI
- **Duration**: ~47 seconds
- **Items**: 3 items totaling $32.49
- **Time Cost**: $1.27
- **Total Bill**: $33.76
- **Payment**: $33.76 (cash)
- **Tip**: $5.00
- **Total Paid**: $38.76

### Database State:
- ✅ Table freed and available
- ✅ Session closed
- ✅ Order closed
- ✅ Bill settled
- ✅ Payment recorded
- ✅ Audit trail complete
- ✅ Financial ledger updated

---

## Key Database Relationships

### Primary Keys & Foreign Keys:
1. **Session Flow**: `table_sessions.session_id` → `orders.session_id` → `bills.session_id` → `payments.session_id`
2. **Billing Flow**: `table_sessions.billing_id` → `orders.billing_id` → `bills.billing_id` → `payments.billing_id` → `bill_ledger.billing_id`
3. **Order Flow**: `orders.order_id` → `order_items.order_id`

### Data Consistency:
- All timestamps use `timestamptz` for timezone awareness
- Financial amounts use `numeric` type for precision
- Status fields use check constraints for valid values
- JSONB fields store flexible item and metadata information

---

## Business Logic Insights

### Time Calculation:
- Sessions tracked with precise start/end times
- Time costs calculated per minute
- Bills include both time and item costs

### Payment Processing:
- Multiple payment methods supported
- Tips tracked separately
- Discount capabilities built-in
- External reference tracking for reconciliation

### Audit Trail:
- Complete payment logs for compliance
- Bill ledger for financial reporting
- Order logs for operational tracking
- Session moves for table management

### Error Handling:
- Check constraints prevent invalid states
- Foreign key relationships maintain data integrity
- Status transitions are controlled and auditable

This complete flow demonstrates the robust, multi-schema architecture of the MagiDesk POS system, ensuring data integrity, financial accuracy, and comprehensive audit trails throughout the entire customer journey.
