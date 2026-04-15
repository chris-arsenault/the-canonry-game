# Multi-Step Behavior Through Sub-Goal Generation

The engine doesn't hardcode behavior chains. Instead, multi-step sequences emerge structurally from the goal system.

## How It Works

1. **Entity has an attribute deficit** — GoalGenerator creates a primary goal (e.g., "increase population via Wavecrest")
2. **A process serves that goal but its precondition is blocked** — the `PreconditionResult` reports which attributes are blocking
3. **A sub-goal is generated from the blocker** — "unblock ideology via Wavecrest"
4. **A different process serves the sub-goal** — that's the intermediary action
5. **After the intermediary fires enough times**, the original precondition passes and the primary process can execute

## Settlement Example: Propaganda -> Attract

- Settlement wants population (deficit from baseline=20)
- `attract` writes population, but requires `SemanticDistanceThreshold("ideology", 0.5)` — ideologies must be close enough
- If ideology distance > 0.5, precondition fails with blocking attribute "ideology"
- Sub-goal generated: "unblock ideology via target"
- `propaganda` writes ideology (shifts target toward self), serving the sub-goal
- Settlement runs propaganda repeatedly, closing the ideology gap
- Eventually distance drops below 0.5, attract's precondition passes, population stolen

## NPC Example: Move -> Eat

- NPC has hunger deficit — goal "reduce hunger"
- "eat" process requires being at a location with sustenance (precondition blocked)
- Sub-goal: "unblock located_at" or similar spatial prerequisite
- "move" process serves the sub-goal — moves NPC hex-by-hex toward the provider
- Move doesn't satisfy hunger directly. It's an enabler that unblocks the eat process

## Key Design Point

No process knows about any other process. The engine doesn't know that propaganda enables attract. It only knows that attract's precondition reported "ideology" as a blocker, and propaganda writes "ideology". The chain is discovered, not programmed.
