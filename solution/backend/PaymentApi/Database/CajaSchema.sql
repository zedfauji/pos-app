-- ============================================
-- Caja System Database Schema
-- ============================================

-- Create caja schema
CREATE SCHEMA IF NOT EXISTS caja;

-- Create caja_sessions table
CREATE TABLE IF NOT EXISTS caja.caja_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    opened_by_user_id TEXT NOT NULL,
    closed_by_user_id TEXT NULL,
    opened_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    closed_at TIMESTAMPTZ NULL,
    opening_amount NUMERIC(10,2) NOT NULL CHECK (opening_amount >= 0),
    closing_amount NUMERIC(10,2) NULL CHECK (closing_amount IS NULL OR closing_amount >= 0),
    system_calculated_total NUMERIC(10,2) NULL,
    difference NUMERIC(10,2) NULL,
    status TEXT NOT NULL CHECK (status IN ('open', 'closed')) DEFAULT 'open',
    notes TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Create caja_transactions table
CREATE TABLE IF NOT EXISTS caja.caja_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    caja_session_id UUID NOT NULL REFERENCES caja.caja_sessions(id) ON DELETE CASCADE,
    transaction_id UUID NOT NULL,
    transaction_type TEXT NOT NULL CHECK (transaction_type IN ('sale', 'refund', 'tip', 'deposit', 'withdrawal')),
    amount NUMERIC(10,2) NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Create indexes for caja_sessions
CREATE INDEX IF NOT EXISTS idx_caja_sessions_status ON caja.caja_sessions(status);
CREATE INDEX IF NOT EXISTS idx_caja_sessions_opened_at ON caja.caja_sessions(opened_at DESC);
CREATE UNIQUE INDEX IF NOT EXISTS idx_caja_sessions_active 
ON caja.caja_sessions(status) 
WHERE status = 'open';
CREATE INDEX IF NOT EXISTS idx_caja_sessions_opened_by ON caja.caja_sessions(opened_by_user_id);
CREATE INDEX IF NOT EXISTS idx_caja_sessions_closed_by ON caja.caja_sessions(closed_by_user_id);

-- Create indexes for caja_transactions
CREATE INDEX IF NOT EXISTS idx_caja_transactions_session ON caja.caja_transactions(caja_session_id);
CREATE INDEX IF NOT EXISTS idx_caja_transactions_type ON caja.caja_transactions(transaction_type);
CREATE INDEX IF NOT EXISTS idx_caja_transactions_timestamp ON caja.caja_transactions(timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_caja_transactions_session_type ON caja.caja_transactions(caja_session_id, transaction_type);

-- Modify pay.payments table
ALTER TABLE pay.payments 
ADD COLUMN IF NOT EXISTS caja_session_id UUID NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'fk_payments_caja_session'
    ) THEN
        ALTER TABLE pay.payments
        ADD CONSTRAINT fk_payments_caja_session 
        FOREIGN KEY (caja_session_id) 
        REFERENCES caja.caja_sessions(id) 
        ON DELETE SET NULL;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_payments_caja_session ON pay.payments(caja_session_id);

-- Modify pay.refunds table
ALTER TABLE pay.refunds 
ADD COLUMN IF NOT EXISTS caja_session_id UUID NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'fk_refunds_caja_session'
    ) THEN
        ALTER TABLE pay.refunds
        ADD CONSTRAINT fk_refunds_caja_session 
        FOREIGN KEY (caja_session_id) 
        REFERENCES caja.caja_sessions(id) 
        ON DELETE SET NULL;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_refunds_caja_session ON pay.refunds(caja_session_id);

-- Add comments
COMMENT ON SCHEMA caja IS 'Caja (Cash Register) System Schema';
COMMENT ON TABLE caja.caja_sessions IS 'Stores caja session records (open/close cycles)';
COMMENT ON TABLE caja.caja_transactions IS 'Stores all transactions linked to a caja session';
COMMENT ON COLUMN pay.payments.caja_session_id IS 'Links payment to caja session';
COMMENT ON COLUMN pay.refunds.caja_session_id IS 'Links refund to caja session';
