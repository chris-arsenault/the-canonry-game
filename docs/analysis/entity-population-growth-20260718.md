# Entity Population Growth Does Not Reach a Demonstrated Steady State

Point-in-time finding, 2026-07-18. This document hands off the simulation-side
investigation prompted by the game reaching roughly 200 active named entities
around tick 15,000. UI scalability is a separate problem; the population shown
by the UI is present in simulation state.

No simulation fix is proposed or implemented here.

## Observed Failure

The active named-entity population grows far beyond the startup census during
the bootstrap horizon, then continues growing through a 100,000-tick run. The
existing evidence does not demonstrate a steady state.

This matters independently of rendering. Active named entities participate in
ordinary simulation programs, while historical entities remain in the graph for
history and relationship reference. Moving an entity to historical status
therefore creates active-population churn but does not delete graph history.

The failure should be stated carefully:

- Reaching 200 active entities is not by itself proof that the intended
  population is wrong; the design explicitly expects a bootstrap phase.
- Continued long-horizon accumulation is a real warning because the intended
  game is much longer than the usual 15,000-25,000-tick evidence window.
- No accepted active-population target currently exists. In particular, a
  target such as 120-150 entities has not been established as a design
  invariant.

## Registered Evidence

All cited values are stored on registered Monte Carlo runs. The two 15,000-tick
runs are a matched seed and show that the recent knowledge-grant repair is not
the source of the population count.

| Run ID | Seed / horizon | Population evidence |
|---|---|---|
| `knowledge-real-grants-baseline` | seed 4200, 15,000 ticks | 203 active, 38 historical, 130 generator fires |
| `knowledge-real-grants-postrepair` | seed 4200, 15,000 ticks | 155 active at tick 10,000; 202 active and 62 historical at tick 15,000; 130 generator fires |
| `relationship-history-lifetime-100k-20260713` | seed 7900, 100,000 ticks | 149 active at tick 10,000; 293 at 25,000; 509 at 50,000; 841 at 100,000; 1,017 historical at the end |

The post-repair 15,000-tick run records 220 generated entities after startup.
At the final tick, artifacts are the largest active kind: 48 active artifacts,
42 of them generated. Penguins are next at 39 active, 36 of them generated.
Only 62 entities of all kinds have become historical by then.

The 100,000-tick run records 859 generator fires and 389 active artifacts at
the final tick. Artifacts alone account for about 46% of its 841 active
entities. Its registered analysis flag `narrative_population_equilibrium` is
`0`.

The long run was produced from an earlier dirty worktree and a different seed,
so it is structural evidence, not a matched-current balance result. A current,
multi-seed long-horizon confirmation is required before accepting a fix.

## Causal Hypothesis

The leading hypothesis is a stock-and-flow mismatch between generator creation
and authored retirement, especially for world anchors.

1. Generators are the high-altitude mechanism that creates new named entities.
   In the post-repair run they introduce 220 entities by tick 15,000.
2. Lifecycle turns mortal entities historical, but does not remove historical
   graph records. By tick 15,000 only 62 entities have made that transition.
3. `Artifact`, `Region`, and `PointOfInterest` currently use
   `IceRemembersLifecycleBand.WorldAnchor`. World anchors receive no lifecycle
   drain and can end only through deliberate gambits or generator consequences.
4. The generator catalog continues to create artifacts, regions, and points of
   interest. Causal limits prevent meaningless immediate repetition of one
   cause; they are intentionally not a global population cap and do not imply
   that created world-anchor stock will later leave the active pool.
5. Generated entities also open new relationships and targets. Some creation
   loops may therefore replenish or expand the conditions that allow later
   creation, even when every individual generator satisfies its local causal
   limit.

The artifact result makes world-anchor accumulation the strongest confirmed
part of the hypothesis. It does **not** establish that all artifacts should age,
that every world anchor is wrong, or that mortal lifecycle balance is already
correct. At 15,000 ticks, Generational and Monumental kinds may still be within
their intended lifespans, so part of the bootstrap rise can be healthy delayed
retirement rather than a defect.

## Owning Layer

Primary ownership is the Ice Remembers authored-domain layer:

- which produced things deserve to become named entities;
- which kinds or subtypes are truly ageless world anchors;
- what ordinary, narratively meaningful acts retire, destroy, consume, merge,
  or transform them;
- generator bundle size and the cause/spend/reopen loops that sustain creation;
- mortal lifecycle bands and supporting health effects.

`Engine.Gameplay` is a secondary owner only if the investigation proves the
authored vocabulary cannot express a needed lifecycle or transformation rule.
`Engine.Kernel` should change only if evidence shows its existing lifecycle,
status-transition, or generator semantics are executing incorrectly. Monte
Carlo and observability consume evidence; they do not own population behavior.

## Investigation Questions

The next pass should begin from the evidence, then trace into authored content.

