# Agent Instructions

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

Production seed worlds used for gameplay, review, screenshots, diagnostics, or
evidence must be generated in Release mode. Never build or generate a seed world
from Debug artifacts. Use an explicit Release invocation such as
`dotnet run --project tools/WorldGenerator -c Release -- ...`; if `--no-build`
is used, first verify that the selected artifact path is the Release output from
the current source. A world produced by a Debug generator is invalid and must
not be reviewed or reported as evidence.

Do not add helpers, abstractions, tests, telemetry, docs, or architecture unless
they are explicitly part of the approved proposal.

When the user asks whether something is complete, answer against the agreed
scope only. Do not expand completion criteria to adjacent future work.

When the user says not to make code changes, do not edit files. Provide analysis
only.

When a previous answer or proposal is corrected, retain the correction as a
durable constraint for the rest of the task. Do not discard earlier accepted
requirements merely because newer feedback is more recent.

### Design synthesis across feedback

Treat design feedback as cumulative evidence, not as an instruction to invert
the previous proposal. Before revising a design, synthesize the full relevant
conversation and design history: the original goal, observed failures, accepted
decisions, rejected approaches, architectural constraints, and the newest
feedback. The revised proposal must be one coherent causal design that satisfies
that combined record. Do not merely restate the user's latest message.

For every affected process or subsystem, deliberately choose and justify one of
these dispositions:

- keep it unchanged because its model and output are sound;
- tune or correct its current algorithm while preserving its contract;
- extend it with additional state or behavior;
- add a new discrete stage for a genuinely missing process;
- replace only the incompatible internal portion while preserving useful
  surrounding structure; or
- remove it because it is redundant, causally wrong, or incompatible with the
  accepted design.

The result will often be a mixed evolution: substantial reuse and targeted
improvement alongside a few new independent stages such as erosion, glaciation,
or reconciliation. Feedback that a proposal rewrites too much does not mean no
new stages may be added; feedback that existing stages should be reused does not
mean they must be preserved unchanged. Likewise, feedback that one behavior is
overrepresented does not mean replacing it everywhere with its opposite.

Balance is not an arbitrary compromise or the midpoint between two extremes.
Choose the technically correct mixture from the causal model and evidence, with
different dispositions where different parts warrant them. Explicitly explain
how the revised proposal integrates prior decisions with the latest correction,
and call out any real conflict instead of silently dropping one side.

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

## Memory Budget

Design for an application working set of approximately 4 GB. A working set up
to 8 GB is acceptable when it supports a cleaner, more durable data shape or
useful precomputation. Do not optimize the architecture for a sub-1-GB process
unless the user explicitly asks for that constraint.

Prefer simple direct fixed-capacity arrays, explicit indexes, and precomputed
state over compact encodings that add complexity solely to minimize memory.
This memory envelope does not relax the kernel requirements for bounded state,
explicit capacity validation, deterministic behavior, or no hot-path
allocations.

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

A test must earn its place. Write a test only to (1) encode a real, durable
contract — behavior, an invariant, a schema/graph rule, an architecture boundary
— or (2) as a temporary scaffold that drives a cleanup or refactor and is
DELETED once that work lands. Do not add a test for every little correction.

Never add brittle guards that ban a word or symbol name across the source to
"lock in" a removal or refactor. They outlive their purpose and punish unrelated
code (a source-wide ban on the word "pressure" once broke an unrelated diving
technology). When a migration is done, delete its guard tests instead of keeping
them forever. Prefer deleting a low-value, obsolete, or content-mirroring test
over updating it — tests are a cleanup mechanism, not a permanent record of every
past change.

Use `make ci` as the final verification pass.

Do not run tests in parallel in this repo.

Default to the repo-level test commands. Use `make test` when the work needs a
test pass, and `make ci` when the work needs final verification.

Hard rule: do not run individual `dotnet test tests/*.csproj` suites as a
verification strategy. This includes "just the affected project", a sequence of
focused projects, or one project per subsystem. In this repo that is slower than
using the repo-level commands and gives worse signal.

The default verification decision is binary:
- For normal test verification, run `make test`.
- For final verification, run `make ci`.

Run a focused test command only when one of these is true:
- The user explicitly asked for that exact focused test command.
- A repo-level `make test` or `make ci` run already exposed a concrete failing
  test, and the focused command is being used only to shorten the debug loop for
  that failure.
- You are changing a single known failing test and need its exact repro while
  developing the fix.

After any focused debug run, return to `make test` or `make ci` for
verification. Do not enumerate adjacent projects or subsystem suites.

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

## Canonical Commands

Never hand-roll `dotnet run`/`dotnet test` invocations for simulation runs,
worldgen, captures, diagnostics, or scoped test suites. Use the Makefile
targets (`make mc`, `make world`, `make capture`, `make diagnose`,
`make test-kernel|test-domain|test-seed|test-gameplay`) - they encode the
flags that hand-rolled commands repeatedly get wrong: Release configuration,
unique Monte Carlo run ids, fresh builds for the MC provenance check, and
the tools' current argument names. If a needed invocation has no target,
add the target first, then run it.
