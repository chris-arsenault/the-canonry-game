namespace Engine.Kernel;

internal record struct RelationshipGraphCursor
{
    internal int DirectionCode;
    internal int FrozenIndex;
    internal int FrozenEnd;
    internal int DeltaIndex;
    internal int DeltaEnd;
    internal int MinimumStrengthUnits;
}
