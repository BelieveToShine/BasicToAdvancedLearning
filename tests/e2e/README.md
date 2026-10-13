# `tests/e2e/` — the end-to-end runner

Full instructions: [`../../docs/testing/README.md`](../../docs/testing/README.md) and [`../../docs/testing/runbook.md`](../../docs/testing/runbook.md).

| File | Purpose |
|---|---|
| `run-e2e.sh` | runner for Linux / macOS / Git-Bash (the reference implementation; the only one that can refresh golden files) |
| `run-e2e.ps1` | the same checks for Windows PowerShell 5.1+ / pwsh 7 (folds CRLF and non-ASCII before comparing) |
| `manifest.tsv` | the 20 console lessons in run order: `Topic · NeedsSql · Stdin · Phase` |
| `expected/<Lesson>.txt` | golden outputs captured in the authoring sandbox; the lesson's database name is stored as `<DB>` |
| `actual/` | raw outputs, diffs and logs of the latest run (git-ignored, deleted at the start of every run) |

Options (bash / PowerShell): `--only A,B` / `-Only A,B` · `--no-sql` / `-NoSql` · `--skip-api` · `--skip-frontend` · `--skip-sqlscripts` · `--skip-docker-config` · `--update-golden` (bash only, developers only).
Environment: `BTAL_SQLSERVER` (required unless `--no-sql`), `E2E_SQLCMD` (custom `sqlcmd` command prefix), `E2E_SQL_VERSION` (text for the report header).

## Adding a lesson
1. Add a row to `manifest.tsv` (folder name = namespace = `<Folder>Example` class prefix).
2. Generate its golden file on a trusted run: `tests/e2e/run-e2e.sh --update-golden --only <Folder>`, then **read it** (no ports, paths, timestamps, tokens; database names only as `<DB>`).
3. Run the lesson twice — the output must be identical. If it needs a database, add it to `sql/00-create-lesson-databases.sql`, `sql/99-drop-lesson-databases.sql` and the matrix in `docs/testing/test-matrix.md`.
4. Keep the two runners twins. They are ordinary files, not protected ones.

The runner edits `BasicToAdvancedLearning.Console/Program.cs` while it runs (it must wire each lesson) and restores it with `git checkout` when it ends or is interrupted.
If a run is killed hard, restore it yourself: `git checkout -- BasicToAdvancedLearning.Console/Program.cs`.
