# Balatro Card Model Research

Research into Balatro's card modification and enhancement systems, for informing our card design (Systems 13-14 and card definitions generally).

---

## Why Balatro Matters for This Game

Balatro separates the **engine** (Jokers) from the **fuel** (playing cards). Your deck of cards is raw material processed through your Joker engine. This creates a fundamentally different modification experience from Slay the Spire, where each card IS the engine.

The key innovations:
1. **Cards have identity.** Your specific Ace of Hearts with Polychrome edition and Red Seal is unlike any other card. You built it. You care about drawing it.
2. **Layered modification.** Three independent layers (enhancement + edition + seal) on each card, plus the Joker engine processing them.
3. **Modification is incremental.** You don't find a "better card." You make existing cards better over time through consumables. This creates attachment.
4. **Engine vs fuel distinction.** Jokers modify the system (change rules about how scoring works). Card modifications make specific cards special. Two parallel upgrade paths.

---

## Joker System: Persistent Modifiers

Jokers sit in a separate row (up to 5 slots, expandable). They provide persistent passive effects. Evaluated left-to-right during scoring -- order matters.

**Joker categories:**
- **Additive:** flat bonuses. Scary Face (+30 Chips if hand has a face card).
- **Multiplicative (xMult):** multiply the entire score. The Duo (x2 Mult if hand has a Pair). These scale exponentially when combined.
- **Economy:** affect money. Golden Joker (earn $4 at end of round).
- **Conditional/Scaling:** grow over the run. Hologram (gains x0.25 Mult every time a card is added to deck). Green Joker (+1 Mult per hand played, accumulating).
- **Retrigger:** cause cards to score multiple times. Sock and Buskin (retriggers face cards).
- **Rule-changing:** alter fundamental game rules. Four Fingers (flushes/straights need 4 cards not 5). Shortcut (straights can have gaps).

**Joker interactions:** because Jokers evaluate left-to-right and additive fires before multiplicative, sequencing creates combos. Blueprints (copies the Joker to its right) duplicates strong xMult Jokers.

**5-slot limit** creates constant tension: every Joker added means another excluded. Selling reliable early-game Jokers to make room for powerful late-game ones is a standard inflection point.

**Mapping to our game:** Jokers map to persistent civilization-level modifiers. A "Maritime Trade Network" modifier could amplify all Commerce-tagged cards. Slot limit creates the same tension as our mana cap. The additive vs multiplicative distinction maps to flat bonuses vs percentage bonuses on simulation effects.

---

## Three-Layer Card Modification

Each playing card can have ONE of each layer simultaneously:

### Enhancements (mutually exclusive, one per card)

Change a card's fundamental behavior:
- **Bonus Card:** +30 Chips when scored
- **Mult Card:** +4 Mult when scored
- **Wild Card:** counts as every suit simultaneously
- **Glass Card:** x2 Mult when scored, but 1-in-4 chance of being DESTROYED
- **Steel Card:** x1.5 Mult while HELD IN HAND (not played). Encourages holding rather than playing.
- **Stone Card:** +50 flat Chips regardless of rank. Always scores even if not part of the poker hand.
- **Gold Card:** earn $3 when held in hand at end of round
- **Lucky Card:** chance-based Mult or money

**Mapping to our game:** enhancement determines HOW the card functions in the tableau. A "Glass" equivalent: card has amplified effect but risks being destroyed after firing. A "Steel" equivalent: card provides passive bonuses while in hand, not just when in tableau. A "Wild" equivalent: card counts as multiple types simultaneously (a generator that also functions as a system).

### Editions (independent layer, one per card)

Visual and mechanical modifier stacking on top of enhancement:
- **Foil:** +50 Chips
- **Holographic:** +10 Mult
- **Polychrome:** x1.5 Mult

Editions also apply to Jokers. A Polychrome Joker that provides xMult compounds terrifyingly.

**Mapping to our game:** edition determines potency. A Polychrome Cultural Diffusion spreads culture faster/further than a base Cultural Diffusion.

### Seals (independent layer, one per card)

Secondary trigger effects:
- **Gold Seal:** earn money when card is played and scores
- **Red Seal:** retriggers the card once (card scores twice)
- **Blue Seal:** creates a Planet card when held at end of round
- **Purple Seal:** creates a Tarot card when discarded

**Mapping to our game:** seal determines secondary effects. A Red Seal generator fires its creation twice. A Purple Seal card generates a consumable when removed from tableau.

