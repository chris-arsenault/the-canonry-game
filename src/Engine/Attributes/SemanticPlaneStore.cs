namespace Engine.Attributes;

using Engine.Graph;

/// <summary>
/// Legacy store for entity positions on semantic planes.
/// Used by the relationship system. Will be migrated to entity-owned
/// SemanticPlane attributes when the relationship system is updated.
/// </summary>
public sealed class SemanticPlaneStore
{
    private readonly Dictionary<string, SemanticPlaneDefinition> _definitions = [];
    private readonly Dictionary<(EntityId EntityId, string PlaneName), (float X, float Y)> _positions = [];

    public void RegisterPlane(SemanticPlaneDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _definitions[definition.Name] = definition;
    }

    public SemanticPlaneDefinition? GetDefinition(string planeName)
    {
        return _definitions.GetValueOrDefault(planeName);
    }

    public void SetPosition(EntityId entityId, string planeName, float x, float y)
    {
        _positions[(entityId, planeName)] = (Math.Clamp(x, 0f, 1f), Math.Clamp(y, 0f, 1f));
    }

    public (float X, float Y)? GetPosition(EntityId entityId, string planeName)
    {
        return _positions.TryGetValue((entityId, planeName), out var pos) ? pos : null;
    }

    public string? GetLabel(EntityId entityId, string planeName)
    {
        if (!_definitions.TryGetValue(planeName, out var definition)) return null;
        if (!_positions.TryGetValue((entityId, planeName), out var pos)) return null;
        return definition.Reduce(pos.X, pos.Y);
    }

    public float? GetDistance(EntityId a, EntityId b, string planeName)
    {
        var posA = GetPosition(a, planeName);
        var posB = GetPosition(b, planeName);
        if (posA is null || posB is null) return null;

        var dx = posA.Value.X - posB.Value.X;
        var dy = posA.Value.Y - posB.Value.Y;
        return MathF.Sqrt((dx * dx) + (dy * dy));
    }

    public void RemoveEntity(EntityId entityId)
    {
        var keys = _positions.Keys.Where(k => k.EntityId == entityId).ToList();
        foreach (var key in keys) _positions.Remove(key);
    }
}
