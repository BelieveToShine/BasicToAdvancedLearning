# BasicToAdvancedLearning — Roadmap (living doc)

Tracks what's done, what's next, and how to add a topic without re-explaining the convention from
scratch. Update this file whenever a topic starts, finishes, or the plan changes — see
[`README.md`](README.md) for how this fits the rest of the docs.

## Status snapshot — 2026-08-31

**Solution scaffolded**: `BasicToAdvancedLearning.sln` + `BasicToAdvancedLearning.Console` (net9.0),
(path is machine-specific — see the clone you are in). Builds clean, 0 warnings/errors. Remote:
https://github.com/BelieveToShine/BasicToAdvancedLearning (default branch `master`).

**Done this session:**
- Topics 1–4 (Programming Basics, Control Flow, Methods & Parameters, Collections) implemented +
  explained with diagrams (see table below). `Program.cs` currently runs Topic 4 (`CollectionsExample`).
- Collections' Memory/0-Overview.svg makes a deliberate point of INVERTING the usual framing: every
  earlier topic treated the Heap as the exception worth calling out; here the Heap is the rule
  (every array/List/Dictionary variable is just a Stack pointer) and Memory/5 is the exception
  (a method with no Heap — or Stack — story at all, which is itself worth showing honestly).
- Topic 3's `Memory/2-Parameters.svg` breaks from the strict single-snapshot format (three side-by-side
  mini call/return panels — value vs ref vs out) because a single instant can't show all three modes;
  this is the second precedent for a locked-shape exception (after the for-loop's slot-reuse insight),
  used only when the comparison itself IS the teaching point.
- Settled the runner pattern: `Program.cs` depends only on `ILearningTopic` (`Interfaces/ILearningTopic.cs`),
  never on a concrete topic class — adding a topic never changes `Program.cs`'s shape, and it doubles
  as a live polymorphism example for the juniors.
- Found and fixed a real gotcha: a namespace segment literally named `Console` shadows
  `System.Console` (see "Conventions & gotchas" below) — fixed via `<RootNamespace>` in the `.csproj`.
- Diagram rules matured twice this session — see `docs/rules/diagram-standards.md`: (a) high-level
  diagrams are a floor, not a ceiling — sub-concepts with their own control shape (loops, especially)
  get their own Flow/Memory diagrams; (b) every Flow diagram must use straight, right-angle routing
  only — a diagonal connector caused a real bug (a loop-back path that visibly cut through a box).
- No GIF tooling on this machine — animated SVG (SMIL) is the standing convention for "show repetition
  over time" instead.
- Memory diagrams are now mandatory PER METHOD (not just where the story differs) — retrofitted Topic 1
  to match: it now has the same `Flow/`+`Memory/` folder layout and per-method files as Topic 2.

**Next:** pick Topic 5 (OOP — classes, objects, constructors, encapsulation) and follow the "Add a
new topic" runbook below.

**Decisions to settle in the Topic 5 plan (get the human's approval before building)** — the docs
deliberately do not pre-decide these, so propose an answer for each and ask:
1. Folder/namespace name (e.g. `OOP` vs `ObjectOrientedProgramming`) — this also fixes the tracker
   row's `` `<Folder>/` `` cell and the names of topic 6.
2. The `ExplainX()` method list (the title implies classes/objects, constructors, encapsulation; more
   — references vs copies, `static` — is a judgement call) and the one running example.
3. How the Memory diagrams show instance methods/constructors (`this`, an extra frame) — see
   "Instance methods and constructors" in `rules/diagram-standards.md`. Whether Topic 5 is where the
   Static column finally gets real content.
4. Flow-diagram metaphors (Collections already used lockers, filmstrip, pinboard, checkpoints,
   signpost — pick fresh ones).
5. Branch name (see `rules/checkin-and-pr.md`).

## Topic tracker

Phases and topics as agreed with Vivek (freshers track: C# Console → SQL → Web API → React).
Status: ✅ Done · 🔶 In progress · ⬜ Pending.

### Phase 1 — C# Fundamentals (Console App)

| # | Topic | Status | Folder | Diagram |
|---|---|---|---|---|
| 1 | Programming basics — variables, data types, operators, input/output | ✅ | `ProgrammingBasics/` | `ProgrammingBasics.svg` (architecture) + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus a numbered pair per method 1–3 (VariablesAndDataTypes, Operators, InputOutput) — retrofitted to match the `ControlFlow/` convention |
| 2 | Control flow — if/else, switch, loops | ✅ | `ControlFlow/` | `ControlFlow.svg` (architecture) + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus one numbered pair per method 1–6 (IfElse, Switch, ForLoop, WhileLoop, DoWhileLoop, Foreach) in `Flow/` and `Memory/` — 15 files total, the reference example for the folder/numbering convention |
| 3 | Methods & parameters | ✅ | `MethodsAndParameters/` | `MethodsAndParameters.svg` (architecture) + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus a numbered pair per method 1–5 (MethodBasics, Parameters, OptionalAndNamedParameters, ParamsKeyword, MethodOverloading) — Memory/2-Parameters.svg is the topic's key diagram (value vs ref vs out, side by side) |
| 4 | Collections — arrays, List, Dictionary | ✅ | `Collections/` | `Collections.svg` (architecture) + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus a numbered pair per method 1–5 (Arrays, Lists, Dictionaries, CollectionSafety, ChoosingACollection). One running example set (exam scores, to-do list, phone book) threads through all five. Flow diagrams use 5 different metaphors (row of lockers, growing filmstrip, pinboard, security checkpoints, signpost fork) per the "encouraged variety" rule |
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

0. **Plan first, then wait for approval.** Present: folder/namespace name, the `ExplainX()` method
   list in call order, the running example, the full diagram file list (exact paths) and any
   Memory-diagram exceptions. Do not build until the human approves (see `aidlc.md` step 3). Create
   the working doc `docs/todo/<short-topic-name>.md` and branch (naming: `rules/checkin-and-pr.md`).

1. **Folder + demo class** — create `BasicToAdvancedLearning.Console/<Topic>/<Topic>Demo.cs`.
   Namespace: `BasicToAdvancedLearning.<Topic>` (no `Console` segment — see gotcha #1). One public
   method per sub-concept (e.g. `ExplainX()`), each printing + explaining via comments, called in
   sequence from the adapter.
2. **Adapter class** — same folder, `<Topic>Example.cs`, namespace `BasicToAdvancedLearning.<Topic>`,
   `internal class <Topic>Example : ILearningTopic` with `Explain()` calling the demo's methods.
3. **Wire up Program.cs** — replace the previous topic's `using` line and the type after `new`; leave
   everything else (including the explanatory comment and `topic.Explain();`) unchanged:
   ```csharp
   using BasicToAdvancedLearning.<Topic>;
   ILearningTopic topic = new <Topic>Example();
   topic.Explain();
   ```
4. **Diagrams — MANDATORY, no exceptions.** Follow [`docs/rules/diagram-standards.md`](rules/diagram-standards.md)
   exactly. Under `ArchitectureDiagrams/<Topic>/`:
   - `<Topic>.svg` — architecture/concept (colour-coded boxes mapped 1:1 to the demo's `ExplainX()` methods)
   - `Flow/0-Overview.svg` + one `Flow/<N>-<Method>.svg` per method (call/return trace, right-angle
     routing only — no diagonals)
   - `Memory/0-Overview.svg` + one `Memory/<N>-<Method>.svg` per method (the Main → Explain() →
     method-frame snapshot, same shape every time, using the locked colour legend and that method's
     real variable names)
   `Flow/<N>` and `Memory/<N>` must use the SAME number for the same method. A topic is not ✅ until
   every method has both files and everything is valid XML.
5. **Verify — all five layers in [`rules/verification.md`](rules/verification.md)**: build + run
   (`dotnet build` 0 warnings, then `dotnet run` from `BasicToAdvancedLearning.Console/`; pipe stdin for
   any `Console.ReadLine()` prompts), `scripts/verify-topic.ps1 -Topic <Topic>` (and `-All -SkipBuild`
   to confirm older topics still pass), the manual diagram checklist, an independent fresh-context
   review, and `scripts/verify-docs.ps1`. Fix every FAIL; explain every WARN.
6. **Update the docs** — in this file flip the topic's Status cell to ✅, put the folder in the
   Folder cell **exactly as `` `<Folder>/` ``** (backticks + trailing slash — `verify-topic.ps1` finds
   the row by that text, so don't write the folder in backticks anywhere earlier in the file), note the
   diagram paths, and add anything surprising to "Conventions & gotchas". Also update the status
   snapshot and the "Next" paragraph. Then go through the stale-fact checklist in
   [`rules/verification.md`](rules/verification.md) layer 5 (several docs hard-code "topics 1–N"; the
   script checks the counts).
