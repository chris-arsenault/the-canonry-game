# Graph Database Evaluation

Research into whether a graph database makes sense as the entity graph's backing store.

## Conclusion: Custom In-Memory Graph, Not a Graph DB

For 1K-10K entities and 5K-50K relationships running queries every tick in a game loop, a graph database is engineering overhead that does not pay off. A custom in-memory graph built from C# dictionaries and adjacency lists outperforms any graph DB by 10-100x on latency, uses less memory, serializes trivially, and is fully debuggable.

## Why Not Graph DBs

### Neo4j, Memgraph, FalkorDB
All require separate server processes. Cannot run embedded in Godot C#. Disqualified on architecture alone -- no external process dependencies for a game.

### SQLite with Graph Extensions
Viable but suboptimal as a runtime engine. Attribute reads via SQL (~1-5 microseconds) are 20-100x slower than dictionary lookups (~50 nanoseconds). At thousands of reads per tick, this adds up. However, SQLite is excellent as a **persistence layer** -- the save file IS a database you can query for debugging.

### The Scale Argument
Graph databases earn their keep at millions of entities and billions of relationships. We are 2-3 orders of magnitude below that. At our scale, brute-force iteration IS the optimal strategy. Query planners add overhead without benefit.

## Recommended Architecture

### Runtime: Custom In-Memory WorldGraph
- `Dictionary<EntityId, Entity>` for O(1) entity lookup
- `Dictionary<EntityId, List<Relationship>>` for outgoing/incoming adjacency lists
- `Dictionary<(EntityId, EntityId, RelKind), List<Relationship>>` for historical queries
- Secondary indexes: `Dictionary<EntityKind, HashSet<EntityId>>`, spatial grid index
- Graph traversal as C# methods (~20 lines for n-hop traversal, debuggable, profilable)

### Persistence: MessagePack or SQLite
- **MessagePack-CSharp** for fast binary serialization (low-megabyte saves, <100ms serialize/deserialize)
- **SQLite** as alternative if we want human-inspectable saves and ad-hoc debug queries against save files
- Both are valid; can start with MessagePack and add SQLite export for debugging

### Historical Data
- Keep all relationships (active and ended) in adjacency structures
- Filter by `EndDate == null` for current state
- Destroyed entities retain data with `Status.Historical`

### Performance at Our Scale
- Entity attribute read: ~50ns
- Adjacency list lookup: ~100ns + iteration
- 2-hop traversal (avg degree 10): microseconds, not milliseconds
- Historical query ("were they ever allied?"): O(1) lookup + short list scan
- Full serialization (10K entities + 50K relationships): <100ms
