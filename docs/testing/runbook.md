# Runbook — end-to-end test against a real SQL Server

Follow the steps in order. Each step says what "good" looks like. Time: about 15 minutes the first time, ≈2 minutes per later run.
Read [`README.md`](README.md) first if you have not (it explains the kit); keep [`status.md`](status.md) open.

## Step 0 — Prerequisites
| Need | Check | Notes |
|---|---|---|
| Git + this repo cloned, on `master` (`git pull`) | `git log -1` | work on a branch if you will push fixes |
| **.NET 9 SDK** | `dotnet --list-sdks` shows `9.0.x` | with only a newer SDK/runtime the runner sets `DOTNET_ROLL_FORWARD=Major` and **says so in the report** — please get a real 9 for the final run |
| **Node 22 + npm** | `node --version` | only for the frontend checks (`--skip-frontend` to omit) |
| `bash` + `curl` | `bash --version` | for `FullStackBank/smoke.sh` (Windows: Git-Bash) |
| `sqlcmd` (optional) | `sqlcmd -?` | for the `sql/` script check; otherwise set `E2E_SQLCMD` (below) or that check is SKIPPED |
| Docker (optional) | `docker info` | only to start a SQL Server quickly and for step 6 |
| PowerShell 7 (`pwsh`) | `pwsh --version` | only if you use the `.ps1` runner or the repo's `scripts/*.ps1` |

## Step 1 — Get a SQL Server (Microsoft SQL Server only — decision D8)
Pick one:
1. **An existing server** (2019 or newer, any edition): you need a login that may `CREATE DATABASE`. If not allowed, a DBA runs
   `sql/00-create-lesson-databases.sql` once (edit `@DbBase` to match your Initial Catalog) and the login then needs rights only inside those databases (`db_owner` on them).
2. **Docker** (nothing installed on the host):
   ```bash
   docker run -d --name sql -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='<Strong#Passw0rd>' -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
   ```
   Password rules: 8+ characters, three of upper/lower/digit/symbol. Wait ~20 s (`docker logs sql | tail`). Docker Hub/MCR rate limits: retry later or use a mirror.
3. **SQL Server Developer/Express or LocalDB (Windows)**: use its connection string (LocalDB: `Server=(localdb)\MSSQLLocalDB;Integrated Security=True;...` — see step 2 note).

## Step 2 — Tell the repo where the server is (never commit this)
```bash
# bash
export BTAL_SQLSERVER='Server=localhost,1433;User Id=sa;Password=<Strong#Passw0rd>;TrustServerCertificate=True;Initial Catalog=BasicToAdvancedLearning'
```
```powershell
# PowerShell
$env:BTAL_SQLSERVER = 'Server=localhost,1433;User Id=sa;Password=<Strong#Passw0rd>;TrustServerCertificate=True;Initial Catalog=BasicToAdvancedLearning'
```
- **Initial Catalog is only a naming base.** Every lesson uses its own database `<Initial Catalog>_<LessonFolder>` (for example `BasicToAdvancedLearning_SqlCrud`), created automatically if the login may. Leave it empty or `master` and the base becomes `BasicToAdvancedLearning`.
- Windows authentication / LocalDB work with the lessons (any valid SqlClient connection string); the runner's *SQL scripts* check derives `sqlcmd` arguments only from SQL authentication — for other styles set `E2E_SQLCMD`, e.g. `export E2E_SQLCMD='sqlcmd -S (localdb)\MSSQLLocalDB -E'`.
- `E2E_SQLCMD` also lets you reach a Docker server without a local `sqlcmd`: `export E2E_SQLCMD='docker exec -i sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P <Strong#Passw0rd>'`.
- Optional: `export E2E_SQL_VERSION="$(…SELECT @@VERSION…)"` so the report records the exact server version.

## Step 3 — (optional) look at the reference tables by hand
```bash
sqlcmd -S localhost,1433 -U sa -P '<…>' -C -b -Q "CREATE DATABASE bank_scratch"
for f in 01-bank-schema 02-bank-seed 03-verify-bank-schema; do sqlcmd -S localhost,1433 -U sa -P '<…>' -C -b -d bank_scratch -i sql/$f.sql; done
```
Expected: `ALL CHECKS PASSED`. Details: [`../../sql/README.md`](../../sql/README.md). (The runner does this automatically in a throwaway database.)

