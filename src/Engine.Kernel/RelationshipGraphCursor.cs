namespace Engine.Kernel;

public record struct RelationshipGraphCursor
{
    internal int DirectionCode;
    internal int FrozenIndex;
    internal int FrozenEnd;
    internal int DeltaIndex;
    internal int DeltaEnd;
    internal float MinimumStrength;
}
