# Low-value systems survey — 2026-08-03

Standard applied: a system earns its keep only if some decision loop consults
it — it changes simulation outcomes, it is read by the active measurement
workflow (mc-fan + `tools/analysis/mc_scorecard.py`), or it is a player-facing
surface of the game. "Something references it" does not count. This supersedes
the reference-based framing of `deletion-survey-20260803.md`.

Evidence: full consumer trace of every exported telemetry table, kernel-level
trace of the z-dimension culture field and the knowledge modifier sites, and
row counts from the latest 30k-tick run (`kindgen-r20-30k-20260803-s9100`).

> **Outcomes (2026-08-03):** owner selected items 1, 4, 5, 6, and 7 for
> deletion; all five are DELETED. Item 2 (sparse-z culture field) and item 3
> (knowledge/tech system) remain as described, undecided. The generator
> target evaluation kernel publish was removed with item 5, so the
> evaluation-lane pressure on the telemetry publisher is gone. The
> MonteCarlo.Tests output-gap acceptance ledger (env-gated, analyzer-era
> contract) was deleted with item 4.

## 1. Culture power ledgers — tracks nothing consults — DELETED

Built 2026-07-11/12 as the per-culture win-definition system (Witness /
Shadow / Wake axes). Components:

- `src/Engine.Host/KernelHost.CulturePower.cs` — full culture-field scan,
  recomputed every 100 ticks in the game runtime and per telemetry cadence in MC
- `src/Engine.Projection/CulturePowerSummary.cs`
- `culture_power_snapshots` telemetry (18,004 rows/world) — read by nothing;
  the analyzer registers it only as an empty schema stub
- Legend panel nations rows + `MapArea.cs` read model plumbing
- `CulturePowerConfig` vocabulary in `DomainConfig.cs:214` +
  `IceRemembersDefinition.CreateCulturePowerDefinition`

Empirical check at tick 30,000 of seed 9100: Aurora led two of the three axes
(Witness 61%, Wake 64%); Nightshelf led Shadow 46% (Aurora 40% close behind);
Wake-Singers led no axis. The design promise "each culture leads its own axis"
does not hold, and it was never noticed because no measurement reads the
ledgers. The devotion-economy and shadow-economy generators created in the
same round affect the simulation directly and are judged separately in the
generator review — they do not go down with the tracks.

## 2. Sparse-z culture field — kernel plumbing whose stated purpose is the ledgers

The culture influence field carries z layers −3..+3. The design comment at
`Simulation.CultureInfluence.cs:37-39` states the intent: forged-cell mass at
z ≠ 0 "feeds the culture power ledgers directly and must not perturb surface
facts". The trace confirms the firewall: locality, borders, contest, crowding,
shelter, claim bands, verb targeting, and relationships all read hardcoded
z=0 accessors; no kernel decision path branches on z.

Costs that remain if the ledgers are deleted:
- Storage and per-tick decay work for every z ≠ 0 cell (decay enumerates and
  maintains all layers)
- Writer plumbing: forging deposits culture mass at forged z; entity emission
  deposits at the entity's z; the parallel apply path carries z through
  `CultureDelta` / `CultureStorageKey`
- Remaining consumers after ledger deletion: hex tooltip spire/tunnel display
  (which comes from map cell events, not culture mass) and telemetry z columns

Two accidental leaks where z ≠ 0 mass does reach simulation outcomes — both
look like missing z filters, not mechanics, and are defects regardless of the
deletion decision:
- `GeneratorProgramRuntime.PlacementTerrain.cs:67-68` — frontier placement
  enumerates culture cells with the z-collapsing cursor, so spire/tunnel mass
  qualifies a column as a placement seed as if the culture held the surface
- `WorldState.MapLayers.cs:810-814` — the global physical mass total sums all
  z layers and feeds world-beat age gating
  (`WorldBeatsProgramRuntime.cs:90`)

If the ledgers go, collapsing z out of the culture field is the natural
follow-on: it removes dead storage/decay cost and both leaks at once. Forged
terrain columns themselves (spires/tunnels as map features) are a separate
system and unaffected.

## 3. Knowledge/tech system — complete wiring, negligible measured effect, parked

27 techs, 92 grants, 15 genuine kernel application sites (generator fire
weight, gambit score/success, verb weight, culture production/persistence,
relationship maintenance, movement, roads, minor-actor work, frontier
thresholds, carrying capacity). The wiring is real. The value is not:

- Magnitudes cluster at ×1.12–1.30, applied on top of systems that already
  stack age multipliers, trait totals, chronicle bonuses, and decision deltas;
  generator fire weight additionally averages the modifier across eligible
  target cultures, diluting a single culture's tech to near-nothing
- Coverage is thin: ~10 of ~85 generators, 10 of 75 gambits, 25 of 180 verbs
- Claiming is automatic: a site is claimed when the culture's semantic field
  reaches raw mass 30 within radius 2 — no cost, no steering, no decision.
  Cultures hold 8–11 techs by tick 2,500 of 30,000; counts then wobble as
  field mass shifts (silent unclaiming)
