# Entity Index

Source of truth for entity kinds, attributes, and verbs is C#, not this document:

- **Enums (kinds, attributes, resources, relationships, verbs, cultures):** `src/Engine/Domain/IceRemembers/IceRemembersSchema.cs`
- **Runtime kind shapes + lifecycles + descriptions:** `src/Engine/Domain/IceRemembers/IceRemembersDefinition.cs`
- **Semantic stocks, verbs, and flow catalog (stock-flow authoring surface):** `src/Engine/Domain/IceRemembers/IceRemembersSemanticDefinition.cs`
- **Invariants enforced at build time:** `src/Engine/Domain/Graph/DefinitionGraphValidator.cs`

## Kind summary

Each kind has three scalar attributes — Health + one outward (Capacity or Projection) + one internal (Need or Drive) — plus a culture-neutral role on the semantic-coord plane (autonomy × resonance × tradition):

| Kind | Health | Outward | Internal | Primary plane |
|---|---|---|---|---|
| Npc | vitality | echo (C) | warmth (N) | physical hex |
| Artifact | condition | power (C) | resonance (D) | physical hex (carried) |
| Settlement | residents | dominion (P) | shelter (N) | semantic |
| Faction | cohesion | reach (P) | provisions (N) | semantic |
| Region | stability | holding (C) | pristiness (N) | semantic |

Region is the geographic area itself, not its inhabitants — its semantic alignment is the animist spirit-of-place.

## Verb roles

Every kind declares exactly one mobility verb (enablement — gets into range of targets) and exactly one drift verb (downtime — one-step random motion on its primary plane). The remaining verbs are Actions: self-sacrifice (H→X), hex→Health, untouched→hex, and the cross-entity RPS ring. See `IceRemembersSemanticDefinition.cs` for the full catalog and authored narrative notes.

## Hex resources

Three ambient hex resources — `food`, `wealth`, `population` — are possibility space, not stock. Attribute→hex verbs expand hex values (+1 per firing). Hex→Health verbs read hex values as rate multipliers (2 × hex.value) and do not drain hex values.
