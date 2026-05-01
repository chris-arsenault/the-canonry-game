# System 2: Entity Kinds

Entity kinds are domain content registered with the engine through `KindId`.
The engine knows category-level behavior; the domain declares the concrete kind
set.

## Current Ice Remembers Kinds

| Kind | Category | Primary Plane | Current Role |
| --- | --- | --- | --- |
| `region` | Spatial | Semantic | Named geographic or mythic area; owns map coverage. |
| `settlement` | Spatial | Semantic | Built locality; grows influence and sometimes coverage. |
| `faction` | Agency | Semantic | Organized doctrine, office, compact, cell, or choir. |
| `penguin` | Agency | Physical | Named individual penguin; mobile actor. |
| `artifact` | Asset | Physical | Named relic, instrument, weapon, tome, or seal. |

Future kinds such as orca, transport, monster, point of interest, and occurrence
should be added through the same domain registration path, not as engine enums.

## Kind Rules

- Kind is permanent identity. Subtype can change; kind should not.
- The domain declares valid subtypes, health attribute, culture labels, category,
  primary plane, and relationship participation.
- The engine may use categories such as spatial, agency, and asset for generic
  validation and runtime behavior.
- Domain strings are compiled to IDs before reaching kernel runtime.

## Subtypes

Current subtypes are domain vocabulary, not engine concepts:

- `region`: `face`, `shelf`, `depths`, `reach`, `sea`, `scar`
- `settlement`: `rookery`, `colony`, `holdfast`, `sanctuary`, `harbor`, `reefhold`
- `faction`: `order`, `compact`, `cell`, `choir`
- `penguin`: `warden`, `broker`, `witness`, `exile`
- `artifact`: `relic`, `instrument`, `weapon`, `tome`, `seal`

Subtype coverage matters for generators and gambits, but every subtype does not
need generator coverage immediately.

## Architecture Rules

- Do not add concrete domain kinds to `Engine.Kernel`, `Engine.Eventing`, or
  `Engine.Constants`.
- If runtime behavior needs to branch by class of entity, prefer category,
  declared plane, declared health attribute, relationship metadata, or compiled
  gameplay config.
- If a new behavior requires concrete kind names in the kernel, the abstraction
  is wrong.
