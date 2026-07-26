# Culture Balance Mechanisms — Options And Judgments

Date: 2026-07-11. Context: the culture power ledgers
(`docs/systems/culture.md`) exposed that Aurora leads every axis. Evidence
run `zb-forging-ledgers-2x25k-r2-20260711`: generator entity outputs split
341 Aurora / 46 Nightshelf / 54 Wake-Singers because outputs inherit the
target's culture and targets are drawn from the living population — a
rich-get-richer loop. This document records the mechanisms considered for
breaking that loop, which were adopted, and which are deferred with enough
design detail to pick up later.

## Rejected: inverse-population generator weighting

Weight generator target scoring by the inverse of the target culture's share
of living population. Rejected because it is a controller on the outcome
variable itself: it watches the scoreboard and steers toward equality
regardless of world events. Player disruptions get silently compensated
away, all cultures share one hidden mechanic under different skins, and the
ledgers become cosmetic because the system resists divergence. Do not
reintroduce this, including in disguised forms (scarcity-gated revival
generators are the same homeostat in miniature).

## Adopted (Nightshelf round, 2026-07)

- **Parasitic cross-cultural generators.** Authored generators where the
  activity is Aurora's but the beneficiary is Nightshelf: smuggling cells
  under thriving trade hubs, war profiteers, counter-archives, secrets from
  Aurora's dead. Aurora's runaway growth becomes Nightshelf's food supply —
  negative feedback that emerges from theme rather than a controller.
  Requires produced entities to take an authored culture rather than
  inheriting the target's.
- **Timeline generators (cadence-based).** A per-generator cadence (every
  ~N ticks, jittered, world-state preconditions) outside the
  population-proportional fire-window pool. Guarantees each culture a minimum
  authored drumbeat without equalizing outcomes. First instance: the
  Nightshelf Deep Conclave. Cadence, not scarcity gating.
- **Per-culture plane bias.** Culture-authored per-plane influence
  production multipliers (Nightshelf semantic-heavy, Aurora mildly
  physical-heavy) so a small culture can produce outsized output on its own
  axis.
- **Seed custody parity.** Nightshelf starts with the assets its identity
  implies — held artifacts, practiced abilities, broker holders, semantic
  base — rather than more settlements.

## Deferred: field-fed spontaneous generation (Option C)

Entities emerge from culture mass, with each culture's spawn substrate being
its own power axis: Aurora spawns where witnessed surface mass (`z >= 0`) is
high; Nightshelf spawns where semantic mass and hidden tunnel mass (`z < 0`)
are high; Wake-Singers (once their axis is settled) from claim corridors.
This makes the ledgers and the z-axis load-bearing — demography feeds the
axes and the axes feed demography — and turns the existing bless-cell
intervention into a fertility lever. Extinction stays possible: no mass, no
spawns.

Design notes for pickup:

- Implement as a generator targeting mode (target = a culture field cell
  selected by mass band, not an entity), not a separate spawn system, so
  provenance/chronicle and causal limits keep working.
- Damping is the hard part: mass → entities → mass is positive feedback.
  Candidate dampers: spawn consumes raw mass from the source cell (mass as
  a spent resource), per-culture spawn budgets per epoch, and the existing
  field decay.
- Depends on each culture actually owning an axis first; amplifies whoever
  already leads. Do not ship before asymmetric identity is working.
- Natural staging: alongside Z-C (semantic z), when Nightshelf's deep
  strata become spawn substrate.

## Deferred: local carrying capacity (Option D)

Density-dependent lifecycle pressure: upkeep drain rises with same-culture
crowding relative to supporting culture mass, so territory determines how
many entities a culture can sustain. Ecological negative feedback on the
winner without subsidizing anyone; expansion visibly raises capacity.
Deferred because it only slows the leader — it gives a trailing culture
nothing — and a global growth damper risks a sluggish world. Reach for it
only if Aurora's absolute growth remains a problem after the adopted
mechanisms land, and tune it as spatial pressure (crowding vs local mass),
never as a global population cap.

## Deferred: auxiliary culture fields (fervor / terror / dread)

A generalized per-culture, per-plane named field — deposited by declared
activities, diffusing and decaying like culture mass, readable by placement,
generator targeting, and verb scoring. The spatial, proximity-coupled kind of
system this project likes (not a stock/flow reservoir), and structurally the
same mechanism as the deferred field-fed spontaneous generation with
emotional semantics. This is the candidate answer to the Wake-Singers loop
bandwidth ceiling: their devotion economy runs through a handful of
relationship edges while Nightshelf's runs through entities and Aurora's
through landmass. Build only after the mundane Wake fixes are measured — if
Wake-Singers still cannot hold their axis with a functioning loop, that is
the evidence this round needs. The largest engine addition since Z-B;
deserves its own design pass (diffusion cadence, capacity bounds, consumer
wiring).

## Deferred: the cultural inversion matrix (six directed pairs)

Cross-cultural feeding channels per ordered culture pair. Status: NS<-A
exists (shadow economy), WS<-A arrives with predation, WS<-NS partially
exists (relic consignment vs custody). Sketched next: A<-WS (martyrs,
memorials, and wardens rise where predation bites — Aurora converts
suffering into Witness). A<-NS and NS<-WS wait for a diagnosed imbalance.
Do not fill the matrix on principle: every generator dilutes the shared
fire-window pool (observed), and each channel is a tuning surface. Watch
`FiresPerWindow` and fire-window width as the roster grows.

## Mandated pass: de-agency-centrify the new systems

The predation/offering/escalation designs are agency-centric (penguins,
orcas, factions act; other kinds are acted upon). Before the culture rounds
are called done, a dedicated pass must verify that all entity kinds —
artifact, region, occurrence, transport, ability, monster, point of
interest — participate in the new systems (offerings, predation, tags,
timelines) with comparable richness, as sources of behavior and not only as
targets. Logged 2026-07-12 at user direction; not optional.

## Open question: the Blood ledger

Wake-Singers balance is out of scope for the Nightshelf round, and the Blood
metric itself (claims + war strength shares) is not confirmed as the right
measure of predatory power. Revisit the definition before building
Wake-Singers mechanisms against it; kill/predation attribution via
provenance-based cause-of-death remains the likely upgrade.
