# Settlement Population and Survival

Owner-corrected design (2026-07-24). Replaces the occupancy-based
comfort/solitude lifeline. Population is not a head-count of co-located
named entities; it is the settlement's standing in the systems that
already represent people and institutions — and survival is a **budget of
multiple terms, none singly decisive**.

## The terms

Against Monumental age drain, a settlement's sustain comes from:

1. **Worker establishment** (largest term, fully graduated): the existing
   minor-actor `sustain_source_lifecycle` maintenance. Scales continuously
   with work tier (cohort size, earned via Deepen the Holding), the local
   culture support the workers spend, and the settlement's actual
   shortfall. No tier is an on/off switch.
2. **The masses**: ambient culture sustain (influence shelter, slots,
   jurisdiction) — gated so it matters but cannot carry a settlement
   alone.
3. **Institutions**: relationship support — trade routes, charters,
   alliances, anchors. Same gate.
4. **Districts**: installed artifact cores (Healing districts) as a
   deliberate substitute for population — a shrine can keep a small
   hermitage alive.

Calibration intent: any single healthy term covers well under the drain;
roughly two strong terms break even in mid-life; the late-life quadratic
asks for three. So a young founding can live on masses + one trade route
while earning tiers, and a great city that loses its culture mass and
routes genuinely declines despite its tier.

## What was removed

The `MapCrowdingComfort`/`MapCrowdingSolitude` settlement lifeline: its
population measure was kind-blind current-tick named-entity co-location
(artifact piles and monsters read as citizens; the actual masses counted
for nothing). `MapCrowdingSolitude` remains in the effect vocabulary as a
generic reusable condition.
