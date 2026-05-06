# Semantic Terrain Design

This note captures the intended long-term shape for semantic terrain. It is a
design target, not a Monte Carlo runbook.

## Research Output

- Conceptual spaces are the strongest fit for the semantic plane: meaning can be
  represented as positions and regions over authored quality dimensions, not only
  as symbolic tags. Source: Peter Gardenfors, *Conceptual Spaces: The Geometry of
  Thought*, MIT Press:
  https://mitpress.mit.edu/9780262572194/conceptual-spaces/
- Cognitive maps support the idea that agents navigate internal structure, not
  just visible geography. Semantic movement should therefore be real traversal
  through learned cultural, doctrinal, mythic, and relational space. Source:
  Edward C. Tolman, "Cognitive maps in rats and men", *Psychological Review*,
  55(4), 189-208, 1948:
  https://cir.nii.ac.jp/crid/1363107370165054592
- Vector semantic spaces show why proximity is useful but insufficient. Nearby
  points imply association, but the game still needs authored terrain affordances
  so nearby does not mean mechanically identical. Source: Mikolov et al.,
  "Efficient Estimation of Word Representations in Vector Space":
  https://arxiv.org/abs/1301.3781
- Influence maps are a useful model for using local scalar fields as movement
  cost, attraction, repulsion, and tactical pressure. Semantic influence should
  use terrain as the medium that changes propagation, claim viability, and route
  formation. Source: Critch and Churchill, "Combining Influence Maps with
  Heuristic Search for Executing Sneak-Attacks in RTS Games":
  https://www.cs.mun.ca/~dchurchill/publications/pdf/cog20influence.pdf

## Core Position

Semantic terrain is the material of meaning at a semantic coordinate. It is not
physical terrain copied onto another map.

The physical plane says what a place is made of: ice, reef, shelf, ocean, scar.
The semantic plane says what a position in meaning-space is made of: lineage,
yoke, witness, silence, severance, breakaway, taboo, beacon, refuge, corruption,
and other authored affordance media.

The implementation should keep one map API. Terrain, regions, superregions,
continents, influence, claims, movement, roads, and projections should remain
plane-aware surfaces of the same systems. A semantic feature should be selected
with `PlaneCodes.Semantic`, not by adding parallel semantic-only programs.

## What Terrain Means Semantically

A semantic terrain answers these questions for a cell:

- What kind of meaning is easy to enter here?
- What kind of entity or influence is viable here?
- What kind of claim can stabilize here?
- What kind of generator operation is eligible here?
- What kinds of repeated travel become roads here?

Terrain is therefore not just a visual label. It is the local ruleset for
movement cost, can-enter, influence multiplier, claim cost, claim chance, road
build speed, and road speed.

The same numeric hooks used by physical terrain should apply to semantic
terrain. The interpretation changes:

- `movementCost`: conceptual friction.
- `canEnter`: whether a kind can inhabit or traverse that medium of meaning.
- `influenceMultiplier`: how strongly ideas, claims, memories, or doctrines
  propagate there.
- `claimCostMultiplier`: how much existing influence is needed to make the cell
  belong to an entity.
- `claimChanceMultiplier`: how likely a claim is to stabilize once influence is
  high enough.
- `roadBuildMultiplier`: how quickly repeated semantic travel becomes habit,
  doctrine, trade practice, rumor channel, or ritual route.
- `roadSpeedMultiplier`: how much an established channel reduces future
  conceptual friction.

## Axes And Travel

The semantic axes already have authored meaning. Terrain should not replace
those axis meanings. The axes define where a coordinate sits in the global
semantic geometry. Terrain defines the local medium at that coordinate.

Travel between semantic cells means an entity shifts its operating position in
meaning-space. A faction may move from lineage toward yoke as it becomes more
hierarchical. A settlement may drift from silence toward witness as its stories
become public. An artifact may move toward severance as it becomes taboo.

This should stay hex traversal for now. The important design constraint is not
Euclidean realism; it is that every semantic step has a readable affordance:
movement crosses a local medium, pays a terrain cost, may create a road, and may
change where influence can later grow.

## Semantic Roads

Roads remain cell-oriented on both planes.

On the physical plane, a road is a traveled surface. On the semantic plane, a
road is a reinforced channel of thought, memory, doctrine, rumor, trade habit,
or ritual. It appears because entities repeatedly travel through a semantic
cell, and it lowers future semantic travel cost through that cell.

This gives roads a useful meaning without needing a separate edge system:

- A light road is an emerging association or remembered route.
- A heavy road is a stabilized custom, canon, trade language, pilgrimage path,
  command habit, or trauma channel.
- Positive reinforcement is expected but must be measured. Roads should attract
  future travel only if the movement chooser sees lower travel cost and the
  resulting paths remain useful.

Roads are not claims. A faction can have a strong road through an unclaimed
semantic cell; a region can own a semantic basin that travelers cross without
owning it.

## Regions, Superregions, Continents

Semantic regions should use the same map cell state as physical regions, but
their meaning is different.

- A semantic region is a connected basin of related meaning, such as a lineage
  field, a silence basin, a yoke court, or a severance scar.
