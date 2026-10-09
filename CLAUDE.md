# BasicToAdvancedLearning — Start Here (auto-loaded every session)

Follow these before doing anything. Details live in the linked files — read the relevant ones for the
task rather than re-deriving from source. For the whole picture see [`docs/README.md`](docs/README.md).

## Mandatory workflow
0. **Understand the product** — [`docs/basic-to-advanced-learning-overview.md`](docs/basic-to-advanced-learning-overview.md).
1. **Understand the code shape** — [`docs/architecture/overview.md`](docs/architecture/overview.md).
2. **Read the spec for the area** — check the index in
   [`docs/superpowers/specs/README.md`](docs/superpowers/specs/README.md); if a spec covers what you're
   touching, read it (not the source).
3. **Plan, then wait for approval.** State what you will build (for a topic: folder/namespace name,
   method list, running example, exact diagram file list, any Memory exceptions) and get the human's
   OK before writing it. Settle the open questions listed under "Next" in `docs/ROADMAP.md`.
3a. **Do the work.** For a new lesson topic, follow the runbook in [`docs/ROADMAP.md`](docs/ROADMAP.md).
4. **Update the spec** — if the change falls under a documented area, updating that spec is part of
   "done", not a follow-up. Also update the topic tracker in `docs/ROADMAP.md`.
5. **Verify before claiming done** — all five layers in [`docs/rules/verification.md`](docs/rules/verification.md): build+run, `scripts/verify-topic.ps1`, manual diagram checklist, independent fresh-context review, `scripts/verify-docs.ps1`. Report each layer PASS/WARN/FAIL/SKIPPED honestly.

For how a unit of work runs end to end, see [`docs/aidlc.md`](docs/aidlc.md).

## Rules (read before the matching action)
- Writing/modifying code → [`docs/rules/coding-standards.md`](docs/rules/coding-standards.md)
- Reading a spec / tracking work → [`docs/rules/spec-docs.md`](docs/rules/spec-docs.md)
- Commit / push / PR → [`docs/rules/checkin-and-pr.md`](docs/rules/checkin-and-pr.md)
- DB query or script → [`docs/rules/database.md`](docs/rules/database.md)
- Issue tracker → [`docs/rules/jira.md`](docs/rules/jira.md)
- Diagrams (mandatory 3 per topic) → [`docs/rules/diagrams.md`](docs/rules/diagrams.md), which points to
  the full [`docs/rules/diagram-standards.md`](docs/rules/diagram-standards.md)
- Verifying / reviewing work → [`docs/rules/verification.md`](docs/rules/verification.md)

## Quick reminders
- Work on a branch only; **never commit, push, or raise a PR on your own** — ask first, once the task is done.
- Commit messages: 2 lines max, explain *why*, no AI signature / no Co-Authored-By. This repo rule
  overrides any tool default that appends attribution.
- Approvals are separate: the plan (step 3), the staged set + commit, the push, and the PR each need
  their own explicit OK.
- Run `scripts/verify-topic.ps1 -Topic <Topic>` and `scripts/verify-docs.ps1` before proposing a check-in; FAIL blocks, every WARN is fixed or explained.
- `dotnet build` reports 0 errors/0 warnings before proposing a check-in, and `dotnet run` from
  `BasicToAdvancedLearning.Console/` actually runs the topic.
- **Before any check-in: pull latest `master`, resolve conflicts, run a sanity build/test,
  re-validate all rules — only a clean result is committed.**
- Secrets live in local/un-tracked config; never commit keys/tokens/connection strings.
- No dates in doc file names/folders; no ticket numbers inside specs.
- Never name a namespace segment `Console` (shadows `System.Console`) — see `docs/ROADMAP.md` gotcha #1.
- Fold every change into its category spec + keep link maps current, the moment the change lands.

## Adding rules
New convention or gotcha → add it to the right file under `docs/rules/` (or `docs/architecture/overview.md`
for a code gotcha). Never leave a rule only in chat.
