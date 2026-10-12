# Topic 8 — File I/O basics (working doc)

Plan, decisions and status for Topic 8. Once shipped, the behaviour is folded into `ROADMAP.md` and
`superpowers/specs/learning-topics/overview.md`; this file stays as the record of the plan.

## Plan (as built)
- **Folder / namespace:** `FileIO/` and `BasicToAdvancedLearning.FileIO`; `FileIODemo.cs`, `FileIOExample.cs`
  (no extra class files — the demo calls .NET's own `File`, `Path`, `StreamWriter`, `StreamReader`).
- **Running example:** a one-line bank statement, `bta-statement.txt`, written/read/deleted in the OS temp folder.
- **Methods, in call order:** `ExplainWriteAndReadText`, `ExplainLinesAndStreams`, `ExplainPathsAndExistence`,
  `ExplainFileErrors` (builds on Topic 7's `FileNotFoundException`).
- **Memory diagrams:** locked 3 frames and columns; each adds a dashed grey "OUTSIDE .NET" box under the Heap for the
  OS file handle and the file on disk (recorded addition). Streams are drawn simplified and say so.
- **Flow metaphors:** filing cabinet · water pipe with a tap · postal address labels · lost-property desk.
- **Diagram list (11):** `FileIO.svg`, `Flow/0-Overview.svg`, `Memory/0-Overview.svg`, and
  `Flow/<N>-<Method>.svg` + `Memory/<N>-<Method>.svg` for N = 1–4.

## Status
- [x] Code: demo + adapter, `Program.cs` wired; temp file cleaned up after every method
- [x] 11 diagrams drawn
- [x] Docs: ROADMAP tracker + snapshot + gotchas, learning-topics spec, overview, architecture
- [ ] Verification layers 1–5, then check-in (recorded in the end-of-work report)
