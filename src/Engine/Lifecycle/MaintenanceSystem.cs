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
        Tick(_graph.AllEntities.Values
            .Where(entity => entity.Status == EntityStatus.Active)
            .ToArray());
    }

    /// <summary>Apply tick effects to a caller-provided active-entity snapshot.</summary>
    public void Tick(IReadOnlyList<Entity> activeEntities)
    {
        ArgumentNullException.ThrowIfNull(activeEntities);

        foreach (var entity in activeEntities)
        {
            entity.Attributes.ApplyTickEffects();
        }
    }
}
