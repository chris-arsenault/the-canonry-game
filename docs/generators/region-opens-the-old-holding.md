# Region Opens the Old Holding

Status: third finalized workshop candidate

## Purpose

This generator is part of the first-pass starter set: one generator per target kind, each creating a real connected bundle of new named entities. It is intended to prove that a region-targeted generator can be caused by the landscape itself, rather than using the region as a generic backdrop for social emergence.

## Summary

- Target kind: `Region`
- Narrative event: A pristine or unstable region becomes permeable: deep ice, old shadow, or sealed memory opens into a sanctuary/holding-place, and a custodial order forms around it.

## Trigger shape

Keep this broad for now. Exact trigger logic should wait until the rest of the starter generators are shaped and we can check seed satisfiability across the whole set.

- latent significance, remoteness, holding, or instability in the region
- `Pristiness` or other region-state pressure that makes revelation plausible
- the sense that something important in the land is becoming newly legible to the simulation

Avoid depending on prior named relationships that may not exist in seed. This should be able to fire from region attributes and general world pressure alone.

## Created entities

- `Settlement` sanctuary / old holding
- `Faction` custodians
- `Artifact` relic / seal
- optional `Penguin` keeper

## Created relationships

- settlement `ControlledBy` new faction
- artifact `Anchored` to settlement
- artifact `NativeTo` target region
- artifact `ClaimedBy` new faction
- optional penguin `MemberOf` new faction
- optional penguin `HomeBase` settlement
- optional later historical version: artifact or settlement `Memorializes` an older founder, order, or catastrophe
- optional later moral-polarity version: sanctuary or relic is `CleansedBy` or `CorruptedBy` something older

## Non-creation consequences

- the target region becomes newly legible as a source of historical or sacred significance
- nearby systems gain a new point of attraction, custody, contest, or pilgrimage
- the sanctuary can gain a named keeper immediately if the optional penguin is present

## Why this belongs in the starter set

- It is generator-shaped rather than action-shaped: the event creates a real connected bundle.
- It is satisfiable from seed state: it can fire from region attributes and broad world pressure alone.
- It follows the cross-kind rule well: target kind is `Region`, but the created center of gravity is `Settlement` + `Faction` + `Artifact`.
- It gives the region slot a stronger authored identity than generic crisis-driven social formation.

## Open questions

- What settlement subtype should the sanctuary prefer: shrine-town, excavation camp, cloister, vault-settlement, or hidden refuge?
- Should the created faction default to custodians, excavators, pilgrims, or opportunists?
- Should the artifact be primarily sacred, historical, or dangerous in the base version?
- Should the relic eventually choose between `Anchored` and `NativeTo` in variants, or should old holdings usually carry both?
