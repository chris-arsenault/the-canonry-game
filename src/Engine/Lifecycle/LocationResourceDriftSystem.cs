namespace Engine.Lifecycle;

using Engine.Attributes;
using Engine.Domain.Config;
using Engine.Graph;
using Engine.Resources;

/// <summary>
/// Updates dynamic tick-effect offsets derived from ambient resources at an entity's
/// current location. This keeps sustainment pressure tied to the map without turning
/// ambient resources into an actor-owned inventory system.
/// </summary>
public sealed class LocationResourceDriftSystem
{
    private readonly WorldGraph _graph;
    private readonly IReadOnlyDictionary<EntityKind, KindConfig> _kindConfigs;
    private readonly ResourceSystem _resources;

    private const string DriftSourcePrefix = "location_resource_drift_";

    public LocationResourceDriftSystem(
        WorldGraph graph,
        IReadOnlyDictionary<EntityKind, KindConfig> kindConfigs,
        ResourceSystem resources)
    {
        _graph = graph;
        _kindConfigs = kindConfigs;
        _resources = resources;
    }

    public void InitializeEffects()
    {
        foreach (var (_, entity) in _graph.AllEntities)
        {
            if (entity.Status != EntityStatus.Active) continue;
            EnsureEffects(entity);
        }
    }

    public void UpdateDriftEffects()
    {
        foreach (var (_, entity) in _graph.AllEntities)
        {
            if (entity.Status != EntityStatus.Active) continue;
            EnsureEffects(entity);
            UpdateEntityEffects(entity);
        }
    }

    private void EnsureEffects(Entity entity)
    {
        if (!_kindConfigs.TryGetValue(entity.Kind, out var kindConfig))
        {
            return;
        }

        foreach (var attrConfig in kindConfig.Attributes)
        {
            if (attrConfig.LocationResourceDriftResource is null)
            {
                continue;
            }

            var scalar = entity.Attributes.GetScalar(attrConfig.Name);
            if (scalar is null)
            {
                continue;
            }

            var sourceId = DriftSourcePrefix + attrConfig.Name;
            if (FindEffect(scalar, sourceId) is null)
            {
                scalar.AddTickEffect(new AttributeEffect(
                    sourceId,
                    $"Ambient {attrConfig.LocationResourceDriftResource.Value.ToDomainName()} offset on {attrConfig.Name}",
                    0f));
            }
        }
    }

    private void UpdateEntityEffects(Entity entity)
    {
        if (!_kindConfigs.TryGetValue(entity.Kind, out var kindConfig))
        {
            return;
        }

        foreach (var attrConfig in kindConfig.Attributes)
        {
            if (attrConfig.LocationResourceDriftResource is null)
            {
                continue;
            }

            var scalar = entity.Attributes.GetScalar(attrConfig.Name);
            if (scalar is null)
            {
                continue;
            }

            var effect = FindEffect(scalar, DriftSourcePrefix + attrConfig.Name);
            if (effect is null)
            {
                continue;
            }

            var resourceValue = _resources
                .GetLayer(attrConfig.LocationResourceDriftResource.Value)
                .GetStockpile(new Engine.Map.HexCoord(entity.Coordinates.Q, entity.Coordinates.R));
            var offset = resourceValue * attrConfig.LocationResourceDriftPerUnit;

            effect.Value = Math.Clamp(
                offset,
                attrConfig.LocationResourceDriftMinOffset,
                attrConfig.LocationResourceDriftMaxOffset);
        }
    }

    private static AttributeEffect? FindEffect(ScalarValue attr, string sourceId)
    {
        foreach (var effect in attr.TickEffects)
        {
            if (effect.SourceId == sourceId) return effect;
        }

        return null;
    }
}
