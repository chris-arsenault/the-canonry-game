# Process Design

How to design new processes so they enrich the simulation instead of feeling bolted on.

This document is the canonical reference for process ideation before implementation.

Concrete loop specifications live under [docs/loops](../loops/README.md).

The typed semantic/definition graph that backs structural validation is described in [15-domain-definition-graph.md](./15-domain-definition-graph.md).

---

## Core Stance

Process design is:

1. **interface-first**
2. **loop-second**
3. **stock/flow-derived**

That ordering is intentional.

We do **not** start with "what cool thing might a faction do?" or "what actions fit an artifact fantasy?" We start by asking where kinds touch each other, then build loops that move state across those interfaces.

The important formalization is:

- attributes are the stocks
- processes are the flows
- relationships and spatial adjacency define legal flow paths
- actions should be derived from that structure, not brainstormed first and justified later

This project does **not** need a goods economy for formal design to apply. `warmth`,
`security`, `cohesion`, `holding`, `prestige`, and similar bounded scalars are
already the stock layer.

---

## Primary Rule: Design At Interfaces

Every new process should be justified by a cross-kind interface.

Examples of good interfaces:

- `Region <-> Settlement`
- `Settlement <-> NPC`
- `Settlement <-> Faction`
- `NPC <-> Artifact`
- `Artifact <-> Settlement`
- `Faction <-> Region`

A process is healthy when it changes what another kind wants, can do, or suffers from.

Bad process shape:

- self-only improvement with no downstream effect
- a closed dyad that never spills into the wider world
- a verb that exists only because it sounds flavorful

Good process shape:

- changes a shared condition
- creates or relieves pressure for another kind
- alters who moves where
- changes which targets are attractive
- makes another process stronger, weaker, or newly legal

Interfaces are not only narrative contact surfaces. They are also the **permission
structure for flows**. If a process moves value from one entity or place to another,
the graph relationship or spatial path should explain why that flow is legal.

---

## Secondary Rule: Every Process Belongs To A Loop

No loop should be single-entity.

Dual-entity loops are usually a warning sign. They can exist as local mechanisms, but the full loop should spill into a third entity and usually a third kind.

Minimum healthy loop shape:

```text
Entity/Kind A changes a condition
  -> Entity/Kind B exploits or responds to it
  -> Entity/Kind C is strengthened, weakened, attracted, or threatened
  -> feedback returns to A or changes the interface A depends on
```

The loop does not need to be symmetric. It does need to circulate consequences.

And every reinforcing loop should have a balancing loop that uses a different variable.
If not, the design is unfinished.

---

## What A Process Must Contribute

Every candidate process should answer most of these questions clearly:

- What cross-kind interface does it live on?
- What ambient condition, attribute, or relationship does it read?
- What does it change that another kind will care about?
- What existing process benefits from it?
- What existing or future process pushes back against it?
- How could the player later amplify, suppress, or redirect it?

If those answers are weak, the process is probably decorative.

An even stricter test:

- if a process does not implement a real inflow or outflow on some stock, it is decorative
- if it writes a stock that no other process meaningfully reads, it is decorative
- if removing it does not materially change a loop, it is decorative

---

## Preferred Loop Topology

The preferred shape is:

```text
ambient world condition
  -> local conversion or exploitation
  -> mobility / attraction / allegiance shift
  -> conflict, custody, or consolidation
  -> recovery or degradation of the ambient condition
```

This keeps the simulation from collapsing into isolated self-maintenance.

The positive loop is only half the design. You must also identify:

```text
growth or coordination
  -> upkeep, saturation, drift, contest, or risk
  -> weaker future growth or coordination
```

That balancing loop is mandatory.

---

## Existing Seed Processes

These processes already exist and should be treated as seed verbs, not throwaway scaffolding.

### Settlement

- `Irrigate`
  - Interface: `Region <-> Settlement`
  - Role: support action that improves ambient local `food`
  - Design use: turns ambient possibility space into later process strength

- `Grow`
  - Interface: `Region <-> Settlement`
  - Role: legacy process concept; settlement health is now `temp_hp`
  - Design use: core settlement sustainment / expansion verb

- `Propaganda`
  - Interface: `Settlement <-> Settlement`
  - Role: changes target civic alignment
  - Design use: ideological convergence tool that should later feed faction and control loops

- `Attract Population`
  - Interface: `Settlement <-> Settlement`
  - Role: legacy process concept; settlement health is now `temp_hp`
  - Design use: migration / concentration / decline verb

### NPC

- `Eat`
  - Interface: `NPC <-> Settlement`
  - Role: named-place sustainment
  - Design use: keeps settlements relevant as destinations without requiring a stockpile economy

- `Move To Target`
  - Interface: currently `NPC <-> Settlement`, later generalized
  - Role: relational enabler process
  - Design use: generic movement discovered by planning rather than hardcoded action chains

- `Wander`
  - Interface: `NPC <-> Map`
  - Role: low-urgency fallback mobility
  - Design use: background circulation and future discovery / exposure behavior

