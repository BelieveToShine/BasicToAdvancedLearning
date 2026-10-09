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

All diagrams are flat, hand-authored SVG (no external tools/libraries) with these base conventions:
`viewBox` sized to content, `font-family="Arial, Helvetica, sans-serif"`,
a title + one-line subtitle, a colour legend, rounded-rect boxes, arrows via one `<marker>`, footer
notes for anything that doesn't fit in a box. Validate every SVG is well-formed XML before calling it
done (`scripts/verify-topic.ps1` does this) — a broken SVG fails silently in a browser.

### How to author a new SVG (start from a copy — never from a blank page)
1. Copy the nearest existing file of the same kind and rewrite its content, keeping its `<svg>`
   header, `<defs>`/`<marker id="a">`, white background `<rect>`, title/subtitle/legend skeleton and
   footer pattern verbatim:
   - Memory per-method → `ArchitectureDiagrams/Collections/Memory/1-Arrays.svg` (has the Heap-with-pointer
     case) or `ControlFlow/Memory/1-IfElse.svg` (value types only, empty-Heap note).
   - Flow per-method → the closest metaphor in `Collections/Flow/` or `ControlFlow/Flow/`; loops → `ControlFlow/Flow/3-ForLoop.svg`.
   - Architecture → `Collections/Collections.svg`. Overviews → the matching `0-Overview.svg`.
2. Layout skeleton (from `Collections/Memory/1-Arrays.svg`): `viewBox="0 0 1050 <h>"`; title at
   `y=26` (19px, bold), subtitle `y=46` (12px, grey), legend swatches row at `y=60`, content below,
   italic footer text near the bottom. Flow diagrams use a narrower `0 0 900 <h>` with title `y=20`.
   Use these as defaults; size the `viewBox` height to the content.
3. Title format: `<Topic> — <Method> — Memory: Stack / Heap / Static` (Memory) or
   `<Topic> — Sub-level Flow: <metaphor>` (Flow). Subtitle says what instant/idea is shown.
4. Work out box sizes from the longest text: ~6px per character at 11px Arial. If a label doesn't fit,
   shorten it or widen the box — never let text cross a border.
5. After drawing, trace every connector's coordinates against every box (and oval) bounding box by
   hand; `verify-topic.ps1` only catches diagonals, not collisions or overflow. Open the SVG in a
   browser and tick the layer-3 checklist in `verification.md`.

### Instance methods and constructors (Memory diagrams for OOP and later topics)
The locked shape has exactly 3 frames (`<Method>`, `Explain()`, `Main`), which fits static-style demo
methods. For code that runs *inside* an object (instance methods, constructors, property setters):
- Default: keep the 3 locked frames. Draw each object as a Heap box listing its **fields with their
  real values**; the Stack local holds a pointer into it. Carry `this` and the constructor's work as a
  footer sentence — the shape stays identical to every other method's.
- Only when a single diagram's whole point is the instance frame itself (e.g. "what `this` is during
  a constructor"), a 4th stacked frame for that call, plus a `this` arrow into the Heap, is allowed on
  **that one** Memory diagram. Treat it as a locked-shape exception: say why in the footer and record
  it in `ROADMAP.md` session notes and the `verification.md` accepted-exceptions list.
- Confirmed in Topic 5: the default was used for every method (no 4th-frame exception). Objects are Heap
  boxes listing real field values; `this` and constructor chaining (`: this(...)`) ride in the footer.

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

**"Call road" motif — use this instead of a flat box-chain whenever a method's body is mostly a
SEQUENCE OF CALLS to other methods** (as opposed to branching/looping within itself — those still use
the flowchart+diamond style, e.g. `ControlFlow/Flow/1-IfElse.svg`). A stack of plain rectangles reads
as boring and hides the call/return nature; a road with off-ramps makes it visible and is genuinely
more engaging:
- A thick horizontal **road** (a bold rounded line, dark fill, dashed lighter centerline for texture)
  is the method's own execution, running left to right.
- **Start/End flags** (a pole + triangular pennant, green for Start, red for End) instead of plain
  ovals — same meaning, more visual interest.
- Each call is an **off-ramp**: a solid arrow drops straight DOWN from a point on the road into a
  colour-coded bubble (the callee), labelled `call: Method(args)`. A DASHED return arrow then goes
  straight down from the bubble, then straight right, then straight UP to a landing point further
  along the road — an orthogonal "dip down and loop back" shape, never a diagonal. Label the return
  arrow with what actually comes back (`returns 8`, `returns (void)`, or the specific insight worth
  calling out — see `MethodsAndParameters/Flow/2-Parameters.svg` for return labels that carry the
  topic's key point: `returns — number now 20 (shared!)`).
- Reuse a sub-concept's Memory-diagram colour where it exists — e.g. the purple "aliased" fill from
  the locked Memory legend on a `ref`/`out` bubble — so the Flow and Memory diagrams visually agree.
- Number of off-ramps = number of calls that method actually makes; don't pad or invent extra ones.
- **Label placement: never center a label ON the line it describes.** A label sitting at the same
  x/y as a stroke it's naming reads as broken — the line shows through the gaps between letters (a
  real instance: `call: Sum(1, 2, 3)` centered directly on its own vertical arrow looked cut in half).
  Put the label just to one side or, for the "call:" label on an off-ramp specifically, as a small
  sign floating just ABOVE the road before the arrow starts — never on the arrow's path itself. A
  label may brush at most one single character of a line/arrow it crosses; if more than that overlaps,
  move the label. Before finishing a Flow diagram, check every label's text this way, same as the
  path-vs-box collision check above.
- **Encouraged: don't default to the exact same "call road" every time.** The motif above is the fix
  for flat box-chains, not the only allowed shape — for a future topic, consider other metaphors that
  fit the content (a relay handoff, a conveyor/assembly line, a subway line with stations, a recipe
  card with steps and a "results" tray). Pick whatever makes THAT topic's actual mechanics click
  fastest and looks genuinely inviting — the goal is a learner wanting to look at the picture, not
  just tolerating it. Keep the hard rules (straight lines, label clearance, real call/return arrows,
  locked Memory legend) no matter which metaphor you pick.

**Routing rule (applies to every Flow diagram, sequence, road, or flowchart): straight lines only, no
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
- **Static** column: always present, always the same empty-state note until some method actually
  uses a `static` member (first done in Topic 5's `Memory/5-StaticMembers.svg`, where the column widens to
  hold the type's static fields — a `static` pointer field arrows into the Heap like any other pointer) — keeping this box identical across every single diagram (not just every
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
