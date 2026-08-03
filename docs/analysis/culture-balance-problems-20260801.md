# Culture balance problem register (2026-08-01)

Parity-config update (2026-08-01, 4 draws — seeds 9100/9101/9102/9103
under the actor-parity build, runs parity-r18-*): census shares
(Aurora/Nightshelf/Wake-Singers/world) = 19/17/55/9, 20/19/53/8,
18/21/50/10, 33/39/17/10 — every draw inside or near the 60/20/20
envelope, Wake-Singers never above 55% and smallest on one draw with no
runaway by anyone. Death rates: Aurora 0.43-0.60, Nightshelf 0.63-0.78,
Wake-Singers 0.32-0.38 — the gap narrowed from ~3:1 to ~1.7:1.
Physical mass top share 0.37/0.81/0.85/0.86 (baseline mean 0.84).
Births on unheld cells, Nightshelf: 0.58-0.84 (baseline 0.76) — little
movement; the occupation placement-ref sweep remains the open lever.
Baselines below are the pre-parity 9-run values.

Kind-general update (2026-08-03, 4 draws — seeds 9100/9101/9102/9103,
runs parity-r19-*): the paired per-kind emissions were replaced by
kind-general generators (target either actor kind, produce the
target's kind, casualties drawn from the joint pool). Census =
56/20/19/5, 15/18/51/17, 19/48/29/3, 33/47/16/4 — every draw within or
near the envelope, maximum culture share 56%, and the leading culture
ROTATES (Aurora tops 9100, Nightshelf 9102/9103, Wake-Singers 9101) —
first build where the winner is not fixed by authorship. Physical top
share 0.57/0.61/0.67/0.88. Death rates: Wake-Singers 0.33-0.43,
Aurora 0.44-0.72, Nightshelf 0.51-0.84. Orca births 45-120 per world
with births/deaths near parity - the kind persists on presence without
dedicated machinery.

Basis: 9 runs of the current configuration, 20k ticks, seeds 9100/9102/9103
(stages-r16g) and 9101/9104/9106/9107/9108/9109 (stages-r17c/r17d).
Telemetry culture ids: 1 = Aurora, 2 = Nightshelf, 3 = Wake-Singers,
4 = world.

Rule: refer to these problems by their descriptive names below — the
name should state the finding plainly enough to need no lookup. Refer
to cultures by name. Define any new metric by its query before using
it. Do not introduce summary nouns for patterns; state the numbers.

## Physical mass share above target

Metric: largest culture share of total physical raw_influence at tick
20000 (cfs, plane 1).
Current: 0.64–0.95 across 9 runs, mean 0.84. Wake-Singers hold the
largest share in 9/9 runs, and in every 2500-tick window in 8/9 runs
(exception: 9104 window 1, Aurora).
Target: <= 0.60.

## Death-rate gap

Metric: deaths in window / population at window start, per culture,
windows 2–7 (ee event 2, ents).
Current: Wake-Singers 0.30–0.35 per window in all runs. Aurora and
Nightshelf 0.75–1.15. Deaths are 78–86% source `gameplay_effects`
(attribute engine), not generators or gambits.
Known mechanism links: attribute regeneration requires own-culture
influence >= 35/40/45 at the entity's cell (InfluenceShelter,
IceRemembersDefinition.cs); cells at >= 35: Wake-Singers hold 950–2000
per run, Aurora and Nightshelf 16–200, world 0.
Target: unset.

## Births on unheld cells

Metric: fraction of births where own-culture raw_influence at the birth
cell is < 1 (bf lane, physical_local_raw_mass).
Current (6-run pool, replicated on the 3-run pool): Aurora 41%,
Nightshelf 76%, Wake-Singers 9%, world 100%. Present from window 0
(Wake-Singers 55% unheld, Nightshelf 82% unheld) — precedes any
territory difference, so it is a property of generator placement refs,
not an effect of holding territory.
Births on contested or border cells are ~1% for all cultures.
Target: unset.

## Smallest-culture population

Metric: smallest of Aurora/Nightshelf/Wake-Singers by living entity
count at tick 20000.
Current: 16–128 across runs. Aurora is smallest in 5/9 runs (minima 16,
25), Nightshelf in 4/9. Wake-Singers are never the smallest.
Target: 60/20/20 implies each culture >= ~20% of the census.

## Nightshelf semantic lead does not persist

Metric: culture with the largest semantic mass share per window (cfs,
plane 2).
Current: Nightshelf leads window 1 in 8/9 runs; leads at tick 20000 in
1/9 (9102).
Target: unset. Open question whether this is a problem or intended
asymmetry.

## Orca/penguin kind asymmetry (root of the death-rate gap)

Owner context: in the original simulation orca was a subtype of one NPC
kind; orcas and penguins are supposed to be treated the same for most
purposes. Authored divergence as of this register:

- Lifecycle band: orca = Enduring (4,500 ticks), penguin = Brief
  (2,000 ticks). 2.25x.
- Observed death rate per window (9 runs, windows 2-7): orca 0.29;
  penguin 1.24 (Nightshelf), 1.38 (Aurora), 2.54 (Wake-Singers' own
  penguins - the highest rate of any actor kind in the world). The
  penguin rate is high in every culture, so the protection is attached
  to the kind, not the culture.
- Generator produce sites: 15 orca, 46 penguin. Orca sites place at
  own-culture anchors (shoal placement refs, CultureRoute placement,
  homed pairs with MemberOf); penguin sites place at the firing
  target (war sites, offering sites, rich hubs). This is the main
  driver of births on unheld cells.
- Reproduction: orcas have an explicit chain (matriarch calving, homed
  scout pairs, succession) added during the orca-extinction fixes;
  penguins have none - every penguin birth is a byproduct of some
  other event.
- Field presence (Locality): orca baseMass 5 / radius 2 / influenceScale
  0.03 / healthScale 0.04 / threat 0.6; penguin baseMass 2 / radius 1 /
  influenceScale 0.02 / healthScale 0.02 / threat 0. Per entity an orca
  projects ~2.5x the mass over ~3x the cells. Feeds physical mass share
  independent of demographics.
- Traits: orca "Sure Wake" is an attribute-regeneration trait (allies
  restore endurance); all penguin traits are verb-weight traits. No
  penguin regeneration trait exists.
- Subtype vocabulary: orca subtypes are a family lattice (matriarch,
  bull, scout, elder) that content builds succession/homing on; penguin
  subtypes are occupations (warden, broker, witness, exile) that
  content places at events.
- Symmetric (checked, equal or penguin-favoring): category, tier,
  primary plane, verb counts (24 vs 21), gambit actor roles (penguin 29,
  orca 8).

Consequence: Wake-Singers are the orca culture, so the kind asymmetry
surfaces as a culture asymmetry in the death-rate gap and physical
mass share.

## Non-problems (previously reported, not reproduced in these 9 runs)

- Physical lead changing hands mid-run: 0 occurrences after window 1.
- Wake-Singers population collapse: never the smallest culture.
- Births on contested cells ("front pressure" on newborns): ~1%.