These seed verbs should be reused by future loops so they become part of a growing system rather than orphaned legacy examples.

They should also be treated as provisional implementations of flows. If the stock/flow
model later shows a seed verb is the wrong abstraction, the model wins.

---

## Process Roles To Balance

Within a loop, prefer having several distinct roles:

- **converter**
  - turns ambient potential into concrete local gain
- **projector**
  - pushes influence, prestige, risk, or ideology outward
- **mover**
  - changes who is where
- **exploiter**
  - extracts advantage from the current state
- **stabilizer**
  - offsets decline, corruption, or fragmentation
- **destabilizer**
  - creates new demand, conflict, or degradation

Not every loop needs every role immediately, but loops become richer as these roles appear.

---

## Anti-Patterns

- adding one process per attribute because the attribute exists
- creating a separate subsystem only one kind cares about
- using processes to patch over missing loop structure
- processes that only ever improve the actor and never alter the world
- processes that have no clear future card leverage
- processes whose only consumer is the same entity that produced them
- using an action name to hide that the underlying flow has not been defined
- deriving process lists before defining what each participating stock means

---

## Using Support Planning Correctly

The planner can now support two important patterns:

1. **blocker resolution**
   - a needed action is illegal
   - another action makes it legal

2. **support planning**
   - a needed action is legal but weak
   - another action improves its payoff

This means process design should not only ask:

- "what unblocks this?"

It should also ask:

- "what makes this worth doing?"

That second question is especially important for ambient resources.

Support planning should generally improve:

- legality of a blocked action
- utility of a weak action
- safety of a risky action

It should not be used as a substitute for defining the underlying flows.

## Formal Design Workflow

Use this workflow for every new slice of process design.

### Phase 0: Global Attribute Semantics

Before designing new loops, define a semantic contract for the active attributes.

For each attribute:

- what stock does it represent?
- what is its polarity?
- is it freely creatable, destructible, transferred, or effectively conserved?
- what kinds of things are allowed to raise it?
- what kinds of things are allowed to lower it?
- what passive drift or engine-side pressure already affects it?

This is a global pass, not full process design. Its purpose is to stop semantic drift.

### Phase 1: Pick The Loop Interface

Choose the cross-kind interfaces first.

Examples:

- `Region <-> Settlement`
- `Settlement <-> NPC`
- `Settlement <-> Faction`

### Phase 2: Define Participating Stocks

For the chosen loop, list only the subset of attributes that actually participate.

For each participating stock, define:

- inflows
- outflows
- rate modifiers
- legal flow paths

This is the point where relationships and map adjacency become the permission structure.

### Phase 3: Sketch Reinforcing And Balancing Loops

Every loop spec must explicitly name:

- at least one reinforcing loop
- at least one balancing loop

The balancing loop must not just be "the number is capped." It should usually involve:

- upkeep
- saturation
- competing claims
- drift
- threat
- risk

### Phase 4: Derive Processes

Only after the stocks and loops are clear do we derive the needed processes.

Each process should map to one or more of:

- inflow implementation
- outflow implementation
- rate modification
- mobility / target acquisition
- loop balancing

If a proposed process does not map cleanly to one of those, it probably should not exist.

### Phase 5: Remove-And-Compare Check

Before accepting a process, ask:

- if this process were removed, would the loop materially change?
- if this stock write were removed, would any important downstream behavior disappear?

If the answer is "not really," the process is probably decorative.

---

## Loop Design Workflow

### 1. Do the global semantics pass first

Confirm that all participating attributes already have stable meanings.

### 2. Pick the interface first

Choose a contact surface between kinds before naming processes.

### 3. Define the circulation

Sketch the loop as state movement, not verbs:

- what grows?
- what constrains it?
- what moves because of it?
- what spills into another kind?
- what pushes back?

### 4. Place the seed verbs

Before inventing a new process, ask whether one of the current seed processes already occupies part of the loop.

### 5. Add the missing roles

If the loop has a converter but no exploiter, or a destabilizer but no stabilizer, add the missing role.

### 6. Check player leverage

Ask how a future card, deck choice, or decision could later influence the loop.

---

## Motif Palette From TheCanonry Desktop

The `../the-canonry-desktop/domain/default-project/*.json` files are useful as a motif source, not as a direct implementation template.

The strongest motifs to borrow into the current MVP process design are:

- control contests
- alliance and bloc formation
- trade / route / wealth spillover
- corruption and cleansing
- anomaly-driven danger and recovery
- artifact custody, gifting, theft, and restoration
- prominence / memory / reflected status

These are good because they are inherently cross-entity and tend to create loops rather than isolated actions.

---

## Immediate Design Target

When choosing the next MVP process set, prefer loops that:

- use at least one existing seed process
- involve at least 3 entities
- cross at least 3 kinds

## Active Loop Specs

- [Settlement Magnet And Circulation](../loops/settlement-magnet-and-circulation.md)
- create both a growth path and a failure path
- obviously support future player intervention

That is the bar for "this process belongs in the simulation."
