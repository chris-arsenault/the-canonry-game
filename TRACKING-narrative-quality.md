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
  - Terminology: the legend-making memory layer is named remembrance; it
    is C3 with convergence pull/entrance beats as C2. The word from the
    earlier draft name is retired from all chronicle content by decision.
  - Live 12k harness after pacing tune: spans 50-4210 ticks (the fixed
    405/800/900 clusters dissolved), dramatic endings emerged from world
    facts (terror driven off x2, hunt fulfilled x2, run cleansed, rivalry
    settled, teaching broken), hunts had zero fades, hunt strike-sets
    differ per hunt, rivalries strike 3-8 beats.
- [x] C1b — extended beat pool (2026-07-14). Three vocabulary additions
  (all additive): a RoleGambit strike (a role executes a named gambit
  against anyone - deeds in the world, not just against the cast), a
  GeneratorOnRole strike (the generator program now reports fires to the
  director, so world-authored developments landing on cast members are
  authored beats - "the war is named", "the toll is counted"), and
  RoleAttributeAtLeast / HeatBelow conditions (recovery and lull beats).
  Beats now also open at cast time, so Always-open beats charge and accept
  strikes from the first pulse. Shared library grew to eleven (added: the
  adversary gathers, the lull - the breather beat, no quarter at heat 8);
  archetype pools gained flavor beats grounded in real actuators: the
  binding hour and second wind (hunt), the war is named / call to banners
  / toll is counted / mutual-ruin pair (rivalry), teaching passes on and
  the student's deed (teaching), raiders on the road and the two forging
  beats (road), the conduit holds and salt and iron (haunted run), the
  practice spreads and the keeper's grace (practice), the deep current
  and blood in the water (terror). Live 12k harness: hunts strike up to
  7 beats (one saga: allies answer, failed slay, blow upon blow, the
  closing, Corner the Beast landing the kill the slay missed, beast at
  bay, fulfilled), haunted runs 7-10 strikes, a rivalry hit 12.
