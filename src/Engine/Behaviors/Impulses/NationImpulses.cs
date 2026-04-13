namespace Engine.Behaviors.Impulses;

#pragma warning disable CA1062 // Impulse parameters validated by UtilityEvaluator framework

using Engine.Graph;
using Engine.Kinds;

/// <summary>
/// Border tension: nations with adjacent rival nations experience increasing tension.
/// Scored by utility AI -- higher when borders are contested.
/// </summary>
public sealed class BorderTensionImpulse : IImpulse
{
    public string Name => "border_tension";

    public float TensionIncrement { get; set; } = 0.5f;

    public float Score(Entity entity, IWorldState worldState)
    {
        // Score higher when the nation has rival relationships
        var rivalCount = 0;
        foreach (var rel in worldState.Graph.OutgoingRelationships(entity.Id, "rival_of"))
        {
            if (rel.IsActive) rivalCount++;
        }

        return rivalCount > 0 ? Math.Min(0.3f + (rivalCount * 0.2f), 0.9f) : 0.1f;
    }

    public void Execute(Entity entity, IWorldState worldState)
    {
        // Increase internal tension based on number of rivals
        var rivalCount = worldState.Graph.OutgoingRelationships(entity.Id, "rival_of")
            .Count(r => r.IsActive);

        if (rivalCount > 0)
        {
            worldState.Attributes.Modify(entity.Id, "stability", -TensionIncrement * rivalCount);
        }
    }
}

/// <summary>
/// Consolidation pressure: large nations experience internal strain.
/// Scored higher for nations controlling many settlements.
/// </summary>
public sealed class ConsolidationImpulse : IImpulse
{
    public string Name => "consolidation";

    public float StrainPerSettlement { get; set; } = 0.1f;
    public int SettlementThreshold { get; set; } = 3;

    public float Score(Entity entity, IWorldState worldState)
    {
        var settlementCount = CountControlledSettlements(entity.Id, worldState);
        return settlementCount > SettlementThreshold ? 0.5f : 0.1f;
    }

    public void Execute(Entity entity, IWorldState worldState)
    {
        var settlementCount = CountControlledSettlements(entity.Id, worldState);
        if (settlementCount > SettlementThreshold)
        {
            var excess = settlementCount - SettlementThreshold;
            worldState.Attributes.Modify(entity.Id, "cohesion", -StrainPerSettlement * excess);
        }
    }

    private static int CountControlledSettlements(EntityId nationId, IWorldState worldState)
    {
        // Settlements have controlled_by -> nation. We need incoming relationships.
        var count = 0;
        foreach (var settlementId in worldState.Graph.GetEntitiesByKind(EntityKind.Settlement))
        {
            foreach (var rel in worldState.Graph.OutgoingRelationships(settlementId, "controlled_by"))
            {
                if (rel.TargetId == nationId && rel.IsActive)
                {
                    count++;
                    break;
                }
            }
        }

        return count;
    }
}
