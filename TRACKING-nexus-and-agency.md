# Nexus Formation And Player Agency Tracking

Date: 2026-07-11

Baseline commit before this tracker: `f33f5a5 Improve chronicle and entity browser controls`.

This file records the diagnosis of why culture nexus/hub geography never forms,
plus the approved implementation round for nexus physics and player agency.
The user approved implementing all items without dedicated design sessions.
Checkboxes are updated only when the work is implemented and verified.

## Diagnosis: The Field Is A Plateau, Not A Peak

Goal state: each of the three social cultures forms a nexus+hub geographic
model on the physical plane, with clear culture boundaries that can ebb and
flow, serving as the player's macro focus.

Observed failure: culture influence stays diffuse at every tuning point.
Retuning `CultureInfluenceActionScale` only moves the plateau up and down
between "everything is nexus" (scale 1.0) and "nexus almost never" (0.15).

Hub formation needs three ingredients working together: superlinear local
reinforcement (rich get richer), global decay (unsustained mass dies), and
centripetal sources (producers orbit a home). The current system has the
opposite of all three, so diffuseness is the equilibrium, and parameter
changes move the system between saturation regimes instead of producing
different worlds. This is also the root of the general brittleness: with no
sinks, the only stabilizers are caps and scale factors.

Ranked causes:

1. **Raw culture mass never decays.** Mass changes only through explicit
   additions and enemy cleanup (`Simulation.CultureInfluence.cs`,
   `WorldState.MapLayers.cs`). Every trace persists forever; the field is a
   ratchet that can spread but never contract. "Ebb and flow" is structurally
   impossible because nothing can ebb.
2. **Scoring actively flattens peaks.** `CultureInfluenceTargetScorer`:
   `SpreadUtility` is zero once a cell reaches controlled (100 raw);
   `ReinforceUtility` scales by shortfall toward the cap. Minor actors use the
   same boundary scores. All marginal production is redirected outward to the
   frontier. The cap (1000) is only 1.43x the nexus threshold (700), so peaks
   have almost no dynamic range.
3. **Mobile producers are unanchored.** Influence verbs deposit around the
   actor's current position; most movement is unanchored `Explore` wander.
   `MovementProgramRuntime.Wander` has an anchored overload with a leash
   radius, currently unused (`maxDistanceFromAnchor: -1`). `HomeBase` is only
   a soft fitness weight on one penguin verb.
4. **Generators mint permanent satellites.** Target selection is
   graph-topology-only; culture scoring applies only after target selection
   inside the local candidate area; `Across` placement founds same-culture
   static clusters 20+ hexes out. With no decay every satellite persists and
   emits workers.
5. **Nexus is a label, not a mechanic.** The nexus band is consumed by the UI
   tooltip and one placement constant. No self-reinforcement loop makes one
   center outcompete its siblings; no winner-take-all pressure selects a
   capital.

### Evidence

Run id `culture-nexus-physical-5x25k-20260522015457` (5x25k, commit 7245e03),
physical plane, tick 25000:

- Components per culture (5-run avg): 41.2 / 58.4 / 44.8; contested cells
  234-312 per culture.
- Run 25000 final field: top-10 cells hold 4.7% / 5.6% / 0.9% of their
  culture's total mass; culture 3 has 471 cells pinned at the 1000 cap and
  782 nexus-band cells (a nexus "band" covering a subcontinent, not a hub).
- Mass-weighted mean distance from each culture's own centroid: 17-33 hexes.

`culture-scale-015-trace-1x5000-20260523` (post-scale-0.15) shows the same
shape at lower altitude: physical nexus entries 46, semantic 1 — the plateau
lowered below the nexus threshold instead of concentrating.

## Approved Actions

### A. Tooling

- [x] **A1. Fix MC CLI build.** `MonteCarlo.csproj` fails on NU1902/NU1903
  warnings-as-errors for MessagePack 3.1.4 advisories via `Engine.Domain`.
  Upgrade MessagePack and refresh lock files so registry/append-stats work.
  Done 2026-07-11: MessagePack 3.1.4 -> 3.1.8 in `Engine.Domain.csproj`;
  MC CLI builds clean and `list` works.

