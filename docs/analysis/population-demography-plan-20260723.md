# Population Demography Plan: Ordered Emergence on a Growing World

Status: merged working plan, 2026-07-23. Investigation-backed; not approved
for implementation. Supersedes
`entity-density-and-retirement-proposal-20260718.md` and
`ordered-emergence-population-plan-20260718.md` by merging them and updating
both against the exploration/terrain arc
(`SUMMARY-exploration-terrain-arc-20260723.md`). Phase 1 evidence baselines
are being produced under this plan; everything after Phase 1 requires
approval of an exact implementation slice.

## Design Goal

Impose order on a chaotic simulation through authored incentives, competing
pressures, and feedback loops — not placement rules or population caps. The
world stays emergent, and the target shape is **hub-and-frontier**: mature
cities, faction holdings, routes, contested borders, and comparatively
sparse ground between them. Clusters exist for identifiable reasons and
generate outward consequences. Uniform distribution would be as wrong as
pile-ups; a six-stack is right for a major city and wrong for an empty ice
hex.

The primary user-facing failure is clutter and illegibility, on **both**
planes. This plan owns simulation structure and balance; UI rendering
performance is a separate effort.

## Observed Failures (settled evidence)

Three coupled failures, established across the 20260718 investigations.
Kind-level numbers cite pre-arc runs and need refreshing (see Evidence
Currency), but the structural findings are corroborated by code inspection
and are treated as settled:

1. **Demographic decoupling.** No term in the population dynamics reads
   territory. Creation is a fixed global pipe — pulse budget
   `DefaultFiresPerPulse = 1` per 25 ticks plus fixed cadences, unmodified
   by any config — and it binds hard (16× eligible-vs-fired surplus
   observed). Mortal kinds (fixed inflow, population-proportional outflow)
   pin at territory-independent equilibria and *thin* as the map grows;
   named-penguin density per covered cell halved over the pre-arc 100k run.
   Anchor kinds never leave: zero genuine retirements of any artifact,
   settlement, region, or POI in 100k ticks (settlement Monumental drain is
   fully cancelled by support effects; gambits retired 22 entities total).
   The mix inverts — the live share shrinks while static anchors climb
   toward half the cast.
2. **Activity without progress.** The cast that moves mostly oscillates:
   ~50% immediate reversal and 2.8% net-displacement efficiency for
   artifacts, 67%/0% for transports (pre-arc measurement). `Explore` did
   not value unknown frontier or contested fronts, and dwell was uniform
   random roam. The exploration arc rebuilt much of this surface; the rates
   must be remeasured before any movement tuning.
3. **Inert piles with a feedback loop.** Physical plane: worst hexes are
   artifact hoards (war memorials placed at casualty hexes; claims like
   `ClaimedBy` never resolve into carrying; each artifact emits map signal
   with `attractorRadius 2`, so piles attract the activity that feeds
   them). Semantic plane: no stacking but monotone spray — population
   growth outruns idea-plane growth, dotting an ever-larger fraction of it.

## Population Model (target)

Dynamic density equilibrium, not fixed-count equilibrium. For each kind
`k`, with a usable-territory measure `U_k(t)` from the plane(s) that kind
occupies (known cells, covered cells, culture-supported cells, components,
holdings, settled/frontier structure — raw cell count alone is not
sufficient):

```
dN_k/dt ≈ rho_k · dU_k/dt
```

with continuing creation *and* retirement on both sides of the net, and
`rho_k` authored per kind or subtype. Consequences:

- mortal kinds churn and grow modestly while usable territory grows; their
  old absolute equilibria are not targets;
- durable kinds have long relevance lifetimes but real paths to historical
  status; active stock plateaus only when the usable map plateaus;
- bootstrap may fill faster than the long-run rate;
- the historical graph keeps growing (archival policy is out of scope).

**Constraint the model must respect:** authored opportunity-scaling cannot
raise throughput through a binding global fire budget. If Phase 1 confirms
the budget still binds at post-arc territory (it bound 16× pre-arc, and no
config raises it), K1 requires a kernel-tuning change to scale the budget
with a territory measure — that is tuning, not semantics, but it crosses
the kernel boundary and needs explicit approval. The `FeatureAnchor`
pattern from the terrain arc (finite geological features, occupancy is the
claim, exhausted anchors leave candidacy) is the model's existence proof:
creation coupled to map structure with natural saturation, no caps.

No numeric density bands are ratified. They must come from Phase 1
multi-seed evidence on the current runtime plus legibility judgment.

## What the Exploration/Terrain Arc Already Delivered

