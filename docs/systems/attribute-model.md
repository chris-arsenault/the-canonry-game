# Attribute Model

The active domain uses a simple 5-bucket model to keep attribute selection disciplined.

The point is not that every kind must fill every bucket evenly. The point is that every attribute should justify itself by serving a clear simulation role, rather than existing because it sounds flavorful.

---

## The 5 Buckets

### 1. Integrity

What keeps the entity present in the world. If integrity reaches zero, the entity dies, collapses, breaks, or otherwise exits active play.

Examples:
- `geologic_stability`
- `residents`
- `cohesion`
- `vitality`
- `condition`

### 2. Need / Drive

What creates ongoing pressure and motivates behavior even when nothing external happens.

Examples:
- `satiety`
- `ambition`
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
- `influence`
- `regional_safety`

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

- `geologic_stability` -> Integrity
- `regional_safety` -> Projection / world-condition quality
- `mystic_resonance` -> Alignment

Regions intentionally do not fill all 5 buckets. They are slow world forces, not person-like actors.

### Settlement

- `residents` -> Integrity
- `security` -> Capacity
- `prestige` -> Projection
- `civic_ideology` -> Alignment

### Faction

- `cohesion` -> Integrity
- `support` -> Capacity
- `influence` -> Projection
- `civic_ideology` -> Alignment

### NPC

- `vitality` -> Integrity
- `satiety` -> Need
- `ambition` -> Drive
- `civic_ideology` -> Alignment

### Artifact

- `condition` -> Integrity
- `containment` -> Drive / pressure
- `power` -> Capacity / Projection
- `mystic_resonance` -> Alignment

---

## Selection Rules

When adding or changing attributes:

- prefer monotonic bounded scalars where possible
- avoid duplicate concepts across buckets
- avoid midpoint "balance" attributes unless the simulation truly needs them
- favor attributes that multiple systems can read: planner, lifecycle, UI, pressures, future cards
- if an attribute does not create meaningful dynamics, cut it

This model is a design tool, not a rigid validator. It exists to keep the attribute set coherent while the simulation grows.
