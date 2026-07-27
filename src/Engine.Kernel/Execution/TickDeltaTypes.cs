namespace Engine.Kernel;

internal readonly record struct ResolvedMovement(
    int EntityId,
    int PlaneCode,
    int FromQ,
    int FromR,
    int FromZ,
    int ToQ,
    int ToR,
    int ToZ,
    int CauseCode,
    KernelExecutionContext Context);

internal readonly record struct ResolvedCellState(
    int CellId,
    CellStateDelta Delta);

internal readonly record struct ResolvedTravelStructure(
    int CellId,
    TravelStructureIntent Intent);

internal readonly record struct ResolvedCultureGroup(
    int OriginalStart,
    int SumUnits,
    int CultureId,
    int PlaneCode,
    int Q,
    int R,
    int Z) : IDeterministicOrder<ResolvedCultureGroup>
{
    internal bool SameCell(ResolvedCultureGroup other) =>
        PlaneCode == other.PlaneCode &&
        Q == other.Q &&
        R == other.R &&
        Z == other.Z;

    public int CompareTo(ResolvedCultureGroup other)
    {
        var comparison = PlaneCode.CompareTo(other.PlaneCode);
        if (comparison != 0) return comparison;
        comparison = Q.CompareTo(other.Q);
        if (comparison != 0) return comparison;
        comparison = R.CompareTo(other.R);
        if (comparison != 0) return comparison;
        comparison = Z.CompareTo(other.Z);
        if (comparison != 0) return comparison;
        comparison = CultureId.CompareTo(other.CultureId);
        return comparison != 0
            ? comparison
            : OriginalStart.CompareTo(other.OriginalStart);
    }
}

internal readonly record struct ResolvedCultureReductionGroup(
    int OriginalStart,
    int SumUnits,
    int OwnCultureId,
    int PlaneCode,
    int Q,
    int R) : IDeterministicOrder<ResolvedCultureReductionGroup>
{
    internal bool SameCell(ResolvedCultureReductionGroup other) =>
        PlaneCode == other.PlaneCode &&
        Q == other.Q &&
        R == other.R;

    public int CompareTo(ResolvedCultureReductionGroup other)
    {
        var comparison = PlaneCode.CompareTo(other.PlaneCode);
        if (comparison != 0) return comparison;
        comparison = Q.CompareTo(other.Q);
        if (comparison != 0) return comparison;
        comparison = R.CompareTo(other.R);
        if (comparison != 0) return comparison;
        comparison = OwnCultureId.CompareTo(other.OwnCultureId);
        return comparison != 0
            ? comparison
            : OriginalStart.CompareTo(other.OriginalStart);
    }
}

internal readonly record struct InfluenceCreditDelta(
    int EntityId,
    int PlaneCode,
    int DeltaUnits) : IDeterministicOrder<InfluenceCreditDelta>
{
    internal bool SameDestination(InfluenceCreditDelta other) =>
        EntityId == other.EntityId && PlaneCode == other.PlaneCode;

    public int CompareTo(InfluenceCreditDelta other)
    {
        var comparison = EntityId.CompareTo(other.EntityId);
        return comparison != 0
            ? comparison
            : PlaneCode.CompareTo(other.PlaneCode);
    }
}

internal readonly record struct CultureStorageKey(
    int CultureId,
    int PlaneCode,
    int Q,
    int R,
    int Z) : IDeterministicOrder<CultureStorageKey>
{
    internal bool SameDestination(CultureStorageKey other) =>
        CultureId == other.CultureId &&
        PlaneCode == other.PlaneCode &&
        Q == other.Q &&
        R == other.R &&
        Z == other.Z;

    public int CompareTo(CultureStorageKey other)
    {
        var comparison = CultureId.CompareTo(other.CultureId);
        if (comparison != 0) return comparison;
        comparison = PlaneCode.CompareTo(other.PlaneCode);
        if (comparison != 0) return comparison;
        comparison = Q.CompareTo(other.Q);
        if (comparison != 0) return comparison;
        comparison = R.CompareTo(other.R);
        return comparison != 0 ? comparison : Z.CompareTo(other.Z);
    }
}

