# System 19: Action Executor

Enables actor behaviors and resolves their outcomes. Defines what the world's entities CAN DO.

---

## Purpose

Action cards define the possibility space. When an action card is in the tableau, entities of the specified kinds GAIN the ability to perform that action. Without the card, the action doesn't exist in the simulation. "Declare War" in the tableau means nations can go to war. Remove it and war becomes impossible -- existing wars may persist (the relationships remain) but no new wars can be declared.

Actions are the bridge between the entity behavior system (which produces impulses and conditions) and significant world-state changes (new relationships, status transitions, entity creation). Low-altitude impulses build conditions. Actions produce outcomes.

---

## How It Differs from Base

Actions extend the card executor base with:

### The executor doesn't fire the action -- actors do

Unlike generators and systems (where the executor evaluates preconditions and fires effects directly), the action executor REGISTERS available actions with the entity behavior system. Actors (nations, factions, cultures, heroes -- any entity kind with agency) then evaluate available actions during their impulse evaluation, using the utility AI to score and select.

The flow:
1. Action card is in the tableau -> executor registers the action as available
2. Entity behavior system runs impulse evaluation for an actor entity
3. Actor evaluates registered actions alongside its normal impulses
4. If the action scores highly AND its preconditions are met for this specific actor, the actor selects it
5. The action executor resolves the action: applies mutations, records the outcome

This means the executor is both a registration system (making actions available) and a resolution system (applying effects when actors choose the action). It does NOT decide when to fire -- actors do.

### Actor selection and targeting

Actions have two selection phases:

**Actor selection:** which entities CAN perform this action. Defined per card:
- By kind: "only nations can declare war"
- By subtype: "only merchant heroes can establish trade"
- By attribute: "only entities with military strength above X"
- By relationship: "only entities with a rival_of relationship"
- Filters can compose with the full precondition system

**Target selection:** given a specific actor, which entities can be the TARGET. Defined per card:
- By kind: "war targets must be nations"
- By relationship to actor: "target must be a rival_of the actor"
- By spatial proximity: "target must be in an adjacent region"
- By attribute: "target must have stability below X" (vulnerable targets)
- Pick strategy: random, nearest, weakest, strongest, highest-disposition, lowest-disposition

Both actor and target selection are preconditions. If no valid actor-target pair exists, the action's preconditions are not met.

### Actions nearly always produce new relationships

This is the key distinction from impulses (System 7). Impulses are small, incremental, relationship-preserving. Actions are significant, discrete, and relationship-creating/destroying:

- "Declare War" creates an at_war_with relationship
- "Forge Alliance" creates an allied_with relationship
- "Establish Trade" creates a trades_with relationship
- "Annex Territory" transfers a controls relationship from one nation to another
- "Found Dynasty" creates a lineage_of relationship between a hero and a nation
- "Cultural Exchange" creates an influenced_by relationship between cultures

Some actions modify existing relationships rather than creating new ones:
- "Betray Alliance" culls an allied_with relationship and may create an enemy_of relationship
- "Strengthen Bond" increases an existing relationship's strength
- "Sever Ties" culls a relationship

### Success and failure outcomes

Actions have dual outcomes. When an actor selects an action and the executor resolves it, there is a success/failure determination:

- **Success probability:** defined per card, may be modified by actor attributes, target attributes, relationship state, or card modifications
- **Success effects:** the intended outcome (alliance formed, war declared, trade established)
- **Failure effects:** the consequences of a failed attempt (reputation damage, relationship worsening, resource loss)

Both success and failure produce mutations on the entity graph. A failed alliance attempt might worsen the relationship between the two nations. A failed invasion might reduce the attacker's military strength.

### Cooldown is per-actor, not per-card

A system card's cooldown is global: "this system can only fire every N ticks." An action card's cooldown is per-actor: "each actor can only attempt this action every N ticks." Multiple actors can attempt the same action on the same tick. But the same actor cannot spam the same action.

---

## Action-Specific Preconditions

In addition to the base precondition types:

