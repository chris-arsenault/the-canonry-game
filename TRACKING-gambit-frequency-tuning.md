# Gambit Frequency Tuning Notes

Date: 2026-05-23

This note records tuning knobs for gambit frequency. It is not a proposal to change them yet.

## Current Scheduling Model

- Gambits run on the main kernel cadence, currently controlled by `KernelProgramTuningOptions.MinorActorMainTickRatio`.
- The default ratio is `5`, so the gambit program evaluates active entities once every 5 simulation ticks.
- On each gambit pulse, every active entity is considered.
- An entity can select at most one gambit per gambit pulse.
- A selected gambit applies both per-gambit cooldown and per-entity gambit cooldown.
- Authored cooldown values are scaled by `ScaleMainTicks(...)`, so the current ratio multiplies authored gambit cooldowns by 5.
- Cooldowns are jittered by roughly +/-25%.

## Tuning Knobs

- Main cadence: `MinorActorMainTickRatio` changes how often gambits, generators, low-altitude verbs, upkeep, movement, and other main programs run relative to minor actors.
- Per-gambit cooldown: `GambitDefinition.Cooldown` controls how soon the same entity can use the same gambit again.
- Per-entity gambit cooldown: `GambitDefinition.EntityCooldown` controls how soon the same entity can use any gambit again.
- Eligibility/preconditions: stricter preconditions reduce selected gambit frequency even when the entity is off cooldown.
- Score expression: zero or low scores reduce selection chance; high scores can crowd out competing gambits for the same actor kind.
- Actor catalog coverage: kinds with only one eligible actor-side gambit will appear to use that gambit disproportionately.
- Success chance: changes how often selected gambits actually mutate state, but does not reduce selected attempt frequency.

## Current Telemetry Gap

Current MC exports selected gambits, successes, actors, targets, and timing. It does not export every skipped entity-pulse or classify idle reasons. To tune frequency precisely, the useful missing breakdown is:

- skipped by entity cooldown
- skipped by per-gambit cooldown
- no matching gambit for actor kind
- no target candidates
- candidates present but total score <= 0
- selected and failed
- selected and succeeded
