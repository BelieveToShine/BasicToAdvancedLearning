# Topic anatomy

Back to [overview](overview.md). Defines exactly what a finished topic consists of.

## Files per topic
| File | Role |
|---|---|
| `BasicToAdvancedLearning.Console/<Topic>/<Topic>Demo.cs` | `public class <Topic>Demo` — the lesson (helper/model classes may live in extra `.cs` files beside it). One public `ExplainX()` per sub-concept; each prints a heading and demonstrates with commented code. Has no knowledge of `ILearningTopic`. |
| `BasicToAdvancedLearning.Console/<Topic>/<Topic>Example.cs` | `internal class <Topic>Example : ILearningTopic` — `Explain()` calls the demo's methods in teaching order. |
| `ArchitectureDiagrams/<Topic>/<Topic>.svg` | Architecture/concept diagram, boxes mapped 1:1 to the `ExplainX()` methods. |
| `ArchitectureDiagrams/<Topic>/Flow/0-Overview.svg`, `Flow/<N>-<Method>.svg` | Call/return trace diagrams. |
| `ArchitectureDiagrams/<Topic>/Memory/0-Overview.svg`, `Memory/<N>-<Method>.svg` | Stack/Heap/Static snapshots using the locked legend. |

Example count: a topic with 6 methods = 1 architecture + 2 overviews + 6 Flow + 6 Memory = 15 SVGs
(`ControlFlow/` is the reference example).

## Wiring
`Program.cs` holds `using BasicToAdvancedLearning.<Topic>;` and
`ILearningTopic topic = new <Topic>Example(); topic.Explain();` — in `Program.cs` only the `using` line and the type after `new` change (see ROADMAP runbook step 3).

## Numbering
`<N>` follows the order `<Topic>Example.Explain()` calls the methods. `Flow/<N>` and `Memory/<N>` always
describe the same method.

## Why this shape (rejected alternatives)
- **One class holding both lesson and runner plumbing** — rejected: it mixes teaching code with
  interface wiring juniors don't need yet.
- **A runtime menu in `Program.cs`** — rejected for now: the one-line type swap is itself a live
  polymorphism example, and keeps `Program.cs` fixed.
- **GIFs for loops** — rejected: no tooling on the machine; animated SMIL SVG stays text/diffable.
- **One shared Memory diagram per topic** — rejected: a learner stuck on one method should open exactly
  one file about it.

## Definition of done (per topic)
All five layers of `docs/rules/verification.md` pass: demo + adapter compile with 0 warnings and `dotnet run` output matches the comments; `scripts/verify-topic.ps1` has 0 FAIL (WARNs explained); every method has both diagrams and the manual checklist is ticked; an independent fresh-context review found nothing unresolved; `scripts/verify-docs.ps1` passes; `ROADMAP.md` tracker row is ✅ and this spec shipped-topics table is updated.
