# Rule: Database & scripts

The database arrives in Phase 2 (topics 10–12) and Phase 3 (ADO.NET / EF Core). These
rules apply from the first DB topic; extend them as real conventions emerge.

## Engine — Microsoft SQL Server only
- **SQL Server is the only engine** (decision D8). SQL lessons are T-SQL; C# data access uses
  `Microsoft.Data.SqlClient` (ADO.NET) and `Microsoft.EntityFrameworkCore.SqlServer` (EF Core). No SQLite,
  PostgreSQL, MySQL or in-memory substitutes in lessons, examples or diagrams.
- **Real connections come later.** The owner will supply a real server; until then a session may test
  against a throwaway SQL Server container (`mcr.microsoft.com/mssql/server:2022-latest`, Docker) if it
  can start one, and must say in its report whether it did. Lesson code must be written for SQL
  Server either way. Such a topic must: (a) build with 0 warnings;
  (b) read the connection string from configuration, never from source (env var `BTAL_SQLSERVER` or
  `dotnet user-secrets`), and print a clear "set the connection string to run this lesson" message
  instead of throwing when it is missing; (c) say in its ROADMAP tracker row whether it was run against a real SQL Server
  (and which version) or **"not yet run against a real SQL Server"**; (d) keep every SQL statement it sends as a plain string or
  EF-generated query that a reviewer can read and check against T-SQL docs.
- Lessons use a dedicated scratch database (e.g. `BasicToAdvancedLearning`), create their own tables
  idempotently (`IF OBJECT_ID(...) IS NULL`/drop-and-recreate) and never touch other databases.
- Never commit a connection string, password, or server name (see the secrets rule in `CLAUDE.md`).

## Read — allowed
- Read-only access to verify data (inspect rows, confirm a script's effect). Querying to validate is fine.

## Write / scripts — never on your own
- Never execute DDL/UPDATE/INSERT/DELETE or seed scripts. Execution is manual, by a human.
- Deliver every change as a reviewed script (schema/one-off under a `sql/` or `scripts/` folder). Hand it
  over; don't apply it. Prefer local emulators/dev DB for testing.
- TODO: pick the folder (`sql/` vs `scripts/`) when topic 10 starts. (Engine decided: SQL Server, D8.)

## Writing data (grows over time)
- If the context defaults to no-tracking, read with tracking before saving an update, or it won't persist.
- "One active per key" behind a filtered unique index: deactivate-then-insert as two ordered commits,
  never one — batching can insert before the deactivate and trip the index.
- Check-then-act inserts need a DB unique constraint as a backstop (overlapping jobs can race).
- Beware provider quirks translating a parameterized `Contains(list)` — confirm the generated SQL.
