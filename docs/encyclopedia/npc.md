# Penguin

A named penguin with agency. Wardens, brokers, witnesses, exiles. Penguins are the visible face of the simulation — their movements, choices, and fates create the stories that the player remembers.

## Subtypes

- **Warden** — defenders, patrol leaders, keepers of routes, and watch-commanders
- **Broker** — traders, couriers, fixers, negotiators, and keepers of exchange
- **Witness** — record-keepers, memory-bearers, interpreters of signs, and ritual speakers
- **Exile** — the displaced, severed, gathered, cast-out, and socially unmoored

## Key Attributes

| Attribute | Description |
|-----------|-------------|
| Vitality | The penguin's health metric — reaches zero and the penguin dies. |
| Warmth | The penguin's sustaining condition. Falls over time and drives movement toward food and shelter. |
| Echo | The remembered interior force that drives outward action. |

## Processes

### Eat
Consume food at the current settlement, restoring warmth and preserving vitality.
- **Reads:** self warmth, location settlement food availability
- **Writes:** self warmth (increases), self vitality (small increase), settlement sustenance (decreases)
- **Precondition:** at a settlement with food available, warmth below threshold
- **Cross-entity:** reduces food at the settlement — penguins compete for food

### Move Toward Target
Pathfind one hex step toward the penguin's current movement target.
- **Reads:** self movement target, self coordinates, map cell movement costs
- **Writes:** self coordinates (changes position)
- **Precondition:** has a movement target, not already at target
- **Note:** the movement target is set by OTHER processes

### Rest
Stay in place, recovering vitality. The default when no other process scores highly.
- **Reads:** self vitality
- **Writes:** self vitality (small increase)
- **Precondition:** none (always available, but scores low when other needs are pressing)

### Forage
Search for food outside settlements. Less efficient than eating at a settlement but doesn't consume settlement food.
- **Reads:** self warmth, map cell fertility
- **Writes:** self warmth (small increase based on fertility)
- **Precondition:** not at a settlement, or settlement has no food
- **Note:** foraging in infertile terrain provides almost no relief

### Train
Invest time in sharpening capability. Costs warmth but deepens the penguin's ability to act.
- **Reads:** self echo, self warmth
- **Writes:** self skill (increases), self warmth (decreases)
- **Precondition:** warmth above threshold

## Decisions (Card-Gated)

*See the card encyclopedia for penguin-targeting decisions like Wield Artifact, Challenge Rival, Mentor, and Swear Loyalty.*

## What Sustains a Penguin

Vitality is the penguin's health metric. It is sustained by:
- Eating at prosperous settlements
- Resting
- Effect transfer from held artifacts

It is threatened by:
- Lifecycle aging drain (old NPCs lose HP faster)
- Loss of warmth (sustained deprivation drains vitality indirectly)
- Combat and hazards (future: decisions that cost HP)
- Starvation (no food available anywhere)
