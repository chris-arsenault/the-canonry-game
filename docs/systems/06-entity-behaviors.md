# System 6: Entity Behaviors

The process and goal system that drives all entity activity.

---

## Purpose

Every entity participates in the same evaluation system. There is no per-kind behavioral code. The domain defines processes as data. The engine evaluates goals, selects processes, and executes mutations. Entity kinds differ only in which processes are registered for them and what attributes they have.

---

## Goal System

Entities maintain persistent goals derived from their attribute deficits. Goals drive process selection. Sub-goals are generated from blocked preconditions.

### Goal Structure

A goal is: change attribute X, acting on provider entity Y. The provider is any entity on the graph that a process can operate on.

- Goals are regenerated every N ticks or when a sub-goal's process fires.
- Max 3 primary goals per entity, plus sub-goals.
- Goals compete via urgency-weighted random selection.

### Goal Generation

1. Compute attribute deficits (urgency from baseline curve, not from max)
2. For each deficit, find processes that write to that attribute
3. For each process, examine wiring contracts for Target-scoped refs
4. Query graph for entities matching target refs → providers
5. Score providers using the process's provider score expression
6. Create goals: (attribute, provider, urgency * provider_weight)

### Sub-Goal Generation

For each goal, evaluate serving processes' preconditions against the goal's provider. If a precondition fails, its `PreconditionResult` reports blocking attributes. Create sub-goals for each blocking attribute. The provider carries through — sub-goals target the same provider as their parent.

Example: attract(Frosthaven) fails because ideology distance > threshold. Blocking attribute: "ideology." Sub-goal: converge ideology with Frosthaven. Propaganda serves this sub-goal.

---

## Process Evaluation (Per Tick)

1. Check entity-wide cooldown
2. Try goals in urgency-weighted order until one has a viable process
3. For the selected goal, find serving processes (writes to goal's target attribute)
4. Filter by per-process cooldowns and preconditions
5. Score viable processes, weighted random select
6. Set goal's provider as target on mutation context
7. Execute winning process's mutations
8. Apply cooldowns with jitter

No branching on entity kind, location, or whether the process operates on the entity itself or another entity. The process's mutations determine what gets modified. The goal's provider determines the target context. The engine is agnostic.

---

## Process Definition

Processes are data. Each process declares:

- **Preconditions**: composable tree returning `PreconditionResult` with diagnostics and blocking attributes
- **Scoring expression**: IExpression evaluated against entity state
- **Provider score expression**: IExpression evaluated per candidate provider to weight goal urgency
- **Mutations**: ordered list of effects using context variables and attribute paths
- **Wiring contracts**: Self-scoped and Target-scoped AttributeRefs declaring what the process reads and writes
- **Cooldowns**: per-process and per-entity, with jitter

### Wiring Contracts and Target Scope

Wiring contracts declare whether a process operates on the entity itself (Self scope) or on another entity (Target scope). This is the ONLY distinction the engine makes — it is data on the process, not code in the engine.

- `Self.population` — reads/writes the entity's own population
- `Target.population` — reads/writes the target provider's population

Processes with Target-scoped refs trigger provider search during goal generation. Processes with only Self-scoped refs use the entity itself as the context. No branching in engine code — the contracts declare the truth.

### Provider Scoring

Each process that has Target-scoped refs declares how to score candidate providers via an IExpression. The expression evaluates with the candidate as `target`, giving access to all its attributes and semantic plane data. Provider weight modulates goal urgency.

Examples:
- Attract: `(self.fame - target.fame) * (1 - semantic_distance(ideology))`
- Propaganda: `semantic_distance(ideology)` (further ideology = more to gain)

This is process-defined, not engine-hardcoded. Distance, attribute comparisons, and semantic plane data are available but none are assumed.

---

## Processes Are Not Kind-Specific

A "settlement process" is just a ProcessDefinition with `TargetKind = Settlement`. The engine doesn't know what a settlement is. It evaluates goals, finds processes registered for that entity kind, selects one, and executes its mutations. If a process writes `Target.ideology`, the engine finds target providers and sets the target on the context. If a process writes `Self.sustenance`, the engine executes the mutation on the entity. Same code path.

The domain defines which processes apply to which entity kinds. The engine provides the evaluation loop. This means:
- Adding a new entity kind requires zero engine changes
- Adding a new process requires zero engine changes
- All entity kinds go through the same evaluator
- All processes go through the same mutation execution

---

## Attribute System

Entity attributes are the foundation of all behavior. Attributes are either scalar (single value with effects) or semantic planes (2D with per-component effects). The attribute system provides:

- **Tick effects**: per-tick modifications (drift, drain, buffs). Compose: `base += sum(deltas) * product(multipliers)`
- **Static effects**: read-time modifications (bonuses from relationships, artifacts). Compose on read.
- **Trend tracking**: recent delta history for UI and degeneracy detection
- **Urgency derivation**: baseline curve determines how urgently the entity needs to change this attribute

Attributes are the broadcast mechanism. Every entity advertises what it has by virtue of having attributes. Processes query for entities with specific attributes. No separate "provider registration" — existence is the broadcast.

---

## Preconditions

Preconditions return `PreconditionResult` with:
- `Passed`: boolean
- `Diagnostic`: human-readable explanation ("self.fame=3.0 (need >= 5)")
- `BlockingAttributes`: which attributes are preventing the precondition from passing

Diagnostics are produced during evaluation, not via separate introspection. The goal system reads blocking attributes from failed results to generate sub-goals. The UI displays diagnostics directly.

---

## Interactions with Other Systems

| System | Interaction |
|--------|-------------|
| Entity Graph | Processes read entity state and relationships. Target mutations modify other entities on the graph. |
| Attributes | Processes read and write via attribute paths (Self and Target scope). Effects compose per-tick and on-read. |
| Pressure | Process outputs feed INTO pressure computation. Processes do NOT read pressure (pressure is for card preconditions). |
| Lifecycle | Maintenance system applies tick effects. Lifecycle system updates age-dependent drain rates. Both are physics, not processes. |
| Cards/Decisions | Decision cards register additional processes (card-gated). Same evaluation loop, just with availability gating. |

---

## Open Questions

- **Bulk flow**: aggregate phenomena (population migration, trade routes) may be separate from per-entity process evaluation
- **GOAP extension**: multi-step planning beyond one level of sub-goals
- **Goal tag design**: how entities broadcast intentions for other entities to respond to
