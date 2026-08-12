# Backlog

Unified work backlog, consolidated 2026-08-03 from the actor-parity
audit, the culture balance problem register
(`docs/analysis/culture-balance-problems-20260801.md`), prior plans,
and the previous version of this file. Last swept 2026-08-12. Point-in-time findings stay in
`docs/analysis/`. Verification-only and hardening-only items are
excluded by charter; completed verification history lives in the
analysis records.

**Culture balance status (owner call, 2026-08-03): mostly solved.**
The kind-general build holds the census inside the 60/20/20 envelope
with rotating leaders across worlds and genuine within-world contests
(9x30k evidence: `docs/analysis/kindgen-30k-overtime-20260803.md`).
Remaining balance-adjacent items below are polish, defects, and
long-horizon observations - not an open balance problem.

## Actor authorship parity (continuation - polish, not balance-critical)

Origin: `docs/analysis/actor-parity-audit-20260801.md`. The parity
slices landed 2026-08-01/03 (shared band/locality/traits, kind-general
generators with target-kind production, joint-pool casualty bindings)
and closed the balance goal. Remaining:

- **Occupation produce sites: kind-general conversion** — the ~40
  event-placed penguin produce sites (witness/broker/warden at war
  sites, offering sites, rich hubs) can use the kind-general machinery
  (second target kind, gated produce slots) where the fiction is
  kind-arbitrary. The former "homing" half of this item is withdrawn:
  measurement (2026-08-03) showed birth location is nearly irrelevant —
  actors travel ~500 hexes over a life and survival tracks the
  culture's territorial coverage, not placement.
- **Grant-gambit parity** — DONE 2026-08-03: the nine penguin-only
  rites are shared builders emitted per kind, plus the upstream
  acquisition links (Wield/Wrest artifact, AbilitySharesPractice with
  orca practitioners). Verification world (seed 9200, 10k): orca
  wrests 201, wields 18; artifact-gated orca rites fire (vessel 2,
  tome-binding 19, deep current 2); ability-gated orca rites now have
  a practice path to open them. unmakes_the_binding and
  turns_the_battle fired for neither kind at 10k (seal/weapon custody
  plus war context is rare that early) - recheck at 20k.
- **Endings and rites by kind** — orcas have authored passage content
  (elders_descend_to_the_god, kills_sung_to_the_deep,
  vigils_at_the_current); penguins die more often and have none. Audit
  whether that is culture flavor or a kind gap; penguin deaths
  currently feed no memory/devotion economy.
- **Non-grant gambit pairing** — penguins hold ~29 gambit actor roles,
  orcas 8. Needs the classify-then-pair pass the generators got.
- **Orca behavior traits** — after the bond-pair fix, penguins have
  five traits to orcas' two; orcas have no verb-weight behavior traits.
- **Subtype vocabulary** — penguin subtypes are all occupations, so
  fledglings are born as "witness". Decide: subtypes stay per-kind
  flavor, or actor kinds share a life-stage/role vocabulary.
- **Name Forge subtype naming coverage** (absorbed from the name-forge
  tracker, tail open since May): split Nightshelf places, transports,
  and monsters with per-subtype incomplete-name markers; split Aurora
  settlement/region/point-of-interest naming by function and terrain;
  give artifact, occurrence, penguin, orca, and faction subtype
  families focused route sets; re-run 100-name samples per kind/subkind
  after each culture pass.

## Culture balance

Origin: problem register, round-16 arc.

- **World-culture shelter** — world-culture entities hold zero
  shelter-grade cells on every measured world by construction (no
  territory by design) and die at the highest rates. Design fix
  candidate: world entities shelter under any culture's influence.
- **Aurora desperation beacon placement failures** — the surge still
  fails at fire time (placement, rejection code 8) on worlds where
  Aurora is already collapsed; the frontier fallback is insufficient
  when the culture has no viable ground.
- **Dead generators** — `faction_claims_the_warm_ground` and
  `penguin_shelters_at_the_vent`: zero attempts across all seeds at the
  full horizon (rounds 4-9 sign-off). Fix or retire the authored
  content.

## Long-horizon lifecycle (new from the 9x30k net, 2026-08-03)

Origin: `docs/analysis/kindgen-30k-overtime-20260803.md`.

- **Succession-anchor accretion** — every the_line_continues launch
  founds a Monumental-band (20k-tick) point of interest; anchors
  outlive their founders and accumulate (204 POIs on the crashed world,
  ~2.7x the 20k norm), driving population re-growth in the final third
  on 6 of 9 worlds and densities above the 0.3-0.5 band on heavy
  worlds. Candidate shapes: shorter anchor band, anchor reuse before
  founding, or founding gated on local anchor count.
- **World-culture accumulation** — world-culture populations grow
  monotonically on every world (e.g. 11 -> 93 over a run): accords and
  monuments have no retirement pressure. Decide whether the world's
  own should also pass into history.

## Track economies

Origin: track portfolio rebalance (2026-07-31), register.

