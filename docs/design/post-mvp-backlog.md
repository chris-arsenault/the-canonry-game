# Post-MVP Backlog

Deferred ideas. These are not commitments.

## Card System

- Deck/hand/tableau loop with draw cadence, hand limits, costs, and discard.
- Card modifications inspired by Balatro-style layered changes.
- Archetypes that select a starting theory of the world.
- Hidden-trigger cards that are visible in the tableau but obscure their exact
  timing.
- One-shot intervention cards that break the normal indirect-control rule at a
  visible cost.

## World-Shaping Systems

- Macro system cards that change broad conditions without becoming hidden
  low-altitude planners.
- Weather, seasons, and terrain-changing events.
- Religious or ideological systems if they can be represented without creating
  hidden director logic.

## Entity Kind Extensions

Shipped as registered kinds (no gambits or generators yet): `orca`,
`point_of_interest`, `ability`, `monster`, `transport`, `occurrence`. See
`docs/systems/entity-kind-registry.md` for the active registry.

Still deferred — these are smaller follow-ons to the above:

### Monster status mechanic

The `monster` kind is registered but the sealed/unbound/risen status mechanic
is deferred. Status is not a subtype (subtypes are types: leviathan, wraith,
beast, revenant). A future status field on the entity, or a relationship
expressing containment topology, will track which state a given monster is in.

### Ability transmission relationships

The `ability` kind uses `practiced_by` for multi-holder ownership and
`taught_by` for teaching lineage. Future work can add richer ability-level
attribution if gambits need to distinguish which specific ability passed
through a given teacher/student relationship.

## Simulation Depth

- richer history-aware relationship maintenance
- occurrence entities tying long-running wars, plagues, crusades, or booms
  together
- multi-resource economy if resources can produce useful decisions rather than
  inert telemetry
- culture innovation and spread
- culture-specific medium-term goals that give each culture explicit strategic
  priorities beyond static slot occupation; these should be authored, visible,
  and eventually readable by generators/gambits, but are deferred until the
  cultural static slot system exists
- cultural cohesion/gravity behavior that makes influence expansion prefer
  connected cultural masses and frontiers instead of forming isolated pockets
- better procedural map/seed diversity

## Map And UI

- stronger map overlays for influence, activity, relationship networks, and
  generator/gambit affordances
- post-run summaries
- entity search and bookmarks
- replay and branch comparison tools
- improved art and animation

## Tooling

- faster Monte Carlo runs
- automated simulation-quality reports
- card balance sweeps
- replay from seed/config
- save-state diffing
