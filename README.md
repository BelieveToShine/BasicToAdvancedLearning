# Basic to Advanced Learning

Learn C# (and then SQL, EF Core, Web API) one small topic at a time. Every topic is a tiny runnable
program plus hand-drawn diagrams that show what happens step by step (Flow) and what sits in memory
while it runs (Memory).

## Run a lesson
```
cd BasicToAdvancedLearning.Console
dotnet run
```
Needs the .NET 9 SDK. `Program.cs` runs one topic at a time; to run another, change the `using` line and
the type after `new` (two lines — the comment in `Program.cs` shows how).

## Where things are
| Folder | What it holds |
|---|---|
| `BasicToAdvancedLearning.Console/<Topic>/` | The lesson code. Read the `*Demo.cs` file top to bottom — the comments are the explanation. |
| `ArchitectureDiagrams/<Topic>/` | The diagrams for that topic. Open the `.svg` files in a browser. Start with the architecture file, then `Flow/` and `Memory/`. |
| `docs/` | The plan (`ROADMAP.md`), rules and specs. Start at `docs/README.md`. |

## Topics
See the tracker in [`docs/ROADMAP.md`](docs/ROADMAP.md) for what is done and what is coming next.

## Database lessons
SQL lessons use **Microsoft SQL Server only**. Set your own connection string in the `BTAL_SQLSERVER`
environment variable (never commit it). Without it, the database lessons print a message and skip.
