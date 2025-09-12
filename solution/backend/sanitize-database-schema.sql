-- Comprehensive Database Schema Sanitization Script
-- This script ensures all tables are properly migrated to UUID schema

-- First, let's check the current state of all relevant tables
SELECT 
    table_schema,
    table_name,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns 
WHERE table_schema IN ('public', 'ord', 'pay', 'menu')
  AND column_name IN ('session_id', 'billing_id', 'payment_id')
ORDER BY table_schema, table_name, column_name;

-- ============================================================================
-- 1. SANITIZE PUBLIC SCHEMA TABLES
-- ============================================================================

-- Fix table_sessions table
DO $$ 
BEGIN
    RAISE NOTICE 'Sanitizing public.table_sessions...';
    
    -- Convert session_id to UUID if it's still text
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
          AND table_name = 'table_sessions' 
          AND column_name = 'session_id' 
          AND data_type = 'text'
    ) THEN
        RAISE NOTICE 'Converting table_sessions.session_id from TEXT to UUID...';
        
        -- Clean invalid data first
        UPDATE public.table_sessions 
        SET session_id = NULL 
        WHERE session_id IS NULL 
           OR session_id = '' 
           OR session_id !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
        
        -- Convert valid UUID strings to UUID type
        UPDATE public.table_sessions 
        SET session_id = session_id::uuid 
        WHERE session_id IS NOT NULL 
          AND session_id ~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
        
        -- Alter column type
        ALTER TABLE public.table_sessions ALTER COLUMN session_id TYPE uuid USING session_id::uuid;
        
        RAISE NOTICE 'table_sessions.session_id conversion completed';
    END IF;
    
    -- Convert billing_id to UUID if it's still text
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
          AND table_name = 'table_sessions' 
          AND column_name = 'billing_id' 
          AND data_type = 'text'
    ) THEN
        RAISE NOTICE 'Converting table_sessions.billing_id from TEXT to UUID...';
        
        -- Clean invalid data first
        UPDATE public.table_sessions 
        SET billing_id = NULL 
        WHERE billing_id IS NULL 
           OR billing_id = '' 
           OR billing_id !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
        
        -- Convert valid UUID strings to UUID type
        UPDATE public.table_sessions 
        SET billing_id = billing_id::uuid 
        WHERE billing_id IS NOT NULL 
          AND billing_id ~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
        
        -- Alter column type
        ALTER TABLE public.table_sessions ALTER COLUMN billing_id TYPE uuid USING billing_id::uuid;
        
        RAISE NOTICE 'table_sessions.billing_id conversion completed';
    END IF;
END $$;

-- Fix bills table
DO $$ 
BEGIN
    RAISE NOTICE 'Sanitizing public.bills...';
    
    -- Convert session_id to UUID if it's still text
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
          AND table_name = 'bills' 
          AND column_name = 'session_id' 
          AND data_type = 'text'
    ) THEN
        RAISE NOTICE 'Converting bills.session_id from TEXT to UUID...';
        
        -- Clean invalid data first
        UPDATE public.bills 
        SET session_id = NULL 
        WHERE session_id IS NULL 
           OR session_id = '' 
           OR session_id !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
        
        -- Convert valid UUID strings to UUID type
        UPDATE public.bills 
        SET session_id = session_id::uuid 
        WHERE session_id IS NOT NULL 
          AND session_id ~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
        
        -- Alter column type
        ALTER TABLE public.bills ALTER COLUMN session_id TYPE uuid USING session_id::uuid;
        
        RAISE NOTICE 'bills.session_id conversion completed';
    END IF;
    
    -- Convert billing_id to UUID if it's still text
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
          AND table_name = 'bills' 
          AND column_name = 'billing_id' 
          AND data_type = 'text'
    ) THEN
        RAISE NOTICE 'Converting bills.billing_id from TEXT to UUID...';
        
        -- Clean invalid data first
        UPDATE public.bills 
        SET billing_id = NULL 
        WHERE billing_id IS NULL 
           OR billing_id = '' 
           OR billing_id !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
        
        -- Convert valid UUID strings to UUID type
        UPDATE public.bills 
        SET billing_id = billing_id::uuid 
        WHERE billing_id IS NOT NULL 
          AND billing_id ~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
        
        -- Alter column type
        ALTER TABLE public.bills ALTER COLUMN billing_id TYPE uuid USING billing_id::uuid;
        
        RAISE NOTICE 'bills.billing_id conversion completed';
    END IF;
END $$;

-- ============================================================================
-- 2. SANITIZE ORDER SCHEMA TABLES
-- ============================================================================

