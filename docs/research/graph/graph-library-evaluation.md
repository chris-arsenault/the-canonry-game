# Graph Library Evaluation

Evaluation of in-memory graph libraries on NuGet for the entity graph implementation.

## Decision: QuikGraph

Use **QuikGraph** (`QuikGraph` NuGet package, v2.5.0) as the entity graph's backing data structure.

- 12M+ downloads, .NET Standard 2.0 (Godot 4 compatible)
- `BidirectionalGraph<TVertex, TEdge>` provides directed in/out adjacency queries
- `TaggedEdge<TVertex, TTag>` provides typed edge properties (strength, kind, metadata)
- Built-in BFS/DFS/shortest-path algorithms
- LINQ over `OutEdges`/`InEdges` for filtered traversal
- MS-PL license
- Feature-complete and stable (last update 2022, but mature)

## Alternatives Considered

**FastGraph** -- QuikGraph successor fork, but stalled in alpha. Same API. Fallback if QuikGraph breaks on future .NET.

**Rivers** -- Too minimal. No edge properties, no typed edges. Would require reimplementing what QuikGraph provides.

## What QuikGraph Does NOT Provide (We Add)

- **Secondary indexes by entity kind:** maintain a `Dictionary<EntityKind, HashSet<EntityId>>` alongside the graph. Trivial wrapper.
- **Historical relationship queries:** QuikGraph doesn't distinguish active vs historical edges. We wrap edges with our `Relationship` class (including start/end dates) and filter.
- **Spatial indexing:** not a graph concern. Separate spatial index on the map system.
- **Attribute storage:** entity attributes live in the Attributes system, keyed by entity ID. The graph stores identity and topology; attributes are a parallel system.

## API Sketch

```csharp
// Our entity graph wraps QuikGraph
public class WorldGraph 
{
    private BidirectionalGraph<EntityId, TaggedEdge<EntityId, Relationship>> _graph;
    private Dictionary<EntityId, Entity> _entities;
    private Dictionary<EntityKind, HashSet<EntityId>> _kindIndex;
    
    // Traversal: built-in BFS with LINQ filtering
    public IEnumerable<EntityId> EntitiesWithinHops(EntityId start, int maxHops, 
        Func<Relationship, bool>? edgeFilter = null) { ... }
    
    // Adjacency: direct from QuikGraph
    public IEnumerable<Relationship> OutgoingRelationships(EntityId entity) 
        => _graph.OutEdges(entity).Select(e => e.Tag);
    
    // Historical: filter by end date
    public IEnumerable<Relationship> HistoricalRelationships(EntityId a, EntityId b, string kind)
        => AllRelationshipsBetween(a, b).Where(r => r.Kind == kind && r.EndDate != null);
}
```
