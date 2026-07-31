# 🧊 The Canonry Game

A civilization simulation where you don't command anyone. You define what a
world *is* — its physics, its peoples, its appetites — through a roguelite
card tableau, then watch centuries unfold: wars get names, relics outlive
their makers, faiths starve or flourish, and the ice remembers all of it.

Built on Godot 4.6 and .NET 8 around a deterministic simulation kernel.
First world: **The Ice Remembers**.

## 🌌 The World: Aurora Berg

A continent-sized iceberg drifting through an endless frozen sea. Three
peoples share it and none of them can leave:

- ☀️ **The Aurora Stack** — surface penguins in crystalline spire-cities,
  who channel aurora light through prismatic lenses and believe the sky
  judges all actions. Open courts, public records, beacons in the dark.
- 🕯️ **The Nightshelf** — tunnel penguins beneath the permanent shelf, lit
  by bioluminescent glyphs, speaking in coded proverbs. Every debt is
  remembered; every record casts a redacted shadow.
- 🐋 **The Wake-Singers** — the orca, whose matriarchs carry centuries of
  song, whose worship pools at warm-water sites, and whose hunts write
  terror into coastal settlements.

The ice itself is the fourth presence: a slow consciousness that records
everything. Named wars, fallen heroes, and broken lands pass into *its*
keeping — world-owned memory that extends no culture's reach. Beneath the
water, something two miles long lies on the abyssal floor, its heart still
beating.

## 🗺️ Where Worlds Come From

Every run starts with generated geography, built in layers the way real
ground is: plate motion decides whether continents weld or rift, rivers
carve what tectonics built, impacts leave craters and ejecta, volcanic
archipelagos rise, and marine sediment settles. The result is a hex world
with real terrain consequence — passes matter, warm vents matter, wastes
stay broken.

And the map is not finished when play begins. The world **grows**: thaw
opens new ground at the frontier, cultures press outward, and reclaimed
land triples the playable space over a long run. Settlement follows
geography; geography keeps happening.

## 🌱 How Worlds Evolve

One deterministic tick clock drives everything (50 ticks ≈ one in-world
year). On it:

- **Culture is territory.** Every people paints influence onto the map by
  living — settlements radiate it, travelers deposit it, decay erodes what
  no one tends. Borders are where fields meet; fronts move when someone
  projects real presence, never by bookkeeping.
- **Entities act for themselves.** Penguins make offerings, mend each
  other, wrest artifacts, declare rivalries. Factions escalate pressed
  rivalries into generational wars — and exhausted wars can end in terms.
- **The world narrates itself.** When the state warrants it, generators
  mint named consequences: a war that burned long enough gets a name and a
  witness; a site charged with devotion answers with a calving blessing; a
  culture pushed to the brink erupts in a desperate surge. Every event
  spends or locks the state that made it possible, so nothing fires forever.
- **Progression runs on parallel tracks** — war, devotion, trade,
  knowledge, diplomacy, memory. Each culture thrives along its native
  track: the Nightshelf's shadow economy feeds on everyone's prosperity,
  the Wake-Singers' faith runs on tended offerings, knowledge lives on the
  semantic plane where held ground *is* known technology.
- **Ages turn.** World-beats open eras of freeze and thaw, tighten or
  loosen the decay tides, and give each epoch a different texture. Long
  chronicles cast named entities into multi-act stories with guaranteed
  endings.

Run it for twenty thousand ticks and you get four centuries of history:
population pyramids, trade networks, extinct pods, capitals earned and
razed — all inspectable, all replayable from a seed.

## 🎴 The Player

You are not a general and not a mayor. You are the author of the world's
nature: the card tableau defines what kinds of things exist, what they
want, and what the world rewards. Then the simulation takes it from there,
and your influence returns as high-level acts — never unit orders. Agency
through worldcraft.

## 🧭 Guiding Principles

- **Deterministic to the bone.** Same seed, same world, every time. The
  kernel is the only state mutator; everything else observes.
