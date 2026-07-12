# System 24: Canons And Jurisdictions

Canons are named bodies of law, custom, prohibition, and doctrine living on
the semantic plane — the feature that lets the simulation invent "The Long
Dark Edict" and have it govern something. A canon is a Spatial,
semantic-primary entity (subtypes `custom`, `taboo`, `accord`, `doctrine`;
health attribute `observance`) whose **named coverage patch on the semantic
plane is the region of idea-space it governs**. Entities whose semantic
position falls inside are living under it.

## Origins

Seeded anchors: The Flipper Accord and The Unspoken Silence. Born from
play: wars end in accords (`ice_settles_accounts`), the Deep Conclave
decrees taboos, pilgrims found doctrine at new sacred sites, and the Accord
tribunal legislates customs at settlements it pacifies. Generator-born
canons claim idea-ground with the `ClaimSemanticJurisdictionEffect`
(radius claim of unowned semantic cells around the canon) and are always
relationship-connected into their founding bundle.

## Jurisdiction effects (both planes)

The `InsideOwnedCoverage` effect condition makes named coverage
mechanically real, filtered by plane, owner kind, owner subtype, and —
via the general rule-culture gate — by the affected entity's culture.
Physical regions use the same condition: scars strain everyone inside,
faces favor Aurora, depths favor Nightshelf, seas favor Wake-Singers.
Semantic canons: accords shelter, taboos strain everyone but their
keepers, doctrines favor the choir, customs shelter mildly.

## Breach and enforcement

Violation is a state, not an event: jurisdiction-filtered tag rules hold
`oathbreaker` (war edges while inside accord coverage) and `profaner`
(devotion flow inside taboo ground) exactly as long as the breach persists
inside the jurisdiction. Enforcement content consumes the states:
`the_accord_holds` censures oathbreakers and damps their wars — accords
actively end wars; `the_silence_answers` smothers profaners' devotion —
taboo ground suppresses worship economies.

## Deferred, recorded deliberately

Per-canon prohibitions (per-entity rule content), event-semantic breach
detection, observance-scaled jurisdiction evidence, and coverage-wide
canon erosion.
