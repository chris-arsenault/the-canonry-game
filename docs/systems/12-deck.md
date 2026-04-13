# System 13: Deck

The player's card collection, draw mechanics, and deck evolution.

---

## Purpose

The deck is the player's theory about what kind of world they want to create. It defines which generators, systems, and actions are available to shape the simulation. The deck differentiates runs: two runs with different decks produce fundamentally different worlds.

The deck is also the primary roguelite mechanic. It starts with a strategy archetype, evolves during the run through milestone-driven modifications, and cycles continuously through the player's hand.

---

## Design Decisions

### Flow: deck > hand > tableau

Cards flow through three states:

1. **Deck:** the full set of cards for this run. Cards cycle through the deck repeatedly (it reshuffles when exhausted). The deck is not visible card-by-card -- the player knows what's in it but not the draw order.

2. **Hand:** cards drawn from the deck. The player's current options. Persistent -- cards stay in hand until played to the tableau or explicitly discarded. Hand has a size limit (starting ~3, max ~7, exact numbers TBD through playtesting).

3. **Tableau:** active cards running in the simulation. Each card in the tableau is a live simulation component (generator, system, or action). Cards stay in the tableau until the player removes them, at which point they return to the deck's discard pile.

When the draw pile is exhausted, the discard pile reshuffles into a new draw pile. Every card in the deck eventually cycles through the hand. This means deck COMPOSITION is the strategic lever -- what's in the deck determines what the player will see, and how frequently (thin deck = see key cards often, fat deck = more variety but less consistency).

### Starting deck: strategy archetype selection

At run start, the player selects a strategy archetype. Each archetype provides a pre-built starter deck themed around a worldview:

- The archetype sets the initial deck composition (which generators, systems, and actions are included)
- Different archetypes emphasize different aspects of the simulation (one might be heavy on cultural generators and diplomatic actions, another on territorial systems and military actions)
- The archetype is the player's starting thesis: "I believe THIS kind of world will be interesting"

The player does NOT build the deck card-by-card before the run. They choose a direction and get a curated foundation. This reduces pre-run decision paralysis while still making the choice meaningful.

Archetypes are unlocked through meta-progression. A new player has 2-3 archetypes. Experienced players have more, reflecting broader understanding of how card combinations interact.

### Two distinct deck interactions: drawing and modification

**Drawing (frequent, expected):** cards move from deck to hand. Draws happen on two triggers:

- **Timed:** a draw occurs at regular intervals during the simulation (exact cadence TBD -- "every minute" equivalent in simulation time). This provides a steady flow of options.
- **Milestone-triggered:** a draw occurs when simulation milestones are reached. A settlement reaches a population threshold. A nation controls a certain number of regions. A pressure value crosses a level. These are game-state-driven draws that the player can work toward -- steering the simulation partly to unlock draws, creating a secondary motivation loop.

Drawing is NOT a major decision. The player draws and sees what they get. The decision is what to DO with what's in hand (play to tableau, hold for later, or discard to cycle toward something better).

**Deck modification (rare, powerful):** the composition of the deck itself changes. Cards are added to or removed from the deck. This is MUCH rarer than drawing and represents a significant run-shaping moment.

Deck modification is triggered by card-specific achievement conditions:
- A generator card might have conditions: "generate 5 entities" or "generate 2 entities of unique cultures." When met, a deck modification event fires.
- The modification event might offer: add a new card to the deck (from the unlocked pool), remove a card from the deck (thinning), upgrade a card (make it more powerful or change its effects), or swap a card for a different one.

This ties deck evolution to simulation engagement. The player must USE their cards effectively to earn the right to modify their deck. A generator that never fires never triggers its modification condition. This creates pressure to build a tableau that actually engages with the simulation, not just theoretically optimal cards that sit unused.

### Deck cycling creates probability management

Because the deck reshuffles and cycles, the player is managing probabilities:
- A thin deck (10-12 cards) cycles fast. The player sees their key cards frequently. But every card must pull its weight -- there's no room for situational cards.
- A fat deck (20+ cards) cycles slowly. More variety, more situational answers, but key cards appear less often. The player may wait many draws before seeing the card they need.
- Deck modification (adding/removing cards) shifts these probabilities. Removing a weak starter card increases the frequency of everything else.

