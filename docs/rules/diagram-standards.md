# Diagram Standards (MANDATORY — every topic)

No topic is "done" (see `docs/ROADMAP.md`) until all three diagrams below exist for it. This is a
hard rule, not a suggestion — apply it every time, without being asked again.

## The three mandatory diagram categories — and the folder they live in

For topic `<Topic>`, laid out under `ArchitectureDiagrams/<Topic>/` like this (see `ControlFlow/` for
the reference example):

```
ArchitectureDiagrams/<Topic>/
  <Topic>.svg                 ← Architecture/concept (one file, topic-wide)
  Flow/
    0-Overview.svg            ← high-level call/return trace across all methods
    1-<FirstMethod>.svg       ← one per method, numbered in the order Explain() calls them
    2-<SecondMethod>.svg
    ...
  Memory/
    0-Overview.svg            ← high-level Stack/Heap/Static overview
    1-<FirstMethod>.svg       ← one per method, same numbering as Flow/
    2-<SecondMethod>.svg
    ...
```

| # | Category | Purpose | Answers |
|---|---|---|---|
| 1 | `<Topic>.svg` | **Architecture / concept diagram** | What is this concept made of? (the pieces, colour-coded, mapped 1:1 to the `Explain*()` methods in `<Topic>Demo.cs`) |
| 2 | `Flow/0-Overview.svg` + `Flow/<N>-<Method>.svg` | **Functional flow diagrams** | Where does execution/data go, step by step, from where to where? (start → ... → end, including any branch) |
| 3 | `Memory/0-Overview.svg` + `Memory/<N>-<Method>.svg` | **Memory management diagrams** | For THIS method's actual variables, which live on the **Stack**, which on the **Heap**, which are **Static**? |

**The numbering matters:** `Flow/<N>-<Method>.svg` and `Memory/<N>-<Method>.svg` use the SAME number
for the same method, so the two folders line up — `Flow/3-ForLoop.svg` and `Memory/3-ForLoop.svg` are
always about the same method. Number in the order `<Topic>Example.Explain()` actually calls them.

All diagrams are flat, hand-authored SVG (no external tools/libraries), same base conventions as the
existing LeadHunter diagrams: `viewBox` sized to content, `font-family="Arial, Helvetica, sans-serif"`,
a title + one-line subtitle, a colour legend, rounded-rect boxes, arrows via one `<marker>`, footer
notes for anything that doesn't fit in a box. Validate every SVG is well-formed XML before calling it
done (`System.Xml.XmlDocument.Load` in PowerShell, or equivalent) — a broken SVG fails silently in a
browser.

## 1. Architecture / concept diagram (`<Topic>/<Topic>.svg`)

Already the established pattern (see `ProgrammingBasics.svg`): pick colours per sub-concept, one
container box per sub-concept with inner example rows pulled verbatim from the code, arrows showing
how they connect, footer explaining how `Program.cs` actually reaches this code (the `ILearningTopic`
indirection). Colours are free to pick per topic — what's locked is the *shape* (legend, columns,
boxes-with-code-examples, footer).

## 2. Functional flow diagrams (`Flow/0-Overview.svg` + `Flow/<N>-<Method>.svg`)

**A call/return sequence diagram, not a linear flowchart.** A plain top-to-bottom chain of boxes does
not answer "where does control go, exactly" — it hides who calls whom and where each call returns to.
Use lifelines instead:

- One vertical **lifeline** per class actually involved in the call chain (typically 3:
  `Program.cs (Main)` → `<Topic>Example` → `<Topic>Demo`), each with a header box at the top and a
  dashed vertical line running down.
- **Solid arrow** = a call, drawn left-to-right when going deeper (caller → callee), labelled with the
  real method name being called (e.g. `ExplainOperators()`).
- **Dashed arrow** = that call *returning* to whoever made it, drawn right-to-left back up. Every call
  gets a matching return — this is what makes "where it goes" unambiguous.
