# Triage guide — a check came back DIFF or FAIL

Back to [`README.md`](README.md). Work through the questions in order. Always start by opening the named file in `tests/e2e/actual/`
(`<Lesson>.diff`, `<Lesson>.txt`, `<Lesson>.err`, `build.txt`, `smoke.txt`, `vitest.txt` …).

## 1. Classify
| Symptom | Most likely class | Look at |
|---|---|---|
| `cannot connect to SQL Server`, `Login failed`, certificate error | **Environment** — connection string / server not running / firewall | [§2 Environment](#2-environment-problems) |
| `CREATE DATABASE permission denied` | **Environment** — login rights | run `sql/00-create-lesson-databases.sql` as a DBA, or use a login with `dbcreator` |
| DIFF only in lines with an error **message text** (the part after "SQL Server refused it (error 547): …") | **Environment** — server version/language/collation wording | [§3](#3-diff-in-sql-server-wording) |
| DIFF in row **order**, decimals, dates, or sort-dependent output | **Environment (collation)** or a real **lesson bug** (missing `ORDER BY`) | [§3](#3-diff-in-sql-server-wording) |
| DIFF in a line that has a **number or sentence the lesson computes** | **Lesson bug or wrong claim** — the real server contradicts the lesson | [§4](#4-the-lesson-is-wrong-fix-it) |
| FAIL: crash / `Unhandled exception` with a stack trace | **Lesson bug** or environment (permissions, missing tool) | read the exception first |
| FAIL: build has warnings | **Code** — the repo promises 0 warnings | fix the warning (may be a net9-vs-10 analyzer difference) |
| DIFF everywhere on Windows, differences only in line endings/characters | **Runner** — use `run-e2e.ps1` (it folds CRLF and non-ASCII) | [§5](#5-runner--golden-file-problems) |
| `smoke.sh` check failed | **API bug / contract drift / environment** | [§4](#4-the-lesson-is-wrong-fix-it) + `FullStackBank/README.md` |
| vitest failed | **Frontend** — a lesson claim broke | the failing test names the lesson and the claim |

## 2. Environment problems
Fix the environment, not the repo: correct `BTAL_SQLSERVER`, add `TrustServerCertificate=True` for self-signed certificates, open the port, start the container,
grant rights. Re-run. If a lesson genuinely cannot work on some server configuration (for example a read-only login), record it in `status.md` §3 as a limitation and
tell the owner — do not change the lesson to hide it.

## 3. DIFF in SQL Server wording
1. Confirm that only message text changed (compare the `error NNN` numbers: they must be identical).
2. If the numbers are identical and only text differs → this is a legitimate environment difference. Do **not** edit the golden file blindly: first check that the lesson's own comment/diagram quotes **only the number** (it should). If a comment or diagram quotes the text, fix that comment/diagram to quote the number.
3. If the text difference is common to every supported server (for example a newer wording on a newer SQL Server version), update the golden file **and** write the server version in the commit message and in `status.md`.
4. If the numbers differ, or the **rows/values** differ → it is not wording: go to §4.

## 4. The lesson is wrong: fix it
The real server beats the comments. When a number, a row or a sentence differs and the cause is "SQL Server really behaves like this":
1. Reproduce the one statement by hand (`sqlcmd`, SSMS, or `sql/01-bank-schema.sql` in a scratch database). Note the exact behaviour and server version.
2. Fix **all three places together**: the lesson code/comments, the lesson's **diagrams** (`ArchitectureDiagrams/<Topic>/Flow|Memory/*.svg` — rows, numbers, footers), and `tests/e2e/expected/<Lesson>.txt`. Students must not be misled anywhere; a fixed comment with a stale diagram is still a defect.
3. Re-run only that lesson: `tests/e2e/run-e2e.sh --only <Lesson>` (twice — output must be repeatable).
4. Run the repo gates: `pwsh scripts/verify-topic.ps1 -Topic <Lesson> -SkipBuild`, `pwsh scripts/verify-docs.ps1`. Look at every changed diagram in a browser (text inside boxes, no arrow through a box).
5. If a **review-worthy** claim changed (an absolute, a security statement, an error-number table), ask for an independent fresh-context review of the topic before pushing ([`../rules/verification.md`](../rules/verification.md) layer 4).

## 5. Runner / golden-file problems
- **Golden file stale because a lesson was deliberately changed:** regenerate with `tests/e2e/run-e2e.sh --update-golden --only <Lesson>` on a Linux/macOS/Git-Bash machine, read the `git diff` of the golden file line by line, and commit it together with the lesson change. Never run `--update-golden` over a whole failing run to "make it green".
- **Golden file machine-dependent** (a path, port, timestamp, machine name, database name slipped in): that is a lesson bug — make the lesson not print it; the runner only normalises the database name.
- **Runner bug** (wrong exit code, restores `Program.cs` wrongly, mis-parses a connection string): fix `tests/e2e/run-e2e.sh` **and** `run-e2e.ps1` together so they stay twins; they are not protected files.

## 6. Never do this
- Never edit a golden file, a check, a script or a lesson only to remove a diff you do not understand.
- Never mark a check PASS in `status.md` that was SKIPPED or not run.
- Never commit a connection string, a password, `.env`, or a result report that contains one (the runner's report does not, but check your own notes).
- Never change protected files (`CLAUDE.md`, `docs/rules/**`, `docs/aidlc.md`, `docs/decisions.md`, `scripts/*.ps1`) for a testing fix: propose it in `docs/review-recommendations.md`.
- Never leave `Program.cs` modified: `git checkout -- BasicToAdvancedLearning.Console/Program.cs` (the runner restores it itself, but an interrupted run may not).

## 7. Reporting
Open findings go to [`status.md`](status.md) §3 (one line each: where, what, status). Anything the owner must decide goes to
`docs/review-recommendations.md` as a new `needs-owner` item. End your session by telling the owner what passed, what differed, and what remains unverified.