- Tiers 3–4, which hold nearly all gambit grants, never activated in the
  recorded evidence run; that run's generator fire count was identical to the
  no-grants baseline (`docs/design/cultural-expansion-and-knowledge.md:83-99`)
- `knowledge_snapshots` (16,704 rows) and `knowledge_claim_snapshots`
  (146,056 rows) are read by nothing; `knowledge_site_symbols` likewise
- The technology overview panel is display-only; no test asserts a claimed
  tech changes any outcome

The system was already parked once (semantic plane reframe kept it as a
future foundation). Options: delete end to end (kernel program, host
snapshot, telemetry, UI panel, 27 authored techs), or keep only if a later
pass makes tech claims a real decision with macro-scale effects. As
implemented it is the largest single ignored system in the program.

## 4. Monte Carlo batch analyzer — bypassed by the active workflow — DELETED

`make mc-fan` — the standard batch path — passes `--no-analysis`; scoring is
done externally by `mc_scorecard.py`, which reads 11 tables. The analyzer
(4 partial files plus report/flag/readiness machinery and its test suite)
runs only under plain `make mc`, which the balance campaign did not use. It
is the sole real reader of ~15 telemetry tables, so its existence makes those
lanes look consumed when nothing in practice runs it. Its maintenance cost is
real: every telemetry schema change this month required analyzer and
gap-test fixes. It also still carries references to tables whose writers were
deleted on 2026-08-03 (`generator_state_snapshots` in RequiredTables,
`cell_influence_events` in OptionalRawTables/readiness).

Options: fold the few sections with proven value (exploration flags caught
the 2026-07-22 measurement traps) into the scorecard and retire the rest, or
keep it and accept that the fan path never runs it.

## 5. Telemetry lanes consumed by nothing — DELETED

Row counts from seed 9100, 30k ticks (~119M rows total). "Count-only" means
the analyzer's readiness roll-up counts rows but derives no metric.

| Table | Rows | Actual readers |
|---|---:|---|
| generator_target_evaluations | 15,935,952 | count-only |
| attribute_events | 11,543,996 | count-only |
| culture_entity_field_snapshots | 5,682,002 | tests only |
| minor_actor_lifecycle_events | 3,333,244 | schema stub only |
| minor_actor_encounter_events | 1,134,628 | schema stub only |
| relationship_snapshots | 580,428 | count-only |
| knowledge_claim_snapshots | 146,056 | nothing |
| culture_power_snapshots | 18,004 | schema stub only |
| knowledge_snapshots | 16,704 | nothing |
| lifecycle_reason_events | 2,665 | count-only |
| chronicle_events | 1,329 | nothing (UI reads live events, not telemetry) |
| culture_slot_occupancy_events | 387 | count-only |
| entity_tag_events | 128 | schema stub only |
| world_beat_events | 58 | nothing (UI reads live events) |

That is ~38M rows (~32% of all telemetry written) with no reader at all, and
another 14.2M (`minor_actor_work_events`) whose only reader is the bypassed
analyzer. `generator_target_evaluations` is also the largest write lane and a
known contributor to the telemetry publisher saturation that crashed seed
9100 at 30k. The mechanics behind the minor-actor, chronicle, and world-beat
lanes are live (simulation- or UI-consumed); only their telemetry is dead
weight.

## 6. Monte Carlo state checkpoints — writer with no reader — DELETED

`--save-state-every` writes `seed-N-tick-T.kstate` files (used in recent fans
via `MC_EXTRA`). No code anywhere loads a `.kstate` file: the only
`LoadKernelState` caller is the pregenerated-world path in
`KernelHostBuilder`. The checkpoints written during the 9×30k net were
insurance that could never have been redeemed.

## 7. Experiment variant machinery — an A/B scaffold that never grew — DELETED

Every run writes one hardcoded `experiment_variants` row
(`ExperimentId: 1, VariantId: 1` — `KernelTelemetryRecorder.cs:40`). There is
no CLI to define a variant, no comparison logic, no second variant ever
recorded. The analyzer reads the table for readiness only.

## Minor notes

- Canon breach machinery is live but rare: 128 tag events across a 30k world
  (oathbreaker/profaner damping). Context for the ongoing generator review,
  not a deletion call here.
- `CellCultureInfluenceChangedKernelEvent` carries a z slot and is never
  constructed by the kernel — dormant eventing shape, tests only.
- Five completed round-tracking docs sit at repo root (`TRACKING-*.md`) —
  clutter, not systems.
- `tools/mutation` (Stryker config, last touched 2026-07-20) is dev tooling
  outside any loop; zero runtime cost.

## Checked and cleared (live by the value standard)

Chronicle director + journal/aura UI (product narrative payload), world beats
(recent design work, UI strip/panel), minor-actor mechanics (ambient churn is
simulation-affecting), roads, coverage/terrain transforms (war scars), culture
slots, tableau + game intent port (player agency path), 3D microgeography and
the other panels wired in `GameRenderTree.cs`, NameForge/WorldGenerator (build
pipeline), knowledge grant *read sites* (all 15 verified real — an earlier
claim that the thaw-threshold grant was orphaned was checked and found wrong:
it is read at `EffectsProgramRuntime.cs:636` and
`LowAltitudeVerbProgramRuntime.Candidates.cs:903`).
