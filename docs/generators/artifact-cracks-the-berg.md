# Artifact Cracks the Berg

Status: fourth finalized workshop candidate

## Purpose

This generator is part of the first-pass starter set: one generator per target kind, each creating a real connected bundle of new named entities. It is intended to give artifact-targeted generation a true high-altitude role by letting a powerful artifact cause a new named region to come into being, rather than only producing custodians or derivative objects.

## Summary

- Target kind: `Artifact`
- Secondary target: `Region`
- Narrative event: A high-power or high-resonance artifact breaks the ice's holding. A true scar-region is named into being, with a choir, cell, or order rising to occupy or worship the rupture.

## Trigger shape

Keep this broad for now. Exact trigger logic should wait until the rest of the starter generators are shaped and we can check seed satisfiability across the whole set.

- `Resonance` High or `Power` High
- a vulnerable or symbolically loaded `Region` as secondary target
- pressure for rupture, overflow, corruption, or catastrophic emergence

Avoid depending on prior named relationships that may not exist in seed. This should be able to fire from artifact attributes plus a suitable region target.

## Created entities

- `Region` scar
- `Faction` choir / order / cell
- optional `Artifact` splinter / broken relic

The created region is a true new named region, not just a state change on the secondary target region.

## Created relationships

- new region `CorruptedBy` target artifact
- new region `ControlledBy` new faction in the political/occupation reading
- target artifact `Anchored` to the new region
- optional splinter artifact `PartOf` target artifact
- optional splinter artifact `NativeTo` new region
- optional new faction `RivalOf` controller of the surrounding parent region

## Non-creation consequences

- the secondary target region loses integrity as the new region tears out of it
- local safety, pristiness, and symbolic stability are degraded around the rupture
- the artifact becomes world-defining rather than merely locally significant

## Why this belongs in the starter set

- It is generator-shaped rather than action-shaped: the event creates a real connected bundle.
- It follows the cross-kind rule strongly: target kind is `Artifact`, but the created center of gravity is `Region` + `Faction`.
- It gives the starter set one unmistakably catastrophic, high-altitude event.
- It makes artifacts capable of reshaping the named map, not just modifying holders or sites.

## Open questions

- Should the target artifact also become `NativeTo` the scar in some variants, or does `Anchored` carry the stronger rupture meaning?
- Should the new faction always be present, or could some future variant create only the region and let later generators populate it politically?
- What artifact subtype should the optional splinter prefer: shard, echo, idol-fragment, cursed tool, or something broader?
- Does the parent region need an explicit relationship to the new region later, or is the split sufficiently carried by placement and narration?
