# Multi-Step Behavior Through Sub-Goal Generation

The engine doesn't hardcode behavior chains. Instead, multi-step sequences emerge structurally from the goal system.

## How It Works

1. **Entity has an attribute deficit** — GoalGenerator creates a primary goal (e.g., "increase population via Wavecrest")
2. **A process serves that goal but its precondition is blocked** — the `PreconditionResult` reports which attributes are blocking
3. **A sub-goal is generated from the blocker** — "unblock in_physical_range via Wavecrest"
4. **A different process serves the sub-goal** — that's the intermediary action
5. **After the intermediary fires enough times**, the original precondition passes and the primary process can execute

## Movement Example: Travel -> Recover

- Penguin wants warmth
- `warm` writes warmth, but requires being in physical range of a suitable settlement
- If the target is too far away, the precondition fails with structural fact `in_physical_range`
- Sub-goal generated: "unblock in_physical_range via target"
- `move_toward_target` writes that structural fact by moving the penguin along the map
- Once the penguin arrives, the recovery process can execute

## Resource Example: Move -> Eat

- Penguin has hunger deficit — goal "reduce hunger"
- "eat" process requires being at a location with sustenance (precondition blocked)
- Sub-goal: "unblock located_at" or similar spatial prerequisite
- "move" process serves the sub-goal — moves the entity hex-by-hex toward the provider
- Move doesn't satisfy hunger directly. It's an enabler that unblocks the eat process

## Key Design Point

No process knows about any other process. The engine doesn't know that travel enables recovery. It only knows that recovery's precondition reported a blocker, and another process writes that blocker. The chain is discovered, not programmed.
