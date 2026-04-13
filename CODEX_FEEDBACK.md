# Codex Design Feedback

This report is based only on `docs/` and `docs/systems/`. I did not read `docs/research/`.

## Executive Assessment

There is a real game here, and it is distinct.

At its best, this project is a contemplative world-simulation roguelite where the player does not command actors directly, but instead defines the world's laws of emergence through a rotating card constitution. That is strong. "The player authors nature, not events" is a good thesis, and the split between low-altitude simulation and mid-altitude card meaning is the clearest and most differentiated part of the design.

The current problem is not lack of vision. The problem is that the design is trying to ship too much architecture before it has locked the minimum player loop, and several of the documents disagree on foundational rules. Right now the implementation complexity is very high, while the player decision model is still partially unresolved.

My conclusion:

- There is enough here to begin a focused prototype.
- There is not enough resolved here to begin full production with confidence.
- The biggest risk is not "too simple." It is "too architecturally ambitious while still under-decided at the player level."

## What Game I Believe You Are Building

The game is a run-based god-perspective simulation where:

- The world has always-on low-altitude physics: geography, relationships, growth, decay, migration pressure, faction pressure, cultural pressure, aging.
- The player assembles and cycles a deck of cards representing world laws.
- Cards come in three classes:
- Generators define what new things can emerge.
- Systems define how accumulated pressure resolves.
- Actions define what actors are allowed to do.
- The player does not tell a nation to attack, settle, or ally. The player makes those concepts available to the world.
- The tableau is the current constitutional layer over the simulation.
- Mana is a complexity budget more than a power budget.
- The intended pacing is long observation, interpretation, then deliberate reconfiguration.
- The intended failure structure is a three-way tension between extinction, stagnation, and monoculture.

That core identity is good. It is coherent enough to defend and market.

## Player Agency Model

The intended player agency model is:

- Pre-run agency: choose an archetype, therefore choose an initial thesis about how this world should function.
- Mid-run structural agency: choose what cards are in the deck, how the deck evolves, and what the tableau currently allows.
- Timing agency: choose when to expose the simulation to a generator/system/action by playing or removing cards.
- Interpretive agency: observe outcomes, infer causality, and revise the theory.

This is not tactical command agency. It is constitutional agency.

That said, the current mechanics partially undermine that thesis:

- `docs/design-principles.md` frames the player as contemplative and deliberate.
- `docs/systems/13-tableau-mana.md` allows instant activation and completely free swapping at any time.
- The persistent hand in `docs/systems/12-deck.md` means the player can hold answers indefinitely.

In practice, that risks turning the player into a reactive operator who hot-swaps world laws to answer emerging pressures. That is much closer to crisis management than the docs claim.

So the real agency question is:

- Is the player a constitutional designer making infrequent, weighty changes?
- Or a live operator reconfiguring the world's rules whenever needed?

The docs currently claim the first and mechanically enable the second.

## What Is Strong

- The central thesis is clear and unusual.
- The low-altitude vs mid-altitude split is an excellent framing tool for both design and implementation.
- The three card classes are legible and conceptually strong.
- "What the player omits matters" is fertile design space.
- The three-way loss model is a strong anti-solved-game idea.
- The world-state transparency principle is correct for a simulation game like this.
- The map-as-observation-tool goal is correct. This game will live or die on readable state.
- The deck as "a theory under test" is a very good roguelite framing.
- The domain system has promise, especially if the engine really stays agnostic.

## Critical Inconsistencies and Design Holes

These are the issues I would resolve before calling the design production-ready.

### 1. MVP map generation is directly contradictory

The docs disagree about whether procedural generation is MVP.

- `docs/design-constraints.md:111-113` says no procedural map generation initially.
- `docs/game-systems.md:79` says procedural map generation is MVP.
- `docs/systems/04-map-spatial.md:67-75` says procedural generation is MVP and central to map quality.

Why it matters:

- This is not cosmetic scope. It changes production order, tooling, content pipeline, debugging, save/load complexity, and playtest reproducibility.

Action:

- Decide one of these now:
- Fixed hand-authored test map for prototype and first playable.
- Procedural generation as part of MVP.

Recommendation:

- Use a fixed authored map or a very small fixed-seed generator for the first playable. Full procgen is not where this game's novelty lives.

Question:

- What does procgen prove for the first playable that a strong fixed map does not?

