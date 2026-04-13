# Entity Behavior Models Research

Research into entity AI decision-making patterns and TTRPG attribute effect systems, for informing System 7 (Entity Behaviors) design.

---

## Part 1: Entity AI Decision-Making Patterns

### Civilization Series: Personality as Scoring Weights

Civilization V encodes AI personality as numerical bias values (1-10 scale with 2-point random variance): Expansion, Growth, Science Emphasis, Culture Emphasis, Gold Emphasis, Offense, Defense, etc. When deciding what to build, the AI combines each option's "flavor" tags with leader bias weights. A building with Science flavor 50 multiplied by Science bias 8 produces a weighted score. AI selects from top-scoring options with randomization.

Civilization VI evolved this into the **Agenda system**: each leader has a public and hidden agenda defining what they value and how they judge others. Agendas function as a utility scoring layer for diplomacy: compute opinion score based on how well others conform to the leader's agenda.

**Key insight: personality is numerical weights on a shared evaluation function, not separate behavioral code paths.** An aggressive leader and a peaceful leader run the same decision logic; they score the same options differently.

### Paradox Grand Strategy: Weighted Scoring with Conditional Modifiers

**Stellaris** uses `ai_weight` applied to nearly every decision. Each option starts with a base weight, then conditional modifiers multiply or add to it. An authoritarian empire evaluating a slave-processing building: base 1.5, +1.5 if slavery is legal (= 3.0), x2.0 if Slaver Guilds civic (= 6.0). Produces emergent strategic personality from shared evaluation framework.

**CK3** uses personality attributes derived from traits: Boldness (resistance to Dread), Compassion, Energy, Greed, Honor, Rationality, Sociability, Vengefulness. Each trait (Ambitious, Craven, etc.) sets these to specific values. AI uses attribute values as weights in decision evaluation. Behavior feels authentic to personality without hand-authored trees per trait.

**Victoria 3** uses three-strategy model: Administrative, Political, and Diplomatic strategies as meta-level utility functions constraining lower-level decisions. High Recklessness causes overestimating military strength. Personality distorts evaluation without changing evaluation structure.

### RTS AI: Influence Maps and Build Priorities

**Influence maps**: grid overlay where each cell accumulates values from nearby entities (friendly = positive, enemy = negative, decaying with distance). AI reads influence map to identify: areas of control, contested borders, undefended flanks, safe expansion zones. Multiple layers (military threat, economic value, exploration) combined for composite spatial evaluation.

**Directly relevant to this game**: the pressure system is architecturally an influence map system. Regional pressures (military tension, cultural friction) are computed the same way.

**Build order optimization** in RTS: multi-objective planning finding concurrent action sequences constrained by dependencies and resource availability. Total War uses modified GOAP for strategic campaign AI.

### Dwarf Fortress: Need-Based Individual Autonomy

Each dwarf has a **Focus** attribute reflecting need satisfaction. Needs include: pray, admire art, listen to poetry, socialize, train martial skills. Need priorities derived from personality and beliefs. Two priority levels: low-priority (interruptible by work) and high-priority (takes precedence over fortress tasks).

**Labor system** as capability filter: dwarves only perform jobs matching enabled labors. **Mood system** as rare behavioral override: strange moods single-mindedly pursue artifact creation.

### Design Pattern Comparison

