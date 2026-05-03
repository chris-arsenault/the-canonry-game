# CODEX.md

Codex operating rules for this repository.

## Current Runtime Model

The low-altitude physics rewrite is complete. The old process deck is removed.
Do not preserve or revive it through compatibility layers.

Current behavior layers:

1. Low-altitude verbs, map activity, map signals, relationship maintenance,
   effects, and upkeep keep the world alive per tick.
2. Gambits are entity-authored high-stakes actions.
3. Generators are high-altitude events that create new named entities and
   relationship bundles.

Planning infrastructure remains for gambits. It is not the default
low-altitude behavior model.

## Architecture Rules

Before editing engine runtime code, read
`docs/kernel/simulation-kernel-discipline.md`.

Engine projects stay domain-agnostic. Ice Remembers content belongs in
`src/IceRemembers*`, not `src/Engine.*`.

Reusable gameplay vocabulary belongs in `Engine.Gameplay` and compiles down to
primitive kernel data. The kernel does not own domain strings or gameplay prose.

`Engine.Kernel` must preserve the microkernel constraints: primitive event
boundary, fixed-capacity state, no runtime strings, no hot-path allocations, no
async/threading/I/O, and one sim writer thread.

## Analysis Tooling

For Monte Carlo output, parquet telemetry, tuning reports, and exploratory data
analysis:

- Use SQL, DuckDB, or Python.
- Do not write one-off C# analysis scripts unless the task is product/tooling
  implementation.
- Do not introduce CSV as the analysis format.
- Lead low-altitude analysis with map utilization, roaming variance, influence
  shape, lifecycle churn, and relationship dynamics, not generator fire counts.

## Testing

Use `make ci` as the final verification pass.

Do not run tests in parallel in this repo.

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

Invariant tests encode hard constraints. If they fail, fix the implementation
or explicitly change the invariant. Diagnostic tests can be updated if they are
testing obsolete behavior.

Do not tune game design just to satisfy a broad statistical test unless the
test has been accepted as an invariant.

## Documentation

When architecture changes, update the stable docs:

- `docs/architecture.md`
- `docs/game-systems.md`
- relevant `docs/systems/*.md`
- `docs/systems/simulation-principles.md` for durable simulation-quality goals

Do not leave session trackers, review dumps, or stale migration plans as if
they are current design documents.