### 2. The engine/domain boundary is not actually settled

The docs disagree on what is hard-coded schema and what belongs to a narrative domain package.

- `docs/design-constraints.md:19-23` says entity kinds, subtypes, relationship kinds, and status values are hard-coded schema.
- `docs/systems/19-narrative-domain.md:33-35` says the domain defines subtypes, attribute definitions, and relationship kind definitions.
- `docs/systems/19-narrative-domain.md:47-60` lists subtype definitions, attribute definitions, and relationship kind definitions as domain-package contents.

Why it matters:

- This is a foundational architecture decision.
- If unresolved, every content file and every system boundary will drift.
- It also determines whether making a second domain is cheap, expensive, or nearly impossible.

Action:

- Write one short canonical "schema ownership" doc and make every system conform to it.

Recommendation:

- Hard-code entity kinds and core relationship semantics in engine.
- Let domains define subtypes, card pools, naming, pressures, archetypes, and parameterized behavior profiles.
- Be cautious about letting domains define entirely new relationship kinds if the engine logic depends on relationship semantics.

Question:

- When a second domain ships, should it be able to invent a brand-new relationship ontology, or only skin and parameterize an existing one?

### 3. Relationship semantics are inconsistent

There are multiple contradictions in the relationship model.

- `docs/game-systems.md:212` says relationship values are continuous `-1 to +1 or similar`.
- `docs/systems/07-relationships.md:46-59` says strength is always `0 to 1` and polarity is encoded by relationship kind.
- `docs/game-systems.md:217` says relationships do not create or destroy links.
- `docs/systems/07-relationships.md:65-79` says low-strength relationships are automatically culled, which is topology change.

Why it matters:

- Relationships are one of the central bridges between simulation and cards.
- You need one mental model for designers, players, and implementation.

Action:

- Standardize on one relationship model.

Recommendation:

- Use `0..1` strength plus relationship-kind polarity.
- Keep disposition separate from relationship strength.
- Define culling as lifecycle on a relationship record, not "destroying" history.
- Update all docs to match this.

Question:

- Is culling considered low-altitude physics, or a graph maintenance rule outside the player-controlled topology doctrine?

### 4. Disposition is described as derived, then treated as mutable state

- `docs/systems/07-relationships.md:40-44` frames disposition as derived from semantic distance and other factors.
- `docs/systems/18-action-executor.md:151-153` uses `modify_disposition(...)` as if disposition were stored mutable state.

Why it matters:

- This is a data-model contradiction.
- It affects AI evaluation, card DSL, save/load, debugging, and player visibility.

Action:

- Pick one:
- Disposition is fully derived and never directly modified.
- Disposition is stored state with derived contributors.

Recommendation:

- Keep disposition derived.
- If you need "failed diplomacy leaves a scar," store a historical modifier or relationship-memory tag that feeds future disposition calculations.

Question:

- Do you want attitudes to be a remembered fact or a computed consequence?

### 5. Resources are both post-MVP and functionally required by MVP

The resource/economy story is currently unstable.

- `docs/game-systems.md:222-226` says resources/economy are tabled entirely until post-MVP and approximated through attributes.
- `docs/game-systems.md:194-196` still makes nation resource flow and settlement production/consumption MVP behaviors.
- `docs/systems/06-entity-behaviors.md:111-129` still treats internal resource flow and settlement production/consumption as core low-altitude behavior.
- `docs/systems/08-resources-economy-POST-MVP.md:42-64` describes an MVP flow model despite the file being marked post-MVP.

Why it matters:

- The economy seems to be doing a lot of causal work in the simulation.
- If it is abstracted, you need to specify the abstraction.
- If it is real, it is not actually post-MVP.

Action:

- Define the MVP economic abstraction explicitly.

Recommendation:

- For MVP, do not implement real multi-resource flow.
- Replace it with 2-3 abstract variables at settlement/region level, such as `sustenance`, `capacity`, and `prosperity`.
- Let those feed demographic pressure and national strain.
- Defer actual per-resource logistics and trade routes.

Question:

- What minimum economic model is necessary for the player to understand why a settlement grows, starves, migrates, or fragments?

### 6. The strategic core is supposedly the three lose conditions, but MVP removes them

