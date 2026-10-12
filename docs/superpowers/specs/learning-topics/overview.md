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
| 5 | OOP | `OOP/` | classes & objects, constructors (and overloads), encapsulation, references vs copies, static members — running example `BankAccount` (+ static `Bank`) |
| 6 | OOP Advanced | `OOPAdvanced/` | inheritance, polymorphism (`virtual`/`override`), abstract classes, interfaces — running family `Account` → `SavingsAccount`/`CurrentAccount`, abstract `Transaction`, `IInterestBearing` |
| 7 | Exception Handling | `ExceptionHandling/` | `try`/`catch`, catch order, `finally`, custom exceptions — running example `SafeAccount` + `InsufficientFundsException` |
| 8 | File I/O | `FileIO/` | `File.WriteAllText`/`ReadAllText`, `StreamWriter`/`StreamReader` with `using`, `Path.Combine` and `File.Exists`, `FileNotFoundException` — running example: a tiny bank statement in the temp folder |
| 9 | LINQ Fundamentals | `LinqFundamentals/` | `Where`/`Select`, `OrderBy` and aggregates (`Sum`, `Max`, `Count`), `Any`/`All`/`First`/`FirstOrDefault`, deferred execution — running example: a `List<decimal>` of bank amounts |
| 10 | Relational Concepts | `RelationalConcepts/` | tables/rows/columns, primary keys, foreign keys, constraints and NULLs, normalization — T-SQL via `SqlLab` on the bank tables |
| 11 | SQL Basics | `SqlBasics/` | SELECT/WHERE, IN/BETWEEN/LIKE, ORDER BY/TOP, joins, GROUP BY and aggregates, WHERE vs HAVING |
| 12 | SQL CRUD | `SqlCrud/` | INSERT, UPDATE, DELETE, transactions, XACT_ABORT, safe habits |
| 13 | ADO.NET | `AdoNet/` | SqlConnection/SqlCommand, parameters (and the injection demo), SqlDataReader, SqlTransaction |
| 14 | EF Core Basics | `EfCoreBasics/` | entity classes, DbContext/DbSet, Fluent API, a real generated migration, EnsureCreated vs Migrate |
| 15 | EF Core CRUD | `EfCoreCrud/` | Add/SaveChanges, LINQ queries, Include, change tracking, Remove, all-or-nothing saves |
| 16 | HTTP and REST | `HttpAndRest/` | requests/responses, methods, status codes, JSON, REST resources — in-process server via `WebLab` |
| 17 | Web API Basics | `WebApiBasics/` | controllers, routing, model binding, action results, dependency injection, service lifetimes |
| 18 | Web API CRUD | `WebApiCrud/` | CRUD endpoints over EF Core + SQL Server, DTOs, validation, 201/404/409 |
| 19 | API Testing | `ApiTesting/` | OpenAPI/Swagger, Swagger UI, documenting endpoints, Postman collection and `.http` file, simple checks |
| 20 | JWT Auth | `JwtAuth/` | 401 vs 403, what a JWT is, login and token issuing, `[Authorize]`, token lifetime |
| 21 | React Fundamentals | `ReactFundamentals/` | JSX, components, props, lists and keys, conditional rendering (frontend lessons) |
| 22 | React State | `ReactState/` | useState, event handlers, state updates, controlled inputs, objects and arrays |
| 23 | React Effects | `ReactEffects/` | useEffect, dependencies, cleanup, fetching data, custom hooks |
| 24 | React Router | `ReactRouter/` | routes and links, params, nested routes, navigation and search params, route guard preview |
| 25 | React API Client | `ReactApiClient/` | calling the real API: GET/POST, status codes in the UI, an API module, proxy and CORS |
| 26 | Full-stack bank | `FullStackBank/` | the end-to-end mini project; the API itself is `FullStackBank/Api/` (its own program) |
| 27 | Cross-layer validation | `CrossLayerValidation/` | browser/API/database validation, field errors, business-rule errors, error boundaries, double submit |
| 28 | Deployment (optional) | `Deployment/` | Docker/compose setup for API + web + SQL Server, configuration vs code |