| Pattern | How It Works | Strengths | Weaknesses | Fit for This Game |
|---------|-------------|-----------|------------|-------------------|
| **Utility AI** | Score multiple actions, pick highest (or weighted random from top) | Scales to many actions; personality via weights; tunable, debuggable | Erratic if poorly calibrated; weak at multi-step plans | **Excellent.** Character sheet drives scoring. Kind-agnostic evaluation framework. |
| **GOAP** | Define goals, planner finds action sequences via A* search | Intelligent multi-step behavior; dynamic goal priority | Expensive; requires complete world-state model; poor for continuous processes | **Poor for most kinds.** Nations don't plan discrete sequences. Heroes might benefit from simplified variant. |
| **Behavior Trees** | Priority-ordered tree; selectors/sequences control flow | Readable, debuggable; natural priority handling | Hand-authored, rigid; scales poorly to many kinds; binary branch logic | **Poor as primary.** Too rigid for continuous attribute spectra. |
| **FSM** | Discrete states with transition conditions | Simple, predictable | Quadratic state explosion above ~20 states | **Poor.** Entities exist on continuous spectra, not discrete states. |
| **Influence Maps** | Spatial heatmaps weighting decisions toward/away from areas | Spatial intelligence; composable; cheap to query | Passive -- informs but doesn't decide | **Excellent.** Pressure system already IS this. |
| **Weighted Random** | Probabilistic selection from options weighted by scores | Prevents determinism; varied playthroughs; personality via weight distributions | Can produce illogical choices | **Strong as modifier on Utility AI.** Compute scores, use as weights for probabilistic pick. |

### Recommended Architecture

**Utility AI as primary framework** + **influence maps (pressure system) as spatial inputs** + **weighted random as final selection mechanism.**

The CK3 model maps directly: personality attributes derived from entity state feed into a shared evaluation function. Each entity kind defines its own scoreable actions and scoring curves, but the evaluation engine is kind-agnostic. This matches the "character sheet drives behavior" design intent.

Concrete flow per entity per tick:
1. Read entity's attribute sheet (base + relationship-derived effects)
2. Enumerate available actions for this entity kind (+ any card-enabled actions)
3. Score each action using kind-specific utility curves weighted by entity attributes
4. Read relevant pressure/influence maps for spatial weighting
5. Read relevant goal tags for external priority weighting
6. Combine scores into final weights
7. Select action via weighted random from top-scoring candidates
8. Execute action (modify attributes, move, set goal tags, etc.)

---

## Part 2: TTRPG Effect Systems

### Foundry VTT Active Effects

Core principle: **non-destructive modification**. Base Data (inherent attributes) is separated from Derived Data (what the entity actually uses after effects). Effects modify a copy; removing the effect restores the original without "undo" logic.

**Effect definition:** each Active Effect specifies:
- **Attribute Key**: which attribute to modify (dot-notation path)
- **Mode**: how to apply (see below)
- **Value**: the modification amount

**Six modes, applied in strict order:**
1. CUSTOM (0): programmatic handling via hooks
2. MULTIPLY (1): multiply current value. Applied early so multipliers operate on base values.
3. ADD (2): add numeric value. Most common mode.
4. DOWNGRADE (3): set value only if effect value is LOWER (minimum enforcement)
5. UPGRADE (4): set value only if effect value is HIGHER (maximum enforcement)
6. OVERRIDE (5): completely replace the value. Applied last, supersedes all prior.

The ordering is critical: MULTIPLY before ADD means multipliers operate on base values, not base+bonuses. OVERRIDE last means it can force a specific value regardless.

### The Transfer Effect Pattern

Items containing Active Effects transfer those effects to their holder. A magical belt with +2 Brawn: the effect stays on the item but applies to the actor. When the item is removed, the effect stops applying automatically (no cleanup needed -- the effect's source no longer applies).

**Direct mapping to this game**: relationships are the "items" that carry effects. The related entity is the "actor" receiving effects. When a relationship is removed, its effects stop. A nation's infrastructure creates effects on its members. An artifact's properties create effects on its holder.

### D&D 5e Stacking Rules

All numerical modifiers stack **unless from the same named source**. Two instances of the same spell don't stack (take higher), but spell + magic item do stack. Simple by design.

**Concentration** as mutual exclusion: only one concentration spell at a time. Elegantly solves buff stacking without complex typed bonus rules.

### Pathfinder 2e Typed Bonus System

Three bonus types: Circumstance, Item, Status. **Same-type bonuses do NOT stack (take highest). Different-type bonuses always stack.** Penalties follow different rules: typed penalties also don't stack (take worst), but **untyped penalties always stack** (deliberate asymmetry -- penalties compound, bonuses don't inflate).

