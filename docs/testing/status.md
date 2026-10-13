# Verification status board (single source of truth — update after every testing run)

**How to use:** (to add a third environment, copy a row of §1, change the first column and paste your report's link) read this before testing to see what is still open; after testing, change the rows you actually verified
(date, commit, where, report file) and add new findings. Never mark a row "verified" without a result report in
[`results/`](results/README.md) that shows it.

Legend: ✅ verified (report exists) · ⬜ not yet verified · 🔶 partly · ❌ failed (finding open)

## 1. Environments
| Environment | Status | Verified on | Commit | Report / evidence |
|---|---|---|---|---|
| Authoring sandbox: SQL Server 2022 in Docker, .NET 10 runtime with `DOTNET_ROLL_FORWARD=Major` (build targets net9.0), Linux | ✅ | 2026-10-13 | see report | [`results/24b82ec-vm.md`](results/24b82ec-vm.md) (bash runner; the PowerShell runner also passed the console lessons + SQL scripts) |
| **Owner's own SQL Server** (version: ?) | ⬜ | — | — | — |
| **A real .NET 9 SDK/runtime** (no roll-forward) | ⬜ | — | — | — |
| Windows (PowerShell runner `run-e2e.ps1`, CRLF/encoding differences) | ⬜ | — | — | — |
| Windows authentication / LocalDB / Azure-hosted SQL connection strings | ⬜ | — | — | — |
| Docker full stack (`FullStackBank/deploy`, `docker compose up --build`) on a developer machine (Docker Desktop, Windows/ARM) | ⬜ | — | — | — |

## 2. Checks (what the runner covers)
| Check | Authoring sandbox | Owner's server |
|---|---|---|
| Solution builds with 0 warnings | ✅ | ⬜ |
| 9 C# lessons that need no database (topics 1–9) match golden output | ✅ | ⬜ |
| SQL lessons 10–15 and 18 run and match golden output | ✅ | ⬜ |
| Web lessons 16, 17, 19, 20 (in-process server) match golden output | ✅ | ⬜ |
| `sql/01..04` scripts (create, seed, verify, reset — twice) | ✅ | ⬜ |
| `FullStackBank/Api` + `smoke.sh` (38 checks, twice) | ✅ | ⬜ |
| Frontend: 234 vitest tests + `vite build` | ✅ | ⬜ |
| `docker compose config` parses `.env.example` | ✅ | ⬜ |
| **Manual:** React lessons 25–27 in a real browser against the running API | 🔶 (reviewers drove them in headless Chromium) | ⬜ |
| **Manual:** Docker full stack up → page → deposit → down (runbook step 6) | 🔶 (builder + reviewer did it with a mirror workaround) | ⬜ |
| **Manual:** Postman GUI import of `ApiTesting/bank-api.postman_collection.json`; VS Code REST Client with `bank-api.http` | ⬜ | ⬜ |
| **Manual:** every diagram opened and looked at (they were reviewed by rendering, but no record is kept) | 🔶 | ⬜ |

## 3. Open findings (add a line per finding; remove only when fixed AND re-verified)
| # | Found by | Where | What | Status |
|---|---|---|---|---|
| — | — | — | none yet | — |

## 4. Known unverified claims (need a real server or runtime — see [`test-matrix.md`](test-matrix.md) §3 for the full list)
- SQL Server **error numbers** (2627, 2601, 547, 515, 2714, 4712, 147, 207, 33064) were confirmed on 2022 only.
- Exact **ProblemDetails text/keys** for broken JSON can differ between ASP.NET Core 9 and 10: the lessons print only version-stable facts, but a net9 run is still outstanding.
- **Windows** console encoding/CRLF handling in the PowerShell runner is untested.
