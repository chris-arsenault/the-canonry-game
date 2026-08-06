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

### Catalog recut plan (proposed 2026-08-06)

The spine is the Three Fires (lore-bible section 7), not a neutral
tech tree. Low tiers are mundane arts; from tier 2 upward each
culture's spokes climb its own fire, and the tier-4 ideas are the
fires at full cost:

- **Aurora-Light** (Aurora; perception, preservation, pattern; costs
  clarity of self) - carries the Remembrance and Cold spokes:
  ice-reading gates (canon rites, testimony, speaking with the dead),
  ice-seeing at distance, and at the top, crystallizing memory into
  physical form. Answers questions, never solves problems - so its
  gates unlock knowing content (chronicles, canons, wardings), not
  force.
- **Captured Fire** (Nightshelf; transformation, force, unmaking;
  costs flesh) - carries the Craft and Deep spokes: core-cracking
  gates forge work, gallery-delving (the vertical forging gambits),
  heated weapons, and at the top, unmaking matter. Movement gates
  here are what forge-masters cut: under-ice ways, tunnel networks.
- **Pressure-Depth** (Orca; force, communion, blood; costs interior
  distance; orca-only by physics, amplified near the Dreaming Tooth)
  - carries the Song and Hunt spokes: subsonic calls at vast range,
  pack coordination, communion with the Dead God (devotion content),
  and at the top, force without contact. Its gates compose the tech
  claim with the orca kind - the physics limit is the fiction's own
  kind-gate.

The fires do not combine; each culture can still claim another's
sites (claims are co-holdable) but the gated content composes with
kind and existing custody preconditions, so reach without the people
to use it yields little - which is the lore's interference, expressed
in gates.

Grounding (lore-bible section 4): the world's central fact is the
ice-memory - a medium that records everything done on or in the ice,
read by trained readers, holding the testimony of the dead and, in the
deep strata, impressions older than penguins. The three peoples relate
to it oppositely: Aurora accumulate identity in it (ice-testimony is
court evidence), Nightshelf treat it as surveillance to hide from
(incomplete names, deeds done where the ice cannot see), Orca deny it
outright (they live in water; the ice does not record them). Techs are
ideas claimed on the semantic map - so the catalog should be arts of
reading, hiding from, and defying the ice-memory, surviving the Long
Dark, and touching the deep ice, not generic survival tools. Several
current tech ids already fit (record_carving, echo_hunting,
trench_charting); the generic ones (improved_ice_fishing, krill_traps,
sled_building, reinforced_hulls) should be renamed into the world's
own vocabulary during authoring - renames touch the seed catalog order
validation, so they land with the recut, not after.

Each spoke becomes one coherent gate family; entries name the gate
type against the implemented primitives. "Unlock" = CultureHoldsTech
precondition on an existing gambit, or RequiredTech on an existing
generator. Multiplier survivors are >= 2x or <= 0.5x. Family framings,
re-grounded: Remembrance is the reading arts (testimony, speaking with
the dead, canon law kept alive by readers); Deep is the descent toward
the oldest strata and what the Nightshelf build where the ice cannot
see (galleries, under-ice ways); Song is the Orca's iceless
coordination - water-borne calls that need no memory but their own;
Craft is what is raised upon the ice (block, spire, hull); Hunt is
provisioning through the Long Dark; Cold is enduring what the Berg
does to the unprepared. The tier-4 techs should read as touching the
Before Times or the deep ice's patient attention - endgame ideas the
ice itself half-resists.

- **Deep — the under-ice family (movement profiles):**
  breath_holding_dives t0 opens shallow under-ice crossings;
  trench_charting t2 opens trench terrain; pressure_diving t3 prices
  deep terrain near-flat (the "air travel" of this world);
  abyssal_mapping t4 flattens everything beneath the ice. deep_netting
  t1 unlocks the deep-forage generator.
- **Craft — the built-world family:** bone_tools t0 keeps a x2 craft
  verb-weight; ice_block_building t1 unlocks the settlement deepen
  stage rung; sled_building t2 is a penguin overland movement profile;
  ice_ship_hulls t3 unlocks the transport/carrier generator;
  reinforced_hulls t4 is a transport profile crossing pressure ridges.
- **Hunt — the provisioning family:** ice_fishing t0 keeps a x2 forage
  multiplier; improved_ice_fishing t1 widens the hunt generator from
  one subkind to all; krill_traps t2 is a x2 carrying-capacity step;
  pack_hunting t3 unlocks the coordinated raid/hunt gambit (and is the
  future seat of attacker mass projection).
- **Song — the coordination family:** call_signals t0 widens a social
  generator's target subkinds; echo_hunting t1 converts from score
  multiplier to unlocking OrcaClaimsHuntingGround; long_distance_calls
  t2 unlocks FactionParleysWithThePod at range; pod_coordination t3
  unlocks the campaign march.
- **Remembrance — the memory family:** record_carving t0 unlocks
  ReadsTheIce; rookery_schools t1 unlocks PassesTheTeaching (teaching
  lineages become an earned institution); lineage_charts t2 widens the
  succession generator's subtypes; stone_libraries t3 unlocks the
  canon-founding generator.
- **Cold — the endurance family:** windbreaks t0 keeps a x2
  decay-persistence multiplier; aurora_light_spectacles t1 unlocks the
  festival/offering generator; thaw_forecasting t2 sharpens to x0.5
  thaw threshold; ice_cellars t3 keeps decay-tide resistance promoted
  to x0.5; storm_prediction t4 keeps its x0.5 reclaim threshold.

All 92 existing x1.1-1.3 grants are deleted in the recut; the catalog
above is each tech's entire effect. Follow-up primitive if wanted
later: terrain-placement gates (founding on wastes/scar terrain) for a
reclamation tech - not required for this recut. Gating existing
content (ReadsTheIce, PassesTheTeaching, parley, march, stage rungs)
makes those behaviors earned rather than universal - each needs a
beneficiary check against current balance before authoring.

Kernel primitives (implemented 2026-08-03). The unlock pattern already
existed once: grant gambits (`IceRemembersGambits.Grants.cs`) are
default-absent gambits gated by a relationship precondition
(holds-artifact / practices-ability at minimum strength), revoked when
the relationship lapses. Knowledge gates generalize that pattern from
entity custody to culture claims. Techs are referenced by stable id:
`GameplayDefinition.KnowledgeTechIds` declares the ordered catalog
(order = kernel site index; the startup seed installer validates its
site list against it), and the compiler resolves ids through
`DomainIdRegistry.KnowledgeTechIdOf`.

- **Gambit unlock** — `CultureHoldsTech(techId, endpoint)` precondition
  (`Preconditions.cs`); evaluated against the knowledge runtime's claim
  bits for the endpoint entity's culture; composable with artifact
  grants (a rite can require both the knowledge and the relic).
- **Generator unlock / target widening** —
  `SemanticGeneratorTarget.RequiredTech` (targets pass only while their
  culture claims the tech) and `.TechWidensSubtypes` (claim bypasses
  the subtype filter: one subkind opens to all).
- **Movement gates** — one primitive covers both examples:
  `MovementProfileAssignmentConfig.TechRules` switches a kind's
  effective movement profile while the entity's culture holds the tech
  (applied after trait rules — an earned capability wins). Mountain
  access is an override profile that prices mountains finite; air mode
  is a profile that prices terrain flat. Consistent with
  roads-as-speed / transports-as-carriers; no per-unit speed creep.

All gates share `KnowledgeProgramRuntime.CultureClaimsTech` (1-based
tech id, 0 never passes); claims recompute from the culture field every
scheduled run, so gate revocation on lost ground comes free.

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
