# Active Gameplay Loops

Loop specifications that are actively being built into the prototype
simulation. More concrete than the per-system process design notes — each
loop names its anchor variables, the participating processes, and the
balancing mechanism that keeps it from becoming pure runaway growth or
collapse.

The current process-design rules these loops are written under:

- **Interface-first** — design from the contract between participating
  kinds, not from the verbs of one kind.
- **Loop-second** — a process exists because it is part of a loop, not
  because it sounds like a thing the kind would do.
- **No single-entity loops** — a loop must require more than one kind, or
  it is a single-entity feedback that belongs in upkeep / drift, not in
  named processes.
- **No balancing logic that relies on ambient resource depletion** —
  ambient hex resources (`food`, `wealth`, `population`) are possibility
  space, not a consumable economy layer. Homeostasis comes from upkeep,
  thresholds, saturation, drift, and competing claims.

## Settlement Magnet And Circulation

The first active loop being built into the prototype.

### Purpose

The loop should make named places matter for multiple kinds at once.

It should:

- keep `Irrigate`, `Grow`, `Eat`, `Move To Target`, and `Wander` as live
  seed verbs.
- give `Region` and `Faction` real process hooks.
- create a reason for NPC circulation to matter beyond sustainment.
- produce bounded positive feedback instead of monotonic growth.

### Core principle

Ambient hex resources are **possibility space**, not a consumable economy
layer. Concretely:

- `food`, `wealth`, and `population` bias and support actions.
- Actions can improve those ambient fields.
- Actions do **not** need to deplete those fields to create balance.
- Homeostasis comes from upkeep, thresholds, saturation, drift, and
  competing claims.

### Positive circulation loop

```text
regional ambient food/population/safety
  -> settlements can Irrigate and Grow more effectively
  -> stronger settlements accumulate residents
  -> NPCs circulate through those settlements to Eat and Create Works
  -> NPC works increase settlement prestige and loyal faction support
  -> factions spend support to Patrol their settlements
  -> patrol raises settlement security and holding
  -> better safety improves the same places as future anchors
```

This is a real loop because the end state changes the same regional and
settlement conditions that made those places attractive in the first
place.

### Balancing / homeostatic loop

```text
Patrol consumes faction support
  -> factions cannot stabilize every settlement equally
  -> low-security settlements stop being strong Create Works targets
  -> prestige and support growth slow
  -> future patrol capacity slows
```

Additional bounds:

- `holding` and `security` are capped bounded attributes.
- `Create Works` requires high `warmth`.
- `Create Works` only fires in safe enough named places.
- `Patrol` only fires on controlled settlements and stops mattering once
  security is already high.
- `Form Militia` only matters when holding is meaningfully below its
  upper band.

The loop stabilizes through support expenditure and bounded values, not
through ambient-resource exhaustion.

### Participating processes

**Existing seed processes**

- `Irrigate` — improves ambient local food.
- `Grow` — converts local ambient conditions into settlement residents.
- `Eat` — keeps named settlements relevant as NPC sustainment
  destinations.
- `Move To Target` — movement enabler for named-place actions.
- `Wander` — low-urgency circulation fallback.

**New closing processes**

- **`Create Works`** (`Npc -> Settlement`) — opportunistic named-place
  contribution. Only fires when already at a settlement. Increases
  settlement prestige. Increases the loyal faction's support. Costs some
  warmth, so contribution is not free.
- **`Patrol`** (`Faction -> Settlement -> Region`) — targets a controlled
  settlement. Spends faction support. Raises settlement security. Raises
  containing region safety.
- **`Form Militia`** (`Region -> Settlement`) — region-side safety
  response. Raises holding. Raises security on contained settlements.

### Interface map

```text
Region <-> Settlement
  Irrigate, Grow, Form Militia

Settlement <-> NPC
  Eat, Create Works

NPC <-> Faction
  Create Works increases loyal faction support

Faction <-> Settlement
  Patrol protects controlled settlements

Faction <-> Region
  Patrol improves holding through the protected settlement
```

### Why this loop exists

Each participating kind has a distinct role:

- `Region` — ambient backdrop plus safety response.
- `Settlement` — conversion and attraction site.
- `Npc` — circulation and prestige generation.
- `Faction` — protective spender that turns social support into order.

No one process closes the loop by itself. The loop requires multiple
kinds and at least one named place.

### Explicit non-goals

- No stockpile economy layer.
- No ambient resource depletion as the main balance mechanism.
- No region weather/status extension in this slice.
- No artifact participation yet.

These may come later, but they are not required to make this first loop
real.
