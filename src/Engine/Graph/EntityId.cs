namespace Engine.Graph;

/// <summary>
/// Strongly-typed entity identifier. Wraps an integer for type safety.
/// </summary>
public readonly record struct EntityId(int Value)
{
    public override string ToString() => $"E{Value}";
}