### B. Nexus physics

- [x] **B1. Add raw culture mass decay** (owning layer: `Engine.Kernel`).
  Slow global decay per plane so territory reflects present activity;
  satellites and wander-trails fade unless sustained. Must respect
  microkernel constraints: batched/lazy application, no hot-path allocation,
  boundary dirty marking. Tuning constant in `KernelTuning`.
  Done 2026-07-11: `CultureFieldDecay` program (code 17) applies
  multiplicative decay with a minimum-per-pulse floor and cull threshold,
  defaults `decayPerPulse=0.005`, `minimumDecayPerPulse=0.6`,
  `cullFloorRawMass=1`, cadence 5 main ticks (25 sim ticks). Two-phase
  scratch iteration (no hot-path allocation), mutations flow through
  `ModifyCellCultureInfluenceRaw` so boundary dirty marking, share recalc,
  and events stay consistent. Tuning exposed via JSON/TOML startup config
  and persisted in kernel snapshots (format v40). 5 unit tests.
- [x] **B2. Make concentration self-reinforcing** (owning layer:
  `Engine.Kernel`). Remove the hard headroom-zero for consolidate/reinforce
  targeting; add component-mass gravity preferring the largest connected
  component's core; give peaks dynamic range (raise cap after decay exists);
  make nexus band mechanically meaningful (slot benefit / minor actor
  emission multiplier inside nexus cells).
  Done 2026-07-11: `ReinforceUtility` keeps a 0.3 positive floor near the cap
  and gains `ComponentGravityBoost` (+75% inside the culture's largest
  component; wired through low-altitude and minor-actor attachments).
  `CultureFullRawMass` raised 1000 -> 2000 so peaks have ~3x nexus dynamic
  range. Minor actor sources standing in an own-culture nexus cell get
  `NexusSourceCultureMultiplier` 1.5x work delta — the nexus band now feeds
  back mechanically. Obsolete anti-concentration test updated; maturity-based
  test expectations now derive from the constant.
- [x] **B3. Anchor mobile influence production** (owning layers:
  `Engine.Kernel` + `Engine.Gameplay` + `IceRemembers`). Remittances: route a
  share of mobile entities' influence production to their home anchor
  (HomeBase target, else birth cell) so roaming stops painting unbounded
  trails while map life is preserved.
  Done 2026-07-11: every entity gets a per-plane culture anchor (birth
  position by default). Positive from-entity influence produced farther than
  `remittanceMinimumDistance` (default 6) from the anchor splits
  `remittanceShare` (default 0.5) to the anchor cell. Relationship kinds can
  declare `IsCultureAnchor` (domain config -> compiler -> installer ->
  kernel); creating one moves the source's anchor to the target — Ice
  Remembers flags `HomeBase`. Static entities are unaffected (distance 0).
  Tuning in JSON/TOML config and snapshots. 4 unit tests.
- [x] **B4. Culture-field-aware generators** (owning layers: `Engine.Kernel`
  generator runtime + `IceRemembers` authored config). Weight target
  eligibility by target connected-component culture mass; gate
  satellite-founding generators (`routes_draw_readers`,
  `charters_find_harbors`, `conduit_points_found_reefholds`) on minimum
  own-culture support at destination or pull `Across` placement toward
  own-culture support.
  Done 2026-07-11: target-eligibility culture weighting already landed in
  `d088ad5` (`GeneratorProgramRuntime.TargetScoring.cs` weights target
  selection by culture placement score incl. connected bonus) — verified, not
  re-implemented. New this round: `CultureInterior` and `CultureRoute`
  placement modes now hard-require own-culture support at the candidate cell
  (raw >= trace, or claimed adjacency, or controlled connected mass) in
  `PlacementCellAllowsCulture`. Unsupported void candidates are rejected and
  the output is not founded — this closes the satellite-founding path for
  harbors, reefholds, settlements, factions, POIs, and regions on both
  planes. Covered by a new rejection test plus updated placement tests.

