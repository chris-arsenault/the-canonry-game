# Domain Definition Graph

The engine now has a typed **definition graph** alongside the runtime world graph.

This is the system used to reason about the domain itself:

- what kinds exist
- what attributes/resources/relationships/pressures exist
- what processes touch them
- what the authored stock/flow semantics are
- what loops and structural errors exist in the domain model

It is not runtime world state. It is not the planner. It is the typed schema and semantic topology those systems operate on.

---

## Purpose

This system exists to replace loose text and worksheet reasoning with typed, testable structure.

It lets us validate questions like:

- does every authored stock actually exist in the domain?
- does every semantic verb point at a real implemented process, if claimed?
- do semantic process references match the current domain config?
- do scalar stocks have the inflow/outflow coverage they claim?
- are there illegal direct scalar reversals?
- are there closed multi-kind loops in the authored stock graph?

The goal is domain hygiene. If the semantic model drifts from the actual domain, the build should fail instead of letting the drift accumulate.

---

## Relationship To Other Graphs

There are now three relevant graph layers:

1. **World graph**
   - runtime entities and relationships
   - implemented in `Engine.Graph`

2. **Definition graph**
   - typed domain schema and process contracts
   - implemented in `Engine.Domain.Graph`

3. **Projected stock graph**
   - a derived graph built from the semantic flow layer
   - used for loop detection and structural validation

The definition graph is built from the authored domain config, not from live simulation state.

Entry point:

- `Engine.Domain.Loader.DomainLoader.BuildDefinitionGraph(DomainConfig config)`

---

## Authoring Surface

The definition graph is fed by the domain package, not by ad hoc text files.

For the current test domain, the typed authoring surface is:

- `Engine.Domain.TestDomain.TestDomainDefinition`
- `Engine.Domain.TestDomain.TestDomainSemanticDefinition`
- `Engine.Domain.TestDomain.TestDomainSchema`

The important distinction is:

- `DomainConfig` declares the runtime domain
- `SemanticDomainConfig` declares the stock/flow semantics of that same domain

The semantic config is now the typed replacement for the earlier worksheet pass.

---

## What The Definition Graph Contains

`DefinitionGraph` has two layers.

### 1. Structural domain schema

These are the typed runtime declarations derived from `DomainConfig`:

- kinds
- attributes
- resources
- relationships
- pressures
- lifecycles
- processes
- standing drives
- critical effects
- location-resource drift hooks

This is the part that answers questions like:

- does `Settlement.security` exist?
- is `Npc.satiety` scalar or 2D?
- which process definitions read/write a given typed ref?

### 2. Semantic flow layer

This lives in `DefinitionGraph.SemanticFlows`.

It contains:

- `SemanticStockNode`
- `SemanticVerbNode`
- `SemanticFlowEdge`
- `SemanticModifierEdge`
- `SemanticProcessReference`

This is the part that answers questions like:

- what stock is a verb intended to increase or decrease?
- is this edge an inflow or an outflow?
- is a stock balancing, anchoring, or drifting?
- which verbs modify other verbs rather than directly moving stock?

---

## Core Semantic Concepts

### Stocks

A stock is a typed attribute that participates in stock/flow reasoning.

Examples:

- `Settlement.residents`
- `Settlement.security`
- `Npc.satiety`
- `Region.regional_safety`

Each stock carries metadata such as:

- stock class
- feedback class
- required flow coverage
- notes/bounds/conservation labels

2D planes are represented as stocks too, but their coverage is axis-aware.

### Verbs

A verb is the semantic owner of one or more flows.

A verb may:

- correspond to an implemented process
- be engine-owned
- be future-facing and not implemented yet

Examples:

- `settlement_grow`
- `npc_eat`
- `faction_patrol`
- `engine_decay`

### Flows

A flow is a typed edge between a verb and a stock, with explicit direction:

- `Inflow`
- `Outflow`

The raw semantic graph stores the authored meaning directly:

- inflow means the verb increases the stock
- outflow means the verb depletes or spends the stock

Each flow may also carry:

- source refs
- mechanism
- functional form
- plane axis
- human notes/signature text

### Modifiers

A modifier does not directly move stock.

It says that one stock changes the effectiveness or legality of a verb.

Examples:

- `Settlement.prestige` gating `settlement_attract`
- `Region.regional_safety` gating `region_shelter`

