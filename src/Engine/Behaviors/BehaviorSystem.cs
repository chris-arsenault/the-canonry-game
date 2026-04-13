namespace Engine.Behaviors;

using Engine.Graph;
using Engine.Kinds;

/// <summary>
/// Runs impulse evaluation for all active entities with agency.
/// Registers with the TickClock at LowAltitude phase.
/// </summary>
public sealed class BehaviorSystem
{
    private readonly WorldGraph _graph;
    private readonly UtilityEvaluator _evaluator;

    /// <summary>Entity kinds that have agency (evaluate impulses).</summary>
    private static readonly EntityKind[] AgencyKinds =
    [
        EntityKind.Nation,
        EntityKind.Npc,
        // Post-MVP: Faction, Monster
    ];

    public BehaviorSystem(WorldGraph graph, UtilityEvaluator evaluator)
    {
        _graph = graph;
        _evaluator = evaluator;
    }

    /// <summary>
    /// Run one tick of behavior evaluation for all active agency entities.
    /// Called by the TickClock at LowAltitude phase.
    /// </summary>
    public void Tick(IWorldState worldState)
    {
        ArgumentNullException.ThrowIfNull(worldState);

        foreach (var kind in AgencyKinds)
        {
            foreach (var entityId in _graph.GetEntitiesByKind(kind))
            {
                var entity = _graph.GetEntity(entityId);
                if (entity is null || entity.Status != EntityStatus.Active)
                {
                    continue;
                }

                _evaluator.EvaluateAndExecute(entity, worldState);
            }
        }
    }
}
