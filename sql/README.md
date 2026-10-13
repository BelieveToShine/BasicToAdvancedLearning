# `sql/` — hand-run T-SQL scripts (Microsoft SQL Server only)

The lessons create everything they need **themselves** (each in its own scratch database). These scripts exist for three other jobs:
1. **Create the bank tables for testing by hand** — look at them, break them, run your own queries.
2. **Pre-create the scratch databases** on a server where the lesson login may not run `CREATE DATABASE`.
3. **Self-check the schema** (`03`) — also used by the end-to-end runner ([`../docs/testing/README.md`](../docs/testing/README.md)).

| Script | Does | Safe to re-run | Destructive |
|---|---|---|---|
| `00-create-lesson-databases.sql` | creates `<DbBase>_<Lesson>` for RelationalConcepts, SqlBasics, SqlCrud, AdoNet, EfCoreBasics, EfCoreCrud, WebApiCrud and FullStackBank | yes | no |
| `01-bank-schema.sql` | creates `Customers`, `Accounts`, `Transactions` with named PK/FK/UNIQUE/CHECK/DEFAULT constraints, only what is missing | yes | no |
| `02-bank-seed.sql` | inserts sample rows (4 customers, 4 accounts, 5 transactions) into tables that are still empty | yes | no |
| `03-verify-bank-schema.sql` | read-only PASS/FAIL rows for every table, every named constraint, and balance = sum of transactions; fails the run (`THROW`) on any FAIL | yes | no |
| `04-reset-bank.sql` | drops the three tables (children first) in the **current** database; refuses system databases and any database whose name does not look like a scratch one (contains `scratch`/`test`/`e2e`, or starts with `BasicToAdvancedLearning_`) | yes | **yes** |
| `99-drop-lesson-databases.sql` | drops the lesson databases named by `00`; does nothing until you change `@Confirm` to `N'YES'` | yes | **yes** |

## Run them
Any T-SQL tool works (SSMS, Azure Data Studio, `sqlcmd`). With `sqlcmd`, `-b` makes failures return a non-zero exit code:
```bash
S="-S localhost,1433 -U sa -P '<password>' -C -b"                 # -C trusts a self-signed server certificate
sqlcmd $S -i sql/00-create-lesson-databases.sql                   # optional (DBA); edit @DbBase = your Initial Catalog first
sqlcmd $S -Q "CREATE DATABASE bank_scratch"                        # a scratch database for the bank tables (drop it when done: DROP DATABASE bank_scratch)
for f in 01-bank-schema 02-bank-seed 03-verify-bank-schema; do sqlcmd $S -d bank_scratch -i sql/$f.sql; done
```
Docker without a local `sqlcmd`: `docker exec -i sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P '<password>' -b -d bank_scratch < sql/01-bank-schema.sql`.

## Naming rule (must match `BasicToAdvancedLearning.Console/Interfaces/SqlLab.cs`)
A lesson's database is `<Initial Catalog of BTAL_SQLSERVER>_<LessonFolder>`; with an empty or `master` catalog the base is `BasicToAdvancedLearning`.
`00` and `99` hard-code the lesson list — if you add a database lesson, add its folder there, to `tests/e2e/manifest.tsv` and `tests/e2e/expected/`.

## Related
- The real API's schema is the EF migration in `FullStackBank/Api/Migrations/`; the container setup applies the same schema as the idempotent script `FullStackBank/deploy/db/01-schema.sql`. Do not duplicate it here.
- Engine rules and the "never commit connection strings" rule: [`../docs/rules/database.md`](../docs/rules/database.md).
