# Concrete Next Moves Proposal

Concrete proposal for the next round of design experiments while performance work is in flight.

This proposal is organized around the currently clarified problem statement:

- movement/action ratio is not itself pathological
- the non-movement deck is comparatively underused and collapsed
- the world stays sparse and does not develop organically
- ambient resources appear inert
- individual entity stocks appear too binary instead of fluctuating

## Recommended Order

1. resource / pressure ablation
2. multi-turn work
3. lightweight development heuristics

That order is intentional:

- item 1 tells us whether two existing subsystems are actually doing any work
- item 2 changes the action economy without introducing a large new subsystem
- item 3 adds explicit map-shaping preferences only after the lower-altitude loops are better understood

---

## 1. Resource / Pressure Ablation

### Goal

Determine whether ambient resources and pressures are currently meaningful enough to justify their complexity.

The current evidence says:

- global `Food`, `Population`, and `Wealth` totals stay flat
- the resource-producing verbs do not meaningfully shape the world
- pressure appears to exist mostly as schema and infrastructure, not as a live behavioral driver

This should be treated as a diagnostic experiment first, not a permanent design commitment.

### Pressure ablation

Pressure is the simpler half:

- there are declared regional pressures
- but there is no meaningful live process path depending on them right now
- `ModifyPressure` is still effectively unwired in [ProcessMutationContext.cs](/home/dev/repos/the-canonry-game/src/Engine/Processes/ProcessMutationContext.cs:135)

Proposal:

- run a branch with pressure computation and pressure references removed from the sim loop
- no process replacements are needed for this branch because pressure is not currently carrying a real action economy

Interpretation:

- if nothing changes, pressure is presently dead weight
- if behavior changes materially, pressure is active enough to deserve redesign instead of removal

### Resource ablation

Resources are more entangled because they currently sit on ten specific verbs:

#### Current hex-dependent health recovery verbs

- `Penguin Warm`: `hex.food -> penguin.vitality`
- `Artifact Repair`: `hex.wealth -> artifact.condition`
- `Faction Organize`: `hex.wealth -> faction.cohesion`
- `Region Root`: `hex.food -> region.stability`
- `Settlement Grow`: `hex.population -> settlement.residents`

#### Current hex-producing verbs

- `Penguin Witness`: `penguin.echo -> hex.population`
- `Artifact Vivify`: `artifact.resonance -> hex.food`
- `Faction Distribute`: `faction.provisions -> hex.food`
- `Region Breach`: `region.holding -> hex.wealth`
- `Settlement Strip Rookeries`: `settlement.shelter -> hex.wealth`

### Proposed resource-ablation structure

#### Phase 1A. Pure ablation branch

Objective:

- remove ambient resource stocks from the active sim branch
- preserve the recovery verbs by rewiring them to named-provider dependencies
- disable the five hex-producing verbs for the branch so the experiment stays clean

This gives a proper answer to the question:

- do we need hex resources at all for the current low-altitude physics to work?

#### Replacement for hex-dependent recovery

Replace hex dependence with direct named-entity provider dependence:

| Current verb | Current source | Proposed replacement | Notes |
|---|---|---|---|
| `Penguin Warm` | `hex.food` | `target settlement.shelter -> self.vitality` | Warmth and survivable company become direct civic support rather than ambient food. |
| `Artifact Repair` | `hex.wealth` | `target faction.provisions -> self.condition` | Preservation becomes a faction-backed investment rather than map wealth. |
| `Faction Organize` | `hex.wealth` | `target settlement.dominion -> self.cohesion` | Cohesion comes from civic institutions, offices, halls, and legitimacy. |
| `Region Root` | `hex.food` | `target artifact.resonance -> self.stability` | The region roots itself around strong sacred/charged anchors rather than ambient cultivation value. |
| `Settlement Grow` | `hex.population` | `target region.holding -> self.residents` | Growth comes from viable surrounding land and protected capacity rather than ambient population stock. |

This branch has a clear design property:

- it makes survival and growth graph-driven again
- locality still matters because actors still need to move into range of named providers
- it tests a middle ground between pure graph Canonry and the current hex-stock model

#### Handling the five hex-producing verbs in Phase 1A

Disable them for this branch:

- `Penguin Witness`
- `Artifact Vivify`
- `Faction Distribute`
- `Region Breach`
- `Settlement Strip Rookeries`

Reason:

