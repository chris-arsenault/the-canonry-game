# Entity Kind Registry

The formalized, reviewed entity kind list. These are engine-level kinds. Subtypes within each kind are domain-defined.

---

## Spatial Kinds

Entities with a physical presence on the map.

### Region

A geographic area defined by natural boundaries. Regions are the spatial unit for pressure computation and bulk geographic queries.

- **Examples:** The Northern Highlands, The Shattered Coast, The Deep Interior
- **Subtypes (domain-defined):** tundra, coastal, mountain, plains, volcanic, etc.
- **Key attributes:** fertility, resource richness, habitability, climate
- **Key relationships:** borders (other regions)
- **Low-altitude behaviors:** mostly passive. Seasonal modifiers, terrain effects on movement and production. Regions don't "act" -- they are the stage.
- **Lifecycle:** regions do not age or decay. They are permanent map features.

### Point of Interest

A geographic landmark or natural feature. Not constructed -- it exists because the terrain produced it. Mountains, volcanoes, canyons, sacred groves, natural harbors, mineral deposits.

- **Examples:** Mt. Icefang, The Wailing Chasm, The Obsidian Vent
- **Subtypes (domain-defined):** peak, water_feature, resource_deposit, natural_wonder, hazard, etc.
- **Key attributes:** strategic significance, resource yield, mystical resonance, danger level
- **Key relationships:** located_in (region)
- **Low-altitude behaviors:** mostly passive. Some points of interest may have ongoing effects on nearby entities (a volcano increases danger, a sacred grove attracts certain NPC subtypes). Resource deposits may deplete over time.
- **Lifecycle:** points of interest do not age. They may be modified post-MVP (volcano erupts, resource depletes) but are permanent for MVP.

### Settlement

A constructed location. Built by entities, not by terrain. Cities, towns, villages, fortresses, outposts, ruins.

- **Examples:** Frosthollow, The Merchant's Rest, Fort Ironbeak
- **Subtypes (domain-defined):** village, town, city, fortress, outpost, ruin, port, etc.
- **Key attributes:** population, development, prosperity, defense
- **Key relationships:** located_in (region), controlled_by (nation), home_to (factions, NPCs)
- **Low-altitude behaviors:** population attraction/loss based on prosperity and safety. Development growth/decline from sustained conditions. Subtype transitions (village -> town -> city) based on attribute thresholds.
- **Lifecycle:** health metric is population. Aging function gradually pushes population down. Settlements reaching zero population transition to "ruin" subtype (historical but remains on map).

---

## Agency Kinds

Entities with decision-making capability. These evaluate impulses and (when action cards are active) take actions.

### Nation

A political entity controlling settlements and territory. The primary macro-level agent.

- **Examples:** The Sovereignty of Kesh, The Free Cities of the Shelf, The Orca-Scarred Compact
- **Subtypes (domain-defined):** monarchy, republic, confederation, tribal, theocratic, etc.
- **Key attributes:** stability, military strength, legitimacy, treasury. Semantic planes: government type (authoritarian...libertarian × secular...theocratic), economic model, ideology.
- **Key relationships:** controls (settlements), borders (nations), allied_with / rival_of (nations), led_by (NPC)
- **Low-altitude behaviors:** border tension with neighbors, internal resource flow between controlled settlements, population pressure driving expansion tendency, consolidation/fragmentation pressure, ideological drift.
- **Lifecycle:** health metric is loyalty/cohesion. Aging function increases internal strain. Nations that reach zero cohesion fragment or collapse to historical status.

### Faction

An organized group with goals that operates within and across nations.

