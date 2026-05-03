# Cards

The tableau renders a deck of cards, each backed by a kernel-side
generator or gambit. Cards are a UI-side projection of those base
definitions, with eventual support for per-instance modifications
(deltas applied on top of the base — different cooldown, swapped
produce subtype, renamed, etc.). This document captures how that
plumbing should look against the actual architecture and the
sequence to get there.

## Architecture as it stands

- `Engine.Kernel` runs the simulation against primitive ids. It does
  not know names, descriptions, or structural metadata.
- `CompiledDomain` (built by `DomainCompiler`) holds the rich
  authored data:
  - `DefinitionGraph.Generators[stableId]` carries `Produces`,
    `Relationships`, `SecondaryTargets`, `Triggers`,
    `Effects`, etc.
  - `Config.Gambits[i]` (with `Mutations`, `Description`,
    `SuccessChance`, cooldowns) carries gambit structure.
- `KernelHost.CompiledDomain` exposes the above to the host layer.
- `ProjectionSymbolCatalog` is a name-resolution service:
  `(kind, id) -> ProjectionSymbol{stableName, displayName,
  description, iconKey, colorKey}`. Nothing more.
- Projections (`UiProjectionState`, `HexMapProjection`,
  `UiChronicleProjection`) accumulate runtime state from kernel
  events.
- `GameRuntimeReadModels` (in `TheCanonryGame.Game`) wraps Map / Ui
  / Chronicle / Symbols. It does **not** currently expose
  `CompiledDomain` or `DefinitionGraph`.
- `GameRuntimeTableau` builds `CardDefinition` instances from the
  symbol catalog at startup. The card's `GeneratorId` /
  `GambitId` point at the kernel symbol id; everything else
  (produces, relationships, modifiers) is unrepresented.
- `Card.cs` (`TheCanonryGame.UI`) currently has a hardcoded
  `id -> produced labels` table because the structural data does
  not reach it through any other path.

## The actual gap

The data the UI needs already exists, authored once, compiled once.
It just doesn't reach `TheCanonryGame.Game` — the project whose
job is to shape kernel data into card-level information for the UI
to render.

The fix is not to add fields to `ProjectionSymbol`, and not to
introduce a parallel "card metadata" catalog. Both pollute layers
with concerns that don't belong there. The fix is to surface the
existing structural data to the project that's responsible for
shaping cards.

## Migration steps

### Step 1 — Make structural data visible to `GameRuntimeTableau`

Goal: drop the hardcoded `ProducedLabelsByCardId` map in `Card.cs`
without inventing new catalogs.

- Surface `CompiledDomain` (or specifically `DefinitionGraph` and
  the gambit list) through `GameRuntimeReadModels`. The host
  already holds it on `KernelHost.CompiledDomain`.
- In `GameRuntimeTableau.BuildGeneratorCards`, populate
  `CardDefinition.Produces` from
  `definitionGraph.Generators[stableId].Produces`. Each entry
  carries `Kind`, `Subtype`/`Subtypes`, `CreateChance`, `Role`.
- In `BuildGambitCards`, walk `gambitDefinition.Mutations`,
  collecting `CreateContextRelationshipMutation` /
  `RemoveContextRelationshipMutation` /
  `SetTargetCoverageTerrainMutation` outcomes into
  `CardDefinition.CreatedRelationships` /
  `RemovedRelationships` / `TerrainTransforms`.
- Extend `CardDefinition` with the small structural shape it
  actually needs (a list of produced-entity descriptors, a list
  of created-relationship descriptors). Defaults stay empty so
  system cards remain zero-fuss.
- `Card.cs` reads from `card.Definition` only — no hardcoded
  table, no string parsing.

After this step the UI is structurally driven and stays correct
when generators or gambits are added, renamed, or have their
produces changed.

### Step 2 — Per-instance modifications (cosmetic / UI-only)

Goal: represent modified cards in the Game/UI without touching
kernel behavior.

