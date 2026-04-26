namespace Engine.Attributes;

using Engine.Domain;

/// <summary>
/// Base interface for all attribute types on an entity.
/// Scalar attributes have a single float value.
/// </summary>
public interface IEntityProperty
{
    string Name { get; }
    AttributeRuntimeId RuntimeId { get; }

    /// <summary>Apply registered tick effects. Called once per tick by MaintenanceSystem.</summary>
    void ApplyTickEffects();

    /// <summary>Remove all effects from a source.</summary>
    void RemoveEffectsBySource(string sourceId);
}
