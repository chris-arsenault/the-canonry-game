# System 8: Resources / Economy (Post-MVP Direction)

Post-MVP resource work should extend the ambient hex-resource model, not replace it with a default settlement-centric economy.

---

## Non-Negotiable Principle

The distinction between:

- **ambient possibility space**
- **concrete available-now stores/services**

must remain explicit.

The MVP system currently models the first. Any later logistics, stores, trade, taxation, or extraction layer should be added as a deliberate second layer rather than quietly collapsing both meanings into one number.

---

## What Post-MVP Expansion Should Add

### 1. Slow world change

Good future additions:

- irrigation or drought changing ambient `food`
- mining or infrastructure changing ambient `wealth`
- migration or crowding changing ambient `population`
- region-scale climate or mystical effects nudging ambient fields over time

These preserve the core idea that hex resources describe what a place can support.

### 2. Explicit conversion processes

If the game needs stronger materiality, add conversion processes at named entities or locations:

- taxing ambient `wealth` into faction benefit
- turning ambient `food` quality into hospitality/sustainment/services
- using ambient `population` as unnamed labor or carry capacity

These should be expressed as explicit actions or services, not hidden automatic background math.

### 3. Limited logistics only when gameplay needs it

If the simulation genuinely needs "available now" semantics, add a clearly separate layer such as:

- named-location stores
- service capacities
- route-based transfers
- card-enabled trade or taxation policies

This should happen only when those distinctions matter to gameplay and player decision-making.

---

## What Post-MVP Expansion Should Avoid

Avoid drifting into:

- a default auto-flow economy just because it is implementable
- per-kind pseudo-inventories that recreate actor-owned resources by accident
- a large logistics simulator that dominates the rest of the game

The resource layer is meant to support the planner and world-state, not replace them.

---

## Planner Implications

Post-MVP resource work should keep supporting both planning patterns:

### Blocker resolution

```text
Need action X
  -> blocked because ambient resource threshold is too low
  -> find a process that raises that ambient field
```

### Utility improvement

```text
Need action X
  -> action is legal but weak
  -> support action improves the ambient field
  -> later executions of X become better
```

This second pattern is important. Resources are not only blockers; they are also quality fields.

---

## Candidate Future Features

- region-driven drought / flood / fertility drift
- faction revenue extraction from high-wealth areas
- artifact movement constrained by ambient `population`
- card-enabled trade or taxation layers
- named-location services or stores if the game truly needs concrete local availability

Those all fit the current direction as long as the ambient-resource distinction stays explicit.
