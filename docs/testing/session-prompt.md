# Ready-made prompt for a testing session

Copy everything below the line into a new Claude Code (or other agent) session started in a clone of this repo **on a machine that has a SQL Server or Docker**.
Set the connection string in the environment first (`BTAL_SQLSERVER`, see [`runbook.md`](runbook.md) step 2) — never paste it into the chat.

---

You are the **end-to-end testing session** for this repository (BasicToAdvancedLearning). Do not rely on any earlier conversation.

1. Read `CLAUDE.md`, then `docs/testing/README.md`, `docs/testing/status.md` and `docs/testing/runbook.md`. Then follow the runbook.
2. Make sure `BTAL_SQLSERVER` is set (ask me only if it is missing and you cannot start a SQL Server with Docker). Run
   `tests/e2e/run-e2e.sh` (Windows: `pwsh tests/e2e/run-e2e.ps1`). Do the manual steps 5–6 of the runbook if a browser/Docker is available.
3. Triage every DIFF/FAIL with `docs/testing/triage-guide.md`. Fix real lesson/diagram/golden problems exactly as that guide says (code, diagrams, golden file and docs together);
   never weaken a check or edit a golden file just to get green. Do not edit protected files.
4. Re-run until the only remaining non-PASS results are honest SKIPs or findings you wrote down.
5. Update `docs/testing/status.md` (what you verified, on which server version/OS/.NET, the report file; open findings), prepare the result report and your fixes for commit
   (2-line commit messages, no AI signature; **commit and push only if I said so in this prompt or when you ask me** — repo rule), run `pwsh scripts/verify-topic.ps1 -All -SkipBuild` and `pwsh scripts/verify-docs.ps1` (0 FAIL), and report to me:
   what passed, what differed and how you resolved it, and what is still unverified.
