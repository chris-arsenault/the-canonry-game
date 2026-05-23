# CLAUDE.md

Repository operating rules for this project.

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

## Application Layers

Seed/worldgen builds the startup world: maps, terrain, subdivisions, initial
entities, initial relationships, and initial physical/semantic placement.

`Engine.Kernel` is the deterministic simulation runtime: fixed-capacity state,
primitive events, program execution, movement, relationships, generators,
gambits, effects, upkeep, map activity, map signals, culture boundaries,
culture slots, and minor actors.

`Engine.Gameplay` is the reusable authored-rules vocabulary that compiles domain
configuration into primitive kernel program data.

`src/IceRemembers*` is the domain content layer: Ice Remembers kinds, terrain,
relationships, verbs, gambits, generators, lifecycles, effects, and seed data.

Low-altitude verbs are committed entity behaviors that create ordinary movement,
culture influence, relationship-strength changes, and other entity-driven world
churn.

Minor actors are unnamed worker cohorts in kernel state, not graph entities.
They provide frequent local labor for culture, roads, and lifecycle support.

Map activity and map signals summarize where entities act and emit support,
threat, capacity, and attraction pressures over physical and semantic space.

Relationship maintenance is passive per-tick strength drift for existing
relationships, not topology creation.

Effects and upkeep apply continuous health, attribute, lifecycle, and attachment
consequences after ordinary simulation activity.

Gambits are active entity-authored strategic actions with preconditions,
success chances, costs, and explicit effects.

Generators are high-altitude background events that create named entities,
relationship bundles, coverage, and major world developments.

Observability and Monte Carlo tooling consume primitive telemetry into parquet
and analysis reports; they do not define simulation behavior.

Projection and UI render simulation state for inspection and play; they should
not become hidden sources of domain logic.

Assets provide visual/audio material and generation prompts; they do not define
runtime mechanics.

## Required Workflow

At the start of any non-trivial repo task, read this file and any directly
relevant tracking or design document before proposing or editing.

For simulation design, balance, Monte Carlo analysis, terrain, seed placement,
relationship dynamics, generators, gambits, telemetry, or architecture work, do
not implement before first providing a proposal and receiving approval.

Every proposal must state the owning layer, the observed failure, the causal
hypothesis, the exact intended files, and what will not be changed.

Start from the finding or design document, then trace to code. Do not start from
the nearest code surface and invent a local fix.

Prefer narrow authored-domain tuning when the diagnosed issue is balance or
content. Change engine runtime only when the runtime model itself is the
diagnosed cause.

New IceRemembers startup seed entity names should be generated or selected
through `tools/NameForge` for the entity's culture, kind, and subtype. Preserve
hand-authored names only for deliberate culture anchors or canonical lore
figures/artifacts, not as a default shortcut. Every startup seed entity also
needs an explicit short reference/inheritable name for generated descendants and
provenance contexts.

Do not add helpers, abstractions, tests, telemetry, docs, or architecture unless
they are explicitly part of the approved proposal.

When the user asks whether something is complete, answer against the agreed
scope only. Do not expand completion criteria to adjacent future work.

When the user says not to make code changes, do not edit files. Provide analysis
only.

When a previous answer or proposal was corrected, treat the correction as a new
constraint for the rest of the task.

Do not create or switch to a feature branch unless the user explicitly commands
branching. Always commit directly to `main` unless the user specifically
instructs otherwise.

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
- Lead low-altitude analysis with map utilization, roaming variance, culture
  shape, minor actor labor, lifecycle churn, and relationship dynamics, not
  generator fire counts.
- Every Monte Carlo evidence run must use the MC CLI registry workflow with a
  unique human-readable `--run-id`; cite run ids in findings, not only paths.
- Do not create, rename, edit, or delete Monte Carlo registry or manifest files
  manually. Use the MC CLI for run creation, shell registration, stat appends,
  analysis updates, listing/showing, and deletion.
- Do not use `--no-build` for Monte Carlo evidence. If stale-build provenance
  fails, rebuild and rerun under a new registered run id.
- Record important ad hoc SQL or DuckDB findings with `append-stats` before
  reporting them, so follow-up analysis does not rely on chat history.

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

When changing IceRemembers domain definitions or seed data, do not preserve
low-value tests that merely mirror authored content. If a test only asserts that
a specific gambit, generator, or seed entry contains the exact precondition,
relationship, output, or placement just written in the definition, delete it
instead of updating it. Prefer tests that verify real contracts or behavior:
compiler behavior over small synthetic domains, graph/schema invariants, seed
validity, runtime simulation behavior, and Monte Carlo outcomes.

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
