# Attribute Model

The active domain uses a simple 5-bucket model to keep attribute selection disciplined.

The point is not that every kind must fill every bucket evenly. The point is that every attribute should justify itself by serving a clear simulation role, rather than existing because it sounds flavorful.

---

## The 5 Buckets

### 1. Integrity

What keeps the entity present in the world. If integrity reaches zero, the entity dies, collapses, breaks, or otherwise exits active play.

Examples:
- `stability`
- `temp_hp`
- `cohesion`
- `vitality`
- `condition`

### 2. Need / Drive

What creates ongoing pressure and motivates behavior even when nothing external happens.

Examples:
- `warmth`
- `echo`
- `containment`

Not every kind needs a strong explicit drive scalar. Regions, for example, are mostly slow world-state rather than self-motivated actors.

### 3. Capacity

What lets the entity do work or support other activity.

Examples:
- `support`
- `power`
- `security`

Capacity is inward-facing: how much the entity can hold together, sustain, or convert into action.

### 4. Projection

What lets the entity affect other entities or shape the world around it.

Examples:
- `prestige`
- `reach`
- `holding`

Projection is outward-facing: attraction, reach, control, or ambient influence.

### 5. Alignment

Where the entity sits in a shared semantic space that affects affinity, conflict, and targeting.

Examples:
- `civic_ideology`
- `mystic_resonance`

Alignment is usually modeled as a plane rather than a scalar.

---

## Current Active-Kind Mapping

### Region

- `stability` -> Integrity
- `holding` -> Projection / world-condition quality
- `pristiness` -> Need / land-wholeness pressure

Regions intentionally do not fill all 5 buckets. They are slow world forces, not person-like actors.

### Settlement

- `temp_hp` -> Integrity
- `shelter` -> Need
- `dominion` -> Projection
- `civic_ideology` -> Alignment

### Faction

- `cohesion` -> Integrity
- `provisions` -> Need
- `reach` -> Projection
- `civic_ideology` -> Alignment

### NPC

- `vitality` -> Integrity
- `warmth` -> Need
- `echo` -> Capacity
- `civic_ideology` -> Alignment

### Artifact

- `condition` -> Integrity
- `power` -> Capacity / Projection
- `resonance` -> Drive / pressure

---

## Selection Rules

When adding or changing attributes:

- prefer monotonic bounded scalars where possible
- avoid duplicate concepts across buckets
- avoid midpoint "balance" attributes unless the simulation truly needs them
- favor attributes that multiple systems can read: planner, lifecycle, UI, pressures, future cards
- if an attribute does not create meaningful dynamics, cut it

This model is a design tool, not a rigid validator. It exists to keep the attribute set coherent while the simulation grows.
