# Cultural Expansion and Knowledge

**Status:** implemented through authored claims and mechanical grants; the
grant model is being redesigned (owner decision 2026-08-03, see Grant
philosophy below). The system stays. Player selection, procedural
technologies, and knowledge stockpiles remain out of scope.

## Goal

Knowledge makes cultural expansion legible and consequential without becoming a
Civ-style research queue. Named technologies occupy authored sites on the
semantic map. A culture knows a technology while its semantic culture field
holds that site, and the technology modifies existing simulation mechanics for
that culture.

The radial layout supplies progression shape:

- radius band is tier;
- one of six angular spokes is discipline;
- outward cultural expansion reaches later technologies;
- different cultural footprints produce different sets of claims;
- retreat can lose claims and their grants.

The 27 current Ice Remembers technologies and their exact grants are documented
in [Knowledge and Technology](../systems/27-knowledge.md).

## Invariants

1. Culture consolidates at strongholds; knowledge does not make mass march
   outward by itself.
2. Culture remains contested on physical and semantic planes.
3. A claim is culture-field state, not a stockpile or resource balance.
4. Technologies are a fixed, growable authored catalog, not procedural output.
5. Tier controls semantic placement difficulty only. It does not implicitly
   scale a grant.
6. Grants modify existing verbs, generators, gambits, movement, relationships,
   map signals, culture fields, minor actors, and frontier rules. They do not
   form a parallel effects engine.
7. Kernel configuration and execution use primitive codes and fixed-capacity
   storage; authored strings are resolved before bootstrap installation.
8. Knowledge can be lost, but ordinary field dynamics should make forgetting
   exceptional rather than constant churn.
9. Claims are co-holdable. One culture claiming a site does not exclude another.
10. Player agency over knowledge is deferred; the current system is emergent.

## Grant philosophy (owner decision, 2026-08-03)

The 2026-08-03 audit found the original grant model unimpactful: 80 of 92
grants were multipliers in the x1.12-1.30 band stacked on mechanics that
already carry age, trait, and chronicle modifiers; generator fire weight
additionally averaged the modifier across eligible targets; the evidence
run's generator fires matched the no-grants baseline exactly. Decision:
keep the system, replace the grant model.

1. **Most technologies are enablement gates**, not modifiers. A gate turns
   something on for the claiming culture that simply does not exist for
   others. The owner's examples — terrain-class travel, air movement,
   gambit/generator unlocks, subkind target widening — illustrate the
   kind, not a defined list.
2. **Multipliers survive only when substantial** — roughly 2x (or the
   reduction equivalent, roughly 0.5x, like the existing storm-prediction
   reclaim-threshold grant). Everything in the x1.1-1.3 band is cut or
   promoted.
3. Tier placement remains reach-only (invariant 5): a gate's power comes
   from what it enables, not from its tier.

### Gate design space

A good gate does one of two things: it **opens a loop that did not
exist**, or it **answers a measured register problem for whoever earns
it**. Families grounded in mechanics that already exist:

- **Opening space** — terrain-class access; founding on wastes/war-scar
  terrain (reclamation: land -> births -> mass, and a reason to want
  wars to end); beyond-startup-rim operation (colonization; the
  exploration flags already measure rim confinement); forging
  depth/height limits as earned tiers (ties to the subsurface culture
  overhaul); travel-structure types and road-building on new terrain
  classes.
- **Opening structure** — relationship kinds per culture (teaching
  lineages via taught_by, devotion channels via consigned_to, new
  culture-anchor kinds that change where a culture banks influence);
  additional culture slots as governance forms; subtype creation
  unlocks (the production-side mirror of target widening).
- **Opening survival modes** — shelter via relationship support where
  territorial mass is absent (a direct answer to the
  life-spent-on-foreign-ground death-rate problem, natural for
  minorities); substantial carrying-capacity steps.
- **Opening pressure** — attacker mass projection for moving fronts as
  an earned war capability rather than a global mechanic.

The six discipline spokes should map to gate families so a culture's
expansion direction shapes what kind of civilization it becomes.
Every authored gate passes the beneficiary check.

Mechanics mapping (owning seams, smallest first). The unlock pattern
already exists once: grant gambits
(`IceRemembersGambits.Grants.cs`) are default-absent gambits gated by a
relationship precondition (holds-artifact / practices-ability at
minimum strength), revoked when the relationship lapses. Knowledge
gates generalize that pattern from entity custody to culture claims:

- **Gambit unlock** — a new precondition kind, culture-holds-tech,
  evaluated against the knowledge runtime for the actor's culture; same
  authoring vocabulary as artifact grants, and composable with them
  (a rite can require both the knowledge and the relic).
- **Generator unlock / target widening** — the same condition at the
  generator target gates (`TargetPrimaryGatesPass` already consults
  knowledge per target for fire weight); fixed-culture generators gate
  on the fixed culture's claim. Widening relaxes the subtype filter
  when the relevant culture holds the tech.
