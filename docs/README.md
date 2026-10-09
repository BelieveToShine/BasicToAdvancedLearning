# BasicToAdvancedLearning Docs — Map

How the docs link together, starting from `CLAUDE.md` (auto-loaded every session). Read
`ROADMAP.md` when picking the project back up — it says what's done, what's next, and how to add a
new topic.

```
CLAUDE.md
   ├──> docs/basic-to-advanced-learning-overview.md  ← what the product is + how it connects
   ├──> docs/architecture/overview.md                ← code shape (read before touching code)
   ├──> docs/aidlc.md                                ← automated per-unit-of-work flow
   ├──> docs/ROADMAP.md                              ← topic tracker, "add a new topic" runbook, gotchas
   ├──> docs/rules/README.md                         ← which rule file to read per action
   │       └──> docs/rules/diagram-standards.md      ← the mandatory 3-diagram rule + locked conventions
   ├──> docs/rules/verification.md                  ← 5-layer verify + review gate (scripts/ + checklists)
   ├──> docs/todo/                                   ← living per-task working docs
   └──> docs/superpowers/specs/README.md             ← spec format + category index
            └──> learning-topics/                    ← how a lesson topic is built (code + diagrams)

BasicToAdvancedLearning.Console/<TopicFolder>/       ← the lesson code
   ├─ <Topic>Demo.cs        (the actual explained concepts + comments)
   └─ <Topic>Example.cs     (adapts the demo to ILearningTopic)
ArchitectureDiagrams/<TopicFolder>/                  ← the pictorial explanation (mandatory)
   ├─ <Topic>.svg           (architecture / concept, one file)
   ├─ Flow/0-Overview.svg + <N>-<Method>.svg          (call/return trace per method)
   └─ Memory/0-Overview.svg + <N>-<Method>.svg        (Stack/Heap/Static snapshot per method)
```

| File | Purpose |
|---|---|
| `../CLAUDE.md` | Session entry point + mandatory workflow. |
| [`basic-to-advanced-learning-overview.md`](basic-to-advanced-learning-overview.md) | Read first: what the repo teaches, who for, whole-system picture. |
| [`architecture/overview.md`](architecture/overview.md) | Projects/folders, `ILearningTopic` pattern, where each kind of file lives, code gotchas. |
| [`aidlc.md`](aidlc.md) | How a unit of work runs end to end (the automated loop). |
| [`ROADMAP.md`](ROADMAP.md) | Living tracker: topic status, session notes, the runbook for adding a topic, conventions & gotchas. |
| [`rules/README.md`](rules/README.md) | Rules index (action → rule file). |
| [`rules/coding-standards.md`](rules/coding-standards.md), [`spec-docs.md`](rules/spec-docs.md), [`checkin-and-pr.md`](rules/checkin-and-pr.md), [`database.md`](rules/database.md), [`jira.md`](rules/jira.md), [`diagrams.md`](rules/diagrams.md) | The per-action rule files (code, specs, check-in/PR, DB, tracker, diagram summary) — which one to read when is in `rules/README.md`. |
| [`rules/diagram-standards.md`](rules/diagram-standards.md) | **Mandatory**: every method gets its own Flow AND Memory diagram; locks the memory colour legend and snapshot shape. |
| [`rules/verification.md`](rules/verification.md) | **Mandatory gate**: build+run, `scripts/verify-topic.ps1`, manual diagram checklist, independent fresh-context review, `scripts/verify-docs.ps1`. Also the acceptance test for the docs themselves. |
| [`superpowers/specs/README.md`](superpowers/specs/README.md) | Spec format + category index (what feature docs exist). |
| [`todo/`](todo/README.md) | Per-task working docs; folded into specs at check-in, then archived. |
| `scripts/verify-topic.ps1`, `scripts/verify-docs.ps1` | Automated cross-checks (PowerShell 5.1+, no extra installs). Topic check: diagram set vs call order, XML, routing, locked colours, variable names, wiring, tracker. Docs check: links, indexing, hygiene, open TODOs. |
| `BasicToAdvancedLearning.Console/Interfaces/ILearningTopic.cs` | The one contract every topic implements (`Explain()`), keeping `Program.cs` a fixed two-line runner. |

## Reading order for a task
CLAUDE.md → overview → architecture → the matching spec (and `ROADMAP.md` for topic work) → do work →
update spec + roadmap → `rules/checkin-and-pr.md`.

## Where new things slot in
- **A new topic** → follow the "Add a new topic" runbook in `ROADMAP.md`. It becomes a new row in
  the roadmap table plus a new folder under the console project and under `ArchitectureDiagrams/`.
- **A gotcha or convention worth keeping** (like the `Console` namespace clash) → add it to the
  "Conventions & gotchas" section of `ROADMAP.md` so it's never rediscovered the hard way twice.
- **A new rule** → the matching file under `rules/`, and a row in `rules/README.md` if it's a new file.
