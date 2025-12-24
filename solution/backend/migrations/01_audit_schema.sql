-- Migration: 01_audit_schema.sql
-- Purpose: Create audit schema and table for immutable audit logs

CREATE SCHEMA IF NOT EXISTS audit;

-- Main audit event table
CREATE TABLE IF NOT EXISTS audit.events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    timestamp TIMESTAMP WITH TIME ZONE DEFAULT NOW() NOT NULL,
    actor_id TEXT NOT NULL,         -- User/System ID
    actor_role TEXT,                -- Role at time of action
    action_type TEXT NOT NULL,      -- e.g., 'ORDER_CREATED', 'PAYMENT_RECEIVED'
    entity_type TEXT NOT NULL,      -- e.g., 'Order', 'Payment'
    entity_id TEXT NOT NULL,        -- PK of the affected entity
    correlation_id TEXT,            -- To trace across microservices
    before_state JSONB,             -- Snapshot before change (null for insert)
    after_state JSONB,              -- Snapshot after change
    source TEXT NOT NULL,           -- e.g., 'API', 'BackgroundJob'
    metadata JSONB                  -- Extra context (IP, UserAgent, etc.)
);

-- Index for fast time-range queries (e.g., "What happened today?")
CREATE INDEX IF NOT EXISTS idx_audit_events_timestamp ON audit.events(timestamp);

-- Index for entity history (e.g., "Show me history of Order X")
CREATE INDEX IF NOT EXISTS idx_audit_events_entity ON audit.events(entity_type, entity_id);

-- Index for correlation (traceability)
CREATE INDEX IF NOT EXISTS idx_audit_events_correlation ON audit.events(correlation_id);

-- Prevent Updates/Deletes on the audit log itself
CREATE OR REPLACE FUNCTION audit.prevent_audit_tampering()
RETURNS TRIGGER AS $$
BEGIN
    RAISE EXCEPTION 'Audit logs are immutable. Update/Delete not allowed.';
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_protect_audit_log
BEFORE UPDATE OR DELETE ON audit.events
FOR EACH ROW EXECUTE FUNCTION audit.prevent_audit_tampering();
