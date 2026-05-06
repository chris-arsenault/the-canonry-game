# Low-Altitude Verb Selection Redesign

Design document for the redesign of low-altitude verb selection. Targets
tracking item 3 ("Affordance Coverage And Attribution") in
`TRACKING-mc-followups.md`, with implications for items 10 (early-game
generator stability) and 11 (low-altitude verb mix).

## Context

The current low-altitude verb-selection model has grown organically and is now
hard to tune. The diagnosed pathologies:

- A six-knob weight tuple authored per verb
  (`Weight, Explore, Staleness, Hotspot, Crowding, Affordance`) is applied via
  `LocationBaseWeight` regardless of target mode. Many of the knobs are
  nonsense for the verb's mode (e.g. `Explore` on `RelationshipStrength`
  verbs).
- Two-stage selection where per-verb-target winners feed the per-entity verb
  lottery using the same numeric scale, so the *verb's* offered weight is one
  sampled target's luck.
- Random-sample candidate generation (16/24/28 cells × 3 or 4 multipliers)
  reranked by tracked map signals (heat, crowding, attractor mass) — high
  tracking cost, weak steering. The sample-of-N random pattern is the largest
  offender.
- Affordance pressure added as a clamped additive term
  (`min(score, 3) × verbAffordWeight × 3`), no global "how much should
  opportunities matter" knob, top-attribution dominated by `PresenceNearEntity`
  so other shapes never surface in MC selected rows.
- `RelationshipAffinityWeight = strength × weight` rewards already-strong
  relationships, opposite of what relationship maintenance needs (deficit-aware).
- Trait modifiers piggyback on the same six-knob surface and re-evaluate trait
  totals three times per candidate.

### Goals

- Make selection legible: a small set of named knobs with clear tuning impact.
- Keep verbs + entity-target pairs.
- Keep generator/gambit influence as definition-derived (not live-runtime)
  affordance pressure — preserve "going here has a high probability of enabling
  something" semantics, not "entity chases an objective."
- Outcome target: routine simulation 50–70% of the time, opportunity-driven
  (affordance-led) selection 30–50% of the time.
- Map signals must steer decisively or be cut.
- Traits remain the entity-level behavioral lever. Canonical examples: penguin
  Ranging Heart vs. Hearthbound (wilderness vs. settlement preference).

## Selection model

### Two-bucket selection with single-pass evaluation

For each entity that needs a new intent at this tick, run **one** pass over
its kind's eligible verbs. For each verb:

1. Run mode-owned candidate generation (described below). Score each
   candidate.
