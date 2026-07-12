# Narrative World-Building Quality Pass — 2026-07-12

Point-in-time analysis. Judges whether completed simulation runs read as
believable cause-and-effect world history or as disconnected random churn.
Analysis only: no engine, domain, seed, or test changes were made.

## Evidence

- `narrative-quality-3x25k-20260712` — 3 seeds (7100-7102), 25k ticks each,
  current main. All quantitative screens were run per seed; cross-seed variance
  was low, so single representative numbers are quoted with ranges.
- `narrative-quality-1x100k-20260712` — 1 seed (7200), 100k ticks, long-horizon
  confirmation for lifespan, persistence, and arc questions.
- Method: a DuckDB "biography workbench" over the parquet telemetry
  reconstructs per-entity beat timelines (relationship create/cull/strength
  events, gambit events, generator births, tags, lifecycle, death reasons, with
  name symbols joined) and episode spans (war/alliance/trade created→culled
  pairs, artifact custody sequences). Quantitative screens per axis, then
  qualitative reads of sampled biographies scored for motive, continuity,
  stakes, and resolution.

## Overall Verdict

The world is a coherent **place** but not yet a coherent **history**. Spatial
grounding is genuinely good: wars are fought between neighbors over contested
ground, stolen artifacts physically travel, activity has geography. The event
vocabulary is expressive enough to produce real story beats — a monster that
steals three artifacts and is hunted by two named penguins, an orca slain by
the penguin it raided in the same tick, an artifact twice defended against the
same recurring thief.

But looking back over a completed run, cause-and-effect mostly is not there.
Things happen because scores and cooldowns permit them, not because of what
came before; arcs are set up and then beheaded by generic old-age death before
any payoff; and the world's memory is inverted — permanent negative bookkeeping
accumulates forever while alliances and living relationships quietly decay to
nothing. A chronicler reading the telemetry can extract incidents, but rarely
stories.

## Axis Verdicts

Seventeen axes, grouped in four families. Verdicts: HEALTHY / WEAK / BROKEN.

### A. Characters

**A1. Protagonist lifespan sufficiency — BROKEN.** Named penguins die at
median ~2,810 ticks (p90 ~3,500) in a 25k run; orcas ~3,000-4,300. The p90/median
ratio is ~1.25: everyone dies at nearly the same age, so no penguin ever
outlives its cohort to become a legend. Max successful-gambit involvements for
any penguin in any seed: 9 (average 3.4). Root cause is authored:
`PenguinEntity` is `LifecycleBand.Brief` (500-tick design target) and
`OrcaEntity` is `Standard` (800); healing effects stretch observed life ~4x but
the band still caps any career. The de facto protagonists are factions (avg
15.5 touches, max 91) and settlements (median life ~11,000-12,000). The lore
kinds intended as main characters are the shortest-lived things in the world.

**A2. Arc shape — BROKEN.** Biographies have no rise/peak/decline. A typical
"top penguin" life (Qai The Icebreaker, seed 7100): born by generator at
21,380, wields one artifact, is raided once, dies of upkeep at 24,975. Event
mix is uniform across life quartiles; nothing marks youth from prime from
decline except the death tick. Status transitions other than
active→historical do not occur for individuals.

**A3. Protagonist concentration — WEAK.** Successful-gambit participation is
smeared flat: top-1 entity holds 1.3-3.0% of touches, top-10 hold ~10-11%,
while 250+ entities have 5+. Nobody stands out; the world has no memorable
cast, just a uniformly busy population. (Per the design principle that named
involvement should be rare escalation, the current spread reads as ambient
churn wearing named-entity costumes.)

**A4. Death meaningfulness — BROKEN.** Every death reason renders as a null
symbol (`lifecycle_reason_events.reason_code_id` resolves to no stable name in
symbol kind 21 — a telemetry naming gap worth fixing regardless). By source:
~85% of deaths via generic upkeep, ~12% via generator, ~2-4% via gambit. Deaths
have no downstream coupling — no succession, revenge, or memorial fired off a
death in any sampled biography. The qualitative flagship: monster
"Oorroo'hrosh, The Echo" (seed 7100) stole three artifacts, corrupted a
transport, was warded by four transports, bound by three artifacts, and
survived six named-penguin slay attempts by two distinct hunters — then died
of upkeep at 13,300 with all hunters alive. The world authored a dragon-slaying
arc and the dragon died of natural causes.

