# Runtime Physical Geography Contract — Design Handoff

Date: 2026-07-18

Status: open design review; no runtime-contract implementation is approved.

## Purpose

This document gives a fresh reviewer the complete current state of the physical
geography gameplay-mirroring task. It records what is already implemented, what
the simulation actually consumes, the constraints established by the user, and
two proposed contract shapes that should **not** be treated as accepted designs.

The immediate need is not another quick taxonomy proposal. The next reviewer
should derive the runtime contract from an explicit consumer-by-consumer model,
then present that proposal for approval before changing simulation code.

## Current task

The causal physical world generator and its inspection UI are implemented. The
next intended map work comprises three related pieces:

1. Define the runtime physical-geography contract between the generated
   external blueprint and the hex-scale simulation.
2. Make traversal use relevant geography, including coherent treatment by
   movement, hierarchical pathfinding, travel reach, roads, and travel
   structures.
3. Make relevant geography affect placement, local capacity, culture, roads,
   and map signals.

The simulation is expected to remain hex-scale. The user has explicitly ruled
out ever putting microtriangles in the simulation kernel. The contract must not
be designed as a compressed copy of the external worldgen model.

Terrain-changing gambits and generators are also open for removal. This is not
yet an accepted decision, but partial terrain mutation must be assessed against
whatever immutable runtime geography accompanies the terrain ID.

## Work that is already complete

The following is implemented and pushed on `progression-lattice`:

- `ee441dd` — causal physical microgeography generation;
- `0ddced9` — causal microgeography inspection views;
- `d945760` — calibration and measured worldgen optimization;
- `403d200` — stable documentation for the implemented generator and boundary.

The complete external blueprint is generated through radius 128. It contains
24 canonical microtriangles per hex plus one extracted hex summary. Generation
includes uplift, ancestral erosion, land/ocean components, bathymetry,
hydrology, climate, multiple warmth systems, ice and glacial carving,
fractures, fire, aurora, pressure, and magic interactions.

The external per-hex summary already contains:

- a synthesized gameplay terrain stable name;
- primary natural/final surface and dominant material;
- significant magic-feature flags;
- elevation, relief, roughness, temperature, moisture, and geothermal values;
- land, water, ocean, lake, river, and permanent-ice coverage;
- peak count and ice statistics;
- fire, aurora, and pressure means and maxima;
- reciprocal river, coast, ridge, cliff, pass, and fracture edge masks;
- superregion and continent IDs.

These facts remain in `Engine.Domain.Startup`, the startup cache, projection,
and UI. They are inspectable without materializing latent cells.

The kernel currently receives only three latent values per physical hex:

- base terrain ID;
- superregion ID;
- continent ID.

This is implemented by
`src/Engine.Kernel/KernelData/KernelPhysicalGeographyData.cs`. Frontier reveal
reads those values and creates the hex. The kernel does not store materials,
surfaces, generation scalars, magic fields, or triangle data.

Physical terrain also already has two runtime layers:

- durable base terrain;
- transient cover, currently Deep Ice.

Deep Ice hides rather than destroys the base terrain. Thaw removes cover and
reveals the current durable base. Frontier-created cells and the reclaim cursor
are persisted. The initially materialized heart remains radius 18.

Frontier radius/pacing and entity-density work are owned separately and are not
part of this contract design.

## Performance context

The application memory target is approximately 4 GB and 8 GB is acceptable.
Optimizing for less than 1 GB is not a design requirement.

The stronger concern here is simulation hot-path cost and cache behavior. The
application already had noticeable first-tick/UI lag, and profiling work was
required to correct it. The contract must therefore be fixed-size,
allocation-free in steady state, string-free in the kernel, and cheap to read.

Radius 128 contains 49,537 physical hexes. A few primitive bytes per latent hex
are inexpensive in memory. That fact does **not** justify putting every
available worldgen fact into the kernel: each additional field also creates
semantic coupling, compiler/install/snapshot work, and possible hot-path
branches.

