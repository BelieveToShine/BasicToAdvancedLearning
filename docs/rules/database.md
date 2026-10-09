# Rule: Database & scripts

No database exists yet — it arrives in Phase 2 (topics 10–12) and Phase 3 (ADO.NET / EF Core). These
rules apply from the first DB topic; extend them as real conventions emerge.

## Read — allowed
- Read-only access to verify data (inspect rows, confirm a script's effect). Querying to validate is fine.

## Write / scripts — never on your own
- Never execute DDL/UPDATE/INSERT/DELETE or seed scripts. Execution is manual, by a human.
- Deliver every change as a reviewed script (schema/one-off under a `sql/` or `scripts/` folder). Hand it
  over; don't apply it. Prefer local emulators/dev DB for testing.
- TODO: pick the folder (`sql/` vs `scripts/`) and the DB engine when topic 10 starts.

## Writing data (grows over time)
- If the context defaults to no-tracking, read with tracking before saving an update, or it won't persist.
- "One active per key" behind a filtered unique index: deactivate-then-insert as two ordered commits,
  never one — batching can insert before the deactivate and trip the index.
- Check-then-act inserts need a DB unique constraint as a backstop (overlapping jobs can race).
- Beware provider quirks translating a parameterized `Contains(list)` — confirm the generated SQL.
