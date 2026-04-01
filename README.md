# elona-clone

Godot 4.6.1 + C# prototype workspace for a modular Elona-inspired clone.

## Current baseline

- `Godot .NET` presentation project at the repository root
- Pure C# runtime skeleton in `src/Elona.Game`
- Deterministic test runner in `tests/Elona.Game.Tests`
- Built-in starter content for `Home`, `Vernis`, and `Puppy Cave`
- Save/load snapshot round-trip and a tiny playable ASCII runtime view

## Architecture

- `Simulation`: runtime state, action resolution, turn scheduling
- `Content`: runtime definitions produced from Godot `Resource` classes
- `Application`: session bootstrap, zone assembly, starter content wiring
- `Persistence`: versioned save snapshot serialization
- `Presentation`: Godot scene bootstrap, input routing, message rendering

## Development workflow

- `Design-first rule`
  - Before implementing any non-trivial feature, system, refactor, or gameplay module, write or update a design document in `docs/plans/`.
  - Before implementing a feature, first research how `Elona` and closely related feature patterns describe or use that mechanic on public sources such as wiki pages, patch notes, guides, and player blogs.
  - The purpose of that research is not to copy blindly, but to identify what kinds of freedom the system should preserve, including alternate use cases, unusual interactions, multiple play styles, optional constraints, failure cases, and future extension points.
  - Each design document for a gameplay feature should capture the relevant reference behavior and explicitly note which freedom points will be supported now, which are deferred, and which interfaces are being reserved for later.
  - Design documents should capture the goal, scope, in-scope and out-of-scope items, architecture impact, data flow, testing strategy, and acceptance criteria for that module.
  - Large-scale project direction, system boundaries, and long-running feature decisions must be written into repository documents instead of being kept only in chat context.
  - Implementation should follow the approved design in small steps. If scope changes during development, update the design document before continuing.
- A task is only considered complete after it passes the checklist below and any issues found have been fixed.
- `Completion checklist`
  - [ ] `1. Automated verification`
    Build the affected project or solution, run the relevant regression tests, and widen the check scope when shared code was touched.
  - [ ] `2. Code-level self-review`
    Re-read changed files and call sites for signature mismatches, broken references, persistence or serialization mistakes, unsafe defaults, regression risks, leftover debug code, and temporary scaffolding that should not ship.
  - [ ] `3. Manual and edge-case review`
    Walk through the main user flow and nearby edge cases, especially blocked actions, invalid inputs, action-order problems, and behaviors that only appear inside the real game loop.
  - [ ] `4. Verification coverage`
    Add or update stable regression tests when the task introduces or changes behavior that would otherwise be left unverified.
  - [ ] `5. Temporary file cleanup`
    Remove temporary verification files created only for the task, such as throwaway scripts, scenes, logs, or scratch test assets.
  - [ ] `6. Regression test retention`
    Keep stable regression tests in `tests/` unless the task explicitly requires removing or replacing them.
- `Default close-out protocol for Codex`
  - Freeze the task scope before sign-off. Do not keep stacking extra changes onto a completed task unless they are required to fix a bug or complete the agreed behavior.
  - For non-trivial work, make sure the latest design intent is written into `docs/plans/` before or during implementation so important architecture decisions are not lost across turns.
  - For gameplay-facing work, complete the reference research pass before implementation and carry the freedom/variation notes into the design document instead of keeping them only in memory.
  - Run automated verification first, starting with the smallest relevant checks and expanding to broader builds or regression runs when shared code was touched.
  - Perform a code-level self-review on every changed file and its nearby call sites before concluding that the task is stable.
  - Perform a manual and edge-case review of the main player flow affected by the change, even if automated checks already passed.
  - If any bug, regression, mismatch, or suspicious behavior is found during those checks, return to implementation, fix it, and repeat the verification loop until the result is clean.
  - Add or update stable regression coverage when the task changes behavior that could break again later.
  - Remove temporary verification artifacts created only for the task, but keep reusable regression tests and long-term project assets.
  - In the final handoff, clearly state what changed, what was verified, and any remaining risks or things that were not verified.

## Commands

```powershell
dotnet build .\elona.sln -m:1
dotnet run --project .\tests\Elona.Game.Tests\Elona.Game.Tests.csproj
```

For VS Code:

- `Ctrl+Shift+B` runs the default `build` task
- `Terminal > Run Task > test` runs the deterministic test runner
- `F5` uses `.vscode/launch.json` and expects a `GODOT4` environment variable pointing to your Godot executable

## Runtime controls

- Arrow keys: move
- `G`: pick up item on the current tile
- `D`: drop the first inventory item
- `C`: claim a ready quest in the current town
- `Space`: wait
- `T`: travel to the suggested connected zone
