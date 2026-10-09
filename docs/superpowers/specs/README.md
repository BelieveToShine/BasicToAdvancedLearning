# Feature specs — format & index

Read before adding or touching anything here. Same format everywhere is the point: a session that has
never seen this repo should open one folder and be productive without re-deriving from source.

## Category index
| Category | Covers |
|---|---|
| [learning-topics/](learning-topics/overview.md) | How a lesson topic is built: demo + adapter code, the runner, and the mandatory diagram set. Covers topics 1–4 as shipped. |

Add a row whenever a new category folder is created (e.g. a Web API or React category when those
phases start).

## Folder & file convention
- One folder per feature area: `<category>/`. No date prefixes anywhere.
- Exactly one `overview.md` per category (mandatory entry point) + as many topic docs as the area needs.

## What `overview.md` must contain
1. "Start here" one-liner.
2. A sub-parts table linking to each topic doc.
3. Architecture-at-a-glance (ASCII call/data-flow, not prose).
4. Running it locally (setup, emulators, required config).
5. Configuration reference (every key, default, purpose).
6. Known gaps / deliberate scope cuts + open questions.
7. Real bugs found & fixed, framed as general lessons.

## A topic doc must contain
- Schema tables for any table it owns; the actual algorithm/flow with real method names to grep;
  and *why*, not just *what* (record rejected alternatives). Back-link to overview; cross-link siblings.

## Process
- Gather first (read code end to end + history), identify natural topic splits, write topic docs then
  the overview, cross-link, retire superseded notes, update this index. No ticket numbers in specs.
