# BasicToAdvancedLearning — Roadmap (living doc)

Tracks what's done, what's next, and how to add a topic without re-explaining the convention from
scratch. Update this file whenever a topic starts, finishes, or the plan changes — see
[`README.md`](README.md) for how this fits the rest of the docs.

## Status snapshot — 2026-10-12

**Solution scaffolded**: `BasicToAdvancedLearning.sln` + `BasicToAdvancedLearning.Console` (net9.0).
Builds clean, 0 warnings/errors. Remote:
https://github.com/BelieveToShine/BasicToAdvancedLearning (default branch `master`).

**Done this session (Topic 9 — LINQ fundamentals, which completes Phase 1):**
- Topic 9 implemented + explained with 11 diagrams (see the tracker row below). The bank thread ends Phase 1 as a
  `List<decimal>` of amounts (positive = deposit, negative = withdrawal) queried four ways — no new classes.
  At the end of that session `Program.cs` ran Topic 9 (`LinqFundamentalsExample`). **Phase 1 (C# Fundamentals, topics 1–9) is complete.**
- Topic 9 decisions (all five from the old plan list were settled): folder/namespace `LinqFundamentals`
  (no segment named `Linq`); methods `WhereAndSelect`, `OrderingAndAggregates`, `AskingQuestions`,
  `DeferredExecution`; Flow metaphors: a conveyor with a filter gate, a scoreboard, a quiz show, a recipe card
  and a pantry. No locked-shape exception was needed.
- Memory diagrams draw a `List<T>` as ONE Heap box of cells (Topic 4's precedent). `Where`/`Select`/`OrderBy`
  results are always NEW lists once `ToList()` runs, and the source list is never edited. `Memory/4` shows an
  unrun query honestly: an iterator object holding a pointer to the source list and a pointer to the lambda
  (itself an object), while `snapshot` is a separate copy — the picture of deferred execution.
- The iterator is drawn as "Where iterator (.NET's internal class, simplified)" because the real type name and
  fields are internal and vary by .NET version; the compiler's hidden static cache for non-capturing lambdas
  is mentioned in the footer but not drawn (the lesson declares no static members).

**Earlier session (Topic 8 — File I/O basics):**
- Topic 8 implemented + explained with 11 diagrams (see the tracker row below). One tiny bank statement
  (`bta-statement.txt`) is written, read and deleted in the OS temp folder by the methods that create it, so running
  the lesson never leaves files behind or touches the repo. At the end of that session `Program.cs` ran Topic 8 (`FileIOExample`).
- Topic 8 decisions (all five from the old plan list were settled): folder/namespace `FileIO` (no segment
  shadows `File` or `Path`); methods `WriteAndReadText`, `LinesAndStreams`, `PathsAndExistence`, `FileErrors`;
  Flow metaphors: a filing-cabinet drawer, a water pipe with a tap, postal address labels, a lost-property desk.
- **Recorded addition to the locked shape:** every Topic 8 Memory diagram adds a dashed grey "OUTSIDE .NET —
  the operating system / the disk" box under the Heap column. It shows what is NOT .NET memory: the open file
  handle and the file on disk. The Stack, Heap and Static columns and the 3 frames are unchanged. Listed in
  `verification.md`.
- A stream is drawn honestly as a doorway: `StreamReader` → `FileStream` are Heap objects, simplified (a
  "(decoder and buffer not shown)" / "(internals simplified)" caption says so), and the handle behind them is in
  the outside box. `using` is shown by the `writer` going out of scope after its `Dispose()` (Memory/2 footer).
- `Memory/3-PathsAndExistence.svg` is the first Memory diagram where the interesting fact (does the file
  exist?) is not in any variable — the footer says `File.Exists` asks the disk every time.

**Earlier session (Topic 7 — Exception handling):**
- Topic 7 implemented + explained with 11 diagrams (see the tracker row below). The bank thread continues
  with `SafeAccount`, whose `Withdraw()` THROWS (a built-in `ArgumentOutOfRangeException`, or the custom
  `InsufficientFundsException : Exception` carrying a `Shortfall`) instead of returning `false` like Topic 5's
  `BankAccount` — Topic 5's "exceptions arrive in Topic 7" comment is now paid off, with no earlier topic edited.
  At the end of that session `Program.cs` ran Topic 7 (`ExceptionHandlingExample`).
- Topic 7 decisions (all five from the old plan list were settled): folder/namespace `ExceptionHandling`;
  methods `TryCatch` (a built-in `FormatException`), `CatchOrder`, `Finally`, `CustomExceptions`; Memory
  diagrams keep the locked 3 frames EXCEPT `Memory/4-CustomExceptions.svg`, which shows 4 frames at the
  instant of the throw (see below); Flow metaphors: a safety net, sorting chutes, a toll booth, a parcel
  thrown up a stairwell.
- **Recorded locked-shape exception:** `Memory/4-CustomExceptions.svg` has 4 stack frames (`Withdraw`,
  `ExplainCustomExceptions`, `Explain`, `Main`) because its whole point is that the failing method has not
  been unwound yet; it also shows the exception object on the Heap with no variable pointing at it. Same
  precedent as the instance-frame exception in `rules/diagram-standards.md`; listed in `verification.md`.
- An exception is drawn as an ordinary Heap object (`Message` pointer + any custom fields); `ex` is just a
  pointer. Message text from .NET itself is not quoted in diagrams because it changes between .NET versions.

**Earlier session (Topic 6 — OOP advanced):**
- Topic 6 implemented + explained with 11 diagrams (see the tracker row below). The bank thread continues
  with its own small family in the OOPAdvanced folder so Topic 5's code stays untouched: `Account` (base) →
  `SavingsAccount`, `CurrentAccount`; `Transaction` (abstract) → `DepositTransaction`,
  `WithdrawalTransaction`; `IInterestBearing` (interface) ← `SavingsAccount`, `FixedDeposit`.
  At the end of that session `Program.cs` ran Topic 6 (`OOPAdvancedExample`).
- Topic 6 decisions (all five from the old plan list were settled): folder/namespace `OOPAdvanced`; methods
  `Inheritance`, `Polymorphism`, `AbstractClasses`, `Interfaces`; Memory diagrams keep the locked 3 frames —
  a derived object is ONE Heap box with the parent's fields and its own captioned separately, and an array
  or variable typed as a parent/interface is only pointers; abstract classes and interfaces never get a Heap
  box. Flow metaphors: a family tree, a universal remote, a form with a blank, a wall socket and plugs.
- Memory diagrams here are the first to show an array of pointers aimed at objects of DIFFERENT classes
  (`Memory/2`, `3`, `4`), and the first to draw a field-level split between inherited and added fields.

**Earlier session (Topic 5 — OOP):**
- Topic 5 implemented + explained with 13 diagrams (see the tracker row below): running example is a
  `BankAccount` (`BankAccount.cs`), plus a static `Bank` class (`Bank.cs`) that only the last method uses.
  At the end of that session `Program.cs` ran Topic 5 (`OOPExample`).
- Topic 5 decisions (all five from the old plan list were settled): folder/namespace `OOP`; methods
  `ClassesAndObjects`, `Constructors`, `Encapsulation`, `ReferencesVsCopies`, `StaticMembers`; Memory
  diagrams keep the locked 3 frames and show each object as a Heap box listing its real field values
  (`this` and constructor chaining are carried as footer sentences — no 4th-frame exception was needed);
  Static gets real content in `Memory/5-StaticMembers.svg` only; Flow metaphors are a blueprint-and-stamp,
  an assembly line, a teller window in front of a vault, house keys, and a town noticeboard.
- `static` state lives in a separate `Bank` class on purpose, so methods 1–4 truly leave Static empty
  and `Memory/5` is where the Static column finally comes alive (its Static column is wider than the
  shared 90px empty-state box — not a frame change, just room for content).
- `Memory/4-ReferencesVsCopies.svg` shows two stack pointers into ONE Heap object, and both objects
  pointing at a single shared `"Anita"` string (identical literals are interned).
- The Flow diagrams use animated SMIL for the one thing that needs motion each: the ticket rolling along
  the constructor belt, the pulsing "same house" note, the ticking counter.

**Earlier sessions (Topics 1–4):**
- Topics 1–4 (Programming Basics, Control Flow, Methods & Parameters, Collections) implemented +
  explained with diagrams (see table below). Topic 4's runner was `CollectionsExample`.
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

**Next:** Phase 1 is finished. Phase 2 (SQL & Database, topics 10–12) is a different kind of material —
tables and queries, not C# objects — so STOP and settle the open questions with the owner before building
Topic 10 (see item R13 in [`review-recommendations.md`](review-recommendations.md)):
1. Which database engine and tooling the lessons use (SQL Server, SQLite, PostgreSQL…) — the docs still list this
   as an open `TODO:`, and `rules/database.md` is only a placeholder stub until it is decided. A cloud build sandbox may not be able to
   run a database server, so a file-based engine (SQLite) may be the only thing a session can actually execute.
2. What "the three mandatory diagrams" become for SQL: Stack/Heap/Static has no meaning for a query. Propose
   replacements (an entity-relationship diagram for the architecture view, a query-pipeline diagram for Flow, a
   "what the table holds before and after" diagram for Memory) and get them approved before drawing anything.
3. Whether SQL lessons live in the C# console project (running queries from code) or in their own folder of `.sql`
   scripts, and what `verify-topic.ps1` should check for them (it currently assumes a C# demo + adapter).
4. Folder/namespace and method list for Topic 10 (relational concepts — tables, keys, normalization), the running
   example (the bank thread: `Customers`, `Accounts`, `Transactions`), and the branch name.

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
| 5 | OOP — classes, objects, constructors, encapsulation | ✅ | `OOP/` | `OOP.svg` (architecture) + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus a numbered pair per method 1–5 (ClassesAndObjects, Constructors, Encapsulation, ReferencesVsCopies, StaticMembers) — 13 files. One running example (a `BankAccount`, plus a static `Bank`) threads through all five. Flow diagrams use 5 fresh metaphors (blueprint and stamp, assembly line, teller window and vault, house keys, town noticeboard). `Memory/5-StaticMembers.svg` is the first diagram with real Static content; `Memory/4-ReferencesVsCopies.svg` is the key one (two pointers, one object) |
| 6 | OOP advanced — inheritance, polymorphism, interfaces, abstract classes | ✅ | `OOPAdvanced/` | `OOPAdvanced.svg` (architecture) + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus a numbered pair per method 1–4 (Inheritance, Polymorphism, AbstractClasses, Interfaces) — 11 files. One running family (`Account` → `SavingsAccount`/`CurrentAccount`; abstract `Transaction`; interface `IInterestBearing` with `SavingsAccount` and `FixedDeposit`) threads through all four. Flow diagrams use 4 fresh metaphors (family tree, universal remote, form with a blank, wall socket and plugs). `Memory/1-Inheritance.svg` is the key one (parent + own fields in one Heap box); `Memory/2`–`4` show arrays of pointers to different classes |
| 7 | Exception handling | ✅ | `ExceptionHandling/` | `ExceptionHandling.svg` (architecture) + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus a numbered pair per method 1–4 (TryCatch, CatchOrder, Finally, CustomExceptions) — 11 files. One running example (`SafeAccount.Withdraw` throwing a built-in and a custom `InsufficientFundsException`) threads through methods 2–4. Flow diagrams use 4 fresh metaphors (safety net, sorting chutes, toll booth, parcel up a stairwell). `Memory/4-CustomExceptions.svg` is the key one and a recorded locked-shape exception (4 frames at the instant of the throw) |
| 8 | File I/O basics | ✅ | `FileIO/` | `FileIO.svg` (architecture) + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus a numbered pair per method 1–4 (WriteAndReadText, LinesAndStreams, PathsAndExistence, FileErrors) — 11 files. One running example (a tiny bank statement in the temp folder, deleted afterwards). Flow diagrams use 4 fresh metaphors (filing cabinet, water pipe, address labels, lost-property desk). Every Memory diagram adds a dashed "OUTSIDE .NET" box for the file handle and the disk — a recorded addition to the locked shape; `Memory/2-LinesAndStreams.svg` is the key one (StreamReader → FileStream → OS handle) |
| 9 | LINQ fundamentals | ✅ | `LinqFundamentals/` | `LinqFundamentals.svg` (architecture) + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus a numbered pair per method 1–4 (WhereAndSelect, OrderingAndAggregates, AskingQuestions, DeferredExecution) — 11 files. One running example (a `List<decimal>` of bank amounts, no new classes). Flow diagrams use 4 fresh metaphors (conveyor with a filter gate, scoreboard, quiz show, recipe card and pantry). `Memory/4-DeferredExecution.svg` is the key one (an unrun query = iterator + lambda; `ToList()` = a separate copy) |

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
   Moving to the next topic means changing TWO lines (the `using` for the topic's namespace and the type after `new`) — `Program.cs` itself
   never grows or needs re-reading. It's also a working example of interfaces/polymorphism baked
   into the project's own structure.
3. **Two files per topic, not one.** `<Topic>Demo.cs` is the actual lesson (the concepts, with
   explanatory comments) and has no knowledge of `ILearningTopic`. `<Topic>Example.cs` is a thin
   adapter that implements `ILearningTopic.Explain()` and calls into the demo class. Keeps the
   teaching code free of "runner" plumbing.
4. **Helper classes get their own files beside the demo.** Topic 5's `BankAccount.cs` and `Bank.cs` sit in
   `OOP/` next to `OOPDemo.cs`. `verify-topic.ps1` finds the demo as the non-`*Example.cs` file that defines
   the public `ExplainX()` methods, so helper files must not define any.
5. **Don't put `static` state on the class that earlier methods are teaching.** A static counter on
   `BankAccount` would have made Topic 5's methods 1–4 show a populated Static column. Keeping it on a
   separate `Bank` class means Static is genuinely empty until method 5 touches it.
6. **Memory-diagram truths worth keeping straight:** `const` members are not stored in Static (the compiler
   pastes their value into each use) — use `static`/`static readonly` for anything a Memory diagram puts in
   Static; identical string literals are interned, so two objects built from `"Anita"` point at ONE Heap string.
7. **A child can only use what its parent lets it see.** Topic 6's `Account` keeps `_balance` as a `protected`
   field so `SavingsAccount` can read it; `private` would hide it from the child. (In larger real projects a
   `protected` property is usually preferred over a `protected` field — the lesson keeps the field so the
   Memory diagram can name it.) A child class also calls `: base(...)` to run the parent's constructor first.
8. **Each topic builds its own classes.** Topic 6 does not inherit from Topic 5's `BankAccount`; it makes
   `Account` so earlier topics never change. Keep names distinct across topics (`BankAccount` vs `Account`).
9. **Don't quote .NET's own exception messages in diagrams or comments.** `decimal.Parse("abc")` says
   "The input string 'abc' was not in a correct format." on current .NET and a different sentence on older
   versions. Topic 7's diagrams show only the fragment `"…was not in a correct format."` (common to old and new wording); messages we write ourselves
   (`InsufficientFundsException`) can be quoted exactly.
10. **Catch clauses are checked top to bottom, and `catch (Exception)` matches everything** — it must be last, or
    the compiler rejects the clause after it (CS0160). `finally` runs on success AND failure, which is why its
    `Console.WriteLine` appears once per lap in the output.
11. **File demos write to the OS temp folder and clean up.** Topic 8 uses `Path.GetTempPath()` plus one constant file
    name and deletes the file in a `finally` (methods 1–2) or straight after use (method 3); method 4 only looks
    for a name that is never created. A run never leaves files behind or writes into the repo. Never print the full
    temp path in a diagram: it differs per operating system (`/tmp/…` vs `C:\Users\…`) — the diagrams write
    `"<temp folder>/…"`.
12. **`using` is what closes a stream.** `using (StreamWriter writer = …) { … }` calls `Dispose()` at the closing
    brace, which hands the buffered text to the operating system and releases the file handle. A stream you forget
    to dispose can lose its last buffered lines, and on Windows can leave the file locked for other programs.
    `ReadLine()` returns `null` at the end of the file — that is the loop's stop signal.
13. **A LINQ query is a plan until something enumerates it.** `Where`/`Select`/`OrderBy` return a lazy sequence;
    `ToList()`, `Count()`, `Sum()`, `First()` and a `foreach` are what actually run it — and each enumeration
    re-reads the source. That is why a `ToList()` taken earlier and a query run later can disagree (Topic 9,
    Memory/4). Use `ToList()` to freeze a result; never assume a query variable holds data.
14. **`First` throws, `FirstOrDefault` guesses.** `First` on no match throws `InvalidOperationException`;
    `FirstOrDefault` returns the type's default (`0` for a `decimal`), which can be confused with a real answer —
    ask `Any(...)` first when 0 is a legitimate value.

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