No runtime profiler evidence exists yet for any proposed geography-aware
contract. Performance claims about a new representation must be validated
after a grounded design exists.

## Current physical terrain catalog

The current physical gameplay terrain IDs are:

- `wastes`
- `crags`
- `tundra`
- `ice`
- `hot_springs`
- `shelf`
- `reef`
- `ocean`

Additional non-surface terrain IDs are:

- `deep_ice`, used as transient physical cover;
- `spire_tier` and `tunnel_gallery`, used for constructed vertical cells.

Semantic terrain has its own existing catalog and is not part of this physical
contract design.

Each terrain definition currently provides:

- a boundary group;
- default and movement-profile travel costs;
- culture-influence multiplier;
- claim-cost and claim-chance multipliers;
- road-build and road-speed multipliers;
- optional kind-specific culture/claim overrides.

These values compile into primitive `KernelTerrainData` tables.

## Verified current consumers

The contract should be derived from these real consumers rather than from the
worldgen palette.

### Traversal

`MovementProgramRuntime` currently computes surface entry and cost from:

- effective terrain ID;
- movement profile;
- travel structure, if any;
- road quality.

Hierarchical pathfinding calls the same terrain-surface cost helper when
evaluating portals. `TravelReachCost` line-samples cells and calls movement
travel cost, but it currently has no directional physical-edge input.

Potentially relevant external facts are river, coast, ridge, cliff, and pass
edge masks. Fracture edges have no established traversal mechanic.

### Generator placement

Generator-produced placement currently authors ordered preferred terrain IDs,
avoided terrain IDs, and a fallback terrain ID for each plane. Placement also
uses locality, radius, occupancy, culture, and other existing constraints.

Current Ice Remembers placement profiles contain concrete cases where terrain
alone is semantically weak:

- point-of-interest subtypes `peak`, `vent`, `shoal`, and `bloom`;
- settlement subtypes `harbor`, `reefhold`, `holdfast`, and `sanctuary`;
- region subtypes including `shelf`, `sea`, `depths`, and `scar`;
- maritime or land preferences for orcas, transports, factions, and other
  kinds.

This does not automatically justify a general 64-feature placement language.
The reviewer should identify exactly which of these cases need something other
than terrain, coast/river access, and existing locality.

### Startup placement

Startup seed placement runs outside the kernel and can already inspect the rich
external geography. It should not be used as a reason to add fields to the
kernel. Reuse between startup and runtime placement is desirable only if the
shared vocabulary remains appropriate at both boundaries.

### Culture and claims

Culture growth and coverage claims currently use terrain-defined influence,
claim-cost, and claim-chance multipliers. There is no existing general feature
modifier system. A new geography fact should affect culture only if a concrete
propagation or claim rule is intended.

### Roads

Road reinforcement currently uses the terrain's road-build multiplier. Road
quality modifies movement cost and participates in route-cache invalidation.
Travel structures are a separate overlay and can make otherwise impassable
terrain enterable for selected movement profiles.

Directional crossings are a credible missing road/travel concern. A general
magic-feature road system is not currently motivated.

### Map signals and capacity

Map Signals currently assign a base local capacity by terrain ID. Entities and
relationships add capacity, support, threat, and attraction signals. There is
no static geography-feature signal rule today.

If warm water, a hot spring on a peak, or another minority feature should make
a hex support life despite its primary terrain, that need must be represented
deliberately. It should not imply that every worldgen anomaly needs a runtime
capacity or signal rule.

### Low-altitude terrain filters

Some low-altitude rules use terrain sets, including maritime terrain and Hot
Springs checks. These are additional consumers of any physical terrain-catalog
change and must be included in the design matrix.

## Verified terrain-mutation paths

Only two Ice Remembers authored uses of broad terrain mutation were found:

1. Generated scar-region coverage sets `TransformTerrain = wastes`.
2. `ArtifactCleansesScar` applies
   `SetTargetCoverageTerrainMutation(hot_springs)`.

The generic engine/domain infrastructure also supports:

- coverage `TransformTerrain`;
- generator `SetTerrainPatchEffect`;
- gambit `SetTargetCoverageTerrainMutation`;
- kernel patch/coverage terrain execution.

No Ice Remembers `SetTerrainPatchEffect` use was found. The internal cell
terrain primitive is also used by ordinary map creation and constructed
vertical cells and should not be conflated with the broad authored repaint
operations.

The coherence problem is real: if terrain ID becomes mutable while other
runtime geography remains immutable, a repaint can create combinations never
produced by worldgen. For example, a coverage-wide Hot Springs repaint could
cross ocean, river, peak, and ice facts without updating them. Possible
resolutions include:

- remove the authored geography repaint operations;
- constrain them to a deliberately defined mutable overlay rather than base
  geography;
- define a complete coherent mutation model for every runtime geography field.

The third option is materially larger and should not be assumed.

## User constraints and corrections

The following must govern the next proposal:

- The simulation must never know about microtriangles.
- Worldgen and magic detail can remain outside the kernel.
- The kernel cares about hex-scale gameplay facts, not how they were generated.
- A richer hex taxonomy may be appropriate, including primary plus significant
  feature data, but only if it is justified by actual gameplay consumers.
- Performance is a serious concern; a field being memory-affordable does not
  make hot-path complexity acceptable.
- The runtime contract must not mix Ice Remembers concepts into generic engine
  code.
- The design should not imply authoring dozens of feature preferences for a
  small generator catalog.
- Removing terrain-changing gambits and generators is available for
  consideration.
- Avoid both extremes: retaining every external distinction and collapsing
  everything back to one terrain ID are equally ungrounded without consumer
  analysis.

## First unaccepted proposal: mirror most geography

The first proposal suggested:

- ten primary physical terrain types;
- 42 non-exclusive natural and magic feature flags;
- six directional edge types;
- nine packed magnitude bands;
- general feature effects on traversal, placement, capacity, culture, roads,
  and map signals.

It also proposed removing `hot_springs` and `reef` as primary terrain IDs and
turning them into features.

Why this proposal is unreliable:

- It started from the external taxonomy instead of demonstrated runtime needs.
- It mirrored magic and inspection concepts into the kernel without authored
  mechanics that consume them.
- It blurred Ice Remembers domain concepts with reusable engine contracts.
- It implied a large feature-rule and placement-authoring surface unsupported
  by the size of the current generator and behavior catalogs.
- It made every consumer appear to need combinable feature modifiers without
  examining how those modifiers would compose or how often they would execute.
- It used the available 64-bit mask as a reason to fill the mask.

This proposal is not approved.

## Second unaccepted proposal: collapse to minimal edges and five traits

After criticism, the second proposal swung to:

- the existing physical terrain catalog plus only `lake` and `sea_ice`;
- one packed word containing river, coast, barrier, and pass edges;
- five opaque site traits: peak, hot spring, warm refuge, geothermal vent, and
  reef;
- terrain-only culture/claims and mostly terrain-only capacity;
- magic entirely external until a later mechanic exists.

Why this proposal is also unreliable:

- It was produced as a reaction to the criticism rather than from a completed
  consumer matrix.
- It combined ridge and cliff into one barrier before establishing whether
  movement profiles, roads, passes, or future authored actions need the
  distinction.
- It retained overloaded terrain identities without carefully defining what a
  primary terrain ID means: dominant physical substrate, dominant surface, or
  dominant gameplay affordance.
- It selected five site traits without demonstrating that those are the exact
  irreducible placement/capacity needs.
- It did not resolve how a minority feature becomes significant or how runtime
  rules distinguish a dominant Hot Springs hex from a rocky peak containing a
  small hot spring.
- It assumed most culture/capacity behavior could remain terrain-only without
  testing that assumption against the intended icebound-but-inhabited setting.

This proposal is not approved either.

## The core design questions still open

### 1. What does primary terrain mean?

The design must choose one stable interpretation:

- dominant physical medium/substrate;
- dominant visible surface;
- dominant gameplay affordance;
- or a deliberately authored synthesis of several hex facts.

The existing synthesizer behaves like the fourth option, but the contract does
not state that clearly. Decisions about Hot Springs, Reef, Ice, Lake, Sea Ice,
and high peaks depend on this definition.

### 2. Which distinctions change current or committed gameplay?

For every candidate fact, identify an actual consumer and effect. Candidate
facts include:

- land versus inland water versus shallow/deep ocean versus frozen water;
- ordinary ground versus high/rugged ground;
- river, coast, ridge, cliff, and pass crossings;
- warmth or habitability that is significant despite the primary terrain;
- peak, vent, reef, spring, or other placement sites;
- worldgen magic anomalies.

A fact with no current or approved consumer should normally remain external.
Near-term geography-specific gameplay may justify retaining a fact now, but
that future mechanic must be named rather than implied.

### 3. What is the smallest non-combinatorial placement vocabulary?

The reviewer should enumerate all current physical placement profiles and show
which ones are adequately expressed by:

- primary terrain alone;
- terrain plus directional coast/river access;
- terrain plus one typed site distinction;
- locality or relationship context rather than geography.

Only then should a feature/tag representation be selected. The existence of a
bit-mask capacity must not imply that every generator authors against it.

### 4. Which edge distinctions are mechanically different?

Traversal needs a proposed step-cost formula for each physical movement
profile. That matrix should determine whether river, coast, ridge, cliff, pass,
and perhaps ice/fracture are separate runtime facts.

The same edge-aware primitive should be used by local movement, hierarchical
pathfinding, and travel reach. Roads and travel structures must have explicit
ordering relative to blockers and surcharges. Route-cache invalidation must be
defined.

### 5. Which facts affect capacity, culture, roads, and signals?

Do not add a generic modifier system first. For each system, state the desired
setting behavior and identify the minimum input needed. It may be acceptable
for terrain identity to handle most of these systems while a small number of
orthogonal facts serve only one consumer.

### 6. Are generated physical facts immutable at runtime?

If yes, broad authored terrain repainting should probably be removed or turned
into a separate narrative/map overlay. If no, the proposal must explain how
terrain, crossing facts, placement facts, rendering, projection, persistence,
and cache invalidation remain coherent after mutation.

### 7. Where is the domain boundary?

Acceptable engine-level concepts may include generic primitive terrain IDs,
directional crossing masks, compact numeric classes, and opaque compiled rule
IDs. Ice Remembers names such as Hot Springs, Aurora Field, Pressure God Locus,
and Thermal War Rim belong in Ice Remembers content or the external blueprint.

If opaque feature IDs are proposed, the reviewer must show why a general
feature registry is simpler than a few typed generic hex fields for the actual
consumers.

## Required fresh-review method

Before proposing fields, build a matrix with rows for candidate runtime facts
and columns for these consumers:

- base entry/travel cost;
- directional crossing cost;
- road construction and road benefit;
- travel structures;
- generator-produced placement;
- startup placement;
- low-altitude terrain filtering;
- culture influence;
- coverage claims;
- map capacity;
- support/threat/attraction signals;
- current authored gambit/generator effects;
- specifically named near-term geography gameplay.

For every non-empty cell, state the exact behavior, whether it is current or
new, and whether terrain identity already expresses it. Candidate facts with no
meaningful cells should stay outside the kernel.

Then compare at least these contract families:

1. A richer primary terrain catalog with only directional crossings.
2. Primary terrain plus a small set of typed, engine-generic hex classes.
3. Primary terrain plus opaque domain-authored feature bits.

The comparison should cover authoring burden, runtime lookup cost, combination
semantics, snapshot/cache consequences, and how each handles a high peak with a
verdant hot spring without erasing either fact.

## Proposal requirements before implementation

The next proposal must include:

- owning layer for each field and rule;
- observed failure being solved;
- causal hypothesis;
- exact runtime consumers;
- exact intended files;
- performance and cache behavior;
- terrain-mutation decision;
- explicit exclusions;
- migration of current terrain filters and placement profiles;
- tests limited to durable contracts;
- profiling and registered Monte Carlo evidence appropriate to the changed
  simulation behavior.

No implementation should begin until that proposal is reviewed and approved.

## Relevant files

External generation and extraction:

- `src/IceRemembers/IceRemembersPhysicalGeography.cs`
- `src/IceRemembers.Seed/IceRemembersMagicWorldGenerator.cs`
- `src/IceRemembers.Seed/IceRemembersPhysicalTerrainSynthesizer.cs`
- `src/IceRemembers.Seed/IceRemembersSeedBuilder.Microgeography.cs`
- `src/Engine.Domain/Startup/DomainPhysicalGeographyBlueprint.cs`

Current runtime contract and installation:

- `src/Engine.Kernel/KernelData/KernelPhysicalGeographyData.cs`
- `src/Engine.Kernel/KernelData/KernelDataSnapshot.cs`
- `src/Engine.Host/KernelConfigurationContext.cs`
- `src/Engine.Host/DomainStartupSeedInstaller.cs`
- `src/Engine.Kernel/Programs/MapFrontier/MapFrontierProgramRuntime.cs`

Terrain gameplay data:

- `src/IceRemembers/IceRemembersTerrain.cs`
- `src/Engine.Domain/Config/DomainConfig.cs`
- `src/Engine.Domain/Compilation/KernelData/CompiledTerrainData.cs`
- `src/Engine.Domain/Compilation/DomainCompiler.Mapping.cs`
- `src/Engine.Kernel/KernelData/KernelTerrainData.cs`
- `src/Engine.Kernel/Simulation.MapTerrain.cs`

Traversal:

- `src/Engine.Kernel/Programs/Traversal/MovementProgramRuntime.cs`
- `src/Engine.Kernel/Programs/Traversal/MovementProgramRuntime.HierarchicalPathfinding.cs`

Placement and authored profiles:

- `src/Engine.Domain/Config/SemanticGeneratorConfig.cs`
- `src/Engine.Domain/Compilation/KernelPrograms/GeneratorProgram.cs`
- `src/Engine.Kernel/Programs/Generators/GeneratorProgramData.cs`
- `src/Engine.Kernel/Programs/Generators/GeneratorProgramRuntime.PlacementTerrain.cs`
- `src/IceRemembers/Entities/IceRemembersEntityDefinition.cs`
- the twelve kind definition files under `src/IceRemembers/Entities/`

Culture, roads, low-altitude filters, and signals:

- `src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramRuntime.cs`
- `src/Engine.Gameplay/MapSignalsConfig.cs`
- `src/Engine.Gameplay/GameplayCompiler.cs`
- `src/Engine.Kernel/Programs/MapSignals/MapSignalsProgramData.cs`
- `src/Engine.Kernel/Programs/MapSignals/MapSignalsProgramRuntime.cs`
- `src/IceRemembers/IceRemembersLowAltitudeVerbs.cs`
- `src/IceRemembers/IceRemembersDefinition.cs`

Terrain mutation:

- `src/IceRemembers/IceRemembersGeneratorDefinition.cs`
- `src/IceRemembers/IceRemembersGambits.cs`
- `src/Engine.Domain/Config/SemanticGeneratorConfig.cs`
- `src/Engine.Domain/Behaviors/Mutations.cs`
- `src/Engine.Domain/Compilation/DomainCompiler.Mapping.cs`
- `src/Engine.Domain/Compilation/DomainCompiler.cs`
- `src/Engine.Kernel/Programs/Generators/GeneratorProgramRuntime.MapEffects.cs`
- `src/Engine.Kernel/Programs/Gambits/GambitProgramRuntime.Execution.cs`

Stable design references:

- `docs/design/physical-microgeography-worldgen.md`
- `docs/systems/04-map-spatial.md`
- `docs/systems/26-growing-map.md`
- `docs/kernel/simulation-kernel-discipline.md`