- their only current job is to feed the ambient resource layer
- if the point of the experiment is to ask whether that layer matters, keeping indirect feeders alive would muddy the result

This is acceptable because the branch is diagnostic, not the final shape.

#### Phase 1B. If the no-resource branch looks better

If Phase 1A materially improves process diversity, stock dynamics, or growth, redesign the five disabled verbs as direct graph-support verbs instead of hex producers.

The cleanest first remap is:

| Old verb | Old role | Direct replacement |
|---|---|---|
| `Penguin Witness` | `echo -> hex.population` | `self.echo -> target settlement.residents` |
| `Artifact Vivify` | `resonance -> hex.food` | `self.resonance -> target region.holding` |
| `Faction Distribute` | `provisions -> hex.food` | `self.provisions -> target penguin.warmth` |
| `Region Breach` | `holding -> hex.wealth` | `self.holding -> target faction.provisions` |
| `Settlement Strip Rookeries` | `shelter -> hex.wealth` | `self.shelter -> target artifact.condition` |

This is not part of the initial ablation. It is the redesign path only if the no-resource branch earns it.

### What to measure

For the ablation branch, compare against the current baseline on:

- non-movement process concentration per kind
- zero-offered verbs
- per-entity stock fluctuation
- deaths by kind
- occupied physical share
- anisotropy
- generator fire counts

### Decision rule

- if removing resources and pressures changes almost nothing, they are presently dead weight
- if removing them improves the sim, the current ambient-layer design is actively harmful
- if removing them makes the sim clearly worse, they matter and should be redesigned rather than discarded

---

## 2. Multi-Turn Work

### Goal

Test whether productive work should persist for several ticks after arrival instead of resolving as one-tick impulses.

This is the colony-sim hypothesis:

- travel can remain common
- the key improvement is that arrival sometimes opens a short work phase instead of an immediate replan

### Proposed scope

Do not generalize the entire deck. Start with a narrow pilot:

- one maintenance verb
- one growth verb
- one projection / extraction verb

Suggested first slice:

- `Settlement Grow`
- `Faction Organize`
- `Region Fortify`

### Proposed behavior

When one of these verbs is chosen:

- the actor enters a short work commitment for `2-4` ticks
- while committed, the actor keeps working the same target/location if still valid
- movement pauses unless the job becomes invalid
- each work tick applies a smaller chunk than the current one-shot action

### Why this is worth testing

- reduces planner churn without reducing movement importance
- makes non-movement actions occupy more of lived sim time
- should create more visible local fluctuation in stocks
- may make places feel active without inventing a new locality subsystem

### Risks

- could simply deepen the existing core
- could make dominant verbs even more dominant
- could pin actors in low-value loops if interruption rules are weak

### What to measure

- consecutive productive turns per arrival
- non-movement process share
- per-entity stock fluctuation
- occupied map share
- anisotropy
- whether commitment sites are mostly core or frontier

---

## 3. Lightweight Development Heuristics

### Goal

Introduce enough hard spatial preference that late-state maps read as developed worlds rather than sparse random scatter.

This is the Civ / SimCity lesson:

- emergence matters
- but late-state spatial structure usually needs some procedural preferences

### Proposed scope

Do not jump to roads, zoning, or infrastructure systems first.

Start with three simple heuristics:

1. settlement spacing
   - discourage new settlements from appearing too close to existing ones unless they are explicitly urban-merging cases
2. terrain desirability
   - favor certain terrain mixes for new coverage/settlement creation
3. adjacency growth preference
   - give modest preference to extending coherent frontiers instead of dropping isolated dots

### Where to apply it

Only in:

- new settlement placement
- new region / named coverage placement
- growth-oriented generator outputs

Do not apply it globally to every move or process.

### Why this is worth testing

- directly targets sparse-map late states
- provides legible spatial structure without requiring a large new player-facing system
- can coexist with both graph-driven and planner-driven behavior

### Risks

- can paper over bad low-altitude physics
- can make the world look good while behaving poorly
- can over-script growth if the heuristics are too strong

### What to measure

- occupied physical share
- anisotropy
- cluster count and size
- frontier survival
- whether late-state maps visually read as developed rather than merely inhabited

---

## Recommended Immediate Move

If only one branch is taken next, take **item 1** first.

Reason:

- it is the sharpest diagnostic
- it tests whether two existing systems are actually contributing
- it gives a cleaner base for interpreting the later multi-turn-work and development-heuristic experiments
