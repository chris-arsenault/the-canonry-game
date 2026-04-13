namespace Engine.Lifecycle;

using Engine.Attributes;
using Engine.Graph;
using Engine.Kinds;

/// <summary>
/// Per-tick lifecycle processing: applies aging drain to health metrics,
/// transitions entities to Historical when health reaches zero.
/// Registers with TickClock at LowAltitude phase.
/// </summary>
public sealed class LifecycleSystem
{
    private readonly WorldGraph _graph;
    private readonly AttributeStore _attributes;
    private readonly LifecycleConfig _config;

    /// <summary>Emitted when an entity transitions to historical status due to health reaching zero.</summary>
    public event EventHandler<LifecycleDeathEventArgs>? EntityDied;

    public LifecycleSystem(WorldGraph graph, AttributeStore attributes, LifecycleConfig config)
    {
        _graph = graph;
        _attributes = attributes;
        _config = config;
    }

    /// <summary>
    /// Process one tick: apply aging drain to all active entities with lifecycle config,
    /// transition to Historical if health reaches zero.
    /// </summary>
    public void Tick(int currentTick)
    {
        var toTransition = new List<EntityId>();

        foreach (var (entityId, entity) in _graph.AllEntities)
        {
            if (entity.Status != EntityStatus.Active)
            {
                continue;
            }

            var config = _config.GetConfig(entity.Kind);
            if (config is null)
            {
                continue;
            }

            var (healthAttr, aging) = config.Value;

            // Compute age and drain
            var age = currentTick - entity.CreatedAt;
            var drain = aging.ComputeDrain(age);

            // Apply drain to health attribute
            _attributes.Modify(entityId, healthAttr, -drain);

            // Check if health reached zero
            var health = _attributes.Get(entityId, healthAttr);
            if (health <= 0f)
            {
                toTransition.Add(entityId);
            }
        }

        // Transition dead entities
        foreach (var entityId in toTransition)
        {
            var entity = _graph.GetEntity(entityId);
            if (entity is null)
            {
                continue;
            }

            _graph.SetEntityHistorical(entityId, currentTick);
            EntityDied?.Invoke(this, new LifecycleDeathEventArgs(entity, currentTick));
        }
    }
}

public sealed class LifecycleDeathEventArgs(Entity entity, int deathTick) : EventArgs
{
    public Entity Entity { get; } = entity;
    public int DeathTick { get; } = deathTick;
}
