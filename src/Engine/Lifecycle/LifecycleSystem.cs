namespace Engine.Lifecycle;

using Engine.Attributes;
using Engine.Graph;
using Engine.Kinds;

/// <summary>
/// Per-tick lifecycle processing: updates age-based drain tick effects on health
/// attributes and transitions entities to Historical when health reaches zero.
///
/// At entity creation, registers a tick effect on the health attribute with the
/// initial drain rate. Each tick, updates the effect value based on current age.
/// The actual drain is applied by MaintenanceSystem via ApplyTickEffects().
/// </summary>
public sealed class LifecycleSystem
{
    private readonly WorldGraph _graph;
    private readonly LifecycleConfig _config;

    /// <summary>Source ID prefix for lifecycle drain effects.</summary>
    private const string DrainSourcePrefix = "lifecycle_drain_";

    /// <summary>Emitted when an entity transitions to historical status due to health reaching zero.</summary>
    public event EventHandler<LifecycleDeathEventArgs>? EntityDied;

    public LifecycleSystem(WorldGraph graph, LifecycleConfig config)
    {
        _graph = graph;
        _config = config;
    }

    /// <summary>Register lifecycle drain tick effects on all active entities. Call once at startup.</summary>
    public void InitializeEffects()
    {
        foreach (var (_, entity) in _graph.AllEntities)
        {
            if (entity.Status != EntityStatus.Active) continue;
            RegisterDrainEffect(entity);
        }
    }

    /// <summary>
    /// Update drain effect values based on current age. Call BEFORE MaintenanceSystem.
    /// </summary>
    public void UpdateDrainRates(int currentTick)
    {
        foreach (var (_, entity) in _graph.AllEntities)
        {
            if (entity.Status != EntityStatus.Active) continue;

            var lifecycleConfig = _config.GetConfig(entity.Kind);
            if (lifecycleConfig is null) continue;

            var (healthAttr, aging) = lifecycleConfig.Value;
            var age = currentTick - entity.CreatedAt;
            var drain = aging.ComputeDrain(age);
            var attr = entity.Attributes[healthAttr];
            if (attr is null) continue;

            var drainEffect = FindEffect(attr, DrainSourcePrefix + healthAttr);
            if (drainEffect is not null)
            {
                drainEffect.Value = -drain;
            }
        }
    }

    /// <summary>
    /// Check for death transitions. Call AFTER MaintenanceSystem has applied tick effects.
    /// </summary>
    public void CheckDeaths(int currentTick)
    {
        var toTransition = new List<EntityId>();

        foreach (var (entityId, entity) in _graph.AllEntities)
        {
            if (entity.Status != EntityStatus.Active) continue;

            var lifecycleConfig = _config.GetConfig(entity.Kind);
            if (lifecycleConfig is null) continue;

            var (healthAttr, _) = lifecycleConfig.Value;
            var attr = entity.Attributes[healthAttr];
            if (attr is not null && attr.Value <= 0f)
            {
                toTransition.Add(entityId);
            }
        }

        foreach (var entityId in toTransition)
        {
            var entity = _graph.GetEntity(entityId);
            if (entity is null) continue;

            _graph.SetEntityHistorical(entityId, currentTick);
            EntityDied?.Invoke(this, new LifecycleDeathEventArgs(entity, currentTick));
        }
    }

    /// <summary>Register a drain tick effect on a new entity's health attribute.</summary>
    public void RegisterDrainEffect(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var lifecycleConfig = _config.GetConfig(entity.Kind);
        if (lifecycleConfig is null) return;

        var (healthAttr, aging) = lifecycleConfig.Value;
        var attr = entity.Attributes[healthAttr];
        if (attr is null) return;

        var sourceId = DrainSourcePrefix + healthAttr;
        var initialDrain = aging.ComputeDrain(0);
        attr.AddTickEffect(new AttributeEffect(
            sourceId, $"Lifecycle aging drain on {healthAttr}", -initialDrain));
    }

    private static AttributeEffect? FindEffect(AttributeInstance attr, string sourceId)
    {
        foreach (var effect in attr.TickEffects)
        {
            if (effect.SourceId == sourceId) return effect;
        }

        return null;
    }
}

public sealed class LifecycleDeathEventArgs(Entity entity, int deathTick) : EventArgs
{
    public Entity Entity { get; } = entity;
    public int DeathTick { get; } = deathTick;
}