- **Cross-culture trade routes** — they do not exist, which blocks
  `envoys_cross_the_ice` (authored, soil-blocked) and caps the trade
  and diplomacy tracks. Needs a mechanism for routes to cross culture
  lines (border markets, corridor brokering, or similar).
- **Rivalry pacing** — accords cool wars and now spend rivalry, but the
  rivalry mill that re-declares wars has had no pass of its own; war
  prevalence is acceptable, war cadence is untuned.

## Deferred by explicit decision

Carried from the previous backlog; origins as noted there.

- **Typed installed-legacy component** (owner decision, 2026-07-24;
  design in `ordered-emergence-population-plan-20260718.md`). Fungible
  legacy is the chosen representation until districts need the typed
  fixed-capacity attachment. Binding constraint either way: installed
  benefits stay attributable and reversible.
- **Artifact resurrection / return from the zeitgeist** (owner note,
  2026-07-18). Rediscovery of historical relics instead of minting new
  ones, or un-installation of an enshrined legacy. Requires the legacy
  data shape above; not separately approved yet.
- **Dynamic meta-entities** — explicitly not a requirement; superseded
  unless clutter persists.
- **Arrival-time repulsion** — resting entities refusing occupied
  hexes; fire only if the stacking tail stays fat at horizon.

## Future systems

- **Knowledge grant model redesign: enablement gates** — decided
  2026-08-03: the system stays and the grant model is replaced (design
  recorded in `docs/design/cultural-expansion-and-knowledge.md`, Grant
  philosophy section — read it first when resuming). Most techs become
  gates (movement through terrain classes, air movement mode, gambit
  unlocks, generator unlocks, subkind-to-all-subkinds target widening
  — examples of the kind, not a list; see the gate design space in the
  doc); multipliers survive only at ~2x / ~0.5x scale. Kernel
  primitives landed 2026-08-03: CultureHoldsTech gambit precondition,
  generator RequiredTech / TechWidensSubtypes target gates, and
  tech-gated movement profile rules, all resolving the
  GameplayDefinition.KnowledgeTechIds catalog. Catalog authored
  2026-08-08 (commit 9373bf68): eleven pure gates across twelve
  gambits and five generators, eight survivors at x2/x0.5, four
  lore-grounded renames; verification world seed 9300 showed gates
  opening per claimed spoke. Remaining: a scorecard section joining
  claims to gated-content usage, and judging the recut on a standard
  20k fan (see the calving watch under Open design questions).
- **Natural wonders (physical parallel to tech sites)** — future
  system, same grant engine: static wonder sites on the physical
  plane, owned while the culture's physical field holds them (same
  radius/band claim test, plane switched); discovery via exploration
  and the growing map, loss via retreat. Candidate anchors: the
  deferred stable named geological feature ids.