internal readonly record struct MovementDelta(
    int EntityOccurrenceHandle,
    int PlaneCode,
    int Q,
    int R,
    int Z,
    int CauseCode,
    int Priority,
    long SourceKey,
    KernelExecutionContext Context) : IDeterministicOrder<MovementDelta>
{
    public int CompareTo(MovementDelta other) =>
        Compare(
            EntityOccurrenceHandle, other.EntityOccurrenceHandle,
            PlaneCode, other.PlaneCode,
            Priority, other.Priority,
            SourceKey, other.SourceKey,
            Q, other.Q,
            R, other.R,
            Z, other.Z);

    private static int Compare(
        int a0, int b0,
        int a1, int b1,
        int a2, int b2,
        long a3, long b3,
        int a4, int b4,
        int a5, int b5,
        int a6, int b6)
    {
        var comparison = a0.CompareTo(b0);
        if (comparison != 0) return comparison;
        comparison = a1.CompareTo(b1);
        if (comparison != 0) return comparison;
        comparison = a2.CompareTo(b2);
        if (comparison != 0) return comparison;
        comparison = a3.CompareTo(b3);
        if (comparison != 0) return comparison;
        comparison = a4.CompareTo(b4);
        if (comparison != 0) return comparison;
        comparison = a5.CompareTo(b5);
        return comparison != 0 ? comparison : a6.CompareTo(b6);
    }
}

internal readonly record struct AttributeDelta(
    int EntityOccurrenceHandle,
    int AttributeId,
    int DeltaUnits,
    long SourceKey,
    KernelExecutionContext Context) : IDeterministicOrder<AttributeDelta>
{
    public bool SameDestination(AttributeDelta other) =>
        EntityOccurrenceHandle == other.EntityOccurrenceHandle &&
        AttributeId == other.AttributeId;

    public int CompareTo(AttributeDelta other)
    {
        var comparison = EntityOccurrenceHandle.CompareTo(other.EntityOccurrenceHandle);
        if (comparison != 0) return comparison;
        comparison = AttributeId.CompareTo(other.AttributeId);
        return comparison != 0 ? comparison : SourceKey.CompareTo(other.SourceKey);
    }
}

internal readonly record struct CultureDelta(
    int CultureId,
    int PlaneCode,
    int Q,
    int R,
    int Z,
    int SourceEntityOccurrenceHandle,
    int DeltaUnits,
    long SourceKey,
    KernelExecutionContext Context) : IDeterministicOrder<CultureDelta>
{
    public bool SameDestination(CultureDelta other) =>
        CultureId == other.CultureId &&
        PlaneCode == other.PlaneCode &&
        Q == other.Q &&
        R == other.R &&
        Z == other.Z;

    public int CompareTo(CultureDelta other)
    {
        var comparison = CultureId.CompareTo(other.CultureId);
        if (comparison != 0) return comparison;
        comparison = PlaneCode.CompareTo(other.PlaneCode);
        if (comparison != 0) return comparison;
        comparison = Q.CompareTo(other.Q);
        if (comparison != 0) return comparison;
        comparison = R.CompareTo(other.R);
        if (comparison != 0) return comparison;
        comparison = Z.CompareTo(other.Z);
        if (comparison != 0) return comparison;
        comparison = SourceEntityOccurrenceHandle.CompareTo(other.SourceEntityOccurrenceHandle);
        return comparison != 0 ? comparison : SourceKey.CompareTo(other.SourceKey);
    }
}

internal readonly record struct CultureReductionDelta(
    int OwnCultureId,
    int PlaneCode,
    int Q,
    int R,
    int DeltaUnits,
    long SourceKey,
    KernelExecutionContext Context) : IDeterministicOrder<CultureReductionDelta>
{
    public bool SameDestination(CultureReductionDelta other) =>
        OwnCultureId == other.OwnCultureId &&
        PlaneCode == other.PlaneCode &&
        Q == other.Q &&
        R == other.R;

    public int CompareTo(CultureReductionDelta other)
    {
        var comparison = OwnCultureId.CompareTo(other.OwnCultureId);
        if (comparison != 0) return comparison;
        comparison = PlaneCode.CompareTo(other.PlaneCode);
        if (comparison != 0) return comparison;
        comparison = Q.CompareTo(other.Q);
        if (comparison != 0) return comparison;
        comparison = R.CompareTo(other.R);
        return comparison != 0 ? comparison : SourceKey.CompareTo(other.SourceKey);
    }
}

internal readonly record struct RelationshipDelta(
    int RelationshipOccurrenceHandle,
    int DeltaUnits,
    long SourceKey,
    KernelExecutionContext Context) : IDeterministicOrder<RelationshipDelta>
{
    public bool SameDestination(RelationshipDelta other) =>
        RelationshipOccurrenceHandle == other.RelationshipOccurrenceHandle;

    public int CompareTo(RelationshipDelta other)
    {
        var comparison = RelationshipOccurrenceHandle.CompareTo(other.RelationshipOccurrenceHandle);
        return comparison != 0 ? comparison : SourceKey.CompareTo(other.SourceKey);
    }
}

