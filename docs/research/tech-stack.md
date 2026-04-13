# Tech Stack Evaluation

## Framing

This game is a tick-based civilization simulation: societies, nations, cultures on a 2D physical map. Think Crusader Kings meets Dwarf Fortress legends mode. The workload is approximately 90% logic (entity graphs, AI, economy, diplomacy, culture simulation) and 10% rendering (2D map, UI panels, data visualization). This inverts the typical game engine priority: we need strong application/simulation capabilities with adequate rendering, not a great renderer with bolted-on logic.

For grounding: RimWorld was built in Unity with C#, using custom object and time handling because Unity's defaults are tuned for smooth-space 3D, not tile-based 2D with thousands of entities. Songs of Syx was built from scratch in Java with LWJGL. Dwarf Fortress is a custom C/C++ engine with SDL. The pattern: simulation-heavy games often fight their engines, and the most successful ones either use minimal engine overhead or build custom.

---

## 1. Godot 4

### Language: GDScript vs C#

GDScript is Godot's native scripting language -- lightweight, Python-like, tightly integrated with the editor. C# runs through the .NET runtime with Godot bindings. For a simulation-heavy game, **C# is the stronger choice**. C# is generally faster for heavy computational tasks: complex AI, procedural generation, large-scale data processing, entity graph traversal. GDScript has improved in Godot 4 (faster execution, lambda support, cyclic dependency handling), but for a game iterating over thousands of entities per tick computing diplomatic relations, economic output, and cultural drift, the performance gap matters.

C# in Godot has one significant caveat: **C# projects cannot export to the web platform in Godot 4**. This is a fundamental limitation of the .NET runtime in browser contexts. If web deployment ever matters, you'd need GDScript or GDExtension (native C/C++). Additionally, Godot does not generate C# bindings for GDExtensions, so calling native extensions from C# requires a GDScript intermediary.

### Tilemap and Hex Map

Godot 4's TileMap system has **native hexagonal grid support** -- select "Hexagon" in the tile set settings and it handles coordinate conversion, neighbor lookups, and rendering. The Asset Library has hex grid demos with A* pathfinding. Several community members have built 4X strategy games with hex tilemaps, including the open-source **OpenCiv3** project (a Civilization III remake in Godot with C#, currently in pre-alpha). The tilemap supports multiple layers, custom data per tile, and runtime modification via `set_cell()` / `get_cell()`.

### UI System

Godot ships with over 30 built-in Control nodes: buttons, labels, trees, item lists, tab containers, scroll containers, rich text labels, graphs, and more. The UI system is resolution-independent with anchoring, stretching, and container-based layout. For data-rich panels (entity inspectors, stat panels, relationship views), Godot's Control node hierarchy is genuinely strong. You can build complex nested panels with `VBoxContainer`, `HBoxContainer`, `TabContainer`, `Tree` (for hierarchical data), and `ItemList`. The theming system allows consistent styling. The `GraphEdit` and `GraphNode` controls could serve as a foundation for relationship graph visualization. Compared to Unity's fragmented UI story (see below), Godot's UI is more unified.

### Simulation-Heavy Games

Godot handles 2D strategy games well at indie scale. Node signals and tilemaps enable rapid prototyping. The natural architecture: simulation logic lives in pure C# (or GDScript) classes -- data structures, entity graphs, tick processing -- with the Godot scene tree used for rendering and UI. This separation is natural in Godot.

The concern is performance at scale: with 10,000+ entities per tick with complex relationship graphs, you'll need to profile and potentially offload hot loops to native code via GDExtension. Godot has no equivalent to Unity's DOTS/Burst for data-oriented parallel processing.

### Community Examples

