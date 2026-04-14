namespace Engine.Lifecycle;

using Engine.Attributes;
using Engine.Domain;
using Engine.Graph;
using Engine.Kinds;

/// <summary>
/// Per-tick attribute drift for all active entities. Each attribute with a non-zero
/// DriftRate in the domain config changes by that amount every tick.
///
/// This is the physics layer for attribute maintenance: hunger increases, condition
/// degrades, resonance accumulates — all defined as data in AttributeConfig.DriftRate.
///
/// Future: modifier composition via the effect system (#73). An artifact that grants
/// aging resistance would multiply the drift rate. For now, drift rates are applied directly.
/// </summary>
public sealed class MaintenanceSystem
{
    private readonly WorldGraph _graph;
    private readonly AttributeStore _attributes;
    private readonly Dictionary<EntityKind, List<(string Attribute, float Rate)>> _drifts;

    public MaintenanceSystem(
        WorldGraph graph,
        AttributeStore attributes,
        IReadOnlyDictionary<EntityKind, KindConfig> kindConfigs)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(attributes);
        ArgumentNullException.ThrowIfNull(kindConfigs);

        _graph = graph;
        _attributes = attributes;

        // Pre-compute drift lists per kind from domain config
        _drifts = [];
        foreach (var (kind, config) in kindConfigs)
        {
            var kindDrifts = new List<(string, float)>();
            foreach (var attr in config.Attributes)
            {
                if (attr.DriftRate != 0f)
                {
                    kindDrifts.Add((attr.Name, attr.DriftRate));
                }
            }

            if (kindDrifts.Count > 0)
            {
                _drifts[kind] = kindDrifts;
            }
        }
    }

    /// <summary>Apply all configured attribute drifts to all active entities.</summary>
    public void Tick()
    {
        foreach (var (kind, drifts) in _drifts)
        {
            foreach (var entityId in _graph.GetEntitiesByKind(kind))
            {
                var entity = _graph.GetEntity(entityId);
                if (entity is null || entity.Status != EntityStatus.Active) continue;

                foreach (var (attribute, rate) in drifts)
                {
                    _attributes.Modify(entityId, attribute, rate);
                }
            }
        }
    }
}