- `docs/design-principles.md:85-96` frames extinction, stagnation, and monoculture as a core design pillar.
- `docs/game-systems.md:317-321` and `docs/systems/14-lose-condition-monitor-POST-MVP.md:1-27` defer the lose condition monitor entirely to post-MVP.

Why it matters:

- If this three-way tension is the strategic spine, removing it from MVP changes the actual game.
- Without stakes, the tableau may feel like a toy rather than a survival theory under pressure.

Action:

- Even if hard fail thresholds are deferred, introduce a soft-failure version in the first playable.

Recommendation:

- Use visible danger bands before full lose conditions are tuned.
- Example: extinction risk high, stagnation risk high, monoculture risk high.
- Let the player feel they are balancing threats even before hard defeat triggers exist.

Question:

- What is the player trying not to become in the first playable if loss does not exist yet?

### 7. Culture is still unresolved as a data model, but many systems depend on it

- `docs/systems/02-entity-kinds.md:39-45` says the kind list is not finalized and explicitly questions whether culture is a kind or an attribute.
- Many other docs assume culture is a first-class entity with behaviors, pressure, relationships, targeting, and overlays.

Why it matters:

- This is not a cosmetic uncertainty.
- It changes map rendering, pressure computation, behavior models, relationship topology, deck content, and domain authoring.

Action:

- Resolve culture before deeper production.

Recommendation:

- For this game, culture should remain a first-class entity or first-class layer, because it is doing too much systems work to be just a decorative attribute.
- If full entity treatment is too heavy, use a hybrid: culture as a region/settlement layer with identity, influence, cohesion, and history, even if not fully graph-entity-equivalent.

Question:

- Do you want culture to be something that acts, or something that is merely distributed?

### 8. Prominence is both present and absent

- `docs/game-systems.md:52` and `docs/systems/03-attributes.md:63` treat prominence as a universal scalar.
- `docs/systems/09-lifecycle.md:53-57` says prominence is post-MVP and absent from MVP.

Why it matters:

- This one is more doc hygiene than existential design risk, but it signals drift in the model.

Action:

- Mark prominence consistently as post-MVP or remove it from MVP examples.

### 9. The backlog reintroduces a rejected storyteller concept

- `docs/design-constraints.md` firmly rejects hidden storyteller/director logic.
- `docs/post-mvp-backlog.md:77-79` mentions "storyteller refocus toward favored nation narrative arc."

Why it matters:

- This is exactly the kind of idea that can quietly erode the game's strongest differentiator.

Action:

- Remove or restate that backlog item unless you truly want to revisit the anti-storyteller pillar.

Question:

- In patron mode, do you want more camera focus and metrics around the favored entity, or an actual narrative curator?

## Complexity Audit

The design is too complex for a true MVP if you try to carry all of this at once.

The cumulative burden is extremely high:

- Historical entity graph with queryable dead states.
- Typed relationships with history, culling, and effect transfer.
- Spatial grid with regions and overlays.
- Per-kind low-altitude simulation.
- Utility AI across multiple entity kinds.
- Semantic planes and reducers.
- Pressure as a smoothing bridge layer.
- Deck, hand, tableau, mana, timed draws, milestone draws.
- Per-card achievement tracking and deck modification.
- Declarative card DSL with mutable per-instance cards.
- Save/load from day one.
- Domain package architecture.

Any one of these is manageable. The stack as written is large.

My concern is not that the design is intellectually incoherent. It is that the team can spend a very long time building infrastructure before learning whether the core act of watching, inferring, and reconfiguring is actually fun.

## Simplicity Audit

Player-facingly, the game may actually be too simple in the wrong places.

Specifically:

- The player mostly watches.
- The player occasionally swaps cards.
- The player does not choose targets or actions directly.
- If swapping is free and immediate, the best play may become obvious reactive maintenance.
- If swapping is restricted later, the game risks long stretches of passive observation unless the map is unusually legible and instructive.

So the design currently risks both extremes:

- Too much implementation complexity.
- Too little meaningful player decision frequency.

This usually means the missing piece is not "more systems." It is a sharper cadence of commitment.

## Where the Agency Loop Needs Sharpening

I would define the player loop more aggressively.

Right now it appears to be:

1. Observe world state.
2. Draw cards over time.
3. Configure tableau.
4. Watch the world evolve.
5. Modify deck through milestones.

That is fine as a skeleton, but it needs one stronger commitment rule.