This is the same strategic axis as Slay the Spire's thin-vs-fat decision, but in a continuous-time context rather than per-combat.

### No turns, persistent hand

There are no turns. The simulation runs continuously. The player's hand persists -- drawn cards stay until played or discarded. This creates a different feel from Slay the Spire:

- No "end of turn discard" pressure. The player can hold cards indefinitely, waiting for the right moment.
- Hand size limit creates a different pressure: if your hand is full, you can't draw more. Playing or discarding something makes room for the next draw.
- The tension is not "play everything this turn" but "is now the right time to commit this card to the tableau, or should I hold it for a better moment?"

### The deck is the roguelite core

The deck IS the run. Different archetype selections, different draw sequences, different modification choices, different emergent synergies -- this is what makes each run distinct. The deck is not a sidecar to the simulation. It is the player's primary instrument for shaping the world's nature.

The roguelite loop:
1. Select archetype (starting deck composition)
2. Play the run: draw cards, manage hand, build tableau, observe simulation
3. Earn deck modifications through card achievement conditions
4. Adapt deck composition based on what the simulation actually needs
5. Eventually lose (extinction/stagnation/monoculture) or choose to end
6. Meta-progression: unlock new cards and archetypes for future runs
7. Start again with deeper understanding and a broader card pool

---

## Card Properties

Each card in the deck has:
- **Type:** generator, system, or action
- **Mana cost:** how much mana it consumes when placed in the tableau
- **Tags:** keywords for synergy identification (connectivity, conflict, creation, etc.)
- **Preconditions:** what simulation state must be true for the card to fire when in the tableau
- **Effects:** what the card does when it fires
- **Modification conditions:** (optional) achievement conditions that trigger deck modification events when met. Tracked per-card, reset each run.

---

## Interactions with Other Systems

| System | How Deck Interacts |
|--------|---------------------|
| Tableau/Mana (#13) | Cards flow from hand to tableau (play) and from tableau back to discard (remove). Mana cost is checked on play. |
| Card Executors (#16-18) | Cards in the tableau are registered with the appropriate executor based on type. |
| Tick Clock (#5) | Timed draws are triggered by tick count intervals. |
| All low-altitude systems | Milestone draw triggers are evaluated against simulation state (entity counts, attribute thresholds, pressure levels). |
| Save/Load (#15) | Full deck state (draw pile order, discard pile, hand contents, modification progress) is serialized. |
| Lose Condition Monitor (#14) | Deck composition affects which lose conditions are easier/harder to manage. |

---

## Open Questions (Deferred)

- **Archetype definitions.** What the starting archetypes are, how many, what cards each contains. Depends on the full card list, which depends on entity kinds and simulation system design.
- **Draw cadence.** How frequent timed draws are. Too fast = hand is always full, draws feel cheap. Too slow = player feels starved. Needs playtesting.
- **Milestone draw specifics.** Which simulation milestones trigger draws. How many milestones exist. Whether milestones are fixed or archetype-dependent.
- **Deck modification event design.** What choices the player gets during a modification event. Whether modification conditions are visible to the player (so they can work toward them) or discovered through play.
- **Card upgrade mechanics.** What "upgrading" a card means. More powerful effects? Lower mana cost? Additional preconditions met? Changed card type?
- **Starting hand.** Does the player draw an initial hand at run start, or does the hand start empty with the first draws coming from timed/milestone triggers?
- **Discard mechanics.** Can the player voluntarily discard from hand? Is there a cost? Does discarding feed back into anything (e.g., a "discard to gain a small benefit" mechanic)?
- **Deck size limits.** Minimum and maximum deck size. Whether the archetype enforces a starting size. Whether modification can grow the deck beyond a cap.
- **Synergy between cards.** How tag-based synergy works across deck, hand, and tableau. Whether having synergistic cards in the same deck (even if not simultaneously in the tableau) provides benefits.
