# BasicToAdvancedLearning — Roadmap (living doc)

Tracks what's done, what's next, and how to add a topic without re-explaining the convention from
scratch. Update this file whenever a topic starts, finishes, or the plan changes — see
[`README.md`](README.md) for how this fits the rest of the docs.

## Status snapshot — 2026-08-31

**Solution scaffolded**: `BasicToAdvancedLearning.sln` + `BasicToAdvancedLearning.Console` (net9.0),
under `C:\Vivek\Projects\BasicToAdvancedLearning`. Builds clean, 0 warnings/errors.

**Done this session:**
- Topic 1 (Programming Basics) implemented + explained with a diagram (see table below).
- Settled the runner pattern: `Program.cs` depends only on `ILearningTopic` (`Interfaces/ILearningTopic.cs`),
  never on a concrete topic class — adding a topic never changes `Program.cs`'s shape, and it doubles
  as a live polymorphism example for the juniors.
- Found and fixed a real gotcha: a namespace segment literally named `Console` shadows
  `System.Console` (see "Conventions & gotchas" below) — fixed via `<RootNamespace>` in the `.csproj`.

**Next:** pick Topic 2 (Control Flow, or whichever is next per the phase table) and follow the
"Add a new topic" runbook below.

## Topic tracker

Phases and topics as agreed with Vivek (freshers track: C# Console → SQL → Web API → React).
Status: ✅ Done · 🔶 In progress · ⬜ Pending.

### Phase 1 — C# Fundamentals (Console App)

| # | Topic | Status | Folder | Diagram |
|---|---|---|---|---|
| 1 | Programming basics — variables, data types, operators, input/output | ✅ | `ProgrammingBasics/` | `ProgrammingBasics.svg` + `ProgrammingBasicsFlow.svg` + `ProgrammingBasicsMemory.svg` |
| 2 | Control flow — if/else, switch, loops | ⬜ | | |
| 3 | Methods & parameters | ⬜ | | |
| 4 | Collections — arrays, List, Dictionary | ⬜ | | |
| 5 | OOP — classes, objects, constructors, encapsulation | ⬜ | | |
| 6 | OOP advanced — inheritance, polymorphism, interfaces, abstract classes | ⬜ | | |
| 7 | Exception handling | ⬜ | | |
| 8 | File I/O basics | ⬜ | | |
| 9 | LINQ fundamentals | ⬜ | | |

### Phase 2 — SQL & Database
| # | Topic | Status |
|---|---|---|
| 10 | Relational DB concepts — tables, keys, normalization | ⬜ |
| 11 | SQL basics — SELECT/WHERE/ORDER BY/JOIN/GROUP BY | ⬜ |
| 12 | SQL CRUD | ⬜ |

### Phase 3 — Connecting C# to SQL
| # | Topic | Status |
|---|---|---|
| 13 | ADO.NET basics | ⬜ |
| 14 | EF Core — DbContext, models, migrations | ⬜ |
| 15 | EF Core CRUD (LINQ-to-Entities) | ⬜ |

### Phase 4 — Web API
| # | Topic | Status |
|---|---|---|
| 16 | HTTP & REST fundamentals | ⬜ |
| 17 | ASP.NET Core Web API basics — controllers, routing | ⬜ |
| 18 | CRUD endpoints backed by EF Core + SQL | ⬜ |
| 19 | Testing APIs with Swagger/Postman | ⬜ |
| 20 | Basic auth/authorization (JWT overview) | ⬜ |

### Phase 5 — React Frontend
| # | Topic | Status |
|---|---|---|
| 21 | React fundamentals — JSX, components, props | ⬜ |
| 22 | State & events (useState) | ⬜ |
| 23 | Side effects & data fetching (useEffect) | ⬜ |
| 24 | React Router | ⬜ |
| 25 | Calling the Web API from React | ⬜ |

### Phase 6 — Full Stack Integration
| # | Topic | Status |
|---|---|---|
| 26 | End-to-end mini project (React → Web API → SQL) | ⬜ |
| 27 | Error handling & validation across all layers | ⬜ |
| 28 | (Optional) Deployment basics | ⬜ |

## Conventions & gotchas

Real lessons found while building this, worth knowing so they're never rediscovered the hard way:

1. **Never let a namespace segment be literally `Console`.** `BasicToAdvancedLearning.Console.ProgrammingBasics`
   as a namespace makes the compiler resolve unqualified `Console.WriteLine`/`ReadLine` to that empty
   *namespace* instead of `System.Console` (CS0234) — because `namespace A.Console.B` desugars to
   nested namespaces, and `Console` becomes a visible sibling name inside `B`. Fixed by setting
   `<RootNamespace>BasicToAdvancedLearning</RootNamespace>` in the `.csproj` (drops the `.Console`
   segment) and keeping every topic namespace as `BasicToAdvancedLearning.<Topic>`. Any lesson class
   that calls `Console.*` unqualified must stay off a namespace containing `Console` anywhere in it.
2. **`ILearningTopic` pattern.** Every topic implements `Interfaces/ILearningTopic.cs` (`void Explain();`).
   `Program.cs` only ever does:
   ```csharp
   ILearningTopic topic = new <Topic>Example();
   topic.Explain();
   ```
   Moving to the next topic is a one-line change (swap the type after `new`) — `Program.cs` itself
   never grows or needs re-reading. It's also a working example of interfaces/polymorphism baked
   into the project's own structure.
3. **Two files per topic, not one.** `<Topic>Demo.cs` is the actual lesson (the concepts, with
   explanatory comments) and has no knowledge of `ILearningTopic`. `<Topic>Example.cs` is a thin
   adapter that implements `ILearningTopic.Explain()` and calls into the demo class. Keeps the
   teaching code free of "runner" plumbing.

## Add a new topic — runbook

Repeat this for each topic in the tracker above:

1. **Folder + demo class** — create `BasicToAdvancedLearning.Console/<Topic>/<Topic>Demo.cs`.
   Namespace: `BasicToAdvancedLearning.<Topic>` (no `Console` segment — see gotcha #1). One public
   method per sub-concept (e.g. `ExplainX()`), each printing + explaining via comments, called in
   sequence from the adapter.
2. **Adapter class** — same folder, `<Topic>Example.cs`, namespace `BasicToAdvancedLearning.<Topic>`,
   `internal class <Topic>Example : ILearningTopic` with `Explain()` calling the demo's methods.
3. **Wire up Program.cs** — change only the two lines:
   ```csharp
   using BasicToAdvancedLearning.<Topic>;
   ILearningTopic topic = new <Topic>Example();
   topic.Explain();
   ```
4. **Diagrams — all 3 are MANDATORY, no exceptions.** Follow [`docs/rules/diagram-standards.md`](rules/diagram-standards.md)
   exactly. Create all three under `ArchitectureDiagrams/<Topic>/`:
   - `<Topic>.svg` — architecture/concept (colour-coded boxes mapped 1:1 to the demo's `ExplainX()` methods)
   - `<Topic>Flow.svg` — functional flow (Start → ... → End, real branches only)
   - `<Topic>Memory.svg` — Stack / Heap / Static, using the locked colour legend and real variable
     names from `<Topic>Demo.cs`
   A topic is not ✅ until all three exist and are valid XML.
5. **Build + run** — `dotnet build` then `dotnet run` from `BasicToAdvancedLearning.Console/`
   (pipe stdin for any `Console.ReadLine()` prompts when testing non-interactively).
6. **Update this file** — flip the topic's status to ✅, note the folder/diagram paths in the
   tracker table, and add anything surprising to "Conventions & gotchas".
