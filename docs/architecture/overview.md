# Architecture — code shape (read before touching code)

## Solution layout
```
BasicToAdvancedLearning.sln
BasicToAdvancedLearning.Console/            ← the only project today (net9.0 console app)
   ├─ BasicToAdvancedLearning.Console.csproj   (RootNamespace = BasicToAdvancedLearning — see gotcha 1)
   ├─ Program.cs                               (fixed runner; depends only on ILearningTopic)
   ├─ Interfaces/ILearningTopic.cs             (void Explain();)
   └─ <TopicFolder>/
        ├─ <Topic>Demo.cs                      (the lesson: public ExplainX() per sub-concept)
        └─ <Topic>Example.cs                   (internal ILearningTopic adapter calling the demo)
scripts/                                        ← verify-topic.ps1, verify-docs.ps1, render-diagrams.ps1 (see docs/rules/verification.md)
ArchitectureDiagrams/<TopicFolder>/            ← mandatory SVGs per topic (see rules/diagram-standards.md)
docs/                                          ← this documentation tree
```
Existing topic folders: `ProgrammingBasics/`, `ControlFlow/`, `MethodsAndParameters/`, `Collections/`, `OOP/`.
Note the Topic 1 files are named `ProgrammingBasics.cs` / `ProgrammingBasicsExample.cs` (no `Demo`
suffix) — newer topics use `<Topic>Demo.cs`; new topics follow the newer convention. A topic may also hold
helper-class files beside its demo (Topic 5: `BankAccount.cs`, `Bank.cs`) as long as they define no `ExplainX()` methods.

## Where each kind of logic lives
| Kind | Lives in |
|---|---|
| Teaching code + explanatory comments | `<Topic>Demo.cs` — no knowledge of `ILearningTopic` |
| Runner plumbing (call order of `ExplainX()` methods) | `<Topic>Example.cs` |
| Choosing which topic runs | `Program.cs` — the one `new <Topic>Example()` line |
| The contract between runner and topics | `Interfaces/ILearningTopic.cs` |
| Visual explanation | `ArchitectureDiagrams/<TopicFolder>/` |

## Namespaces
- Every topic: `namespace BasicToAdvancedLearning.<Topic>;`. Interface: `BasicToAdvancedLearning.Interfaces`.
- `Program.cs` has `using` lines for the interface namespace and the active topic's namespace.

## Gotchas (code)
1. **No namespace segment named `Console`.** It shadows `System.Console` and breaks unqualified
   `Console.WriteLine` (CS0234). The `.csproj` sets `<RootNamespace>BasicToAdvancedLearning</RootNamespace>`
   to avoid the default `BasicToAdvancedLearning.Console`. Full story in `ROADMAP.md`.
2. **`Program.cs` never references a concrete topic except in the one `new` line** — don't add logic there.
3. `ILearningTopic` and the `*Example` adapters are `internal`; demos are `public`.
4. TODO: add further gotchas here as later phases (SQL, EF Core, Web API, React) add projects.

## Future shape
Later phases add projects/folders (Web API, React client, database scripts). When one is introduced,
add it to this file's layout and create a spec category for it.
