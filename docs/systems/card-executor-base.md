# Card Executor Base

This file records the shared contract for high-altitude cards. The current
runtime has concrete generator and gambit executors; system cards remain future
work.

## Shared Concepts

- A card has preconditions.
- Target finding is part of precondition evaluation.
- A card has ordered effects.
- A card tracks last run, execution count, and waiting diagnostics.
- A card is compiled from domain/gameplay config into kernel IDs before runtime.

## Preconditions

Useful precondition families:

- entity kind/subtype/culture
- relationship existence or absence
- relationship strength threshold
- health threshold
- trait presence or absence
- physical or semantic distance
- map coverage or influence
- map-signal threshold
- cooldown / cadence
- target availability

## Effects

Generators may create named entity bundles. Gambits should usually avoid entity
creation and instead change relationships, status, health, traits, or rare
boolean edge qualifiers.

Effect families:

- create entity
- create relationship
- add boolean relationship qualifier, if the fact system remains
- modify relationship strength or polarity
- modify status
- modify subtype
- modify health
- assign trait
- place entity physically or semantically

## Architecture Rules

- If a numeric gate is required, prefer relationship strength. Use health only
  when survival, damage, repair, or decay is the gate. Locality-based queries
  are acceptable when map or semantic context is the point.
- Do not duplicate targeting systems for primary and secondary targets. Use the
  same resolve-or-create query path with explicit `create_if_missing`.
- Do not create card-only marker edges when a real relationship plus fact would
  be understood by the rest of the graph.
- Diagnostic traces should flow through the eventing/observability pipeline, not
  direct file writes from kernel code.
