# Rule: Spec docs — read before, update after

## Before a task
1. Read `../architecture/overview.md`.
2. Check the category index in `../superpowers/specs/README.md`; if a spec covers the area, read it
   before reading source.
3. No spec for a non-trivial area you're about to change? Document current behaviour first, then change.

## During a task — track in docs/todo/
- Keep a living working doc per task at `../todo/<TICKET>-####.md` (or `../todo/<short-topic-name>.md`
  while no ticket prefix exists — TODO: decide). It holds the plan, locked decisions, done vs pending.
  This is the ONLY place ticket numbers belong.
- Do NOT edit specs mid-task. Specs describe shipped behaviour; update them at check-in.
- When the work ships: fold the behaviour into the spec, move the working doc to `../todo/archive/`.

## After a change
- If the change falls under an existing category, update that spec as part of the same task (method
  names, folder layout, config keys, flows, known gaps, bugs-as-lessons).
- Genuinely new area → new category folder + a new row in the specs index. Don't shoehorn.
- Topic work also updates the tracker table and "Status snapshot" in `../ROADMAP.md`.

## Link maps & hygiene
- On any doc add/rename/remove, update the global map in `../README.md` and the category's own link map.
- No date prefixes on file or folder names. No ticket numbers inside specs — a reader should navigate
  from the docs alone. (`ROADMAP.md`'s in-body status date is the one deliberate exception: it is a
  running session log, not a spec.)

- `scripts/verify-docs.ps1` must pass (links resolve, categories indexed) — see `verification.md` layer 5.

## Non-negotiable
- "Done" for a documented area includes updated spec content AND updated link maps.
