# Settlement Names the Office

Status: fifth finalized workshop candidate

## Purpose

This generator is part of the first-pass starter set: one generator per target kind, each creating a real connected bundle of new named entities. It is intended to give the settlement slot a stronger institutional and symbolic event than simple daughter-colony formation.

## Summary

- Target kind: `Settlement`
- Narrative event: A settlement with strong dominion makes authority explicit by naming an office, seating a leader, and binding the role to a seal.

## Trigger shape

Keep this broad for now. Exact trigger logic should wait until the rest of the starter generators are shaped and we can check seed satisfiability across the whole set.

- `Dominion` High
- `Shelter` High or general civic stability
- pressure toward formalization, office, named authority, or institutional consolidation
- optional secondary target: existing `Region` whose broader importance the settlement is trying to answer or dominate

Avoid depending on prior named relationships that may not exist in seed. This should be able to fire from settlement attributes and broad civic pressure alone.

## Created entities

- `Faction` named office / civic order
- `Penguin` office-holder
- `Artifact` office seal / tome / regalia

## Created relationships

- target settlement `ControlledBy` new faction
- leader `MemberOf` new faction
- leader `HomeBase` target settlement
- leader `Holds` artifact
- artifact `Anchored` to target settlement
- artifact `ClaimedBy` new faction
- optional secondary existing region shapes where the civic order is trying to project legitimacy or answer wider pressure

## Non-creation consequences

- the target settlement's political identity becomes more explicit and legible
- diffuse civic pressure is concentrated into a named office and named institution
- nearby systems gain a clearer center of administration, prestige, or echo

## Why this belongs in the starter set

- It is generator-shaped rather than action-shaped: the event creates a real connected bundle.
- It is satisfiable from seed state: it can fire from settlement attributes and broad civic pressure alone.
- It follows the cross-kind rule better than the colony version: target kind is `Settlement`, but the created bundle is `Faction` + `Penguin` + `Artifact`.
- It gives the starter set a settlement-centered path to leadership, institution, and regalia without requiring expansion into a new daughter settlement.

## Open questions

- What faction subtype should the civic order prefer: `order`, `compact`, or `choir`? Narrative expression can still read as magistracy, ruling house, city guard, or civic liturgy.
- What NPC subtype should the leader prefer: magistrate, war-leader, speaker, prince, or founder-ruler?
- Should the artifact default to seal, charter, crown, baton, banner, or civic relic?
- Should `ControlledBy` be guaranteed in the first pass, or should some variants produce the order without full political control?