internal readonly record struct RelationshipEndDelta(
    int RelationshipOccurrenceHandle,
    int ThresholdUnits,
    bool Unconditional,
    long SourceKey,
    KernelExecutionContext Context) : IDeterministicOrder<RelationshipEndDelta>
{
    public bool SameDestination(RelationshipEndDelta other) =>
        RelationshipOccurrenceHandle == other.RelationshipOccurrenceHandle;

    public int CompareTo(RelationshipEndDelta other)
    {
        var comparison = RelationshipOccurrenceHandle.CompareTo(other.RelationshipOccurrenceHandle);
        return comparison != 0 ? comparison : SourceKey.CompareTo(other.SourceKey);
    }
}

internal readonly record struct TagDelta(
    int EntityOccurrenceHandle,
    int TagId,
    bool Assigned,
    int Priority,
    long SourceKey,
    KernelExecutionContext Context) : IDeterministicOrder<TagDelta>
{
    public bool SameDestination(TagDelta other) =>
        EntityOccurrenceHandle == other.EntityOccurrenceHandle &&
        TagId == other.TagId;

    public int CompareTo(TagDelta other)
    {
        var comparison = EntityOccurrenceHandle.CompareTo(other.EntityOccurrenceHandle);
        if (comparison != 0) return comparison;
        comparison = TagId.CompareTo(other.TagId);
        if (comparison != 0) return comparison;
        comparison = Priority.CompareTo(other.Priority);
        if (comparison != 0) return comparison;
        comparison = SourceKey.CompareTo(other.SourceKey);
        return comparison != 0 ? comparison : Assigned.CompareTo(other.Assigned);
    }
}

internal readonly record struct CellStateDelta(
    int PlaneCode,
    int Q,
    int R,
    int TerrainId,
    int OwnerEntityId,
    int Priority,
    long SourceKey,
    KernelExecutionContext Context) : IDeterministicOrder<CellStateDelta>
{
    public bool SameDestination(CellStateDelta other) =>
        PlaneCode == other.PlaneCode && Q == other.Q && R == other.R;

    public int CompareTo(CellStateDelta other)
    {
        var comparison = PlaneCode.CompareTo(other.PlaneCode);
        if (comparison != 0) return comparison;
        comparison = Q.CompareTo(other.Q);
        if (comparison != 0) return comparison;
        comparison = R.CompareTo(other.R);
        if (comparison != 0) return comparison;
        comparison = Priority.CompareTo(other.Priority);
        if (comparison != 0) return comparison;
        comparison = SourceKey.CompareTo(other.SourceKey);
        if (comparison != 0) return comparison;
        comparison = TerrainId.CompareTo(other.TerrainId);
        return comparison != 0 ? comparison : OwnerEntityId.CompareTo(other.OwnerEntityId);
    }
}

internal readonly record struct LifecycleDelta(
    int EntityOccurrenceHandle,
    int AttributeId,
    long ThresholdUnits,
    long SourceKey,
    KernelExecutionContext Context) : IDeterministicOrder<LifecycleDelta>
{
    public int CompareTo(LifecycleDelta other)
    {
        var comparison = EntityOccurrenceHandle.CompareTo(other.EntityOccurrenceHandle);
        if (comparison != 0) return comparison;
        comparison = AttributeId.CompareTo(other.AttributeId);
        return comparison != 0 ? comparison : SourceKey.CompareTo(other.SourceKey);
    }
}

internal static class EntityStateIntentCodes
{
    internal const int SetCulture = 1;
    internal const int SetStatus = 2;
    internal const int IncreaseWorkTier = 3;
    internal const int DecreaseWorkTier = 4;
}

internal readonly record struct AttributeSetIntent(
    int EntityOccurrenceHandle,
    int AttributeId,
    int ValueUnits,
    long SourceKey,
    KernelExecutionContext Context) : IDeterministicOrder<AttributeSetIntent>
{
    public int CompareTo(AttributeSetIntent other)
    {
        var comparison =
            EntityOccurrenceHandle.CompareTo(other.EntityOccurrenceHandle);
        if (comparison != 0) return comparison;
        comparison = AttributeId.CompareTo(other.AttributeId);
        return comparison != 0
            ? comparison
            : SourceKey.CompareTo(other.SourceKey);
    }
}

internal readonly record struct EntityStateIntent(
    int EntityOccurrenceHandle,
    int OperationCode,
    int Value,
    long SourceKey,
    KernelExecutionContext Context) : IDeterministicOrder<EntityStateIntent>
{
    public int CompareTo(EntityStateIntent other)
    {
        var comparison =
            EntityOccurrenceHandle.CompareTo(other.EntityOccurrenceHandle);
        return comparison != 0
            ? comparison
            : SourceKey.CompareTo(other.SourceKey);
    }
}

