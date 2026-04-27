# Artifact

A named, significant object. Weapons, armor, instruments, relics, scrolls. Only notable artifacts are tracked — generic items are not entities. Artifacts grant power to their holders and accumulate mystical resonance through involvement in significant events.

## Subtypes

- **Weapon** — combat artifacts (swords, bows, siege tools)
- **Armor** — protective artifacts (shields, helms, wards)
- **Instrument** — cultural artifacts (drums, horns, sacred bells)
- **Relic** — ancient objects of spiritual significance
- **Scroll** — knowledge artifacts granting abilities to their holders

## Key Attributes

| Attribute | Description |
|-----------|-------------|
| Condition | Physical integrity. The artifact's health metric — reaches zero and the artifact is destroyed. |
| Mystical Resonance | Accumulated spiritual energy. Grows from involvement in events. Can be spent on self-repair. |
| Power | The artifact's effective potency. Affects the magnitude of effects transferred to its holder. |

## Processes

### Accumulate Resonance
Absorb ambient mystical energy. Faster when held by an NPC (proximity to life and events concentrates energy).
- **Reads:** self resonance, whether held (via holds relationship)
- **Writes:** self resonance (increases, 2x rate when held)
- **Note:** resonance is the "fuel" for other artifact processes

### Resist Degradation
Spend accumulated resonance to slow the lifecycle drain on condition. The artifact fights entropy.
- **Reads:** self condition, self resonance
- **Writes:** self condition (small increase), self resonance (decreases — spent on repair)
- **Precondition:** resonance above minimum threshold, condition below maximum

### Radiate Influence
Project the artifact's power to affect nearby entities. Artifacts at settlements affect the settlement's attributes. Artifacts held by NPCs affect the NPC via the typed effect system.
- **Reads:** self power, self coordinates/holder location
- **Writes:** nearby entity attributes via effect transfer (power-dependent magnitude)
- **Cross-entity:** affects the holder (via holds relationship) and nearby settlement entities

## Decisions (Card-Gated)

*See the card encyclopedia for artifact-targeting decisions like Increase Power, Corrupt Holder, Call to Wielder, and Move via Carrier (costs population — someone has to pick it up).*

## What Sustains an Artifact

Condition is the artifact's health metric. It is sustained by:
- Resist degradation process (spending resonance to repair)
- Being held by an NPC (higher resonance accumulation = more fuel for repair)

It is threatened by:
- Lifecycle aging drain (condition decreases over time)
- Use in significant events (future: combat costs condition)
- Neglect (unheld artifacts accumulate resonance slower, can't repair as effectively)