### B. Causality

**A5. Motive legibility (causal depth) — WEAK.** For each successful gambit,
count prior events linking actor and target. Most gambit kinds fire at median
**zero** prior pair history: steal/wield artifact, open trade route, charter
settlement, bind monster, share practice, raid (60-80% of fires are
zero-history for these). War declaration is seed-unstable (median 156 / 22 / 4
prior events across seeds), and its authored preconditions confirm why:
contested map influence + own cohesion — no grudge, rivalry, or prior contact
required. The deep-history exceptions (sue for peace ~270-760, raise causeway
~1,300-1,800, slay orca/monster ~150-330) mostly acquire their depth *from the
war itself* after it starts. Verdict: actions are legible from map state, not
from relationship history.

**A6. Consequence persistence — WEAK (inverted).** Consequences never decay:
of relationships created by gambits, only `at_war_with` is ever culled (by
peace or generator spend). `betrayed_by`, `ward_against`, `corrupted_by`,
`cleansed_by`, `holds`, `controlled_by` — zero culls across all seeds; the
graph accretes permanent edges. Meanwhile positive live relationships decay
through maintenance drift: Along Meridian's founding `allied_with` bled from
0.72 to 0.09 to cull while a dozen of its wars raged. The world never forgets
grudge bookkeeping and never retains friendship.

**A7. Reciprocity and grudges — WEAK.** After an offense (raid, theft, haunt),
some hostile answer lands on the offender within 3k ticks in ~45-67% of cases
vs ~30% expected by chance. Above baseline but loose; the best payoffs are
real (orca Khkhrakhrdrrak, nine failed raids, finally succeeds against Dominus
Noatus — and is slain by that same penguin in the same tick), yet most
offenses dissolve into the general churn.

**A8. Pacing and escalation — HEALTHY (with an asterisk).** Inter-event CV for
entities with 5+ touches: median 1.26, ~40% bursty, almost none metronomic.
Dramatic involvement does cluster into episodes. The asterisk: a share of the
burstiness is war-flicker artifact (declare/peace/declare within tens of
ticks), which is clustering, but not escalation.

### C. Conflict and diplomacy

**A9. War believability — BROKEN.** 110-187 war episodes per 25k seed; median
duration 265-455 ticks; 16-25% last under 100 ticks. War/peace flickers
between the same pairs: up to 7 wars between one pair; 20-27% of war pairs
have repeat wars. Along Meridian (top faction) fought its own parent faction —
of which it remained a member — four separate times. 9% of war pairs are
kin/allies. One genuine bright spot hides in the endings: ~25% of ended wars
are culled by a generator, and tracing these shows they are truce-canon
births — `ice_settles_accounts` ended The Threshold's war at tick 8,630 in the
same event that created canon "The Hollow Truce" claimed by the exhausted
belligerent. Those are the most meaningful war endings in the system.
Mechanically the flicker loop is authored: declaration needs only contested
influence, peace scores on
inverse cohesion, and the contested ground persists after peace, so
declare→capitulate→redeclare cycles are structural. Nothing happens *during* a
war except strength drift verbs (Press the War / Let the War Cool) and
generator triggers — no battles, casualties, or territory transfer that a
chronicle could cite.

**A10. Alliance formation and durability — BROKEN.** 13-18 alliance episodes
per seed vs 110-187 wars (~10:1 wars). No gambit forms alliances; every
`allied_with` arrives as a generator birth-bundle. Alliances then either sit
inert until run end or decay to cull by drift. No sampled alliance ever
influenced a war (no joint declarations, no defense).

**A11. Bloc coherence — VACUOUS PASS.** Zero ally-of-ally-at-war triads, but
only because alliances are too rare and inert to form triads at all. There are
no sides in this world, so sides cannot be inconsistent.

**A12. Spatial coherence — HEALTHY.** Median hex distance between belligerents
at war start: 4-13, vs 10-50 for random faction pairs — wars are fought
between neighbors, matching the contested-influence precondition. Stolen
artifacts are physically carried away by thieves (`MoveTargetToSelfPhysical`).
This is the strongest narrative substrate in the current build.

