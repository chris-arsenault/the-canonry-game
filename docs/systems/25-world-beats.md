# World Beats: the Ages of the World

The world-beats program (`Engine.Kernel/Programs/WorldBeats/`) applies the
chronicle beat treatment to the world itself. Where the Chronicle Director
recognizes stories among a cast, world beats recognize *eras* across the whole
simulation - and, like the director, the framework writes no world state. It
only modulates the rates of content that already exists.

## Two tracks

A beat occupies one of two regime tracks; each holds at most one open beat.
Each track carries four **fallback** beats (loosely gated everyday moods, one
always holds the track) and four **hard** beats (rarely-triggered, consequential
eras that preempt the fallbacks).

- **World** - the Berg's own condition. The four fallbacks are the seasons as a
  fortune-cycle recognized from culture-mass growth (the Thaw when it surges,
  the Endless Day when steady, the Freeze when slowing, the Long Dark when it
  stalls); the hard beats are physical/cosmic breaks (the Heartbeat Quickens,
  the Before Times Surface, the Great Thaw, the Great Freeze).
- **Peoples** - what the three cultures are doing. The fallbacks are everyday
  activity (the Witnessed Life, the Hunt, the Long Trade, the Deepening Craft);
  the hard beats are the epochal eras from recorded history (the Faction Wars,
  the Orca Incursion, the Frozen Peace, the Clever Ice Age).

Beats are recurring moods, not a timeline: openings are loose and closings are
hysteretic, so they come and go and recur, and each amplifies the very activity
it is named for (the Hunt raises hunt selection and success; the Thaw quickens
travel and opens the map). The no-condition default fallback on each track
(the Endless Day, the Witnessed Life) guarantees one beat is always active,
preempted by a higher-priority mood or era when its conditions hold. The
sixteen beats are named from the lore bible; see `IceRemembersAges`.

## Recognition, never scheduling

Each pulse (tuning `programs.world_beats.cadence_main_ticks`, default 25 main
ticks) the program samples macro facts from world state:

- ticks since start,
- active entities of a kind (settlements, canons, abilities, ...),
- living and culled relationships of a kind (wars, campaigns),
- total physical culture raw mass and its growth per pulse window,
- cumulative legendary chronicle resolutions (from the Chronicle Director).

Living and ended relationship counts are updated explicitly when relationships
are created or culled. The pulse does not scan relationship storage, and ended
edges do not remain resident merely to support age recognition.

An open age closes when **any** closing condition holds, after its authored
minimum span. Each pulse the highest-priority age whose opening conditions
**all** hold (and whose post-close cooldown has passed) takes each track: it
fills a blank track, and it **preempts** a lower-priority incumbent that has
met its minimum span - so a low-priority fallback like the Still Ice never
traps the track and blocks a real age. Transitions emit `WorldBeatChanged`
primitive events (track, age id, opened/closed) into telemetry
(`world_beat_events`), the UI top bar, and the ages strip/panel.

## Charges

While open, an age's charges multiply existing authored rates. The push is
idempotent, re-applied every pulse (so a snapshot reload self-heals), and each
target program owns its own O(1) modulation array:

- generator pulse weights (`AgePulseWeightMultipliers`),
- low-altitude verb weights (`AgeWeightMultiplier` per rule),
- gambit scores (`AgeScoreMultipliers`),
- the contest decay tide (`CultureFieldDecay.ScaleContestTide`), contested cells only,
- the global decay tide (`CultureFieldDecay.ScaleGlobalTide`), every cell - the
  Long Dark and Great Freeze charge it so untended frontier cools and the map recedes,
- gambit success (`Gambits.ApplyAgeSuccessMultiplier`), per gambit - the Hunt and
  the Faction Wars raise not just selection but the odds an act lands,
- movement step rate (`Movement.ScaleAgeStepRateMultiplier`), global - the Thaw and
  the Great Thaw quicken travel; the Freeze and the dark slow it.

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
