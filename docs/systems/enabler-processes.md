# Enabler Processes: Implemented via Goal/Sub-Goal System

Status: **implemented** via the goal system's sub-goal generation from blocked preconditions.

---

## The Pattern

Some processes don't directly satisfy an entity's need — they create conditions for other processes to fire. The goal system handles this generically through sub-goals.

When a primary goal's process fails its precondition, the `PreconditionResult` reports which attributes are blocking. The GoalGenerator creates sub-goals for those blocking attributes, inheriting the same provider. Processes that write to the blocking attribute serve the sub-goal.

## Examples

| Entity | Goal | Direct Process | Blocks On | Sub-Goal | Enabler Process |
|--------|------|---------------|-----------|----------|-----------------|
| Settlement | population via Frosthaven | attract | ideology distance | converge ideology with Frosthaven | propaganda |
| Settlement | population via self | grow | sustenance | increase sustenance | stockpile |

## How It Works

1. Goal: increase population via Frosthaven
2. Attract writes population → serves this goal
3. Attract precondition: ideology distance ≤ 0.5 → FAILS
4. PreconditionResult.BlockingAttributes: ["ideology"]
5. Sub-goal created: {attr: population, provider: Frosthaven, blocking: "ideology"}
6. Propaganda writes target ideology → serves the sub-goal
7. Propaganda fires against Frosthaven → ideology converges
8. Eventually attract's precondition passes → attract fires

The provider (Frosthaven) is constant through the chain. No special enabler code — the sub-goal system discovers enabling relationships from precondition results and wiring contracts.

## Key Design Point

There is no separate "enabler" concept in the engine. There are goals and sub-goals. A sub-goal happens to serve as an enabler when its resolution unblocks a parent goal's process. This is emergent from the precondition/wiring system, not a coded pattern.
