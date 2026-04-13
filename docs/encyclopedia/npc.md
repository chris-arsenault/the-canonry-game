# NPC

A named individual with agency. Heroes, merchants, scholars, warriors. NPCs are the visible face of the simulation — their movements, choices, and fates create the stories that the player remembers.

## Subtypes

- **Hero** — exceptional individuals who shape history
- **Merchant** — trade-oriented, seek economic opportunity
- **Scholar** — knowledge-oriented, seek developed settlements
- **Warrior** — conflict-oriented, seek military opportunity

## Key Attributes

| Attribute | Description |
|-----------|-------------|
| HP | Health points. The NPC's health metric — reaches zero and the NPC dies. |
| Hunger | Current hunger level. Increases over time, reduced by eating. Drives movement toward food. |
| Ambition | Drive to seek opportunity. Influences goal selection — high ambition NPCs seek high-value locations. |
| Age | Time since creation. Increases lifecycle drain as NPC ages. |

## Processes

### Eat
Consume food at the current settlement, reducing hunger and slightly healing.
- **Reads:** self hunger, location settlement food availability
- **Writes:** self hunger (decreases), self HP (small increase), settlement sustenance (decreases)
- **Precondition:** at a settlement with food available, hunger above threshold
- **Cross-entity:** reduces food at the settlement — NPCs compete for food

### Move Toward Target
Pathfind one hex step toward the NPC's current movement target.
- **Reads:** self movement target, self coordinates, map cell movement costs
- **Writes:** self coordinates (changes position)
- **Precondition:** has a movement target, not already at target
- **Note:** the movement target is set by OTHER processes (eat sets target toward food, ambition sets target toward opportunity)

### Rest
Stay in place, recovering HP. The default when no other process scores highly.
- **Reads:** self HP
- **Writes:** self HP (small increase)
- **Precondition:** none (always available, but scores low when other needs are pressing)

### Forage
Search for food outside settlements. Less efficient than eating at a settlement but doesn't consume settlement food.
- **Reads:** self hunger, map cell fertility
- **Writes:** self hunger (small decrease based on fertility)
- **Precondition:** not at a settlement, or settlement has no food
- **Note:** foraging in infertile terrain provides almost no relief

### Train
Invest time in improving skills. Costs hunger (training is hard work) but builds capability.
- **Reads:** self ambition, self hunger
- **Writes:** self skill (increases), self hunger (increases)
- **Precondition:** hunger below threshold (can't train while starving)

## Decisions (Card-Gated)

*See the card encyclopedia for NPC-targeting decisions like Wield Artifact, Challenge Rival, Mentor, and Swear Loyalty.*

## What Sustains an NPC

HP is the NPC's health metric. It is sustained by:
- Eating at prosperous settlements (food reduces hunger, hunger reduction preserves HP)
- Resting (direct HP recovery)
- Effect transfer from held artifacts (artifact bonuses to holder)

It is threatened by:
- Lifecycle aging drain (old NPCs lose HP faster)
- Hunger (sustained hunger drains HP indirectly)
- Combat and hazards (future: decisions that cost HP)
- Starvation (no food available anywhere)