### D. World memory and lineage

**A13. Artifact provenance — WEAK.** Chains exist: avg 1.4-1.6 holders per
held artifact, max 5, 2-10 artifacts per seed with 3+ holders, and beats can
recur (Firstkilltooth Of Grund twice defended against the same named thief).
But two mechanics behead the tales. First, "steal" requires the artifact to be
*unheld* — theft is scavenging; no artifact is ever taken from a living
holder, so every custody transfer is downstream of a holder's upkeep death,
never of conflict. Second, artifacts themselves age to death
(`LifecycleBand.Enduring`): ~70% of named artifacts die of upkeep mid-run,
ending their provenance in anticlimax.

**A14. Generational continuity — WEAK.** Generator-created entities arrive
with ~1.6 relationship links, but only ~25% connect to any entity that had
prior history; 75% of newcomers are contextless. Naming generators undercut
their own fiction: `wars_get_names` created "Floemaster Ying The Stormcaller"
as a war-faction member holding an artifact — who then never touched the war
and died of upkeep 3,800 ticks later.

**A15. Institutional memory — MIXED.** The two memory channels diverge
sharply. Canons are the healthiest narrative mechanism in the build: every
canon birth in seed 7100 is anchored to an entity with substantial prior
history (366-12,701 prior beats), and truce canons are born in the same event
that ends the underlying war — real institution-from-history causality.
`memorializes` is the opposite: of ~94-118 memorial relationships per seed,
**0-1** honor an entity that was dead at memorial time. Memorials commemorate
living, mid-story entities; the relationship currently means "is thematically
about", not "remembers". Canon fidelity to breach enforcement specifically was
not deep-audited (candidate follow-up).

**A16. Relationship sensibility — MIXED.** The 26-kind vocabulary is rich and
mostly sane in usage. Defects: 2-5 simultaneous `allied_with`+`at_war_with`
contradictions per seed; `faction_sues_for_peace` stamps `betrayed_by` on
every capitulation (authored deliberately — "the long memory of betrayal" —
but at 30-50 peaces per run it mass-produces permanent betrayal edges that
mean nothing); self-targeted "deepens the holding" gambits generate
actor==target beat noise in every biography.

**A17. Cast connectivity — WEAK.** Of ~330-365 named gambit pairs per seed,
only 9-14% ever recur and ~4% recur 3+ times. Recurring antagonists exist but
are rare; most encounters are one-shot pairings between entities that never
meet again.

## Long-Horizon Confirmation (1x100k)

Run `narrative-quality-1x100k-20260712` (seed 7200) confirms every 25k
finding is structural, not a bootstrap artifact:

- **Careers never lengthen.** Penguin median life 2,845 / p90 3,585 / max
  5,250 — out of 100,000 ticks. Max successful-gambit touches for any of 434
  named penguins: 8, identical for penguins born in the first half and second
  half of the run (avg 3.4 vs 3.6). Generations of interchangeable short
  careers; the age mechanics guarantee it at any horizon.
- **War flicker is the steady state.** 648 war episodes (~162 per 25k, the
  same rate as the short runs), median 360 ticks, 144 under 100 ticks.
- **Alliances stay rare and inert.** 72 episodes over 100k — the same ~18 per
  25k rate — with 70 still open at run end and no bloc structure emerging.
- **Permanent-edge accretion is linear and unbounded.** The never-decaying
  kinds (`betrayed_by`, `ward_against`, `corrupted_by`, `cleansed_by`) gained
  229-312 edges per 25k quarter at a constant rate: 1,055 created vs 9 culled
  across the run. All 165 betrayal episodes remain open. The graph fills with
  dead bookkeeping forever.
- **Institutions do scale.** Settlements reach 87,260-tick lives
  (near-whole-run continuity) and factions 27,440 with up to 56 touches — the
  backdrop layer is capable of long history; the character layer is not.

## Ranked Failures and Owning Layers

1. **The upkeep reaper beheads every arc** (A2, A4, A13, A1). Monsters with
   hunters, artifacts with histories, penguins with careers — all end in
   `? [upkeep]`. Owning layer: authored domain (lifecycle bands + effects
   balance give age-death near-total dominance over story-death). The
   companion telemetry gap (null death-reason symbols) is
   Engine.Observability/domain symbol registration.
