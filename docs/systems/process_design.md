# Process Design

How to design new processes so they enrich the simulation instead of feeling bolted on.

This document is the canonical reference for process ideation before implementation.

Concrete loop specifications live under [docs/loops](../loops/README.md).

---

## Core Stance

Process design is:

1. **interface-first**
2. **loop-second**

That ordering is intentional.

We do **not** start with "what cool thing might a faction do?" or "what actions fit an artifact fantasy?" We start by asking where kinds touch each other, then build loops that move state across those interfaces.

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
  - Role: converts ambient `food` and `population` into `residents`
  - Design use: core settlement sustainment / expansion verb

- `Propaganda`
  - Interface: `Settlement <-> Settlement`
  - Role: changes target civic alignment
  - Design use: ideological convergence tool that should later feed faction and control loops

- `Attract Population`
  - Interface: `Settlement <-> Settlement`
  - Role: pulls `residents` across settlements when prestige and ideology line up
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

---

## Loop Design Workflow

### 1. Pick the interface first

Choose a contact surface between kinds before naming processes.

### 2. Define the circulation

Sketch the loop as state movement, not verbs:

- what grows?
- what constrains it?
- what moves because of it?
- what spills into another kind?
- what pushes back?

### 3. Place the seed verbs

Before inventing a new process, ask whether one of the current seed processes already occupies part of the loop.

### 4. Add the missing roles

If the loop has a converter but no exploiter, or a destabilizer but no stabilizer, add the missing role.

### 5. Check player leverage

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
