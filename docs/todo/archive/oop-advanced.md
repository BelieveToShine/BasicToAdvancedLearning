# Topic 6 — OOP advanced (working doc)

Plan, decisions and status for Topic 6. Once shipped, the behaviour is folded into `ROADMAP.md` and
`superpowers/specs/learning-topics/overview.md`; this file stays as the record of the plan.

## Plan (as built)
- **Folder / namespace:** `OOPAdvanced/` and `BasicToAdvancedLearning.OOPAdvanced`; `OOPAdvancedDemo.cs`,
  `OOPAdvancedExample.cs`, plus the example classes below.
- **Running family:** `Account` (base) → `SavingsAccount`, `CurrentAccount`; `Transaction` (abstract) →
  `DepositTransaction`, `WithdrawalTransaction`; `IInterestBearing` (interface) ← `SavingsAccount`, `FixedDeposit`.
  Self-contained — Topic 5's `BankAccount` is untouched.
- **Methods, in call order:** `ExplainInheritance`, `ExplainPolymorphism`, `ExplainAbstractClasses`, `ExplainInterfaces`.
- **Memory diagrams:** locked 3 frames; a derived object is one Heap box with inherited and added fields captioned
  separately; arrays of parent/interface-typed pointers point at objects of different classes; abstract
  and interface types never get a box; Static stays empty. No locked-shape exception.
- **Flow metaphors:** family tree · universal remote · form with a blank · wall socket and plugs.
- **Diagram list (11):** `OOPAdvanced.svg`, `Flow/0-Overview.svg`, `Memory/0-Overview.svg`, and
  `Flow/<N>-<Method>.svg` + `Memory/<N>-<Method>.svg` for N = 1–4.

## Status
- [x] Code: demo, adapter, six class/interface files, `Program.cs` wired
- [x] 11 diagrams drawn and rendered
- [x] Docs: ROADMAP tracker + snapshot + gotchas, learning-topics spec, overview, architecture
- [ ] Verification layers 1–5 and the check-in — see the report given at the end of the session
