# Project Milestones

Definitions for what each phase means. The review agents have been treating MVP as a shippable product. It is not. These definitions clarify what "done" means at each stage.

---

## MVP (Minimum Viable Prototype)

**Audience:** The developer (me). Nobody else sees this.

**Purpose:** Determine whether the core simulation loop is interesting enough to invest further. Can I watch the world, understand what's happening, make card changes, and see meaningful consequences? Does the altitude model (low-altitude physics -> pressure -> cards) actually produce emergent behavior that's worth observing?

**What "done" looks like:**
- A fixed test map with 5 MVP entity kinds (Region, Settlement, Nation, NPC, Artifact)
- Low-altitude physics running (entity behaviors, relationships, lifecycle, resources)
- Pressure computation working
- A handful of test cards (generators, systems, actions) placeable via test UI
- Event log showing what's happening
- Basic map visualization (entity markers, territory, at least one overlay)
- Save/load functional
- I can sit and watch the simulation for 30+ minutes and find it interesting
- I can swap cards and see the world change in response

**What it is NOT:**
- Pretty
- Balanced
- Complete
- Playable by anyone other than me
- Running on the final engine (could be a throwaway prototype)

**Quality bar:** Functional, observable, modifiable. Bugs are expected. Art is placeholder. UI is developer-facing.

---

## Tech Demo

**Audience:** Close friends and family. 5-10 people who can tolerate jank.

**Purpose:** Validate that the game is interesting to someone other than the developer. Does watching and steering the simulation hold attention for a meaningful session? Is the card system comprehensible? Is the feedback loop (observe -> understand -> act -> observe consequences) legible?

**What "done" looks like:**
- One complete narrative domain (The Ice Remembers) with real subtypes, real cards, real naming
- One or more playable archetypes with a real starting deck
- Deck/hand/tableau mechanics functional (draw, play, remove with cost)
- Map visualization polished enough to be readable
- Event log useful for understanding what's happening
- UI layout navigable (map, tableau, hand, entity details all accessible)
- Anti-degeneracy detectors and warnings functional
- A 1-2 hour playable session that doesn't require developer guidance
- Known bugs documented, not necessarily fixed
- Still one domain, still limited card pool, still rough edges

**What it is NOT:**
- Bug-free
- Feature-complete
- Balanced beyond "doesn't obviously break"
- Ready for public feedback
- Monetizable

**Quality bar:** Playable, comprehensible, interesting for at least one session without hand-holding.

---

## Early Access

**Audience:** Public (or semi-public). Players who accept unfinished games.

**Purpose:** Broader validation and community feedback. Balance tuning with real player data. Feature prioritization based on actual player behavior.

**What "done" looks like:**
- Complete The Ice Remembers domain with full card pool
- Multiple archetypes
- Deck modification events functional
- Card enhancement/modification (Balatro-inspired) functional
- Procedural map generation
- Multiple post-MVP entity kinds implemented (at least Faction and Monster)
- Degenerate state metrics with warnings (possibly soft fail thresholds)
- Meta-progression across runs
- Polished UI
- Save/load robust
- Performance acceptable for 4+ hour sessions
- Tutorial or guided onboarding
- Patron mode (favored nation) functional

**What it is NOT:**
- Final content
- All entity kinds implemented
- Multiple domains
- Fully balanced
- Multiplayer

**Quality bar:** Publishable, retainable, worth paying for (even in early access framing).

---

## How This Affects Design Docs

When the design docs say "MVP," they mean the first milestone above: **can I sit and watch this simulation and find it interesting?** The bar is personal validation, not public readiness.

Reviewers should evaluate MVP scope against this bar, not against a public release bar. A system that's "not polished enough for players" is fine for MVP if it's functional enough for the developer to evaluate the core loop.

Post-MVP features are not deferred because they're unimportant. They're deferred because they don't help answer the MVP question: "is this interesting?"
