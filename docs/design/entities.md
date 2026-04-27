# Entities and Relationships

Entity kinds, attributes, verb roles, and the relationship taxonomy that
ties them together. The source of truth for the schema is C# code; this
file is the design narrative around it.

## Source of truth (code, not this doc)

- **Enums** (kinds, attributes, resources, relationships, verbs, cultures):
  `src/IceRemembers/IceRemembersSchema.cs`
- **Runtime kind shapes, lifecycles, descriptions:**
  `src/IceRemembers/IceRemembersDefinition.cs`
- **Semantic stocks, verbs, flow catalog (stock-flow authoring surface):**
  `src/IceRemembers/IceRemembersSemanticDefinition.cs`
- **Build-time invariants:** `src/Engine.Domain/Graph/DefinitionGraphValidator.cs`
- **Engine-level relationship category constraints:**
  `src/Engine.Domain/Kinds/RelationshipKindDefinition.cs` (engine
  relationships are defined at EntityCategory granularity, not kind)

## Kind summary

Each kind has three scalar attributes — Health + one outward (Capacity or
Projection) + one internal (Need or Drive) — plus a culture-neutral role on
the semantic-coord plane (autonomy × resonance × tradition):

| Kind | Health | Outward | Internal | Primary plane |
| --- | --- | --- | --- | --- |
| NPC | vitality | echo (C) | warmth (N) | physical hex |
| Artifact | condition | power (C) | resonance (D) | physical hex (carried) |
| Settlement | residents | dominion (P) | shelter (N) | semantic |
| Faction | cohesion | reach (P) | provisions (N) | semantic |
| Region | stability | holding (C) | pristiness (N) | semantic |

Region is the geographic area itself, not its inhabitants — its semantic
alignment is the animist spirit-of-place.

## Verb roles

Every kind declares exactly one mobility verb (enablement — gets into range
of targets) and exactly one drift verb (downtime — one-step random motion
on its primary plane). The remaining verbs are Actions split into four
categories:

- **Self-sacrifice** (H→X): `ArtifactChannel`, `FactionAgitate`,
  `PenguinEndure`, `RegionCleanse`, `SettlementOverreach`
- **Hex→Health inflow:** `PenguinWarm`, `ArtifactRepair`, `FactionOrganize`,
  `RegionRoot`, `SettlementGrow`
- **Untouched→Hex outflow:** `PenguinWitness`, `ArtifactVivify`,
  `FactionDistribute`, `RegionBreach`, `SettlementStripRookeries`
- **Cross-entity RPS ring** (10 verbs; the only verbs that target another
  entity — see below)
- **Mobility:** `PenguinTravel`, `ArtifactCompel`, `SettlementDeliberate`,
  `FactionPreach`, `RegionCommune`
- **Drift:** `PenguinWander`, `ArtifactCirculate`, `SettlementGossip`,
  `FactionQuarrel`, `RegionDream`

The full catalog with authored narrative notes lives in
`IceRemembersSemanticDefinition.cs`.

### The cross-entity RPS ring (10 verbs)

The only verbs that target another entity, and the load-bearing
narrative-mechanical structure of the simulation.

**+1 ring** (adjacent predator): each verb drains a neighbour's outward into
the actor's outward.

| Actor | Drains | Target's attribute → actor's attribute |
| --- | --- | --- |
| Settlement | NPC | Echo → Settlement Dominion (`SettlementClaimsEcho`) |
| Faction | Settlement | Shelter → Faction Reach (`FactionExtort`) |
| Region | Faction | Provisions → Region Holding (`RegionFortify`) |
| Artifact | Region | Pristiness → Artifact Power (`ArtifactCorrupt`) |
| NPC (Penguin) | Artifact | Power → NPC Echo (`PenguinWield`) |

**+2 ring** (skip predator): each verb drains a non-adjacent kind's
internal into the actor's internal.

| Actor | Drains | Target's attribute → actor's attribute |
| --- | --- | --- |
| Faction | NPC | Warmth → Faction Provisions (`FactionStarve`) |
| Region | Settlement | Dominion → Region Pristiness (`RegionReclaim`) |
| Artifact | Faction | Reach → Artifact Resonance (`ArtifactBind`) |
| NPC | Region | Holding → NPC Warmth (`PenguinForage`) |
| Settlement | Artifact | Resonance → Settlement Shelter (`SettlementSanctify`) |

