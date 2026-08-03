# Knowledge and Technology

Knowledge is a culture-scoped authored modifier layer over existing simulation
mechanics. Technologies are named semantic-map sites. A culture receives a
site's grants only while its semantic culture field claims that site.

## Ownership and data flow

`Engine.Gameplay` owns `KnowledgeGrantConfig` and the reusable grant-domain
vocabulary. `IceRemembersKnowledge` owns the 27 Ice Remembers technologies and
their grant bundles. Startup seed data carries stable target names. During
kernel configuration, `Engine.Host` resolves those names through
`DomainIdRegistry` and installs primitive site and grant rows.

`Engine.Kernel` owns claim detection and derived modifiers. Its knowledge
runtime uses fixed-capacity arrays for sites, grants, claim bits, and a
`PrimitiveFloatTable` keyed by culture plus packed domain/target. It contains no
technology names or gameplay prose.

Configuration is reinstalled on fresh and snapshot-loaded kernels. Claims and
modifiers are not serialized because they are derived from the serialized
culture field and are rebuilt on the next scheduled knowledge run.

## Claim and combination rules

- A site is claimed when claimed-band semantic raw mass exists within radius 2.
- Claims are evaluated independently for every culture and site.
- Tier is telemetry and placement metadata, never an effect multiplier.
- Held grants multiply. Losing a claim removes its multipliers on recomputation.
- A domain-wide target (`0`) and a specific target both apply to a specific
  query; their values multiply.
- All modifiers default to `1`.

## Consumer semantics

- Culture production and persistence are per plane.
- Verb, gambit, movement, road, and minor-actor grants use the acting culture.
- Generator fire selection averages the eligible target cultures' modifiers;
  target selection applies each candidate target culture's modifier.
- Relationship maintenance takes the stronger qualifying endpoint modifier.
  Positive maintenance is multiplied; negative passive drift is divided. An
  edge never receives both endpoint modifiers.
- Carrying capacity applies for the entity's culture and plane before crowding
  load and comfort are calculated.
- Attraction multiplies the source entity's final non-negative attractor mass.
- Movement grants apply only to physical travel and matching entity kinds.
- Road-work grants apply to culture-owned minor-actor road work and road growth
  caused by travel.
- Decay-tide resistance affects only excess tide above `1`:
  `effective = 1 + ((tide - 1) * resistance)`. Ordinary tide is unchanged.
- Thaw threshold is the base frontier threshold times the pressing culture's
  modifier.
- Refreeze compares the strongest culture at a cell with the base reclaim
  ceiling times that culture's modifier. It does not assume culture ID 1.

## Ice Remembers catalog

| Technology | Explicit grants while claimed |
|---|---|
| Record-Carving | semantic decay ×0.94; `memorializes` maintenance ×1.25 |
| Breath-Holding Dives | `penguin_go_fishing`, `artifact_sink_seaward` weight ×1.25 |
| Bone Tools | all minor-actor work ×1.12 |
| Ice Fishing | physical capacity ×1.10; `penguin_go_fishing` ×1.20 |
| Call-Signals | `allied_with` maintenance ×1.25; pod-call and envoy verbs ×1.20 |
| Windbreaks | physical culture decay ×0.92; hearth-banking ×1.25 |
| Rookery Schools | artifact-knowledge and sanctuary-colony generators ×1.25 |
| Deep Netting | physical capacity ×1.14; fishing ×1.15 |
| Ice-Block Building | physical decay ×0.86; hearth-banking ×1.30 |
| Improved Ice Fishing | physical capacity ×1.18; fishing ×1.25 |
| Echo-Hunting | orca hunt ×1.35; hunting-ground gambit score ×1.20 and success ×1.12 |
| Aurora Light Spectacles | physical attraction ×1.25; lamplight ×1.30 |
| Lineage Charts | `member_of`, `led_by`, `taught_by` maintenance ×1.30; continuity verbs ×1.20 |
| Trench Charting | three depth/conduit generators ×1.25; conduit charting ×1.25 |
| Sled Building | penguin/faction physical travel cost ×0.90; road work ×1.25 |
| Krill Traps | physical culture production ×1.12; bounty pulse ×1.20 |
| Long-Distance Calls | semantic production ×1.12; gossip/news/deed-name verbs ×1.25 |
| Thaw Forecasting | physical thaw threshold ×0.82 |
| Stone Libraries | semantic decay ×0.72; memorial continuity ×1.35 |
| Pressure Diving | `consigned_to`, `anchored`, `conduit_of` maintenance ×1.30; custody/conduit verbs ×1.20 |
| Ice-Ship Hulls | transport travel cost ×0.85; sailing/port/trade ×1.25; harbor generator ×1.20 |
| Pack Hunting | three hunt/slaying gambits: score ×1.25, success ×1.15 |
| Pod Coordination | campaign labor ×1.25; pod/column verbs ×1.25; banner gambit score ×1.20 and success ×1.12 |
| Ice Cellars | physical capacity ×1.22; excess physical decay tide ×0.70 |
| Abyssal Mapping | deep-current/offering/leviathan generators ×1.30; deep-current gambits score ×1.25 and success ×1.15 |
| Reinforced Hulls | transport travel cost ×0.75; sea/trade verbs ×1.30; transport/sea gambits score ×1.25 and success ×1.15 |
| Storm Prediction | physical refreeze ceiling ×0.50; excess physical decay tide ×0.65 |

The source catalog remains authoritative for the exact targeted verb, generator,
and gambit sets.

## Player-facing overview

The bottom bar's **Technology (K)** action opens a journal-style culture ledger.
Its left pane lists every culture and its current claim count. Selecting a
culture shows the named technologies its semantic field currently controls,
their authored grant descriptions, and the effective combined multipliers for
every affected target.

The panel does not recompute claims or combine grants. `Engine.Host` reads the
kernel's current claim bits and derived modifier table into a revisioned
`KnowledgeOverviewSnapshot`; `TheCanonryGame.Game` joins stable technology and
target IDs to the Ice Remembers catalog and projection symbols; the Godot panel
only formats that read model. Lower multipliers are shown directly for mechanics
where reduction is beneficial, such as decay, travel cost, and frontier
thresholds.

## Telemetry

The knowledge telemetry lanes (`knowledge_snapshots`,
`knowledge_claim_snapshots`, `knowledge_site_symbols`) were deleted
2026-08-03: no tool read them
(`docs/analysis/low-value-systems-survey-20260803.md`). The kernel claim
state and the Technology (K) overview remain; any future measurement of
grant effects should be built as a scorecard section joining claims to
outcomes, not as an unread export.

Registered integration evidence `knowledge-real-grants-postrepair` exported all
27 site symbols and 14,193 claim rows across 41 distinct culture/site pairs. At
15,000 ticks, the three surviving cultures held 14, 15, and 4 named
technologies. The run reached tier 2, so tier 3–4 natural activation remains a
longer-horizon evidence question rather than an implementation gap.
