# Manual checks — add this section under the generated table of a result report

Copy, fill in, delete lines that do not apply. Use only: PASS · FAIL · SKIP (with a reason). "Looked fine" is not a result — say what you opened and what you saw.

## Environment
- OS / machine: …
- SQL Server: `SELECT @@VERSION` → … (edition, collation `SELECT SERVERPROPERTY('Collation')` → …)
- .NET SDK / runtimes: `dotnet --list-sdks`, `dotnet --list-runtimes` → … (net9 runtime present: yes/no)
- Browser (for step 5): …  ·  Docker version (for step 6): …

## Manual checks
| Check | Result | What I did / saw |
|---|---|---|
| React lessons 25 ReactApiClient 01–05 against the running API | | |
| React lessons 26 FullStackBank 01–05 (list, detail, deposit, 422 on too-large withdrawal, open account, 409 on duplicate) | | |
| React lessons 27 CrossLayerValidation 01–05 | | |
| Pages show the friendly "API not reachable" state with the API stopped | | |
| Docker stack: `docker compose up --build` → page → deposit → `down -v` | | |
| Postman: import `ApiTesting/bank-api.postman_collection.json`, run it against the API (set `baseUrl`) | | |
| `ApiTesting/bank-api.http` opens and runs in VS Code REST Client / Visual Studio | | |
| Diagrams opened in a browser for topics I touched (text inside boxes, arrows do not cross boxes, numbers match output) | | |

## Findings
| # | Where | What differed | Class (environment / lesson bug / stale golden / runner) | Fixed in commit / still open |
|---|---|---|---|---|
| | | | | |