- Split `CardDefinition` (immutable, derived from kernel /
  domain) from `CardInstance` (carries player-meaningful state).
- Add a `CardModifier` discriminated set on `CardInstance`:
  `RenameCard`, `RewriteDescription`, `OffsetCooldown`,
  `OffsetSuccessChance`, `SwapProducedSubtype(slot, subtype)`,
  `MarkProduceOptional(slot, chance)`, etc.
- Introduce an `EffectiveCardView` (Game-project record) that
  resolves base + modifiers each refresh. The UI binds to that
  view, not to `CardDefinition` directly.
- `Card.cs` grows a small modifier strip: chips below the name
  showing each delta. Border accent shifts to gilt or ember when
  any modifier is present, vs the default aurora/ember.

This step is fully self-contained inside `TheCanonryGame.Game`
and `TheCanonryGame.UI`. The kernel does not need to know yet,
provided modifiers don't yet change runtime-observable behavior.

### Step 3 — Kernel-side variants (so modifications change behavior)

Goal: modifications affect simulation, not just card text.

The kernel currently compiles generators and gambits into
fixed-size arrays at startup
(`GeneratorProgramData`, `BehaviorProgram`). One generator-id,
one set of produces, one cooldown. A modified card has nowhere
to live in that representation.

Two viable shapes:

**Option A — Pre-allocated variant slots.** At deck build time,
each card with modifications is registered as its own kernel
slot. "Wield an Artifact" and "Wield an Artifact (+18 cooldown)"
become two distinct gambit ids; the second is generated from
base + modifiers. New modifiers acquired mid-run require
either reserving headroom (e.g. 4 variant slots per base) or
rebuilding kernel programs in-place.

**Option B — Override tables.** Kernel keeps one slot per base
and adds parallel arrays keyed by `CardRuntimeId`:
`CooldownOverride[card_runtime_id]`,
`ProducedSubtypeOverride[card_runtime_id, slot]`, etc.
Generator and gambit evaluation consult overrides before
reading base values. Adding a modifier mid-run is a write into
the override array.

Option B fits the kernel's current discipline better:
fixed-size arrays sized by max-card-count, no runtime slot
allocation, no schema-version churn on the program data.
The cost is threading `CardRuntimeId` through every step that
needs to look up an override.

Concrete changes (Option B path):

- `GeneratorProgramData` and `BehaviorProgram` gain parallel
  override arrays keyed by `CardRuntimeId`.
- `GeneratorProgramRuntime.Fire` and
  `GambitProgramRuntime.Execute` consult overrides before
  reading base values.
- The `CardModifier` list (from step 2) compiles to override
  writes at modifier-apply time.
- A new `CardModifierAppliedKernelEvent` so projections can
  observe modifier changes deterministically.
- `UiProjectionState` adds a per-card-runtime-id projection
  tracking applied modifiers, fire stats, and effective values.
- Card modifiers participate in the kernel snapshot so save /
  load round-trips include them.

## Sequencing

1. Step 1 lands first. It is small, removes a known UI sprawl,
   and makes the tableau structurally honest. Nothing downstream
   depends on it being deferred.
2. Step 2 lands when the design has settled on a concrete set
   of modifier types and a source for them (drafting / gambit
   outcome / world event). Pick the modifier vocabulary before
   building the strip UI.
3. Step 3 lands when modifications need to change behavior, not
   before. It is the largest change, touching kernel data
   layouts, runtime reads, snapshot format, and projections.
   Doing it speculatively wastes kernel surface.

## Notes

- The previous card iteration (commit `a1c05ea`) had a hover
  blowup detail panel that broke down `WHEN`, `CREATES`, and
  `HISTORY` sections. Restoring that is mostly a step-1
  consequence — once `DefinitionGraph` is reachable from the UI,
  the detail panel is straightforward to bring back.
- Modifier sources (player drafting, gambit outcomes, world
  events, encounter rewards) are an orthogonal design decision.
  They determine the modifier vocabulary needed in step 2 but
  not the shape of the steps themselves.
