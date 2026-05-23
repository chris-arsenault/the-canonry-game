# Project Milestones

These milestones define project readiness. MVP means developer validation, not
public release.

## MVP: Minimum Viable Prototype

Audience: developer only.

Purpose: determine whether observing and lightly steering the simulation is
interesting enough to keep investing.

Done means:

- The Ice Remembers domain runs with at least the core MVP kind slice: region,
  settlement, faction, penguin, artifact.
- Low-altitude physics creates visible movement, culture influence, minor labor,
  relationship drift, aging, and death.
- Generators create named connected bundles.
- A small set of gambits creates meaningful relationship/status changes.
- The map, tableau, entity details, and event log make behavior observable.
- Monte Carlo telemetry can measure the simulation quality goals.
- A 30+ minute run produces enough world motion and named history to study.

Not required:

- balance
- public UX polish
- full card deck loop
- final art
- full procedural content breadth

## Tech Demo

Audience: small external testers.

Done means:

- One coherent Ice Remembers slice with real naming, subtypes, generators,
  gambits, and enough deck/tableau interaction to steer the world.
- Procedural map and seed generation are good enough to avoid overfitting to one
  hand-authored seed.
- UI supports observing map utilization, entity details, tableau diagnostics,
  and event history without developer guidance.
- Simulation-quality reports expose bad trends without ending the run.
- Save/load is reliable enough for multi-session testing.

## Early Access

Audience: public players comfortable with unfinished systems.

Done means:

- Full starter card loop: deck, hand, tableau, costs, modifications.
- Multiple archetypes or starting theories.
- Broader generator and gambit pool.
- Meta-progression and onboarding.
- Robust procedural maps and longer-run performance.
- Enough polish for players to understand why the world changed.