Explicitly out of scope: retuning `CultureInfluenceActionScale` or band
thresholds before B1/B2 land.

### C. Player agency

- [x] **C1. Gambit cards toggleable.** `GameRuntimeTableau.RequestToggle`
  requires `GeneratorId > 0`; add gambit enable/disable through the intent
  port so gambit cards match generator cards.
  Done 2026-07-11: full mirror of the generator path — `SetGambitEnabled`
  kernel command (type 3), `GambitEnabledChanged` kernel event (type 32),
  `TrySetGambitEnabledById` on the gambit runtime (selection already
  respected the enabled flag), projection `UiGambitProjection.IsEnabled`,
  driver `SetGambitEnabledCommand`, `GameIntentPort.RequestGambitEnabled`,
  tableau card state carries `GambitId`, and the card toggle button is
  visible for gambit cards. Kernel command test + updated tableau tests.
- [x] **C2. State-of-the-nations panel.** Player-facing per-culture trend
  panel from existing kernel boundary metrics (influenced cells, largest
  component share, contested cells, nexus count, trend arrows). Read-only
  over the boundary program/projection; no new simulation behavior.
  Done 2026-07-11: implemented as a nations line per culture inside the
  existing culture legend panel (`LegendPanel.AddNationRow`) rather than a
  fourth map overlay: territory cells, heartland share (largest component),
  nexus cells, contested cells, each with a 500-tick trend arrow computed
  from boundary-enriched map projection data. No simulation changes.
- [x] **C3. One-shot spatial intervention cards.** Minimal targeted player
  verbs routed through the intent port into primitive kernel effects:
  consecrate (local own-culture influence boost at a chosen cell area) and
  favor (temporary influence-production multiplier on a chosen entity).
  Visible as one-shot action cards; costs/mana out of scope for this round.
  Done 2026-07-11: two kernel commands (`BlessCellCulture`, `FavorEntity`)
  applied on the sim thread under a new domain-registered `player` source
  code, so every intervention is attributable in events/telemetry.
  Consecrate adds 120 raw over radius 1 to the clicked cell's dominant
  culture; Favor doubles a clicked entity's influence production for 500
  ticks via a favor multiplier with kernel-side expiry. Player flow: two
  INTERVENTION cards in the tableau with a Use button and a 2500-tick
  recharge; Use arms targeting, next left-click on a map hex resolves the
  target (dominant culture for consecrate, first active occupant for favor).
  Kernel bounds-checks radius/amount/duration and only touches known cells.
  3 kernel command tests.

### D. Verification

- [x] **D1. `make ci` green.** (2026-07-11, after all A/B/C items.)
- [x] **D2. MC evidence run.** Run id `nexus-physics-2x25k-20260711` (2x25k,
  fresh build), compared against
  `culture-nexus-physical-5x25k-20260522015457`, physical plane, final tick:
  - top-10 cells' mass share: 0.9-5.6% -> 6.4-24.2% (real peaks; max raw
    reaches the new 2000 cap).
  - components per culture: 41-58 -> 3-36; largest-component share up to
    50-90%.
  - contested cells: 234-312 -> 1-29 per culture (boundaries are clean).
  - mass-weighted spread from own centroid: 17-33 hexes -> 6.1-12.2 for the
    two minor cultures (tight hub+nexus), 26.4 for the dominant culture (a
    large empire that still has a peaked core).
  - nexus-band cells are concentrated cores (5-173 per culture) instead of
    the old all-or-nothing plateau.
  Stats recorded via `append-stats`. Follow-ups observed, out of this round's
  scope: one culture still runs away in total mass (~6x, the same asymmetry
  class as the baseline — balance work), and run 25001 flagged
  `map_degenerate=1` (needs its own diagnosis).

## Playability Framing (context for future rounds)

The nexus work is the fun work: legible, contestable territory is the
scoreboard that makes entity stories meaningful. Follow-on agency (mana,
deck/hand loop, more intervention verbs) should build on a working
nexus/border dynamic, not precede it. The road problem
(`the-road-problem.md`) is the same class of failure (no decay + no
consolidation) and should adopt the same pattern later.
