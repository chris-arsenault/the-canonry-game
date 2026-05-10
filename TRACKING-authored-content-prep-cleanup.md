# Authored Content Prep Cleanup Tracking

Status: backlog for cleanup before the second wave of authored content.

Note: long-term HPA routing architecture is tracked separately in
`TRACKING-hpa-architecture.md`. This file only tracks pre-authoring cleanup
items that remain independent of that movement architecture design.

## Deferred Items

1. Generalize map travel structures beyond roads.
   - Need a first-class non-entity, non-terrain map-object/travel-structure
     concept before adding bridges, ice crossings, or orca water tubes.
   - Desired behavior: authored gambits can create long stretches of travel
     structure; structures may alter enterability and cost per movement profile.
   - Do not implement this as terrain conversion or as a road-quality special
     case.

2. Revisit settlement coastal bloom. Partially fixed.
   - Observed symptom: long runs can litter the coast with 30-50 settlements by
     around tick 8,000.
   - Fixed: new trade routes created by settlement route gambits and
     `routes_draw_readers` now start at low strength, above immediate cull risk
     but below the strong-route generator threshold.
   - Remaining contributors: settlement-producing generators, trade-route
     reinforcement, and missing merge/absorb pressure.
   - Revisit longer-run settlement density after movement reachability and travel
     structures are stable enough that unreachable-target churn does not
     contaminate the diagnosis.

3. Add fog of war / unrevealed water-glass presentation.
   - Desired behavior: both planes should start partially unrevealed.
   - Visual direction: unrevealed cells use the stained-glass pane art; revealed
     terrain takes precedence.
   - Requires reveal-state design in projection/UI and possibly kernel events;
     do not mix with semantic terrain readability fixes.
