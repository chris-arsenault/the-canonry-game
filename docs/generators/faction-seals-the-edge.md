# Faction Seals the Edge

Status: second finalized workshop candidate

## Purpose

This generator is part of the first-pass starter set: one generator per target kind, each creating a real connected bundle of new named entities. It is intended to be satisfiable from seed state and to prove that a faction-targeted generator can create place-centered structure rather than only more agency-agency relationships.

## Summary

- Target kind: `Faction`
- Narrative event: A cohesive faction pushes fire-cores, aurora light, or offerings to the edge of its reach and seals a threatened boundary with a holdfast, warden, and warding seal.

## Trigger shape

Keep this broad for now. Exact trigger logic should wait until the rest of the starter generators are shaped and we can check seed satisfiability across the whole set.

- `Cohesion` High
- `Reach` High, or growing territorial pressure
- edge pressure, external threat, or internal need to make reach spatially explicit
- optional secondary target: `Region` that can plausibly receive a new frontier settlement
- optional secondary target: existing `Faction` whose pressure, rivalry, or shadow helps explain the stronghold

Avoid depending on `AtWarWith`, `RivalOf`, or other relationships that may not exist in seed. Those can become strengthening conditions later rather than starter requirements.

## Created entities

- `Settlement` holdfast
- `Penguin` edge warden
- `Artifact` seal / wardstone / boundary instrument

## Created relationships

- holdfast `ControlledBy` target faction
- commander `MemberOf` target faction
- commander `HomeBase` holdfast
- warden `Holds` artifact
- artifact `Anchored` to holdfast
- artifact `ClaimedBy` target faction
- optional existing secondary faction `RivalOf` target faction
- optional stronger territorial version: holdfast `ClaimedBy` rival faction

## Non-creation consequences

- the target faction's political presence becomes spatially explicit
- frontier pressure becomes localized around a named site
- nearby regions or settlements gain a new center of control, defense, or contest
- an existing rival faction can be pulled more explicitly into the event through optional secondary targeting

## Why this belongs in the starter set

- It is generator-shaped rather than action-shaped: the core output is a new connected bundle.
- It is satisfiable from seed state: it can be triggered from faction attributes and general frontier pressure rather than requiring existing rivalry/war edges.
- It follows the cross-kind rule well: target kind is `Faction`, but the created center of gravity is `Settlement` + `Penguin` + `Artifact`.
- It strengthens artifact creation in the starter set without making the whole event artifact-dependent.

## Open questions

- What holdfast expression should this prefer: keep, watchpost, citadel, or cliff-cut bastion?
- Should the created artifact default to political insignia, military standard, or ritual ward?
- Should `Holds` and `Anchored` be mutually exclusive variants, or can the banner/wardstone move between commander and holdfast over time?
- Does this generator want an explicit conflict hook such as optional `ClaimedBy`, or should frontier conflict be added by later generators?
- Should the existing secondary faction be purely optional color, or should some variants require it?
