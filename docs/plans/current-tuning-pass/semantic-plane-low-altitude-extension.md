# Semantic Plane Low-Altitude Extension

## Framing

The semantic plane is currently a 2D axial hex plane:

- `q` = ideology
- `r` = autonomy
- `s` / resonance = calculated as `-q-r`

Do not treat semantic space as independently 3D until the engine and domain intentionally support that. The first extension should use the same low-altitude primitives already introduced for physical space, made plane-aware where necessary.

## Proposal

1. Make influence plane-aware.

   Influence becomes `(plane, entity, q, r)`, while coverage remains physical-only. Influence events and Monte Carlo telemetry should include `plane_code`. Existing physical behavior should remain equivalent.

2. Generalize influence consumers.

   `EffectsProgram` should read related influence on the effect's configured plane. `RelationshipProgram` should use the maintenance rule's plane for `SourceInfluenceAtTarget` and `TargetInfluenceAtSource`. `MapSignals` should include semantic influence when computing semantic capacity and attractor mass.

3. Keep physical coverage physical-only.

   `ExpandCoverage` remains physical. Semantic influence is non-exclusive pressure, not ownership.

4. Add semantic movement verbs for semantic-primary entities.

   Settlements: `SettlementDeliberate`, `SettlementGossip`.

   Factions: `FactionPreach`, `FactionQuarrel`.

   Regions: `RegionCommune`, `RegionDream`.

   These are movement-style intents on semantic `q/r`, with `s = -q-r`.

5. Add semantic influence verbs for semantic-primary entities.

   Settlements should exert civic, ritual, and habitability pressure around their semantic position.

   Factions should exert doctrinal, political, and logistical pressure around their semantic position.

   Regions should exert genius-loci / animist pressure around their semantic position.

   These use `GrowInfluence`, `ConsolidateInfluence`, and `ReduceInfluence` on `PrimaryPlane.Semantic`.

6. Add semantic influence verbs for physical-primary entities.

   Penguins and artifacts still physically move, but they should also affect semantic space.

   Penguins emit witness, echo, warmth, and social pressure around their semantic position.

   Artifacts emit resonance, power, corruption, warding, or consecrating pressure around their semantic position.

7. Update health effects by primary plane.

   Penguins and artifacts can keep mostly physical health effects, plus limited semantic effects if useful.

   Settlements, factions, and regions should get semantic support/threat/crowding effects.

   Regions need health effects added; they currently only age/drain.

8. Tune relationship maintenance as relationship-specific analysis.

   Existing maintenance already applies to all active relationships by relationship kind. The plane used by a maintenance rule should come from what best explains that relationship's behavior, not from labeling the relationship itself as physical or semantic.

   Example: `enemy_of` would probably strengthen from physical closeness because enemies encountering each other creates conflict pressure, even though `enemy_of` is semantically meaningful.

   Likewise, a spatial-looking relationship could strengthen from semantic closeness if the narrative meaning is alignment, ritual affinity, or shared doctrine.

9. Keep map activity dual-plane.

   `MapActivity` already records both physical and semantic presence. Semantic low-altitude verbs should consume the same activity, staleness, crowding, and hotspot mechanics.

10. Validate through Monte Carlo.

    Metrics should check semantic roaming variance, semantic influence spread, plane-specific relationship strengthening, lifespan effects by semantic exposure, and whether generator locality improves without collapsing physical map behavior.