- **Examples:** The Tide Watchers' Guild, The Herring Brotherhood, The Cult of the Deep Current
- **Subtypes (domain-defined):** trade_guild, military_order, religious_order, criminal_syndicate, political_movement, secret_society, etc.
- **Key attributes:** influence, resources, cohesion, secrecy. Semantic planes: ideology, methods (overt...covert).
- **Key relationships:** based_in (settlement), member_of (nation -- optional, factions can be trans-national), led_by (NPC), allied_with / rival_of (factions)
- **Low-altitude behaviors:** goal pursuit influencing member NPC behavior via goal tags and attribute effects. Influence accumulation from member actions. Internal politics creating pressure on host nation.
- **Lifecycle:** health metric is cohesion. Aging function increases organizational entropy. Factions reaching zero cohesion dissolve to historical status.

### NPC

A named individual with agency. The fine-grained actor in the simulation. Heroes are a subtype, not the only subtype.

- **Examples:** Captain Thornfin the Scarred, Elder Whisperstone, The Nameless Drifter
- **Subtypes (domain-defined):** hero, merchant, scholar, warrior, spy, elder, prophet, artisan, criminal, etc.
- **Key attributes:** health, warmth, echo, skill, age. Semantic planes: personality/temperament, ideology.
- **Key relationships:** loyal_to (nation/faction), located_at (settlement), mentor_of / rival_of (NPC), holds (asset)
- **Low-altitude behaviors:** need evaluation (warmth, safety, echo) driving movement and goal selection. Response to goal tags from nations and factions. Aging toward death.
- **Lifecycle:** health metric is HP. Aging function increases mortality risk and decreases proficiency. NPCs reaching zero HP die (transition to historical status).

### Monster

A non-civilized entity with agency. Threats, wildlife, supernatural creatures. Operates outside the nation/faction structure.

- **Examples:** Grandmother Orca, A Frost Wyrm, The Swarm of the Deep Shelf
- **Subtypes (domain-defined):** predator, beast, spirit, swarm, titan, etc.
- **Key attributes:** ferocity, territory size, warmth, power
- **Key relationships:** inhabits (region/point_of_interest), threatens (settlements/nations), hunted_by (NPCs)
- **Low-altitude behaviors:** territorial behavior, predation dynamics (threatening nearby settlements), migration patterns, breeding/spawning. Monsters create environmental pressure that nations and NPCs respond to.
- **Lifecycle:** health metric is HP or vitality. Aging depends on subtype (some are ageless, some are mortal). Monsters reaching zero HP are killed (historical status).

---

## Asset Kinds

Significant objects and vehicles that exist in the world, have holders, and influence the simulation through their presence.

### Artifact

A named, significant object. Weapons, armor, instruments, relics. Only named, notable artifacts are entities -- generic items are not tracked.

- **Examples:** The Frostbite Blade, The Choir of Bones (instrument), The Mantle of Deep Currents (armor), The Omega Star Fist (scroll granting a technique)
- **Subtypes (domain-defined):** weapon, armor, instrument, relic, scroll, device, etc.
- **Key attributes:** condition, mystical resonance, power, influence radius
- **Key relationships:** held_by (NPC/settlement/faction), created_by (NPC), created_at (settlement)
- **Low-altitude behaviors:** condition degradation over time. Mystical resonance accumulation from involvement in significant events. Influence on holder (attribute effects transferred via the relationship). Scrolls and devices grant abilities to their holders.
- **Lifecycle:** health metric is condition. Aging function increases degradation. Artifacts reaching zero condition are destroyed (historical status, but may leave a "legacy" tag on former holders).

### Transportation

A significant named vehicle. Ships, war machines, legendary mounts. Provides mobility, capacity, and strategic capability.

- **Examples:** The Icebreaker Resolute, The War-Sled of the Northern Clans, The Phantom Skiff
- **Subtypes (domain-defined):** personal (mount, small craft), group (ship, caravan), military (war machine, siege engine), etc.
- **Key attributes:** condition, speed, capacity, combat power
- **Key relationships:** owned_by (nation/faction/NPC), located_at (settlement/region), crew (NPCs)
- **Low-altitude behaviors:** condition degradation over time. Movement along routes (if crewed). Strategic value affecting owner's military and economic capabilities.
- **Lifecycle:** health metric is condition. Aging function increases degradation. Transportation reaching zero condition is destroyed/wrecked (historical status, wreck may persist as a point of interest).

