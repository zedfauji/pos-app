-- Migration Script for Shift Controller
-- 1. Drop existing incompatible shifts table
DROP TABLE IF EXISTS shifts CASCADE;

-- 2. Create shifts table (Adapted for Integer User IDs)
CREATE TABLE shifts (
    shift_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    shift_number SERIAL NOT NULL,
    opened_by_user_id INTEGER NOT NULL REFERENCES "Users"("Id"),
    opened_by_name VARCHAR(100),
    opened_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    starting_cash DECIMAL(10,2) NOT NULL,
    closed_by_user_id INTEGER,
    closed_by_name VARCHAR(100),
    closed_at TIMESTAMPTZ,
    declared_cash DECIMAL(10,2),
    expected_cash DECIMAL(10,2),
    difference DECIMAL(10,2),
    difference_category VARCHAR(50),
    close_reason TEXT,
    status VARCHAR(20) NOT NULL DEFAULT 'open',
    idempotency_key UUID,
    UNIQUE(shift_number)
);

CREATE INDEX idx_shifts_status ON shifts(status);
CREATE INDEX idx_shifts_opened_at ON shifts(opened_at);

-- 3. Add shift_id to existing tables
ALTER TABLE "TableSessions" ADD COLUMN IF NOT EXISTS shift_id UUID REFERENCES shifts(shift_id);
ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS shift_id UUID REFERENCES shifts(shift_id);

-- 4. Create bills table (Essential for payment tracking)
-- Assuming 1 Bill per TableSession for now, but separating allows for future splitting/merging
CREATE TABLE IF NOT EXISTS bills (
    bill_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    shift_id UUID NOT NULL REFERENCES shifts(shift_id),
    table_session_id INTEGER NOT NULL REFERENCES "TableSessions"("Id"),
    total_amount DECIMAL(10,2) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'unsettled', -- unsettled, settled, voided
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by_user_id INTEGER REFERENCES "Users"("Id")
);

CREATE INDEX idx_bills_shift_id ON bills(shift_id);
CREATE INDEX idx_bills_table_session_id ON bills(table_session_id);

-- 5. Create payments table
CREATE TABLE IF NOT EXISTS payments (
    payment_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    shift_id UUID NOT NULL REFERENCES shifts(shift_id),
    bill_id UUID NOT NULL REFERENCES bills(bill_id),
    amount_paid DECIMAL(10,2) NOT NULL,
    payment_method VARCHAR(50) NOT NULL, -- cash, card
    is_voided BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by_user_id INTEGER REFERENCES "Users"("Id")
);

CREATE INDEX idx_payments_shift_id ON payments(shift_id);
CREATE INDEX idx_payments_bill_id ON payments(bill_id);
