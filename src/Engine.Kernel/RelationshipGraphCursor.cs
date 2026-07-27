namespace Engine.Kernel;

internal record struct RelationshipGraphCursor
{
    internal int DirectionCode;
    internal int FrozenIndex;
    internal int FrozenEnd;
    internal int DeltaRelationshipId;
    internal int MinimumStrengthUnits;
    internal bool PublishedOnly;
}
