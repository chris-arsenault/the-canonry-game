# Narrative Quality Remediation — Tracking

Approved plan from the 2026-07-12 narrative world-building quality pass.
Analysis evidence: `docs/analysis/narrative-quality-pass-20260712.md`, runs
`narrative-quality-3x25k-20260712` and `narrative-quality-1x100k-20260712`.

Goal: completed runs should read as believable cause-and-effect world history —
connected entities and events, wars with formation and duration, artifacts with
ownership tales, and protagonist kinds that can carry multi-event stories —
instead of disconnected churn.

## Established Context

### Time scale (canonical calibration)

- `MinorActorMainTickRatio = 5` (worker introduction): all authored durations
  are **main ticks**; raw ticks are 5x denser. The conversion is applied
  uniformly (upkeep age, gambit cooldowns, verb commitments, tenure). The 5x
  had the intended effect on lifespan — nothing is broken there.
- What was never revisited: authored band values against horizon growth
  (1,500-tick evidence era → 25k bootstrap → 100k+ sessions).
- Canonical mapping: **250 raw ticks ≈ 1 year** (50 main ticks/year). A full
  simulation ≈ 10,000 years ≈ 2.5M raw ticks. 25k ticks ≈ 100 years ≈ the
  bootstrap (1-3 generations).
- Tonal reference: **Malazan Book of the Fallen** — ordinary mortals live
  decades; some main characters a few hundred years; ancients ~100k years.
  The world must feel historical and deep, with a rare legendary long-lived
  tier rising out of real generational turnover — not uniform longevity.

### Lifespan targets

| Kind | Band (main ticks) | Observed median (raw) | Years | Note |
|---|---|---|---|---|
| Penguin | 2,000 | ~11k | ~45 | ordinary mortals |
| Orca | 3,500 | ~20k | ~80 | long-lived mortals |
| Monster | 4,500 | ~26k | ~100 | hoard renewal → centuries |
| Faction | 10,000 | ~57k | ~230 | institutions |
| Settlement | 20,000 | ~114k | ~450 | renewal → millennia |
| Artifact | exempt from age drain | — | ageless | condition wear; ends by deed |
| WorldAnchor / ancients | exempt | — | ~100k y | the Ice, the dead god |

Observed ≈ band × 5 (main→raw) × ~1.14 (healing surplus). Legendary lifespans
beyond these medians come from **active renewal** (R2), never from bands.

### Design constraints

- Owning layers: everything except R5 is authored domain (`src/IceRemembers*`)
  plus small `Engine.Domain`/`Engine.Gameplay` vocabulary additions and
  observability fixes. R5 is the one deliberate `Engine.Kernel` extension.
- Microkernel discipline holds throughout: fixed capacity, primitive events,
  no runtime strings, no hot-path allocations.
- No stock/flow reservoir attributes; accumulation lives in relationship
  strength, tags, or culture-style mass.
- Legendary sustainment must be **relational and in-universe** (artifacts,
  abilities, story participation) — never a bolted-on "keep alive" flag, and
  not framed as passive "the ice memorializes you".
- Renewal is **non-passive**: granted gambits the entity must choose to use,
  visible as biography beats.
- Protect what works: spatial war grounding, physical artifact transport,
  canon births from war exhaustion (the template mechanism), bursty pacing,
  above-baseline retaliation.
- Protagonist concentration should emerge from grants/Director feedback, not
  be forced by tuning.
- At the 2.5M-tick full-game ambition, unbounded edge accretion is
  load-bearing: the R4 decay fixes are required, not cosmetic.

## Rounds

Each round: implement → `make test` → registered MC batch with the axis
queries from the analysis pass → commit with cited run id. No `--no-build`.

### R1 — Time recalibration

- Lifecycle band values and per-kind assignments per the table above
  (`src/IceRemembers/Entities/IceRemembersEntityDefinition.cs` + per-kind
  entity files).
- Artifacts exempt from age drain (ageless; `condition` wear remains the
  physical-state channel; destruction only by deliberate acts).
- Sweep other authored durations for year-sanity (e.g. the 630-tick
  trade-route life). War cooldowns move in R3.
- Telemetry side-fixes: register stable names for death reason codes (symbol
  kind 21 currently renders null); emit relationship cull telemetry when an
  entity goes historical, if cleanly scoped — otherwise document the gap.
- Record the time calibration in `docs/systems/simulation-principles.md`.
- Update/delete low-value tests that mirror old band constants.
- Verify: lifespan distributions in years; penguin median ~40-50 y; artifacts
  no longer die of upkeep.

### R2 — Gambit grants (active legendary renewal)

