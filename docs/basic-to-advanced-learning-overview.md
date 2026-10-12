# BasicToAdvancedLearning — Overview (read first)

## What it is
A teaching repository for freshers, built so one author can resume it session after session. It walks a
learner from C# console fundamentals up to a full-stack app (React → Web API → SQL). Each topic is a
runnable lesson (heavily commented C# code) plus a mandatory set of hand-authored SVG diagrams that
explain the concept before the code is shown. Primary author/owner: Vivek. Audience: junior developers.

Planned track (see [`ROADMAP.md`](ROADMAP.md) for per-topic status):
1. C# Fundamentals (console) — topics 1–9
2. SQL & Database — 10–12
3. Connecting C# to SQL (ADO.NET, EF Core) — 13–15
4. Web API — 16–20
5. React frontend — 21–25
6. Full-stack integration — 26–28

Currently done: topics 1–28 — every planned topic (C# fundamentals, SQL Server, ADO.NET and EF Core, Web API, React, the full-stack mini project and optional deployment basics). The SQL / API / container lessons were run against SQL Server 2022 in Docker only; running them against the owner's own server is the first thing to do.
Next: the owner's review and the provisional decisions listed in [`ROADMAP.md`](ROADMAP.md) and `review-recommendations.md` (R14, R15).

## Whole-system architecture
```
Program.cs (Main)
   │  ILearningTopic topic = new <Topic>Example();   ← (with its `using` line) the only two lines that change per topic
   ▼
<Topic>Example : ILearningTopic          (thin adapter — Explain() calls the demo in order)
   │
   ▼
<Topic>Demo                              (the lesson: one ExplainX() method per sub-concept)
   │
   └──> Console output  ──paired with──>  ArchitectureDiagrams/<Topic>/  (Architecture + Flow + Memory SVGs)
```

## Feature-area map
- **Lesson topics (code + diagrams)** → spec category
  [`superpowers/specs/learning-topics/`](superpowers/specs/learning-topics/overview.md)
- **Topic status & how to add one** → [`ROADMAP.md`](ROADMAP.md)
- **Diagram conventions** → [`rules/diagram-standards.md`](rules/diagram-standards.md)

## External systems
- **GitHub** — `https://github.com/BelieveToShine/BasicToAdvancedLearning` (branch `master`; a
  `MuhilWorkingRepo` branch also exists). TODO: confirm the branching/integration model.
- **Issue tracker** — TODO: none wired yet.
- **PR-validation AI tool** — TODO: none wired yet.
- **Databases / third-party services** — Microsoft SQL Server only (decision D8; connection string in the environment variable `BTAL_SQLSERVER`, never committed). Docker for topic 28; NuGet packages are pinned in the csproj files, npm packages in `frontend/package.json`.