- [x] C1c — story coherence fixes (2026-07-14, after the "this isn't a
  coherent story" finding on a Living Practice tale). Four defects:
  (1) strikes fired on failed gambits while narratives claimed success
  ("Share a Practice failed" struck "finds another pair of hands") —
  strikes now require success unless the beat is authored AcceptFailure,
  reserved for outcome-neutral phrasings (the naming, the answer, blow
  upon blow, the tide turns, no quarter, the beast at bay); (2) shared
  beats with no stance references (the lull) vacuously applied to every
  archetype — the shared library now applies only to stance-mapped
  archetypes; (3) the observed record was polluted by cast members'
  routine solo deeds (Deepen the Holding, deep currents at random
  settlements, failed wrests against non-cast) — observed events now
  require two cast members or the world acting ON a cast member;
  (4) struck beats and raw log lines were indistinguishable — the journal
  splits "The telling" (authored struck-beat narratives) from "From the
  record" (observed events). Quiet tales are now honestly quiet: a
  practice tale reads as its one true telling line instead of eleven
  lines of unrelated errands.
- [x] C2a — entrance beats and open roles (2026-07-14, after the "never
  more than 2 cast members" finding). Convergence's first mechanism:
  archetypes declare open roles (kind-constrained cast slots that start
  empty), and entrance beats fill them - the outside party of the strike
  joins the cast (the outsider acting on a role, or the outside target of
  a role's deed). An entrance without a qualified entrant is no strike;
  entrants must be alive, of the role's kind, and not already cast.
  Authored entrances: the hunt's blood_calls_to_blood ($second penguin
  answers any attempt on the quarry) and the_binding_hour ($instrument -
  the binding artifact joins), the terror's the_coast_rises ($second),
  the rivalry's sworn_to_the_cause (the cold-accord partner joins as
  $ally), the haunted run's salt_and_iron ($instrument). Joined roles
  carry their own charges (the second's slay is heat-scaled). Template
  filling is now role-id aligned (UiChronicleTale.RoleIds) so narratives
  fill correctly around still-empty slots. Live 12k harness: a
  four-member hunt (quarry, hunter, second, bound artifact), grown
  terror and haunted-run casts.
- [x] C2b — the convergence pull (2026-07-14). The map-signal attractor
  field was the planned carrier but has no runtime consumers, so the pull
  was built on the surface every action already passes through: past a
  heat floor (4), any outsider's gambit against a chronicle's cast gains
  heat-scaled score (slope 0.3, cap 2.0). Power is drawn toward running
  stories as one uniform Director rule - and it directly feeds entrance
  beats. Live harness: grown casts rose from 3 to 5 in the same window,
  hunts striking 9-11 beats.
- [x] C3 — remembrance (2026-07-14). Flows entirely through generators;
  the Director writes nothing. Generator targets gain chronicle-fact
  gates: ChronicleHeatMin (cast in a running chronicle at heat) and
  ChronicleLegendaryWithinTicks (recently resolved out of a chronicle
  whose final heat reached the legendary threshold of 8 - the suppression
  ring now records final heat). Two authored generators: the_ice_takes_
  notice (remembrance-stone artifact memorializing the living center of a
  blazing story, gated heat >= 6, deduped on incoming memorializes) and
  legend_enters_the_canon (births a saga - new canon subtype with its own
  name forms - claiming semantic jurisdiction around a legend within 1500
  ticks of a legendary resolution, memorializing and claiming them).
  Remembrance is the C3 name for the legend-making layer; the earlier
  draft name was judged too Malazan and is retired from chronicle content.
- [x] C2c — story favor (2026-07-15, after the "so what / fails and
  fizzles / beats are nothing-happens" finding). Root cause chain:
  baseline gambit success is 0.25 and charges only biased selection, so
  the story pushed actions the dice failed three times out of four; deed
  beats are success-gated, so tellings filled with lulls, flights, and
  fades. Charges now carry an authored SuccessBonus added to the gambit
  success roll while the beat is open (never heat-scaled; typical 0.1 to
  0.3, the beast at bay 0.3 on top of the granted corner) - the story
  favors the committed. Nothing-beats quieted (lull heat 1 to 0.5,
  flight 1.5 to 1). Roads gain their first entrance: raiders_on_the_road
  joins the raiding orca as $raider with heat-scaled raid charges - road
  tales gain an antagonist.
- [x] C2d — the quiet trio (2026-07-15). Teaching, practice, and road
  were structurally silent: their beats keyed to rare grant-gated gambits
  or thresholds their 1505-tick lifespan never reached, so they faded at
  the global grace floor with empty tellings. Three fixes: (1) guaranteed-
  expression beats on the facts these tales must pass through - the bond
  frays (taught_by below 0.35, charging the fight to mend it), the bond
  deepens, the master falters (vitality), the discipline tested, the road
  strains (route below 0.5, charging both ports to shore it up);
  (2) entrances wired to deeds these casts actually perform - the
  practice's own sharing draws the new practitioner in as $novice
  (RoleDrawsIn on ability_shares_practice), the master's passing of the
  teaching draws in $second_student; (3) per-archetype slow-burn fade
  windows (new authored knob: FadeGraceTicks/FadeQuietTicks, 0 = engine
  default) - teaching 3500/2500, practice 3000/2000, road 2500/2000 -
  because bonds drift and masters age on ten-year scales, and a slow
  story force-fitted to a six-year clock can only fade. Harness: teaching
  tales now span 2255-4510 ticks and reach the_teaching_broken; practices
  grow casts and reach the_keeper_passes; roads span 2500-4000 with
  strikes throughout.
- [x] C2e — chronicle presence (2026-07-15, after the "disjointed / not
  clearly special / needs an interest measure" finding). Heat is the
  interest measure and is now exposed: ChronicleChanged gains a heat
  float (schemaMinor 1; chronicle_events telemetry gains the column).
  The journal shows a named intensity band (cold embers / murmuring /
  burning / roaring / legendary) with glyphs, sorts unfolding tales by
  heat, and displays glyphs in the nav. Displayed heat is decayed from
  the last event to the current tick with the director's own cooling
  rate so it never overstates. The map gains a ChronicleAuraLayer:
  pulsing rings around every cast member of an unfolding tale, tinted
  cold blue to ember orange by heat and pulsing faster when hot - a
  running story is visibly worth following in the world. The journal's
  split telling/record lists are merged into one chronological timeline:
  struck-beat narratives lead, and the raw record events sit indented
  and dimmed beneath them so cause and telling read together.

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

## Kinetics round — proposal space (2026-07-15, drafted; not yet approved)

Diagnosis: `docs/analysis/world-kinetics-20260715.md`. Metrics to move:
displacement efficiency (80 hops/epoch nets 11 hexes; 86% cancels),
flux localization (culture gains:losses 1.02 uniform; no fronts, no
ownership reversals), watchable density (0.13 deeds/tick worldwide).
Low-altitude verbs are the intended ebb/flow engine, with gambits as
punctuation; the verb layer runs constantly but its product is
centripetal jitter.

Axis 1 — journeys:
- J1 destination verbs (authored): pilgrimage, wandering season,
  caravan runs, chronicle-charged pursuit; commitment-until-arrival.
- J2 consume the attractor field (systemic): verb selection treks to
  high-attractor cells beyond a minimum range; chronicle heat already
  boosts cast attractor mass, so convergence becomes visible for free.
- J3 the anchor drifts: crowding-triggered anchor relocation and
  settlement fission with founding treks.

Axis 2 — fronts:
- F1 conflict scars the field: wars/raids apply concentrated culture
  erosion at the event site via gambit/generator effects.
- F2 frontier contest: contested mixed-influence cells decay the weaker
  side faster and draw both sides' verb targeting; the standing tide.
- F3 the ice breathes: epochal terrain-modulated decay; only as a layer
  under F2.

Axis 3 — tempo:
- T1 duty-cycle travel (rest long, then move every tick to arrival).
- T2 motion rendering (trails, interpolation).
- T3 render minor actors: the kernel's invisible cohort labor drawn as
  flow along roads - highest visual life per effort, zero sim change.

Bundles: A "The Tide" (J2+F2+T1, systemic, heaviest tuning risk);
B "The Deeds" (J1+F1+T3, authored, independently shippable);
C "The Stage" (T1+T2+T3+pursuit only, legibility probe).
Draft recommendation: B first, then F2 (+J2) as its own macro round.
Targets: journeying-kind displacement efficiency >= 40%; per-region
gains:losses spread 0.8-1.3; >= 1 boundary reversal per 25k; motion in
any 20-hex viewport within seconds.
Not changed by any bundle: chronicle/beat system, scarcity caps,
lifecycle bands, microkernel constraints, no-Director-writes.

### Map geometry findings + the bold program (2026-07-15, exploration)

Geometry (run 4700, r40 hex map, 4,921 cells): the map is simultaneously
too co-located and too diffuse, at different scales. Micro: median
penguin-to-nearest-penguin distance is 2 hexes, penguin-to-monster 4,
72% of penguins sit within 5 hexes of a settlement, and 100% of
penguins are within slay range (24) of a monster - interaction geometry
is not the blocker today. Meso: only 48% of cells were ever visited by
any named entity across 25k ticks; civilization is ~6 dense settlement
halos separated by dead ocean no journey crosses. Generator target
rejections are graph conditions, not geography (subtype/tags 43%,
required relationship 31%, trigger 10%, absence 10%; placement 0.1%).
Culture fragmentation is halo-shaped: components grow 2 -> 11 while
largest-component share stays 74-91%.

Scaling implication: current physics survives ONLY because of
co-location. Fixed hex-radius gates (24/18) plus centripetal verbs on a
very large map would produce dozens of disconnected halo-villages with
zero inter-halo interaction. Scale-out fails without reach that grows
with the world.

The bold program (three pillars, subsumes bundles A/B):

1. WORLD-BEATS - the ages of the world as a macro-chronicle. The world
   itself gets the beat treatment: an unordered pool of Ages with
   opening conditions on macro facts (saturation, population, first
   legendary saga, war exhaustion, canon count), charges that modulate
   the whole domain while open (generator pulse weights, verb
   weightings, casting gates, decay tides per age), and transitions
   recognized from world state - never timers, never scripts. Age of
   Founding (exploration-charged) strikes into the Age of Striving when
   frontiers first collide; the Age of Accords opens on war exhaustion
   (diplomacy/canon-charged); the Long Dark on saturation + stagnation.
   Exploration, war, and diplomacy epochs emerge in whatever order this
   world earns them.

2. THE GROWING MAP - map size becomes a result of history. Bootstrap on
   a small thawed heart (co-location by construction, dense
   interaction), and let epoch pressure open the world: thaw rings /
   reveal regions via terrain transformation and cell creation as the
   Age of Founding charges exploration. Founding treks colonize the new
   ground; wars ignite where cultures meet; the very large map is
   earned, structured by the paths history took, never dead-on-arrival.

3. TRAVEL-COST REACH - civilization shrinks the world. Replace raw
   hex-radius interaction gates with route-cost distance (the movement
   program already computes road/causeway/conduit-aware costs). Roads
   and conduits then extend interaction reach: connected settlements 60
   hexes apart are "near" for trade, war, and diplomacy; wilderness
   stays far. Infrastructure becomes the connective tissue that keeps
   gambits and generators hooking in at any map size - and building it
   is visible flow.

Kinetics bundles A/B become sub-rounds: journeys/fronts/tempo are the
moment-to-moment texture inside whichever Age is open.

### Civ-parallel refinements + the progression epic (2026-07-15, direction)

Mental models by decision: Civ is the closest analogue for map/movement
scaling; Universim for player agency (directing where the bulk
civilization grows, and having it actually grow).

Travel-cost reach, refined by the Civ parallel (what makes it work
there as ages advance):
- Roads are speed, not decoration: road tiers lower effective travel
  cost (and later tiers lower it further - the road -> railroad curve).
  Road quality already exists on cells and the movement program already
  prices it; the change is making tiers matter enough to be felt and
  seen: caravans visibly faster on good roads.
- Transport-class entities become actual carriers: they load and carry
  (artifacts, founding cohorts, trade goods-as-relationships) between
  connected settlements and move materially faster than walkers. A
  transport is the setting's railroad: reach and capacity embodied in a
  named entity you can watch cross the ocean of ice.
- Explicitly rejected: innate per-entity movement increases across
  ages (Civ's unit-speed creep) - wrong for this setting. Speed comes
  from the world (roads, conduits, carriers), never from the creature.

Follow-up epic (needs its own design round; build toward it): the
PROGRESSION LATTICE - a tech/unlock tree, possibly the key missing
piece against stagnation. The domain already holds the in-fiction
carriers, so this is not a bolted-on tech screen:
- abilities = techniques (already grant gambits via practice),
- canons = institutions (already gate/charge behavior via
  jurisdictions; sagas feed them via remembrance),
- travel structures = infrastructure tiers (causeway -> conduit -> ...),
- world-beats/Ages = the clock that opens branches.
Unlocks charge domain content the way beats charge gambits: a culture
that establishes a practice/canon/work unlocks verbs, gambits,
generator eligibility, and structure tiers downstream of it. Player
agency lands in the existing card/tableau layer: directing which
branches the bulk civilization pursues (Universim-style), so growth
direction is the player's hand without puppeting any entity.

### Kinetics round, Bundle B in progress (2026-07-15, branch kinetics-deeds)

- B1 minor-actor flow rendered: MinorActorFlowProjection (recent-pulse
  ring over MinorActorVerbExecuted events) + MinorActorFlowLayer
  (fading culture-tinted motes under the entity layer). The kernel's
  invisible cohort labor is now on the map; zero sim change.
- B2 conflict scars: new ErodeTargetCulture primitive for both
  authorship classes (gambit mutation + generator effect) - erodes the
  target's culture influence in a radius at the target's position,
  strongest at center. Authored: faction_declares_territorial_war
  (r2/4), orca_raids_trade_route (r2/3), war_takes_its_toll (r3/5),
  ice_settles_accounts (r3/6). Conflict now couples to concentrated
  territorial loss.
- B3 pursuit journeys: new ChroniclePursuit verb target selector - the
  verb layer asks the Director for the entity's chronicle costar
  (cast converge on the primary; heat floor 2.5, min distance 5) and
  commits a long journey (answer_the_tale verbs for penguin/orca/
  monster, 250-600 tick commitments, heat-scaled weight). The tale
  draws its cast together, visibly.
- Pre-MC live check: penguin displacement efficiency median 33%
  (12 net hexes / 46 gross hops per epoch) vs ~14% at diagnosis.
- Journey verbs added after the pursuit finding: the wandering season,
  pilgrimage (to canons), far currents - long commitments to
  structurally far targets.
- MC verdict (kin-deeds-3x25k-20260715b, vs run-4700 baseline): p90
  range per epoch 33 -> 41 hexes (+24%), map utilization +11%; median
  endpoint displacement unchanged (13-14%) - journeys are round trips
  and the home-pull owns the median, which is defensible for a settled
  species. Pursuit is correct but gated by co-located casts (min
  distance rarely met); it will matter on grown maps and pulled-in
  entrants. Scars at 4x still produced zero net-losing regions -
  event-pulse erosion cannot beat continuous verb reinforcement;
  visible fronts require the continuous-contest macro round (F2), as
  bundled. Flow rendering and auras need visual confirmation in Godot.
- Next: F2 frontier contest as its own macro round; duty-cycle step
  tuning; caravan carriers per the Civ-parallel refinements.

### Kinetics round, Bundle A "The Tide" (2026-07-15, branch kinetics-deeds)

Scope: F2 frontier contest + J2 attractor consumption + T1 duty-cycle.
Evidence chain: kin-tide-3x25k-20260715b (first cut), -c (gate fixes),
-d (final, contest 6x); baselines kin-deeds-3x25k-20260715b and
c2d-quiet-trio-3x25k-20260715.

- F2a contest decay (kernel CultureFieldDecay): in a cell where a rival
  culture holds more raw mass (and at least ContestFloorRawMass 4), the
  locally weaker side's decay is multiplied by ContestDecayMultiplier.
  Two neutering bugs found via the b/c runs and fixed: (1) the gate
  originally exempted local mass < 4 - exactly the thin weaker tails
  where fronts live (only 53 of 519 mixed cells qualified); (2) the
  multiplier originally scaled the decay *rate*, which the 0.6
  MinimumDecayPerPulse floor swallowed for all raw < 120. Final form:
  decay = max(raw * rate, minimum) * contest, contest 6x after 2.5x
  proved invisible (+2.8% losses) against the ~4M raw background.
- F2b frontier verb weighting: contested cells (both sides >= 4 raw)
  weight foreign-influence verb targets x1.75. Confirmed working:
  verb-sourced culture losses nearly doubled (79k -> 145k raw).
- J2 attractor consumption: chronicle heat now radiates into the
  attractor field (HeatFor x 2 in EntityAttractorMass) - convergence
  made spatial; and a new AttractorTrek verb selector samples an
  annulus (min distance 8) weighted by prior-pulse attractor mass,
  through a declared Simulation.ReadAttractorMassSnapshot boundary
  read. Authored penguin/orca "Follow the Gathering" treks (300-700
  tick commitments, weight 0.5, distance scale 40).
- T1 duty-cycle: confirmed already structural (committed movement steps
  every 1-3 ticks; gaps between intents are dwell). No change.
- MC verdict (kin-tide-3x25k-20260715d at 6x): contested-band decay
  losses +9.5% vs Bundle B; final mixed cells (both cultures > 0.5 raw)
  -16% - boundaries visibly sharpen; culture reversals, chronicle
  volume (132 tales), and displacement all stable. But ownership flips
  fell 233 -> 133: contest decay is a conservative force - it crushes
  weak incursions before they flip cells, so fronts sharpen while
  ownership stabilizes. Decay-side contest alone cannot produce moving
  fronts; boundary MOVEMENT needs the attacking side to project mass
  (concentrated reinforcement campaigns, settlement founding/loss) -
  that is Ages/generator territory, recorded as the follow-up.
- Trek verdict: physical verb hop volume +21% (470k -> 571k) - more
  visible motion on the map - but epoch displacement efficiency dipped
  (0.218 -> 0.176): treks are round trips; they add journey volume,
  not migration. Same defensible-for-a-settled-species shape as the
  Bundle B journey verbs.
- Next: world-beats/Ages round (mass-projection campaigns that can
  move the sharpened fronts), growing map, travel-cost reach, caravan
  carriers, progression-lattice epic.

### Kinetics round, Bundle C "The Stage" (2026-07-15, branch kinetics-deeds)

Scope: the legibility probe (T1+T2+T3+pursuit). T1 proved structural
and T3 + pursuit shipped in Bundle B, so this bundle is the remaining
piece: T2 motion rendering. Zero simulation change - projection and UI
only, so no MC evidence run (provenance would be identical to
kin-tide-3x25k-20260715d).

- T2a interpolation: already present (Entity.MoveTo tweens position
  over 0.25s); no change.
- T2b trails: new EntityTrailProjection (4096-segment ring over
  EntityMoved kernel events, same pattern as MinorActorFlowProjection)
  + EntityTrailLayer (fading kind-tinted lines along recent hops,
  90-tick fade, drawn behind the entity layer). Wired through
  KernelUiRuntime.EntityTrails, GameRuntimeReadModels, GameRenderTree.
  Journeys now read as paths being drawn - a trek, pursuit, or
  pilgrimage leaves a visible wake instead of a token teleporting.
- Live check: ring fills (12.5k movement events over a 3k-tick run),
  plane filtering and fade ages correct. Ring covers ~110 ticks at
  late-sim hop volume, comfortably above the 90-tick fade.
- Needs visual confirmation in Godot alongside Bundle B's flow motes
  and chronicle auras.
- Next: world-beats/Ages round (mass-projection campaigns for the
  sharpened fronts), growing map, travel-cost reach, caravan carriers,
  progression-lattice epic.

### World-beats (Ages) + mass-projection campaigns (2026-07-15, branch kinetics-deeds)

The combined round commanded after Bundle A's finding (contest decay
sharpens but stabilizes; boundary movement needs attacker mass
projection). Evidence: ages-campaigns-3x25k-20260715b, probes
ages-frontier-probe-1x25k-20260715b; baseline kin-tide-3x25k-20260715d.

- WORLD BEATS: new kernel program (Programs/WorldBeats) - the beat
  treatment applied to the world. Three regime tracks (Ice, Peoples,
  Memory), each holding at most one open age; authored ages open when
  ALL opening conditions are recognized from macro facts (population
  by kind, wars living/culled, total culture mass and growth,
  legendary resolutions, ticks) and close when ANY closing condition
  holds after a minimum span - never timers. Priorities pick among
  eligible ages; cooldowns stop flapping; the Still Ice (no
  conditions, priority 0) makes "at least one age open" structural.
  Charges push idempotently every pulse into per-slot multiplier
  arrays owned by the target programs: generator pulse weights, verb
  weights, gambit scores, the contest decay tide. WorldBeatChanged
  primitive event -> world_beat_events parquet + top-bar age display.
  Authored pool: the Thaw / the Still Ice / the Long Dark;
  Striving / Accords; Signs / Canons (IceRemembersAges).
- CAMPAIGNS: wages_campaign_against relationship (exclusive, decaying
  commitment; cull ends the campaign), armed by
  faction_marches_the_banners (Striving charges it x4), re-pressed by
  the_column_holds generator + faction_feed_the_column verb while the
  war justifies it. While the edge lives, the minor-actor CampaignLabor
  emitter spawns worker columns at the live frontier toward the
  target: anchor walks from the enemy position back to the first
  own-trace cell, then a campaign-specific spawn scorer picks thin own
  ground with maximum foreign contact (the ordinary spawn scorer
  maximizes connected mass and marched columns into their own core,
  where every verb scored zero - found via probe: 97% no-target
  pulses, fixed to 90% -> worked pulses x51, 23.5k raw delivered).
- Verdict (b run vs tide-d): ages transition in different earned
  orders per seed (Striving opens t9.7k/13.8k/14.8k; Canons opens in
  2 of 3 runs after legendary resolutions; run 4901 even reached
  Accords in the first cut). 80 campaigns lived and 61 ended -
  war exhaustion now accumulates. OWNERSHIP FLIPS 163 vs 133
  (+33% per cell-epoch) - the first movement in this metric across
  three rounds. Reversals 12 vs 11; chronicles stable (133 tales).
- Honest limits: campaign columns still idle ~90% of pulses (they
  outrun their own supported field and stall on fresh corridor);
  flip movement is real but modest. Next lever is column drift toward
  the target and campaign-purpose scoring that lets a column press
  from its own laid corridor - plus the growing map, where campaigns
  get real geography to cross.
- Docs: docs/systems/25-world-beats.md, architecture.md,
  game-systems.md updated.

### Growing map (true cell creation, r18 heart) + travel-cost reach (2026-07-16, branch kinetics-deeds)

The next big chunk after world-beats: map size becomes a result of history,
and reach scales with the world. Evidence: grown-map-reach-3x25k-20260716
vs ages-campaigns-3x25k-20260715b; unit tests MapFrontierProgramRuntimeTests
+ TravelReachCostTests.

- THE HEART: physical grid seeded at PhysicalHeartRadius 18 (~1027 of the
  eventual 4921 cells); semantic plane stays full (ideas have no ice). The
  heart is dense by construction - the co-location the early sim needs.
- MAPFRONTIER (new kernel program): reads WorldBeats.OpenAgeId(Ice) and
  grows/shrinks the physical world. THAW (Age of the Thaw): culture mass at
  a frontier cell (raw >= threshold) opens its unknown neighbours as new
  cells via runtime Simulation.CreateCell, region inherited from the pressing
  cell, bounded by MaxRadius 40 + per-pulse budget. RECLAIM (the Long Dark):
  rim ground the program thawed with no mass and no road is taken back as
  impassable deep_ice (a new terrain, all-zero movement profiles); a thawed-
  cell ring keeps reclaim to ground the program opened. Re-freeze transforms
  terrain rather than deleting cells, so occupancy/culture rows stay valid.
- TRAVEL-COST REACH: new WithinTravelReach(maxCost, plane) precondition line-
  samples the hex line between actor and target, summing road-discounted
  terrain cost (Movement.TravelCost); impassable/un-thawed ground makes the
  route infinite, roads shrink it. Reader passed through QueryBinding
  (kernel query stays sim-agnostic; no reader = gate fails). Authored on the
  hunt (Corner the Beast), Slay Monster/Orca, Claim Point of Interest, Raise
  Causeway; short-range and semantic gates stay on hex distance.
- VERDICT: the map grows (1027 -> 1268-1683 cells per seed, +241-656). The
  decisive win is utilization: the grown world is 78-88% occupied vs the old
  r40 world's 44-53% - the "dead ocean no journey crosses" problem from the
  geometry analysis is essentially gone; nearly all the world the player sees
  is lived-in, and it grows. Ownership flips TRIPLED (507 vs 163) - the dense
  heart makes cultures actually contest. Travel-reach gambits all still fire
  (hunt, slays, claims, causeways); on the compressed heart the gates are
  passable, so behaviour is preserved while the mechanism now scales.
- Honest limits: the Long Dark did not open in 25k ticks (young worlds keep
  growing, mass never stalls), so reclaim is unit-tested, not yet seen in a
  full run - it needs a longer horizon or a saturation scenario. Chronicles
  dipped slightly (112 vs 133 tales) in the smaller world. Tuned thaw
  (threshold 25, budget 48) and lengthened the founding window (Thaw closes
  on culture-mass saturation, not settlement count) so growth is legible.
- Seed geometry tests recalibrated to the r18 heart (cell counts, spans,
  subdivision land) or rewritten to durable growing-map contracts
  (SeedCivilizations_SpanTheHeartWithNearAndFarNeighbors, subdivision test);
  the old r40 near/far cluster narrative no longer holds on the bootstrap.
- Docs: docs/systems/26-growing-map.md, architecture.md, game-systems.md.
- Next: caravan carriers (transports as true carriers per the Civ parallel),
  a saturation/Long-Dark scenario to exercise reclaim, the progression epic.

### Tier 2: carriers, campaign-idle fix, saturation scenario, world-beats UI (2026-07-16, branch kinetics-deeds)

Round the user commanded after the tier-2 backlog review (they rejected
merging unfinished work, added the world-beats UI). Evidence:
tier2-carriers-3x25k-20260716b (main) vs grown-map-reach baseline;
tier2-saturation-longdark-1x25k-20260716i (reclaim scenario).

- CARRIERS: transports now load, haul, and deposit passengers. New
  `carries` follow-relationship (transport -> entity, physical follow,
  exclusive); load gambit `transport_takes_up_the_passenger` and deposit
  `transport_sets_down_the_passenger` (sets down where named life
  gathers). Road-speed for carriers: a laden carrier (holds a carries
  edge) on a good road gets a shorter movement step interval - the
  Civ railroad, speed from the road and the load, never the creature.
  Result: 519 loads / 351 deposits succeed, ~120 passengers relocated
  per run.
- CAMPAIGN-IDLE FIX: campaign columns scored zero on thin frontier
  because the interior culture scorers need deep connected support.
  New campaign-purpose scoring presses from local own presence
  (SupportedAdjacentCount), not home components. Worked pulses
  49% -> 85%, culture delta +47% (96653 -> 141924). Fronts improved
  too (flips 590 vs 507).
- SATURATION SCENARIO + reclaim, the hard part. A config
  (tools/MonteCarlo/scenarios/saturation-longdark.json) raises culture
  decay to stall growth so the Long Dark opens. Getting reclaim to
  actually fire exposed and fixed three real issues:
  1. WORLD-BEATS PREEMPTION (framework bug): opening only filled blank
     tracks, so the priority-0 Still Ice fallback trapped the Ice track
     forever and blocked the Long Dark. Fixed: a higher-priority
     eligible age now preempts a lower-priority incumbent past its min
     span. Unit-tested.
  2. THE THAW never closed under stagnation (mass < its 900k close
     threshold): added a growth-stall close condition (inert in growing
     worlds, which close on mass first).
  3. RECLAIM SOURCE: cold edge cells are the heart boundary and fully
     culled (mass 0) - absent from both the thawed ring and the culture
     cursor. Reclaim now scans the frontier box for cold edge cells and
     the Long Dark charges a new GLOBAL DECAY TIDE (every cell, "the
     dark presses in on every frontier") so untended rim cools below the
     ceiling. A heart-radius floor protects a minimal core.
  Result: the full Ice-age arc cycles (Thaw -> Still Ice -> Long Dark
  -> Thaw -> Still Ice) and deep_ice appears exactly when the Long Dark
  opens (t15250). The recede is conservative (peak 4 cells) - the dense
  heart stays warm and reclaim is deliberately gentle; a dramatic
  dissolve would need a deeper/longer stagnation, which is fine.
- WORLD-BEATS UI: always-visible ages strip (three track chips
  Ice/Peoples/Memory -> current age) clickable to open a panel with each
  track's age, its telling, and a transition timeline. Projection
  exposes age names + a transition ring. Needs visual confirmation in
  Godot with the other unrendered layers (trails, flow, auras, growing
  map, deep ice).
- Docs: 25-world-beats.md (preemption, global tide), 26-growing-map.md
  (reclaim mechanism), architecture.md, game-systems.md.
- Next: the progression-lattice epic; Godot visual pass; caravan
  capacity/reach growth across ages.
