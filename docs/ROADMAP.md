# BasicToAdvancedLearning — Roadmap (living doc)

Tracks what's done, what's next, and how to add a topic without re-explaining the convention from
scratch. Update this file whenever a topic starts, finishes, or the plan changes — see
[`README.md`](README.md) for how this fits the rest of the docs.

## Status snapshot — 2026-10-12

**Solution scaffolded**: `BasicToAdvancedLearning.sln` + `BasicToAdvancedLearning.Console` (net9.0).
Builds clean, 0 warnings/errors. Remote:
https://github.com/BelieveToShine/BasicToAdvancedLearning (default branch `master`).

**Done this session (Phases 2–6, topics 10–28 — built in one unattended run; the owner reviews at the end):**
- Topics 10–28 are implemented, each with its lesson code and the mandatory diagram set (13–15 SVGs per topic), and each went
  through an independent fresh-context review whose findings were fixed (see the tracker rows). The owner asked for SQL Server
  only (decision D8 in `decisions.md`; engine rules in `rules/database.md`). Real connection strings come later: lessons read
  `BTAL_SQLSERVER`, print a friendly skip message when it is missing, and give every lesson its OWN scratch database
  (`<Initial Catalog>_<TopicFolder>`, created automatically by `Interfaces/SqlLab.cs`).
- **Everything below was run against SQL Server 2022 in Docker, on a .NET 10 runtime with the `net9.0` target (roll-forward).
  Nothing has been run against the owner's own SQL Server or on a net9 runtime yet** — please do that first.
- Shape of the new topics: SQL topics 10–12 are C# lessons that send T-SQL through `SqlLab` (so the existing `ILearningTopic`
  shape, scripts and diagram rules still apply; every Memory diagram adds the dashed "OUTSIDE .NET — SQL Server" box, the
  Topic 8 precedent). Web topics 16–20 host a small ASP.NET Core server inside the console process (`Interfaces/WebLab.cs`,
  `FrameworkReference Microsoft.AspNetCore.App` on the plain SDK so web implicit usings do not leak into other topics) and call
  it with `HttpClient`; lessons use no `async`/`await` (not taught yet). React topics 21–27 and 28's lesson page are plain-JavaScript
  lessons in `frontend/src/lessons/<Topic>/` (Vite + React 19; `NN-Name.jsx` files are auto-listed by the picker; vitest tests prove
  the claims). Topic 26/27 call the REAL API in `FullStackBank/Api/` (its own program; contract in `FullStackBank/README.md`).
  Topic 28 ships a tested Docker/compose setup in `FullStackBank/deploy/`.
- **Provisional decisions awaiting the owner** (all recorded in `review-recommendations.md` R15): SQL topics keep the C#-lesson shape with
  the SQL-Server box added to Memory diagrams; React/deployment "Memory" diagrams show where data/config lives instead of
  Stack/Heap/Static (extra legend colours: light blue for module data, yellow refs, blue effect closures, a volume colour);
  one shared console project for topics 1–20; package versions pinned in the csproj; SQL Server image `2022-latest` for testing.
- `Program.cs` runs one topic at a time exactly as before: change the `using` line and the type after `new`. After this run it is wired to
  `LinqFundamentalsExample` (Topic 9); topics 10–20 are not wired, which is why the WARN baseline in `rules/verification.md` grew.
- Phase 1 got a deep accuracy pass first (wrong claims, loop-back arrows through boxes, clipped footers): see the commits titled
  "Fix Phase 1 audit findings" and "Phase 1 review round 2".

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

**Next:** all 28 planned topics are built. What is left is the owner's review and these decisions (details in
[`review-recommendations.md`](review-recommendations.md)): confirm or change the provisional choices listed in the
status snapshot (R15), run the SQL / API / container lessons against the owner's own SQL Server (nothing so far ran against it),
and decide which of the proposed script and rule improvements (R14) to adopt. Adding a topic 29+ follows the runbook
below; React topics follow `frontend/` conventions (see the gotchas), database and web topics follow `rules/database.md`.

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