Options:

- Era-based reconfiguration: tableau can only be materially reconfigured at era breaks or after a cooldown window.
- Activation inertia: cards activate immediately but do not fully contribute until they have persisted for some time.
- Removal consequence: removing a card creates temporary instability, discouraging constant toggling.
- Limited interventions: one direct divine act per era or per threat spike, so the player has a relief valve without becoming a firefighter.

Recommendation:

- If the thesis is contemplative authorship, do not allow unlimited free hot-swapping in the long term.
- Preserve some reactivity, but make the dominant skill forecasting rather than patching.

## Is the Game Too Simple or Too Complex?

My answer is:

- Architecturally: too complex for MVP.
- Systemically: close to the right level.
- Player-agency-wise: still under-defined.

The main game does not need more kinds, more pressures, or more card modification layers right now.

It needs:

- A smaller first slice.
- A locked player commitment cadence.
- A clear soft-threat model.
- A resolved culture/economy relationship.
- One canonical schema boundary.

## Is There Enough Here To Begin Production?

There is enough to begin prototype production.

There is not enough to begin broad production with confidence.

I would not recommend full content production, broad engine generalization, or deep domain packaging work until the following are locked:

- Culture model.
- Schema ownership boundary.
- MVP economy abstraction.
- Swap/commitment rules.
- Threat model for the first playable.
- Map generation scope.
- Exact first playable kind set.

## Recommended First Playable Slice

If I were narrowing this toward a real first playable, I would cut to:

- One fixed map or one fixed-seed map family.
- Four active kinds only: settlement, nation, culture, faction.
- Heroes and artifacts either cut or heavily stubbed.
- No full resource system.
- Two or three abstract economic attributes.
- Two pressures only at first: demographic pressure and cultural/military tension.
- Nine to twelve cards total across all archetypes.
- Three archetypes maximum.
- Soft failure tracks for extinction, stagnation, monoculture before hard lose conditions.
- Tableau reconfiguration with friction.
- No Balatro-style per-card modification layers until the base loop is proven.

That slice would answer the real question:

- Is it fun to study the world, infer why things are happening, and then change its laws?

## Specific Actionable Recommendations

### Design-level

- Write a one-page "core loop" doc that states exactly what the player does in a quiet minute and in a crisis minute.
- Write a one-page "agency doctrine" doc that decides whether this is constitutional play or reactive live operations.
- Write a one-page "schema ownership" doc that resolves engine vs domain responsibilities.

### System-level

- Replace real MVP economy language with explicit abstract variables.
- Resolve culture as a first-class layer.
- Standardize relationship semantics and disposition semantics.
- Add a soft-threat monitor to MVP even if hard failure is deferred.
- Remove or restate backlog items that contradict core pillars.

### Scope-level

- Cut procgen from the first playable unless it directly tests a core hypothesis.
- Cut per-card modification layers from MVP.
- Cut full hero simulation unless it materially improves the main loop.
- Treat save/load as desirable but not sacred if it slows learning on the core loop.

## Questions I Would Want Answered Before Full Production

1. What exact player decision are you expecting every 2-5 minutes of play?
2. What is the penalty for being wrong about the world's needs?
3. If the player can swap freely, why is forecasting more valuable than reaction?
4. If the player cannot swap freely, what keeps observation from becoming idle waiting?
5. Is culture an actor, a layer, or an attribute?
6. What is the smallest economy model that still creates migration, scarcity, and political strain?
7. Are relationships stored facts, derived feelings, or both?
8. Which parts of schema are truly engine-level and which are domain content?
9. Does the first playable need procgen to validate the design thesis?
10. What is the minimum set of visible metrics that teaches the player causality?
11. What causes stagnation in practice in the first playable?
12. What causes monoculture in practice in the first playable?
13. What does a bad tableau look like, and how quickly can the player recognize that it is bad?
14. What does a good tableau feel like besides "it prevents loss"?
15. What are you willing to cut if the contemplative loop is fun but the full architecture is not yet ready?

## Final Recommendation

Do not add more conceptual systems until the current design is made stricter.

The strongest version of this game is not "the biggest simulation." It is "the clearest simulation with the most legible authorship." Your best next move is to reduce ambiguity, reduce MVP scope, and force the player-agency model to become mechanically honest.

If you do that, there is a compelling game here.