### Why layering works

Three independent layers on a single card means each card has 8+ possible configurations (enhancement × edition × seal). This creates attachment: you invested in modifying this specific card, and now it's unique to your run. The "build your card" feeling is stronger than the "find a better card" feeling.

---

## Consumables: Modification Mechanics

### Tarots (primary card modifier)

Single-use consumables that apply enhancements or editions to selected cards. Hold up to 2. Acquired from shops and card packs.
- The Magician: enhance cards to Lucky
- The Empress: enhance cards to Mult
- Strength: increase card rank by 1
- Death: transform one card into a copy of another (duplicate your best modified card)
- The Hanged Man: destroy cards (deck thinning)

### Planets (category upgrader)

Upgrade entire poker hand TYPES. Each use permanently increases base stats for that hand type. Buying Neptune repeatedly makes all Flushes permanently stronger.

**Mapping to our game:** upgrade entire card types. A Planet equivalent could permanently improve all Generator cards, or all cards tagged "connectivity."

### Spectrals (rare, high-risk/high-reward)

Dramatic effects with severe costs. Aura (add random edition to a card). Wraith (create a Rare Joker but set money to $0). Ouija (add rank to all cards but -1 hand size permanently).

**Mapping to our game:** rare deck modification events with significant tradeoffs. "Restructure your entire tableau, but lose 2 mana cap permanently."

---

## Deck Size Strategy

Standard 52-card deck. Smaller is generally better (draw your modified cards more often). Thinning via The Hanged Man Tarot and shop removal (cost escalates: $1, $2, $3...).

Some strategies want larger decks: Hologram (gains xMult when cards are added), Trading Card (earn money for discarding). But these are the exception.

**Mapping to our game:** deck thinning represents civilizational focus -- abandoning capabilities to become more reliable at others. Escalating removal cost mirrors the difficulty of abandoning entrenched systems. Specialization vs generalization as core strategic axis.

---

## Economy: The Shop

Between rounds:
- 2 Jokers for sale (rerollable at $5 per reroll)
- 2 card packs (containing Tarots, Planets, Jokers, or enhanced playing cards)
- 1 voucher (permanent run upgrade: more Joker slots, cheaper rerolls, etc.)
- Card removal option (escalating cost)

Interest mechanic: earn $1 per $5 held, up to $5 at $25. Creates classic tension: spend now on upgrades vs save for passive income.

**Mapping to our game:** deck modification events are our "shop." The choices (add card, remove card, increase mana, modify a card) map to Balatro's shop decisions. The escalating cost of removal maps to deck thinning tension.

---

## Roguelite Progression

- Winning runs (and meeting conditions) unlocks new Jokers into the pool
- Starting deck variants (Plasma Deck merges Chips/Mult, Abandoned Deck has no face cards) change the starting conditions dramatically
- 8 difficulty stakes add restrictions (fewer discards, score inflation, boss mechanics)
- Unlocks expand the pool, not per-run power

**Mapping to our game:** strategy archetypes are our starting deck variants. Meta-progression unlocks new cards into the pool and new archetypes. Difficulty could escalate lose condition thresholds or reduce starting mana cap.

---

## Key Takeaways for Our Card Design

1. **Cards should have identity.** A "Cultural Diffusion" card that the player has modified over the run should feel like THEIR Cultural Diffusion, not a generic one. Per-card modification creates attachment.

2. **Separate the engine from the fuel.** Joker-equivalents (persistent modifiers) vs deck cards (the components you play). This may map to: the locked tableau / persistent civilization traits (engine) vs the cycling deck of playable components (fuel). Or it may be a layer on top of the existing system.

3. **Layered modification over replacement.** Don't make the player find a "better Cultural Diffusion." Let them enhance the one they have. Multiple independent modification axes create combinatorial depth.

4. **Make deck thinning meaningful.** Removing cards increases the frequency of remaining cards. This should be a real strategic choice in our game -- a player who removes 5 generators to keep 3 excellent ones sees those 3 more often.

5. **Consumables as modification delivery.** Tarots and Spectrals are the mechanism for applying modifications. Our deck modification events serve the same role -- they deliver the modification opportunity, but the player chooses what to modify.

6. **Escalating costs prevent infinite optimization.** Each card removal costs more. Each reroll costs more. The player can't perfect their deck -- they run out of resources. Our mana cap expansion being one-of-N choices at modification events serves the same purpose.
