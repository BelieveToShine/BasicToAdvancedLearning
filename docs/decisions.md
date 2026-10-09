# Owner decisions log (append-only)

The only place a rule may cite "the owner decided". Read [`rules/governance.md`](rules/governance.md)
section 4 first. Never edit or delete an entry — add a new one that supersedes it.

**Entry format:** `D<n>` · date · who gave it · the owner's wording (quote) · scope (**one-off** or
**standing**) · what it changed / where recorded.

| ID | Date | Decision (owner's wording, condensed) | Scope | Recorded in |
|---|---|---|---|---|
| D1 | 2026-10-09 | "commit these files on a branch" — the AIDLC docs/scripts commit | one-off | commit `18e4f06` |
| D2 | 2026-10-09 | "no need pull request now.. directly push the changes to main or master" — for that same docs commit | one-off (that push only) | `master` fast-forwarded to `18e4f06` |
| D3 | 2026-10-09 | Asked for a review-recommendations file + governance set-up after the Topic 5 review | one-off | `review-recommendations.md`, `rules/governance.md` |

| D4 | 2026-10-09 | "commit this as a rules commit on a branch" — the governance / review-recommendations / render-script set-up | one-off (commit on a branch only; **not** a push, **not** a merge) | branch `rules/governance-and-review` |
| D5 | 2026-10-09 | "push it to master" — the `rules/governance-and-review` branch (`6458b23`) | one-off (that push only; does **not** answer Q1) | `master` fast-forwarded and pushed. **Deviation:** no independent layer-4 review was run on this change before the push (governance §8); only `verify-docs.ps1` (0 FAIL) was run. |
| D6 | 2026-10-09 | Answer to Q1, picked from the fixing-session question "Is direct-to-master check-in with no PR a STANDING policy for completed topics, or one-off?": **"Standing policy"**. Same session, earlier: "While pushing also push to master.. no need of PR.. by pass that" | **standing** (completed lesson topics only; conditions in `rules/checkin-and-pr.md`) | `rules/checkin-and-pr.md` (single source); `CLAUDE.md` and `aidlc.md` now link to it |
| D7 | 2026-10-09 | Answer to review item R5: **"Amend the rule text"** — metaphor-style Flow diagrams may name the result variable and show a `new` as the object arriving; every real `Console.*` call must still be shown or footnoted | one-off decision, applied now | `rules/diagram-standards.md` section 2 |

## Open questions awaiting the owner
| ID | Question | Why it matters | Status |
|---|---|---|---|
| Q1 | Is **direct-to-`master` check-in with no PR** a **standing** policy for completed topics, or was it one-off? | A session wrote it into `rules/checkin-and-pr.md` as a standing "Owner instruction", citing a chat instruction no entry here backs. It contradicts `CLAUDE.md` and `aidlc.md`. See review item R1. | **closed** — answered by D6 |
| Q2 | Ticket prefix / issue tracker, integration branch, branch naming, PR-review tool, DB engine | Listed as `TODO:` in the docs (`verify-docs.ps1` lists them) | open |

When the owner answers a question: add a `D<n>` row, mark the question closed, and update the canonical
rule file in the same `rules:` commit.
