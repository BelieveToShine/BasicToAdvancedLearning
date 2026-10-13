# E2E result — 2026-10-13 on vm (commit 24b82ec)

- commit: `24b82ec`  ·  dotnet: `10.0.112`  ·  node: `v22.22.0`  ·  roll-forward used: YES - no .NET 9 runtime found, using DOTNET_ROLL_FORWARD=Major (results are NOT net9 results)
- SQL Server: 16.0.4295.3 Developer Edition (64-bit), collation SQL_Latin1_General_CP1_CI_AS (Docker image mcr.microsoft.com/mssql/server:2022-latest)
- totals: PASS 26 · DIFF 0 · FAIL 0 · WARN 0 · SKIP 0

| Check | Result | Notes |
|---|---|---|
| build solution | PASS | 0 Warning(s), 0 Error(s) |
| lesson ProgrammingBasics | PASS | identical to golden (43 lines) |
| lesson ControlFlow | PASS | identical to golden (45 lines) |
| lesson MethodsAndParameters | PASS | identical to golden (34 lines) |
| lesson Collections | PASS | identical to golden (50 lines) |
| lesson OOP | PASS | identical to golden (38 lines) |
| lesson OOPAdvanced | PASS | identical to golden (29 lines) |
| lesson ExceptionHandling | PASS | identical to golden (27 lines) |
| lesson FileIO | PASS | identical to golden (26 lines) |
| lesson LinqFundamentals | PASS | identical to golden (27 lines) |
| lesson RelationalConcepts | PASS | identical to golden (272 lines) |
| lesson SqlBasics | PASS | identical to golden (312 lines) |
| lesson SqlCrud | PASS | identical to golden (166 lines) |
| lesson AdoNet | PASS | identical to golden (45 lines) |
| lesson EfCoreBasics | PASS | identical to golden (97 lines) |
| lesson EfCoreCrud | PASS | identical to golden (185 lines) |
| lesson HttpAndRest | PASS | identical to golden (73 lines) |
| lesson WebApiBasics | PASS | identical to golden (55 lines) |
| lesson WebApiCrud | PASS | identical to golden (84 lines) |
| lesson ApiTesting | PASS | identical to golden (61 lines) |
| lesson JwtAuth | PASS | identical to golden (42 lines) |
| sql scripts 01-04 (idempotent) | PASS | schema, seed, verify, reset, schema again: all ran |
| api smoke test (twice) | PASS | PASS: 38 checks, 0 failures |
| frontend vitest | PASS |  Tests 234 passed (234) |
| frontend vite build | PASS | ok |
| docker compose config | PASS | .env.example parses (no containers started; full stack test: docs/testing/runbook.md) |

Diffs and raw outputs: `tests/e2e/actual/` (git-ignored). Triage every DIFF/FAIL with `docs/testing/triage-guide.md`.
