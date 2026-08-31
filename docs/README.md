# BasicToAdvancedLearning Docs — Map

How the docs link together. Read `ROADMAP.md` first when picking this project back up in a new
session — it says what's done, what's next, and how to add a new topic without re-explaining the
convention from scratch.

## Link map

```
docs/README.md          ← you are here
   │
   ├──> docs/ROADMAP.md  ← START HERE each session: status snapshot, topic tracker,
   │        │               "add a new topic" runbook
   │        │
   │        └──> BasicToAdvancedLearning.Console/<TopicFolder>/   ← the lesson code
   │                ├─ <Topic>Demo.cs        (the actual explained concepts + comments)
   │                └─ <Topic>Example.cs     (adapts the demo to ILearningTopic)
   │        └──> ArchitectureDiagrams/<TopicFolder>/     ← the pictorial explanation (mandatory)
   │                ├─ <Topic>.svg               (architecture / concept, one file)
   │                ├─ Flow/0-Overview.svg + <N>-<Method>.svg per method (call/return trace)
   │                └─ Memory/0-Overview.svg + <N>-<Method>.svg per method (Stack/Heap/Static snapshot)
   │
   └──> docs/rules/diagram-standards.md   ← the mandatory 3-diagram rule + locked conventions
```

## What each file is for

| File | Purpose |
|---|---|
| [`ROADMAP.md`](ROADMAP.md) | Living tracker: which topics are done/in-progress/pending, session notes, and the runbook for adding a new topic (folder + namespace + interface + diagram conventions). |
| `BasicToAdvancedLearning.Console/<Topic>/` | One folder per topic. `<Topic>Demo.cs` holds the actual teaching code with explanatory comments; `<Topic>Example.cs` implements `ILearningTopic` so `Program.cs` never has to change shape. |
| `BasicToAdvancedLearning.Console/Interfaces/ILearningTopic.cs` | The one contract every topic implements (`Explain()`). Lets `Program.cs` stay a fixed two-line runner regardless of which topic is active. |
| `ArchitectureDiagrams/<Topic>/` | **Mandatory per topic** — see [`docs/rules/diagram-standards.md`](rules/diagram-standards.md): one architecture/concept file, plus a `Flow/` and `Memory/` folder each holding a `0-Overview.svg` and one numbered file **per method** (`Flow/<N>` and `Memory/<N>` share the same number). Same visual language as the LeadHunter diagrams (colour-coded boxes, legend, arrows = flow) — used to explain the concept to juniors before showing code. |
| [`rules/diagram-standards.md`](rules/diagram-standards.md) | **Mandatory rule**: no topic is done until every method has its own Flow AND Memory diagram. Locks the memory-diagram colour legend and per-method snapshot shape (Stack/Heap/Static) so it's identical across every topic. |

## Reading order for a new session
1. `ROADMAP.md` (what's done, what's next) → 2. the current topic's `<Topic>Demo.cs` (the code) →
3. the matching `ArchitectureDiagrams/<Topic>/<Topic>.svg` (the picture) → 4. do the work →
5. update `ROADMAP.md`.

## Where new things slot in
- **A new topic** → follow the "Add a new topic" runbook in `ROADMAP.md`. It becomes a new row in
  the roadmap table plus a new folder under the console project and under `ArchitectureDiagrams/`.
- **A gotcha or convention worth keeping** (like the `Console` namespace clash) → add it to the
  "Conventions & gotchas" section of `ROADMAP.md` so it's never rediscovered the hard way twice.