Update these out from under both parent docs — the spread half of the
problem has largely shipped:

- physical frontier placement exists and is authored (founding ratchet:
  colonies, reefholds, harbors, vents, orca bulls place beyond the rim);
- the culture-support wall became a leash (`BeyondCultureSupport` costs);
- `FeatureAnchor` discovery placement ties creation to finite map features
  with natural saturation;
- the map genuinely grows (bootstrap +1,600..+2,500 cells; 3× start extent
  by 50k), which makes the fixed creation pipe and pinned mortal equilibria
  *more* wrong, not less — the thinning accelerates;
- exploration verbs, expedition gambits, waymarks, and ages of wandering
  address part of purposeful movement for explorers (unmeasured post-arc).

## Knobs

Four coupled knobs. Dispositions per the design-synthesis rule: each names
what is kept, tuned, extended, or added. No single knob imposes the order
alone.

### K1. Territory-coupled creation and churn

*Disposition: extend (authored gates) + likely one kernel-tuning change
(budget scaling), gated on Phase 1 confirmation of the budget bind.*

- Make generator opportunity depend on settlement, holding, culture-support,
  route, frontier, and component structure rather than only a global pulse;
  generalize the `FeatureAnchor` saturation pattern where fiction supports
  it.
- Let map expansion create opportunity without a birth quota; let stagnant
  or overcrowded components reduce creation fitness.
- Scale the global fire budget with a territory measure if and only if the
  bind is confirmed post-arc. Preserve causal limits and authored fiction.
- Tune per kind: mortal kinds grow modestly with territory; durable kinds
  grow slowly and turn over via K4.

Owning layer: `src/IceRemembers/IceRemembersGeneratorDefinition*.cs`,
`src/IceRemembers/Entities/*Entity.cs`. Kernel surfaces only for the budget
change: `KernelProgramTuningOptions` /
`src/Engine.Kernel/Programs/Generators/` (read
`docs/kernel/simulation-kernel-discipline.md` first).

### K2. Purposeful movement and commitment

*Disposition: remeasure first; then tune authored verbs; extend engine
vocabulary only on demonstrated gaps.*

Actors need spatial jobs held long enough to alter the world: explorers
continue into a frontier sector until they discover/cover/connect
something; faction agents reinforce contested boundaries or extend
holdings; transports connect holdings/ports/routes; artifact movement
follows custody arcs (circulation, pilgrimage, theft, return,
installation) — this is where "claims resolve into carrying" lives;
semantic movers reinforce or contest idea-fronts.

The exploration arc rebuilt the explorer path (seeker verbs, waymarks,
expeditions). Phase 1 must re-derive reversal/efficiency/frontier-entry
rates and attribute movement with a trace-enabled run before touching
weights. First pass is authored:
`src/IceRemembers/IceRemembersLowAltitudeVerbs.cs`. Engine vocabulary
(frontier-sector / contested-front targeting) only if attribution proves
it inexpressible: `src/Engine.Gameplay/LowAltitudeVerbConfig.cs`,
`LowAltitudeVerbShapes.cs`,
`src/Engine.Kernel/Programs/LowAltitudeVerbs/*`. The selector stays
probabilistic — weighted opportunity and persistent intent, never forced
assignment.

### K3. Productive clustering and carrying capacity

*Disposition: tune and connect existing signals; no new universal damage
number.*

Feedback shape: structure attracts relevant actors → co-location pays
(coordination, trade, culture, gambit opportunity) → benefits diminish as
capacity fills → excess actors get increasing incentives to depart, found,
extend a route/front, retire into a host, or lose relevance → mature
clusters leave durable structure, not an ever-growing foreground cast.

Inputs already exist: map crowding/capacity/support/threat/attraction,
culture slots, saturation. Two specifics from the investigation must be
addressed here: artifact self-attraction (piles emitting
support/attraction that feeds the pile) and hub differentiation (strong
hubs legitimately support more than weak sites — capacity is per-site, not
global). Owning layer: `src/IceRemembers/IceRemembersDefinition.cs`,
`Entities/*Entity.cs`, `IceRemembersLowAltitudeVerbs.cs`,
`IceRemembersGambits*.cs`. Diagnostic engine surfaces:
`src/Engine.Kernel/Programs/MapSignals/`, effects/weighting runtimes.

### K4. Relevance lifecycle and authored retirement

*Disposition: extend bands + author endings + add one new mechanic (legacy
retirement gambits).*

Lifecycle means leaving the foreground zeitgeist, not ceasing to exist.
Mix deliberately per subtype and circumstance — long-lived anchors,
ordinary relevance lifetimes, and authored endings coexist within a kind:

