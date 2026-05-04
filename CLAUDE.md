# CLAUDE.md

Repository-specific agent guidance.

## Current Architecture

The Canonry Game is a deterministic microkernel simulation. The current
low-altitude runtime is **not** the old process deck. It is:

- `LowAltitudeVerbs`
- `MapActivity`
- `MapSignals`
- `Relationships`
- `Effects`
- `Upkeep`
- `Gambits`
- `Pressures`
- `Generators`

Behavior bytecode and planner support remain for gambits. Do not reintroduce
standing drives, generated low-altitude processes, or compatibility shims for
the removed process system.

## Required Reading

Before changing engine runtime code, read:

- `docs/kernel/simulation-kernel-discipline.md`
- `docs/architecture.md`
- `docs/systems/simulation-principles.md`

## Hard Rules

Engine code must not reference domain content. No Ice Remembers nouns in
`src/Engine.*`.

`Engine.Kernel` must stay primitive and allocation-disciplined: no runtime
strings, no LINQ, no allocation-heavy collections, no async/threading, no
console or file I/O.

Gameplay-policy vocabulary belongs in `Engine.Gameplay`. Domain content belongs
in `IceRemembers`. The kernel executes compiled ids, codes, floats, and fixed
arrays.

Low altitude can move entities, write influence/activity/signals, modify
attributes, and drift existing relationship strength. It does not create named
entities or routine new relationship topology.

Gambits create or change significant relationships. Generators create named
entities and relationship bundles.

Tests that encode architecture invariants must not be loosened for convenience.
If a test encodes obsolete architecture, update the test and the docs together.

Use `make ci` as the final verification pass. Do not run tests in parallel in
this repo.

Prefer one meaningful verification run over many overlapping runs. If the full
suite is cheap enough for the current task, run the full suite once instead of
stacking focused suites and then rerunning the same coverage.

Run focused tests only while diagnosing a concrete failure or checking a narrow
compile/runtime break before the final verification pass.

Do not rerun passing tests unless code, configuration, generated sources, or
test inputs changed after the previous run. Formatting checks, `git diff`, and
read-only inspection are not reasons to rerun tests.

Before running tests, decide and state the intended verification scope. Run
additional tests only when a new failure or new change creates a specific reason
for them.
