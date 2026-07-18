# Growing Map and Microhex Terrain Review and Repair Proposal

Date: 2026-07-18

Status: Approved and amended proposal; implementation not started

## Scope

This review covers the recently introduced growing physical and semantic maps,
their thaw/reclaim behavior, post-startup terrain selection, the terrain
collapse visible in existing Monte Carlo output, and the physical microhex
geography foundation that should be established while the terrain blueprint is
being replaced.

No new Monte Carlo run was made for this review. Important ad hoc findings were
recorded against the existing registered runs with the MC CLI.

## Findings

### The apparent radius-24 limit is not the configured hard limit

The physical and semantic frontier limits are both configured as radius 40.
There is no literal radius-24 maximum in the current implementation.

Existing runs show both that growth can pass radius 24 and that current pacing
often produces a plateau near it:

| Registered run | Relevant result |
|---|---|
| `grown-map-reach-3x25k-20260716` | Physical growth reached rings 25, 29, and 35. |
| `worldbeats-rework-3x25k-20260716b` | Physical growth stopped around rings 23, 24, and 21. |
| `frontier-balanced-2` | Semantic growth reached ring 29. |
| `atomic-fire-fix-15k-20260718` | Physical growth reached ring 23 and semantic growth reached ring 29. |

The observed plateau has several contributing causes.

First, the frontier candidate arrays are allocated from the default thaw budget
of 24 before the authored MapFrontier configuration is installed. The authored
budget is 48, but the runtime can never retain more than 24 candidates per
pulse.

Second, recurring reclaim now applies substantial opposing pressure early in a
run. In the world-beat rework evidence, the Long Dark begins reclaiming terrain
between ticks 125 and 2,000 rather than remaining a late or saturation-only
condition.

Third, the kernel's authoritative live map extent does not grow. The world
radius remains the startup radius of 18 after `CreateCell` materializes cells
beyond it. Projection notices the new cells, but pathfinding, placement, several
low-altitude systems, culture-boundary integration, and snapshot persistence
continue to consult the stale radius. Grown cells outside radius 18 are omitted
from world snapshots.

The radius-40 configuration is nevertheless too small for the intended long
horizon. A genuinely unbounded map is inconsistent with the kernel's fixed
capacity discipline, so the durable choice is an explicit generation radius
greater than 100 with validated capacity.

### Newly created terrain is deliberately uniform

Every new physical frontier cell is currently created as Tundra. Every new
semantic frontier cell is created as OpenField. The subdivision, subregion, and
continent identities are copied outward from the pressing cell rather than
being generated from a coherent larger geography.

Existing evidence is unambiguous:

- `grown-map-reach-3x25k-20260716`: all 1,225 physical frontier additions were
  Tundra.
- `semantic-growing-map-1`: all 160 semantic frontier additions were OpenField.
- `frontier-balanced-2`: all 587 semantic frontier additions were OpenField;
  final OpenField share was 51.9%.
- `atomic-fire-fix-15k-20260718`: all 45 physical additions were Tundra and all
  514 semantic additions were OpenField. Final OpenField share was 50.5%, up
  from 25.3% at startup.

The startup seed already has substantially richer physical and semantic terrain
generation. The growing-map runtime bypasses those systems and substitutes the
two configured fallback terrains.

### Deep Ice reclaim destroys prior terrain

Reclaim scans the entire configured disk rather than only territory created by
frontier growth. It can overwrite most of the original radius-18 heart outside
the radius-6 protected floor.

The system stores no pre-freeze terrain. When a Deep Ice cell later thaws, it is
assigned Tundra regardless of whether its previous terrain was Ocean, Reef,
Ice, or another terrain. Repeated freeze/thaw cycles therefore collapse terrain
variety even when frontier expansion is ignored.

The reclaim scan also restarts from the same coordinate direction on every
pulse, creating a deterministic spatial bias.

Existing evidence shows the scale of the behavior:

- `worldbeats-rework-3x25k-20260716b`: 836 conversions to Deep Ice and only 260
  DeepIce-to-Tundra restorations. Deep Ice peaked around 23% to 25% of physical
  cells.
- `atomic-fire-fix-15k-20260718`: 255 conversions to Deep Ice and only 50
  restorations. Deep Ice peaked at 22.7%.

### Hot Springs has a separate source