- passive relevance decay where fiction supports fading (artifact/POI band
  reclassification out of `WorldAnchor`, case by case; **regions stay
  `WorldAnchor`**);
- fix settlement accounting so Monumental drain is not fully cancelled —
  thriving settlements persist, immortality stops being the default;
- carrying-pressure retirement for overcrowded/unsupported entities (K3
  coupling);
- gambit outcomes that resolve wars, monsters, factions, settlements,
  artifacts; generator consequences that transform completed patterns into
  history (e.g. battlefield relics consolidating into one memorial canon);
- **legacy retirement gambits**: an artifact with mature custody installs
  itself into its host (museum, shrine, archive, ward, standard) —
  atomically end active custody, materialize a durable host benefit,
  archive the relationship, go historical, leave UI/targeting/signals, and
  preserve identity + provenance for an optional later reactivation.
  Evaluate the *fungible legacy* representation (durable host-owned state)
  first; add the *typed installed-legacy* component (fixed-capacity
  attachment: source id, host/coordinate, legacy kind, magnitude, tick,
  state) only if distinct mechanical identities are needed. Narrow causal
  gates, not a universal random sink.

Owning layer: `src/IceRemembers/Entities/*.cs`,
`IceRemembersGambits*.cs` (new `IceRemembersGambits.Retirement.cs`),
`IceRemembersGeneratorDefinition*.cs`, `IceRemembersSchema.cs`. Legacy
materialization engine surfaces if approved:
`src/Engine.Domain/Behaviors/Mutations.cs`, domain compiler + gambit
program data/runtime, `WorldState.cs`, `KernelSnapshot.cs`. Retirement plus
legacy install must execute atomically (the generator executor's
plan/commit fire is the precedent).

## Evidence Currency

- Pre-arc runs (`relationship-history-lifetime-100k-20260713`, the seed
  4200 15k family) remain valid for *structural* claims (retirement paths,
  budget architecture, pile composition) and invalid as density/movement
  baselines — they predate the growing map and the exploration arc.
- All pre-2026-07-18 retirement queries must filter
  `retire_tick > create_tick` (phantom rollback entities; fixed by the
  atomic-fire change, commit `ec4d9e5`).
- The `map_snapshots` stream is the growth source of truth; the
  CellCreated event stream undercounts ~20× and must not be used for
  growth.

## Phase 1: Baselines (this session)

Multi-seed, current runtime, Release via Makefile targets, registered run
ids:

- `demography-baseline-3x100k-20260723` — 3 seeds × 100,000 ticks
  (`make mc`). Entity snapshots give 25k/50k intermediate states from the
  same runs.
- `demography-trace-1x25k-20260723` — 1 seed × 25,000 ticks with
  `--trace` for movement/verb attribution.

Metrics (analysis leads with map utilization, roaming, culture shape,
lifecycle churn, and relationship dynamics per repo rules; generator fire
counts are supporting evidence):

- usable-map denominators per plane over time; active/created/retired by
  kind against them; creation and retirement per unit of map growth;
- mortal share of the active population over horizon (must not fall for
  the plan to be unnecessary; expected: it falls);
- budget bind: eligible-vs-fired ratio over time and vs territory;
- per-plane density: active per covered cell (physical), occupied fraction
  (semantic); exact-stack and radius/component cluster distributions,
  cluster age/turnover/productivity;
- movement: immediate reversal, path efficiency, destination novelty,
  frontier-cell entry share, route adherence, contested-front
  reinforcement — post-arc values to compare against the pre-arc 50%/2.8%;
- pile composition: wild vs held/claimed artifacts; artifact-signal
  neighborhoods;
- gambit offer/selection/success/resolution and retirement rates;
  crowding/support exposure followed by departure, founding, retirement.

Material findings recorded via `append-stats` on the registered runs.

### Phase 1 results (2026-07-23, recorded on the registered runs)

Baselines complete; all structural claims confirmed on the current runtime:

- **Growth vs territory:** physical cells ×4.5, semantic ×3.6, covered
  440→~1,013; active 44→753 (×17). Active per covered cell 0.10→0.74,
  still rising at 100k, consistent across all three seeds.
- **Anchor leak:** zero retirements of artifacts (376 created/run),
  regions (79), settlements (73), POIs (28); canons ~0.7. Mortals churn
  (penguins 333/296).
- **Thinning:** mortal share of the active population 24.5% at 25k →
  8.4% at 100k.
- **Budget bind:** fires flat at ~425 per 25k quarter for the whole run
  while eligible-not-selected holds ~8,600/quarter — a 20× eligible
  surplus that never converts as territory triples. K1's budget-scaling
  premise is confirmed.