-- Fix ord.orders table
DO $$ 
BEGIN
    RAISE NOTICE 'Sanitizing ord.orders...';
    
    -- Convert session_id to UUID if it's still text
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'ord' 
          AND table_name = 'orders' 
          AND column_name = 'session_id' 
          AND data_type = 'text'
    ) THEN
        RAISE NOTICE 'Converting ord.orders.session_id from TEXT to UUID...';
        
        -- Clean invalid data first
        UPDATE ord.orders 
        SET session_id = NULL 
        WHERE session_id IS NULL 
           OR session_id = '' 
           OR session_id !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
        
        -- Convert valid UUID strings to UUID type
        UPDATE ord.orders 
        SET session_id = session_id::uuid 
        WHERE session_id IS NOT NULL 
          AND session_id ~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
        
        -- Alter column type
        ALTER TABLE ord.orders ALTER COLUMN session_id TYPE uuid USING session_id::uuid;
        
        RAISE NOTICE 'ord.orders.session_id conversion completed';
    END IF;
    
    -- Convert billing_id to UUID if it's still text
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'ord' 
          AND table_name = 'orders' 
          AND column_name = 'billing_id' 
          AND data_type = 'text'
    ) THEN
        RAISE NOTICE 'Converting ord.orders.billing_id from TEXT to UUID...';
        
        -- Clean invalid data first
        UPDATE ord.orders 
        SET billing_id = NULL 
        WHERE billing_id IS NULL 
           OR billing_id = '' 
           OR billing_id !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
        
        -- Convert valid UUID strings to UUID type
        UPDATE ord.orders 
        SET billing_id = billing_id::uuid 
        WHERE billing_id IS NOT NULL 
          AND billing_id ~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
        
        -- Alter column type
        ALTER TABLE ord.orders ALTER COLUMN billing_id TYPE uuid USING billing_id::uuid;
        
        RAISE NOTICE 'ord.orders.billing_id conversion completed';
    END IF;
END $$;

-- ============================================================================
-- 3. SANITIZE PAYMENT SCHEMA TABLES
-- ============================================================================

-- Fix pay.payments table
DO $$ 
BEGIN
    RAISE NOTICE 'Sanitizing pay.payments...';
    
    -- Convert payment_id to UUID if it's still text
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'pay' 
          AND table_name = 'payments' 
          AND column_name = 'payment_id' 
          AND data_type = 'text'
    ) THEN
        RAISE NOTICE 'Converting pay.payments.payment_id from TEXT to UUID...';
        
        -- Clean invalid data first
        UPDATE pay.payments 
        SET payment_id = NULL 
        WHERE payment_id IS NULL 
           OR payment_id = '' 
           OR payment_id !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
        
        -- Convert valid UUID strings to UUID type
        UPDATE pay.payments 
        SET payment_id = payment_id::uuid 
        WHERE payment_id IS NOT NULL 
          AND payment_id ~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
        
        -- Alter column type
        ALTER TABLE pay.payments ALTER COLUMN payment_id TYPE uuid USING payment_id::uuid;
        
        RAISE NOTICE 'pay.payments.payment_id conversion completed';
    END IF;
    
    -- Convert session_id to UUID if it's still text
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'pay' 
          AND table_name = 'payments' 
          AND column_name = 'session_id' 
          AND data_type = 'text'
    ) THEN
        RAISE NOTICE 'Converting pay.payments.session_id from TEXT to UUID...';
        
        -- Clean invalid data first
        UPDATE pay.payments 
        SET session_id = NULL 
        WHERE session_id IS NULL 
           OR session_id = '' 
           OR session_id !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
        
        -- Convert valid UUID strings to UUID type
        UPDATE pay.payments 
        SET session_id = session_id::uuid 
        WHERE session_id IS NOT NULL 
          AND session_id ~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
        
        -- Alter column type
        ALTER TABLE pay.payments ALTER COLUMN session_id TYPE uuid USING session_id::uuid;
        
        RAISE NOTICE 'pay.payments.session_id conversion completed';
    END IF;
    
    -- Convert billing_id to UUID if it's still text
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'pay' 
          AND table_name = 'payments' 
          AND column_name = 'billing_id' 
          AND data_type = 'text'
    ) THEN
        RAISE NOTICE 'Converting pay.payments.billing_id from TEXT to UUID...';
        
        -- Clean invalid data first
        UPDATE pay.payments 
        SET billing_id = NULL 
        WHERE billing_id IS NULL 
           OR billing_id = '' 
           OR billing_id !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
        
        -- Convert valid UUID strings to UUID type
        UPDATE pay.payments 
        SET billing_id = billing_id::uuid 
        WHERE billing_id IS NOT NULL 
          AND billing_id ~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
        
        -- Alter column type
        ALTER TABLE pay.payments ALTER COLUMN billing_id TYPE uuid USING billing_id::uuid;
        
        RAISE NOTICE 'pay.payments.billing_id conversion completed';
    END IF;
END $$;

