# Enabler Processes: Design Note

Status: architectural observation, not yet implemented generically. Current code has an NPC-specific implementation (IsDirected + StepTowardTargetMutation) that should eventually be replaced by this general pattern.

---

## The Pattern

Some processes don't directly satisfy an entity's need — they create conditions for other processes to fire. These are **enabler processes**.

The pattern repeats across all entity kinds:

| Entity Kind | Need | Direct Process | Blocked Because | Enabling Process |
|-------------|------|---------------|-----------------|-----------------|
| NPC | hunger | eat | no food at current hex | move (change location) |
| Nation | expansion | expand_borders | no population at border | transfer_population |
| Settlement | development | build | insufficient gold | trade |
| Artifact | influence | radiate | not near important entities | movement via carrier |

In every case:
1. Entity has an urgent attribute deficit
2. A direct process writes to that attribute
3. The direct process has a precondition that currently fails
4. An enabling process can change world state to make that precondition pass
5. The enabler's score derives from the urgency of the blocked direct process

## Scoring Formula (General)

```
enabler_score = blocked_process_urgency × (1 - can_blocked_process_fire_here)
enabler_target = resolve what world state change makes the precondition pass
```

Both halves are derivable from existing systems:
- Urgency: NeedResolver (attribute deficit from ideal)
- "Can it fire here": evaluate the direct process's precondition
- Target: GraphAttributeQuery (find providers of what's missing)

The **what world state change** varies by kind: NPCs change their coordinates, nations transfer resources, settlements accumulate gold. But even this may be derivable from the precondition structure: the precondition says "I need X at my location" → the enabler provides X at the location.

## Current State (NPC-Specific)

The current implementation uses `IsDirected` on ProcessDefinition and `StepTowardTargetMutation`. This is an NPC-movement-specific instance of the general pattern. It works but shouldn't be treated as the general solution.

**What's wrong with it:**
- `IsDirected` implies movement is special. It's not — it's one enabler among many.
- `StepTowardTargetMutation` hardcodes hex-stepping and settlement detection. Other enablers would need different mutations.
- The scoring doesn't account for whether direct processes are blocked. Move scores the same whether eat is viable here or not.

## Future Direction: The Sims Model

The Sims doesn't have a separate "move" action. Instead:
- Each process scores against ALL reachable providers, including distant ones
- Score = need_urgency × provider_value × inverse_distance
- The winning process-provider pair becomes the entity's current goal
- If the entity is at the target, execute the process. If not, step toward it.
- Re-evaluate each tick — urgency changes can preempt the current goal.

This eliminates enablers as a separate concept. Movement is what happens when the winning process's target is elsewhere. For non-NPC entity kinds, the "movement" equivalent would be whatever changes world state to reach the target (resource transfer, influence projection, etc.).

This requires **goal persistence across ticks** — entities remember what they're working toward. The current system is stateless per tick.

## When to Generalize

Don't build the general enabler system until we have at least 2-3 concrete use cases across different entity kinds. The NPC case validates the pattern. The nation population transfer case (when implemented) will validate whether the abstraction holds. If it does, factor the common scoring/targeting logic into a shared system and retire `IsDirected`.
