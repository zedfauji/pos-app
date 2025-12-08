-- Clean All Tables Script
-- This script will clean all billing, session, and payment related tables
-- Run this block by block in Cloud SQL Studio

-- Block 1: Clean payment related tables
DO $$ 
BEGIN
    -- Clean payment logs
    DELETE FROM pay.payment_logs;
    RAISE NOTICE 'Cleaned pay.payment_logs';
    
    -- Clean bill ledger
    DELETE FROM pay.bill_ledger;
    RAISE NOTICE 'Cleaned pay.bill_ledger';
    
    -- Clean payments
    DELETE FROM pay.payments;
    RAISE NOTICE 'Cleaned pay.payments';
END $$;

-- Block 2: Clean order related tables
DO $$ 
BEGIN
    -- Clean order items
    DELETE FROM ord.order_items;
    RAISE NOTICE 'Cleaned ord.order_items';
    
    -- Clean orders
    DELETE FROM ord.orders;
    RAISE NOTICE 'Cleaned ord.orders';
END $$;

-- Block 3: Clean billing and session tables
DO $$ 
BEGIN
    -- Clean bills
    DELETE FROM public.bills;
    RAISE NOTICE 'Cleaned public.bills';
    
    -- Clean table sessions
    DELETE FROM public.table_sessions;
    RAISE NOTICE 'Cleaned public.table_sessions';
END $$;

-- Block 4: Reset table status to available
DO $$ 
BEGIN
    -- Reset all tables to available status
    UPDATE public.table_status 
    SET 
        occupied = false,
        order_id = NULL,
        start_time = NULL,
        server = NULL
    WHERE occupied = true;
    RAISE NOTICE 'Reset all tables to available status';
END $$;

-- Block 5: Verification queries
-- Run these to verify cleanup
SELECT 'pay.payment_logs' as table_name, COUNT(*) as remaining_records FROM pay.payment_logs
UNION ALL
SELECT 'pay.bill_ledger' as table_name, COUNT(*) as remaining_records FROM pay.bill_ledger
UNION ALL
SELECT 'pay.payments' as table_name, COUNT(*) as remaining_records FROM pay.payments
UNION ALL
SELECT 'ord.order_items' as table_name, COUNT(*) as remaining_records FROM ord.order_items
UNION ALL
SELECT 'ord.orders' as table_name, COUNT(*) as remaining_records FROM ord.orders
UNION ALL
SELECT 'public.bills' as table_name, COUNT(*) as remaining_records FROM public.bills
UNION ALL
SELECT 'public.table_sessions' as table_name, COUNT(*) as remaining_records FROM public.table_sessions;

-- Block 6: Check table status
SELECT 
    table_label,
    type,
    occupied,
    order_id,
    start_time,
    server
FROM public.table_status 
ORDER BY table_label;