- A semantic superregion is a larger school, era, mythic current, factional
  doctrine family, or culture-wide conceptual province.
- A semantic continent is a top-level semantic landmass: broad worlds of memory,
  authority, survival, corruption, trade, witness, kinship, or rupture.

The goal is not a forced one-to-one copy from physical regions. The goal is a
one-to-one API surface. Physical regions and semantic regions should share the
same projection, storage, and query shapes, while domain rules decide when a
physical place seeds or corresponds to a semantic place.

## Physical To Semantic Analogs

The physical-to-semantic mapping should be analogical, not literal.

Examples:

- A physical reef can seed a semantic harbor of exchange, not "semantic reef".
- A physical scar can seed severance, taboo, corruption, or witness depending on
  what caused it.
- A physical hot spring can seed refuge, cleansing, witness, or contested
  revelation.
- A physical ocean route can seed trade habit, rumor channel, or orca mythic
  passage.
- A physical mountain or spire can seed a beacon, doctrine peak, taboo ridge, or
  authority seat.

This keeps the useful one-to-one mechanical mapping without flattening semantic
terrain into a reskinned physical terrain table.

## Starter Semantic Terrain Vocabulary

The current asset vocabulary already suggests a first set:

- `lineage`: low friction for kinship, memory, practice, and tradition; stronger
  positive relationship maintenance and claim stabilization.
- `yoke`: authority, command, oath, and obligation; easier for factions or
  controlled entities to claim, harder for unaffiliated movement.
- `witness`: public memory and testimony; high influence propagation, strong
  road formation, weaker secrecy.
- `silence`: concealment, buried memory, taboo, and latent threat; lower
  influence spread, higher movement cost for most entities, stronger persistence
  for monsters, artifacts, and hidden occurrences.
- `severance`: rupture, exile, betrayal, war aftermath; high friction, poor
  positive influence propagation, strong negative or hostile persistence.
- `breakaway`: escape, dissent, splintering, discovery; easier exploration and
  movement, weaker claim stability.

Additional generator-friendly terrains:

- `beacon_spire`: high salience and long-term road attraction; amplifies
  influence and relationship discovery.
- `taboo_ridge`: semantic mountain; hard to cross, blocks or damps influence,
  useful for containment.
- `cave_pocket`: semantic cave; local, concealed, high retention, low broadcast.
- `open_field`: default low-meaning baseline with neutral costs.

## Generator Operations

Generator map-cell operations should be plane-aware through the existing effect
surface. The desired operation vocabulary:

- `set terrain patch`: existing primitive. It should continue to work on either
  physical or semantic plane.
- `dig cave`: creates a bounded `cave_pocket` or `silence` patch around an
  anchor. It should make influence less visible or less transmissive while
  increasing local retention.
- `build spire`: creates a salient center cell, usually `beacon_spire` or
  `witness`, with optional surrounding terrain. It should become a road and
  influence attractor if entities use it.
- `raise mountain`: creates a `taboo_ridge`, `severance`, or other high-friction
  barrier patch. It should alter movement and influence paths, not just color.
- `open pass`: lowers friction through a high-cost terrain pocket and can turn
  repeated difficult traversal into an explicit road.
- `seed basin`: creates a named semantic region with coherent local terrain,
  owner, superregion, and continent metadata.

Generator coverage and terrain filters must become plane-aware everywhere they
are not already. Produced coverage should not remain implicitly physical if the
produced entity, target, or effect is semantic.

## Current Repo Reality

What exists now:

- `WorldState` can store terrain, coverage owner, superregion, and continent by
  plane.
- `Simulation.MapTerrain` exposes plane-aware travel, influence, claim cost, and
  claim chance hooks.
- The movement program handles physical and semantic movement and uses
  plane-aware terrain and roads for both.
- Low-altitude influence and claim operations use the rule plane for terrain
  multipliers.
- Generator `SetTerrainPatch` effects can target the semantic plane.
- `HexMapProjection` projects cells, influence, roads, owner, superregion, and
  continent by plane.
- Kernel tests cover semantic cell state isolation, semantic terrain snapshot
  round trips, semantic movement cost, semantic roads, and semantic terrain patch
  effects.

What is not complete:

- There is no authored semantic terrain taxonomy in the domain yet. Semantic
  cells default to terrain id `1` unless explicitly transformed.
- Semantic terrain generation and semantic regionization are not designed as
  first-class domain content yet.
- Produced coverage placement still has physical-only paths that must be made
  plane-aware before semantic regions are complete.
- Some relocation and legacy convenience accessors still assume physical
  coordinates.
- The UI can display plane-aware projection data, but semantic terrain art is
  still broad pane/overlay vocabulary rather than a complete terrain tile set.

## Design Rules

- Keep roads cell-oriented on both planes.
- Extend existing plane-aware APIs instead of introducing semantic-only
  parallel surfaces.
- Treat direct physical-to-semantic copying as a seeding rule, not as the model.
- Make semantic terrain mechanically meaningful before adding more visual
  categories.
- Require Monte Carlo analysis to distinguish road creation, road use, road
  reinforcement, and actual road prioritization.
- Keep semantic regions readable as meaning basins, not anonymous partitions.