**OpenCiv3** (Civilization III remake, C#, active development). Various grand strategy prototypes on forums and Medium devlogs. The itch.io strategy tag for Godot shows dozens of projects, though most are small-scope. **No AAA or major commercial strategy title has shipped on Godot yet** -- you'd be among the first.

### Deployment

Windows, macOS, Linux, Android, iOS, and web (WebAssembly/WebGL 2.0). Web export limited to the Compatibility renderer, and C# projects cannot export to web. Desktop deployment is straightforward. Godot 4.3+ improved web exports with single-threaded options.

### Learning Curve

Low to moderate. GDScript is approachable. C# in Godot has some friction (build pipeline, IDE setup). The editor is lightweight and responsive. Documentation is good but not as deep as Unity's for advanced topics.

### Pain Points Reported by Developers

Performance ceiling for large simulations without native code. C# web export gap. Smaller plugin ecosystem than Unity. Fewer shipped commercial strategy games to reference. Occasional breaking changes between minor versions (though less severe than Bevy).

---

## 2. Unity 2D

### Core Fit

Unity is the engine behind **RimWorld**, the most commercially successful game in this genre neighborhood. C# is excellent for simulation logic, and Unity's mature tooling handles rendering/input/audio/deployment while staying out of the way for gameplay code. However, as RimWorld's developer has noted, Unity required custom object and time handling systems because Unity is designed for smooth-space 3D with hundreds of objects, not tile-based 2D with thousands. You'll be working *around* Unity's assumptions, not with them.

### Tilemap System

Unity has native hexagonal tilemap support since 2018.2. The **Catlike Coding hex map tutorial series** is an industry-standard reference for Civilization-style hex maps: terrain, elevation, rivers, roads, fog of war, and pathfinding across 27 chapters. The tilemap supports rule tiles, animated tiles, and programmatic modification. Well-optimized: reduces load times, memory, and CPU versus individual sprite GameObjects.

### UI: UI Toolkit vs UGUI

Unity's messiest area. Two UI systems coexist:

- **UGUI (Unity UI)**: The established system. Everything is a GameObject with components. Well-documented, years of tutorials and assets. Good for in-game HUD with animations.
- **UI Toolkit**: The newer system inspired by web development (UXML for structure, USS for styling, C# for logic). Better performance for complex panels -- batched rendering, no GameObject overhead. Excels at inventory systems, skill trees, quest logs, data-heavy screens. But: limited animation support (transition-based only), no mask component, limited shader effects.

For data-rich panels, entity inspectors, and stat displays, **UI Toolkit is the better fit**, and the two systems can coexist. But the fragmentation is real: tutorials, assets, and community knowledge are split, and Unity's guidance has vacillated over the years.

### DOTS/ECS for Simulation

Unity 6 includes DOTS (Data-Oriented Technology Stack) with ECS, Burst compiler, and the Jobs system. Designed for simulation-heavy games: parallel processing of thousands of entities with cache-friendly data layouts. DOTS can deliver extraordinary performance for tick-based simulations. However: steep learning curve, fundamentally different paradigm from MonoBehaviour, smaller community knowledge base. Many Unity devs (including RimWorld's) use traditional MonoBehaviour and optimize with standard C# techniques.

### Strategy Games Built with Unity

**RimWorld** (colony simulation, massive success), **Northgard** (RTS), **Humankind** (4X by Amplitude, custom fork), **Oxygen Not Included** (colony sim by Klei), **Slay the Spire** (card strategy). Unity has the **strongest track record of shipped, commercially successful strategy and simulation games** of any engine.

### Deployment

Windows, macOS, Linux, Android, iOS, WebGL, consoles. The most comprehensive deployment story of any engine. WebGL builds work but can be heavy.

### Licensing

Unity cancelled the controversial runtime fee in 2024 after massive backlash. Current model: Unity Personal is free for revenue under $200,000/year. Pro subscriptions increased 8% in Jan 2025, Enterprise by 25%. The runtime fee debacle **permanently damaged trust** -- many developers cite it as a reason to avoid Unity even after the reversal. The concern is not current pricing but the demonstrated willingness to change terms retroactively.

### Learning Curve

Moderate to high. Unity is large and complex. 2D workflow requires understanding which features to ignore (most of the 3D pipeline). The volume of tutorials is both advantage (answers exist) and disadvantage (many outdated or contradictory).

### Pain Points

Engine bloat for a 2D game. UI system fragmentation. Licensing trust issues. Editor startup time and project size. DOTS learning curve. The engine assumes GameObject-centric architecture that simulation games often want to escape.

---

## 3. Bevy (Rust)

### ECS Architecture Fit

Bevy's ECS is architecturally ideal for entity-graph simulations. Components are plain data structs, systems are functions that query components, the scheduler automatically parallelizes non-overlapping data access. For a game that's 90% logic iterating entity graphs, this is the dream architecture. Bevy's ECS scales to millions of entities. Recent versions (0.15-0.18) added built-in one-to-many entity relationships, mapping directly to dynasty trees, diplomatic relations, and cultural hierarchies.

### Maturity Concerns

This is where the dream collides with reality. Bevy is at v0.18 as of early 2026. **A new version with breaking API changes ships approximately every 3 months.** Migration guides between 0.15-0.18 each document substantial breaking changes: reworked error handling, event system redesign, entity allocation API removal, rendering type reorganization. Building a multi-year project on Bevy means repeatedly rewriting code. Third-party crate compatibility breaks with each release. This is not theoretical -- it is the lived experience of Bevy developers.

### UI State

Bevy's built-in UI (`bevy_ui`) is primitive -- basic flex layout with Node elements, cumbersome for complex interfaces. The community workaround is `bevy_egui` (immediate-mode GUI). egui is functional for debug panels but not designed for polished, themed game UIs with complex layouts. Building character sheets, dynasty trees, economic overviews, and diplomatic maps would be a significant undertaking. The Bevy team has a vision document for a future UI system, but it doesn't exist yet.

### WASM Deployment

Compiles to WebAssembly, but: debug builds are enormous, release builds ~60MB, WASM compile times are painful, dynamic linking unsupported. Desktop works well. Web is technically possible but unpleasant.

### Strategy Games

**POLDERS** (city builder, has a Steam page) and a community 4X with P2P networking are the most notable. Ecosystem is thin for strategy games.

### Learning Curve

High. Rust (borrow checker, lifetimes, trait system) plus Bevy's API churn plus code-only workflow (no visual editor). Documentation fragmented between official docs, the unofficial cheat book, and community posts that may be outdated.

---

## 4. Defold

### Overview

Free, source-available, 2D-focused engine from the Defold Foundation (originally King/Candy Crush). Lua scripting, lightweight runtime.

### Strategy Game Fit

**Warnament** is the most notable strategy game in Defold -- a grand strategy game where the developer had to drop to C++ for performance-critical map operations. This is telling.

### Limitations

Tilemaps cannot be used in the GUI system. Tilemap dimensions are fixed and cannot be dynamically expanded. Performance drops significantly with large tilemaps. No native hex grid support. The recommended workaround is chunking, adding complexity.

### Lua as Simulation Language

Lua is fast for a scripting language but significantly slower than C# or Rust for heavy computation. For thousands of entities per tick with complex relationship graphs, Lua will be a bottleneck. Warnament's move to C++ confirms this.

### Deployment

Windows, macOS, Linux, iOS, Android, HTML5. Tiny ~2MB runtime.

### Pain Points

Lua performance ceiling. Fixed tilemap sizes. GUI/tilemap separation. Small community. No hex support. Very few people building this kind of game in Defold.

---

## 5. Phaser / PixiJS (Web-Native)

### Reality Check

These are HTML5/JavaScript frameworks designed for casual web games. For a Crusader Kings-meets-Dwarf Fortress simulation, they are fundamentally mismatched. No built-in ECS or entity management. No tilemap system at grand strategy scale. The UI story is "build it yourself." No strategy or simulation games of meaningful complexity have shipped with these.

### What You Get

TypeScript is good for simulation logic. Web deployment is trivially simple. The architecture of pure TS simulation + Phaser/PixiJS rendering is clean.

### Hard Limitations

Browser memory limits, single-threaded JS execution, no file system access. For hundreds of years of civilization history with thousands of entities, you will hit performance ceilings. No hex map libraries, no grand strategy UI frameworks, no asset ecosystem. You're building everything from scratch while constrained to browser capabilities.

### Where It Fits

A simplified browser-based prototype or "lite" companion app. Not the full vision.

---

## 6. SDL2 / Raylib + Custom

### The Case For

Dwarf Fortress and Songs of Syx took this path. When your game is 90% simulation, the "engine" is mostly getting in the way. SDL2/Raylib give you a window, tile drawing, input, audio. Everything else is custom. Full control over memory layout, data structures, threading, tick scheduling. No overhead, no fighting assumptions.

### The Case Against

You're building everything: tilemap rendering with culling and scrolling, camera systems, UI layout, text rendering, theming, scrollbars, panels, buttons, dropdowns, trees, tabs, input handling, font rendering, save/load, asset pipeline. All solved in Godot and Unity. Months of work before writing a single line of simulation code.

### Realistic Assessment

Makes sense if building the engine is part of the fun. Songs of Syx's developer spent years on infrastructure before gameplay was playable. Dwarf Fortress has 700,000 LOC and 20 years of development. These are cautionary tales as much as inspirations.

---

## Ranking

### 1. Godot 4 with C# (Recommended)

Best balance of capability, overhead, and fit. Strong UI system for data-rich panels. Native hex tilemap support. C# for simulation logic. Lightweight, stays out of your way. MIT license -- zero licensing risk. Active community building grand strategy and 4X games.

**Tradeoffs:** No DOTS-equivalent for parallel processing. C# web export blocked. Smaller ecosystem than Unity. Performance ceiling for very large simulations without GDExtension. No major commercial grand strategy title shipped on Godot yet.

**Mitigation:** Structure simulation as pure C# classes decoupled from Godot's scene tree. Maximum performance for logic, easy unit testing, option to move hot paths to GDExtension later. This also makes the simulation portable -- if you ever switch rendering layers, simulation code doesn't change.

### 2. Unity 2D with C#

Proven track record (RimWorld, Oxygen Not Included). Strongest ecosystem of strategy game assets and tutorials. UI Toolkit good for data panels. DOTS/ECS exists for extreme performance. Best deployment coverage.

**Tradeoffs:** Engine bloat for 2D. UI fragmentation. Licensing trust damage. Engine assumptions fight tick-based simulation. You'd build custom systems to work around Unity's design, as RimWorld did.

### 3. Bevy (Rust)

Architecturally the best fit for entity-graph simulation. ECS with relationships is exactly right. Rust's performance and safety are real.

**Tradeoffs:** API instability for a multi-year project. Immature UI = months of framework building before displaying a character sheet. Steep learning curve. Thin ecosystem. A bet on the future, not a pragmatic choice for shipping.

### 4. SDL2/Raylib + Custom

Maximum control, zero engine overhead.

**Tradeoffs:** Months to years of infrastructure before gameplay. Only if building the engine is part of the fun.

### 5. Defold

Lightweight, 2D-focused, free.

**Tradeoffs:** Lua performance ceiling, fixed tilemap sizes, no hex support, weak UI. Constraints compound for this genre.

### 6. Phaser / PixiJS

Web deployment simplicity.

**Tradeoffs:** Wrong tool for this job. Useful for a browser-playable prototype, not the full game.

---

## Recommendation

**Godot 4 + C#.** It handles rendering, UI, tilemaps, input, and deployment while letting you focus on simulation. Structure the simulation layer as engine-agnostic C# so you're never trapped. MIT license, no vendor lock-in, active development.

Unity is the safe second choice if you value proven commercial track record over licensing simplicity and engine weight. Bevy is the ambitious third choice if you're comfortable with Rust and willing to tolerate instability for architectural elegance.

The key architectural principle regardless of engine: **the simulation layer should be pure language code (C#, Rust, etc.) with zero engine dependencies.** This makes the simulation testable, portable, and decoupled from rendering concerns. The engine renders state; it doesn't own it.

---

## Sources

- [GDScript vs C# in Godot 4](https://chickensoft.games/blog/gdscript-vs-csharp) - Performance and ergonomic comparison
- [OpenCiv3 - Civilization III in Godot](https://github.com/C7-Game/Prototype) - Community strategy game
- [Godot Hex Grid Tilemap](https://godotengine.org/asset-library/asset/2085) - Asset library demo
- [Godot Control & UI System](https://deepwiki.com/godotengine/godot/4.4-control-and-ui-system) - DeepWiki reference
- [Godot Web Export Limitations](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_web.html) - C# restriction docs
- [Unity UI Toolkit vs UGUI 2025](https://medium.com/@studio.angry.shark/unity-ui-toolkit-vs-ugui-2025-developer-guide-8407312c91ed)
- [Unity Cancels Runtime Fee](https://unity.com/blog/unity-is-canceling-the-runtime-fee)
- [Catlike Coding Hex Map Tutorials](https://catlikecoding.com/unity/tutorials/hex-map/) - 27-part hex series
- [Godot vs Unity 2025](https://sunstrikestudios.com/en/godot_vs_unity_in_2025)
- [Bevy Migration Guide 0.17-0.18](https://bevy.org/learn/migration-guides/0-17-to-0-18/)
- [Warnament - Defold Grand Strategy](https://blog.luden.io/defold-game-engine-for-steam-games-production-1f4e657c012e)
- [Grand Strategy Anatomy in Godot](https://medium.com/@squashfold/anatomy-of-a-grand-strategy-game-devlog-1-3962ba395ae4)
