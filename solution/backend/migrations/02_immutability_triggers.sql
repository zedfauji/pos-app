-- Migration: 02_immutability_triggers.sql
-- Purpose: Enforce strict NO UPDATE / NO DELETE on financial tables

-- Generic function to block mutations
CREATE OR REPLACE FUNCTION public.enforce_immutability()
RETURNS TRIGGER AS $$
BEGIN
    RAISE EXCEPTION 'Table % is immutable. UPDATE and DELETE are strictly forbidden.', TG_TABLE_NAME;
END;
$$ LANGUAGE plpgsql;

-- Apply to Order Schema
CREATE TRIGGER trg_immutable_orders
BEFORE UPDATE OR DELETE ON ord.orders
FOR EACH ROW EXECUTE FUNCTION public.enforce_immutability();

CREATE TRIGGER trg_immutable_order_items
BEFORE UPDATE OR DELETE ON ord.order_items
FOR EACH ROW EXECUTE FUNCTION public.enforce_immutability();

-- Apply to Payment Schema
CREATE TRIGGER trg_immutable_payments
BEFORE UPDATE OR DELETE ON pay.payments
FOR EACH ROW EXECUTE FUNCTION public.enforce_immutability();

CREATE TRIGGER trg_immutable_ledger
BEFORE UPDATE OR DELETE ON pay.payment_ledger
FOR EACH ROW EXECUTE FUNCTION public.enforce_immutability();

-- Apply to Billing Schema
CREATE TRIGGER trg_immutable_bills
BEFORE UPDATE OR DELETE ON billing.bills
FOR EACH ROW EXECUTE FUNCTION public.enforce_immutability();

CREATE TRIGGER trg_immutable_bill_items
BEFORE UPDATE OR DELETE ON billing.bill_items
FOR EACH ROW EXECUTE FUNCTION public.enforce_immutability();

-- Apply to Shifts
CREATE TRIGGER trg_immutable_shifts
BEFORE UPDATE OR DELETE ON public.shifts
FOR EACH ROW EXECUTE FUNCTION public.enforce_immutability();