- **Movement:** artifacts dominate all motion (10.1M moves across 3 runs,
  10× penguins) at 67% immediate reversal, 10.4% efficiency. Trace
  attribution splits it roughly evenly between the gameplay verb layer
  (54.6% reversal; artifacts run a full roaming verb catalog —
  `artifact_devote`, `artifact_seek_bearer`, `artifact_circulate`, …) and
  the relationship program (47% reversal — competing relationship pulls
  produce tug-of-war oscillation; transports show the same signature at
  66–76%). Penguin verb movement is healthy (26.3% reversal, improved by
  the exploration arc); K2's first targets are artifact and transport
  movement, not explorers.
- **Piles and spray:** max stack 30 at 100k (pre-arc 14), 23 hexes ≥6 per
  run; semantic occupancy 16.0%→19.6% against the grown plane; 85% of
  artifacts wild.

Numeric band ratification and the Phase 2 slice proposal can proceed from
these numbers.

### Phase 2–3 implementation (2026-07-24, awaiting the bundled 3×100k verdict)

Implemented and smoke-verified on matched seed 8100 (registered runs
`slice1*`–`slice3*`): zeitgeist bands for artifacts/POIs; settlement
inverse carrying capacity (crowding-comfort lifeline + `MapCrowdingSolitude`
drain — empty foundings die in a lifetime, peopled hubs persist; first
settlement deaths ever); willful-artifact gambits (slip-the-grasp,
enshrinement endowing a reversible work-tier legacy); the endings catalog
on cadence clocks (seal-shatter, one-word-yields, memorial wall, tome-read,
offering-spent) plus war razing of hollowed settlements; artifact
rest-at-destination with longer commitments; carried-entity movement
suspension (tug-of-war fix — artifact reversal 67%→14–18%); crowded-origin
departure boost; semantic travel influence; artifact beacon removal +
saturation 0.6; and provincial pulses (per-superregion clocks, bands
Frontier ×8 / Settled ×3 / Heartland ×1 on base width 50, thresholds
15/100), calibrated 2,278→990→570 fires per 25k (~2.6× the saturated
global clock). Verification debts and the run scorecard: `docs/backlog.md`.

## Phases 2–5 (post-approval)

2. **Tune existing authored knobs** (verb weights/commitment, generator
   conditions, bands, health effects, gambit rates, map signals) against
   matched seeds. Do not accept a lower active count that also destroys
   map utilization, mortal growth, clustering, or narrative churn.
3. **Add missing reusable vocabulary** only where Phase 2 attribution
   demonstrates the gap: frontier/contested-front movement targeting,
   explicit self-retirement, atomic legacy materialization, territory-
   scaled fire budget. Kernel discipline doc first.
4. **Author Ice Remembers retirement content**: a deliberate mix of
   endings, starting with high-volume clutter patterns (war-memorial
   consolidation, artifact installation, settlement razing, canon
   supersession). Validate that clusters become more meaningful, not
   merely emptier.
5. **Long-horizon validation**: matched multi-seed runs against the
   acceptance shape.

## Acceptance Shape

Numeric bands ratified from Phase 1, but a successful design shows: active
stock grows with usable territory on both planes; mortal stock churns with
modest positive growth during expansion; durable kinds have observable,
fictionally coherent retirement; physical and semantic density stop
diverging from their maps; large clusters associate with settlements,
holdings, routes, fronts, or named events and generate outward
consequences; artifacts and transports show materially lower reversal and
higher progress per path length; frontier and contested-boundary activity
rises without scripted allocation; historical identity is preserved; the
foreground cast is legible.

## What Will Not Change

- No global active-entity cap, hidden ledger, or reservoir state.
- No uniform-distribution rule or blanket anti-clustering penalty.
- No forced city/holding/route/frontier assignments; selectors stay
  probabilistic.
- No deletion of historical entities as a density fix.
- No dynamic meta-entity system (legacy retirement gambits supersede it).
- No revival of the removed process deck.
- Regions stay `WorldAnchor`.
- Mortal-kind death mechanics are sound; their *inflow* scales, not their
  dying.
- No assumption that existing runtime vocabulary limits design — engine
  changes stay available where the reusable mechanic genuinely belongs
  there, with the kernel discipline doc as gatekeeper.

## Approval Boundary

Phase 1 (evidence only) proceeds now. No simulation, tuning, telemetry
schema, or architecture change lands until an exact first implementation
slice — its knobs, files, and matched-run comparison — is approved against
this plan.
