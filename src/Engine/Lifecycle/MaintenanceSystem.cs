namespace Engine.Lifecycle;

using Engine.Graph;

/// <summary>
/// Per-tick attribute maintenance for all active entities. Calls ApplyTickEffects()
/// on every entity's attribute collection, which composes all registered tick effects
/// (drift, drain, buffs, debuffs) into a single per-tick delta.
///
/// Drift rates are registered as tick effects at entity creation time from
/// AttributeConfig.DriftRate in the domain config. Other tick effects come from
/// relationships, artifacts, cards, etc.
/// </summary>
public sealed class MaintenanceSystem
{
    private readonly WorldGraph _graph;

    public MaintenanceSystem(WorldGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        _graph = graph;
    }

    /// <summary>Apply tick effects to all active entities.</summary>
    public void Tick()
    {
        foreach (var (_, entity) in _graph.AllEntities)
        {
            if (entity.Status != EntityStatus.Active) continue;
            entity.Attributes.ApplyTickEffects();
        }
    }
}
