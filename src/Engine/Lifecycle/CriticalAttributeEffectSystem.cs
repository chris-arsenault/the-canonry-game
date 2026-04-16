namespace Engine.Lifecycle;

using Engine.Attributes;
using Engine.Domain.Config;
using Engine.Graph;
using Engine.Kinds;

/// <summary>
/// Applies domain-defined penalties when a standard 0-100 scalar attribute falls into
/// the shared critical band. The engine owns when an attribute is "critical"; the
/// domain owns which secondary attribute is penalized and by how much.
/// </summary>
public sealed class CriticalAttributeEffectSystem
{
    private readonly WorldGraph _graph;
    private readonly IReadOnlyDictionary<EntityKind, KindConfig> _kindConfigs;

    public CriticalAttributeEffectSystem(
        WorldGraph graph,
        IReadOnlyDictionary<EntityKind, KindConfig> kindConfigs)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(kindConfigs);
        _graph = graph;
        _kindConfigs = kindConfigs;
    }

    public void UpdateEffects()
    {
        foreach (var (_, entity) in _graph.AllEntities)
        {
            if (entity.Status != EntityStatus.Active)
            {
                continue;
            }

            if (!_kindConfigs.TryGetValue(entity.Kind, out var kindConfig))
            {
                continue;
            }

            foreach (var attribute in kindConfig.Attributes)
            {
                var criticalEffect = attribute.CriticalEffect;
                if (criticalEffect is null || !StandardNeedScale.AppliesTo(attribute))
                {
                    continue;
                }

                var source = entity.Attributes.GetScalar(attribute.Name);
                var target = entity.Attributes.GetScalar(criticalEffect.TargetAttribute);
                if (source is null || target is null)
                {
                    continue;
                }

                var sourceId = GetSourceId(attribute.Name, criticalEffect.TargetAttribute);
                var existing = FindEffect(target, sourceId);
                if (!StandardNeedScale.IsCritical(source.Value))
                {
                    if (existing is not null)
                    {
                        target.RemoveEffectsBySource(sourceId);
                    }

                    continue;
                }

                var description = criticalEffect.Description
                    ?? $"Critical {attribute.Name} penalty on {criticalEffect.TargetAttribute}";
                if (existing is null)
                {
                    target.AddTickEffect(new AttributeEffect(sourceId, description, criticalEffect.DeltaPerTick));
                }
                else
                {
                    existing.Value = criticalEffect.DeltaPerTick;
                }
            }
        }
    }

    private static string GetSourceId(string sourceAttribute, string targetAttribute) =>
        $"critical_{sourceAttribute}_to_{targetAttribute}";

    private static AttributeEffect? FindEffect(ScalarValue attribute, string sourceId)
    {
        foreach (var effect in attribute.TickEffects)
        {
            if (effect.SourceId == sourceId)
            {
                return effect;
            }
        }

        return null;
    }
}
