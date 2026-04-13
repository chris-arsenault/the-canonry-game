# System 14: Tableau / Mana

The player's active card configuration, resource management, and the interface between cards and the running simulation.

---

## Purpose

The tableau is where cards become real. A card in the hand is potential. A card in the tableau is an active simulation component -- a generator creating entities, a system shaping forces, an action enabling behaviors. The mana pool determines how many cards can be active simultaneously, gating the complexity of the player's world-shaping.

---

## Design Decisions

### Playing a card is instant

When the player moves a card from hand to tableau (and has sufficient mana), the card immediately activates. Its simulation component registers with the appropriate executor and begins evaluating preconditions on the next tick. There is no ramp-up period, no summoning sickness, no activation delay.

This supports reactive play: the player sees a crisis forming, plays a response card, and it takes effect immediately. The contemplative tempo doesn't mean slow controls -- it means the player takes time to DECIDE, not that decisions take time to execute.

### Removing a card stops ongoing effects but preserves history

When the player removes a card from the tableau:
- The simulation component deregisters. It stops firing on subsequent ticks.
- Any effects that already occurred remain in place. Entities created by a generator still exist. Relationships created by an action card still exist. Attribute changes from a system card are not reversed.
- The card returns to the deck's discard pile and will eventually cycle back through draws.

This means the tableau is not a toggle -- playing and removing a card has permanent consequences. Running "Cultural Diffusion" for 50 ticks and then removing it leaves the cultural landscape changed. The pressure that was building doesn't snap back; it just stops being actively pushed.

### Single mana pool for MVP

All cards draw from one mana pool. A generator that costs 3 mana and a system that costs 2 mana compete for the same resource. This creates pure opportunity cost: every card played is mana not available for other cards.

**Future consideration:** differentiated mana types (creation mana, influence mana, etc.) or minimum per-type requirements (at least one generator of each entity kind must be active). These may be needed to prevent degenerate tableaux (all systems, no generators = world stagnates). For MVP, the single pool combined with the stagnation lose condition should provide sufficient pressure to diversify.

### Mana cap expands through deck modification events

The mana cap increases as a reward during deck modification events (the same milestone-driven events described in System 13). When a card's achievement conditions are met and a modification event fires, the player chooses from options that may include:
- Add a card to the deck
- Remove a card from the deck
- **Increase mana cap**
- Other options TBD (card upgrades, card modifications -- see Balatro research)

This ties mana growth to simulation engagement. The player earns more complexity budget by actively using their cards and achieving their conditions. A passive player who never fires their generators never earns modification events, never expands their mana cap, and is stuck with a small tableau.

Starting mana cap is TBD (needs playtesting). Likely enough for 3-4 active cards, expandable to 7-9 through a full run's worth of modification events.

### The tableau has no slot limit beyond mana

There is no hard slot limit on the tableau. The constraint is purely mana: each card costs mana, and the player can place cards until their mana is exhausted. A player with 10 mana and cards costing 2 each can have 5 active. A player who saved modification events for mana cap increases might have 15 mana and run 7 cards.

This means card mana costs are a critical balancing lever. Powerful cards should cost more mana, creating tension between a few powerful cards and many weaker ones.

### Tableau composition is visible and inspectable

The player can see at all times:
- Which cards are in the tableau
- What each card does (preconditions, effects, type)
- Current mana usage vs. cap
- Whether each card's preconditions are currently met (is this card "firing" or "waiting"?)
- Achievement condition progress per card (toward deck modification events)

This supports the "everything visible" design principle. The player understands exactly what their tableau is doing to the simulation.

---

## Tableau Interactions

### Playing a card (hand -> tableau)

1. Player selects a card from hand
2. System checks: does the player have enough mana? (card cost <= available mana)
3. If yes: card moves to tableau, mana pool decreases by card cost, card's simulation component registers with the appropriate executor
4. If no: card cannot be played. Player must remove something from the tableau first or wait for mana cap expansion.

### Removing a card (tableau -> discard)

1. Player selects a card in the tableau
2. Card's simulation component deregisters from its executor
3. Mana pool increases by the card's cost (mana is freed)
4. Card moves to the deck's discard pile
5. Existing effects from the card remain in the entity graph

### Swapping (removing + playing)

No special swap mechanic. The player removes a card (freeing mana) then plays a different card (spending mana). These are two discrete actions. The removed card's effects stop; the new card's effects start on the next tick.

There is no cooldown, cost, or restriction on swapping for MVP. The player can reconfigure their tableau freely at any time. If this proves too flexible (allowing trivial min-maxing), cooldowns or swap costs can be added as tuning levers.

---

## Card Cost Design

Card mana costs are a primary balancing lever:

