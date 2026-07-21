# Terrain reset calibration — v12 worlds (2026-07-21)

Point-in-time findings from slice 7 of the terrain-layer reset, measured
after the drainage/elevation rework landed. Instrument: `WorldGenerator
diagnose --hex-contract`; visuals: entity-overlay captures in
`.artifacts/captures/seed*/`.

## Headline

**The fact-driven contract absorbed the elevation rework without retuning.**
Capacity, placement taste, and movement derive from the quantized fact
columns via formulas, so when the generator's elevation model changed
underneath them they re-derived correct answers on their own. The only
recalibrations the rework required were generator-side (temperature lapse,
scree threshold, floodplain relief gate) and were made in that workstream.

## Quantizer health (fact columns)

Full-field saturation at byte 255, seed 42:

| fact | %at255 | note |
|---|---|---|
| elevation | 0.6–1.4% | doubled elevation band still fits; clamps only on extreme peaks, 0% in the heart |
| relief | 4.7–6.6% | clamped hexes are impassable crag anyway; watch item, not a defect |
| moisture | ~48% | this is the ocean share — open water is fully wet; not quantizer blindness |
| everything else | ≤1.5% | healthy |

`FromElevation` (−256..1784, 8 units/step) and `FromReliefUnits` (4/step)
stay as they are.

## Acceptance and validator teeth

Validator now enforces: land/water partition per entity, per-kind
dispersion (>50% of a kind within distance 1 fails), heart viability
(≥40 cells with derived capacity ≥1).

The teeth immediately exposed a structural hole (fixed in e093474): the
avoided-terrain penalty was a score, so a host-anchored search whose ring
held only sea seated land kinds on water. With the partition as a hard
candidacy filter plus a widened-ring fallback, acceptance went from
**16 attempts (seed 42) to 0**. v12 worlds: 42/0, 4200/0, 9173/2,
21031/0 — the 9173 rejections are coverage/subdivision infrastructure
retries, not placement failures.

## Per-seed character (heart, 1027 cells)

| seed | habitable ≥1 | rich ≥3 | shape read from overlay |
|---|---|---|---|
| 42 | 66% | 26% | continental heart, hub + corridor spread |
| 4200 | 60% | 24% | land-bridge coast; original edge-pile failure gone |
| 9173 | 12% | 3% | harsh world; entities hug the one habitable fringe |
| 21031 | 48% | 5% | archipelago; placements string along the shore, water kinds in the strait |

Taste spreads (p90−p10) stay real on every world (18–92 points), so
identity always has geography to read; on the harsh 9173 the spreads
compress but do not flatten.

Dispersion after the fixes: no kind exceeds the adjacency ceiling;
penguins meanNN 3.4–8.2 (the seed-4200 failure was a single stacked
pile). Artifacts run closest to the ceiling (by design: they anchor to
settlements at radius 4–5) — watch item.

## MC runtime check (12 runs × 1000 ticks, seeds 1000–1011)

_See `.artifacts/monte-carlo/terrain-reset-v12b`._

- **Acceptance at scale:** 12/12 worlds accepted, max attempt 3 — the
  validator does not reject healthy worlds.
- **Utilization:** physical activity reaches 52–79% of heart cells at
  only 1000 ticks (the growing-map target band starts at ~44%).
- **Movement respects identity:** settlements, POIs, canons, factions
  never move physically; penguins (94% movers), orcas (84%), transports
  (100%) roam at radius ~13; artifacts move by being carried.
- **Terrain occupancy:** life concentrates on tundra, the frozen coastal
  band (sea-ice), hot springs, and productive water; wastes and basalt
  stay near-empty. No pathological pile-ups.
- **Found and fixed:** exact-attribution queries showed the kernel
  generator placement seating land kinds on walkable water (artifacts on
  shelf, ~4/run) — the same score-only partition hole the seed builder
  had. The avoided set is now a hard candidacy filter on the physical
  plane (semantic avoidance stays soft), pinned by
  `AvoidedTerrain_IsAHardWall_PlacementWidensPastAnAllWaterRing`. The
  audit also surfaced an authoring gap the other direction: wake-singers
  generators produce leviathans and deep-given artifacts anchored at sea,
  so Monster and Artifact gained `CultureRule(WakeSingers, Maritime())` —
  their water placements are now legal rather than accidental.
- The post-fix MC re-confirmation is pending: a concurrent workstream's
  in-progress worldgen edits currently fail the analyzer build, so a
  fresh MC binary cannot be produced. The kernel test carries the
  guarantee until then.

Worlds are at **v13** (the wake maritime rules change seed placement):
42/0, 4200/0, 9173/2, 21031/0.

## Watch items

- Relief byte clamps on ~5–7% of full-field hexes (extreme mountains).
  If gameplay ever needs to distinguish severity above the clamp, widen
  `FromReliefUnits` to 6 units/step and bump the worldgen version.
- Artifact adjacency sits at the dispersion ceiling on some seeds.
- The kernel-facing accessor for geological features (rivers, lakes as
  named objects) is persisted in the blueprint but not yet read by the
  kernel — that is the natural next terrain-layer feature.
