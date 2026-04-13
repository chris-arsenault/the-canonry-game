namespace Engine.Relationships;

using Engine.Graph;
using Engine.Map;

/// <summary>
/// Proximity modifier: nearby entities trend toward stronger relationships.
/// Distance is measured in hex distance between entity coordinates.
/// </summary>
public sealed class ProximityModifier : IStrengthModifier
{
    public string Name => "proximity";

    /// <summary>Max hex distance at which proximity has any effect.</summary>
    public int MaxDistance { get; set; } = 10;

    /// <summary>Strength delta per tick at distance 0.</summary>
    public float MaxDelta { get; set; } = 0.005f;

    public float ComputeDelta(Relationship relationship, IRelationshipContext context)
    {
        ArgumentNullException.ThrowIfNull(relationship);
        ArgumentNullException.ThrowIfNull(context);

        var sourceCoords = context.GetCoordinates(relationship.SourceId);
        var targetCoords = context.GetCoordinates(relationship.TargetId);

        if (sourceCoords is null || targetCoords is null)
        {
            return 0f;
        }

        var dist = HexCoord.Distance(
            new HexCoord(sourceCoords.Value.Q, sourceCoords.Value.R),
            new HexCoord(targetCoords.Value.Q, targetCoords.Value.R));

        if (dist >= MaxDistance)
        {
            return 0f;
        }

        // Linear falloff: closer = more effect
        var factor = 1f - ((float)dist / MaxDistance);
        return MaxDelta * factor;
    }
}

/// <summary>
/// Shared culture modifier: entities of the same culture trend positive.
/// </summary>
public sealed class SharedCultureModifier : IStrengthModifier
{
    public string Name => "shared_culture";

    /// <summary>Bonus delta per tick when cultures match.</summary>
    public float MatchDelta { get; set; } = 0.003f;

    /// <summary>Penalty delta per tick when cultures differ.</summary>
    public float MismatchDelta { get; set; } = -0.001f;

    public float ComputeDelta(Relationship relationship, IRelationshipContext context)
    {
        ArgumentNullException.ThrowIfNull(relationship);
        ArgumentNullException.ThrowIfNull(context);

        var source = context.GetEntity(relationship.SourceId);
        var target = context.GetEntity(relationship.TargetId);

        if (source is null || target is null)
        {
            return 0f;
        }

        return string.Equals(source.Culture, target.Culture, StringComparison.Ordinal)
            ? MatchDelta
            : MismatchDelta;
    }
}

/// <summary>
/// Neglect modifier: relationships without reinforcing factors gradually weaken.
/// Applies a small negative delta every tick as a baseline decay.
/// </summary>
public sealed class NeglectModifier : IStrengthModifier
{
    public string Name => "neglect";

    /// <summary>Decay delta per tick (negative).</summary>
    public float DecayDelta { get; set; } = -0.001f;

    public float ComputeDelta(Relationship relationship, IRelationshipContext context)
    {
        return DecayDelta;
    }
}
