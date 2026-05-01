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
- relationship fact existence
- relationship saturation limit
- attribute threshold
- physical or semantic distance
- map coverage or influence
- pressure threshold
- cooldown / cadence
- target availability

## Effects

Generators may create named entity bundles. Gambits should usually avoid entity
creation and instead change relationships, facts, status, or attributes.

Effect families:

- create entity
- create relationship
- add relationship fact
- modify relationship strength or polarity
- modify status
- modify subtype
- modify attribute
- place entity physically or semantically

## Architecture Rules

- Do not duplicate targeting systems for primary and secondary targets. Use the
  same resolve-or-create query path with explicit `create_if_missing`.
- Do not create card-only marker edges when a real relationship plus fact would
  be understood by the rest of the graph.
- Diagnostic traces should flow through the eventing/observability pipeline, not
  direct file writes from kernel code.