**Strong candidate for this game.** Define effect types mapping to relationship categories:
- Political type (from nation membership)
- Cultural type (from cultural affiliation)
- Factional type (from faction membership)
- Artifact type (from artifact possession)
- Environmental type (from settlement/location)

Same-type effects: take best bonus / worst penalty. Cross-type effects: always stack. Prevents triple-faction bonus abuse while allowing diverse sources to accumulate meaningfully.

### Effect Source Tracking

Universal pattern: every modifier carries metadata identifying its origin. Essential for:
1. **Display**: player sees "this +2 comes from Ironforge national infrastructure"
2. **Removal**: when source is removed, all its effects auto-clean
3. **Stacking resolution**: same-source effects identified and deduplicated

Maps directly to the "everything visible" design principle. Players must inspect any entity's effective attributes and see the full breakdown.

### Duration Models

- **Permanent until removed**: persists as long as source exists (relationship-derived effects in this game)
- **Round/tick-based**: lasts N ticks, decrementing (potential card-driven temporary effects)
- **Concentration/maintained**: persists only while actively maintained (potential card mechanic)
- **Triggered expiration**: ends when condition is met (goal tags: persist until achieved or cancelled)

For this game: relationship-derived attribute effects use permanent-until-removed (simplest, most appropriate for ongoing relationships). Goal tags use triggered expiration. Card-driven temporary effects (post-MVP low-altitude override cards) would use tick-based duration.

### The Effective Value Pattern

**Effective Value = Base Value + sum(filtered active effects)**

"Filtered" = stacking rules applied, mutual exclusion checked, expiration checked before summing. Base value is immutable during resolution. Effective value is what all downstream systems consume.

**Implementation**: store base attributes separately from computed effective attributes. Recompute effective attributes at start of each tick (or when dirty-flagged). Provide effective values to all behavioral evaluation.

### Cascading Effects and Computation Order

An effect modifying an attribute that is itself an input to another computation creates cascading dependencies. Solution (from Foundry VTT): define strict computation order:

1. Base attributes
2. Apply relationship-derived effects (using stacking/typing rules)
3. Compute effective attributes
4. Run semantic plane reducers (continuous positions -> categorical labels)
5. Evaluate card preconditions (using effective attributes and reduced labels)

Each stage uses outputs of the previous stage, never reaching backwards. Identical to the tick phase ordering already in the design.

---

## Synthesis: Recommended Architecture for This Game

### Decision Making
Use **Utility AI** with **weighted random selection** for MVP. Entity kind defines available impulses (low-altitude behaviors) and scoring curves. Entity's attribute sheet (base + effects) provides scoring inputs. Pressure system provides spatial inputs. Goal tags provide external priority inputs. Shared evaluation engine, kind-specific configuration. No behavior trees, no FSMs.

**GOAP as post-MVP extension.** The utility AI handles moment-to-moment impulse selection well but cannot plan multi-step sequences. GOAP can be layered on top: entities set high-level goals based on utility evaluation, then GOAP plans impulse sequences to achieve them. The base architecture must be extensible to support this.

**Critical distinction:** the utility AI evaluates "impulses" (low-altitude, always-available, small effects: move, consume, radiate influence). These are distinct from card-gated "Actions" (mid-altitude, large effects: declare war, forge alliance, invade territory). The entity behavior system handles impulses. The Action Executor (System #19) handles Actions.

### Effect System
Use **Pathfinder 2e-style typed bonuses** with **Foundry VTT transfer pattern**. Effects live on relationships and transfer to related entities. Effect types match relationship categories (political, cultural, factional, artifact, environmental). Same-type bonuses don't stack (take best). Cross-type bonuses stack. All effects carry source metadata for transparency and auto-cleanup. Effective values computed per tick in strict pipeline order.