- New precondition `SelfHasRelationshipToEntityOfSubtype(relationshipKind,
  targetKind, targetSubtype, minStrength)` in `Engine.Domain` preconditions +
  compilation (pattern-sibling of `SelfHasOutgoingRelationship`).
- Grants are gambits gated on holding/practicing a granting entity of a
  subtype. Losing the relationship revokes the gambit.
- Artifact grants (via `holds`, by artifact subtype), starter five:
  1. *Draw on the Vessel* — self health restoration (longevity).
  2. *Unmake the Binding* — dissolve one relationship the artifact's memory
     contains (a war, a corruption, a claim).
  3. *Call the Deep Current* — bearer long-range movement / travel structure.
  4. *Turn the Battle* — force a war-strength swing in a war the bearer's
     faction fights (first in-war battle event).
  5. *Bind What Was Loosed* — upgraded monster binding for bearers.
- Ability grants (via `practiced_by`, by ability subtype), starter five:
  1. *Winter's Patience* — self health restoration (longevity practice).
  2. *Mend Another* — restore a target's health (healers; other-extension).
  3. *Pass the Teaching* — create `taught_by`/`practiced_by` on a student
     (lineages; abilities spread through play, not generator fiat).
  4. *Read the Ice* — reveal/strengthen a semantic claim or canon bond.
  5. *Still the Waters* — suppress a threat signal / cool a war locally.
- Longevity grants have cooldowns + strength floors so renewal throughput —
  an in-universe activity — sets how far past the band an entity lives.
- Rare static longevity trait (~3-5%, 2-3x band) for born-different variance.
- Abilities stop being inert: this round is also "abilities do things now".
- Later extension (post-R5 backlog): grants from canons, settlements,
  occurrences.
- Verify: p90/median lifespan ≥ 2.5 for penguins/orcas, max ≥ 10x median,
  medians hold near R1 targets; renewal beats visible in biographies; grant
  gambit utilization sane across subtypes.

### R3 — War and alliance package

- Declaration preconditions: contested influence **and** grudge evidence
  (`rival_of` above threshold, or existing `betrayed_by`/raid edge).
- New cheap escalation rung: *Declare Rivalry* gambit (or rivalry drift verb)
  so war is the third beat of a chain, never the first.
- Peace precondition: `at_war_with` strength below ~0.3 — wars end when
  cooled/exhausted. Press the War / Let the War Cool / Exhaust the War become
  the campaign plot.
- During-war cost: `AttributeDeltaEffect` draining member health/cohesion
  while faction war edges are strong → wars kill named members (story
  deaths); factions genuinely want peace.
- Post-peace: truce-canon claim suppresses re-declaration between the pair
  while strong; breaking early is real betrayal.
- Sue-for-peace `betrayed_by` stamp scaled down / conditional on early
  capitulation so betrayal edges stay rare and meaningful.
