# Review recommendations (open items to check and fix)

Findings from the independent review of Topic 5 (commit `4716ac4`, 2026-10-09) plus process gaps found
along the way. Each item says where it is, the evidence, the suggested fix, and how to prove it is fixed.

**Status values:** `open` · `fixed` · `wont-fix` (with reason) · `needs-owner` (blocked on a decision in
[`decisions.md`](decisions.md)). `scripts/verify-docs.ps1` counts items still `open`/`needs-owner`.

## How to use this file — prompt to give a fixing session
> Read `CLAUDE.md`, then `docs/rules/governance.md` and `docs/review-recommendations.md`. For EACH item:
> (1) first re-verify the finding is still true (read the cited file/line; run the cited command) — if it
> is not true any more, set `Status: wont-fix (already resolved)` with evidence; (2) fix it exactly as
> suggested or propose a better fix and say why; (3) run the "Verify by" check; (4) set `Status: fixed`
> and add a one-line result. Items marked `needs-owner` must NOT be fixed until the owner's answer is in
> `docs/decisions.md` — ask the owner, don't guess. Protected files (see `rules/governance.md` §1) may be
> edited here **only because the owner asked for this fix session**; put those edits in a separate
> `rules:` commit. Finish with the full verification report (`rules/verification.md`) and do NOT push
> until the owner approves.

---

## A. Findings from the Topic 5 review

