# System 2: Entity Kinds

Entity kinds are domain content registered with the engine through `KindId`.
The engine knows category-level behavior; the domain declares the concrete kind
set.

## Current Ice Remembers Kinds

| Kind | Category | Primary Plane | Current Role |
| --- | --- | --- | --- |
| `region` | Spatial | Semantic | Named geographic or mythic area; owns map coverage. |
| `settlement` | Spatial | Semantic | Built locality; grows culture influence and sometimes coverage. |
| `point_of_interest` | Spatial | Semantic | Named natural feature — vent, peak, krill bloom, shoal. Smaller than a region, never built. |
| `faction` | Agency | Semantic | Organized doctrine, office, compact, cell, or choir. |
| `penguin` | Agency | Physical | Named individual penguin; mobile actor. |
| `orca` | Agency | Physical | Named orca, member of a matriarchal pod; primarily Wake-Singer-aligned. |
| `monster` | Agency | Physical | Bound or partially bound peril. Containment health plus static binding/hunger traits. |
| `artifact` | Asset | Physical | Named relic, instrument, weapon, tome, or seal. |
| `ability` | Asset | Semantic | Combat technique, magical rite, or piece of technology. Multi-holder; carrier (book, marking, tool) is real but the entity lives on the semantic plane. |
| `transport` | Asset | Physical | Named, mobile, large named asset — trade fleet, battleship, scout, or citadel. Significant in its own right; owned and commanded by Agency entities. |
| `occurrence` | Agency | Semantic | Named, active, major event — war, trade conference, plague, festival. Born by generators when relationship and locality gates cross thresholds; resolves when momentum reaches zero. Primary movement is through cultural/conflict space; anchored to the world via relationships. |

The current Ice Remembers kind catalog is fully domain-registered.

## Kind Rules

- Kind is permanent identity. Subtype can change; kind should not.
- The domain declares valid subtypes, health attribute, trait catalog, culture
  labels, category, primary plane, and relationship participation.
- The engine may use categories such as spatial, agency, and asset for generic
  validation and runtime behavior.
- Domain strings are compiled to IDs before reaching kernel runtime.

## Subtypes

Current subtypes are domain vocabulary, not engine concepts:

- `region`: `face`, `shelf`, `depths`, `reach`, `sea`, `scar`
- `settlement`: `rookery`, `colony`, `holdfast`, `sanctuary`, `harbor`, `reefhold`
- `point_of_interest`: `vent`, `peak`, `bloom`, `shoal`
- `faction`: `order`, `compact`, `cell`, `choir`
- `penguin`: `warden`, `broker`, `witness`, `exile`
- `orca`: `matriarch`, `bull`, `scout`, `elder`
- `monster`: `leviathan`, `wraith`, `beast`, `revenant`
- `artifact`: `relic`, `instrument`, `weapon`, `tome`, `seal`
- `ability`: `technique` (combat), `rite` (magic), `craft` (technology)
- `transport`: `trade`, `battleship`, `scout`, `citadel`
- `occurrence`: `war`, `trade_conference`, `plague`, `festival`

Subtype coverage matters for generators and gambits, but every subtype does not
need direct generator coverage. The active monster model uses `containment`
health plus binding/hunger traits; there is no separate monster status field.

## Architecture Rules

- Do not add concrete domain kinds to `Engine.Kernel`, `Engine.Eventing`, or
  `Engine.Constants`.
- If runtime behavior needs to branch by class of entity, prefer category,
  declared plane, declared health attribute, relationship metadata, or compiled
  gameplay config.
- If a new behavior requires concrete kind names in the kernel, the abstraction
  is wrong.
