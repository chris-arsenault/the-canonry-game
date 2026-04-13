# System 6: Tick Clock

Time management. Drives everything.

---

## Purpose

The tick clock advances the simulation. Every change in the world happens because a tick occurred and a system processed it. The clock is the heartbeat of the simulation -- without it, everything is frozen.

---

## Design Decisions

### One clock, no formal tick types

There is a single simulation clock. It ticks at a constant rate. There are no distinct "actor ticks," "system ticks," or "generation ticks" as separate clocks. Individual systems register to run at whatever cadence makes sense for them -- some every tick, some every N ticks. The distinction between fast-updating systems (entity movement) and slow-updating systems (generation) is a matter of cadence configuration, not separate clock infrastructure.

The earlier design references to "actor ticks," "system ticks," and "generation ticks" described update frequencies, not separate clocks. These terms may still be useful as shorthand for "systems that run frequently" vs. "systems that run infrequently," but there is no architectural distinction.

### Ticks map to in-world time

Each tick represents a fixed amount of in-world time. The exact mapping is TBD -- the canonry used 1 tick = 1 year, which is too high-level for this game. Likely candidates:
- 1 tick = 1 day (allows seasonal cycles, hero travel, daily resource production)
- 1 tick = 1 week (faster macro progression, still supports hero-level events)
- 1 tick = some abstract unit that isn't calendar-mapped

The mapping affects:
- **Lifecycle:** how many ticks a hero lives, how fast settlements grow
- **Seasons:** how many ticks per season/year cycle
- **Speed feel:** how much in-world time passes per second of real time at 1x speed
- **Session length:** how many in-world years pass in a 4+ hour session

This needs playtesting to find the right feel. The architecture should treat the mapping as a configurable constant, not a hard-coded assumption.

### Speed control: ticks per frame

Speed control works by varying how many ticks are processed per rendered frame:
- **Pause:** 0 ticks per frame. The simulation is frozen. The player can inspect, plan, and configure the tableau.
- **1x:** baseline tick rate. Ticks happen at a pace that feels like "watching the world unfold."
- **2x, 4x, etc.:** multiple ticks per frame. The simulation advances faster. The player sees the world evolve more quickly.

Preferred model: 2x means ticks happen twice as fast (each tick still represents the same in-world time, but more of them happen per second). This preserves simulation determinism -- a run at 1x and a run at 4x produce identical results, just at different speeds.

If performance cannot support this (if ticks become too expensive to run multiple per frame at high speeds), the fallback is increasing the in-world time each tick represents at higher speeds. This is a performance concession, not a design preference.

### Strict execution ordering within a tick

All systems execute in a defined, strict order within each tick. The results of phase 1 are visible to phase 2, but phase 2's results are NOT visible to phase 1 within the same tick.

**Tick phase order:**
1. Low-altitude systems update (entity behaviors, relationships, resources, lifecycle, bulk flow)
2. Pressure recomputation (aggregates the updated low-altitude state)
3. Card precondition evaluation (checks pressures, attributes, relationships against active cards)
4. Card execution (generators, systems, actions whose preconditions were met)
5. Effects applied to entity graph (new entities, modified attributes, new/changed relationships)

This ordering is strict for determinism and sanity. A card cannot react to state that was just created by another card in the same tick. A low-altitude system cannot see a card effect until the next tick. This creates a clean one-tick delay between cause and visible effect, which is acceptable at the game's contemplative pace.

The specific ordering of systems WITHIN a phase (e.g., which low-altitude system runs first) will be worked out during implementation. The phase boundaries are the design commitment.

---

## Cadence Configuration

Each system registers a cadence: how many ticks between its updates.

- **Every tick (cadence 1):** entity movement, resource consumption, relationship strength shifts. The fast-updating low-altitude systems.
- **Every N ticks (cadence N):** pressure recomputation might run every 5 ticks. Seasonal changes every ~90 ticks (if 1 tick = 1 day). Generator precondition checks every 10-20 ticks. The exact numbers are configurable and will be tuned through playtesting.
- **The cadence of a system does not change its phase.** A system with cadence 10 still runs in the same phase (e.g., phase 1 for low-altitude systems) -- it just skips 9 out of 10 ticks.

---

## Interactions with Other Systems

| System | How Tick Clock Interacts |
|--------|--------------------------|
| All systems | Every system is driven by the tick clock. Systems register their cadence and phase. |
| Map/Spatial (#4) | Seasons cycle based on tick count (periodic function). |
| Lifecycle (#9) | Entity aging is a function of ticks elapsed. |
| Save/Load (#15) | Current tick count is part of serialized state. Restoring a save resumes from the saved tick. |

---

## Open Questions (Deferred)

- **In-world time mapping.** What does 1 tick equal? Needs playtesting. Affects lifecycle, seasons, pacing, and session length.
- **Speed levels.** How many speed options (1x, 2x, 4x, 8x, max?). Whether a "skip to next event" option exists.
- **Intra-phase ordering.** Which low-altitude system runs first within phase 1. Whether this ordering matters or whether systems within a phase can be treated as independent (and potentially parallelized).
- **Performance budget per tick.** How expensive can a single tick be before speed control breaks down. Determines how complex individual systems can be and how many entities the simulation can support.
- **Tick visualization.** Whether the player sees a tick counter, an in-world calendar, or both. How time is communicated in the UI.