Provider selection currently uses primary-plane locality (within 1 hex on
physical for NPC/Artifact, or semantic for Settlement/Faction/Region — the
semantic plane is a separate hex grid with Autonomy/Resonance/Tradition
axes), attribute thresholds (source ≥ floor, destination ≤ ceiling), and
target enumeration via `AttributeRef.Target(attr, kind)`.

## Hex resources

Three ambient hex resources — `food`, `wealth`, `population` — are
**possibility space**, not stock. Attribute→hex verbs expand hex values
(+1 per firing). Hex→Health verbs read hex values as rate multipliers
(2 × hex.value) and do not drain them. Resources bias and support actions;
they do not need to be consumed for balance, which is supplied by upkeep,
thresholds, saturation, drift, and competing claims.

## Relationships: graph topology, not effect carriers

**Relationships = graph topology.** They describe what is connected to
what. Actions and generators use them for selection filters
(`has_relationship`, `lacks_relationship`, `shares_related`,
`relationship_count`, `graph_path`), variable resolution (walk edges to
find related entities), wiring (produce edges as part of bundles), and
mutation (`create_relationship`, `archive_relationship`).

**Typed effects = attribute modifiers defined on entities** — a separate
system. An artifact may grant a stacking buff to its wielder; a region may
grant effects to anyone at its hexes. These effects live on the **source
entity** and flow to affected entities along topological connections (a
`Holds` edge, hex containment). Stacking follows the Pathfinder 2e rules
in CLAUDE.md Principle #10 — same-type take best, cross-type stack. The
relationship edge is the **path**, not the payload.

### Stripped as derivable (do NOT count in the catalog)

- `LocatedAt` — agent's current hex; derivable from position.
- `StoredAt` — artifact's current hex; derivable from position.
- `Borders` — region adjacency; derivable from hex grid.
- `Contains` — region→settlement geometric containment; derivable.
- `LoyalTo` — overlaps `MemberOf` + relationship strength.
- `InvolvedIn` — semantically null catch-all; deleted.

### Category-pair counts

Engine relationship granularity is `EntityCategory × EntityCategory`, not
`Kind × Kind`. Target is 3–4 per pair; three pairs are intentionally over
at 6.

| Pair | Count | Relationships |
| --- | --- | --- |
| A×A | 6 | AlliedWith, MemberOf, LedBy, RivalOf, AtWarWith, BetrayedBy |
| A×Sp | 6 | ControlledBy, HomeBase, Memorializes, ClaimedBy, CorruptedBy, CleansedBy |
| A×As | 6 | Holds, CreatedBy, Memorializes, ClaimedBy, CorruptedBy, CleansedBy |
| Sp×Sp | 3 | TradeRoute, FoundedFrom, Memorializes |
| Sp×As | 4 | NativeTo, Memorializes, Anchored, ConduitOf |
| As×As | 6 | PartOf, Memorializes, ConduitOf, WardAgainst, CorruptedBy, CleansedBy |

(A = Agency, Sp = Spatial, As = Asset.)

### Engine primitives the relationship catalog depends on

- **Generator selection filters** — pick targets by graph state:
  `graph_path`, `has_relationship`, `lacks_relationship`,
  `relationship_count`.
- **Generator variable resolution** — look up existing entities by
  traversing edges from the target.
- **Generator wiring** — create new edges as part of the produced bundle.
- **Generator saturation limits** — cap edges of a kind on an entity
  before the generator stops applying.
- **Action actor selection** — same filters/traversals, applied to find
  who can act.
- **Action targeting** — `has_relationship`, `lacks_relationship`,
  `shares_related` (both endpoints share an edge to a third entity),
  `graph_path` asserts.
- **Action outcomes** — `create_relationship`, `archive_relationship`,
  `archive_all_relationships`, `change_status` (cascades edge lifecycle).
- **Process preconditions** — `TargetHasRelationshipToSelf`,
  `SelfHasRelationshipToTarget`, `RelatedEntityMatchesTarget`. Available
  in code; not yet emitted by any verb.
- **Process mutations** — `ModifyOrCreateRelationshipMutation`. Available
  in code; not yet emitted by any verb.
- **Narration verbs** — edge formed/ended produces text via
  `verbs.formed/ended/inverseFormed/inverseEnded`.
- **Historical persistence** — non-cullable edges survive target ending.
- **Edge metadata** — exclusivity, symmetry, polarity, cullable,
  decayRate read by culling, narration, and filter logic.

### Relationship catalog

Each entry: narrative meaning, concrete simulation use, and process
integration (which verbs it gates, scores, or mutates on). Where current
verbs do not naturally use a relationship, we say so rather than invent.