- **Activation bar** (a short filled rectangle on the lifeline) spans the time a method is actually
  running, so it's visually obvious which class is "in control" at any point in the diagram.
- **Note box** beside each call: one or two lines of *high-level* content (concepts covered, e.g.
  "runs arithmetic, comparison, logical and assignment operators") — never variable names, those
  belong only in the Memory diagram. Every note must also explicitly say **which Console methods that
  step uses** (`Console.Write` / `Console.WriteLine` / `Console.ReadLine`) — don't leave console I/O
  implicit, call it out on every activation that does it.
- **`alt` box** (dashed border) for a real branch only (e.g. a `TryParse` success/failure split) —
  labelled `alt — <condition>`, divided into labelled outcomes. Don't invent a branch if the topic has
  none; a topic with no branching is just a straight chain of call/return pairs.
- End with a small filled **End** marker on the root lifeline (after its final return) plus a one-line
  "where to where, in one line" summary in the footer.

**Routing rule (applies to every Flow diagram, sequence or flowchart): straight lines only, no
diagonals, no clipping through a box or oval.** A diagonal connector that crosses other shapes at a
shallow angle reads as tangled, and one that isn't obviously routed can silently pass *through* a box
it should avoid — that's a bug, not just a style nit (a real instance: a do-while loop-back path routed
straight over the setup box, and the arrow appeared to "end nowhere"). When two shapes aren't directly
above/below each other, route with an L-shaped or Z-shaped path of horizontal/vertical segments (a
side "bus" line is fine — see the loop-back paths in `ControlFlow/Flow/3-ForLoop.svg` /
`ControlFlow/Flow/4-WhileLoop.svg`, and the merge paths in `ControlFlow/Flow/1-IfElse.svg`), never a
straight diagonal. Before finishing a Flow diagram, trace every path's coordinates against every box/oval's
bounding box (and, for an oval, its actual curve — a straight bounding-box check can still let a line
clip an oval's corner) and confirm it doesn't cross through anything it isn't meant to touch. A single
clean 90° crossing between two otherwise-independent paths is fine; running alongside/through a shape
is not.

## 3. Memory management diagrams (`Memory/0-Overview.svg` + `Memory/<N>-<Method>.svg`)

**One detailed Memory diagram PER METHOD, no exceptions — this is not optional and not judgement-based.**
Every method in `<Topic>Demo.cs` gets its own `Memory/<N>-<Method>.svg`, in the exact same
Main → `Explain()` → method-frame snapshot shape every time (see `ControlFlow/Memory/1-IfElse.svg`
through `6-Foreach.svg` for six worked examples — five simple, one with real Heap content). A shared
high-level overview (`Memory/0-Overview.svg`) is a floor, not a substitute — it can point at which
methods are worth a closer look, but it never replaces the per-method files.

**Locked colour legend — reuse exactly across every topic's Memory diagrams:**

| Region | Fill | Stroke | Meaning |
|---|---|---|---|
| Stack | `#b6d7a8` | `#6aa84f` | Value-type locals + reference-type *pointers* (the variable slot itself), one frame per active method call |
| Heap | `#f9cb9c` | `#e69138` | The actual object data a reference-type variable points to (strings, arrays, class instances, `List<>`, etc.) |
| Static | `#d5c9ea` | `#674ea7` | `static` fields/consts — one copy for the whole type, lives for the program's lifetime |

**Per-method snapshot layout (locked shape — every `Memory/<N>-<Method>.svg` uses this):**
- **Stack** column: a call stack of exactly 3 frames, always in this order top-to-bottom —
  `<Topic>Demo.<Method>()` (bright green, ACTIVE, lists every local this method declares), then
  `<Topic>Example.Explain()` (dimmed, "paused — waiting for the call above to return"), then
  `Program.cs (Main)` (dimmed, "paused — waiting for topic.Explain() to return"). Inside the active
  frame:
  - Value types (`int`, `double`, `decimal`, `bool`, `char`, structs) → written inline with their value
    (e.g. `age = 28`).
  - Reference types (`string`, arrays, class instances, `List<T>`, etc.) → drawn as a small slot that
    holds a pointer, with an arrow crossing into the **Heap** column to the actual object.
- **Heap** column: if the method has no reference-type locals, say so explicitly in a dashed empty box
  (*"No Heap usage here — `<var>` is a value type, entirely on the Stack."*) — don't just omit the
  column, the ABSENCE of Heap usage is itself something worth a learner seeing method after method.
  If it does have one, draw one box per object a Stack arrow points to, showing its real content.
- **Static** column: always present, always the same empty-state note until some topic actually
  introduces a `static` field — keeping this box identical across every single diagram (not just every
  topic) is what lets a learner compare any two Memory diagrams and immediately recognise the shape.

Use **real variable names from that method**, not placeholders — the whole value of this diagram is a
learner matching a name they just saw in code to a box in the picture. Where a method has a genuinely
notable memory story (a loop counter reusing one Stack slot across every lap; an array being a two-hop
reference into the Heap), say so in the footer in plain words — keep the diagram's shape identical to
every other method's, and carry the extra insight as text, not as a structural difference. Diagrams do
NOT animate — the "snapshot" framing means a single point in time; save motion for the Flow diagrams.

## Scaling: high-level first, then one Flow + one Memory diagram per method

`<Topic>.svg`, `Flow/0-Overview.svg`, and `Memory/0-Overview.svg` are the high-level trio — one diagram
per category, covering the whole topic. On top of that floor, **every method gets its own numbered
Flow AND Memory diagram** (see the folder layout above) — this is now the standing rule for every
topic, not a case-by-case judgement call. A topic with 6 methods produces 2 high-level + 6 Flow + 6
Memory = 14 diagram files, plus the 1 architecture file. That volume is intentional: "quicker
explanation to juniors" means a learner opens exactly the one file about the one method they're stuck
on, not a shared diagram trying to cover six things at once.

The one place judgement still applies is Flow diagram *richness*: a sub-concept with its own distinct
control shape (a loop, a multi-way branch, recursion) needs its own animated/annotated Flow diagram
(see `Flow/3-ForLoop.svg` for the pattern); a simple sub-concept's Flow diagram can be a short,
straightforward chain. Memory diagrams don't get this judgement call — every method gets the full
snapshot format regardless of how simple its variables are (see `Memory/1-IfElse.svg`: one int, still
gets the complete Stack/Heap/Static treatment).

## Animation instead of GIF

When a diagram needs to show *repetition over time* (a loop iterating, a counter incrementing) a
static picture loses the "and then it goes back" part. Default to a **self-contained animated SVG**
(inline SMIL `<animate>` / `<animateTransform>`, or a `<style>` block with CSS `@keyframes`) rather
than a binary `.gif`:
- No external tool or install needed (this machine has no ImageMagick/ffmpeg/Python) — it's still just
  hand-authored markup, opens and loops in any browser, and stays diffable as text like every other
  diagram here.
- Keep it simple and purposeful: animate the ONE thing that needs motion to be understood (e.g. the
  loop-back arrow pulsing, or a counter box cycling through its values with `set`/`animate`), not the
  whole diagram — a diagram that never stops moving is harder to read than one that's still.
- Give the animation a generous `dur` (2–4s per cycle) and let it `repeatCount="indefinite"` so it
  reads as "this keeps happening" without being distracting.
- Only reach for a real `.gif` if a specific destination needs a portable image file that can't render
  SVG (and that would need installing Pillow or ImageMagick first — ask before adding that dependency).

## Where this is enforced

`docs/ROADMAP.md` → "Add a new topic" runbook, step 4, points back here. A topic's tracker row does
not get a ✅ until: the architecture file exists; `Flow/0-Overview.svg` and `Memory/0-Overview.svg`
exist; and EVERY method in `<Topic>Demo.cs` has both a `Flow/<N>-<Method>.svg` and a
`Memory/<N>-<Method>.svg` sharing the same number — all valid XML.
