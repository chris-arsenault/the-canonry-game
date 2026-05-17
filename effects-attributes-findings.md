# Effects and Attributes Findings

Date: 2026-05-17

Purpose: brief review note for a follow-up agent. This summarizes what is true
in the current checkout about `Effects`, attributes, and influence reads.

## Summary

Effects and attributes are still present and active in the current simulation.
They do not appear to be deleted. However, their design value is questionable:
the active rules look like an older health/shelter/stress layer, and they
should not be treated as a strong design reason to preserve entity-owned
influence without further review.

The strongest current non-self-referential use of influence remains map
control/coverage expansion. Relationship maintenance and effects also read
influence, but those uses may be legacy or conceptually weak.

## Evidence

- `src/IceRemembers/IceRemembersDefinition.cs` includes
  `Effects = CreateEffects()` in the gameplay definition.
- The same file declares the `attribute_effects` module and builds effects for
  every Ice Remembers entity kind through `CreateEffects()` / `AddHealthEffects()`.
- `src/Engine.Gameplay/CompiledGameplayPrograms.cs` has
  `EffectsProgram.EnabledByDefault = true`.
- `src/Engine.Host/KernelProgramInstaller.cs` installs the Effects program
  unless program configuration disables it.
- `src/Engine.Kernel/Simulation.cs` registers the Effects program and ticks all
  active systems through `TickSystems()`.
- `src/Engine.Kernel/Programs/Effects/EffectsProgramRuntime.cs` iterates every
  active entity each tick and applies attribute delta rules for that entity kind.

Monte Carlo evidence from
`.artifacts/monte-carlo/authoring-distribution-tune-10x5000-20260514/telemetry.duckdb`:

- `attribute_events` contains `15,304,712` rows across 10 runs.
- By source code, deduplicating symbols:
  - `gameplay_effects`: `11,323,466`
  - `upkeep`: `3,976,606`
  - `generator`: `4,340`
  - `startup`: `300`

This indicates the Effects runtime is not dormant in the latest MC run.

## What Effects Do

In `IceRemembersDefinition.cs`, `AddHealthEffects()` creates rules for each
kind's health-like attribute:

- positive deltas from related influence at location for relationships such as
  `allied_with`, `home_base`, `anchored`, `controlled_by`, and `trade_route`;
- negative deltas from related influence at location for relationships such as
  `rival_of`, `at_war_with`, `corrupted_by`, and `betrayed_by`;
- positive/negative deltas from relationship proximity;
- positive/negative deltas from local map signals such as support, threat,
  crowding comfort, and crowding load.

These rules mutate attributes such as:

- settlement `temp_hp`;
- faction `cohesion`;
- penguin `vitality`;
- artifact `condition`;
- monster `containment`;
- region `stability`;
- transport `integrity`;
- occurrence `momentum`;
- ability `clarity`;
- orca `endurance`;
- point of interest `presence`.

## Influence Reads

Influence is read in several places, but not all uses carry equal design weight.

Real and important:

- Coverage expansion checks actor influence on the target cell before claiming
  coverage. This is the strongest concrete dependency.

Mechanically real but possibly conceptually weak:

- Relationship maintenance uses source/target influence at the other endpoint's
  location for `controlled_by`, `claimed_by`, and `trade_route` maintenance.
- Effects use related entity influence at the subject's location for
  shelter/stress-style attribute deltas.

Mostly self-referential:

- Low-altitude own influence and foreign influence targeting read influence,
  but that is the influence behavior subsystem feeding itself.

Minimal gambit use:

- `faction_declares_territorial_war` has a physical
  `ContestedInfluencePrecondition`.
- Latest MC shows `317` attempts and `80` successes for that gambit.
- No broad gambit dependence on influence was found in Ice Remembers.

## Interpretation

The current code proves Effects and attributes are live, not deleted. It does
not prove they are desirable.

For the culture/influence redesign, do not use Effects as a primary argument
against cultural roll-up. Treat Effects and attribute deltas as a separate
legacy/design review target. The safer conclusion is:

- keep entity influence for now because coverage/control depends on it;
- add culture influence as a roll-up/read model;
- audit whether relationship maintenance and Effects should continue reading
  entity influence, read cultural influence, or be removed/simplified.