- **Vertical axis overhaul: make subsurface culture real** (2026-08-03).
  The lore intent is Nightshelf pockets of concentrated culture beneath
  Aurora surface territory. Measured (seed 9100, tick 30k) that never
  materialized: Nightshelf held 19 mass in 2 tunnel cells vs 27k on the
  surface, while Aurora's spires held 1.1k — forging deposits one fixed
  dose, per-layer decay eats it, and no reinforcement, placement, or
  emission loop feeds z != 0. Since the culture power ledgers were
  deleted, z-layer culture mass has zero consumers; it leaks into the
  sim only through two missing z-filters (frontier placement seeding,
  world-beat mass total), which are defects under any design. Overhaul
  shape discussed: own-culture support and shelter at a column read
  surface plus own subsurface mass (tunnels shelter Nightshelf without
  contesting Aurora's surface ownership), plus a feed loop — tunnel
  dwellers' local emission becoming visible, or the shadow economy
  depositing into galleries. Beneficiary: Nightshelf, currently
  mid-pack. Alternative if the identity is judged already carried by
  the semantic plane (215k semantic vs 27k physical, 8:1): collapse
  culture storage to z = 0, which also removes both leaks. Spires,
  tunnels, and vertical movement are a separate live feature either
  way.

## Deferred ideas (post-MVP, absorbed 2026-08-03)

Absorbed from `docs/design/post-mvp-backlog.md` and the root round
trackers, both now deleted. These are deferred ideas, not commitments.

### Card system and player agency

- Deck/hand/tableau loop with draw cadence, hand limits, costs, and
  discard; Balatro-style layered card modifications; archetypes that
  select a starting theory of the world.
- Hidden-trigger cards visible in the tableau but obscuring exact
  timing; one-shot intervention cards that break the indirect-control
  rule at a visible cost.

### World-shaping systems

- Macro system cards that change broad conditions without becoming
  hidden low-altitude planners; weather, seasons, and terrain-changing
  events; religious or ideological systems if representable without
  hidden director logic.

### Entity kind follow-ons

- **Monster status mechanic** — sealed/unbound/risen as a status field
  or containment-topology relationship, not a subtype (subtypes stay
  types: leviathan, wraith, beast, revenant).
- **Ability transmission attribution** — richer ability-level
  attribution on `practiced_by`/`taught_by` if gambits ever need to
  distinguish which ability passed through which teacher.

### Simulation depth

- Richer history-aware relationship maintenance; occurrence entities
  tying long-running wars, plagues, crusades, or booms together.
- Multi-resource economy only if resources produce decisions rather
  than inert telemetry; culture innovation and spread.
- Culture-specific medium-term goals — authored, visible strategic
  priorities beyond static slot occupation, eventually readable by
  generators/gambits.
- Deeper cultural cohesion and front behavior on top of boundary,
  slot, low-altitude influence, and minor-actor systems; better
  procedural map/seed diversity.
- Longevity-grant sources beyond artifacts and abilities: canons,
  settlements, occurrences (post-R5 note from the narrative-quality
  rounds).

### World generation and geography

- Vegetation and cover classification (forest, marsh, scrub,
  climate-derived aridity) as a later domain layer that does not
  replace landform, substrate, rivers, or magic features.
- Pre-Collapse archaeology seeding (roads, settlements, megaliths,
  weapon sites) into the mature temperate landscape before the Deep
  Freeze overprint; must not compensate for defective natural
  geography.
- Stable named geological feature ids beyond rivers/lakes (ranges,
  peaks, plains, basins, waterfalls, craters); kernel accessor only
  when a gameplay consumer exists.
- Higher-fidelity plate/terrane model only when a concrete gameplay or
  visual need warrants it; physical-frontier growth pacing as runtime
  balance work, separate from latent map capacity.

### Map and UI

- **Fog of war / unrevealed water-glass presentation** (was the last
  open item gating "the second wave of authored content"): both planes
  start partially unrevealed; unrevealed cells use the stained-glass
  pane art, revealed terrain takes precedence; needs reveal-state
  design in projection/UI and possibly kernel events; keep separate
  from semantic terrain readability fixes.
- Stronger overlays for culture influence, activity, relationship
  networks, and generator/gambit affordances; post-run summaries;
  entity search and bookmarks; replay and branch comparison; improved
  art and animation.

### Tooling

- Faster Monte Carlo runs; automated simulation-quality reports; card
  balance sweeps; replay from seed/config. (The old "save-state
  diffing" idea presumed the checkpoint writer deleted 2026-08-03; a
  future version would need to reintroduce state saves deliberately.)

## Audit follow-ups (2026-08-03 deletions)

- **minor_actor_work_events lost its only reader** — the batch
  analyzer was its sole codified consumer; the lane still writes ~14M
  rows per 30k world. Decide: delete it like the other unread lanes,
  or keep it as the one ad-hoc window into minor-actor work. It
  survived the first cut only because it was not on the approved list.

## Kernel (owner lane)

- **Movement-array crash, seed 9105** — growing map minted more cellIds
  than the fixed movement arrays hold (131072);
  MovementProgramState.RecordTravel index-out-of-bounds ~tick 14k.
  Routed to the owner's kernel/movement workstream.
- **Telemetry publisher hard-faults under load** (9x30k net,
  2026-08-03) — KernelEventPublisher.TryWrite throws when the event
  channel saturates instead of degrading; seed 9100 crashed at ~tick
  22.5k after a dropped-rows warning phase. Pressure dropped sharply on
  2026-08-03: the kernel no longer publishes per-candidate generator
  target evaluation events (the largest lane, ~16M events per 30k
  world) — the fail-hard behavior itself is still the owner's call.

## Open design questions

- **Wake-Singer calving now sits behind Deep tier 3** (tech recut,
  2026-08-08) — the_current_provides requires pressure_diving; in the
  10k verification world (seed 9300) that gate stayed shut, so no pods
  calved from devotion. If Wake-Singers do not reliably claim the Deep
  spoke by mid-horizon on the standard 20k fan, their reproduction
  path thins - watch census and the devotion economy on the next fan,
  and consider dropping the gate to Deep tier 1-2 if it starves them.

Undecided directions surfaced by the balance arc; no commitment
implied.

- **Nightshelf semantic lead fades** — they lead the semantic plane in
  window 1 on 8/9 baseline runs and hold it at horizon on 1/9. Problem
  or intended asymmetry?
- **OfferedTo net-positive growth** — the devotion economy rests on
  contact renewal slightly above decay (+0.0006 tended); owner is
  lukewarm on positive relationship growth as a pattern.
- **Mass faucets beyond entity emission** — deeds depositing influence
  at sites and relationships emitting along corridors (the ice
  remembers what happened, not only who stands there). Would also give
  world culture a footprint. Candidate only; the authorship-parity
  direction was chosen first.
- **Are 60%+ excursions acceptable?** (9x30k) — census balance is
  durable but not bounded: one world ran Nightshelf to 77% for four
  windows before receding to 62%. Dramatic temporary hegemony may be
  desirable fiction; decide before any bounding mechanism is
  considered.
- **Wake-Singer long-horizon fade** — smallest culture at the 30k
  horizon on 3 of 9 worlds plus a tie, leading only one; horizon means
  ~31/35/24 (A/N/W). Watch whether rotation stays healthy or becomes a
  slow slide against the orca culture.