### Ability

A singular technique, power, or knowledge that can be possessed and used. Magic, combat techniques, crafting secrets, diplomatic arts. Like artifacts, abilities are named and unique -- generic skills are not tracked. Unlike artifacts, abilities are intangible: they cannot be physically destroyed, but they can be lost, forgotten, or stolen.

- **Examples:** The Omega Star Fist (combat technique), The Song of Deep Currents (ice magic), The Merchant's Third Eye (trade intuition), The Orca-Ward Seal (protective ward)
- **Subtypes (domain-defined):** combat_technique, magic, craft_secret, diplomatic_art, ward, ritual, etc.
- **Key attributes:** power, mastery (how well the holder can use it), stability (risk of being lost/forgotten), mystical resonance
- **Key relationships:** held_by (NPC -- primary), created_by (NPC), derived_from (another ability or artifact)
- **Low-altitude behaviors:** mastery changes over time (improves with use, degrades with neglect). Stability affected by holder's attributes (a weakening holder may lose grip on a powerful technique). Influence on holder through effect transfer (the Omega Star Fist grants combat bonuses to its holder).
- **Lifecycle:** health metric is stability. Aging function represents the difficulty of maintaining mastery. Abilities reaching zero stability are "lost" or "forgotten" (historical status -- they existed, the holder once knew them, but they are no longer active). Can potentially be rediscovered or taught to a new holder through cards.

**Relationship to artifacts:** Abilities follow the same ownership/effect-transfer model as artifacts and transportation. The distinction: artifacts are physical objects (can be in a location without a holder, can be picked up), abilities are intangible (always bound to a holder, transfer requires teaching/stealing, not physical movement). The three asset kinds share mechanical patterns but have different spatial and transfer rules.

---

## Record Kinds

Entities that represent happenings rather than things.

### Occurrence

A long-running situation or event. Wars, crises, plagues, golden ages, trade booms. NOT individual events (those are brief log entries). An occurrence has duration, participants, and a lifecycle.

- **Examples:** The War of the Shattered Shelf, The Great Famine of Year 47, The Golden Age of Frosthollow
- **Subtypes (domain-defined):** war, crisis, plague, golden_age, migration, trade_boom, religious_movement, etc.
- **Key attributes:** severity, scope (how many entities involved), duration
- **Key relationships:** involves (nations, factions, NPCs, settlements), caused_by (entity or card), located_in (regions)
- **Low-altitude behaviors:** mostly passive. Occurrences are a formal way to tie related entities and events together for graph queries and card preconditions. A "War" occurrence binds the participants, and cards can check "is there an active war involving Nation X?"
- **Lifecycle:** health metric follows the aging function -- occurrences naturally wind down over time unless sustained by ongoing conditions. A war that no one is fighting gradually transitions to historical status. An occurrence at zero health is "over."

---

## Not Entity Kinds

### Culture

Culture is an ATTRIBUTE, not an entity kind. See `docs/systems/culture.md`. Culture is a static, domain-defined classification that determines naming, attribute tendencies, and identity. Entities HAVE a culture; culture does not exist as a standalone entity.

### Era

Eras were a construct for forcing world history phases in a single-shot simulation (the canonry model). They do not have the same role in this game's continuous, player-driven simulation. Eras are not an entity kind. If temporal structure is needed later, it can be added as a tick-clock concept or a domain-specific mechanic.

---

## Summary

