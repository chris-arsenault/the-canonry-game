# Entity Stages

Owner-approved design (2026-07-24). Supersedes and absorbs: the tier-as-stage
exploration, the Deepen the Holding rebalance, the settlement signed-bleed
proposal (dead), and the settlement Generational band flip (absorbed into the
stage tables). Companion docs: `settlement-population.md` (its survival terms
become promotion/demotion criteria), `artifact-districts.md`.

## Model

Every entity kind may declare an ordered catalog of **discrete named
stages** — identity, not a bare number:

```
Settlement: town -> city -> capital
Artifact:   trinket -> relic -> legend
Faction:    warband -> order -> dynasty
Canon:      edict -> accord -> doctrine
```

A stage is an authored object with a stable name, a display name, and an
**extensible property set**. Initial properties:

- **Band multiplier** — the stage's lifecycle clock, applied to the kind's
  base band (settlement base = town ≈ Standard; city ×3 ≈ Generational;
  capital ×6 ≈ Monumental). Founding fragility and earned permanence come
  from one mechanism.
- **Worker bonus** — cohort slots granted at this stage (replaces the raw
  `+tier` term in the minor-actor cohort formula; steps at promotions).
- **Ambient emission** — per-tick culture influence the entity radiates at
  this stage (the structured replacement for treadmill mesh-holding).
- **District capacity** — installed-artifact cores the entity may hold.

The set is designed to grow: future properties (gambit grants, map-signal
scaling, movement rights, pulse weighting) attach to the same catalog entry
without new axes. Kinds without a catalog have a single implicit base stage
and behave exactly as today.

Kernel discipline: the kernel stores only a per-entity stage index (reusing
the existing per-entity progression store) plus per-(kind, stage) property
registries — no strings, fixed capacity. Stage names register as symbols so
telemetry and UI resolve identity the way subtypes and traits do. Each new
kernel-relevant property is one new registered table; authoring-only
properties never touch the kernel.

## Progression

- **Promotion is rare, hard, and named.** Deepen the Holding is reborn as
  the promotion gambit family for catalog kinds (its fiction always was
  "the holding deepens"): standard 25% success, per-stage scaling gates
  (culture mass thresholds, districts held, era context). The 12-kind
  universal tier grind is retired outright — measured at 50.1% of all
  gambit executions, its removal is the pool decluttering.
- **Demotion comes from events**: generator target side-effects ("the war
  ruins the city"), plus a hysteresis-gated hard-times path (sustained low
  health over a window, demotion cooldowns, promotion gates set well above
  demotion thresholds to prevent spirals). Demotion steps the entity down
  one stage with a partial health reset; death occurs only at the base
  stage.
- A reawakened district core returns at the stage it held when installed.

## The mesh compensation package

Measured (run `slice5b-generational-settlements-1x25k-20260724`): worker
labor grosses 1.79M influence against 1.69M decay (+95k net, treadmill
holding interior ground), while entity travel/verb/birth influence nets
+451k — already the growth engine. Removing the grind removes the treadmill,
so interior holding moves to structured sources:

1. Per-stage ambient emission (above) — interior mass follows civic
   maturity, and promotion becomes mesh-consequential.
2. District culture tiles tuned up from their current trivial magnitude.
3. `DecayPerPulse` 0.005 → 0.004 (the rate was calibrated against
   treadmill-era gross input).
4. Optional: travel influence on one or two more mobile verbs.

**Known risk to gate on:** lifecycle-maintenance labor is 78% tier-carried
today; mobile kinds losing grind tiers shortens their effective lifespans.
Mortal churn must be re-verified within its healthy band.

## Phases

- **P1 kernel/vocabulary**: stage catalogs in domain config compiled to
  per-(kind, stage) registries (band multiplier consumed by upkeep, worker
  bonus consumed by minor actors, emission consumed by effects, district
  capacity consumed by install); demote op + mutation + generator effect;
  stage identity in telemetry.
- **P2 authored**: promotion gambits with scaling gates for settlement,
  faction, artifact, canon; generator demotion/promotion side-effects on
  the war and accord loops; grind removal; the mesh package; artifact
  stages wired to fade rates and district install rights (legend-only).
- **P3 validation gates** (registered matched runs, before the bundled
  3×100k): mesh income statement holds or improves; deepen family <10% of
  gambit executions; stage pyramid forms with deaths only at base stage;
  mortal lifespans within band.

## Not changed

Decay model (rate tweak only), minor-actor emission machinery, provincial
pulses, the district model, band enum values, mortal death mechanics.
