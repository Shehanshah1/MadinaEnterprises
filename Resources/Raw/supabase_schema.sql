-- ============================================================
-- Madina Enterprises — Supabase schema
-- Run this once in the Supabase SQL Editor after creating your
-- project. It creates the 5 tables the app syncs with plus the
-- row-level-security policies that let the anon API key used by
-- the desktop app read and write.
-- ============================================================

CREATE TABLE IF NOT EXISTS ginners (
    ginner_id       TEXT PRIMARY KEY,
    ginner_name     TEXT,
    contact         TEXT,
    iban            TEXT,
    address         TEXT,
    ntn             TEXT,
    stn             TEXT,
    bank_address    TEXT,
    contact_person  TEXT,
    station         TEXT,
    updated_at      TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS mills (
    mill_id     TEXT PRIMARY KEY,
    mill_name   TEXT,
    address     TEXT,
    owner_name  TEXT,
    updated_at  TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS contracts (
    contract_id            TEXT PRIMARY KEY,
    ginner_id              TEXT,
    mill_id                TEXT,
    total_bales            INTEGER,
    price_per_batch        DOUBLE PRECISION,
    total_amount           DOUBLE PRECISION,
    commission_percentage  DOUBLE PRECISION,
    date_created           TEXT,
    delivery_notes         TEXT,
    payment_notes          TEXT,
    description            TEXT,
    updated_at             TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS deliveries (
    delivery_id     TEXT PRIMARY KEY,
    contract_id     TEXT,
    amount          DOUBLE PRECISION,
    total_bales     INTEGER,
    factory_weight  DOUBLE PRECISION,
    mill_weight     DOUBLE PRECISION,
    truck_number    TEXT,
    driver_contact  TEXT,
    departure_date  TEXT,
    delivery_date   TEXT,
    updated_at      TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS payments (
    payment_id      TEXT PRIMARY KEY,
    contract_id     TEXT,
    total_amount    DOUBLE PRECISION,
    amount_paid     DOUBLE PRECISION,
    total_bales     INTEGER,
    date            TEXT,
    transaction_id  TEXT,
    updated_at      TIMESTAMPTZ DEFAULT NOW()
);

-- ------------------------------------------------------------
-- Row Level Security
-- ------------------------------------------------------------
-- Enable RLS and create permissive policies for the anon role.
-- Because this is a small private business app using a single
-- hardcoded login, we allow full access with the anon key.
-- If you later add Supabase Auth, replace these with stricter
-- auth.uid()-based policies.
-- ------------------------------------------------------------

ALTER TABLE ginners    ENABLE ROW LEVEL SECURITY;
ALTER TABLE mills      ENABLE ROW LEVEL SECURITY;
ALTER TABLE contracts  ENABLE ROW LEVEL SECURITY;
ALTER TABLE deliveries ENABLE ROW LEVEL SECURITY;
ALTER TABLE payments   ENABLE ROW LEVEL SECURITY;

DO $$
DECLARE
    tbl TEXT;
BEGIN
    FOREACH tbl IN ARRAY ARRAY['ginners','mills','contracts','deliveries','payments']
    LOOP
        EXECUTE format('DROP POLICY IF EXISTS "madina_all_%I" ON %I;', tbl, tbl);
        EXECUTE format(
            'CREATE POLICY "madina_all_%I" ON %I FOR ALL USING (true) WITH CHECK (true);',
            tbl, tbl);
    END LOOP;
END $$;
