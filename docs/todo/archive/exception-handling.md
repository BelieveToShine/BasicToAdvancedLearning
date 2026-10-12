# Topic 7 — Exception handling (working doc)

Plan, decisions and status for Topic 7. Once shipped, the behaviour is folded into `ROADMAP.md` and
`superpowers/specs/learning-topics/overview.md`; this file stays as the record of the plan.

## Plan (as built)
- **Folder / namespace:** `ExceptionHandling/` and `BasicToAdvancedLearning.ExceptionHandling`;
  `ExceptionHandlingDemo.cs`, `ExceptionHandlingExample.cs`, plus `SafeAccount.cs` and `InsufficientFundsException.cs`.
- **Running example:** `SafeAccount.Withdraw` throws `ArgumentOutOfRangeException` (non-positive amount) or the
  custom `InsufficientFundsException` (carries `Shortfall`). Topic 5's `BankAccount` is untouched.
- **Methods, in call order:** `ExplainTryCatch` (built-in `FormatException`), `ExplainCatchOrder`,
  `ExplainFinally`, `ExplainCustomExceptions`.
- **Memory diagrams:** locked 3 frames for methods 1–3; method 4 shows 4 frames at the instant of the throw
  (recorded exception). Exceptions are ordinary Heap objects; `ex` is a pointer; Static stays empty.
- **Flow metaphors:** safety net · sorting chutes · toll booth · parcel up a stairwell.
- **Diagram list (11):** `ExceptionHandling.svg`, `Flow/0-Overview.svg`, `Memory/0-Overview.svg`, and
  `Flow/<N>-<Method>.svg` + `Memory/<N>-<Method>.svg` for N = 1–4.

## Status
- [x] Code: demo, adapter, two class files, `Program.cs` wired
- [x] 11 diagrams drawn
- [x] Docs: ROADMAP tracker + snapshot + gotchas, learning-topics spec, overview, architecture
- [ ] Verification layers 1–5, then check-in (recorded in the end-of-work report)