## Running it locally
- Prerequisites: .NET 9 SDK; for topics 10–20 a SQL Server you may create scratch databases on (set `BTAL_SQLSERVER`; see the Configuration reference); for topics 21–28 Node 22 + npm (`cd frontend && npm install && npm run dev`); Docker for topic 28.
- From `BasicToAdvancedLearning.Console/`: `dotnet build` then `dotnet run`. `Program.cs` runs exactly
  one topic (currently `LinqFundamentalsExample`; topics 10–20 are C# lessons too, topics 21–28 are React lessons run with `npm run dev`); change the `using` line and the type after `new` to run another (both must change, or the build fails).
- Topics that call `Console.ReadLine()` (Programming Basics) need stdin — pipe input when running
  non-interactively.
- Diagrams are plain SVG: open in any browser. Verify with `powershell -ExecutionPolicy Bypass -File scripts/verify-topic.ps1 -Topic <Topic>` (and `scripts/verify-docs.ps1` for the docs).

## Configuration reference
- `BTAL_SQLSERVER` — SQL Server connection string used by topics 10–15, 18 and by `FullStackBank/Api` (never committed). Each lesson uses its own database named `<Initial Catalog>_<TopicFolder>`; the variable's own database is only the naming base. If it is missing the lesson prints a skip message.
- `API_URL` — where the Vite dev server proxies `/api` (default `http://localhost:5080`).

## Known gaps / deliberate scope cuts
- No unit-test project; verification is build + run + `scripts/verify-topic.ps1` + manual and independent review (see `rules/verification.md`). The script checks structure and rules, not visual quality or the truth of a diagram.
- `Program.cs` runs one topic at a time — no menu to pick a topic at runtime.
- Topic 1 file names (`ProgrammingBasics.cs`, `ProgrammingBasicsExample.cs`) predate the
  `<Topic>Demo.cs` convention.
- Topic 5's helper classes (`BankAccount.cs`, `Bank.cs`) sit beside `OOPDemo.cs`; they define no `ExplainX()`
  methods, so the verify script still finds the demo. Their `Describe()` output is what the lesson prints.
- Topic 6's classes live in several files beside `OOPAdvancedDemo.cs` (`Account.cs`, `SavingsAccount.cs`,
  `CurrentAccount.cs`, `Transaction.cs` — which also holds its two concrete children —, `IInterestBearing.cs`,
  `FixedDeposit.cs`); none defines an `ExplainX()` method.
- Topic 7's `SafeAccount.cs` and `InsufficientFundsException.cs` sit beside `ExceptionHandlingDemo.cs`; neither
  defines an `ExplainX()` method. `Memory/4-CustomExceptions.svg` is the topic's one locked-shape exception
  (4 frames, shown at the instant of the throw).
- Topic 8 touches the real file system, so the methods that create a file use ONE file in `Path.GetTempPath()` and delete it in a
  `finally` (or straight after use). Every Memory diagram adds a dashed "OUTSIDE .NET" box (file handle + disk) under
  the Heap — a recorded addition to the locked shape (see `rules/verification.md`).
- Topic 9 needs no helper classes: every method queries a `List<decimal>` built by a private `CreateAmounts()` helper
  inside the demo. All 28 topics are built. This spec describes the lesson-topic shape (code + diagrams); the SQL/web variations are in `rules/database.md`, the ROADMAP gotchas 15–22 and `Interfaces/SqlLab.cs` / `Interfaces/WebLab.cs`; the React lessons follow the same diagram numbering under `frontend/src/lessons/`.
- Open question: whether to add a runtime topic picker or keep the deliberate two-line swap (the `using` and the type after `new`; it doubles
  as the polymorphism teaching example).

## Real bugs found & fixed (as general lessons)
- **Static state on the class being taught leaks into every earlier diagram.** A shared counter on
  `BankAccount` would have forced Topic 5's methods 1–4 to show a non-empty Static column. Lesson: put
  `static` state on its own type so a diagram's Static column is empty exactly where the code leaves it empty.
- **A namespace segment named like a BCL type shadows it.** `...Console.<Topic>` made unqualified
  `Console.*` resolve to the namespace. Fixed with `<RootNamespace>` in the `.csproj`. Lesson: don't
  name namespace segments after types you call unqualified.
- **A diagonal connector can silently cut through a shape.** A loop-back path ran over a box and the
  arrow looked like it "ended nowhere". Lesson: right-angle routing only, and trace every path against
  every box's bounding box.
- **A centred label on its own arrow looks broken.** The line shows through the letter gaps. Lesson:
  place labels beside or above the line, never on it.