- **Disposition threshold:** "actor's disposition toward target is above/below X" (willingness to cooperate or confront)
- **Relationship prerequisite:** "actor must have/lack a specific relationship type with target" (can't declare war on an ally without first breaking the alliance)
- **Mutual exclusion:** "actor cannot have another action of type X active" (can't simultaneously be at war and in alliance negotiations with the same target)
- **Actor state:** "actor's semantic plane position must be in the [label] region" (only authoritarian nations can perform "Impose Vassalage")

---

## Achievement Conditions (Deck Modification Triggers)

Action cards track usage toward deck modification events:

- **Fire count:** "this action has been performed N times this run"
- **Success rate:** "this action has succeeded N times"
- **Diversity:** "this action has been performed by N distinct actors"
- **Consequence tracking:** "relationships created by this action have survived for N ticks" (measuring lasting impact)
- **Chain tracking:** "this action has been followed by action Y within N ticks" (measuring action sequences)

---

## Example Action Card (Illustrative)

```
action: "Forge Alliance"
mana_cost: 3
cooldown_per_actor: 30 ticks
cadence: 5 ticks (how often actors evaluate this action)
tags: [diplomacy, connectivity, relationship]

actor_selection:
  kind: [nation, faction]
  filters:
    - attribute("stability", min=20)  # must be stable enough to commit
    - NOT relationship_exists(actor, "at_war_with", target=$any)  # not currently at war

target_selection:
  kind: same_as_actor
  filters:
    - disposition(actor, target, min=0.6)  # must have positive disposition
    - NOT relationship_exists(actor, "allied_with", target)  # not already allied
    - spatial_proximity(actor, target, max_distance=20)
  pick: highest_disposition

preconditions:
  AND(
    actor_available(),
    target_available(),
    pressure("military_tension", region=shared_region(actor, target), min=20)
      # some tension motivates seeking allies
  )

success_probability: 
  base: 0.6
  modifiers:
    - attribute(actor, "stability"): +0.01 per point above 50
    - disposition(actor, target): +0.2 per 0.1 above 0.6
    - semantic_distance(actor, target): -0.1 per unit distance

success_effects:
  - create_relationship(kind="allied_with", source=$actor, target=$target, strength=0.6, symmetric=true)
  - modify_attribute(entity=$actor, attribute="stability", delta=+5)
  - modify_attribute(entity=$target, attribute="stability", delta=+5)
  - modify_pressure(pressure="military_tension", region=shared_region($actor, $target), delta=-15)

failure_effects:
  - modify_attribute(entity=$actor, attribute="stability", delta=-3)
  - modify_disposition($actor, $target, delta=-0.1)  # failed attempt sours relations

achievement_conditions:
  - { type: fire_count, target: 3, reward: deck_modification }
  - { type: success_count, target: 5, reward: deck_modification }
  - { type: surviving_relationships, min_ticks: 50, target: 2, reward: deck_modification }
```

---

## Relationship to Entity Behaviors (System 7)

The action executor and entity behavior system are tightly coupled:

**Entity Behaviors provides:** impulse evaluation, actor state, goal tag response. The utility AI scores available actions alongside impulses. An action that scores highly enough is selected by the actor.

**Action Executor provides:** the set of available actions (registered from active action cards). Resolution logic when an actor selects an action.

**The boundary:** entity behaviors decides WHAT the actor wants to do. The action executor determines WHETHER it succeeds and WHAT the consequences are.

**Impulses vs Actions (recap):**
- Impulses: always available, small/frequent, attribute-modifying, no card required
- Actions: card-gated, significant/infrequent, relationship-creating, card required in tableau

An actor's tick evaluation considers BOTH its impulses (always available) and any registered actions (from active action cards). The utility AI scores all of them on the same scale. An action must score higher than all impulses to be selected -- it must be genuinely the best thing the actor can do right now.

---

## Interactions with Other Systems

| System | Action Executor-Specific Interaction |
|--------|--------------------------------------|
| Entity Behaviors (#6) | Registers available actions. Actors evaluate and select actions during impulse evaluation. |
| Entity Graph (#1) | Creates new relationships (primary purpose). May create entities (less common than generators). |
| Relationships (#7) | Creates, modifies, and culls relationships. Actions are the primary card-driven topology changer. |
| Attributes (#3) | Success/failure effects modify actor and target attributes. |
| Pressure (#11) | Actions may directly modify pressure (e.g., alliance reduces military tension). |

---

## Open Questions (Deferred)

- **Action scoring integration.** How the utility AI scores registered actions against impulses. Whether actions use the same scoring curves or a separate evaluation. Whether action scores are boosted or penalized relative to impulses.
- **Multi-actor actions.** Can an action involve more than two entities? A "Form Coalition" action might require three nations. How does multi-party targeting work?
- **Action chains.** Can actions trigger other actions? If "Declare War" fires, can it immediately trigger "Call to Arms" for the defender's allies? Or must this wait for the next tick? (Current design: next tick.)
- **Actor agency vs player control.** The player enables actions via cards but doesn't choose WHEN actors use them. Should the player have any influence over action timing or targeting beyond card preconditions? (E.g., in patron mode, can the player nudge their favored nation to choose a specific action?)
- **Action visibility.** How the player sees actions being taken. Log entries? Map animations? Entity notifications? How much detail about success/failure probabilities is shown?