### Phase 2 — SQL & Database (SQL Server only)
| # | Topic | Status | Folder | Diagram |
|---|---|---|---|---|
| 10 | Relational DB concepts — tables, keys, normalization | ✅ | `RelationalConcepts/` | `RelationalConcepts.svg` (architecture: ER-style picture of Customers/Accounts/Transactions) + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus a numbered pair per method 1–5 (TablesRowsColumns, PrimaryKeys, ForeignKeys, ConstraintsAndNulls, Normalization) — 13 files. T-SQL on SQL Server sent from C# through `Interfaces/SqlLab.cs`; the bank (Customers → Accounts → Transactions) is the running example. Every Memory diagram adds the dashed grey "OUTSIDE .NET — SQL Server" box with the real rows (Topic 8 precedent). Run against SQL Server 2022 (Docker) only — not yet against the owner's server. |
| 11 | SQL basics — SELECT/WHERE/ORDER BY/JOIN/GROUP BY | ✅ | `SqlBasics/` | `SqlBasics.svg` + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus a numbered pair per method 1–6 (SelectAndWhere, HandyFilters, OrderingAndTop, Joins, GroupByAndAggregates, WhereVsHaving) — 15 files. Seeds 4 customers / 6 accounts / 12 transactions; Dana Smith has no account so LEFT JOIN is interesting. `WhereVsHaving` shows the two errors SQL Server really gives (147, 207). Same SQL-Server box on every Memory diagram. Run against SQL Server 2022 (Docker) only — not yet against the owner's server. |
| 12 | SQL CRUD — INSERT, UPDATE, DELETE, transactions | ✅ | `SqlCrud/` | `SqlCrud.svg` + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus a numbered pair per method 1–6 (InsertRows, UpdateRows, DeleteRows, Transactions, XactAbort, SafeHabits) — 15 files. Memory diagrams show table rows before and after inside the SQL Server box. Run against SQL Server 2022 (Docker) only — not yet against the owner's server. |

### Phase 3 — Connecting C# to SQL
| # | Topic | Status | Folder | Diagram |
|---|---|---|---|---|
| 13 | ADO.NET basics | ✅ | `AdoNet/` | `AdoNet.svg` + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus a numbered pair per method 1–4 (ConnectionAndCommand, Parameters, ReadingRows, Transactions) — 11 files. `Microsoft.Data.SqlClient` used directly (SqlConnection/SqlCommand/SqlDataReader/SqlTransaction); the SQL-injection demo only touches scratch tables. Run against SQL Server 2022 (Docker) only — not yet against the owner's server. |
| 14 | EF Core — DbContext, models, migrations | ✅ | `EfCoreBasics/` | `EfCoreBasics.svg` + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus a numbered pair per method 1–5 (ModelClasses, DbContextAndDbSet, FluentApiConfiguration, Migrations, EnsureCreatedVsMigrate) — 13 files. Contains a REAL generated `Migrations/` folder (`dotnet ef migrations add InitialCreate`, EF Core 9.0.20). Run against SQL Server 2022 (Docker) only — not yet against the owner's server. |
| 15 | EF Core CRUD (LINQ-to-Entities) | ✅ | `EfCoreCrud/` | `EfCoreCrud.svg` + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus a numbered pair per method 1–6 (AddAndSave, QueryingWithLinq, RelatedData, UpdatingTrackedEntities, RemovingRows, SaveChangesIsAllOrNothing) — 15 files. The lesson prints the SQL EF really sends (via a small interceptor, `SqlSpy`). Memory diagrams are 1300px wide (second Heap column for tracker-row → entity pointers): a recorded locked-shape exception. Run against SQL Server 2022 (Docker) only — not yet against the owner's server. |

