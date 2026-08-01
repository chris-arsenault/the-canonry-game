# Actor authorship parity audit (2026-08-01)

Scope: every place content treats K.Orca and K.Penguin differently.
Companion to docs/analysis/culture-balance-problems-20260801.md.
Engine constraint: a generator target and a produce each name one
concrete kind, so parity is achieved with builders parameterized by
kind and emitted per kind (the existing MakesOffering /
DrawOnTheVessel pattern), plus identical physics and patterns.

## Produce sites (46 penguin, 15 orca)

Pattern split, from the full extraction:

- Penguin sites: occupations (witness/broker/warden/exile) placed at
  the firing target — war sites, offering sites, rich hubs, monster
  sites. placementRef = "$target" in nearly every site.
- Orca sites: family roles (scout/calf/bull/matriarch/elder) placed at
  own-culture anchors — "$shoal", pod homes, CultureRoute placement —
  or targeting the pod's own home-holding orca.

Parity action (phase: placement-ref parity): actor products home at an
own-culture anchor by the same pattern regardless of kind. Occupations
may still appear at events; their home base is an anchor.

## Kind-specific targets

Penguin-targeted generators where the gate is kind-arbitrary (an orca
could satisfy the same fiction): the_tome_is_read,
what_the_artifact_knows (holders of artifacts), a_hero_falls_to_the_
deep, war_takes_its_toll, ice_settles_accounts (war casualties),
legend_enters_the_canon, the_ice_takes_notice (renown),
the_crater_keeps_its_count, the_stone_remembers_the_walk.
Parity action (phase: shared machinery builders): emit these from
kind-parameterized builders for both actor kinds.

Kind-specific targets that are justified asymmetry (biome or family
fiction): claimed_waters_draw_bulls, pod_currents_launch_scouts,
elders_descend_to_the_god, vigils_at_the_current, the_polynya_never_
closes (maritime); the_dread_song_breaks_the_watch (warden-specific).

## Physics (from the problem register, to be equalized)

- Band: orca Enduring (4500) vs penguin Brief (2000). Action: both
  Standard (3500).
- Locality: orca baseMass 5 / radius 2 / influenceScale 0.03 /
  healthScale 0.04 / threat 0.6 / capacity 0.4 / support 0.3;
  penguin 2 / 1 / 0.02 / 0.02 / 0 / 0.2 / 0.2. Action: one shared
  actor locality at the midpoint: baseMass 3.5, radius 2,
  influenceScale 0.025, healthScale 0.03, threat 0.3, capacity 0.3,
  support 0.25, attractorRadius 2.
- Traits: orca has a regeneration trait (allies restore endurance) and
  a drain trait (rivalry burns endurance); penguin has only
  verb-weight traits. Action: shared builder emits the same
  regeneration + drain pair for both kinds against each kind's health
  attribute; behavior traits remain per kind (they reference per-kind
  verbs).

## Machinery

- Reproduction/succession: orca-only (matriarch calving, homed scout
  pairs). Action: generalize the builder over (actor kind, anchor
  kind, home relationship) and emit for penguins with settlement
  anchors.
- Offerings: already shared (MakesOffering). Grants: already shared
  (DrawOnTheVessel per kind). These are the pattern to follow.
- Desperation surges: per-culture, kind follows culture. No action.

## Justified asymmetries (keep)

Movement profiles (Wake vs Trail/Shore), physical placement biome
(Maritime vs Land), semantic terrain profiles, health attribute names
(Endurance vs Vitality), subtype vocabularies as flavor. These differ
in expression, not in treatment.