MapFrontier does not thaw cells into Hot Springs; it thaws them into Tundra. The
large Hot Springs coverage comes from `ArtifactCleansesScar`.

That gambit targets any Region rather than requiring the target to be a scar,
then repaints the target's entire coverage as Hot Springs. In
`atomic-fire-fix-15k-20260718`, 456 cells were converted by
`penguin_wields_artifact`. Hot Springs rose from 0.8% at startup to 40.7% at the
end of the run.

Deep Ice replacement and uniform frontier generation amplify the overall
terrain collapse, but they are not the direct source of Hot Springs.

## Causal Summary

The map becomes uniform through four interacting mechanisms:

1. Physical expansion introduces only Tundra.
2. Semantic expansion introduces only OpenField.
3. Reclaim overwrites arbitrary existing terrain with Deep Ice and restores it
   as Tundra rather than its prior type.
4. `ArtifactCleansesScar` can repaint the entire coverage of an arbitrary
   Region as Hot Springs.

The apparent radius-24 ceiling is a pacing and runtime-integration outcome, not
the configured maximum. The hard maximum is radius 40, candidate selection is
silently capped at 24, reclaim opposes growth early, and multiple kernel
consumers still believe the map ends at radius 18.

## Approved Repair Proposal

### Generation limit and capacity

Use an explicit generation radius of 128 rather than claiming the fixed-capacity
kernel is unbounded.

A complete radius-128 hex disk contains 49,537 cells per plane, or 99,074 cells
across the physical and semantic planes. Raise map-cell capacity from 16,384 to
131,072 to provide a clear fixed bound and headroom. Validate other
coordinate-indexed capacities against the same configured limit.

Owning layer: `Engine.Kernel` for capacity and overflow behavior;
Ice Remembers configuration for the authored generation limit.

### Microhex-native deterministic latent terrain field

Generate coherent physical and semantic terrain fields through radius 128 using
the same terrain principles as the startup heart. Materialize only radius 18 at
startup. When frontier growth reaches a coordinate, reveal the planned terrain
and geography rather than assigning Tundra or OpenField.

The physical field must be microhex-native. Each physical hex contains 24
canonically indexed microtriangles with stable within-hex adjacency and stable
edge mappings to the neighboring hexes. Physical generation operates over the
whole radius-128 field rather than independently rolling triangles when a hex is
revealed. This lets elevation, ridges, coasts, watersheds, and rivers remain
continuous across hex boundaries.

A microtriangle is not represented by a single overloaded terrain enum. Its
packed primitive data should include, at minimum:

- A material or landform identifier.
- Numeric elevation.
- Hydrology or flow flags sufficient to preserve connected drainage.
- Reserved feature flags only where a concrete current requirement exists.

The 49,537 physical hexes at radius 128 contain 1,188,888 microtriangles. A
straightforward four-to-eight-byte packed representation is approximately
4.5-to-9.1 MiB before indexes and summaries, comfortably inside the approved
memory envelope. The design should favor clarity and direct access rather than
bit-packing beyond what the primitive data shape naturally requires.

The hex-scale simulation continues to consume a deterministic summary derived
from the 24 microtriangles. The summary initially includes:

- The base terrain identifier used by current terrain rules.
- Mean and maximum elevation.
- Roughness or slope.
- Water coverage.
- River-crossing and coast edge masks.

The effective terrain identifier remains the compatibility boundary for current
movement, culture, placement, and authored terrain rules. It must be derived by
one definitive kernel surface rather than independently recomputed by consumers.

The semantic field remains a varied per-hex field. Microtriangles are not added
to the semantic plane without a separate design need.

Together, the physical microgeography and semantic blueprint must include:

- Physical microtriangle data and derived hex summaries.
- Semantic terrain identifiers.
- Physical subdivision identifiers.
- Semantic subregion and continent identifiers.
- Deterministic coordinate coverage through the configured generation limit.
- Feature density and noise scales appropriate for radius 128, so the larger
  field is not merely a stretched or sparse version of the current heart.

Seed/worldgen owns physical elevation, material, hydrology, derived base terrain,
and semantic terrain. Domain startup and Host compile and install the results.
The kernel receives fixed primitive coordinate data and only materializes it.
The kernel must not acquire Ice Remembers terrain strings, runtime worldgen
objects, hot-path allocations, or domain-specific generation logic.