2. Reservoir-pick one target within the verb (the verb's "chosen target").
3. Cache: `(verbId, chosenTarget, chosenTargetWeight, sumAffordanceScore)`.

After the single pass, decide which bucket wins:

```
if any verb has sumAffordanceScore > 0
   and NextFloat() < OpportunityRate:
       bucket = Opportunity
else:
       bucket = Routine
```

Pick a verb from the chosen bucket:

- **Routine bucket:** weighted-random over all verbs by `chosenTargetWeight`
  (verb's authored Weight × trait deltas × mode scoring on the chosen target).
- **Opportunity bucket:** weighted-random over verbs that have
  `sumAffordanceScore > 0`, weighted by `sumAffordanceScore`.

The chosen verb already has its target cached — no second walk.

`OpportunityRate` is the single global knob for the routine/opportunity split.
Default 0.4; expected tuning range 0.3–0.5.

### Per-mode scoring (modes own factors, verbs do not)

Each `TargetMode` owns its scoring shape. Verbs author only what they uniquely
control (`Weight`, mode params, plus optional per-mode overrides). The
per-verb six-tuple goes away.

| Mode | Mode-owned scoring factors |
|---|---|
| Explore | sampling biased by staleness; score = `Weight` × recencyMultiplier × cellSaturation |
| Terrain | sampling biased by terrain match + staleness; score = `Weight` × terrainAffordance × cellSaturation × recencyMultiplier |
| EntityKind | iterate all eligible kind targets; score = `Weight` × distancePref × relationshipFitness × recencyMultiplier × cellSaturation(targetCell) |
| RelationshipTarget | iterate matching relationships; score = `Weight` × relationshipFitness × recencyMultiplier |
| OwnInfluence | sampling biased by current own-influence shortfall; score = `Weight` × influenceOpportunity(increase) × cellSaturation × recencyMultiplier |
| ForeignInfluence | sampling biased by foreign-influence presence; score = `Weight` × influenceOpportunity(reduce) × cellSaturation × recencyMultiplier |

`relationshipFitness` is signed-aware: deficit if the verb's polarity
strengthens, surplus if it weakens. Replaces
`RelationshipAffinityWeight = strength × weight` (which currently rewards
already-strong relationships, opposite of what maintenance needs).

`distancePref`: cheap nonlinear distance score so close targets win against
far targets without a hard radius gate. Default shape
`1 / (1 + dist/distanceScale)`. Authored as **per-mode default with per-verb
override**.

After mode-scoring a candidate, multiply in **affordance pressure** for that
target:

```
affordanceScore = combineAffordances(matchingRowsForTarget)   // see overlap rule
score *= 1 + affordanceScore × (OpportunityBonus + traitOpportunityBonusDelta)
```

This is what makes affordance show up in the *target* score (so the
within-verb winner is the affordance-rich target). The bucket coin is what
makes affordance show up at the *verb* level.

### Cell-target candidate generation: bias the sampling

For Explore / Terrain / OwnInfluence / ForeignInfluence modes, replace the
"draw N random cells, rerank by signals" pattern with a weighted cell sampler.
Cells get drawn proportional to the relevant biasing factors:

```
Explore:        bias = staleness * f1 - cellOccupancy * f2
Terrain:        bias = staleness * f1 - cellOccupancy * f2 + terrainMatch * f3
OwnInfluence:   bias = ownInfluenceShortfall - cellOccupancy * f2
ForeignInfluence: bias = foreignInfluencePresence - cellOccupancy * f2
```

The signals now decide which cells get scored at all, instead of barely
nudging an arbitrary random draw. Score function on the drawn cells stays
simple: `Weight × recencyMultiplier × affordanceBoost`.

`CandidateSampleCount` survives but is interpreted as "how many biased samples
to draw." Per-mode multipliers (`*3` for Terrain, `*4` for Influence) retire;
sample count is authored once per verb.

### Affordance overlap rules

**Per-target combination: diminishing-returns sum.**

Sort matching affordance weights for the target descending. Multiply by the
fixed factor sequence and sum, capped at six matches:

```
factors = [1.0, 0.7, 0.5, 0.4, 0.3, 0.25]
score = sum(s * f for s, f in zip(sorted_desc(matches)[:6], factors))
```

Replaces the hard `AffordanceScoreCap = 3`. Settlement A with 4 matches at
weight 1.0 each → 1.0 + 0.7 + 0.5 + 0.4 = 2.6. Hex Y with 1 match at 1.0 →
1.0. Clear ratio without runaway.

**Verb-level rollup: sum across candidates.**

A verb's opportunity bucket weight is the sum of per-candidate affordance
scores across all its candidates. Verbs with broad opportunity coverage get
proportionally more pull than verbs with one excellent candidate.

> Risk: Explore-style verbs that hit many cells with weak affordance matches
> can dominate the opportunity bucket. Watch in MC; if it shows up, revisit
> toward `max` or a hybrid (e.g. mean of nonzero) without changing the rest
> of the model.

## Saturation system

Three multiplicative score modifiers, all live in mode-owned scoring:

| Modifier | Field used | Purpose | Knob |
|---|---|---|---|
| `recencyMultiplier` | per-actor recent-target ring (size K) | prevent one entity re-picking same target | `RecentTargetCooldownTicks` |
| `cellSaturation` divisor | cell occupancy (1-ring sum) | prevent regional clustering across actors | `CellSaturationFactor` |
| (deferred) target-intent count divisor | active intents pointing at target | prevent multi-actor convergence on scarce targets | `TargetSaturationFactor` |

Composition:
```
score = baseScore
      × recencyMultiplier
      × affordanceBoost
      / (1 + cellOccupancy × cellSaturationFactor)
```

The third (target-intent count) is a known follow-up but not in this
redesign's first cut. Adding it later is composable.

### Per-actor recent-target ring

Each entity carries a small ring buffer (default K=3) of
`(targetId | packedCellCoord, completedTick)`. On intent end (expire,
complete, or interrupt), push the entry. At candidate scoring time:

```
recencyMultiplier = 1.0
foreach entry in actor.recentRing:
    if candidate.target matches entry.target:
        cooled = clamp01((tick - entry.completedTick) / RecentTargetCooldownTicks)
        recencyMultiplier = min(recencyMultiplier, cooled)
```

Just-left target ≈ 0× (effectively excluded). Long-cooled target = 1×. Smooth
ramp. Cell-target packing means the ring handles entity targets and cell
targets uniformly. Replaces per-cell `VisitCount` and `LastVisitedTick`.

### Cell saturation (replaces `CrowdingWeight`)

Multiplicative divisor on score, not subtractive on base weight. Reads cell
occupancy at the target's cell (1-ring sum so "this region is busy" is
captured, not just "this exact cell is busy"). Default linear divisor
`1 / (1 + cellOccupancy × CellSaturationFactor)`.

`CrowdingWeight` per-verb knob disappears entirely — clustering is a sim-level
concern, not a verb-level one.

## Trait hooks

Traits remain the entity-level behavioral lever, with a smaller surface that
tracks the new authoring shape.

### New trait modifier shape

Replace the seven-delta surface:

```csharp
RegisterTraitWeightModifier(
    kindId, traitId, verbId,
    weightDelta, exploreWeightDelta, stalenessWeightDelta,
    hotspotWeightDelta, crowdingWeightDelta, affordanceWeightDelta,
    relationshipAffinityWeightDelta)
```

with a three-delta surface:

```csharp
RegisterTraitWeightModifier(
    kindId, traitId, verbId,
    weightDelta,              // shifts authored verb Weight (routine appeal)
    opportunityBonusDelta,    // shifts how strongly opportunities pull this entity for this verb
    fitnessWeightDelta)       // shifts relationship-fitness sensitivity for this verb
```

`weightDelta` is the primary surface and covers most existing trait
expressions. `opportunityBonusDelta` lets a trait make an entity more or less
responsive to affordance pressure (e.g. a "Curious" trait that boosts
opportunity pull). `fitnessWeightDelta` lets a trait amplify or dampen
relationship-driven selection (e.g. a "Loyal" trait that amplifies
relationship maintenance).

Trait totals are summed **once per (verb, tick, actor)** during the single
pass and cached, replacing the current three-times-per-candidate trait sum.

### How "penguin prefers wilderness vs. settlements" works

The verbs are already partitioned by behavior — `PenguinExplore` and
`PenguinGoFishing` are wilderness verbs; `PenguinVisitRookery` is a settlement
verb. So expressing wilderness vs. settlement preference is purely verb-level
base-weight modulation:

- **PenguinRangingHeart (wilderness preference):**
  - `weightDelta(PenguinExplore) = +1.0`
  - `weightDelta(PenguinGoFishing) = +0.4`
  - `weightDelta(PenguinVisitRookery) = -0.5`

- **PenguinHearthbound (settlement preference):**
  - `weightDelta(PenguinExplore) = -0.7`
  - `weightDelta(PenguinVisitRookery) = +0.6`
  - `fitnessWeightDelta(PenguinVisitRookery) = +0.4` (loyal-to-home flavor)

Strictly less expressive than the current six-knob surface but expresses the
canonical examples cleanly. The thin spots (e.g. trait says "I care more
about staleness specifically") are deliberately dropped — staleness is a
mode-owned property now, and verb-level weight changes carry the same effect
with simpler tuning.

### Trait redesign happens in this same change

A few existing traits use the soon-to-be-removed deltas (`stalenessDelta`,
`hotspotDelta`, `crowdingDelta`). These get re-expressed as combinations of
the three remaining deltas — usually by amplifying the verb's overall
`Weight` and trusting mode-owned scoring to do the rest. The Ranging Heart /
Hearthbound pair already cleanly converts. Other Ice Remembers traits in
`IceRemembersDefinition.cs` get an authorial audit pass.

### Optional extension (out of scope for v1)

For finer-grained "wilderness penguin going on a generic verb prefers
wilderness-located targets" semantics, a future
`RegisterTraitTerrainPreference(kindId, traitId, terrainId, multiplier)` could
apply at target scoring for cell- and entity-target modes. Not needed for the
canonical wilderness/settlement example because that's already handled at
verb level. Added later if MC shows verb-level modulation is too coarse.

## Locked decisions

1. **Cell-target candidate generation: bias sampling** for Explore / Terrain /
   Influence modes. No more random-sample-then-rerank.
2. **`OpportunityBonus`: single global** in `KernelProgramTuningOptions`.
   Traits shift effective bonus per (entity, verb) via
   `opportunityBonusDelta`.
3. **Trait redesign in this PR.** Engine surface shrinks to three deltas; all
   Ice Remembers traits get rewritten in the same change.
4. **Tuning constants live on `KernelProgramTuningOptions`.** Single home.
5. **Heat and AttractorMass: cut entirely.** SignalSnapshot retains
   `CurrentOccupancy` (and possibly `CrowdingLoad`); `VisitCount` and
   `LastVisitedTick` retire in favor of the per-actor recent-target ring.
6. **Affordance overlap on one target: diminishing-returns sum.** Sorted
   matching weights × `[1.0, 0.7, 0.5, 0.4, 0.3, 0.25]`, capped at six
   matches.
7. **Verb-level affordance rollup: sum across candidates.** Verbs with broad
   opportunity coverage rank higher in the opportunity bucket. Watch for
   Explore over-dominance in MC.
8. **Distance scoring: per-mode default + per-verb override.** Each mode
   declares a `DistanceScale`; verbs whose natural reach differs override.

## Out-of-scope follow-ups

- Multi-actor convergence (target-intent count saturation) — defer; composes
  cleanly later.
- Per-(trait, terrain) target preference — defer; verb-level trait modulation
  handles canonical examples.
- Saturation divisor shape — start linear `1 / (1 + n × factor)`; only
  revisit if MC shows clustering pathology.

## Knob surface (before / after)

### Per-verb authored knobs

| Before | After |
|---|---|
| `Weight, ExploreWeight, StalenessWeight, HotspotWeight, CrowdingWeight, AffordanceWeight, RelationshipAffinityWeight` (7) | `Weight` + `OpportunityBonus` (optional override) + `FitnessWeight` (for fitness-using verbs) + `DistanceScale` (optional override) |
| `CandidateSampleCount, TargetRadius, MinTicks, MaxTicks, DwellTicks` | unchanged |
| Mode params (target kind, relationship kind/dir, terrain filter, influence delta/radius/etc.) | unchanged |

### Per-trait modifier deltas

| Before | After |
|---|---|
| 7 deltas (one per verb knob) | 3 deltas (`weightDelta`, `opportunityBonusDelta`, `fitnessWeightDelta`) |

### Global tuning knobs (new, on `KernelProgramTuningOptions`)

| Knob | Default | Range |
|---|---|---|
| `OpportunityRate` | 0.4 | 0.3–0.5 |
| `OpportunityBonus` | 1.0 | tune from MC |
| `RecentTargetCooldownTicks` | ~90 | 60–120 |
| `RecentTargetRingSize` (K) | 3 | 2–4 |
| `CellSaturationFactor` | tbd | tune from MC |
| Per-mode `DistanceScale` defaults | tbd | per mode |

### Knobs/state removed

- `AffordanceScoreCap = 3`, `AffordanceWeightMultiplier = 3` — replaced by
  diminishing-sum overlap rule + named `OpportunityBonus`.
- Per-verb `CrowdingWeight` — replaced by sim-level `CellSaturationFactor`.
- `LowAltitudeSignalSnapshot.Heat`, `AttractorMass` — cut.
- `LowAltitudeSignalSnapshot.VisitCount`, `LastVisitedTick` — superseded by
  per-actor recent-target ring.
- Hard-coded `staleness / 120f`, `Heat / 100f`,
  `((100 - influence) / 100) * 2`, `(influence / 100) * 3`, `samples * 3`,
  `samples * 4` — replaced by named, single-purpose constants.

## Critical files

### Engine kernel

- `src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramRuntime.cs`
  — main runtime; `ChooseIntent` flow.
- `src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramRuntime.Candidates.cs`
  — per-mode candidate generation; biased samplers.
- `src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramRuntime.Weighting.cs`
  — formula collapse, new affordance combine, saturation divisors.
- `src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramRuntime.Helpers.cs`
  — `VerbTargetCandidate` shape changes.
- `src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramData.cs`
  — registration surface shrink (verb six-tuple → simpler;
  trait seven-delta → three-delta).
- `src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramState.cs`
  — add per-actor recent-target ring storage.
- `src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeSignalSnapshot.cs`
  — drop fields cut by the redesign; keep cell-occupancy.
- `src/Engine.Kernel/DecisionModifier.cs` — collapse to three-delta
  `LowAltitudeDecisionModifierTotals`.
- `src/Engine.Kernel/KernelProgramTuningOptions.cs` (or current location of
  global tuning) — add `OpportunityRate`, `OpportunityBonus`,
  `CellSaturationFactor`, `RecentTargetCooldownTicks`,
  `RecentTargetRingSize`, per-mode `DistanceScale`.

### Domain compilation / host

- `src/Engine.Domain/Compilation/KernelPrograms/BehaviorProgram.cs` (and
  related compilation files) — update for new shape.
- `src/Engine.Host/KernelProgramInstaller*.cs` — update for new registration
  signatures.
- `src/Engine.Gameplay/TraitOperationConfig.cs` —
  `TraitLowAltitudeWeightOperationConfig` shrinks to three deltas.
- Any `LowAltitudeVerbConfig` shape file in `Engine.Gameplay` — drop the
  removed knobs.

### Ice Remembers domain

- `src/IceRemembers/IceRemembersLowAltitudeVerbs.cs` — rewrite
  `MovementVerb`, `RelationshipStrengthVerb`, `InfluenceVerb`,
  `InfluenceVerbConfig` helpers with new tuple shape; rewrite all verb
  authorings.
- `src/IceRemembers/IceRemembersDefinition.cs` (around lines 230–246 and
  similar trait blocks) — re-express existing traits in new three-delta
  surface; redesign any trait whose effect cannot be captured.

### Tests

Any test that hard-codes the six-knob tuple, the seven-delta trait surface,
or expects the cut signals (`Heat`, `AttractorMass`, `VisitCount`,
`LastVisitedTick`) needs paired updates. Architecture-encoding tests must not
be loosened; if a test encodes the obsolete shape, update it and the docs
together (per `CLAUDE.md` hard rule).

Likely paired updates in:
- `tests/Engine.Kernel.Tests` — verb selection / weighting / trait effect tests.
- `tests/Engine.Gameplay.Tests` — config shape tests.
- `tests/Engine.Domain.Tests` — compilation tests for verb/trait config.
- `tests/Engine.Host.Tests` — registration-pipeline tests.
- `tests/MonteCarlo.Tests` — analyzer/parquet column expectations if the
  affordance trace columns change shape.

## Verification

- **Unit tests** at the kernel layer:
  - selection: with a fixed RNG seed, an entity picks expected verb/target.
  - trait: entity with Hearthbound picks `VisitRookery` more than baseline;
    entity with Ranging Heart picks `PenguinExplore` more.
  - saturation: re-picking the same target during cooldown drops to ~0;
    cell occupancy reduces score correctly.
  - affordance overlap: diminishing-sum gives expected ratios; single global
    `OpportunityBonus` scales target score correctly.
- **Architecture-invariant tests** must remain green; any test that encodes
  the obsolete weight shape is updated alongside the docs.
- **MC smoke run** (10x500) compared to
  `.artifacts/monte-carlo/followups-10x500-20260505`:
  - Verb mix less influence-dominated (item 11 sanity).
  - Routine vs opportunity-attributed selection counts roughly 60/40 (target
    `OpportunityRate` = 0.4).
  - Affordance shape coverage broader: relationship-strength, influence,
    terrain shapes appear in selected rows, not only `PresenceNearEntity`.
  - Generator first-10-pulse coverage no worse than baseline (item 10
    sanity).
  - Trade-route bootstrap not regressed (item 9 sanity).
- **Long MC run** (10x2500) once short run looks clean.
- `make ci` as the final pass per `CLAUDE.md`. Tests do not run in parallel
  in this repo.

## Implementation order

A suggested order to keep compilation green between phases:

1. Add new fields to `KernelProgramTuningOptions` (with defaults).
2. Update `LowAltitudeSignalSnapshot` to drop cut fields.
3. Update `LowAltitudeVerbProgramData`: add new registration surface, leave
   old surface in place as deprecated overload to avoid call-site churn
   during the transition.
4. Update `LowAltitudeVerbProgramState` to add the recent-target ring.
5. Update `DecisionModifier` to the three-delta surface.
6. Rewrite `LowAltitudeVerbProgramRuntime.Weighting.cs`: new affordance
   combine, saturation divisors, per-mode scoring.
7. Rewrite `LowAltitudeVerbProgramRuntime.Candidates.cs`: biased samplers.
8. Update `LowAltitudeVerbProgramRuntime.cs` `ChooseIntent` for two-bucket
   selection.
9. Update `LowAltitudeVerbProgramRuntime.Helpers.cs` `VerbTargetCandidate`
   shape.
10. Update domain compilation in
    `src/Engine.Domain/Compilation/KernelPrograms/`.
11. Update `Engine.Gameplay/TraitOperationConfig.cs` and
    `LowAltitudeVerbConfig`.
12. Rewrite `IceRemembers/IceRemembersLowAltitudeVerbs.cs` helpers and verb
    authorings.
13. Re-author Ice Remembers traits in
    `IceRemembers/IceRemembersDefinition.cs`.
14. Drop the deprecated registration overload from step 3.
15. Update tests broken by the surface changes.
16. Run focused suites for diagnosis as needed; finish with `make ci`.
17. MC smoke run (10x500); long run (10x2500) once clean.
