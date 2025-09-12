-- Manual Database Migration Script
-- This script converts TEXT fields to UUID fields in the public.bills table

-- First, let's check the current schema
SELECT column_name, data_type, is_nullable 
FROM information_schema.columns 
WHERE table_schema = 'public' 
  AND table_name = 'bills' 
  AND column_name IN ('billing_id', 'session_id')
ORDER BY column_name;

-- Convert billing_id from TEXT to UUID
DO $$ 
BEGIN
  -- Check if billing_id column exists and is TEXT type
  IF EXISTS (
    SELECT 1 FROM information_schema.columns 
    WHERE table_schema = 'public' 
      AND table_name = 'bills' 
      AND column_name = 'billing_id' 
      AND data_type = 'text'
  ) THEN
    RAISE NOTICE 'Converting billing_id from TEXT to UUID...';
    
    -- First, update any empty or invalid values to NULL
    UPDATE public.bills 
    SET billing_id = NULL 
    WHERE billing_id IS NULL 
       OR billing_id = '' 
       OR billing_id !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
    
    -- Convert valid TEXT billing_id to UUID
    UPDATE public.bills 
    SET billing_id = billing_id::uuid 
    WHERE billing_id IS NOT NULL 
      AND billing_id ~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
    
    -- Alter column type to UUID
    ALTER TABLE public.bills ALTER COLUMN billing_id TYPE uuid USING billing_id::uuid;
    
    RAISE NOTICE 'billing_id conversion completed';
  ELSE
    RAISE NOTICE 'billing_id is already UUID type or does not exist';
  END IF;
END $$;

-- Convert session_id from TEXT to UUID
DO $$ 
BEGIN
  -- Check if session_id column exists and is TEXT type
  IF EXISTS (
    SELECT 1 FROM information_schema.columns 
    WHERE table_schema = 'public' 
      AND table_name = 'bills' 
      AND column_name = 'session_id' 
      AND data_type = 'text'
  ) THEN
    RAISE NOTICE 'Converting session_id from TEXT to UUID...';
    
    -- First, update any empty or invalid values to NULL
    UPDATE public.bills 
    SET session_id = NULL 
    WHERE session_id IS NULL 
       OR session_id = '' 
       OR session_id !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
    
    -- Convert valid TEXT session_id to UUID
    UPDATE public.bills 
    SET session_id = session_id::uuid 
    WHERE session_id IS NOT NULL 
      AND session_id ~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
    
    -- Alter column type to UUID
    ALTER TABLE public.bills ALTER COLUMN session_id TYPE uuid USING session_id::uuid;
    
    RAISE NOTICE 'session_id conversion completed';
  ELSE
    RAISE NOTICE 'session_id is already UUID type or does not exist';
  END IF;
END $$;

-- Also check and convert table_sessions table
DO $$ 
BEGIN
  -- Check if session_id column exists and is TEXT type in table_sessions
  IF EXISTS (
    SELECT 1 FROM information_schema.columns 
    WHERE table_schema = 'public' 
      AND table_name = 'table_sessions' 
      AND column_name = 'session_id' 
      AND data_type = 'text'
  ) THEN
    RAISE NOTICE 'Converting table_sessions.session_id from TEXT to UUID...';
    
    -- Convert valid TEXT session_id to UUID
    UPDATE public.table_sessions 
    SET session_id = session_id::uuid 
    WHERE session_id IS NOT NULL 
      AND session_id ~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
    
    -- Alter column type to UUID
    ALTER TABLE public.table_sessions ALTER COLUMN session_id TYPE uuid USING session_id::uuid;
    
    RAISE NOTICE 'table_sessions.session_id conversion completed';
  ELSE
    RAISE NOTICE 'table_sessions.session_id is already UUID type or does not exist';
  END IF;
END $$;

-- Check if billing_id column exists and is TEXT type in table_sessions
DO $$ 
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.columns 
    WHERE table_schema = 'public' 
      AND table_name = 'table_sessions' 
      AND column_name = 'billing_id' 
      AND data_type = 'text'
  ) THEN
    RAISE NOTICE 'Converting table_sessions.billing_id from TEXT to UUID...';
    
    -- Convert valid TEXT billing_id to UUID
    UPDATE public.table_sessions 
    SET billing_id = billing_id::uuid 
    WHERE billing_id IS NOT NULL 
      AND billing_id ~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
    
    -- Alter column type to UUID
    ALTER TABLE public.table_sessions ALTER COLUMN billing_id TYPE uuid USING billing_id::uuid;
    
    RAISE NOTICE 'table_sessions.billing_id conversion completed';
  ELSE
    RAISE NOTICE 'table_sessions.billing_id is already UUID type or does not exist';
  END IF;
END $$;

-- Verify the final schema
SELECT column_name, data_type, is_nullable 
FROM information_schema.columns 
WHERE table_schema = 'public' 
  AND table_name IN ('bills', 'table_sessions')
  AND column_name IN ('billing_id', 'session_id')
ORDER BY table_name, column_name;

RAISE NOTICE 'Database migration completed successfully!';
