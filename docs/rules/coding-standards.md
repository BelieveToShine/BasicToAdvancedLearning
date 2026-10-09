# Rule: Coding standards

Small on purpose; grows over time. Follow these; match surrounding style otherwise.

This is a *teaching* repo: the code is read by juniors, so clarity beats cleverness, and comments
explain *why* a concept works the way it does.

## Lesson code shape
- One public `ExplainX()` method per sub-concept in `<Topic>Demo.cs`, called in order from
  `<Topic>Example.Explain()`. Namespace `BasicToAdvancedLearning.<Topic>` — never a `Console` segment.
- Use a running example across a topic's methods so ideas connect (e.g. Collections: exam scores,
  to-do list, phone book).

## No magic values (in real logic)
- Never put a raw literal in non-demo logic — use a named const or configuration.
- Exception: values whose literal-ness *is* the lesson (e.g. `int age = 28;` in Programming Basics).
  Keep those obvious and commented.
- A group of related values → an enum.

## Method summaries & size
- Every new/modified method carries a 1–2 line summary of its purpose (a comment for the learner).
- One method = one responsibility, readable on one screen (~25–30 lines; extract by ~40–50).

## Best-effort work degrades, never aborts
- If a call only enriches the result, wrap it (try/catch → log + fallback); it must not collapse the
  main operation. Ask per call: "if this throws, is the operation still meaningful?"

## Don't inherit violations
- "Follow existing patterns" means style only — never copy an existing rule violation. Fix/extract as
  part of your change. Self-check size/magic-values/summary after each edit.

## Build hygiene
- `dotnet build` stays at 0 errors **and 0 warnings**.

## Growing this file
- When testing finds a defect whose root cause is a repeatable coding mistake, add the scenario here as
  part of the same fix. (Example already captured elsewhere: the `Console` namespace clash —
  `ROADMAP.md` gotcha #1.)