- Alliances: *Swear the Cold Accord* (shared enemy → `allied_with`);
  *Honor the Accord* (join ally's war — creates blocs and meaningful refusal);
  faction maintenance verb on `allied_with` (tended alliances persist).
- Retune declare/peace cooldowns to multi-year wars: ordinary war median
  ~2-10 years (500-2,500 raw ticks); pressed generational wars possible.
- Verify: war duration distribution in years; wars-per-pair down; alliance
  count/duration up; ally-join-war events exist; internecine wars ~0;
  war-caused death share up.

### R4 — Memory, artifact conflict, newcomer anchoring

- `memorializes` generator targeting requires the honored entity be
  historical — memorials become consequences of death.
- Slow negative drift on `betrayed_by`/`rival_of` in maintenance config:
  grudges fade unless re-pressed (wars re-press them via R3 preconditions).
- *Wrest the Artifact*: theft from a living holder — culls the old `holds`,
  creates `rival_of`/hunt edges between old and new holder. Scavenging gambit
  remains for unheld artifacts.
- Zero-history gambit fixes: thread prior-edge preconditions into fictions
  that imply history (wield wants prior affinity/practice; raids want route
  familiarity or grudge; charters want lineage). Target: halve the ~70%
  zero-history share. Ambient opportunism (share practice, open route) stays.
- Generator bundle anchoring: every generated named entity gets ≥1 edge to a
  storied anchor and inherits a feud/practice/claim from it. Special case:
  `wars_get_names` newborns get edges into the war they are named for.
- Verify: memorial fidelity (honored_was_dead ≈ 1.0); grudge half-life
  finite; custody chains advance under conflict; zero-history share halved;
  generated-entity storied-link share ≥ 0.75.

### R5 — Chronicle Director

The one full new system: cast, plot, direction — the forward-running
counterpart of the illuminator chronicle pipeline
(`../the-canonry/apps/illuminator`, `webui/src/lib/chronicleTypes.ts`), whose
schema (ChronicleEntityRole, NarrativeFocus, ChroniclePlot.normalizedBeats,
sections with goals/emotional arcs, cohesion checks) is deliberately shared:
the illuminator fits plot to past events and writes prose; the Director casts
roles first and biases future events into the beats.

- `Engine.Kernel`: fixed-capacity story table (archetype id, role→entity
  bindings, act index, tension, deadlines); `StoryDirector` program slotted
  after Gambits; primitive events `story_cast`, `story_act_advanced`,
  `story_resolved`. No strings, no allocation, one writer.
- `Engine.Gameplay`: story archetype vocabulary — role eligibility
  (kind/tag/relationship patterns), act entry conditions over primitive
  evidence, per-act **role-scoped gambit affordance bias** (protagonist
  scores higher acting toward antagonist — the novel primitive that makes
  plot directional), per-act cast effects (including renewal access /
  drain relief while cast: story participation as the third legendary
  mechanism), deadlines, weighted resolution table with authored endings
  emitting real outputs (deaths, canons, memorials-of-the-dead,
  relationship rewrites). Resolution is guaranteed by deadline.
- `IceRemembers`: starter archetypes — the Hunt, the Revenge, the Rise, the
  Doomed Accord, the Artifact Quest — mirroring illuminator narrative-style
  structures (three-act heroic, ensemble, episodic).
- Casting detection works like generator triggers over primitive evidence
  (a pair's third war → Revenge; a monster surviving two slayers → Hunt;
  an artifact's third holder → Quest).
- Export: resolved story telemetry serializes as a ready-made ChroniclePlan
  skeleton (cast roles + contributions, beats with event ids, act sections)
  for the illuminator to render as prose.

**v1 design decisions (locked before build):**

- Casting reuses the generator targeting machinery (SharedQuery + the
  RelatedToTarget filter): primary role = generator-style target selector,
  remaining roles = secondary bindings related to the primary.
- Act conditions are pull-based predicates over role state — no
  cross-program event coupling. v1 condition kinds: RolePairRelationship
  (kind + min strength between two roles), RoleHistorical(role), Deadline.
  Every act has a deadline; the final act always resolves.
- Direction v1 = per-act role-pair gambit score bias (flat bonus when the
  scoring actor/target match bound roles, optionally a specific gambit) +
  per-act cast sustain (attribute delta applied by the director pulse).
- Resolution rows are weighted; outcomes are primitive writes:
  relationship creates/culls between roles, optional SetHistorical on a
  role, plus a story_resolved event carrying the outcome code.
- Primitive events: story_cast, story_act_advanced, story_resolved (no
  strings; projection joins names later).
- v1 archetypes: The Hunt (hunter penguin / quarry monster), The Rivalry
  (faction pair from pressed rival_of), The Artifact Quest (seeker penguin /
  storied unheld prize). Deliberately three; expansion after evidence.
- The story table is the first consumer of historical-relationship queries;
  its query needs feed the capacity/eviction design (per the handoff).
- **Capacity answer (from the built v1):** the Director queries only LIVE
  relationships — role conditions use FindRelationship on active pairs and
  resolutions write live edges. Kernel state needs no historical
  relationship queries; the chronicle reconstructs history from telemetry.
  Historical edges can be evicted from kernel state (archived to the event
  stream) without breaking the Director.
- Verify: stories cast/resolve at sane rates; resolution outcomes diverse;
  cast members show arc-shaped biographies; no runaway affordance bias
  (non-cast behavior stays healthy).

## Status

- [x] Investigation + analysis report (committed `c84f464`)
- [x] R1 — time recalibration (`89d1faa`; evidence
  `nq-r1-recalibration-3x25k-20260712`: penguin median 34-41y, artifacts no
  longer age-die, institutions persist)
- [x] R2 — gambit grants (`af6d81f`; evidence `nq-r2-grants-3x25k-20260713`:
  all 12 grants fire, 533 renewal beats, tail ratio 1.5-1.9 under 25k
  censoring; tail acceptance rides on the 100k long run)
- [x] R3 — war and alliance package (`237d3de`; evidence
  `nq-r3-warsaga-3x25k-20260713`: war median 5.9y, 6/186 under 100 ticks,
  72 rivalries -> 97 declarations, 23 sworn alliances, 44 ally-joins-war,
  58 named war casualties memorialized in death)
- [x] R4 — memory, artifact conflict, raider careers (`3b5168a`; evidence
  `nq-r4-memory-3x25k-20260713` + `nq-r1to4-longrun-1x100k-20260713`)
- [x] R5 — Chronicle Director core (`53ae586`; evidence
  `nq-r5-director-3x25k-20260713` + live smoke: all three archetypes cast
  from real history and resolve with [story]-sourced authored endings).
  Story cast/act/resolve telemetry landed as the story_events table
  (StoryChanged kernel event with story_sequence identity, archetype,
  event type, act index, outcome reason code, and all four role entity
  ids). The ChroniclePlan/illuminator export is shelved indefinitely by
  decision (2026-07-13); do not build it without an explicit ask.
- [x] Chronicle beats rework (2026-07-14, after the "chronicles have no
  beats" finding). Diagnosis was two implementation gaps and one authored
  flaw: (1) journal beats were retro-scanned from the rolling 160-entry
  event log, so any tale older than the window lost its history; (2) beat
  sources were only pair gambit/generator entries — relationship changes
  between cast members (war declared, route culled, practice broken) were
  invisible; (3) flourishing_road act 2's advance condition (trade_route
  >= 0.95) was already true when act 2 began, so every road resolved at
  exactly 405 ticks with an unearned "Proving". Fix: UiChronicleProjection
  now captures beats durably per tale at event arrival (cap 12, oldest
  single-cast beat evicted first) from four sources — gambits/generators
  between cast or by a cast actor or landing on the protagonist,
  relationship forged/severed events between cast pairs (created/culled
  event codes threaded from DomainRuntimeCodes), and act-advance markers;
  the game layer reads BeatsFor(sequence) instead of re-filtering the log;
  flourishing_road act 2 recut to an 850-tick Proving with causeway bias.
  Evidence (12k-tick live harness, seed 20260714): 41/42 tales carry
  beats, median ~5, rivalries/hunts hit the 12 cap, roads now span 1250
  ticks with 4-8 beats.
- [x] C1 — the beat engine (2026-07-14, approved plan). The act/resolution
  model was teleological: fixed act order, deadline-forced endings, and
  Director resolution writes that edited world state to match a chosen
  outcome. Replaced with authored beats — the fourth authorship class
  beside verbs, gambits, and generators. A beat opens on a world/heat
  condition (openings may reference world state and heat, never another
  beat's strike — heat is the only narrative memory), charges the world
  while open (role-pair gambit bias, heat-scaled for convergence, plus
  chronicle-granted gambits), and strikes on a real execution or a
  recognized state fact. Terminal beats recognize endings (deaths, edges
  culled by real gambits and drift); the Director writes nothing. A cold,
  quiet chronicle fades (heat decay 0.99/pulse, grace 1500, quiet 1250).
  Resolved casts enter a suppression ring (2000 ticks) so freshest-
  provenance casting cannot immediately recast the same tale.
  - Cross-story library (IceRemembersChronicleBeats): eight beats written
    against stances (protagonist/adversary) — naming, answer, blow upon
    blow, tide turns, flight, closing, allies answer, gathering storm.
    Archetypes opt in via a stance map; hunt, terror, and haunted run map
    stances, so most of their pools are shared, not bespoke.
  - The Hunt is the authored showcase: wound beats on real attributes
    (quarry containment, hunter vitality), The Beast at Bay grants the
    chronicle-only gambit penguin_corners_the_beast, and four recognized
    endings (fulfilled / dies with the hunter / beast endures via
    penguin_forsakes_the_hunt or grudge decay / outlasted the telling).
    Every ending path has a world actuator; forsake was authored to fill
    the only gap.
  - The other six archetypes are mechanical translations (old biases as
    charge-only beats, old resolution Whens as terminal recognitions,
    dice endings dropped, fades authored — some as positive quiet endings:
    "the practice endures", "the road endures quietly").
  - Terminology: the legend-making memory layer is named remembrance (not
    witness); it is C3 with convergence pull/entrance beats as C2.
  - Live 12k harness after pacing tune: spans 50-4210 ticks (the fixed
    405/800/900 clusters dissolved), dramatic endings emerged from world
    facts (terror driven off x2, hunt fulfilled x2, run cleansed, rivalry
    settled, teaching broken), hunts had zero fades, hunt strike-sets
    differ per hunt, rivalries strike 3-8 beats.

## Scope notes and follow-ups discovered during R1-R4

- The lifecycle-reason telemetry channel is vestigial: `reason_code_id` is a
  copy of `source_code_id` and symbol kind 21 has no names. Real death-reason
  semantics need a kernel event change; documented rather than half-fixed.
- Relationship drops on entity death emit no cull telemetry (edges soft-drop
  via active-endpoint filtering). Observability gap, not a state bug.
- Orca lifespan is dominated by generator consumption (median death age 0 in
  some seeds), not aging; fixing orca longevity means retuning the orca
  loops' spend-style retirement, not bands.
- Memorializes retargeting was scoped down: `war_takes_its_toll` now makes
  memorials-of-the-dead a substantial channel, and grudges decay, but the
  full audit of living-honoree `memorializes` edges across generator bundles
  remains backlog.
- Zero-history preconditions were threaded into raids (hunting-claim careers)
  and wars (rivalry escalation); scavenging/wield/practice remain legitimate
  opportunism by design.
- Serial-styled artifact names (e.g. W9-8JT5B) are canonical precursor
  artifacts, not a naming bug. Do not route them through NameForge.
- War duration target updated by decision (2026-07-13): shorten from the
  ~35y steady state but keep well above 10y so players register wars;
  peace threshold moved 0.35 -> 0.45 aiming for roughly 15-25y medians.
- The carrying-capacity governor (CrowdingBeyondCultureSupport) is exempt
  from health-effect time dilation: it is a population-rate control, and
  dilating it let growth outrun equilibrium.
- Chronicle participation should be roughly even across entity kinds
  (regions/canons excepted): seven archetypes now cover penguins, orcas,
  monsters, factions, settlements, transports, and abilities.
- **Chronicles are scarce by design (2026-07-13).** They are player focus
  objects in the three-culture framing - signal above entity noise, detail
  below culture focus. Global concurrency cap of 7 (SimulationOptions
  .ChronicleCapacity), one active chronicle per archetype, long casting
  cadences, selective casting gates. ~1000 tales per run was noise; the
  target is a handful running at once and a followable few dozen per
  century.
- **Terminology unified (2026-07-13): there are only chronicles.** All
  Story* engine/domain/telemetry names are renamed Chronicle*
  (chronicle_events table, ChronicleChanged event, chronicle source code).
  The pre-existing provenance causal-chain config (DomainConfig.Chronicle,
  ChronicleQueries) is not a rival concept: it is the chronicle system's
  causality substrate.
- **Provenance merge v1:** the Director consumes the causal record when
  casting - among gate-passing candidates, the one whose qualifying edge
  has the most recent provenance invocation tick wins the tale. Deeper
  merges (authored causal chains as casting triggers; chain evidence as
  act-advance conditions) are the next chronicle round.
- MC registry hygiene: superseded evidence runs are deleted to save space;
  keep only recent runs worth referencing instead of re-running long MC
  processes (2026-07-13 cleanup: 143G -> 21G).
- Scarcity verified (`nq-chronicle-scarcity-3x25k-20260713`): 76-87
  chronicles per century per run (was ~320), max 7 concurrent, archetype
  mix flattened (roads 69 / teachings 41 / haunted runs 37 / practices 36
  / terrors 23 / hunts 20 / rivalries 17 across three seeds).
- Chronicle UI (2026-07-13): the ChroniclePanel now leads with a
  Chronicles section - unfolding tales first (archetype, act, age in
  years, cast), then recent endings with outcomes; every cast name is a
  link that focuses the entity on the map and opens its card; tale
  begins/act-advances/endings also interleave into the event log with a
  Chronicles filter. Projection: UiChronicleTale read model over
  ChronicleChanged events; archetype and outcome display names flow
  through the symbol catalog.
- Trade-route lifetimes (uniform ~630 ticks) are drift-to-cull artifacts of
  creation strength 0.1; retune alongside future trade content.
- New reusable vocabulary added along the way: gambit-grant preconditions
  (`SelfHasRelationshipToEntityOfSubtype`, `EndpointSubtypeIs`), the
  generator `RelatedToTarget` binding filter, and the
  `CullEndpointIncomingRelationships` operation.
- The kernel event log (65536, DropOldest) silently overwrites unread
  events when consumers lag. A harness pumping every 500 ticks lost
  ChronicleChanged Cast events wholesale (tales appeared to resolve
  instantly); pumping every 5 ticks fixed it. The game pumps per frame, so
  normal play is safe, but sustained fast-forward could shed chronicle and
  beat events the same way — worth a dropped-event counter or a larger log
  if fast-forward speeds grow.
- Relationship slots are lifetime totals (culled edges stay allocated as the
  historical record). The recalibrated world exhausted the old 8192 cap
  mid-run at the 100k horizon; capacities were raised (relationships 65536,
  entities 16384) for evidence runs, but the 2.5M-tick full-game horizon
  needs a relationship/entity history-eviction architecture. This is the
  accretion warning made concrete.