Intended files:

- `src/IceRemembers.Seed/IceRemembersSeedBuilder.cs`
- `src/IceRemembers.Seed/IceRemembersSeedBuilder.Terrain.cs`
- `src/IceRemembers.Seed/IceRemembersSeedBuilder.SemanticTerrain.cs`
- `src/IceRemembers.Seed/IceRemembersSeedBuilder.Subdivisions.cs`
- `src/IceRemembers.Seed/IceRemembersSeedModel.cs`
- `src/Engine.Domain/Startup/DomainStartupSeed.cs`
- New `src/Engine.Domain/Startup/DomainPhysicalGeographyBlueprint.cs`
- New `src/Engine.Domain/Startup/DomainSemanticTerrainBlueprint.cs`
- `src/Engine.Host/DomainStartupSeedInstaller.cs`
- New `src/Engine.Kernel/KernelData/KernelPhysicalGeographyData.cs`
- `src/Engine.Kernel/Programs/MapFrontier/MapFrontierProgramData.cs`
- New `src/Engine.Kernel/Programs/MapFrontier/MapFrontierSemanticTerrainBlueprint.cs`
- `src/Engine.Kernel/Programs/MapFrontier/MapFrontierProgramRuntime.cs`
- `src/Engine.Kernel/Programs/MapFrontier/MapFrontierProgramRuntime.SemanticGrowth.cs`
- `src/Engine.Domain/Config/DomainConfig.cs`
- `src/Engine.Domain/Compilation/DomainCompiler.MapFrontier.cs`
- `src/Engine.Domain/Compilation/KernelPrograms/MapFrontierProgram.cs`
- `src/Engine.Host/KernelProgramInstaller.cs`
- `src/IceRemembers/IceRemembersDefinition.cs`

### Correct live extents and frontier budgeting

Maintain separate materialized physical and semantic extents. Update the
appropriate extent whenever a cell is created, and make every map consumer use
the correct plane's authoritative live extent.

Snapshot persistence must include all materialized cells, including cells grown
beyond the startup radius.

Allocate frontier candidate scratch space from an explicit kernel option rather
than from pre-installation program defaults. Validate authored thaw and semantic
budgets against that fixed capacity. Rotate the culture scan starting point so
lower culture identifiers cannot consistently monopolize the shared frontier
budget.

Owning layer: `Engine.Kernel`, with Host consumers updated to use the corrected
extent.

Intended files:

- `src/Engine.Kernel/SimulationOptions.cs`
- `src/Engine.Kernel/WorldState.MapLayers.cs`
- `src/Engine.Kernel/Simulation.MapTerrain.cs`
- `src/Engine.Kernel/Persistence/WorldStateSnapshot.cs`
- `src/Engine.Kernel/Persistence/KernelSnapshot.cs`
- `src/Engine.Kernel/Programs/MapFrontier/MapFrontierProgramState.cs`
- `src/Engine.Kernel/Programs/MapFrontier/MapFrontierProgramRuntime.cs`
- `src/Engine.Kernel/Programs/MapFrontier/MapFrontierProgramRuntime.SemanticGrowth.cs`
- `src/Engine.Kernel/Programs/Traversal/MovementProgramRuntime.Pathfinding.cs`
- `src/Engine.Kernel/Programs/Traversal/MovementProgramRuntime.RoadMarks.cs`
- `src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramRuntime.Helpers.cs`
- `src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramRuntime.SemanticCoordinates.cs`
- `src/Engine.Kernel/Programs/Generators/GeneratorProgramRuntime.Placement.cs`
- `src/Engine.Host/KernelHost.CultureBoundaries.cs`

### Layered, reversible, frontier-scoped reclaim

Track which cells were created through frontier expansion. Ordinary frontier
reclaim should operate only on those cells outside the startup heart.

Do not implement freeze/thaw as destructive terrain replacement plus a saved
previous identifier. Store durable base terrain separately from transient terrain
cover. Deep Ice is a cover applied to an eligible physical hex; thaw removes the
cover and exposes whatever durable base terrain is currently underneath it.
Permanent terrain transformations update the durable base rather than the
temporary cover, so a transformation that happens under Deep Ice becomes visible
when the cover recedes.

The existing simulation continues to read one effective terrain identifier:
transient cover when present, otherwise the derived base terrain. Replace the
full-disk scan that restarts from one edge with a persistent cursor over eligible
frontier cells.