These matter to the semantic model, but they are not the same thing as first-order inflow/outflow.

### Process references

These are typed cross-checks against the current implemented process graph.

They answer:

- does this process read the stock it is supposed to read?
- does it directly write the stock it is supposed to write?
- does the authored semantic layer still match the runtime domain config?

This is where semantic drift becomes a test failure instead of a guess.

---

## Raw Semantic Graph Vs Projected Stock Graph

The raw semantic graph is not the same thing as the graph we use for loop detection.

Reason:

- the raw graph is organized around `verb <-> stock`
- loop detection wants `stock -> stock`

So we project the raw semantic graph into a first-order stock graph.

Projection rule:

- `Inflow`: `source stock -> target stock`
- `Outflow`: `target stock -> destination stock`

This projection is implemented in:

- `Engine.Domain.Graph.SemanticStockGraphProjection`

That projected graph is what powers:

- direct scalar reversal validation
- closed-loop detection

This distinction matters because it prevents us from accidentally reading an authored outflow backwards.

---

## Current Validators And Analyzers

### Definition graph validator

`DefinitionGraphValidator` validates both the structural graph and the semantic layer.

Current checks include:

- semantic stocks point to real attributes
- semantic verbs point to real implemented processes when claimed
- semantic process references match the current definition graph
- scalar stocks satisfy their declared inflow/outflow coverage
- direct scalar two-node reversals are illegal

The direct reversal rule is intentionally scoped to scalar stocks right now.

2D plane drift is currently treated separately because bidirectional drift is a legitimate authored pattern there.

### Coverage analyzer

`SemanticCoverageAnalyzer` summarizes authored flow coverage for each stock.

For plane stocks, coverage is axis-aware:

- scalar inflows/outflows
- `X` inflows/outflows
- `Y` inflows/outflows

This prevents a plane from falsely appearing "covered" just because one axis has data.

### Loop analyzer

`SemanticLoopAnalyzer` runs against the projected stock graph and finds closed cycles with configurable minimum distinct kinds and maximum length.

This is used to detect whether the authored semantics actually form multi-kind loops instead of dead-end chains.

---

## How Hex Resources Fit

Hex resources currently straddle two systems.

### In runtime

They are ambient per-hex map-state values:

- `food`
- `wealth`
- `population`

They live in:

- `Engine.Map.HexCell`
- `Engine.Map.TerrainAmbientResources`
- `Engine.Resources.ResourceLayer`
- `Engine.Resources.ResourceSystem`

At runtime they are keyed by `HexCoord` and stored as map-level float fields.

They are not entity attributes.

### In the definition graph

They are typed **resources**, not semantic stocks.

They appear as:

- `DefinitionResourceNode`
- `ResourceValueId`
- `AttributeRef` resource refs inside semantic flows

This means the semantic graph can represent statements like:

- a stock is modified by `hex.food`
- a verb converts a region stock into `hex.population`
- a process is gated by `hex.wealth`

But resources are not yet first-class `SemanticStockNode`s, so current stock coverage and loop analysis are primarily about entity attributes, not the ambient map resource layer itself.

That split is intentional for now.

Remaining cleanup:

- runtime still exposes engine-level `ResourceType` in domain-facing APIs
- tracked in issue `#133`

---

## What This System Is For

Use the definition graph when the question is about the authored domain model itself.

Good uses:

- validating stock/flow consistency
- detecting semantic drift
- checking process ownership claims
- checking inflow/outflow coverage
- finding closed loops
- enforcing structural invariants before runtime tuning

Do not use it as a replacement for:

- runtime simulation debugging
- planner execution traces
- world-state inspection

Those belong to the world graph and runtime systems.

---

## Current Limits

This system is intentionally first-order.

It does **not** yet fully reason about:

- second-order modifiers as loop edges
- full plane-axis semantic completeness as a hard requirement
- resources as first-class stock nodes
- temporal strength, only topology and declared flow coverage

That is acceptable for the current phase. The immediate goal is to make semantic structure typed, testable, and resistant to drift.

---

## Practical Workflow

When changing the domain:

1. update the typed domain schema/config
2. update the typed semantic config
3. rebuild the definition graph in tests
4. let the validator and analyzers catch drift

The definition graph is the programmatic contract between:

- the authored domain
- the process semantics
- the higher-order reasoning we want to do on top of them

It exists so we stop doing this work by memory and prose alone.
