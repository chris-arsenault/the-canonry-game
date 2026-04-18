# Entity Index

Source of truth for entity kinds, attributes, and verbs is C#, not this document:

- **Enums (kinds, attributes, resources, relationships, verbs, cultures):** `src/Engine/Domain/TestDomain/TestDomainSchema.cs`
- **Runtime kind shapes + lifecycles + descriptions:** `src/Engine/Domain/TestDomain/TestDomainDefinition.cs`
- **Semantic stocks, verbs, and flow catalog (stock-flow authoring surface):** `src/Engine/Domain/TestDomain/TestDomainSemanticDefinition.cs`
- **Invariants enforced at build time:** `src/Engine/Domain/Graph/DefinitionGraphValidator.cs`

## Kind summary

Each kind has three scalar attributes — Health + one outward (Capacity or Projection) + one internal (Need or Drive) — plus a culture-neutral role on the semantic-coord plane (autonomy × resonance × tradition):

| Kind | Health | Outward | Internal | Primary plane |
|---|---|---|---|---|
| Npc | vitality | motivation (C) | satiety (N) | physical hex |
| Artifact | condition | power (C) | aura (D) | physical hex (carried) |
| Settlement | residents | dominion (P) | sanitation (N) | semantic |
| Faction | cohesion | influence (P) | provisions (N) | semantic |
| Region | geologic_stability | regional_safety (C) | pristineness (N) | semantic |

Region is the geographic area itself, not its inhabitants — its semantic alignment is the animist spirit-of-place.

## Verb roles

Every kind declares exactly one mobility verb (enablement — gets into range of targets) and exactly one drift verb (downtime — one-step random motion on its primary plane). The remaining verbs are Actions: self-sacrifice (H→X), hex→Health, untouched→hex, and the cross-entity RPS ring. See `TestDomainSemanticDefinition.cs` for the full catalog and authored narrative notes.

## Hex resources

Three ambient hex resources — `food`, `wealth`, `population` — are possibility space, not stock. Attribute→hex verbs expand hex values (+1 per firing). Hex→Health verbs read hex values as rate multipliers (2 × hex.value) and do not drain hex values.
