# Backlog

Unified work backlog, consolidated 2026-08-03 from the actor-parity
audit, the culture balance problem register
(`docs/analysis/culture-balance-problems-20260801.md`), prior plans,
and the previous version of this file. Point-in-time findings stay in
`docs/analysis/`. Verification-only and hardening-only items are
excluded by charter; completed verification history lives in the
analysis records.

## Actor authorship parity (continuation)

Origin: `docs/analysis/actor-parity-audit-20260801.md`. The first
parity slice landed 2026-08-01 (shared band/locality/traits, shared
succession, nine paired generators) and brought the census inside the
60/20/20 envelope. Remaining, in leverage order against the open
register problems (Nightshelf death rate ~1.7x Wake-Singers; Nightshelf
births 58-84% on unheld cells):

- **Occupation produce sites: kind-general conversion** — the ~40
  event-placed penguin produce sites (witness/broker/warden at war
  sites, offering sites, rich hubs) can use the kind-general machinery
  (second target kind, gated produce slots) where the fiction is
  kind-arbitrary. The former "homing" half of this item is withdrawn:
  measurement (2026-08-03) showed birth location is nearly irrelevant —
  actors travel ~500 hexes over a life and survival tracks the
  culture's territorial coverage, not placement.
- **Grant-gambit parity** — 9 of 13 grant gambits are penguin-only
  (UnmakesTheBinding, CallsTheDeepCurrent, TurnsTheBattle,
  BindsWhatWasLoosed, KeepsWintersPatience, MendsAnother,
  PassesTheTeaching, ReadsTheIce, StillsTheWaters). The shared-builder
  pattern exists in the same file (DrawOnTheVessel, MakesOffering).
  GambitId enum ids append at the end (positional name mapping).
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

- **Knowledge / tech system** — parked design
  (`docs/design/cultural-expansion-and-knowledge.md`; Civ model,
  current state, and open questions recorded there). Read that doc
  first when resuming.

## Kernel (owner lane)

- **Movement-array crash, seed 9105** — growing map minted more cellIds
  than the fixed movement arrays hold (131072);
  MovementProgramState.RecordTravel index-out-of-bounds ~tick 14k.
  Routed to the owner's kernel/movement workstream.

## Open design questions

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