## Step 4 — Run everything
```bash
tests/e2e/run-e2e.sh            # Linux / macOS / Git-Bash          (add --only SqlCrud,AdoNet for a subset)
pwsh tests/e2e/run-e2e.ps1      # Windows PowerShell 5.1+ or pwsh   (-Only SqlCrud,AdoNet)
```
The runner: preflight → builds the solution (`--no-incremental`, must be 0 warnings) → for each of the 20 console lessons temporarily rewires
`Program.cs`, runs the lesson and diffs it against `tests/e2e/expected/<Lesson>.txt` → runs `sql/01..04` twice → starts `FullStackBank/Api`
and runs `smoke.sh` twice → `vitest` + `vite build` → `docker compose config` → writes `docs/testing/results/<commit>-<host>.md` and **restores `Program.cs`**.

| Result | Meaning | Next |
|---|---|---|
| `PASS` | identical to golden / check succeeded | nothing |
| `DIFF` | lesson ran but printed something different | [`triage-guide.md`](triage-guide.md) — diff is in `tests/e2e/actual/<Lesson>.diff` |
| `FAIL` | crash, cannot connect, build warning, failing test | open the named file in `tests/e2e/actual/`, then triage |
| `SKIP` | not run (missing tool/permission/flag) | allowed, but must be written down in `status.md` as **not verified** |
| `WARN` | ran but no golden file | add the lesson to the manifest and golden folder |

Exit code: `0` all executed checks passed · `1` something failed or differs · `2` could not start (preflight).
Typical connection failures: login failed (user/password), `TrustServerCertificate=True` missing for a self-signed server certificate, wrong port, firewall, the login lacks `CREATE DATABASE` (then run `sql/00-…` as a DBA).

## Step 5 — Manual end-to-end check of the browser lessons (about 5 minutes)
The runner cannot click a browser. In two terminals, with `BTAL_SQLSERVER` set in both:
```bash
cd FullStackBank/Api && dotnet run                      # terminal 1: API on http://localhost:5080 (Development: creates + seeds its database)
cd frontend && npm install && npm run dev               # terminal 2: open the printed URL
```
In the lesson picker open, in order: **ReactApiClient / 01…05**, **FullStackBank / 01…05**, **CrossLayerValidation / 01…05**. Confirm for each: the page shows data
(not the red "API not reachable" box), a deposit changes the balance, withdrawing too much shows the 422 message, a duplicate account number shows the 409 message,
and stopping the API (Ctrl+C) makes the pages show the friendly "API not reachable" state. Record anything odd in `status.md` §3.

## Step 6 — (optional, needs Docker) the full container stack
```bash
cd FullStackBank/deploy && cp .env.example .env     # then replace every CHANGE_ME value with generated passwords (see deploy/README.md)
docker compose up --build -d && docker compose ps  # wait until db, api and web are healthy and db-init has exited 0
curl http://127.0.0.1:8080/api/accounts            # (WEB_PORT in .env) → the 3 demo accounts
docker compose down -v                             # tear down (deletes the data volume)
```
Follow `FullStackBank/deploy/README.md` for the explanations and the troubleshooting table (Docker Hub 429 is common).

## Step 7 — Close out (do not skip)
- [ ] The report `docs/testing/results/<commit>-<host>.md` exists — add the SQL Server version and OS if the runner could not (edit the header lines).
- [ ] [`status.md`](status.md): update §1/§2 rows you really verified (date, commit, report link); add findings to §3; keep §4 honest.
- [ ] Every DIFF/FAIL is either fixed (re-run shows PASS) or listed as an open finding. Never leave one unexplained.
- [ ] Fixes: lesson code + its diagrams + `tests/e2e/expected` + docs updated **together**; `scripts/verify-topic.ps1 -All -SkipBuild` still 0 FAIL and `scripts/verify-docs.ps1` 0 FAIL ([`../rules/verification.md`](../rules/verification.md)).
- [ ] `git status` shows no `Program.cs` change and no secrets; commit (2-line message, no AI signature — repo rule) and push as the owner's workflow requires ([`../rules/checkin-and-pr.md`](../rules/checkin-and-pr.md)).
- [ ] Tell the owner: what passed, what differed, what is still unverified.
