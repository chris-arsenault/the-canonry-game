# Sea Raises the Reefhold

Status: coverage-focused workshop candidate

## Purpose

This generator exists to improve first-pass subkind coverage, especially for oceanic settlements, non-scar regions, and smaller faction forms. It should help push the starter catalog toward a broader sense of place rather than keeping all major events landlocked, institutional, or catastrophic.

## Summary

- Target kind: intentionally flexible for now
- Narrative event: a dangerous water-edge becomes newly legible as a named sea and a built reefhold rises to meet it

## Trigger shape

Keep this broad for now. The point of this draft is coverage and event shape, not precise semantic authoring.

- pressure around deep water, offshore survival, or exposed edge conditions
- some combination of reach, shelter, holding, or power that makes construction at sea plausible
- enough narrative pressure that a new named maritime site would enter the simulation

We can choose the exact target later based on what fits the graph best. The event matters more than target purity here.

## Created entities

- `Settlement` `reefhold`
- `Region` `sea`
- optional `Faction` `cell`
- optional `Artifact` `instrument`

## Created relationships

- reefhold connected to the new sea-region as its named built presence
- optional cell tied to the reefhold as its keepers, workers, or wardens
- optional instrument anchored to the reefhold or native to the sea
- if a faction is present, the reefhold should still read as the center of gravity, not the faction

Exact relationship choices can wait until the next semantic pass.

## Non-creation consequences

- the named map gains a true ocean-facing site instead of another inland or civic center
- a previously implied water margin becomes important enough to be named
- the world gets a place where offshore labor, ritual, salvage, hunting, warning, or listening can accumulate

## Why this belongs in the starter expansion set

- It directly improves weak subkind coverage: `reefhold`, `sea`, `cell`, and `instrument`
- It adds an oceanic event shape that the current five generators lack
- It keeps the created bundle cross-kind and place-centered
- It broadens the world away from artifact catastrophe and civic consolidation as the only strong event modes

## Open questions

- Should the reefhold feel more like a fortified outpost, a salvage platform, a ritual listening-post, or a working harbor on broken ice?
- Should the optional faction be a true resident `cell`, or should some variants omit faction creation entirely and let later generators politicize the site?
- Should the optional `instrument` read as navigational, ritual, acoustic, or extractive?
- Does the new `sea` want to be truly new named water, or a newly separated reach of an older parent region?
