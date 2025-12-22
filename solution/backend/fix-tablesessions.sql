-- Fix TableSessions Schema to match TableRepository.cs expectations (UUIDs, Dapper)
-- This replaces the incompatible integer-based schema.

DROP TABLE IF EXISTS "TableSessions" CASCADE;

CREATE TABLE "TableSessions" (
    session_id UUID PRIMARY KEY,         -- Matches: Guid sessionId
    table_label TEXT NOT NULL,           -- Matches: string tableLabel ('Table 1')
    server_id TEXT,                      -- Matches: string serverId
    server_name TEXT,                    -- Matches: string serverName
    start_time TIMESTAMPTZ NOT NULL DEFAULT NOW(), -- Matches: DateTime start_time
    end_time TIMESTAMPTZ,                -- Matches: DateTime end_time
    status TEXT NOT NULL DEFAULT 'active', -- Matches: string status ('active', 'ended')
    billing_id UUID,                     -- Matches: Guid billingId
    items JSONB,                         -- Matches: session.items (Legacy JSON support)
    shift_id UUID REFERENCES shifts(shift_id) -- Link to GAP-03 shift
);

-- Index for performance
CREATE INDEX idx_tablesessions_status ON "TableSessions"(status);
CREATE INDEX idx_tablesessions_label ON "TableSessions"(table_label);
