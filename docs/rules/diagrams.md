# Rule: Diagrams

The authoritative, detailed rules live in [`diagram-standards.md`](diagram-standards.md) — read it in
full before creating or editing any diagram. This file is the AIDLC-level summary.

## The three mandatory views (per topic)
- **Architecture / concept** — `ArchitectureDiagrams/<Topic>/<Topic>.svg` (what the concept is made of).
- **Functional flow** — `Flow/0-Overview.svg` + `Flow/<N>-<Method>.svg` (call/return trace per method).
- **Memory** — `Memory/0-Overview.svg` + `Memory/<N>-<Method>.svg` (Stack/Heap/Static snapshot per method).

`Flow/<N>` and `Memory/<N>` use the same number for the same method. A topic is not ✅ until all exist.

## Workflow
- Hand-authored SVG is the editable source. Validate every SVG as well-formed XML before calling it done.
- Build high-level first (the trio), then per-method files.
- Mechanical checks: `scripts/verify-topic.ps1`; the visual checklist and independent review are in `verification.md`.
- Diagrams stay uncommitted until a human says to commit.
- TODO: this repo currently writes finished diagrams directly under `ArchitectureDiagrams/`. If a
  draft/approval step is wanted (`docs/diagrams/drafts/` → approved), decide it here.

## Style essentials (full detail in diagram-standards.md)
- Straight, right-angle connectors only — never diagonal, never through a box.
- Labels never sit on the line they describe; text must fit inside its box.
- Locked Memory legend: Stack green, Heap orange, Static purple — identical across every topic.
- Always a title, one-line subtitle, and legend.
- Animation (SMIL) instead of GIF where repetition over time must be shown.
