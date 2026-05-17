# World Spine Exploration

Brainstorm. Not canon. Captures the design conversation about lowering
cognitive load over the simulation's 20-30 factions by introducing a
small static set of positions / categories / arrivals that entities
rotate into and out of.

## Problem

The simulation produces 20-30 factions at steady state. Every faction
is structurally equivalent: same kind, same handful of attributes,
same relationship vocabulary. The player has no way to grok the world
at a glance because there is no static thread to tie things to.

Adjacent observations:

- Cultures (Aurora Stack, Nightshelf, Wake-Singers) carry strong lore
  identity but have near-empty config (`CultureConfig` is `Id` +
  `DisplayName` and nothing else).
- Map influence is rich substrate but isn't projected into anything
  that the player can read as "who dominates what."
- Existing relationship exclusivity (`led_by`, `home_base`,
  `controlled_by`, `anchored`, `holds`) is already "slot-like" at the
  primitive level but invisible as a structural concept.
- The lore for named factions is wildly asymmetric: Council of
  Luminaries is a quorum; Icebound Exchange is a ledger; Midnight
  Claws is a syndicate with a deliberately occluded leader;
  Wake-Singers are pod-cult orca theology. A uniform per-faction
  scaffold (Sovereign + Heir + Capital) does not fit them.

## Faction count, verified from code

From `IceRemembersSeedBuilder.Entities.cs` and the generator catalog:

- 4 seeded factions at startup (Council of Luminaries, Icebound
  Exchange, Midnight Claws, Wake-Singers).
- 5 generators can create new factions, gated by causal limits.
- Long runs (~5000 ticks) produce 15-25 additional procedural
  factions, for a total of ~20-30. Factions are not destroyed.

This is the population the spine has to make legible.

## Exemplars (researched, with sources)

### EU4 Great Powers
8 fixed slots, any nation can hold. Ranking by composite "Great Power
score" (development + prestige + other). 5-year grace period prevents
flicker. Great Powers unlock unique diplomatic verbs (debt payoff,
influence nation, force-break alliance). UI: persistent ledger of the
8 + map markers + verb gating in the diplomacy screen. Rotation
broadcasts as banner notification. Source:
https://eu4.paradoxwikis.com/Great_power

### Victoria 3 Rank Tiers
6 categorical tiers (Great Power / Major Power / Regional / Minor /
Insignificant / Unrecognized). Threshold is Prestige + Recognition.
Tier itself gates diplomatic plays. Tier-based, not rank-based, so the
mental model is "Britain is GP this decade; Two Sicilies just dropped
to Major" rather than "rank 7 of 80." Source:
https://vic3.paradoxwikis.com/Rank

### Stellaris Galactic Council / Custodian / Galactic Emperor
3 council seats (extensible to 5), ranked by single composite
Diplomatic Weight (Fleet + Economy + Tech + Pops). Council seat grants
+20% Diplomatic Weight — positive feedback into the position.
Custodian is a single named position with term limits; can transition
to permanent Emperor via Senate resolution. UI: resolution voting bar
sizes each empire by Diplomatic Weight; "who matters" is one glance.
Source: https://stellaris.paradoxwikis.com/Galactic_Community

### Crusader Kings 3 Council + Court
5 named council slots (Chancellor / Marshal / Steward / Spymaster /
Court Chaplain), conditioned by government type (Feudal / Clan /
Tribal / Theocracy / Administrative). Royal Court DLC adds dozens of
court positions gated by rank. Each slot has a stat aptitude; the
mechanic forces characters to compete for slot fit. Source:
https://ck3.paradoxwikis.com/Council

### EU4 Institutions
7 named historical milestones (Feudalism, Renaissance, Colonialism,
Printing Press, Global Trade, Manufactories, Enlightenment) that
spawn in specific provinces, spread geographically, and can be
embraced by nations. A nation's identity becomes "I am
Renaissance + Printing Press but still pre-Colonialism." Not-yet
institutions impose a tech cost penalty. Source:
https://eu4.paradoxwikis.com/Institutions

### Civilization 6 Wonders / Great Works / Era Score
Wonders: ~50 single-occupant exclusive positions ("who has the
Pyramids"). Great Works: named slot vocabulary (Writing, Art, Music,
Artifact, Relic) that lives in specific buildings and can be traded.
Era Score: every civ classifies into Normal / Golden / Heroic / Dark
each era by accumulating Historic Moments. Sources:
https://civilization.fandom.com/wiki/Great_Work_(Civ6) and
https://www.thegamer.com/civilization-6-complete-guide-to-era-score-golden-dark-heroic-age/

### Endless Legend Empire Summary
Asymmetric factions, uniform outer summary template. The legibility
scaffolding lives in the *common surface*: empire plan (4-axis stance
each season), assimilated minor factions roster, the empire summary
panel. Internals can differ wildly; the player reads every faction
through the same shell. Source:
https://wiki.hoodedhorse.com/ENDLESS_Legend_2/Beginner's_Guide

### Cross-cutting patterns
1. Fixed-cardinality slot set (3-10 items).
2. Composite scalar for slot rotation.
3. Hysteresis to prevent flicker.
4. Uniform outer template over heterogeneous internals.
5. Stack distinguished singletons on the coarse tier.

