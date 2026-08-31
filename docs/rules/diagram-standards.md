# Diagram Standards (MANDATORY — every topic)

No topic is "done" (see `docs/ROADMAP.md`) until all three diagrams below exist for it. This is a
hard rule, not a suggestion — apply it every time, without being asked again.

## The three mandatory diagrams

For topic `<Topic>`, all three live in `ArchitectureDiagrams/<Topic>/`:

| # | File | Purpose | Answers |
|---|---|---|---|
| 1 | `<Topic>.svg` | **Architecture / concept diagram** | What is this concept made of? (the pieces, colour-coded, mapped 1:1 to the `Explain*()` methods in `<Topic>Demo.cs`) |
| 2 | `<Topic>Flow.svg` | **Functional flow diagram** | Where does execution/data go, step by step, from where to where? (start → ... → end, including any branch) |
| 3 | `<Topic>Memory.svg` | **Memory management diagram** | For the actual variables in this topic's code, which live on the **Stack**, which on the **Heap**, and which are **Static**? |

All three are flat, hand-authored SVG (no external tools/libraries), same base conventions as the
existing LeadHunter diagrams: `viewBox` sized to content, `font-family="Arial, Helvetica, sans-serif"`,
a title + one-line subtitle, a colour legend, rounded-rect boxes, arrows via one `<marker>`, footer
notes for anything that doesn't fit in a box. Validate every SVG is well-formed XML before calling it
done (`System.Xml.XmlDocument.Load` in PowerShell, or equivalent) — a broken SVG fails silently in a
browser.

## 1. Architecture / concept diagram (`<Topic>.svg`)

Already the established pattern (see `ProgrammingBasics.svg`): pick colours per sub-concept, one
container box per sub-concept with inner example rows pulled verbatim from the code, arrows showing
how they connect, footer explaining how `Program.cs` actually reaches this code (the `ILearningTopic`
indirection). Colours are free to pick per topic — what's locked is the *shape* (legend, columns,
boxes-with-code-examples, footer).

## 2. Functional flow diagram (`<Topic>Flow.svg`)

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

## 3. Memory management diagram (`<Topic>Memory.svg`)

**Locked colour legend — reuse exactly across every topic's Memory diagram:**

| Region | Fill | Stroke | Meaning |
|---|---|---|---|
| Stack | `#b6d7a8` | `#6aa84f` | Value-type locals + reference-type *pointers* (the variable slot itself), one frame per active method call |
| Heap | `#f9cb9c` | `#e69138` | The actual object data a reference-type variable points to (strings, arrays, class instances, `List<>`, etc.) |
| Static | `#d5c9ea` | `#674ea7` | `static` fields/consts — one copy for the whole type, lives for the program's lifetime |

Layout convention:
- **Stack** column: draw it as a call stack — one rectangle per active method frame, oldest/caller at
  the bottom, currently-executing method on top (brighter fill; paused caller frames dimmed/greyed).
  Inside the *active* frame, list every local variable declared in that method:
  - Value types (`int`, `double`, `decimal`, `bool`, `char`, structs) → written inline with their value
    (e.g. `age = 28`).
  - Reference types (`string`, arrays, class instances, `List<T>`, etc.) → drawn as a small slot that
    holds a pointer, with an arrow crossing into the **Heap** column to the actual object.
- **Heap** column: one box per reference-type object that a Stack arrow points to, showing its real
  content (e.g. the string `"Vivek"`).
- **Static** column/strip: always present even when empty — if the topic has no `static` fields yet,
  say so explicitly (e.g. *"Not used yet — first static example arrives in the OOP topic."*). Keeping
  the three regions structurally identical across every topic's Memory diagram is the point: a learner
  should be able to compare Memory diagrams across topics and immediately recognise the same three
  boxes.

Use **real variable names from that topic's `<Topic>Demo.cs`**, not placeholders — the whole value of
this diagram is a learner matching a name they just saw in code to a box in the picture.

## Where this is enforced

`docs/ROADMAP.md` → "Add a new topic" runbook, step 4, points back here. A topic's tracker row does
not get a ✅ until all three diagram files exist and pass the XML-well-formed check.
