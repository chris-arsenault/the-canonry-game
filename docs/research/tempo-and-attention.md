# Tempo and Attention Patterns Research

Research into how contemplative strategy games handle moment-to-moment attention flow, observation rhythm, and the balance between watching and acting.

---

## MicroCiv -- Strategy/Idle Hybrid

MicroCiv is a minimalist civilization-building game at the intersection of incremental/idle games and 4X strategy. It compresses the Civilization experience into a handful of meaningful decisions layered on top of automated resource accumulation. Various forms exist across web browsers and mobile platforms (itch.io being a common home).

### Core Loop

The player starts with a small settlement and basic resources ticking upward automatically. The idle layer handles production: food grows, population increases, basic materials accumulate. The strategy layer is where the player intervenes -- choosing what to build, where to expand, what to research, and occasionally how to respond to threats. The design question it answers: "What if Civilization were a game where 80% of the turns played themselves and you only stepped in for the 20% that mattered?"

### Idle vs Strategy Split

Resource generation, population growth, and basic territorial maintenance run automatically. The player's input is reserved for structural decisions: building placement, technology path selection, and crisis response. This creates a rhythm where the player checks in, surveys the state, makes two or three decisions, then waits for those decisions to play out.

### Tempo

Long stretches of passive accumulation punctuated by brief windows of active decision-making. Unlike a pure idle game (check in every few hours), MicroCiv compresses the cycle. Watch resources tick up for 30-60 seconds, notice a threshold, make a build decision, watch again. The rhythm is "monitor, decide, monitor, decide" with monitoring phases lasting much longer than decision phases.

### Attention Triggers

The player needs to pay attention when: a resource hits a cap (build something or waste production), a new technology becomes available, a crisis fires, or enough has accumulated to make a meaningful choice. Between these moments, attention is optional. Neglect has a cost (capped resources, missed opportunities) but not a catastrophic one.

### Session Length

Sessions range from five minutes of active play to extended background monitoring. The game is designed to be walked away from. No natural ending point -- more of a rhythm of check-ins, making it a companion activity rather than primary focus.

---

## SimCity and Cities: Skylines -- Attention Patterns

The city builder genre is one of the richest case studies in attention flow because the player is simultaneously a macro planner and a micro problem-solver.

### Map Overview vs Detail Panels

Persistent tension between bird's-eye view and data view. The map overview is where the player spends most passive time -- watching traffic flow, seeing buildings grow, observing the city as a living system. Detail panels (budget screens, utility overlays, zone demand indicators) are for diagnosis and planning. Split is roughly 60-70% map watching to 30-40% data consulting, shifting dramatically based on city state. A well-running city invites more map-gazing. A city in crisis sends the player into panels.

### The Rhythm

A typical cycle:
1. Watch the city at normal or fast speed
2. Notice something -- a traffic jam, a building turning brown, a demand bar spiking
3. Pause or slow down
4. Zoom into the problem area
5. Consult relevant data overlays
6. Plan and execute a fix
7. Unpause
8. Watch to see if the fix worked
9. Return to general observation

This cycle takes 30 seconds to several minutes depending on intervention complexity. Observation phases tend to be longest -- players genuinely enjoy watching their city operate.

### What Triggers Action

Action triggers fall into distinct categories:

- **Problem-reactive:** something visibly goes wrong (fire, traffic, buildings abandoned). Most urgent.
- **Data-reactive:** a metric crosses a threshold (budget going negative, demand filling, approval dropping). Slower-burn.
- **Milestone-driven:** population hits a number unlocking new buildings or zones. Most positive.
- **Curiosity-driven:** "I wonder what happens if I put a park here" or "let me follow this one car." Most contemplative, unique to the watching phase.
- **Boredom-driven:** nothing is wrong, nothing needs attention, so the player invents a project -- a new neighborhood, a highway redesign, an aesthetic improvement. The game shifts from reactive management to proactive creation.

### Speed Control

Speed management is itself a core attention mechanic. Players speed up time when waiting for money, zone development, or effects to manifest. They slow down or pause when planning complex interventions -- laying road networks, positioning utilities, dealing with multi-part crises.

