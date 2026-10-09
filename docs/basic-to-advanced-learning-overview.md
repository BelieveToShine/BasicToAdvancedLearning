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

Currently done: topics 1–4 (Programming Basics, Control Flow, Methods & Parameters, Collections).
Next: topic 5 (OOP).

## Whole-system architecture
```
Program.cs (Main)
   │  ILearningTopic topic = new <Topic>Example();   ← the only line that changes per topic
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
- **Databases / third-party APIs** — none today. Phases 2–6 will introduce SQL, EF Core, ASP.NET Core
  and React; add each here (and a spec category) when its topic starts.
