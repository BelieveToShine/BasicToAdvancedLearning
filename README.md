# Basic to Advanced Learning

Learn C#, then SQL Server, EF Core, Web API, React and a full-stack app, one small topic at a time (28 topics). Every topic is a tiny runnable
program plus hand-drawn diagrams that show what happens step by step (Flow) and what sits in memory
while it runs (Memory).

## Run a lesson
```
cd BasicToAdvancedLearning.Console
dotnet run
```
Needs the .NET 9 SDK (if you only have a newer SDK/runtime, set `DOTNET_ROLL_FORWARD=Major`). `Program.cs` runs one topic at a time; to run another, change the `using` line and
the type after `new` (two lines — the comment in `Program.cs` shows how).

## Where things are
| Folder | What it holds |
|---|---|
| `BasicToAdvancedLearning.Console/<Topic>/` | The lesson code. Read the `*Demo.cs` file top to bottom — the comments are the explanation. |
| `ArchitectureDiagrams/<Topic>/` | The diagrams for that topic. Open the `.svg` files in a browser. Start with the architecture file, then `Flow/` and `Memory/`. |
| `frontend/` | The React lessons (topics 21–28) — Vite + React; needs Node 22. |
| `FullStackBank/` | The real bank Web API used by topics 25–27 (`Api/`), its smoke test, and the Docker setup for topic 28 (`deploy/`, needs Docker). |
| `docs/` | The plan (`ROADMAP.md`), rules and specs. Start at `docs/README.md`. |

## React lessons (topics 21–28)
```
cd frontend
npm install
npm run dev      # the lesson picker; pick a lesson on the left
npx vitest run   # the tests that prove each lesson's claims
```
Topics 25–27 call the real bank API: start it first (`cd FullStackBank/Api && dotnet run`, with `BTAL_SQLSERVER` set; see `FullStackBank/README.md`). Topic 28's container setup is in `FullStackBank/deploy/README.md`.

## Topics
See the tracker in [`docs/ROADMAP.md`](docs/ROADMAP.md) for what is done and what is coming next.

## Database lessons
SQL lessons use **Microsoft SQL Server only**. Set your own connection string in the `BTAL_SQLSERVER`
environment variable (never commit it), for example `Server=localhost,1433;User Id=sa;Password=<yours>;TrustServerCertificate=True;Initial Catalog=BasicToAdvancedLearning`.
Each lesson creates and uses its own scratch database (`<Initial Catalog>_<LessonFolder>`), so use a server where that is fine. Without the variable, the database lessons print a message and skip.
A throwaway local server: `docker run -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=<strong password> -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest`.