- **No hidden storyteller.** Every important fact is visible through
  events, projections, snapshots, or telemetry. If it can't be inspected,
  it doesn't exist.
- **Causality over quotas.** Events are paced by spending and locking real
  world state, not by caps or schedules. Balance emerges from theme —
  parasites on prosperity, desperation at the brink — never from a
  scoreboard controller.
- **No reservoirs.** Accumulation lives as relationship strength and
  culture mass on the map, not as invisible stockpiles.
- **Evidence before tuning.** Changes are judged by Monte Carlo runs across
  many worlds — trend shape over point targets, measured distributions
  over guessed thresholds.

## 🏗️ Architecture

A deterministic C# microkernel hosted by Godot. Domain content (all the
penguins, rites, and wars) compiles to primitive ids before it reaches the
kernel; the kernel mutates fixed buffers and emits primitive events; UI,
telemetry, and analysis consume those events through independent cursors.

```text
TheCanonryGame.App      Godot entrypoint
TheCanonryGame.Game     runtime facade and player intent port
TheCanonryGame.UI       Godot UI scripts

Engine.Host             runtime wiring and startup
Engine.Domain           domain ids, compilation, definition graph
Engine.Gameplay         reusable gameplay vocabulary and validation
Engine.Kernel           deterministic mutation, fixed buffers, primitive ids
Engine.Eventing         primitive event log and schema
Engine.Projection       UI/read-model projections
Engine.Observability    telemetry, run logs, parquet export inputs
Engine.WorldGen         hex grid, terrain, tectonics, startup generation

IceRemembers            domain content
IceRemembers.Seed       startup seed module
```

Engine projects stay domain-agnostic. `Engine.Kernel` allows no runtime
strings, LINQ, allocation-heavy collections, async, or I/O — fixed state,
primitive events, one clock. Details:
[docs/architecture.md](docs/architecture.md),
[docs/kernel/simulation-kernel-discipline.md](docs/kernel/simulation-kernel-discipline.md).

## 🚀 Getting Started

To play in Godot:

1. Open the project in Godot 4.6.
2. Run `scenes/Main.tscn`.
3. Controls: `Space` pause/resume, `Right` step one tick, `1`/`2`/`3` set
   tick rate, `R` reset with new seed, `N` toggle names, `G` toggle
   readings, `WASD` pan, scroll to zoom.

```bash
make ci         # lint + build + test
make build      # restore + release build
make test       # run xunit tests
make lint       # analyzer lint + format verification
make clean      # remove .artifacts/
make world      # generate a Release seed world
make mc         # create a registered Monte Carlo evidence run
make mc-fan     # fan a multi-seed evidence batch, one process per seed
make capture    # render a world through the canonical capture path
```

`make test` runs the already-built Release test outputs. Use `make build`
first after a clean checkout. Monte Carlo evidence runs are registered with
unique run ids — use the workflow in
[docs/tools/monte-carlo.md](docs/tools/monte-carlo.md).

## 📚 Documentation

- [docs/lore/lore-bible.md](docs/lore/lore-bible.md) — the world of Aurora
  Berg: peoples, regions, seasons, and what sleeps under the water.
- [docs/game-systems.md](docs/game-systems.md) — current system overview.
- [docs/systems/simulation-principles.md](docs/systems/simulation-principles.md)
  — durable simulation quality goals and analysis criteria.
- [docs/systems/culture.md](docs/systems/culture.md) — culture identity,
  influence, boundaries, slots, and labor.
- [docs/systems/16-generator-executor.md](docs/systems/16-generator-executor.md)
  — how the world narrates itself.
- [docs/systems/18-action-executor.md](docs/systems/18-action-executor.md)
  — gambits: how entities act.
- [docs/performance.md](docs/performance.md) — performance baseline and
  evidence.
- [docs/backlog.md](docs/backlog.md) — decided-but-unscheduled work and
  verification ledger.

Research under `docs/research/` is historical background; the numbered
systems docs describe what is implemented.