1. What are creation, active-to-historical transition, and net-active rates by
   kind in 5,000- or 10,000-tick windows? Do those rates converge across
   several seeds at 25,000, 50,000, and 100,000 ticks?
2. Which generators contribute the most surviving entity-ticks, not merely the
   most fires? Account for bundle size, optional produces, kind, subtype, and
   the survival of each output.
3. Which causal loops expand their own future target pool? Identify the exact
   cause, spend/transform, and reopen path before changing a gate.
4. For every generator-produced `WorldAnchor`, what fiction makes it a
   permanent named object or place? What deliberate gameplay path can end or
   transform it? Is `WorldAnchor` truly a kind-wide property, or does the domain
   need a more specific authored distinction?
5. How much of 15,000-tick growth is healthy bootstrap stock in Generational or
   Monumental kinds, and how much persists after their expected lifespan bands?
6. Why do mortal kinds leave the active pool when they do? Separate age drain,
   effects, minor-actor maintenance, and explicit story consequences rather
   than tuning total deaths alone.
7. What population shape does the fiction require by kind? Establish desired
   stock and churn ranges at explicit horizons before changing authored values.
8. Does historical graph growth remain operationally healthy after the active
   population is stabilized? That is related but distinct from active census.

The confirmation runs must use unique registered run IDs, fresh builds, and
matched seeds for candidate/control comparisons. Record material DuckDB
findings through the Monte Carlo CLI before reporting them.

## Likely Code and Document Surfaces

Start with these authored-domain surfaces:

- `src/IceRemembers/Entities/IceRemembersEntityDefinition.cs`
- `src/IceRemembers/Entities/ArtifactEntity.cs`
- `src/IceRemembers/Entities/RegionEntity.cs`
- `src/IceRemembers/Entities/PointOfInterestEntity.cs`
- the remaining `src/IceRemembers/Entities/*Entity.cs` definitions when
  measuring mortal stock and churn
- `src/IceRemembers/IceRemembersGeneratorDefinition.cs`
- `src/IceRemembers/IceRemembersGeneratorDefinition.Canons.cs`
- `src/IceRemembers/IceRemembersGeneratorDefinition.NextSet.cs`
- `src/IceRemembers/IceRemembersGeneratorDefinition.Nightshelf.cs`
- `src/IceRemembers/IceRemembersGeneratorDefinition.OrcaMonsterLoops.cs`
- `src/IceRemembers/IceRemembersGeneratorDefinition.PreyResponse.cs`
- `src/IceRemembers/IceRemembersGeneratorDefinition.Remembrance.cs`
- `src/IceRemembers/IceRemembersGeneratorDefinition.Targets.cs`
- `src/IceRemembers/IceRemembersGeneratorDefinition.WakeSingers.cs`
- `src/IceRemembers/IceRemembersGeneratorDefinition.WarSaga.cs`
- `src/IceRemembers/IceRemembersDefinition.cs`
- `src/IceRemembers/IceRemembersMinorActors.cs` and authored effects only if
  the mortal-retirement analysis points there

Read and preserve these design contracts:

- `docs/design/generators.md`
- `docs/systems/09-lifecycle.md`
- `docs/systems/20-generator-causal-limits.md`
- `docs/systems/simulation-principles.md`
- `docs/tools/monte-carlo.md`

Kernel runtime surfaces such as
`src/Engine.Kernel/Programs/Upkeep/UpkeepProgramRuntime.cs` and
`src/Engine.Kernel/Programs/Generators/` are diagnostic surfaces first, not the
default edit location.

## Constraints on Any Remediation

- Do not impose an arbitrary global active-entity cap, hidden recent-run
  ledger, per-generator saturation counter, or UI-only visibility cap as a
  simulation fix.
- Do not make every artifact, region, or point of interest mortal. Preserve the
  intentional distinction between ordinary mortality and genuine world
  anchors; classify outputs case by case.
- Do not globally slow every generator before identifying which outputs and
  reopening loops create retained stock. Generators are supposed to keep an
  open-ended world alive.
- Do not delete historical entities to make the active count look smaller.
  Historical retention is a separate graph-history contract.
- Do not infer that technology grants caused the growth. The matched baseline
  and post-repair runs end at 203 and 202 active entities with the same 130
  generator fires.
- Do not treat one seed, one end-state total, or a UI performance threshold as
  a balance invariant.
- Do not edit engine runtime until a runtime failure, rather than authored
  content balance, is demonstrated.
- Do not tune only to reduce a broad statistical count. Any retirement,
  consumption, or transformation must make sense in the world and preserve the
  diversity, map-life, and narrative-continuity goals in simulation principles.

## Handoff Outcome

The next agent should produce a scoped proposal, not an immediate fix. That
proposal must name the target population shape and horizon, the exact creation
and retirement loops being changed, the owning layer and files, the matched
Monte Carlo comparison, and what world-anchor behavior will remain untouched.
