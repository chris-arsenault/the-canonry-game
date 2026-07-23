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
  and eventually readable by generators/gambits
- deeper cultural cohesion and front behavior on top of the current boundary,
  slot, low-altitude culture influence, and minor actor systems
- better procedural map/seed diversity

## World Generation And Geography

- Add vegetation and cover classification—forest, marsh, ecological scrub,
  climate-derived aridity—as a later domain layer that does not replace or hide
  landform, substrate, rivers, or magic features.
- Seed pre-Collapse archaeology—roads, settlements, megaliths, and weapon
  sites—into the mature temperate landscape before the Deep Freeze overprint.
  This stage must not compensate for defective natural geography.
- Expand stable named geological feature IDs beyond the current rivers and
  lakes to ranges, peaks, plains, basins, notable waterfalls, and craters. Add a
  kernel or gameplay accessor only when an actual gameplay consumer requires
  one; generation-time identity alone stays outside the kernel.
- Consider a higher-fidelity plate or terrane model only when a concrete future
  gameplay or visual need warrants replacing part of the current kinematic
  model.
- Tune physical-frontier growth pacing as runtime balance work. This is
  separate from the fixed latent map capacity and physical generation model.

## Map And UI

- stronger map overlays for culture influence, activity, relationship networks,
  and generator/gambit affordances
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
