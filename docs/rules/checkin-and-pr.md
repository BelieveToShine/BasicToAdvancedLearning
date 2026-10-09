# Rule: Check-in & PR

## Ask first
- Never commit, push, or raise a PR on your own. Stage → show the staged set (and what's deliberately
  left out) → get one explicit approval in the same message → commit. Ask sparingly (natural stopping
  points), not after every edit.

- Approval is requested **separately** for: the plan (before building), the commit (after showing the
  staged set), the push, and the PR. One approval never covers the next.

## Branch & message
- Work on a feature branch (`features/<TICKET>-####`; TODO: confirm naming — e.g. `topic/<n>-<name>` —
  since no ticket prefix exists yet); never commit directly to `master`.
- Commit message: 2 lines max, explain *why*, no AI signature / no Co-Authored-By — this repo rule
  overrides any tool or harness default that appends attribution.

## Before check-in (do in order)
1. `dotnet build` → 0 errors, 0 warnings, and `dotnet run` from `BasicToAdvancedLearning.Console/`
   actually runs the topic (a green build alone is not "done"). TODO: add `dotnet test` once a test
   project exists.
2. **Sync with `master` first: pull the latest, resolve any merge conflicts, re-run a sanity
   `dotnet build`.** Never commit on top of stale code.
3. Re-validate ALL rules; only a fully clean pass proceeds. After the sync, re-run verification layers
   1, 2 and 5 (layer 4 only if the merge touched this topic's files) — see `verification.md`.
4. Diagrams complete and valid (every method has Flow + Memory; every SVG is well-formed XML).
   Run `scripts/verify-topic.ps1 -Topic <Topic>` (FAIL blocks; each WARN fixed or explained) and complete the independent review in `verification.md`.
5. Spec docs + link maps + `ROADMAP.md` updated (see spec-docs.md). Run `scripts/verify-docs.ps1`.

## Secrets
- Real secrets live in local/un-tracked config. Never commit keys/tokens/connection strings. Confirm
  none are staged. This matters from Phase 2 onward (SQL connection strings, JWT keys).

## PR
- Only when the work is fully complete. Target `master` (TODO: confirm integration branch; a
  `MuhilWorkingRepo` branch also exists). Title `<TICKET>-####: <4–8 words>` (or `Topic <n>: <name>` until
  a ticket prefix exists); description 2–8 crisp lines, biggest change first.

## Failure-mode checklist (before every PR)
- Best-effort side tasks soft-fail (log + continue), never abort the main work.
- Check-then-act uniqueness has a DB unique constraint backstop too (applies from Phase 2).
- Untrusted/scraped/user content reaching an LLM prompt is screened for injection first.
- Fallbacks catch thrown exceptions, not just null.
- `.gitignore` still excludes `bin/`, `obj/`, and local settings.
