# Rule: Governance — who may change the rules, and how claims are made

Why this exists: a session once wrote "Owner instruction: check topics in directly to `master`, no PR"
into a rule file as a standing rule. The claim could not be verified, it loosened a human gate, it
contradicted `CLAUDE.md` and `aidlc.md` (which it did not touch), and the independent review ran only
*after* the push. Every rule below closes one of those gaps. These rules apply to every session,
including ones the owner is not watching.

## 1. Protected files (the "constitution")
| Path | Why protected |
|---|---|
| `CLAUDE.md` | Entry point; sets the mandatory workflow every session obeys |
| `docs/rules/**` | Standing rules, gates and checklists |
| `docs/aidlc.md` | The approval/verification flow |
| `docs/decisions.md` | The log of owner decisions (append-only) |
| `scripts/*.ps1` | The automated checks — weakening them weakens every gate |

## 2. Changing a protected file
- Edit one **only** when the human, in *this* session, explicitly asks for that specific change. A
  feature task (a new topic, a fix) is never a reason to edit a protected file — if the work reveals a
  rule is wrong or missing, **propose it** by adding an entry to
  [`../review-recommendations.md`](../review-recommendations.md) and tell the human; don't edit.
- Exception: *recording* facts that a rule file already asks you to keep current (e.g. the WARN
  baseline or accepted-exceptions list in `verification.md`, a gotcha in `ROADMAP.md`) is allowed, as
  long as it loosens nothing.
- Protected-file edits go in their **own commit** with the subject prefixed `rules:` — never mixed into
  a feature commit — and the end-of-work report lists every protected file touched and why.
- `scripts/verify-docs.ps1` prints a WARN for any protected file changed in the working tree or the
  last commit, so an unreported edit is visible to the next session and to the reviewer.

## 3. Never loosen a gate on your own
Loosening = removing/skipping an approval, a verification layer, a check, a branch/PR requirement, or
narrowing what the scripts test. Only the owner can do this, and it must be written to
[`../decisions.md`](../decisions.md) first (see 4). "It was faster" and "I was told in chat" are not
enough on their own.

## 4. Instruction provenance — what counts as an owner decision
- A chat instruction is **scoped to what was actually asked**. "Push this to master" authorises that
  push, once. It does not become a standing rule unless the owner says so in words like "from now on".
- Docs must not assert that "the owner said X" anywhere except `docs/decisions.md`. Rule files state
  the rule; `decisions.md` records who decided it, when, the owner's wording, and its scope.
- When the owner gives a decision, the session that received it adds the `decisions.md` entry (quote
  it, mark scope: one-off vs standing) and tells the owner it did.
- A rule edit that cites an owner instruction with no matching `decisions.md` entry is **unauthorised**:
  report it, don't rely on it.

## 5. One source of truth per policy
Restating a policy in several files is how contradictions are born. The canonical home of each policy:
| Policy | Canonical file | Other files |
|---|---|---|
| Branches, commits, who approves what, PRs | `rules/checkin-and-pr.md` | link to it; don't restate details |
| Verification layers, order, evidence | `rules/verification.md` | link to it |
| Diagram look, shape, exceptions | `rules/diagram-standards.md` | `diagrams.md` summarises and links |
| Who may change rules; claims | `rules/governance.md` (this file) | link to it |
| Topic runbook | `ROADMAP.md` | link to it |

When a policy changes: update the canonical file, then `grep -rn` the old wording (and its key words —
e.g. `master`, `PR`, `approval`) across `CLAUDE.md` and `docs/` and fix every restatement **in the same
commit**. Leave no two files disagreeing.

## 6. If two docs disagree, stop
Do not pick the one you like. Stop, tell the human which files and lines conflict, and continue only
after they say which is right.

## 7. Evidence rule for claims
Any "verified / checked / rendered / reviewed" statement — in a commit message, a working doc, or the
final report — must say **how** (the command or script) and **what it covered** (which files/diagrams).
- Layer 3 (visual review) means you looked at the rendered picture: run
  `scripts/render-diagrams.ps1 -Topic <Topic>` and open the PNGs. Say which diagrams you actually viewed;
  the rest are "not visually verified".
- Never write a verification claim you did not execute. A skipped layer is reported as SKIPPED.

## 8. Order matters
Layers 1–5 (including the independent layer-4 review) are complete **before** any push to the shared
branch and before any PR. If a review happens after the push, the report must say so as a deviation,
not as a pass.