2. **Protagonist kinds cannot carry a story** (A1, A3). Brief/Standard bands
   for penguins/orcas vs Enduring for scenery inverts the lore's focus.
   Owning layer: authored domain entity definitions; also touches the design
   principle that named entities are the heroics.
3. **War is a flicker, not a saga** (A9, A5). No history precondition, peace
   on inverse cohesion, contested ground persists, nothing occurs during
   wars. Owning layer: authored domain (gambit preconditions/score/cooldown +
   missing during-war expression).
4. **Alliances have no author** (A10, A11). No alliance-forming gambit;
   positive-edge drift kills what generators seed. Owning layer: authored
   domain (gambit vocabulary + relationship maintenance polarity asymmetry).
5. **History does not gate action** (A5). Most gambits need no prior
   relationship between actor and target even where the fiction implies one.
   Owning layer: authored domain preconditions (`SelfHasRelationshipToTarget`
   exists and is simply unused by most gambits).
6. **Memory vocabulary is inverted** (A6, A15, A16). Permanent negative
   bookkeeping accretes; memorials honor the living; betrayal is a peace
   byproduct. Owning layer: authored domain (generator targeting + maintenance
   config + sue-for-peace mutation scale). The canon channel shows the correct
   pattern and should be the template.
7. **Theft that takes nothing** (A13). Steal/wield require unheld targets, so
   ownership never changes hands under pressure. Owning layer: authored domain
   gambit preconditions.
8. **Newcomers arrive contextless** (A14). 75% of generated entities connect
   to nothing with history. Owning layer: authored domain generator bundles.

No fixes were applied. Each item above is a candidate for a separate scoped
proposal; several (1-4) interact and should be sequenced deliberately.

## What Is Working

Worth protecting during any remediation: spatial war grounding (A12), physical
artifact transport, canon births from war exhaustion (A15), bursty pacing
(A8), retaliation above baseline (A7), the relationship vocabulary's
expressive range, and the existing capacity of the event stream to produce
genuinely tellable beats when mechanics align.

## Appendix: Exemplar Reconstructed Timelines (seed 7100, condensed)

**The dragon that died in bed — monster "Oorroo'hrosh, The Echo (sung)".**
Born t7505 by `corruption_wakes_monsters` at Longa Hollowspan. Steals
Polar-lyre Of Gleam (t7980), Lucnivnocum's Bonekey (t9615), ☽'miraeus'
Bone-flute (t13130). Haunts and corrupts transport Tide-lantern (t11315).
Warded by four transports, bound by three artifacts. Survives six
`penguin_slays_monster` attempts by two named hunters (Plungecaller Mou ×3,
Qingshangzhei ×3, t9790-12770). Dies t13300: `? [upkeep]`. All hunters alive.

**The war that would not mean anything — faction "Along Meridian (sunward)".**
Born t11380 by generator as member of Lianning Frostlings; declares war on The
Threshold 5 ticks after birth with no prior contact. Wars with The Syndicate
declared four times (t12185, 13480, 14995, …), the first lasting 10 ticks
before a peace that stamps `betrayed_by`. Fights its own parent faction
Lianning four separate times (t17295, 18360, 18915, 20480) while remaining a
member. Its one founding `allied_with` drifts 0.72→0.09→cull unnoticed
(t14015). Dies t23030: `? [upkeep]`.

**The raid that finally landed — orca "Khkhrakhrdrrak Grundgruk".** Born
t15005 with home base Bonecrossing. Attempts `orca_raids_trade_route` ten
times against ten unrelated targets, failing nine. At t22875 the tenth raid
succeeds against Dominus Noatus☽ Voidbanisher — who slays him in the same
tick. The one genuinely reciprocal death in the sample, embedded in nine
zero-history dice rolls.

**The law born from exhaustion — canon "The Hollow Truce".** The Threshold's
war with The Meridian, re-declared at t8485, is culled at t8630 by
`ice_settles_accounts` in the same event that creates the canon, `claimed_by`
The Threshold. Institution-from-history causality working as intended.