The pause button functions almost like a "planning mode" toggle. Paused planning feels fundamentally different from watching the simulation run -- chess (static analysis) vs aquarium (dynamic observation).

### Overlays as Attention Lenses

Overlays radically change what the player notices. Without overlays: the aesthetic surface (buildings, roads, parks). With traffic overlay: bottlenecks invisible before. With pollution overlay: industrial zones reveal invisible damage.

Overlays are "attention lenses" -- they don't change the city, they change what the player perceives. Cities: Skylines' info views are effective because they use strong color coding making problems spatially obvious -- you don't need to read numbers, you see red areas and know something is wrong.

**Design insight:** The same space contains multiple information layers. The player chooses which layer to attend to.

### Lulls and Session Length

No true "nothing to do," but moments where nothing NEEDS to be done. The city is running, budget is positive, no crises. Players either enjoy watching (legitimate activity -- the simulation is visually rewarding), invent projects, or decide to stop.

Typical sessions: 1-3 hours. "One more thing" loop extends sessions. Players stop when they complete a self-set goal, run out of money, encounter a problem they don't want to solve now, or reach equilibrium that feels "done enough."

Natural lulls serve an important function: they give the player graceful exit points.

---

## The Contemplative Strategy Tempo

A distinct tempo across several genres: extended observation, deliberate analysis, a single purposeful action, then observation of consequences. Looking is the primary activity. Doing is punctuation.

### Zachtronics Games (Opus Magnum, SpaceChem, Shenzhen I/O)

The purest expression of contemplative strategy. The loop: study the problem, design a solution mentally, build it, test it, watch it fail, study why, redesign, test again. The "study" and "watch" phases are where gameplay happens. Building is just translating thought into action.

Players can stare at a puzzle for five minutes before touching anything, and that staring IS the game. The test phase -- watching the machine run -- is equally active. You're tracking whether each component does what you intended.

Tempo: minutes of contemplation, seconds of action, minutes of observation.

### Mini Metro / Mini Motorways

Start contemplative, become frantic. Early game: watch passengers accumulate, study flow patterns, make deliberate line placements. Late game: react to cascading failures.

The tempo shift teaches observation early, then tests whether you learned to see the right things. The "intervention when it breaks" pattern means attention oscillates between background monitoring and crisis response.

Works because the system is visually legible -- everything that matters is on one screen.

### The Witness

Observation literally IS the mechanic. Walk through an environment, notice patterns, translate observations into puzzle solutions. Tempo is almost entirely observation with brief bursts of interaction (drawing a line on a panel).

Works because observation is richly rewarded -- the environment is dense with visual information, and the player's ability to notice directly determines progress. No waiting, no idle time. Slowness is self-directed, not system-imposed.

### Grand Strategy at Low Speed

Europa Universalis, Crusader Kings, Dwarf Fortress in Fortress mode. These games allow -- even encourage -- pausing the simulation to study the map, read tooltips, examine relationships, and plan moves. Map-staring is not idle time; it's strategic assessment.

Player rhythm: unpause, watch time advance, notice something developing, pause, study, decide, unpause. Sessions can involve more paused analysis than live simulation.

### What Makes Contemplative Tempo Work vs Boring

Three factors determine the boundary:

1. **Information density:** During observation, is there genuinely something to study? If the game state is rich enough to reward careful looking, observation feels active. If simple and obvious, observation feels like waiting.

2. **Consequentiality:** Do the player's eventual actions matter enough to justify the study? High-stakes decisions make observation feel like preparation. Trivial decisions make it feel like procrastination.

3. **Visual legibility with hidden depth:** The state should be readable at a glance but reveal more on inspection. This separates productive staring from blank staring. The player needs to feel that every additional second of looking teaches something new.

**The tempo fails** when the observation phase has no ceiling (nothing more to learn by looking longer) or when the system is so opaque that looking doesn't help.

**It works best** when the player has a growing sense of understanding during observation, a moment of "I see it now" that triggers the decision, and visible feedback that confirms or challenges their understanding.

**The key insight:** The rhythm of contemplation depends on the game continuously teaching the player how to see.
