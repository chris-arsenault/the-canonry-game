# Settlement Magnet And Circulation

This is the first active gameplay loop being built into the prototype.

It is the canonical example of the current process-design rules:

- interface-first
- loop-second
- no single-entity loops
- no balancing logic that relies on ambient resource depletion

## Purpose

The loop should make named places matter for multiple kinds at once.

It should:

- keep `Irrigate`, `Grow`, `Eat`, `Move To Target`, and `Wander` as live seed verbs
- give `Region` and `Faction` real process hooks
- create a reason for NPC circulation to matter beyond sustainment
- produce bounded positive feedback instead of monotonic growth

## Core Principle

Ambient hex resources are **possibility space**, not a consumable economy layer.

That means:

- `food`, `wealth`, and `population` bias and support actions
- actions can improve those ambient fields
- actions do **not** need to deplete those fields to create balance
- homeostasis should come from upkeep, thresholds, saturation, drift, and competing claims

## Positive Circulation Loop

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

This is a real loop because the end state changes the same regional and settlement
conditions that made those places attractive in the first place.

## Balancing / Homeostatic Loop

```text
Patrol consumes faction support
  -> factions cannot stabilize every settlement equally
  -> low-security settlements stop being strong Create Works targets
  -> prestige and support growth slow
  -> future patrol capacity slows
```

Additional bounds:

- `holding` and `security` are capped bounded attributes
- `Create Works` requires high `warmth`
- `Create Works` only fires in safe enough named places
- `Patrol` only fires on controlled settlements and stops mattering once security is already high
- `Form Militia` only matters when holding is meaningfully below its upper band

The loop should stabilize through support expenditure and bounded values, not through
ambient-resource exhaustion.

## Participating Processes

### Existing Seed Processes

- `Irrigate`
  - improves ambient local food
- `Grow`
  - converts local ambient conditions into settlement residents
- `Eat`
  - keeps named settlements relevant as NPC sustainment destinations
- `Move To Target`
  - movement enabler for named-place actions
- `Wander`
  - low-urgency circulation fallback

### New Closing Processes

- `Create Works` (`Npc -> Settlement`)
  - opportunistic named-place contribution
  - only fires when already at a settlement
  - increases settlement prestige
  - increases the loyal faction's support
  - costs some warmth, so contribution is not free

- `Patrol` (`Faction -> Settlement -> Region`)
  - targets a controlled settlement
  - spends faction support
  - raises settlement security
  - raises containing region safety

- `Form Militia` (`Region -> Settlement`)
  - region-side safety response
  - raises holding
  - raises security on contained settlements

## Interface Map

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

## Why This Loop Exists

This loop gives each participating kind a distinct role:

- `Region`
  - ambient backdrop plus safety response
- `Settlement`
  - conversion and attraction site
- `Npc`
  - circulation and prestige generation
- `Faction`
  - protective spender that turns social support into order

No one process closes the loop by itself. The loop requires multiple kinds and at
least one named place.

## Explicit Non-Goals

- no stockpile economy layer
- no ambient resource depletion as the main balance mechanism
- no region weather/status extension in this slice
- no artifact participation yet

Those may come later, but they are not required to make this first loop real.