-- Fix pay.bill_ledger table
DO $$ 
BEGIN
    RAISE NOTICE 'Sanitizing pay.bill_ledger...';
    
    -- Convert billing_id to UUID if it's still text
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'pay' 
          AND table_name = 'bill_ledger' 
          AND column_name = 'billing_id' 
          AND data_type = 'text'
    ) THEN
        RAISE NOTICE 'Converting pay.bill_ledger.billing_id from TEXT to UUID...';
        
        -- Clean invalid data first
        UPDATE pay.bill_ledger 
        SET billing_id = NULL 
        WHERE billing_id IS NULL 
           OR billing_id = '' 
           OR billing_id !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
        
        -- Convert valid UUID strings to UUID type
        UPDATE pay.bill_ledger 
        SET billing_id = billing_id::uuid 
        WHERE billing_id IS NOT NULL 
          AND billing_id ~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
        
        -- Alter column type
        ALTER TABLE pay.bill_ledger ALTER COLUMN billing_id TYPE uuid USING billing_id::uuid;
        
        RAISE NOTICE 'pay.bill_ledger.billing_id conversion completed';
    END IF;
    
    -- Convert session_id to UUID if it's still text
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'pay' 
          AND table_name = 'bill_ledger' 
          AND column_name = 'session_id' 
          AND data_type = 'text'
    ) THEN
        RAISE NOTICE 'Converting pay.bill_ledger.session_id from TEXT to UUID...';
        
        -- Clean invalid data first
        UPDATE pay.bill_ledger 
        SET session_id = NULL 
        WHERE session_id IS NULL 
           OR session_id = '' 
           OR session_id !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
        
        -- Convert valid UUID strings to UUID type
        UPDATE pay.bill_ledger 
        SET session_id = session_id::uuid 
        WHERE session_id IS NOT NULL 
          AND session_id ~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
        
        -- Alter column type
        ALTER TABLE pay.bill_ledger ALTER COLUMN session_id TYPE uuid USING session_id::uuid;
        
        RAISE NOTICE 'pay.bill_ledger.session_id conversion completed';
    END IF;
END $$;

-- ============================================================================
-- 4. CLEAN UP INVALID DATA
-- ============================================================================

-- Remove any orphaned records with invalid UUIDs
DO $$ 
BEGIN
    RAISE NOTICE 'Cleaning up invalid data...';
    
    -- Clean up bills with invalid session_id or billing_id
    DELETE FROM public.bills 
    WHERE session_id IS NOT NULL 
      AND session_id::text !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
    
    DELETE FROM public.bills 
    WHERE billing_id IS NOT NULL 
      AND billing_id::text !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
    
    -- Clean up table_sessions with invalid session_id or billing_id
    DELETE FROM public.table_sessions 
    WHERE session_id IS NOT NULL 
      AND session_id::text !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
    
    DELETE FROM public.table_sessions 
    WHERE billing_id IS NOT NULL 
      AND billing_id::text !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
    
    -- Clean up orders with invalid session_id or billing_id
    DELETE FROM ord.orders 
    WHERE session_id IS NOT NULL 
      AND session_id::text !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
    
    DELETE FROM ord.orders 
    WHERE billing_id IS NOT NULL 
      AND billing_id::text !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
    
    -- Clean up payments with invalid IDs
    DELETE FROM pay.payments 
    WHERE payment_id IS NOT NULL 
      AND payment_id::text !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
    
    DELETE FROM pay.payments 
    WHERE session_id IS NOT NULL 
      AND session_id::text !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
    
    DELETE FROM pay.payments 
    WHERE billing_id IS NOT NULL 
      AND billing_id::text !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
    
    -- Clean up bill_ledger with invalid IDs
    DELETE FROM pay.bill_ledger 
    WHERE billing_id IS NOT NULL 
      AND billing_id::text !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
    
    DELETE FROM pay.bill_ledger 
    WHERE session_id IS NOT NULL 
      AND session_id::text !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
    
    RAISE NOTICE 'Invalid data cleanup completed';
END $$;

-- ============================================================================
-- 5. VERIFY FINAL SCHEMA
-- ============================================================================

-- Final verification of all schema changes
SELECT 
    table_schema,
    table_name,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns 
WHERE table_schema IN ('public', 'ord', 'pay', 'menu')
  AND column_name IN ('session_id', 'billing_id', 'payment_id')
ORDER BY table_schema, table_name, column_name;

-- Show data counts after cleanup
SELECT 'public.bills' as table_name, COUNT(*) as record_count FROM public.bills
UNION ALL
SELECT 'public.table_sessions', COUNT(*) FROM public.table_sessions
UNION ALL
SELECT 'ord.orders', COUNT(*) FROM ord.orders
UNION ALL
SELECT 'pay.payments', COUNT(*) FROM pay.payments
UNION ALL
SELECT 'pay.bill_ledger', COUNT(*) FROM pay.bill_ledger;

RAISE NOTICE 'Database schema sanitization completed successfully!';
