# Authored Content Prep Cleanup Tracking

Status: remaining cleanup before the second wave of authored content.

Note: long-term HPA routing architecture is tracked separately in
`TRACKING-hpa-architecture.md`. This file only tracks pre-authoring cleanup
items that remain independent of that movement architecture design.

## Remaining Items

1. Add fog of war / unrevealed water-glass presentation.
   - Desired behavior: both planes should start partially unrevealed.
   - Visual direction: unrevealed cells use the stained-glass pane art; revealed
     terrain takes precedence.
   - Requires reveal-state design in projection/UI and possibly kernel events;
     do not mix with semantic terrain readability fixes.
