# Test matrix — every check, what it proves, what it needs

Back to [`README.md`](README.md). The runner (`tests/e2e/run-e2e.sh` / `.ps1`) executes sections 1 and 2; section 3 lists what only a
real server/runtime/browser can confirm; section 4 is the manual part.

## 1. Console lessons (golden-output checks)
Each row: the lesson runs through the runner (it rewires `Program.cs` temporarily), its output is compared with `tests/e2e/expected/<Lesson>.txt`.
`<DB>` in a golden file stands for the lesson's own database name (`<Initial Catalog>_<Folder>`), which the runner normalises.

| Topic | Folder | Needs SQL | What a PASS proves |
|---|---|---|---|
| 1–9 | `ProgrammingBasics` (input `Vivek`, `28`), `ControlFlow`, `MethodsAndParameters`, `Collections`, `OOP`, `OOPAdvanced`, `ExceptionHandling`, `FileIO`, `LinqFundamentals` | no | the C# fundamentals still compile and print what the comments/diagrams say (regression guard; `FileIO` writes to the OS temp folder and cleans up) |
| 10 | `RelationalConcepts` | **yes** | tables, PK/FK/UNIQUE/CHECK/NOT NULL behave and fail with the documented SQL Server **error numbers** (2627, 547, 515) |
| 11 | `SqlBasics` | **yes** | SELECT/WHERE/ORDER BY/JOIN/GROUP BY results on the seeded bank; errors 147 and 207 |
| 12 | `SqlCrud` | **yes** | INSERT/UPDATE/DELETE, `SCOPE_IDENTITY` vs `@@IDENTITY`, TRUNCATE error 4712, transactions, `XACT_ABORT` ON/OFF behaviour |
| 13 | `AdoNet` | **yes** | `SqlConnection/SqlCommand/SqlDataReader/SqlTransaction`; the injection demo drops only its own scratch table |
| 14 | `EfCoreBasics` | **yes** | model → create script, a real migration applies (`__EFMigrationsHistory`), `EnsureCreated`↔`Migrate` clash (error 2714) |
| 15 | `EfCoreCrud` | **yes** | EF CRUD, the SQL EF sends, change-tracker states, all-or-nothing `SaveChanges` |
| 16 | `HttpAndRest` | no | in-process Kestrel + `HttpClient`: status codes, headers, JSON casing |
| 17 | `WebApiBasics` | no | controllers, routing, model binding, DI lifetimes |
| 18 | `WebApiCrud` | **yes** | CRUD endpoints over EF Core + SQL Server, 201/400/404/409 |
| 19 | `ApiTesting` | no | Swashbuckle OpenAPI document, the `.http` and Postman files replayed |
| 20 | `JwtAuth` | no | JWT issue/validate, 401 vs 403, expiry |

## 2. Other automated checks
| Check | Command (done by the runner) | PASS means |
|---|---|---|
| Solution build | `dotnet build BasicToAdvancedLearning.sln --no-incremental` | 0 warnings, 0 errors (console project + `FullStackBank/Api`) |
| SQL scripts | `sql/01 → 02 → 03 → 01 → 02 → 03 → 04 → 01 → 03` in a throwaway database | idempotent create/seed/verify/reset, every `03` check PASS ([`../../sql/README.md`](../../sql/README.md)) |
| API smoke test | start `FullStackBank/Api` on :5290, run `FullStackBank/smoke.sh` twice | 38 checks, 0 failures, repeatable |
| Frontend tests | `cd frontend && npm install && npx vitest run` | 234 tests pass |
| Frontend build | `npx vite build` | no errors |
| Compose file | `docker compose --env-file .env.example config -q` | the container definition parses |

The repo's own gates are separate and still apply to every change: `pwsh scripts/verify-topic.ps1 -All -SkipBuild` (expect **0 FAIL**, 25 WARN baseline)
and `pwsh scripts/verify-docs.ps1` (0 FAIL) — see [`../rules/verification.md`](../rules/verification.md).

## 3. Claims that can only be confirmed on a real server / runtime (please look at these specifically)
Collected from the review notes of the build session. A `DIFF` in a lesson that touches one of these is *expected-risk*: triage it first.

**SQL Server (version- or configuration-dependent)**
- Error numbers 2627 / 2601 / 547 / 515 / 2714 / 4712 / 147 / 207 and the first line of their messages (printed at run time in topics 10–12, 15; comments and diagrams quote only numbers). A different **collation** or **language** can change message text and sort order of unordered results (lessons order everything they print).
- `SCOPE_IDENTITY()` vs `@@IDENTITY` demo (uses a trigger on a second table); DELETE keeps the identity counter, TRUNCATE resets it; TRUNCATE on a table referenced by a foreign key fails.
- A SqlClient session starts with `XACT_ABORT OFF` (the lesson shows ON/OFF side by side); table variables keep rows after `ROLLBACK`.
- `DECIMAL(18,2)` results, `SYSUTCDATETIME()` defaults; password-policy rejection (error 33064) in the Docker deployment.
- Permissions: lessons need `CREATE DATABASE` (or the databases pre-created by `sql/00`); `EnsureDeleted()` (topics 15, 18) needs rights to drop the lesson's own database.

**EF Core 9.0.20**
- Default `decimal` mapping is `decimal(18,2)` and EF logs a *DecimalTypeDefaultWarning* when precision is unset; `EnsureCreated` then `Migrate` fails with 2714; `Migrate` then `EnsureCreated` returns `False`.
- The migration files in `EfCoreBasics/Migrations/` are real `dotnet ef` output (9.0.20); `dotnet tool install --global dotnet-ef --version 9.0.20` regenerates them (if the generated timestamp changes, the lesson's printed migration id and three diagrams change too).
- "EF starts transactions through ADO.NET, not as a `BEGIN` line in the SQL text"; `SET IMPLICIT_TRANSACTIONS OFF; SET NOCOUNT ON;` and `OUTPUT 1` appear in printed statements.

**ASP.NET Core / .NET 9 vs 10**
- The sandbox ran net9.0 builds on a **.NET 10 runtime**. Exact `ProblemDetails` key names/messages for broken JSON can differ between 9 and 10: lessons print only version-stable facts. The `FullStackBank` API image **did** run on `aspnet:9.0` in Docker.
- Swashbuckle 10.3.0 on net9 (OpenAPI version printed by the library, `3.0.4` in the sandbox).
- JwtBearer 9.0.20 error text (`invalid_token` messages) is version-dependent and not printed in full.

**Environment-specific**
- Windows: CRLF line endings and console encoding (the PowerShell runner folds both before comparing), `FileIO` temp-folder behaviour, Docker Desktop and ARM images.
- Docker Hub rate limits (HTTP 429) when pulling `node`/`nginx` (a mirror was used in the sandbox); base images are not pinned to a digest.

**Not run by anyone yet:** the Postman GUI import of `ApiTesting/bank-api.postman_collection.json` (it ran 8/8 with `newman`), the VS Code REST Client / Visual Studio opening `bank-api.http`, HTTPS anywhere, an API with CORS enabled, a real xUnit + `WebApplicationFactory` project.

## 4. Manual checks (the runner cannot do these)
See [`runbook.md`](runbook.md) steps 5 and 6: React lessons 25–27 against the running API in a browser; the Docker stack; looking at the diagrams
(open `ArchitectureDiagrams/<Topic>/**/*.svg` in a browser — text must sit inside boxes, no arrow may cross a box, the numbers must match the lesson output).
