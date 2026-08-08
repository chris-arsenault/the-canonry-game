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

## Ice Remembers catalog (recut 2026-08-08: enablement gates)

Most technologies are enablement gates: the gated content carries a
`CultureHoldsTech` precondition or generator `RequiredTech` and is
default-absent for cultures without the claim. The few surviving
multipliers are all >= x2 or <= x0.5. Tiers 0-1 are mundane arts;
tiers 2-4 climb the Three Fires (lore-bible section 7).

| Technology | Effect while claimed |
|---|---|
| Record-Carving | gate: Read the Ice (canon observance rites) |
| Breath-Holding Dives | sunken-relic recovery x2 |
| Bone Tools | all minor-actor work x2 |
| Ice Fishing | fishing and ordinary hunt verbs x2 |
| Call-Signals | `allied_with` maintenance x2 |
| Windbreaks | physical culture decay x0.5 |
| Rookery Schools | gate: Pass the Teaching (lineages exist) |
| Deep Netting | `offered_to` maintenance x2 |
| Ice-Block Building | hearth-banking x2 |
| The Breathing-Hole Watch | gate: Corner the Beast |
| Echo-Hunting | gate: Claim the Hunting Ground |
| Aurora Light Spectacles | physical attraction x2 |
| Lineage Charts | `member_of`, `led_by`, `taught_by` maintenance x2 |
| Trench Charting | gate: conduits_map_hidden_peaks generator |
| Carved Ways | penguin/faction travel cost x0.5 |
| Long-Dark Larders | physical carrying capacity x2 |
| Long-Distance Calls | gate: Parley with the Pod |
| Thaw Forecasting | thaw threshold x0.5 |
| Stone Libraries | semantic culture decay x0.5 |
| Pressure Diving | gate: Lay the Deep Current; the_current_provides generator |
| Ice-Ship Hulls | transport travel cost x0.5; gate: charters_find_harbors |
| Pack Hunting | gate: Slay the Monster, Drive Off an Orca, Raid the Trade Route |
| Pod Coordination | gate: March the Banners; campaign-column work x2 |
| Ice Cellars | physical decay tides x0.5 |
| Abyssal Mapping | gate: elders_descend_to_the_god, relics_are_given_to_the_deep |
| Plate of the Before Times | gate: Survey the Region, Open a Conduit |
| Storm Prediction | refreeze threshold x0.5; decay tides x0.5 |

The source catalog (`IceRemembersKnowledge.cs`) and the gate
annotations on the content remain authoritative.

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
