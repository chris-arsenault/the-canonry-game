# Deferred Locality Thickening

Potential future direction for making localities richer. This is intentionally not the current recommended next move.

## Summary

One way to make movement more productive is to have actions thicken places:

- important actions leave short-lived local affordances behind
- named entities project small mixed-kind halos of value around themselves
- terrain periodically exposes temporary windows of value
- one action can set up a second action at the same locality for another kind

Examples:

- `Faction Preach` leaves a congregation or offering site
- `Settlement Deliberate` leaves rumor or office activity
- `Artifact Compel` leaves a charged or thinned site
- `Penguin Forage` leaves spoor, carrion, or opened caches

These would not be full named entities. They would be transient local conditions meant to create 2-3 action visits and more cross-kind overlap at the same place.

## Why It Is Deferred

The first locality-planning Monte Carlo pass does show that selected bundles are too thin:

- selected localities are overwhelmingly single-goal
- multi-goal share is only around `0-2.5%` depending on kind
- the rare thicker bundles mostly cluster on existing semantic hubs rather than frontier physical places

That does suggest the world is not naturally producing enough rich localities. But locality thickening introduces a large new tuning surface:

- new transient state type(s)
- new emit/decay rules
- new cross-kind affordance mappings
- terrain-side timing windows
- new observability and saturation questions

That is too much new machinery to treat as the default next step while the planner and process deck are still being tuned.

## Revisit Conditions

Revisit this direction only if lower-complexity changes fail to materially improve:

- selected locality multi-goal share
- realized actions per visit
- occupied physical map share
- physical anisotropy
- process monopoly / dead-verb count

## Lower-Complexity Alternatives To Try First

Before building locality thickening, prefer exploring:

1. Planner reward shaping:
   movement still obeys current preconditions, but destination choice and visit persistence reward productive follow-through more strongly.
2. Graph-side requests / obligations:
   entities emit satisfiable requests into the graph, and movement is how those graph demands are realized in space.
3. Greater baseline self-sufficiency:
   entities can maintain themselves enough to avoid pure recovery treadmills, while cross-kind interactions remain the stronger path.
4. Cross-kind emergence through follow-on effects instead of direct dependency:
   entities do not need another kind for every maintenance action, but their actions create graph or attribute consequences that other kinds care about.

## Current Position

This remains a plausible future direction, especially if the sim continues to show:

- movement without enough payoff after arrival
- frontier placements that fail to accumulate mixed-kind local value
- locality planning that mostly finds only one-action destinations

But it should be treated as a deferred branch, not the current primary intervention.