| Category | Kind | Low-Altitude Behaviors | Has Agency | Lifecycle | MVP |
|----------|------|----------------------|------------|-----------|-----|
| Spatial | Region | Passive (stage) | No | Permanent | **MVP** |
| Spatial | Point of Interest | Passive (local effects) | No | Permanent | Post-MVP |
| Spatial | Settlement | Growth/decline, subtype transitions | No | Population-based | **MVP** |
| Agency | Nation | Border tension, resource flow, expansion, fragmentation | Yes | Cohesion-based | **MVP** |
| Agency | Faction | Goal pursuit, influence, internal politics | Yes | Cohesion-based | Post-MVP |
| Agency | NPC | Needs, movement, aging, goal response | Yes | HP-based | **MVP** |
| Agency | Monster | Territory, predation, migration | Yes | Vitality-based | Post-MVP |
| Asset | Artifact | Degradation, resonance, holder effects | No | Condition-based | **MVP** |
| Asset | Transportation | Degradation, movement, strategic value | No | Condition-based | Post-MVP |
| Asset | Ability | Mastery drift, stability, holder effects | No | Stability-based | Post-MVP |
| Record | Occurrence | Passive (binding construct), natural wind-down | No | Duration-based | Post-MVP |

**11 entity kinds total. 5 MVP kinds (Region, Settlement, Nation, NPC, Artifact). 6 post-MVP kinds.**

MVP kinds are the structural minimum needed for the core simulation loop: geographic areas for pressure scoping (regions), places to be (settlements), political entities (nations), individual actors (NPCs), and significant objects (artifacts). The remaining 6 kinds are well-defined and ready to implement but are not needed for the first playable.

---

## Category-Level Relationship Taxonomy

Relationships are defined primarily at the CATEGORY level (Spatial, Agency, Asset, Record), not as an 11x11 cross-product of all kinds. This dramatically simplifies the relationship model: a 4x4 matrix with category-specific relationship kinds, not a combinatorial explosion.

### Spatial × Spatial
- **borders:** region-to-region adjacency
- **contains:** region-to-point_of_interest, region-to-settlement (spatial containment)

### Spatial × Agency
- **controlled_by:** settlement/region controlled by a nation
- **located_at:** agency entity's current position at a spatial entity
- **home_base:** faction or nation's primary spatial anchor
- **inhabits:** monster's territorial claim on a region/point_of_interest

### Spatial × Asset
- **stored_at:** asset located at a spatial entity (an artifact in a settlement, a ship in a port) when not held by an agency entity

### Spatial × Record
- **located_in:** occurrence's geographic scope (this war is happening in these regions)

### Agency × Agency
- **allied_with:** positive diplomatic/organizational relationship (symmetric)
- **rival_of:** negative diplomatic/organizational relationship (may be asymmetric)
- **led_by:** faction/nation led by an NPC (exclusive -- one leader)
- **member_of:** NPC belongs to faction/nation (non-exclusive -- can belong to multiple)
- **threatens:** monster threatens a nation/settlement
- **hunted_by:** monster is actively targeted by an NPC/faction

### Agency × Asset
- **holds:** agency entity possesses an asset (NPC holds artifact, NPC knows ability, nation owns transportation). The primary ownership relationship. Transfers effects from asset to holder.
- **created_by:** asset was created by an agency entity

### Agency × Record
- **involved_in:** agency entity is a participant in an occurrence (this nation is involved in this war)
- **caused_by:** agency entity initiated this occurrence

### Asset × Asset
- **derived_from:** one asset was created from or inspired by another (an ability derived from a scroll, a weapon reforged from fragments)

### Asset × Record
- **involved_in:** asset plays a role in an occurrence (this artifact was contested in this war)

### Record × Record
- **escalated_from:** one occurrence led to another (a trade dispute escalated into a war)

### What this enables
- Card preconditions and targeting use CATEGORY-level relationship queries: "find any Agency entity that holds an Asset with power > 50" works without knowing whether the holder is an NPC or a faction.
- Domain-defined subtypes can add kind-specific relationship variants within these category patterns, but the structural taxonomy is set at the category level.
- New entity kinds added to an existing category automatically participate in that category's relationship types.

### Relationship properties (inherited from System 7 design)
- Strength: 0-1, polarity defined by relationship kind
- Exclusivity: per relationship kind (led_by is exclusive, member_of is not)
- Symmetry: per relationship kind (allied_with is symmetric, controlled_by is not)
- History: culled relationships retain start/end dates for historical queries
