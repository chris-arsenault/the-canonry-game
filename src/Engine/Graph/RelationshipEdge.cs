namespace Engine.Graph;

using QuikGraph;

/// <summary>
/// QuikGraph edge wrapping our Relationship type.
/// TaggedEdge provides the Source/Target vertex pair plus our Relationship as the tag.
/// </summary>
public sealed class RelationshipEdge : TaggedEdge<EntityId, Relationship>
{
    public RelationshipEdge(EntityId source, EntityId target, Relationship relationship)
        : base(source, target, relationship)
    {
    }
}
