# Topic 5 — OOP (working doc, shipped)

Working doc for Topic 5. Shipped: behaviour is folded into `ROADMAP.md` and
`superpowers/specs/learning-topics/overview.md`; this file is the archived record of the plan.

## Plan (as built)
- **Folder / namespace:** `OOP/` and `BasicToAdvancedLearning.OOP`; files `OOPDemo.cs`, `OOPExample.cs`,
  plus helper classes `BankAccount.cs` and `Bank.cs`.
- **Running example:** a `BankAccount` (owner + balance). `Bank` is a separate static class used only by the
  last method, so Static stays empty in methods 1–4.
- **Methods, in call order:**
  1. `ExplainClassesAndObjects` — class = blueprint, `new` = object, each object owns its fields.
  2. `ExplainConstructors` — constructor, overload chained with `: this(...)`, clamping a bad opening balance.
  3. `ExplainEncapsulation` — private fields, read-only properties, methods that refuse invalid requests.
  4. `ExplainReferencesVsCopies` — `=` copies the pointer; `new` builds a second object; `ReferenceEquals`.
  5. `ExplainStaticMembers` — one shared counter and name on the type (`Bank`), reached via the type name.
- **Memory diagrams:** locked 3 frames; objects drawn as Heap boxes with real field values; `this` and
  constructor chaining carried as footer sentences (no 4th-frame exception). Static column populated only
  in `Memory/5-StaticMembers.svg`.
- **Flow metaphors:** blueprint and stamp · assembly line · teller window in front of a vault · house keys ·
  town noticeboard.
- **Diagram list (13):** `OOP.svg`, `Flow/0-Overview.svg`, `Memory/0-Overview.svg`, and
  `Flow/<N>-<Method>.svg` + `Memory/<N>-<Method>.svg` for N = 1–5.

## Status
- [x] Code: demo, adapter, helper classes, `Program.cs` wired
- [x] 13 diagrams, rendered in a browser and checked
- [x] Docs: ROADMAP tracker + snapshot + gotchas, learning-topics spec, overview, architecture, rules
- [x] Verification layers 1–5 (see the report given at check-in)
