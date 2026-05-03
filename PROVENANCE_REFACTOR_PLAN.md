# Provenance and Generator Causal Limit Refactor Plan

This plan records the current state after moving production history into
kernel provenance and removing the old relationship edge-annotation layer.
The active model is:

- relationship kinds describe reusable graph meaning;
- relationship strength carries magnitude for graph gates;
- provenance records what produced entities, relationships, and changes;
- generator causal limits prevent repeat firing by changing ordinary world
  state, not by keeping hidden per-target memory.

## Completed

- Kernel provenance invocations and outputs are preallocated, append-only,
  snapshot persisted, and fail with typed capacity errors when exhausted.
- Kernel events carry `ProvenanceInvocationId`.
- Generator fires open provenance scopes and record produced outputs.
- Relationship-gated generators bind the concrete trigger relationship and
  use that relationship creator invocation as parent when present.
- Entity-only generator triggers bind the target entity as trigger evidence.
- Gambit invocations bind primary actor, secondary target, and concrete
  provider evidence where available.
- Projection-level provenance read helpers index invocations, outputs,
  created-by entity/relationship lookups, parent traversal, and relationship
  trigger traversal.
- `Engine.Projection` now stays domain-free: it exposes generic provenance
  query helpers but no chronicle pattern recognition.
- The Ice Remembers domain compiles an initial curated two-hop chronicle query
  for `artifact_cleanses_scar` -> `what_the_spring_remembers` through the
  concrete `cleansed_by` trigger relationship.
- The game layer executes compiled chronicle queries against the generic
  provenance projection, matching the same boundary pattern used for card
  definition knowledge.
- `taught_by` is an ordinary relationship.
- The old relationship edge-annotation layer has been removed from domain
  config, ID registries, definition graph nodes, compiler mappings, host
  installers, kernel relationship storage, query filters, event payloads,
  UI projection rows, telemetry rows, seed data, and tests.

## Generator Causal Limits

Generator causal limits replace saturation. A generator should not ask whether
a target has already been served. It should ask whether visible world state
justifies the event and what the event consumes, relieves, transforms, or locks
so the same situation is no longer immediately eligible.

Current Ice Remembers mappings:

| Generator | Persistent cause | Causal spend / transform |
| --- | --- | --- |
| `ice_settles_accounts` | A concrete `at_war_with` trigger edge plus an offered penguin from the target culture. | The offered penguin becomes historical and the selected `at_war_with` pair is broken. |
| `war_breaks_the_land` | A concrete `at_war_with` trigger edge plus local land that can still be scarred. | The generator requires fewer than 8 local `wastes` cells, then spends finite map state into scar coverage / waste terrain. |
| `what_the_spring_remembers` | A concrete incoming `cleansed_by` trigger edge on a region. | The region must have no existing incoming `anchored` relationship; the event creates durable anchored state. |
| `routes_draw_readers` | A concrete `trade_route` trigger edge from the settlement. | The selected trade route must be strong, then the route pair loses strength. |
| `what_the_artifact_knows` | A concrete `holds` trigger edge from penguin to artifact. | The penguin must have no outgoing `home_base`; the event creates one. |

`war_makes_wardens` was deleted instead of being reshaped into a larger
occurrence generator.

## Remaining Work

- Keep repeat limits expressed through causal spend or durable output-state
  gates.
- Add or maintain compiler/domain checks that every persistent generator gate
  has a matching consume, transform, or output-locking effect unless the
  generator is explicitly safe to repeat.
- Add tests for each causal limit proving:
  - the same cause cannot immediately fire again after being spent;
  - newly created causes and unrelated targets remain eligible;
  - ordinary gameplay can rebuild eligible causes where intended.
- Extend domain-compiled chronicle queries with more curated k-hop chains such
  as teacher -> student -> ability use, hostility -> war, and war ->
  scar/memorial. Keep the projection layer generic; query semantics belong in
  domain config/compiler output and game presentation.

## Documentation To Keep Aligned

- `docs/systems/07-relationships.md`
- `docs/systems/16-generator-executor.md`
- `docs/systems/18-action-executor.md`
- `docs/systems/20-generator-causal-limits.md`
- `docs/systems/event-log.md`
- narrative / chronicle notes

## Testing Requirements

- Kernel no-allocation checks continue to pass.
- Provenance capacity exhaustion tests cover invocation and output capacity.
- Snapshot tests prove provenance history survives save/load.
- Generator tests cover trigger relationship binding and causal spend.
- Gambit tests cover provider evidence binding.
- Chronicle tests cover the current `artifact_cleanses_scar` ->
  `what_the_spring_remembers` query at the game/domain boundary.
