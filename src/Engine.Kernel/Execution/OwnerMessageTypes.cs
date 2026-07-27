namespace Engine.Kernel;

// Typed messages exchanged between continuous logical owners.

internal readonly record struct ContinuousResolvedMovement(
    int Tick,
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

internal readonly record struct ContinuousCellReconciliation(
    int CellId,
    int PreviousOwnerEntityId,
    bool TerrainChanged,
    CellStateDelta Delta);

internal readonly record struct ContinuousRoadWork(
    int Tick,
    int PlaneCode,
    int Q,
    int R,
    FixedScalar Amount,
    KernelExecutionContext Context);

internal readonly record struct PreparedEntityCreation(
    EntityCreationIntent Intent,
    int PhysicalCellId,
    int SemanticCellId);

internal readonly record struct PreparedRelationshipCreation(
    RelationshipCreationIntent Intent);

internal readonly record struct CellMaterializationIntent(
    int PlaneCode,
    int Q,
    int R,
    int TerrainId,
    int OwnerEntityId,
    int SuperregionId,
    int ContinentId,
    int Tick,
    KernelExecutionContext Context);

internal readonly record struct TerrainCoverIntent(
    int Q,
    int R,
    int TerrainId,
    bool Clear,
    int Tick,
    KernelExecutionContext Context);

internal static class ProgramSignalCodes
{
    internal const int ChronicleGambitExecution = 1;
    internal const int ChronicleGeneratorFire = 2;
    internal const int WorldBeatRelationshipCreated = 3;
    internal const int WorldBeatRelationshipEnded = 4;
    internal const int WorldBeatChargeReset = 5;
    internal const int WorldBeatChargeApply = 6;
}

internal readonly record struct ProgramSignal(
    int ProgramId,
    int SignalCode,
    int A,
    int B,
    int C,
    int D,
    long ValueUnits);

internal readonly record struct EntityInfluenceMultiplierIntent(
    int EntityOccurrenceHandle,
    int DeltaUnits,
    int Tick,
    KernelExecutionContext Context);

internal readonly record struct CultureInfluenceMultiplierIntent(
    int CultureId,
    int DeltaUnits,
    int Tick,
    KernelExecutionContext Context);

internal readonly record struct ResolvedTravelStructure(
    int CellId,
    TravelStructureIntent Intent);

internal readonly record struct InfluenceCreditDelta(
    int EntityId,
    int PlaneCode,
    int DeltaUnits)
{
    internal bool SameDestination(InfluenceCreditDelta other) =>
        EntityId == other.EntityId &&
        PlaneCode == other.PlaneCode;
}

internal readonly record struct CultureStorageKey(
    int CultureId,
    int PlaneCode,
    int Q,
    int R,
    int Z);

internal readonly record struct MovementDelta(
    int EntityOccurrenceHandle,
    int PlaneCode,
    int Q,
    int R,
    int Z,
    int CauseCode,
    KernelExecutionContext Context);

internal readonly record struct AttributeDelta(
    int EntityOccurrenceHandle,
    int AttributeId,
    int DeltaUnits,
    KernelExecutionContext Context)
{
    internal bool SameDestination(AttributeDelta other) =>
        EntityOccurrenceHandle == other.EntityOccurrenceHandle &&
        AttributeId == other.AttributeId;
}

internal readonly record struct CultureDelta(
    int CultureId,
    int PlaneCode,
    int Q,
    int R,
    int Z,
    int SourceEntityOccurrenceHandle,
    int DeltaUnits,
    KernelExecutionContext Context)
{
    internal bool SameSourceDestination(CultureDelta other) =>
        CultureId == other.CultureId &&
        PlaneCode == other.PlaneCode &&
        Q == other.Q &&
        R == other.R &&
        Z == other.Z &&
        SourceEntityOccurrenceHandle ==
            other.SourceEntityOccurrenceHandle;
}

internal readonly record struct CultureReductionDelta(
    int OwnCultureId,
    int PlaneCode,
    int Q,
    int R,
    int DeltaUnits,
    KernelExecutionContext Context)
{
    internal bool SameDestination(CultureReductionDelta other) =>
        OwnCultureId == other.OwnCultureId &&
        PlaneCode == other.PlaneCode &&
        Q == other.Q &&
        R == other.R;
}

internal readonly record struct RelationshipDelta(
    int RelationshipOccurrenceHandle,
    int DeltaUnits,
    KernelExecutionContext Context)
{
    internal bool SameDestination(RelationshipDelta other) =>
        RelationshipOccurrenceHandle ==
        other.RelationshipOccurrenceHandle;
}

internal readonly record struct RelationshipEndDelta(
    int RelationshipOccurrenceHandle,
    int ThresholdUnits,
    bool Unconditional,
    KernelExecutionContext Context);

internal readonly record struct TagDelta(
    int EntityOccurrenceHandle,
    int TagId,
    bool Assigned,
    KernelExecutionContext Context);

internal readonly record struct CellStateDelta(
    int PlaneCode,
    int Q,
    int R,
    int TerrainId,
    int OwnerEntityId,
    KernelExecutionContext Context,
    int ExpectedOwnerEntityId = -1);

internal readonly record struct LifecycleDelta(
    int EntityOccurrenceHandle,
    int AttributeId,
    long ThresholdUnits,
    KernelExecutionContext Context);

internal static class EntityStructuralProjectionCodes
{
    internal const int CultureChanged = 1;
    internal const int Retired = 2;
}

internal readonly record struct EntityStructuralProjection(
    int EntityOccurrenceHandle,
    int OperationCode,
    int PreviousCultureId,
    int CurrentCultureId,
    int Tick,
    KernelExecutionContext Context);

internal readonly record struct RelationshipStructuralProjection(
    int RelationshipOccurrenceHandle,
    int RelationshipKindId,
    int TargetEntityId,
    int Tick,
    KernelExecutionContext Context);

internal readonly record struct RelationshipFollowProjection(
    int TargetEntityOccurrenceHandle,
    int RelationshipId,
    int SourceEntityId);

internal readonly record struct EntityRetirementCleanup(
    int EntityOccurrenceHandle,
    int ProgramId,
    int Tick,
    KernelExecutionContext Context);

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
    KernelExecutionContext Context);

internal readonly record struct EntityStateIntent(
    int EntityOccurrenceHandle,
    int OperationCode,
    int Value,
    KernelExecutionContext Context);

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
    KernelExecutionContext Context);

internal readonly record struct RelationshipCreationIntent(
    int RelationshipId,
    int KindId,
    int PolarityCode,
    int SourceEntityId,
    int TargetEntityId,
    int StrengthUnits,
    KernelExecutionContext Context);

internal readonly record struct RelationshipSetIntent(
    int RelationshipOccurrenceHandle,
    int StrengthUnits,
    KernelExecutionContext Context);

internal readonly record struct StrategicCellStateIntent(
    int PlaneCode,
    int Q,
    int R,
    int TerrainId,
    int OwnerEntityId,
    KernelExecutionContext Context);

internal readonly record struct TravelStructureIntent(
    int PlaneCode,
    int Q,
    int R,
    int TravelStructureId,
    KernelExecutionContext Context);

internal readonly record struct ForgeCellIntent(
    int EntityOccurrenceHandle,
    int Direction,
    int TerrainId,
    KernelExecutionContext Context);

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
    KernelExecutionContext Context);

internal readonly record struct DistrictResolution(
    StrategicCellStateIntent CellState,
    EntityStateIntent EntityState,
    EntityCreationIntent EntityCreation,
    RelationshipCreationIntent RelationshipCreation);