- **Terrain-class movement gate** — movement's `CanEnter` gains a
  (culture, terrain-class) permission consulting knowledge; today
  enterability is per-kind movement profile with no culture term.
- **Air movement mode** — the largest piece: a per-culture movement
  profile override (tech held switches the entity's effective profile),
  consistent with roads-as-speed / transports-as-carriers; no per-unit
  speed creep.

Tier reach today (kindgen-r20 seed 9100, 30k ticks): Aurora holds 3
tier-3 sites, Nightshelf 5, Wake-Singers top out at tier 2, tier 4
unreached. Gates at tiers 0-3 activate within a standard horizon; tier 4
is endgame content on real play lengths (4x-50x the 30k bootstrap).

## Natural wonders (planned physical parallel)

A future parallel system on the physical plane: natural wonders are
discovered and owned, granting benefits through the same grant engine.
The symmetric shape: wonder sites are static authored/worldgen-placed
physical entities; a culture owns a wonder while its physical culture
field holds the site (same radius/band claim test as tech sites, plane
switched) — discovery falls out of exploration and the growing map,
ownership out of territorial mass, and loss out of retreat, exactly as
with semantic tech sites. Candidate tie-in: the deferred stable named
geological feature ids (ranges, peaks, craters) as wonder anchors. One
grant engine, two planes; wonders prefer the same gate-style benefits.

## Expansion substrate

The semantic map grows where culture presses its frontier. Entity-creating
generators use a deliberate mix of near and frontier placement: founded static
place-anchors can extend reach, while retinues, restoration bundles, conflict
entities, and mobile productions remain attached to their causal locations.
This preserves a dense cultural heart while allowing the anchor network to
advance.

The last accepted expansion evidence is MC run `frontier-balanced-2`: the
semantic frontier reached ring 29 and continued climbing, with roughly 79% of
living entities in the heart and 21% at the frontier. Knowledge does not replace
that expansion mechanism; it rewards the resulting spatial reach.

## Claim model

`IceRemembersKnowledge.Techs` authors each technology's stable id, display name,
tier, discipline, grant bundle, and explanatory text. Startup places each site
at the midpoint of its tier band along its discipline spoke. A culture claims a
site when any known semantic cell within radius 2 holds at least claimed-band
raw culture mass.

`KnowledgeProgramRuntime` recomputes claims every ten ticks. Claims and
modifiers are derived state over the serialized culture field:

- current claims are recalculated rather than serialized;
- all modifiers are cleared and rebuilt on each scheduled run;
- losing a claim removes its grants on that run;
- multiple held grants multiply;
- a target-specific query multiplies the culture-global and target-specific
  entries for that domain.

The runtime also maintains claimed count, highest tier, and per-site claim bits
for observability. See [Knowledge and Technology](../systems/27-knowledge.md)
for the owning layers and consumer semantics.

## Evidence

The pre-repair comparison run is registered as
`knowledge-real-grants-baseline` (1 run, seed 4200, 15,000 ticks). It completed
without map degeneration and produced 130 generator fires. Final claims were
9 / 18 / 7 / 0 for cultures 1–4; final semantic mass was approximately
92.5k / 453.6k / 59.5k / 0. This run represents the removed binary
Production/Persistence implementation and is retained only as a comparison
baseline.

Post-repair evidence used the registry-owned MC workflow and reported both
aggregate claim progression and the named rows obtained by joining
`knowledge_claim_snapshots` to `knowledge_site_symbols` on run and site index
(those telemetry lanes were deleted 2026-08-03 — no tool read them). Grant
effects should be evaluated on their owning mechanics rather than inferred
from claim count alone.

The matching post-repair run is `knowledge-real-grants-postrepair` (same seed,
ticks, and run count). It also completed with 130 generator fires and no map
degeneration. The export contains all 27 technology symbols, 14,193 named claim
snapshot rows, and 41 distinct culture/site pairs. Final claims were 14 / 15 / 4
for cultures 1–3, with different real sets: culture 1 uniquely held Windbreaks
between the two leaders, culture 2 held Rookery Schools and Improved Ice
Fishing, and culture 3 ended with Record-Carving, Bone Tools, Ice Fishing, and
Windbreaks. Final semantic mass was approximately 136.8k / 217.2k / 27.0k and
physical mass 330.4k / 65.1k / 70.2k.

This is a single-seed integration comparison, not a balance conclusion. The run
reached tier 2 only. Tier 3–4 grants are installed and covered by synthetic
kernel routing/claim-loss contracts, but their natural activation needs a
longer or broader evidence horizon.

## Deferred work

- player-facing selection or intervention;
- procedural technologies or dynamically authored sites;
- knowledge resources, costs, or stockpiles;
- physical-frontier retuning and tier-reach tuning (tier 4 activation is
  an endgame-length question, not a 30k one);
- additional grant domains without a diagnosed existing mechanic to consume
  them — gates consume existing mechanics (movement, gambits, generators)
  and stay inside this rule.
