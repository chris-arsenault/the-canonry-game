# Generator Work Log

Short handoff log for picking the design work back up without rereading the entire thread.

## Current state

We now have a first-pass starter set of five generator workshop docs, one per target kind:

- [penguin-gathers-exiles.md](/home/dev/repos/the-canonry-game/docs/generators/penguin-gathers-exiles.md)
- [faction-seals-the-edge.md](/home/dev/repos/the-canonry-game/docs/generators/faction-seals-the-edge.md)
- [settlement-names-the-office.md](/home/dev/repos/the-canonry-game/docs/generators/settlement-names-the-office.md)
- [region-opens-the-old-holding.md](/home/dev/repos/the-canonry-game/docs/generators/region-opens-the-old-holding.md)
- [artifact-cracks-the-berg.md](/home/dev/repos/the-canonry-game/docs/generators/artifact-cracks-the-berg.md)

We also now have two coverage-focused workshop candidates aimed primarily at widening subkind coverage:

- [sea-raises-the-reefhold.md](/home/dev/repos/the-canonry-game/docs/generators/sea-raises-the-reefhold.md)
- [the-shelf-yields-a-tome.md](/home/dev/repos/the-canonry-game/docs/generators/the-shelf-yields-a-tome.md)

Shared principles, coverage summaries, and subtype proposals live in [design-work.md](/home/dev/repos/the-canonry-game/docs/generators/design-work.md).

## Important design decisions already made

- Generators are high-altitude world events that intermittently bring new named entities into the simulation.
- They must create connected bundles, not merely mutate existing relationships.
- Starter generators should be satisfiable from seed state.
- Cross-kind creation is generally more interesting than same-kind echo.
- The current settlement generator was intentionally changed from colony formation to civic-order formation to reduce guaranteed settlement overproduction.
- The region generator was intentionally changed from refugee-polity emergence to buried-sanctuary revelation so the landscape itself is the cause of the event.
- The artifact generator is locked to the corrupt version: it creates a true new region plus a faction, with optional splinter artifact.

## Current subtype proposal

First-pass subtype families currently proposed in [design-work.md](/home/dev/repos/the-canonry-game/docs/generators/design-work.md):

- `Penguin`: `Outcast`, `Commander`, `Ruler`, `Mystic`
- `Faction`: `ExileBand`, `CivicOrder`, `FrontierHost`, `Cult`
- `Settlement`: `Refuge`, `Fortress`, `Town`, `Sanctuary`
- `Artifact`: `Regalia`, `Relic`, `Ward`, `Shard`
- `Region`: `Wilderness`, `Heartland`, `Frontier`, `Scar`

These are still design proposals, not encoded schema.

## Next major pass

Next step is a domain pass.

First task:

- rename `IceRemembers` to the shipping-facing domain name for this project, rather than continuing to build on a placeholder

Working recommendation:

- display/name-facing domain name: `The Ice Remembers`
- code-facing short identifier: `IceRemembers`
- short abbreviation when one is truly needed: `TIR`

Recommendation detail:

- Prefer `IceRemembers` for C# type names, namespaces, config identifiers, and folder names.
- Avoid baking `TIR` in as the main code identifier. It is fine as a shorthand, but too compressed for primary use.
- Use `The Ice Remembers` only where the full player-facing/domain-facing name is wanted.

## Why this matters

We are doing enough naming-sensitive design work now that continuing to author under `IceRemembers` will create avoidable churn later. Better to switch the domain identity early, even if the shipped content is still partial.

## Resume point

When resuming:

1. Do the domain rename pass from `IceRemembers` toward `IceRemembers` / `The Ice Remembers`.
2. Revisit the subtype proposal during that domain pass so the subtype names and the domain identity reinforce each other.
3. Only after that, move the generator workshop docs toward semantic config encoding.