## Proposed shape — Three Stacked Spines

Three abstractions at three cognitive scales. Each is a pure read-side
projection over the existing simulation. None of them modifies the
kernel, the gambit/generator runtime, or the relationship catalog.

### Tier 1: The Eminences

Approximately 7 named positions, each held by at most one faction,
rotation by position-specific scalar. Position names are lore-shaped:
each is the dominant cultural ideal of one of the three cultures, so
that "who currently holds The Veil" becomes interesting whether the
holder is Nightshelf (expected) or someone else (drama).

First-draft positions and scalars:

| Position | Resolved By | Lore Origin |
| --- | --- | --- |
| Wakeful Throne | sum of map influence | Aurora witnessed legitimacy |
| Reckoning | total `trade_route` strength | Icebound Exchange's counted obligation |
| Veil | count of `claimed_by` POIs + sealed artifacts | Nightshelf concealment |
| Choir | semantic-plane cultural reach | Wake-Singer doctrine spread |
| Witnessed Court | recent gambit success count + active alliances | Council legitimacy by visibility |
| Penitent | inverse cohesion + recent losses | The visibly diminishing |
| Forsaken | vacant `led_by` | The headless |

Hysteresis: a challenger needs ~15% margin over current holder to
displace, or use Vic3-style "within N% of #1" threshold. Open question.
Vacancy is allowed for some positions (Witnessed Court could be empty
if no faction qualifies).

### Tier 2: Standing

Every active faction lives in one of three tiers:

- Eminent — top ~5 by composite Standing scalar
- Named — next ~5-8
- Lesser — everyone else, collapsed under "+N lesser factions"

The cardinality is the entire point — EU4 ledger logic. Player tracks
5 in detail, recognizes another 5-8 by name, ignores the rest unless
something promotes them.

### Tier 3: Arrivals

A fixed list of ~10 world-state milestones that have either *arrived*
(monotonic, irreversible) or are *not yet*. Drawn from existing
generator outputs, framed as one-shot global events.

First-draft list:
- First Cleansing
- First Memorial
- First Conduit
- First Sanctuary
- First Named War
- First Slaying
- The Dread Awakens (monster broken its ward)
- The Hollow Bell (cleansed→memorialized→unwarded chain)
- The Choir Heard (one ability practiced across all three cultures)
- The Codex Sealed (tome anchored to a sanctuary)

Each Arrival:
- Detected by a one-shot scanner over the event stream
- Once observed, never reverts
- Optionally gates future generator/gambit eligibility
- Generates a permanent chronicle entry

## The unresolved problem: weight

The user's critique of this draft: *"these are interesting ideas, but
none of them carry any weight. they're meaningless designations."*

This is the load-bearing critique. Naming a faction "The Wakeful
Throne" is decorative until holding it actually *does something*. The
spine needs mechanical consequence to be more than a fancy ranking.

Possible directions to make the spine matter (not yet evaluated):

- Position-holders gain a faction-wide modifier (Stellaris Council
  seat → +20% Diplomatic Weight). Holding a position should make
  holding it easier, creating positive feedback that rewards
  dominance and makes losing the position consequential.
- Position-holders attract specific gambits/generators against them
  (assassinate the Sovereign of The Wakeful Throne; raid The
  Reckoning's trade routes). The position becomes a target.
- Arrivals gate eligibility of named gambits/generators. Pre-Cleansing
  there are no memorial generators; pre-Named-War there are no
  treaty gambits.
- Standing tier gates verb availability the way Vic3 tiers gate
  diplomatic plays.
- The player's eventual card-deck eligibility could be tied to
  spine state (cards become eligible only when certain positions
  are held by certain cultures, or after certain Arrivals).

The shared shape of all of these: the spine has weight when the
*simulation reads* it, not just when the *UI displays* it. Until the
simulation reads it, it's a leaderboard.

## Open decisions before any implementation

1. Vocabulary for the Eminences — first draft above is provisional.
2. Standing tier count (3 vs 5).
3. Arrivals list — author all 10 now, or start with 5 known-good
   ones and let the others emerge from playtest.
4. Hysteresis model — relative margin or absolute scalar units.
5. UI scope — replace the current registry browser, or stack on
   top of it.
6. **Weight**: which of the "make the spine matter" directions above
   to commit to. This is the load-bearing decision; the rest is
   plumbing.

## Mapping back to user's framing

- *"static thread to tie things to"* → the 7 + 3 + 10 = 20 named
  handles never change shape.
- *"emergent behavior while having more of a spine"* → kernel and
  simulation are unchanged; the spine is a read-side abstraction.
- *"culture and influence layers"* → Eminence names are culture-
  shaped; Eminence scalars are influence-derived.
- *"designing this after a real nation"* → wrong scale at the faction
  level (the lore says factions are NOT homogeneous polities); right
  scale at the world level. The Berg is the nation; factions are its
  great houses; Arrivals are its constitutional moments.
- *"static slots that things fall into and out of"* → Eminences are
  exactly that. Arrivals are a monotonic variant (fall in, never
  fall out).

## Status

Not in flight. Captured here for later reference after the weight
question is resolved.