### R1 — Direct-to-`master` rule added without a verifiable owner decision  `BLOCKER`
- **Status:** fixed
- **Result:** owner chose standing policy (D6); `checkin-and-pr.md` is the single source, `CLAUDE.md` and `aidlc.md` link to it, "Owner instruction" wording removed
- **Where:** `docs/rules/checkin-and-pr.md` lines 13–15 ("never commit directly to `master` — except a
  completed topic, per the owner instruction below" + "Owner instruction (given while finishing
  Topic 5): … check it in directly to `master` — no PR for topic work").
- **Evidence:** it loosens a human gate and cites an instruction that has no entry in `decisions.md`
  (open question Q1). It contradicts, untouched: `CLAUDE.md` ("Work on a branch only; never commit,
  push, or raise a PR on your own"; "Approvals are separate…"), `docs/aidlc.md` line 4 and steps 7–8
  (human approves commit/push/PR; PR review), and `checkin-and-pr.md`'s own "Ask first" and "## PR"
  sections. The commit also landed on `master` before its independent review.
- **Decision needed (owner):** (a) *Keep* direct-to-`master` for completed topics, or (b) *revert* to
  branch + PR.
- **Suggested fix:** record the answer in `decisions.md` (new `D<n>`, close Q1). Then in ONE `rules:`
  commit make `checkin-and-pr.md` the single source and reconcile `CLAUDE.md` + `aidlc.md` to link to it.
  If (a): state the exact conditions (all 5 layers passed, **layer 4 done before the push**, report
  delivered, owner said "push"), keep "approvals are separate" for commit/push, and drop the words
  "Owner instruction…" from the rule text (the decision lives in `decisions.md`). If (b): delete lines
  14–15 and the "— except a completed topic…" clause.
- **Verify by:** `grep -rn "master" CLAUDE.md docs/aidlc.md docs/rules/checkin-and-pr.md` shows no two
  files disagreeing; `scripts/verify-docs.ps1` passes.

### R2 — Wrong cross-reference in a Flow diagram  `SHOULD-FIX`
- **Status:** fixed
- **Result:** footer now cites Topic 3's pass-by-value `int` (verified in `MethodsAndParametersDemo.cs`)
- **Where:** `ArchitectureDiagrams/OOP/Flow/4-ReferencesVsCopies.svg` line 54 (footer): "Compare int in
  Topic 1, where = really did copy the value".
- **Evidence:** `ProgrammingBasics/ProgrammingBasics.cs` has no int-to-int copy (it declares `int a = 10;
  int b = 3;` as separate values). Value-vs-reference passing is taught in Topic 3 (Parameters).
- **Suggested fix:** change to "Compare an `int` (a value type): `int b = a;` really does copy the value
  — see Topic 3's value-vs-ref parameters" or drop the sentence. Don't cite a topic that doesn't show it.
- **Verify by:** read the cited topic's code and confirm the claim; re-render
  (`scripts/render-diagrams.ps1 -Topic OOP -Only 'Flow/4*'`) and confirm the footer still fits.

### R3 — Memory/4 breaks the locked legend colour without recording it  `SHOULD-FIX`
- **Status:** fixed
- **Result:** both variations recorded in `verification.md` accepted exceptions and `diagram-standards.md` "Sanctioned variations"
- **Where:** `ArchitectureDiagrams/OOP/Memory/4-ReferencesVsCopies.svg` line 29: shared Heap box stroke
  `#cc0000` instead of the locked Heap stroke `#e69138`. Also `Memory/5-StaticMembers.svg` uses
  `viewBox` width 1060 and a widened Static box (mentioned only as a note in `ROADMAP.md`).
- **Evidence:** the red outline is a good teaching device (emphasises "one object, two pointers") but
  it is not in the accepted-exceptions list in `rules/verification.md`, so the next reviewer can't tell
  intent from error. The script passes only because other boxes keep the locked colours.
- **Suggested fix:** keep the red stroke and record it: add Memory/4 (red stroke on the shared object) and
  Memory/5 (wider Static column) to `verification.md`'s accepted-exceptions list and to
  `diagram-standards.md`'s "Locked colour legend" notes as the sanctioned way to highlight sharing — OR
  restore `#e69138` and use a callout instead. Either is fine; undocumented is not.
- **Verify by:** exceptions list names both files; `verify-topic.ps1 -Topic OOP` still 0 FAIL/0 WARN.

### R4 — Unverified "rendered in a browser and checked" claim  `SHOULD-FIX`
- **Status:** fixed
- **Result:** all 13 OOP diagrams rendered with headless Chromium (Edge not available here) and viewed; wording in the archived working doc corrected
- **Where:** `docs/todo/archive/oop.md` line 27.
- **Evidence:** nothing records which of the 13 diagrams were viewed. In the 2026-10-09 review only 4
  (Memory 1/4/5, Flow 2) were rendered and viewed; all were fine, the other 9 are text-checked only.
- **Suggested fix:** run `scripts/render-diagrams.ps1 -Topic OOP`, open every PNG, tick the layer-3
  checklist for each, then reword the line to state exactly what was done ("13/13 rendered with
  render-diagrams.ps1 and viewed" or "N/13 viewed; the rest text-checked").
- **Verify by:** the line names the script and the count; the count is true.

### R5 — Flow diagrams don't follow the written Flow rules  `SHOULD-FIX` (decide direction)
- **Status:** fixed
- **Result:** owner chose amend-the-rule (D7); `diagram-standards.md` section 2 "Metaphor Flows"
- **Where:** `OOP/Flow/1-ClassesAndObjects.svg`, `2-Constructors.svg`, `4-ReferencesVsCopies.svg`.
- **Evidence:** `rules/diagram-standards.md` says Flow notes name no variables (those belong to Memory)
  and every call has a matching return; these three put variable names in the body (`anitaAccount`,
  `fullAccount`, `accountA`) and draw no return arrow for `new`. Flows 1, 2, 4 also skip real print steps
  (Flow 1 never draws the two `Describe` prints before the deposit); footers cover this.
- **Suggested fix:** the metaphor-flowchart style is an accepted precedent (Topics 2–4 do it too), so
  prefer amending the **rule text** to say: metaphor Flows may name the variable a result lands in and
  may show the return as the object arriving at its tray; but every real `Console.*` call must still be
  shown or footnoted. (Protected-file edit — owner asked for this fix session, so allowed in a `rules:`
  commit.) Alternative: change the three diagrams.
- **Verify by:** rule text and the OOP diagrams agree; older Flow diagrams are not now "violations".

### R6 — Run-on sentence in diagram-standards  `NIT`
- **Status:** fixed
- **Result:** Static-column bullet split into sentences
- **Where:** `docs/rules/diagram-standards.md` line 189 onward (the Static-column bullet).
- **Suggested fix:** split into 2–3 sentences; keep the content (empty note until a method uses a `static`
  member; first done in Topic 5 `Memory/5`; Static column may widen to hold fields).
- **Verify by:** reads cleanly; no content lost.

### R7 — Flow/5 first step only noted, not drawn  `NIT`
- **Status:** fixed
- **Result:** `Console.WriteLine(… Bank.Name …)` is now a drawn step reading from the board
- **Where:** `OOP/Flow/5-StaticMembers.svg` line 12 ("first (not drawn): Bank.Name is read straight…").
- **Suggested fix:** draw the `Console.WriteLine(Bank.Name)` step as a real stop on the route, or leave
  as is and say why in the footer. Low priority.

### R8 — Flow/4 box fed from only one object  `NIT`
- **Status:** fixed
- **Result:** second feeder arrow from object #1 into the `accountA, accountC` box (right-angle routing; render checked)
- **Where:** `OOP/Flow/4-ReferencesVsCopies.svg` — the `ReferenceEquals(accountA, accountC)` box is fed
  only from object #2 although it compares A and C.
- **Suggested fix:** add a second feeder arrow (right-angle routing only) or relabel. Re-run
  `verify-topic.ps1` (diagonal check) and re-render.

### R9 — Learner comments skip three syntax points  `NIT`
- **Status:** fixed
- **Result:** comments added for `?:`, `=>` and `ReferenceEquals`
- **Where:** `OOP/BankAccount.cs` line 22 (`? :` ternary), lines 32/34 (`=>` expression-bodied
  members), `OOP/OOPDemo.cs` lines 87–88 (unqualified `ReferenceEquals`).
- **Suggested fix:** add a one-line comment at each saying what the symbol means in plain words
  (`?:` = "if … then … else" in one line; `=>` = "this property just returns …"; `ReferenceEquals`
  asks "are these the very same object?"). Audience is freshers.
- **Verify by:** `dotnet build` 0 warnings; `dotnet run` output unchanged; `verify-topic.ps1 -Topic OOP` clean.

---

## B. Older-topic cleanups (optional, from the WARN baseline)
### R10 — Known gaps in earlier diagrams  `NIT`
- **Status:** open
- `Collections/Flow/4-CollectionSafety.svg`: two `<line>`s drift 1px off straight → snap to exact
  horizontal/vertical.
- `Collections/Memory/1-Arrays.svg`: loop variable `score` is not shown → add it to the snapshot or note
  why it is out of scope.
- `MethodsAndParameters/Memory/1-MethodBasics.svg`: local `total` not shown → add it.
- **Verify by:** the three matching WARNs disappear from `verify-topic.ps1 -All -SkipBuild`; then lower
  the WARN baseline number in `rules/verification.md` (recording, allowed).

### R11 — Open `TODO:` decisions  `needs-owner`
- **Status:** needs-owner
- Ticket prefix/tracker, integration branch, branch naming, PR-review tool, DB engine/folder, diagram
  draft/approval step. Listed by `verify-docs.ps1`. Owner answers go in `decisions.md` (Q2).

---

## C. Process gaps and the measures that now address them
| Gap seen | Measure | Where | Status |
|---|---|---|---|
| A session turned a chat instruction into a standing rule | Protected files + "propose, don't edit"; owner decisions only in `decisions.md`; instructions are one-off unless "from now on" | `rules/governance.md` §1–4, `decisions.md` | **done** |
| Rule edit contradicted untouched docs | One canonical file per policy; update all restatements in the same commit; "if docs disagree, stop" | `rules/governance.md` §5–6 | **done** |
| Loosening not visible | `verify-docs.ps1` WARNs on any protected-file change in working tree / last commit | `scripts/verify-docs.ps1` | **done** |
| "Checked in a browser" with no proof | Evidence rule + a script that renders SVG→PNG so the review can really look | `rules/governance.md` §7, `scripts/render-diagrams.ps1` | **done** |
| Independent review after the push | Order rule: layers 1–5 incl. layer 4 before any push; late review = reported deviation | `rules/governance.md` §8, `rules/verification.md` | **done** |
| Reviewer didn't check rule edits for loosening | Reviewer brief now audits rule-file edits and the decisions log | `rules/verification.md` layer 4 | **done** |
| Open review items forgotten | `verify-docs.ps1` counts `open`/`needs-owner` items in this file | `scripts/verify-docs.ps1` | **done** |
| Nothing *prevents* a session pushing to `master` | Only convention + after-the-fact warnings. A real guard needs GitHub branch protection (require PR / block force-push) or a git `pre-push` hook. **Needs owner choice** — it conflicts with the direct-to-master option in R1. | GitHub settings / `.git/hooks` | **needs-owner** |
| Docs can't see an unrecorded owner decision | `verify-docs.ps1` cannot verify who said what; only the owner can confirm `decisions.md` entries are true | — | accepted limit |

### R12 — Decide on a hard guard against unreviewed pushes  `needs-owner`
- **Status:** needs-owner
- Options: (1) GitHub branch protection on `master` requiring a PR (strongest; incompatible with
  direct-to-master unless you allow yourself to bypass); (2) a local `pre-push` hook in `.git/hooks`
  that runs the verify scripts and refuses on FAIL (stops mistakes, not a determined session; hooks are
  per-clone and untracked, so ship it as `scripts/pre-push.sample` and document installing it);
  (3) rely on the governance rules + warnings only. Recommended: (2) now, plus (1) if you move back to
  PR-based topics.

---

## Closing checklist for the fixing session
- [ ] Every item above re-verified, then fixed / wont-fix / left `needs-owner` with its status updated.
- [ ] `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-topic.ps1 -All` → 0 FAIL, WARN
      count equals the baseline in `verification.md` (or lower, with the baseline lowered to match).
- [ ] `scripts/verify-docs.ps1` → 0 FAIL; the open-items WARN count matches what is really left.
- [ ] Protected-file edits are in their own `rules:` commit and listed in the report.
- [ ] Layer-4 independent review done **before** any push; report states each layer PASS/WARN/FAIL/SKIPPED.
- [ ] Nothing pushed until the owner says so.
