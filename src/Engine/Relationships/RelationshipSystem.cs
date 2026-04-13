namespace Engine.Relationships;

using Engine.Graph;
using Engine.Kinds;

/// <summary>
/// Per-tick processing of all active relationships: applies strength modifiers,
/// culls relationships below threshold. Registers with TickClock at LowAltitude phase.
/// </summary>
public sealed class RelationshipSystem
{
    private readonly WorldGraph _graph;
    private readonly KindRegistry _kindRegistry;
    private readonly List<IStrengthModifier> _modifiers = [];

    public RelationshipSystem(WorldGraph graph, KindRegistry kindRegistry)
    {
        _graph = graph;
        _kindRegistry = kindRegistry;
    }

    public void AddModifier(IStrengthModifier modifier)
    {
        ArgumentNullException.ThrowIfNull(modifier);
        _modifiers.Add(modifier);
    }

    /// <summary>
    /// Process one tick: modify all active relationship strengths and cull weak ones.
    /// </summary>
    public void Tick(int currentTick, IRelationshipContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Collect relationships to process (avoid modifying collection during iteration)
        var toProcess = new List<Relationship>();
        var toCull = new List<Relationship>();

        foreach (var (entityId, _) in _graph.AllEntities)
        {
            foreach (var rel in _graph.OutgoingRelationships(entityId))
            {
                if (rel.IsActive)
                {
                    toProcess.Add(rel);
                }
            }
        }

        // Apply modifiers to each relationship
        foreach (var rel in toProcess)
        {
            var totalDelta = 0f;
            foreach (var modifier in _modifiers)
            {
                totalDelta += modifier.ComputeDelta(rel, context);
            }

            if (Math.Abs(totalDelta) > float.Epsilon)
            {
                _graph.UpdateRelationshipStrength(rel, rel.Strength + totalDelta);
            }

            // Check cull threshold
            var relDef = _kindRegistry.GetRelationshipKindDefinition(rel.Kind);
            var threshold = relDef?.CullThreshold ?? 0.05f;

            if (rel.Strength <= threshold)
            {
                toCull.Add(rel);
            }
        }

        // Cull weak relationships
        foreach (var rel in toCull)
        {
            _graph.CullRelationship(rel, currentTick);
        }
    }
}
