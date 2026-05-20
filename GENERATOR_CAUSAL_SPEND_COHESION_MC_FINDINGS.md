# Generator Causal Spend Cohesion MC Findings

Date: 2026-05-20

## Question

Does generator causal spend prevent cultural cohesion by targeting strong cultural components once, consuming or locking the cause, and then forcing later generator activity away from that component?

## Runs

Primary run:

- `generator-causal-spend-slot-multiplier-5x5000-20260520`
- 5 runs, 5000 ticks each
- seeds 7400-7404
- 1000 generator fires total
- artifacts: `.artifacts/monte-carlo/generator-causal-spend-slot-multiplier-5x5000-20260520`

Comparison run:

- `generator-causal-cost-cohesion-5x5000-20260520`
- 5 runs, 5000 ticks each
- different seed set, so this is directional rather than a clean A/B

## Telemetry Used

The analysis used:

- `generator_target_evaluations.parquet`
- `analysis/generator_post_fire_eligibility_decay.parquet`
- `analysis/generator_component_fire_windows.parquet`

The key post-fire table tracks whether a fired target remains eligible, when it becomes eligible again, whether the same cultural component still has eligible candidates, and whether strong-field candidates are blocked by required-relationship spend or relationship-absence locks.

## Findings

The hypothesis is substantially supported, but the lockout is not only relationship-strength spend. Under the repo's broader causal-spend model, output-state locks are the larger blocker.

Primary run totals:

- `275,029` generator target evaluation rows.
- Same fired target was eligible at the next evaluation only `24.4%` of the time.
- Same fired target became eligible again within 500 ticks only `35.6%` of the time.
- Same-component eligible candidates after fire averaged `0.43`.
- Same-component candidate count was zero after fire in `74.85%` of fire-plane rows.

Strong-field fired targets:

- Physical strong fields:
  - same target eligible next evaluation: `30.86%`
  - same target reeligible within 500 ticks: `43.21%`
  - average same-component candidates after fire: `0.70`
  - component zero-candidate rate: `58.02%`
  - average connected mass delta after 250 ticks: `-9.12`
- Semantic strong fields:
  - same target eligible next evaluation: `23.23%`
  - same target reeligible within 500 ticks: `33.57%`
  - average same-component candidates after fire: `0.65`
  - component zero-candidate rate: `60.91%`
  - average connected mass delta after 250 ticks: `-37.09`

Strong-field next-evaluation blocking:

- Physical strong fired targets were next blocked mostly by:
  - `RelationshipAbsence`: `47.36%`
  - `RequiredRelationship`: `19.45%`
  - eligible: `31.71%`
- Semantic strong fired targets were next blocked mostly by:
  - `RelationshipAbsence`: `66.81%`
  - `RequiredRelationship`: `8.30%`
  - eligible: `24.30%`

Component repetition:

- `85.94%` of generator/culture/component/500-tick windows had exactly one fire.
- Only `55.56%` of physical strong-field fires and `58.50%` of semantic strong-field fires were followed by another same-generator fire within 500 ticks.
- When another same-generator fire did happen within 500 ticks, it stayed in the same component only:
  - physical: `34.81%`
  - semantic: `27.60%`

## Slot Multiplier Effect

The slot causal-spend multiplier improved the lockout metrics but did not solve cohesion.

Compared directionally against the earlier run:

- Same-target reeligibility within 500 ticks improved from `24.8%` to `35.6%`.
- Strong-field candidate loss from required relationship plus absence lock fell from `5522` to `3657`.
- Same-component candidate availability remained poor, averaging below `1`.
- Component-window repetition still remained mostly one-shot.

## Conclusion

Causal lockout is a real cohesion blocker. The biggest immediate offender is relationship-absence output locks, especially on semantic strong-field targets. Relationship-strength spend also contributes, but less than absence locks in the post-fire same-target path.

The deeper problem is that strong cultural components do not retain enough eligible generator targets after a fire. Generator activity therefore hops to other targets or components instead of repeatedly reinforcing a cultural hub.

The slot multiplier moves the right numbers, but it is not enough on its own. Future fixes should preserve causal limits while increasing component-local reeligibility or same-component alternate targets, especially for generators whose outputs currently create relationship-absence locks.
