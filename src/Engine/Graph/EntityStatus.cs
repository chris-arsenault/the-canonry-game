namespace Engine.Graph;

/// <summary>
/// Lifecycle state of an entity. MVP: Active and Historical only.
/// Extensible for future statuses (dormant, exiled, ruined, etc.)
/// </summary>
public enum EntityStatus
{
    Active,
    Historical,
}
