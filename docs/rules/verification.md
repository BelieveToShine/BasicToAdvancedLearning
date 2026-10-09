# Rule: Verification & review (cross-check before anything is "done")

"Done" is never self-declared. Every unit of work passes **five layers**, in order. A layer that
cannot be run is reported as skipped — never silently assumed to pass.

| Layer | What | Who/what runs it | Catches |
|---|---|---|---|
| 1 | Build + run | `dotnet build` (0 errors, 0 warnings) and `dotnet run` | Compile errors, namespace clashes, output that contradicts the comments |
| 2 | Automated topic check | `scripts/verify-topic.ps1` | Missing/misnumbered diagrams, bad XML, diagonal connectors, missing locked colours, code variables absent from Memory diagrams, namespace/wiring/tracker slips |
| 3 | Manual diagram review | Author, against the checklist below | What scripts can't judge: text fit, label clearance, clarity, accuracy of the picture vs the code |
| 4 | Independent review | A **fresh-context** reviewer (separate agent/session) | The author's blind spots — it re-derives expectations from the code, not from the author's summary |
| 5 | Docs check | `scripts/verify-docs.ps1`, then human approval | Broken links, unindexed docs, stale trackers, forbidden patterns |

## Layer 1 — Build + run
```
dotnet build                                          # 0 errors AND 0 warnings
dotnet run --project BasicToAdvancedLearning.Console  # pipe stdin if the topic calls Console.ReadLine()
```
(On a machine that only has a newer .NET runtime than the project's `net9.0`, set `DOTNET_ROLL_FORWARD=Major`
before `dotnet run`; the scripts also run under PowerShell 7 as `pwsh -NoProfile -File scripts/<script>.ps1`.)
(Written to work in PowerShell 5.1 and Git Bash alike — don't chain with `&&`, PowerShell 5.1 rejects it.)
Read the real output against what the code comments and diagrams claim. A green build alone is not done.

## Layer 2 — Automated topic check
```
powershell -ExecutionPolicy Bypass -File scripts/verify-topic.ps1 -Topic <Topic>    # one topic
powershell -ExecutionPolicy Bypass -File scripts/verify-topic.ps1 -All -SkipBuild    # regression over every topic
```
- **FAIL** blocks check-in. **WARN** must each be either fixed or explained in the PR/working doc.
- It derives the expected diagram list from the *order `Explain()` calls the demo methods*, so a
  renamed/reordered method that leaves stale diagrams is caught.
- **WARN baseline** — `-All -SkipBuild` currently gives 0 FAIL / 10 WARN. Your change must not add to
  this list; anything else is yours to fix or explain.
  - *Accepted exceptions (don't "fix"):*
    - `Memory/2-Parameters.svg` (MethodsAndParameters): three-panel value/ref/out layout, so no Heap
      colour and no Main/Explain frames (3 WARNs) — see `ROADMAP.md` session notes.
    - `OOP/Memory/4-ReferencesVsCopies.svg`: red `#cc0000` stroke on the shared Heap object (emphasises one
      object, two pointers). The locked fills are intact, so no WARN — intent recorded here.
    - `OOP/Memory/5-StaticMembers.svg`: wider Static column and a 1060px viewBox to hold real static fields.
    - `Program.cs does not currently run <Topic>Example` (4 WARNs, one per older topic): only the
      active topic is wired; it only matters for the topic being finished.
  - *Known gaps in older topics (candidate cleanups, not blockers for new work):*
    - `Collections/Flow/4-CollectionSafety.svg`: two lines drift 1px off straight.
    - `Collections/Memory/1-Arrays.svg` doesn't show the loop variable `score`.
    - `MethodsAndParameters/Memory/1-MethodBasics.svg` doesn't show `total`.
  - Keep this list current: when a topic adds an accepted exception or a cleanup is done, edit it here.
- Script constraints: the demo class (the file defining the public `ExplainX()` methods) may sit
  beside helper-class files; `ExplainX()` methods must be public, `void`, parameterless. Helper
  classes may live in other `.cs` files in the topic folder.
- When a check proves wrong or too loose, fix the script in the same change (like rules, it grows).

## Layer 3 — Manual diagram checklist (per diagram)
Render the diagrams and LOOK at each one — don't tick from the source text:
```
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/render-diagrams.ps1 -Topic <Topic>    # PNGs in %TEMP%\bta-render\<Topic>
```
Open every PNG (the image reader works on them) or the SVG in a browser, and tick. In the report state
exactly how many diagrams you viewed; any you did not view are "not visually verified"
(`rules/governance.md` §7). Checklist:
- [ ] Title + one-line subtitle + a legend are present; Arial; viewBox fits the content (no clipping).
- [ ] Every label's text fits inside its box; no text overlaps other text.
- [ ] No label sits on the line it describes (a label may brush at most one character of a line).
- [ ] Every connector is straight/right-angle; none passes through a box or clips an oval's curve.
- [ ] Flow: every call has a matching return; notes name the `Console.*` methods used; `alt` boxes only
      for real branches; ends with an End marker and a one-line "where to where" footer.
- [ ] Memory: Stack/Heap/Static columns use the locked colours; 3 frames (method → Explain() → Main);
      value types inline, reference types point into the Heap; empty-Heap/Static boxes say so.
- [ ] Names and values in the picture are the **real** ones from that method's code, not placeholders.
- [ ] The architecture diagram's boxes map 1:1 to the `ExplainX()` methods.
- [ ] Animation (if any) is slow (2–4 s), loops, and animates only the one thing that needs motion.

## Layer 4 — Independent review (fresh context)
The author reviewing their own work shares its blind spots. Before the PR, start a **separate** reviewer
(a new Claude session, or a subagent that has not seen the build conversation) with this brief:

> You are a REVIEWER, not the builder. Do not implement, edit, stage or commit anything, and ignore
> `CLAUDE.md`'s "do the work / update the spec" steps — they are for the author.
> Repo: `<absolute repo path>`. Topic `<Topic>` was just built on branch `<branch>`; the work is
> **uncommitted in the working tree** — review the working tree (`git status`, `git diff`), not history.
> Read `docs/rules/verification.md` and `docs/rules/diagram-standards.md` first. Do NOT trust the
> author's summary; derive expectations from the code yourself.
> 1. Read `<Topic>Demo.cs` (+ helper classes) and `<Topic>Example.cs`; list every method, every local
>    variable and its type; confirm `Program.cs` wires `<Topic>Example` and only the `using` + `new`
>    line changed.
> 2. For each method open `Flow/<N>-*.svg` and `Memory/<N>-*.svg` as text/XML. Confirm each variable's
>    Stack/Heap/Static placement is *correct C#/.NET semantics*, the flow matches the real call order
>    and branches, and real names/values from the code are used. (You cannot render SVG: check
>    coordinates/text lengths against box sizes, and say explicitly which visual checklist items you
>    could NOT verify so the author does them in a browser.)
> 3. Run `scripts/verify-topic.ps1 -Topic <Topic>` and `scripts/verify-docs.ps1` (PowerShell:
>    `powershell -NoProfile -ExecutionPolicy Bypass -File ...`) and report new WARNs vs the baseline in
>    `verification.md`.
> 4. Check the `ROADMAP.md` tracker row (✅, `` `<Folder>/` `` cell), the learning-topics spec table,
>    and the other hard-coded "topics 1–N" claims were updated.
> 5. Check the learner-facing comments are accurate and the running example is consistent.
> 6. **Audit protected-file edits** (`git show --stat`, and `git diff` of `CLAUDE.md`, `docs/rules/**`,
>    `docs/aidlc.md`, `scripts/*.ps1`, `docs/decisions.md`): was anything loosened (an approval, a layer, a
>    check, a branch/PR requirement)? Does any rule cite an "owner instruction" with no matching entry in
>    `docs/decisions.md`? Do any two docs now contradict each other (grep `master`, `PR`, `approval`)?
>    Were those edits in their own `rules:` commit and reported? Quote everything you flag.
> 7. Check every "verified/checked/rendered" claim in the commit message and working doc says how and
>    what it covered (`rules/governance.md` §7).
> Report each finding as **blocker / should-fix / nit**: file, what's wrong, evidence. End with a
> one-line verdict.

Launch it with the Agent tool (`subagent_type: general-purpose`) passing that brief as the prompt — a
subagent starts with no memory of the build conversation, which is the point. The author triages every
finding: fix, or reply with reasoning. Re-run layers 1–2 after fixes. For code changes, `/code-review`
may also be run on the diff.

## Layer 5 — Docs check
```
powershell -ExecutionPolicy Bypass -File scripts/verify-docs.ps1
```
The script checks that links resolve, categories are indexed, and the "topics 1–N done" claims agree
with the ROADMAP tracker. It cannot see other stale facts, so go through this **stale-fact checklist**
by hand after a topic ships — each of these hard-codes something that changes:
- `docs/ROADMAP.md`: status snapshot (date, "Done this session"), "Next" paragraph, the tracker row.
- `docs/basic-to-advanced-learning-overview.md`: "Currently done" / "Next" lines.
- `docs/superpowers/specs/README.md`: category-index row ("Covers topics 1–N").
- `docs/superpowers/specs/learning-topics/overview.md`: shipped-topics table, and the line saying which
  topic `Program.cs` currently runs.
- `docs/architecture/overview.md`: "Existing topic folders".
- `docs/README.md`: link map, if a doc was added/renamed.

Then read, as a human, the files the work should have touched and confirm each one actually changed:
- [ ] `docs/ROADMAP.md` — tracker row ✅ with folder + diagram paths; "Status snapshot" and "Next" updated;
      new gotchas recorded.
- [ ] `docs/superpowers/specs/learning-topics/overview.md` — shipped-topics table row added.
- [ ] `docs/README.md` / `CLAUDE.md` — link maps still accurate; nothing new left unlinked.
- [ ] `docs/architecture/overview.md` — updated if a folder/project/convention changed.
- [ ] `docs/todo/` working doc moved to `archive/` after shipping.
- [ ] Open `TODO:` items either resolved or knowingly left (the script lists them all).

## Order: review before push
Layers 1–5, **including the independent layer-4 review**, finish before anything is pushed to a shared
branch or a PR is opened (see `rules/governance.md` §8). A review done after the push is reported as a
deviation, not as a pass. Verification does not authorise the push — that is a separate approval
(`rules/checkin-and-pr.md`).

## After syncing with `master`
If merging/pulling `master` changed anything: re-run layers 1, 2 and 5. Re-run layer 4 only if the merge
touched this topic's files or conflicted with them.

## Reporting
End every unit of work with a verification report: each layer → PASS / WARN (with disposition) /
FAIL / SKIPPED (with reason). Say plainly what was *not* verified (e.g. "no unit tests exist",
"visual checks done on 3 of 14 diagrams"). Never claim a layer passed that wasn't run.

## Acceptance test for this system itself
The docs are only good if a cold session can use them. Periodically (and after any big docs change)
run a **fresh session** with only: "Do the next topic on the roadmap." Success = it reads `CLAUDE.md`,
follows the runbook without asking what the convention is, produces a topic that passes layers 1–5,
and flags anything the docs left ambiguous. Every ambiguity it hits becomes a fix to the docs.