### Phase 4 — Web API
| # | Topic | Status | Folder | Diagram |
|---|---|---|---|---|
| 16 | HTTP & REST fundamentals | ✅ | `HttpAndRest/` | `HttpAndRest.svg` + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus a numbered pair per method 1–5 (RequestAndResponse, HttpMethods, StatusCodes, JsonBodies, RestResources) — 13 files. A tiny in-process server (`Interfaces/WebLab.cs`) answers real `HttpClient` calls; Memory diagrams add a dashed box for the network text. Run on a .NET 10 runtime with the net9.0 target (roll-forward), not on a net9 runtime. |
| 17 | ASP.NET Core Web API basics — controllers, routing, model binding, action results, DI | ✅ | `WebApiBasics/` | `WebApiBasics.svg` + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus a numbered pair per method 1–6 (ControllerAndAction, Routing, ModelBinding, ActionResults, DependencyInjection, ServiceLifetimes) — 15 files. `Memory/4` and `Memory/6` have a wider Static column (a controller's static list, a shared probe counter): recorded exception. Run on a .NET 10 runtime with the net9.0 target; the exact key names of a 400 ProblemDetails for broken JSON were deliberately not printed because they can differ between versions. |
| 18 | CRUD endpoints backed by EF Core + SQL Server | ✅ | `WebApiCrud/` | `WebApiCrud.svg` + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus a numbered pair per method 1–4 (DbContextInDI, ReadEndpoints, CreateEndpoint, UpdateAndDelete) — 11 files. DTOs (entities never leave the API), `[ApiController]` validation, 201+Location, 409 mapped from SQL error numbers. The lesson drops its own scratch database with `EnsureDeleted()` and says so on screen. Run against SQL Server 2022 (Docker) only — not yet against the owner's server. |
| 19 | Testing APIs with Swagger/OpenAPI and Postman | ✅ | `ApiTesting/` | `ApiTesting.svg` + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus a numbered pair per method 1–5 (WhatIsOpenApi, SwaggerUi, DocumentingEndpoints, PostmanAndHttpFiles, ManualVsAutomatedTests) — 13 files. Ships a real `bank-api.http` and a Postman v2.1 collection (run for real with `newman`, 8/8 passing); the lesson replays the collection against the in-process API. Swashbuckle 10.3.0. Not opened in the Postman GUI or VS Code REST Client. |
| 20 | Basic auth/authorization — JWT overview | ✅ | `JwtAuth/` | `JwtAuth.svg` + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus a numbered pair per method 1–5 (AuthenticationVsAuthorization, WhatIsAJwt, IssuingATokenLogin, ProtectingEndpoints, TokenLifetime) — 13 files. Signing key generated at run time, nothing secret printed; demo users are in memory and the plain-text password compare is labelled DEMO ONLY. Memory diagrams add a dashed "OUTSIDE THIS METHOD — the API server" box. |

### Phase 5 — React Frontend
| # | Topic | Status | Folder | Diagram |
|---|---|---|---|---|
| 21 | React fundamentals — JSX, components, props | ✅ | `ReactFundamentals/` | `ReactFundamentals.svg` + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus a numbered pair per lesson 1–5 (Jsx, Components, Props, ListsAndKeys, ConditionalRendering) — 13 files. 36 tests; the index-key bug and the stray `0` are reproduced in a real browser. Lessons 3–5 use `useState` only as a labelled preview of Topic 22. Lives in `frontend/src/lessons/<Folder>/` (Vite + React 19, plain JavaScript); run with `cd frontend && npm install && npm run dev`, tests with `npx vitest run`. Memory diagrams follow the PROVISIONAL React convention (where the data lives: props / state / derived / DOM) — owner decision pending (R15). |
| 22 | State & events (useState) | ✅ | `ReactState/` | `ReactState.svg` + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus a numbered pair per lesson 1–5 (UseState, EventHandlers, StateUpdates, ControlledInputs, ObjectsAndArrays) — 13 files. 25 tests (render counts proved with React's `<Profiler>`); money is kept in whole cents. Lives in `frontend/src/lessons/<Folder>/` (Vite + React 19, plain JavaScript); run with `cd frontend && npm install && npm run dev`, tests with `npx vitest run`. Memory diagrams follow the PROVISIONAL React convention (where the data lives: props / state / derived / DOM) — owner decision pending (R15). |
| 23 | Side effects & data fetching (useEffect) | ✅ | `ReactEffects/` | `ReactEffects.svg` + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus a numbered pair per lesson 1–5 (WhatIsAnEffect, Dependencies, Cleanup, FetchingData, CustomHooks) — 13 files. 28 tests; StrictMode's development double-run and the async-effect crash were verified in a real browser. Two extra legend colours (refs, effect closures). Lives in `frontend/src/lessons/<Folder>/` (Vite + React 19, plain JavaScript); run with `cd frontend && npm install && npm run dev`, tests with `npx vitest run`. Memory diagrams follow the PROVISIONAL React convention (where the data lives: props / state / derived / DOM) — owner decision pending (R15). |
| 24 | React Router | ✅ | `ReactRouter/` | `ReactRouter.svg` + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus a numbered pair per lesson 1–5 (RoutesAndLinks, RouteParams, NestedRoutesAndLayouts, NavigationAndSearchParams, ProtectedRoute-preview) — 13 files. react-router-dom 7.18.4 inside `MemoryRouter`s; the guard lesson says plainly that client-side guards are UX only. Lives in `frontend/src/lessons/<Folder>/` (Vite + React 19, plain JavaScript); run with `cd frontend && npm install && npm run dev`, tests with `npx vitest run`. Memory diagrams follow the PROVISIONAL React convention (where the data lives: props / state / derived / DOM) — owner decision pending (R15). |
| 25 | Calling the Web API from React | ✅ | `ReactApiClient/` | `ReactApiClient.svg` + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus a numbered pair per lesson 1–5 (GetFromTheApi, PostAndRefresh, StatusCodesInTheUi, ApiModule, ProxyAndCors) — 13 files. Calls the real `FullStackBank/Api` through the Vite proxy (verified in headless Chromium); shows a friendly state when the API is not running. Lives in `frontend/src/lessons/<Folder>/` (Vite + React 19, plain JavaScript); run with `cd frontend && npm install && npm run dev`, tests with `npx vitest run`. Memory diagrams follow the PROVISIONAL React convention (where the data lives: props / state / derived / DOM) — owner decision pending (R15). |

### Phase 6 — Full Stack Integration
| # | Topic | Status | Folder | Diagram |
|---|---|---|---|---|
| 26 | End-to-end mini project (React → Web API → SQL) | ✅ | `FullStackBank/` | `FullStackBank.svg` + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus a numbered pair per lesson 1–5 (AccountsList, AccountDetail, MoneyForm, OpenAccount, TheWholeApp) — 13 files. The React side lives in `frontend/src/lessons/FullStackBank/`; the real API (EF Core + SQL Server, rowversion concurrency, ProblemDetails errors) is its own program in `FullStackBank/Api/` (see `FullStackBank/README.md`; `FullStackBank/smoke.sh` checks 38 behaviours). No authentication, HTTPS or CORS — local learning API only. Lives in `frontend/src/lessons/<Folder>/` (Vite + React 19, plain JavaScript); run with `cd frontend && npm install && npm run dev`, tests with `npx vitest run`. Memory diagrams follow the PROVISIONAL React convention (where the data lives: props / state / derived / DOM) — owner decision pending (R15). |
| 27 | Error handling & validation across all layers | ✅ | `CrossLayerValidation/` | `CrossLayerValidation.svg` + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus a numbered pair per lesson 1–5 (ThreeLayers, FieldErrorsFromProblemDetails, BusinessRuleErrors, UnexpectedErrors, LoadingRetryAndDoubleSubmit) — 13 files. Every claim about which layer rejects which input was checked against the real API and the real database; the only class component is the ErrorBoundary. Lives in `frontend/src/lessons/<Folder>/` (Vite + React 19, plain JavaScript); run with `cd frontend && npm install && npm run dev`, tests with `npx vitest run`. Memory diagrams follow the PROVISIONAL React convention (where the data lives: props / state / derived / DOM) — owner decision pending (R15). |
| 28 | (Optional) Deployment basics | ✅ | `Deployment/` | `Deployment.svg` + `Flow/0-Overview.svg` + `Memory/0-Overview.svg`, plus a numbered pair per guide step 1–5 (EnvFile, ApiImage, WebImage, ComposeUp, VerifyAndTearDown) — 13 files. The container setup is real and was built and run (`FullStackBank/deploy/`: API image, nginx + React image, SQL Server, init job with a least-privilege login). Memory diagrams show where configuration lives (files / image layers / container environment / volume). Not run on Windows, Docker Desktop or ARM; HTTPS is not set up. Lesson page in `frontend/src/lessons/Deployment/`. |

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
15. **SQL Server only, one scratch database per lesson.** `Interfaces/SqlLab.cs` reads `BTAL_SQLSERVER`, takes the folder name of the
    lesson that called `SqlLab.IsReady()` and uses `<Initial Catalog>_<Folder>` as that lesson's database (created if missing). So lessons
    never share tables, and `EnsureDeleted()` (Topics 15, 18) only drops the lesson's own database — but it still deletes a whole database, so
    point the variable only at a server where scratch databases are fine. Never commit a connection string.
16. **Lessons must be repeatable.** Every database lesson drops/recreates its own tables (children before parents) at the start; the whole run
    was executed twice per topic and the outputs compared byte for byte. Keep printed output free of ports, timestamps, tokens and
    version-dependent .NET messages (print status codes, error NUMBERS, field names instead).
17. **EF Core facts worth keeping straight.** With no precision set, EF Core 9 maps `decimal` to `decimal(18,2)` and logs a warning that values
    which do not fit would be silently truncated; `EnsureCreated()` then `Migrate()` fails (error 2714) while `Migrate()` then `EnsureCreated()`
    does nothing; a refused INSERT still uses up an IDENTITY value; EF does not print `BEGIN`/`COMMIT` in the command text (it starts the transaction
    through ADO.NET). Migrations were generated with `dotnet-ef` 9.0.20 (`dotnet tool install --global dotnet-ef --version 9.0.20`).
18. **Hosting a web server inside a console lesson.** Use `WebLab` (random free port, logging cleared, `AddControllersOnly(...)` so each topic only sees its
    own controllers). Web usings are NOT implicit (plain SDK): add them. ASP.NET Core error bodies carry a changing `traceId` and version-dependent text:
    print status codes and field names only. `verify-topic.ps1` counts `{`/`}` inside string literals, so an unbalanced `{` in a literal inside a lesson
    method makes it read the next methods as part of that body and raise a false variable WARN: keep such literals brace-balanced or at class level.
19. **Environment caveat.** The author machine had only a .NET 10 runtime, so the `net9.0` projects ran with `DOTNET_ROLL_FORWARD=Major`. Behaviour that differs
    between 9 and 10 (for example exact ProblemDetails key names for broken JSON) is deliberately not printed. Run everything once on a net9 SDK.
20. **React lessons.** `frontend/` is one Vite app; the picker (`src/App.jsx`) lists every `src/lessons/<Topic>/NN-*.jsx` (names start with a digit; tests and
    helpers do not). In development StrictMode runs every effect twice on mount — counters and request logs are one higher than a production build shows
    (say so on the page). Tests mock `fetch`; the real API was also driven in headless Chromium. An `async` function passed to `useEffect` crashes React 19
    on cleanup/unmount (also in production builds) — the Effects lesson shows it.
21. **Browser vs API.** A browser `fetch` resolves on 4xx/5xx (`ok` is false); it rejects only when no HTTP answer arrives. With the Vite dev proxy and the API
    stopped, the proxy answers 502 `text/plain`. The proxy's string form sets `changeOrigin: true`, so a 201's `Location` header carries the API's own origin:
    use its path only. CORS is enforced by the browser; a direct JSON POST to another origin stops at the OPTIONS preflight.
22. **Containers.** Docker Hub rate limits (429) may block `node`/`nginx` pulls (use a mirror and retag locally); the `aspnet` image has no curl (the healthcheck uses
    bash `/dev/tcp`); the API only migrates in Development (`launchSettings.json` forces Development for `dotnet run`; use `--no-launch-profile` to try Production);
    shell scripts need LF line endings (`deploy/.gitattributes`).

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