- **Cheap cards (1-2 mana):** narrow effects, situational utility. Can run many simultaneously. "A specific cultural impulse modifier" or "a minor action enabling a rare behavior."
- **Mid-cost cards (3-4 mana):** meaningful effects, the backbone of most tableaux. Most generators, systems, and major actions. "Cultural Diffusion," "Found Settlement," "Declare War."
- **Expensive cards (5+ mana):** powerful, broad effects that shape the simulation significantly. Running one means NOT running 2-3 cheaper cards. "Continental Trade Network," "Age of Empires," "Divine Mandate."

The cost spectrum creates the thin-vs-fat tableau tradeoff: a few powerful cards vs. many specific ones.

---

## Deck Modification Events (Expanded)

Deck modification events are the primary progression mechanic within a run. They occur when card-specific achievement conditions are met. The player chooses ONE reward from the options presented:

**Confirmed options:**
- Add a card to the deck (from the unlocked pool, probably draft 1 of 3)
- Remove a card from the deck (thinning -- increases frequency of remaining cards)
- Increase mana cap (allows more simultaneous tableau cards)

**Balatro-inspired options (see `docs/research/balatro-card-model.md`):**
- **Enhance a card:** apply a modification layer to a specific card in the deck. Like Balatro's Tarot cards applying enhancements, this makes a specific card MORE powerful or changes HOW it works. A "Glass" enhancement on a generator might double its output but risk the card being destroyed after firing. A "Wild" enhancement might let a system card count as multiple types. Multiple independent modification layers per card create combinatorial depth and card identity -- "this is MY Cultural Diffusion, not a generic one."
- **Upgrade a card type:** improve ALL cards of a category, like Balatro's Planet cards upgrading entire hand types. "Upgrade all generators" or "upgrade all connectivity-tagged cards."
- **Acquire a persistent modifier:** Balatro's Jokers are persistent passive effects in limited slots. Our equivalent might be civilization-level traits or institutions that modify how all cards of a certain tag behave. "Maritime Trade Network" amplifies all Commerce-tagged cards. These would occupy their own limited slots, separate from the tableau.

The choice between these options IS the mid-run strategic decision. Do you want more cards (breadth), fewer better cards (depth), or more capacity (scale)? The answer depends on the current world state and the player's evolving theory about what the world needs.

---

## Interactions with Other Systems

| System | How Tableau/Mana Interacts |
|--------|----------------------------|
| Deck (#12) | Cards flow from hand to tableau (play) and from tableau to discard (remove). |
| Card Executors (#16-18) | Tableau cards register/deregister with executors when played/removed. |
| Entity Graph (#1) | Card effects write to the graph. Removing a card doesn't undo graph changes. |
| Attributes (#3) | System cards in the tableau modify entity attributes each tick. |
| Pressure (#11) | Card preconditions read pressure values. Card effects can modify pressure directly. |
| Tick Clock (#5) | Tableau cards fire on tick phases (after low-altitude, after pressure recomputation). |
| Save/Load (#15) | Tableau state (which cards, mana pool, mana cap, achievement progress) is serialized. |
| Lose Condition Monitor (#14) | Tableau composition affects lose condition risk. No generators = stagnation risk. All generators = monoculture risk. |

---

## Open Questions (Deferred)

- **Starting mana cap.** Enough for 3-4 cards? Needs playtesting against card cost ranges.
- **Mana cap growth rate.** How many modification events per run? How much cap per increase? Needs to feel meaningful without making late-game tableaux unmanageably large.
- **Card cost balancing.** The entire cost structure needs iteration. Costs are the primary lever for making the tableau constraint feel right.
- **Swap restrictions.** MVP has no cooldown or cost for swapping. If this makes the game too fluid (no commitment to a configuration), add friction: cooldown per swap, mana cost for swapping, or a "configuration lock" period after changes.
- **Card modification layers.** Balatro research suggests three independent modification axes per card (enhancement + edition + seal equivalent). How these map to simulation cards: does enhancement change HOW the card fires, edition change POTENCY, and seal add SECONDARY EFFECTS? How many layers per card? Are modifications permanent across deck cycles? See `docs/research/balatro-card-model.md`.
- **Persistent modifiers (Joker equivalent).** Whether the game has a separate row of persistent civilization-level modifiers, distinct from the tableau, that amplify/modify all cards matching certain tags. How many slots. How acquired. Whether these are the same thing as the "locked tableau" concept from design constraints.
- **Minimum tableau requirements.** Whether the game requires at least one card of each type (generator, system, action) or whether fully skewed tableaux are allowed and the lose conditions handle balance.
- **Tableau visual design.** How the active cards are displayed alongside the map. Sidebar, bottom panel, overlay. How much space the tableau occupies vs. the map and entity details.