If the Long Dark is intended to invade the original heart, that should be an
explicit reversible invasion rule with its own coverage and pacing rather than
an accidental consequence of ordinary frontier reclaim.

Owning layer: `Engine.Kernel`; Ice Remembers continues to provide Deep Ice as
the authored frozen terrain.

Intended files:

- `src/Engine.Kernel/Programs/MapFrontier/MapFrontierProgramState.cs`
- `src/Engine.Kernel/Programs/MapFrontier/MapFrontierProgramRuntime.cs`
- `src/Engine.Kernel/Programs/MapFrontier/MapFrontierProgramRuntime.Reclaim.cs`
- `src/Engine.Kernel/WorldState.MapLayers.cs`
- `src/Engine.Kernel/Simulation.MapTerrain.cs`
- `src/Engine.Kernel/SimulationOptions.cs`
- Relevant kernel snapshot persistence files listed above

### Restrict the Hot Springs gambit

Add a target subtype requirement for `scar`, together with the appropriate
scar/corruption topology requirement, to `ArtifactCleansesScar`. Its coverage
repaint can then remain because generated scars are bounded patches of roughly
four to eight Wastes cells.

Owning layer: Ice Remembers domain content.

Intended file:

- `src/IceRemembers/IceRemembersGambits.cs`

## Durable Verification Contracts

The implementation should add or update only tests that encode durable
behavioral or persistence contracts:

- An authored frontier budget greater than 24 is honored up to the explicit
  fixed candidate capacity.
- The physical blueprint has exactly 24 stably indexed microtriangles per hex,
  including correct adjacency mappings across all six shared hex edges.
- Physical microgeography produces deterministic, varied hex summaries with
  continuous elevation and hydrology across hex boundaries.
- Physical and semantic frontier cells materialize the terrain and geography
  supplied for their coordinates, including multiple terrain types.
- Ordinary reclaim cannot select startup-heart cells.
- A freeze/thaw cycle changes only transient cover and exposes the unchanged
  durable base terrain afterward.
- A permanent base transformation made while a cell is frozen remains present
  after thaw.
- Physical and semantic live extents advance independently.
- Microgeography, derived summaries, terrain cover, and grown cells survive
  snapshot round trips.
- Capacity validation covers the radius-128 two-plane and 1,188,888-triangle
  requirements.
- `ArtifactCleansesScar` cannot target an arbitrary Region. This should be
  covered only if it can be expressed as a domain graph or compiler contract,
  not as a test that merely mirrors the authored precondition.

Expected test files:

- `tests/Engine.Kernel.Tests/MapFrontierProgramRuntimeTests.cs`
- `tests/Engine.Kernel.Tests/KernelSnapshotTests.cs`
- `tests/Engine.Kernel.Tests/TerrainAwareMapTests.cs`
- `tests/IceRemembers.Seed.Tests/IceRemembersSeedBuilderTests.cs`

Final verification is `make ci`. After implementation and CI, a new registered
Monte Carlo evidence run should check terrain diversity, continued long-horizon
growth, reclaim restoration, map utilization, roaming variance, culture shape,
and Hot Springs coverage. Any important ad hoc SQL findings must be appended to
that run's registry record.

## Out of Scope

This proposal does not change:

- The radius-18 initially materialized thawed heart.
- Culture, entity, or knowledge seed placement.
- World-beat timing or Long Dark narrative recognition.
- Relationship, movement, generator, or gambit architecture beyond the narrow
  scar-target correction.
- Semantic frontier entity placement.
- Entity movement, placement, culture, coverage, and ordinary simulation remain
  hex-scale and consume the derived effective terrain.
- Microtriangle rendering, UI inspection, projection payloads, assets, and
  existing telemetry schemas.
- Runtime edits or transformations of individual microtriangles.
- River-specific movement, river crossings, mountain passes, peak occupation,
  microtriangle resources, structures, combat, or entity placement. Those are
  follow-up gameplay features built on the geography substrate.
- Microtriangles on the semantic plane.
- Runtime domain logic or compatibility fallbacks preserving the uniform
  Tundra/OpenField behavior.

World-beat cadence should not be tuned to mask the terrain-loss and extent bugs.
It can be evaluated separately after the core system is correct and new evidence
exists.
