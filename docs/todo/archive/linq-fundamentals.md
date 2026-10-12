# Topic 9 — LINQ fundamentals (working doc)

Plan, decisions and status for Topic 9, the last topic of Phase 1. Once shipped, the behaviour is folded into
`ROADMAP.md` and `superpowers/specs/learning-topics/overview.md`; this file stays as the record of the plan.

## Plan (as built)
- **Folder / namespace:** `LinqFundamentals/` and `BasicToAdvancedLearning.LinqFundamentals`;
  `LinqFundamentalsDemo.cs`, `LinqFundamentalsExample.cs` (no extra class files).
- **Running example:** a `List<decimal>` of bank amounts (positive = deposit, negative = withdrawal), rebuilt fresh in
  each method by a private `CreateAmounts()` helper.
- **Methods, in call order:** `ExplainWhereAndSelect`, `ExplainOrderingAndAggregates`, `ExplainAskingQuestions`,
  `ExplainDeferredExecution`.
- **Memory diagrams:** locked 3 frames and columns, no exceptions. A List is one Heap box of cells (Topic 4's
  precedent); `Memory/4` draws an unrun query as an iterator (pointer to the list + pointer to the lambda).
- **Flow metaphors:** conveyor with a filter gate · scoreboard · quiz show · recipe card and pantry.
- **Diagram list (11):** `LinqFundamentals.svg`, `Flow/0-Overview.svg`, `Memory/0-Overview.svg`, and
  `Flow/<N>-<Method>.svg` + `Memory/<N>-<Method>.svg` for N = 1–4.

## Status
- [x] Code: demo + adapter, `Program.cs` wired
- [x] 11 diagrams drawn
- [x] Docs: ROADMAP tracker + snapshot + gotchas 13–14 + Phase 2 handoff, learning-topics spec, overview, architecture, R13
- [x] Layer 1: `dotnet build` 0 warnings / 0 errors; `dotnet run` output compared with the comments and diagrams
- [x] Layer 2: `verify-topic.ps1 -Topic LinqFundamentals` = 0 FAIL / 0 WARN; `-All -SkipBuild` = 0 FAIL / 14 WARN (baseline)
- [x] Layer 3: headless-Chromium PNG renders (no Edge here) viewed one by one — `LinqFundamentals.svg`, `Flow/0`–`Flow/4`,
      `Memory/0`–`Memory/4`, 11 of 11 (Flow/4 and the architecture diagram re-viewed after the review's wording tweaks)
- [x] Layer 4: one fresh-context subagent review before any push — no blockers, no should-fix; its nits were applied in a
      follow-up commit
- [x] Layer 5: `verify-docs.ps1` = 0 FAIL