| # | Relationship | Pairs | Narrative | Simulation use | Process integration |
| --- | --- | --- | --- | --- | --- |
| 1 | **AlliedWith** | A×A | Two peer agents in mutual alliance. | `lacks_relationship: allied_with` guards expansion targeting allies; `has_relationship: allied_with` enables joint-defense / muster-ally actions. Bidirectional `create_relationship` from alliance-forming actions; `archive` from betrayal. Generator variable: "pick an existing ally of $target." Saturation cap so one faction can't dominate the alliance graph. Symmetric, slow decay. | Exclusion gate on predation verbs (`SettlementClaimsEcho`, `FactionExtort`, `FactionStarve`, `ArtifactCorrupt`) — you don't drain allies. Implementable as `lacks_relationship` precondition or scoring penalty. Positive-scoring for `RegionFortify` (allies contributing provisions for shared defense). |
| 2 | **MemberOf** | A×A | Agent is formally part of another (NPC of faction). | Find members for faction-internal actions (promote, exile, dispatch). Faction-level actions use `graph_path` via incoming `member_of` to count or filter. Generator variable: "pick a faction $npc belongs to." Saturation cap → `faction_splinter` triggers when faction overflows. Member inherits culture from parent. Cullable, slow decay. | Primary gate for two RPS verbs: `FactionStarve` requires NPC `MemberOf` actor faction (you starve your own followers). `SettlementClaimsEcho` requires the target NPC to be `MemberOf` a faction `ControlledBy` the settlement (civic legitimacy claims its own names). Both are `TargetHasRelationshipToSelf` precondition patterns, possibly chained. |
| 3 | **LedBy** | A×A | Faction led by NPC, or NPC led by senior NPC. | **Exclusive.** Find leaders; find leader from led-entity via incoming `leader_of`. Succession actions archive old `LedBy` and create new (canonry's `leadership_succession` pattern). Actor condition "must be a leader" via `graph_path`. **Historical persistence**: edge ends when leader goes Historical, which itself triggers succession generators. | Thin for current verbs. Leaders are decision-layer (card / action) targets, not attribute-drain participants. A leader's Echo could feed `FactionAgitate` scoring via typed effects, but that's typed-effect, not process-targeting. |
| 4 | **RivalOf** | A×A | Peer hostility between agents. | Find rivals for hostile actions (raid, assassinate); `lacks_relationship: rival_of` guards diplomacy. Bidirectional create from raid/betrayal; archive on peace. Saturation caps prevent everyone-at-war. Cullable, medium decay (rivalries fade without reinforcement). | Positive-enable gate for hostile RPS: `ArtifactCorrupt` could prefer-score regions controlled by a rival faction; `FactionExtort` against a rival-controlled settlement. Inverse scoring for non-predatory verbs. Weaker gate than `AtWarWith`. |
| 5 | **ControlledBy** | A×Sp | Place politically controlled by agent. | **Exclusive** (one controller). Expansion targets `lacks_relationship: controls` with $actor; `seize_control` archives old + creates new. "Find previous owner" by incoming `controlled_by`. Saturation: "faction controls at most N places" gate. | Central gate for faction→place verbs. `FactionExtort` gates on settlement `ControlledBy` actor (extorting your own holdings is kickbacks; extorting rivals requires `AtWarWith`). `RegionFortify` gates on faction being controller of (or adjacent to) the region. `SettlementClaimsEcho` chains through `MemberOf → ControlledBy` to find citizen-NPCs. Indirect signal for `RegionReclaim` (abandoned settlements reclaim faster). |
| 6 | **HomeBase** | A×Sp | Agent's anchor — where they live / are rooted. | **Exclusive.** Find residents at a place via incoming `home_base`. Generator variable: "find the home of $npc" to place produced bundles nearby. Many penguin actions require an actor with a HomeBase (no homeless wanderers acting). Agent inherits culture from HomeBase settlement. | Penguin-targeting chain: `SettlementClaimsEcho` uses `HomeBase` to identify residents. `FactionStarve` could score more severely against penguins without `HomeBase` (vagrants are already marginal). `PenguinTravel` destination scoring could prefer the penguin's HomeBase (return-home drift). |
| 7 | **Holds** | A×As | Agent possesses artifact. | Find holders / held; theft, gift, tribute traverse. Outcomes archive + create. Saturation: cap artifacts per agent. Cullable. | **Critical precondition.** `PenguinWield` must require `SelfHasRelationshipToTarget: holds` — you cannot wield what you don't hold. `ArtifactBind` scopes to factions that `Hold` the artifact (chained `MemberOf → Holds`). `SettlementSanctify` requires the artifact `Holds`-by a resident (currently no edge for "settlement custody" — `Holds` would need to widen). |
| 8 | **CreatedBy** | A×As (As→A) | Artifact made by agent. | **Non-cullable** — creator can go Historical, edge persists for lineage. "Find artifacts created by historical factions" surfaces orphan-heritage items for restoration. Artifact inherits culture from creator. | Not used by current verbs. Creator lineage doesn't drive any live attribute-flow. Available for future authenticity-gated verbs (e.g. ritual coronation requiring crown-artifact `CreatedBy` to match). |
| 9 | **Memorializes** | A×Sp, A×As, Sp×Sp, Sp×As, As×As (Sp/As → Ag/Sp/As) | Source bears memory of target, often Historical. | **Non-cullable** — the whole point: target can be Historical and the edge still matters. "Find memorials to $historical_npc" drives tribute/inheritance actions. Generator variable: "pick a memorialized ancestor" to anchor a lineage-claim bundle. Narration: edge is readable history. | Not used by current verbs. Future "pilgrimage" or "veneration" verb (hypothetical) would target memorial sites. Generator/action territory only. |
| 10 | **ClaimedBy** | A×Sp, A×As (Sp/As → Ag) | Source claimed by an agent that doesn't currently control / possess it. | Find places `ClaimedBy` $actor that actor doesn't `Control` → enforcement actions; find places claimed by rivals → contested-territory actions; claim-vs-control mismatches feed war-trigger generators. Convert to `ControlledBy` on successful seize. | Not used by current verbs. `RegionReclaim` could prefer-score settlements on `ClaimedBy` land, but that's invention. |
| 11 | **TradeRoute** | Sp×Sp | Economic tie between two places. | **Symmetric**, cullable, medium decay. `establish_trade_route` action targets two settlements `lacks_relationship: trades_with`. Caravan actions traverse to find destinations. Saturation caps routes per settlement. | Not used by current verbs. `SettlementGrow` reads its own hex Population, not trade-partner hexes. Cross-settlement provisioning verb would walk `TradeRoute`, but none authored. |
| 12 | **FoundedFrom** | Sp×Sp | Daughter place founded from parent. | **Asymmetric**, **non-cullable** (lineage permanent). Parent can end, daughter retains edge. "Find daughters of an ended parent" → succession/abandonment/revival. Daughter inherits culture from parent at strength. | Not used by current verbs. Pure lineage topology with no live flow. |
| 13 | **NativeTo** | Sp×As | Artifact's place of origin; persists through relocation. | **Non-cullable** (origin permanent). Historical-capable. "Find artifacts NativeTo $region" drives return-home / restoration quests; "NativeTo a Historical region" surfaces diaspora relics. Artifact inherits culture from native place. | Not used by current verbs. Could enter as score modifier on `ArtifactVivify`/`ArtifactCorrupt` (effects stronger on `NativeTo` region), but that's typed-effect, not process-targeting. |
| 14 | **Anchored** | Sp×As | Artifact normatively bound to a place — should not leave. | **Exclusive.** Theft/removal filters OUT anchored artifacts (or requires "break anchor"); pilgrimage/desecration targets anchored artifacts at contested places. Consecration creates; sacrilege archives. | Exclusion gate on artifact mobility / drift verbs: `ArtifactCompel` and `ArtifactCirculate` should require `lacks_relationship: anchored` — an anchored artifact doesn't move. No role in conversion verbs (anchored artifacts can still drain in place). |
| 15 | **ConduitOf** | Sp×As, As×As (As → Sp/As) | Artifact channels source's projection outward. | "Find artifacts that are `ConduitOf` an ended faction" → orphan-conduit / re-consecration actions. Consecration creates; breaking archives, may cascade to downstream bundles. | Not used by current verbs. Would matter only if a projection-verb is added (e.g. `ArtifactVivify` affects a hex remote from the artifact via a ConduitOf chain). |
| 16 | **PartOf** | As×As | Artifact is component of composite. | **Asymmetric**, **non-cullable** (composition structural). "Find scattered parts of $composite" drives reunification quests. Generator variable: "find the whole that $part belongs to." Saturation caps parts per whole. | Not used by current verbs. Generator/action territory. |
| 17 | **WardAgainst** | As×As | Artifact ritually bound to oppose another. | **Asymmetric negative**, cullable. "Find wards against $target" for seek-and-destroy; "artifacts with no wards" for vulnerable-target identification. Consecration creates; ward's destruction archives with narrative weight. | Exclusion gate on hostile artifact conversions: `ArtifactCorrupt` blocked or penalised when a `WardAgainst` edge from a region-anchored ward points at the corrupting artifact. Similarly `ArtifactBind` if wards should suppress that channeling. |
| 18 | **CorruptedBy** | A×Sp, A×As, As×As (Sp/As → Ag/As) | Source actively taints target, persistently. | **Negative polarity**, non-exclusive, cullable, slow decay. `has_relationship: corrupted_by` gates access to sacred places; `lacks_relationship: corrupted_by` is a purity precondition for priest/ritual actions. Cleansing archives; corruption-spreading creates. **Historical persistence** — source can end, edge persists. | **Mutation outcome of `ArtifactCorrupt`**: the verb should ALSO emit `CorruptedBy` from drained region to corrupting artifact (via `ModifyOrCreateRelationshipMutation`). The edge is the lingering scar of the conversion, outlasting the flow itself. Exclusion gate: `SettlementSanctify` should require `lacks_relationship: corrupted_by` — can't sanctify what is corrupted (cleanse first). |
| 19 | **CleansedBy** | A×Sp, A×As, As×As (Sp/As → Ag/As) | Source actively restores target, persistently. | **Positive polarity**, non-exclusive, cullable, slow decay. `has_relationship: cleansed_by` gates holy-office / priest-level actions. Sanctuary/rite actions create; fall-from-grace archives. **Historical persistence** — sanctity persists as heritage. | **Mutation outcome of `SettlementSanctify`**: the verb should emit `CleansedBy` from settlement to artifact (settlement is cleansed-by that relic). Persists as sanctity heritage beyond the flow. `RegionCleanse` is self-sacrifice (same-entity flow) so a self-edge is odd — skip mutation there. |
| 20 | **AtWarWith** | A×A (faction↔faction in practice) | Formal active war — distinct from `RivalOf`'s background hostility. | **Symmetric**, **negative**, cullable, slow decay. `sue_for_peace` and `claim_victory` exclusively target factions the actor is at-war-with (canonry's `actions.json` pattern). `lacks_relationship: at_war_with` gates trade / diplomacy / alliance. Bidirectional create from declare-war; archive on peace/victory/conquest. "Factions AtWarWith for N ticks" triggers war-weary bundles (refugees, war-heroes, peace-brokers — canonry's `war_weary` tag). Distinct from `RivalOf`: rivalry is standing, war is activity; both can coexist. | Enable-gate for cross-faction hostile flows: `FactionExtort` against a settlement `ControlledBy` someone else reads as banditry normally but as war-levy if `AtWarWith` the controller. Precondition "target settlement's controller is `AtWarWith` actor OR is actor itself" cleanly distinguishes peacetime from wartime extortion. `FactionStarve` could similarly require `AtWarWith` when targeting NPCs of another faction. |
| 21 | **BetrayedBy** | A×A (faction→faction in practice) | A named, permanent record of betrayal — never forgotten. | **Asymmetric negative**, **non-cullable** (`decayRate: "none"` in canonry). `lacks_relationship: betrayed_by` gates alliance / trust-requiring actions. Betrayal-class actions create as a persistent outcome alongside other state (alliance breakage, war declaration). Specific absolution / atonement archives at high cost. "Faction with ≥2 outgoing BetrayedBy" → paranoia/isolation bundles. "Mutual BetrayedBy" → blood-feud dynasty bundles. **Historical persistence** — successor inherits the grudge. The only relationship in the catalog whose primary purpose is to carry memory of a single past event. | Exclusion gate on cooperative flows: `FactionDistribute` and `RegionFortify` should exclude or penalise flows toward/from factions the actor `BetrayedBy` — you don't charitably feed a betrayer's place, nor accept protection from one. Narrative-weight gate, can be scoring penalty. No role in hostile verbs (betrayal is `AtWarWith` / `RivalOf` territory). |

## Open design work on relationships

- **Decay rate policy per edge** — pick between canonry's categorical
  `slow / medium / none` and a numeric cull threshold; assign per edge.
- **Narration verbs per edge** — every edge needs formed / ended /
  inverseFormed / inverseEnded strings.
- **Cullable / non-cullable per edge** — lineage and memorial edges
  non-cullable; alliance / rivalry / trade cullable.
- **Process-schema extension** — semantic-verb schema needs fields for
  relationship preconditions and relationship-creating mutations so the
  cross-entity verbs above can emit what the catalog describes.
