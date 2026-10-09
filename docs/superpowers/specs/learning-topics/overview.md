# Learning topics — overview

**Start here:** a *learning topic* is one lesson in the curriculum — a `Demo` class that teaches, an
`Example` adapter that runs it, and a mandatory set of SVG diagrams. This spec describes how that is
built today; the topic status list lives in [`../../../ROADMAP.md`](../../../ROADMAP.md).

## Sub-parts
| Doc | Covers |
|---|---|
| [topic-anatomy.md](topic-anatomy.md) | Exact files, namespaces, `ILearningTopic` wiring, and the diagram set a topic consists of |

TODO: further topic docs once there is enough to say — e.g. `diagram-conventions.md` (a spec-level
digest of `rules/diagram-standards.md`) and one doc per topic's teaching sequence.

## Architecture at a glance
```
Program.cs ──new──> <Topic>Example : ILearningTopic
                         │ Explain()
                         ├──> <Topic>Demo.ExplainA()   ──> Console output
                         ├──> <Topic>Demo.ExplainB()   ──> Console output
                         └──> ...
ArchitectureDiagrams/<Topic>/   (one Flow + one Memory SVG per ExplainX(), same number)
```

## Shipped topics
| # | Topic | Code folder | Demo methods |
|---|---|---|---|
| 1 | Programming Basics | `ProgrammingBasics/` | variables & data types, operators, input/output |
| 2 | Control Flow | `ControlFlow/` | if/else, switch, for, while, do-while, foreach |
| 3 | Methods & Parameters | `MethodsAndParameters/` | method basics, parameters (value/ref/out), optional & named, `params`, overloading |
| 4 | Collections | `Collections/` | arrays, `List<T>`, `Dictionary<TKey,TValue>`, collection safety, choosing a collection |

## Running it locally
- Prerequisite: .NET 9 SDK.
- From `BasicToAdvancedLearning.Console/`: `dotnet build` then `dotnet run`. `Program.cs` runs exactly
  one topic (currently `CollectionsExample`); change the type after `new` to run another.
- Topics that call `Console.ReadLine()` (Programming Basics) need stdin — pipe input when running
  non-interactively.
- Diagrams are plain SVG: open in any browser. Verify with `powershell -ExecutionPolicy Bypass -File scripts/verify-topic.ps1 -Topic <Topic>` (and `scripts/verify-docs.ps1` for the docs).

## Configuration reference
None. There are no config keys, environment variables, or connection strings yet.

## Known gaps / deliberate scope cuts
- No unit-test project; verification is build + run + `scripts/verify-topic.ps1` + manual and independent review (see `rules/verification.md`). The script checks structure and rules, not visual quality or the truth of a diagram.
- `Program.cs` runs one topic at a time — no menu to pick a topic at runtime.
- Topic 1 file names (`ProgrammingBasics.cs`, `ProgrammingBasicsExample.cs`) predate the
  `<Topic>Demo.cs` convention.
- Open question: whether to add a runtime topic picker or keep the deliberate one-line swap (it doubles
  as the polymorphism teaching example).

## Real bugs found & fixed (as general lessons)
- **A namespace segment named like a BCL type shadows it.** `...Console.<Topic>` made unqualified
  `Console.*` resolve to the namespace. Fixed with `<RootNamespace>` in the `.csproj`. Lesson: don't
  name namespace segments after types you call unqualified.
- **A diagonal connector can silently cut through a shape.** A loop-back path ran over a box and the
  arrow looked like it "ended nowhere". Lesson: right-angle routing only, and trace every path against
  every box's bounding box.
- **A centred label on its own arrow looks broken.** The line shows through the letter gaps. Lesson:
  place labels beside or above the line, never on it.