internal readonly record struct EntityCreationIntent(
    int EntityId,
    int KindId,
    int SubtypeId,
    int CultureId,
    int PhysicalQ,
    int PhysicalR,
    int SemanticQ,
    int SemanticR,
    int SemanticS,
    long SourceKey,
    KernelExecutionContext Context) : IDeterministicOrder<EntityCreationIntent>
{
    public int CompareTo(EntityCreationIntent other)
    {
        var comparison = EntityId.CompareTo(other.EntityId);
        return comparison != 0
            ? comparison
            : SourceKey.CompareTo(other.SourceKey);
    }
}

internal readonly record struct RelationshipCreationIntent(
    int RelationshipId,
    int KindId,
    int PolarityCode,
    int SourceEntityId,
    int TargetEntityId,
    int StrengthUnits,
    long SourceKey,
    KernelExecutionContext Context) : IDeterministicOrder<RelationshipCreationIntent>
{
    public int CompareTo(RelationshipCreationIntent other)
    {
        var comparison = SourceEntityId.CompareTo(other.SourceEntityId);
        if (comparison != 0) return comparison;
        comparison = TargetEntityId.CompareTo(other.TargetEntityId);
        if (comparison != 0) return comparison;
        comparison = KindId.CompareTo(other.KindId);
        return comparison != 0
            ? comparison
            : SourceKey.CompareTo(other.SourceKey);
    }
}

internal readonly record struct RelationshipSetIntent(
    int RelationshipOccurrenceHandle,
    int StrengthUnits,
    long SourceKey,
    KernelExecutionContext Context) : IDeterministicOrder<RelationshipSetIntent>
{
    public int CompareTo(RelationshipSetIntent other)
    {
        var comparison = RelationshipOccurrenceHandle.CompareTo(
            other.RelationshipOccurrenceHandle);
        return comparison != 0
            ? comparison
            : SourceKey.CompareTo(other.SourceKey);
    }
}

internal readonly record struct StrategicCellStateIntent(
    int PlaneCode,
    int Q,
    int R,
    int TerrainId,
    int OwnerEntityId,
    long SourceKey,
    KernelExecutionContext Context) :
    IDeterministicOrder<StrategicCellStateIntent>
{
    internal bool SameDestination(StrategicCellStateIntent other) =>
        PlaneCode == other.PlaneCode &&
        Q == other.Q &&
        R == other.R;

    public int CompareTo(StrategicCellStateIntent other)
    {
        var comparison = PlaneCode.CompareTo(other.PlaneCode);
        if (comparison != 0) return comparison;
        comparison = Q.CompareTo(other.Q);
        if (comparison != 0) return comparison;
        comparison = R.CompareTo(other.R);
        return comparison != 0
            ? comparison
            : SourceKey.CompareTo(other.SourceKey);
    }
}

internal readonly record struct TravelStructureIntent(
    int PlaneCode,
    int Q,
    int R,
    int TravelStructureId,
    long SourceKey,
    KernelExecutionContext Context) : IDeterministicOrder<TravelStructureIntent>
{
    public int CompareTo(TravelStructureIntent other)
    {
        var comparison = PlaneCode.CompareTo(other.PlaneCode);
        if (comparison != 0) return comparison;
        comparison = Q.CompareTo(other.Q);
        if (comparison != 0) return comparison;
        comparison = R.CompareTo(other.R);
        return comparison != 0
            ? comparison
            : SourceKey.CompareTo(other.SourceKey);
    }
}

internal readonly record struct ForgeCellIntent(
    int EntityOccurrenceHandle,
    int Direction,
    int TerrainId,
    long SourceKey,
    KernelExecutionContext Context) : IDeterministicOrder<ForgeCellIntent>
{
    public int CompareTo(ForgeCellIntent other)
    {
        var comparison =
            EntityOccurrenceHandle.CompareTo(other.EntityOccurrenceHandle);
        return comparison != 0
            ? comparison
            : SourceKey.CompareTo(other.SourceKey);
    }
}

internal static class DistrictIntentCodes
{
    internal const int Install = 1;
    internal const int Reawaken = 2;
}

internal readonly record struct DistrictIntent(
    int OperationCode,
    int EntityOccurrenceHandle,
    int TargetOccurrenceHandle,
    int A,
    int C,
    long XUnits,
    int ReservedEntityId,
    int ReservedRelationshipId,
    long SourceKey,
    KernelExecutionContext Context) : IDeterministicOrder<DistrictIntent>
{
    public int CompareTo(DistrictIntent other)
    {
        var comparison =
            EntityOccurrenceHandle.CompareTo(other.EntityOccurrenceHandle);
        return comparison != 0
            ? comparison
            : SourceKey.CompareTo(other.SourceKey);
    }
}

internal readonly record struct DistrictResolution(
    StrategicCellStateIntent CellState,
    EntityStateIntent EntityState,
    EntityCreationIntent EntityCreation,
    RelationshipCreationIntent RelationshipCreation);
