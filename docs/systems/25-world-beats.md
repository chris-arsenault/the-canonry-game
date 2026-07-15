# World Beats: the Ages of the World

The world-beats program (`Engine.Kernel/Programs/WorldBeats/`) applies the
chronicle beat treatment to the world itself. Where the Chronicle Director
recognizes stories among a cast, world beats recognize *eras* across the whole
simulation - and, like the director, the framework writes no world state. It
only modulates the rates of content that already exists.

## Three tracks

An age occupies one of three regime tracks; each track holds at most one open
age, and tracks are independent (semi-overlapping regimes):

- **Ice** - the physical regime of the land itself (the Thaw, the Still Ice,
  the Long Dark). The Ice track carries the domain's fallback age (no opening
  conditions, priority 0), which structurally guarantees at least one age is
  always open somewhere.
- **Peoples** - the social regime among the living kinds (the Age of Striving,
  the Age of Accords).
- **Memory** - the regime of the semantic world (the Age of Signs, the Age of
  Canons).

## Recognition, never scheduling

Each pulse (tuning `programs.world_beats.cadence_main_ticks`, default 25 main
ticks) the program samples macro facts from world state:

- ticks since start,
- active entities of a kind (settlements, canons, abilities, ...),
- living and culled relationships of a kind (wars, campaigns),
- total physical culture raw mass and its growth per pulse window,
- cumulative legendary chronicle resolutions (from the Chronicle Director).

An open age closes when **any** closing condition holds, after its authored
minimum span. A blank track opens the highest-priority age whose opening
conditions **all** hold and whose post-close cooldown has passed. Transitions
emit `WorldBeatChanged` primitive events (track, age id, opened/closed) into
telemetry (`world_beat_events`) and the UI top bar.

## Charges

While open, an age's charges multiply existing authored rates. The push is
idempotent, re-applied every pulse (so a snapshot reload self-heals), and each
target program owns its own O(1) modulation array:

- generator pulse weights (`AgePulseWeightMultipliers`),
- low-altitude verb weights (`AgeWeightMultiplier` per rule),
- gambit scores (`AgeScoreMultipliers`),
- the contest decay tide (`CultureFieldDecay.ScaleContestTide`).

A null/zero charge target charges the whole domain family.

## Campaigns: the first age payload

Boundary movement needs the attacker to project mass (see the Bundle A finding
in the tracker: contest decay sharpens fronts but stabilizes ownership). The
campaign loop is that projection:

1. `faction_marches_the_banners` (charged x4 by the Age of Striving) creates a
   `wages_campaign_against` relationship from a warring faction to its enemy.
2. While that edge lives, the minor-actor program's `CampaignLabor` emitter
   spawns worker cohorts anchored at the campaign *target's* position instead
   of home - a labor column at the front, pressing the attacker's culture in
   and clearing the defender's (continuous mass, not event pulses).
3. The campaign edge decays (a committed push, not a standing hatred); the
   `the_column_holds` generator and the `faction_feed_the_column` verb re-press
   it while the war justifies it. War's end starves the column.
4. Culled wars accumulate into the war-exhaustion fact that opens the Age of
   Accords - the campaigns themselves feed the next era's recognition.

## Authoring

Ages are domain content (`IceRemembersAges`), configured as
`SemanticDomainConfig.WorldBeatAges` via `WorldBeatAgeConfig` and compiled by
`DomainCompiler.WorldBeats` into primitive kernel data. Age ids live in the
generator id/symbol space (like chronicle archetypes), so age names resolve
through the ordinary symbol catalog. Thresholds are balance values, calibrated
against registered Monte Carlo evidence.
