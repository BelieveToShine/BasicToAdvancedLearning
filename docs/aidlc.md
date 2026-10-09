# BasicToAdvancedLearning — AI-Driven Development flow (AIDLC)

How each unit of work (a new lesson topic, a fix, a doc change) runs, using the agent + the specs/rules
in this repo. Human approval is needed at the plan, the commit and the push (each separately), and for a PR only if one is asked for — see [`rules/checkin-and-pr.md`](rules/checkin-and-pr.md); everything else is automated.

1. **Work item in** — a `<TICKET>-####` id if a tracker is wired up (TODO: ticket prefix + tracker not
   decided yet), otherwise the next row in the [`ROADMAP.md`](ROADMAP.md) topic table (e.g. "Topic 6 — OOP advanced").
2. **Spec-first read** — overview → architecture → the matching spec + rules (not the whole codebase).
3. **Plan + scope** — work item + spec → an implementation plan. For a topic this is: the `ExplainX()`
   method list, the running example threaded through them, and the diagram list (1 architecture +
   `Flow/` and `Memory/` each with `0-Overview` + one file per method). A scope checklist picks which
   steps to run (token-aware: full end-to-end, or code only when tokens are tight — but a topic is not
   ✅ without all diagrams). **We review and approve.**
4. **Build** — implement the demo + adapter, wire `Program.cs`; progress tracked in
   `docs/todo/<id>.md`. TODO: there is no unit-test project yet; until one exists, "tested" means
   `dotnet run` output matches what the comments and diagrams claim.
5. **Verify (5 layers)** — per [`rules/verification.md`](rules/verification.md): (1) `dotnet build` 0 warnings + `dotnet run`; (2) `scripts/verify-topic.ps1`; (3) manual diagram checklist; (4) independent fresh-context review (a separate session/subagent that has not seen the build); (5) `scripts/verify-docs.ps1` + reading the docs the work should have touched. The agent reports each layer PASS/WARN/FAIL/SKIPPED — never claims an unrun layer passed.
6. **Pre-check-in gate** — **pull latest `master` → resolve conflicts → sanity build → re-validate
   ALL rules.** Only a fully clean pass proceeds.
7. **Commit + push** — commit (2-line message) and push after our review — a completed topic goes straight to `master` with no PR under the conditions in [`rules/checkin-and-pr.md`](rules/checkin-and-pr.md); raise a PR against `master` only if asked; fold
   changes into the specs and flip the topic's status in `ROADMAP.md`; move the todo doc to archive.
8. **AI PR review** — only if a PR is raised. TODO: no PR-validation tool wired yet. Until then, the human reviews the PR and
   the agent fixes the valid comments and re-validates.

Self-updating: specs, rules, `ROADMAP.md` and the diagrams are kept in sync as part of "done", so the
next session starts from accurate context.
