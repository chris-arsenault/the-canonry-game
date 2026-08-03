namespace Engine.Observability;

using System.Collections.ObjectModel;

/// <summary>
/// In-memory primitive telemetry rows for a Monte Carlo run or batch.
/// Writers project these rows to Parquet/DuckDB-compatible tables.
/// </summary>
public sealed class TelemetryBatch
{
    public Collection<SymbolRow> Symbols { get; } = Rows<SymbolRow>();
    public Collection<EntityNameSymbolRow> EntityNameSymbols { get; } = Rows<EntityNameSymbolRow>();
    public Collection<RunRow> Runs { get; } = Rows<RunRow>();
    public Collection<TickSnapshotRow> TickSnapshots { get; } = Rows<TickSnapshotRow>();
    public Collection<KindSnapshotRow> KindSnapshots { get; } = Rows<KindSnapshotRow>();
    public Collection<EntitySnapshotRow> EntitySnapshots { get; } = Rows<EntitySnapshotRow>();
    public Collection<EntityAttributeSnapshotRow> EntityAttributeSnapshots { get; } = Rows<EntityAttributeSnapshotRow>();
    public Collection<EntityEventRow> EntityEvents { get; } = Rows<EntityEventRow>();
    public Collection<TraceRow> TraceEvents { get; } = Rows<TraceRow>();
    public Collection<MovementEventRow> MovementEvents { get; } = Rows<MovementEventRow>();
    public Collection<RelationshipEventRow> RelationshipEvents { get; } = Rows<RelationshipEventRow>();
    public Collection<GambitEventRow> GambitEvents { get; } = Rows<GambitEventRow>();
    public Collection<GeneratorEventRow> GeneratorEvents { get; } = Rows<GeneratorEventRow>();
    public Collection<GeneratorOutputRow> GeneratorOutputs { get; } = Rows<GeneratorOutputRow>();
    public Collection<GeneratedEntityRow> GeneratedEntities { get; } = Rows<GeneratedEntityRow>();
    public Collection<LifecycleEventRow> LifecycleEvents { get; } = Rows<LifecycleEventRow>();
    public Collection<MapGenerationEventRow> MapGenerationEvents { get; } = Rows<MapGenerationEventRow>();
    public Collection<MapCellEventRow> MapCellEvents { get; } = Rows<MapCellEventRow>();
    public Collection<CultureFieldCellSnapshotRow> CultureFieldCellSnapshots { get; } = Rows<CultureFieldCellSnapshotRow>();
    public Collection<MinorActorWorkEventRow> MinorActorWorkEvents { get; } = Rows<MinorActorWorkEventRow>();
    public Collection<CultureEntityBirthFieldEventRow> CultureEntityBirthFieldEvents { get; } = Rows<CultureEntityBirthFieldEventRow>();
    public Collection<CellRoadEventRow> CellRoadEvents { get; } = Rows<CellRoadEventRow>();
    public Collection<MapSnapshotRow> MapSnapshots { get; } = Rows<MapSnapshotRow>();
    public Collection<CoverageSnapshotRow> CoverageSnapshots { get; } = Rows<CoverageSnapshotRow>();
    public Collection<CoverageTerrainSnapshotRow> CoverageTerrainSnapshots { get; } = Rows<CoverageTerrainSnapshotRow>();
    public Collection<PlacementEventRow> PlacementEvents { get; } = Rows<PlacementEventRow>();

    public void ClearRows()
    {
        Symbols.Clear();
        EntityNameSymbols.Clear();
        Runs.Clear();
        TickSnapshots.Clear();
        KindSnapshots.Clear();
        EntitySnapshots.Clear();
        EntityAttributeSnapshots.Clear();
        EntityEvents.Clear();
        TraceEvents.Clear();
        MovementEvents.Clear();
        RelationshipEvents.Clear();
        GambitEvents.Clear();
        GeneratorEvents.Clear();
        GeneratorOutputs.Clear();
        GeneratedEntities.Clear();
        LifecycleEvents.Clear();
        MapGenerationEvents.Clear();
        MapCellEvents.Clear();
        CultureFieldCellSnapshots.Clear();
        MinorActorWorkEvents.Clear();
        CultureEntityBirthFieldEvents.Clear();
        CellRoadEvents.Clear();
        MapSnapshots.Clear();
        CoverageSnapshots.Clear();
        CoverageTerrainSnapshots.Clear();
        PlacementEvents.Clear();
    }

    private static TelemetryRowCollection<T> Rows<T>() => new();
}

public sealed class TelemetryRowCollection<T> : Collection<T>
{
    private readonly List<T> _items;

    public TelemetryRowCollection()
        : this(new List<T>())
    {
    }

    public TelemetryRowCollection(int capacity)
        : this(new List<T>(capacity))
    {
    }

    private TelemetryRowCollection(List<T> items)
        : base(items)
    {
        _items = items;
    }

    public void EnsureAdditionalCapacity(int additionalCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(additionalCount);
        var requiredCapacity = Count + additionalCount;
        if (_items.Capacity < requiredCapacity)
        {
            _items.Capacity = requiredCapacity;
        }
    }
}

public sealed record SymbolRow(
    int SymbolKind,
    int Id,
    string StableName,
    string DisplayName,
    string? Description = null,
    string? IconKey = null,
    string? ColorKey = null);

public sealed record EntityNameSymbolRow(
    int RunId,
    int EntityId,
    int KindId,
    int SubtypeId,
    int CultureId,
    int NameSeed,
    string DisplayName,
    string ReferenceName);

public sealed record RunRow(
    int RunId,
    int? Seed,
    int DomainId,
    long StartedUnixMilliseconds);

public sealed record TickSnapshotRow(
    int RunId,
    int Tick,
    int ActiveEntities,
    int HistoricalEntities,
    int TotalGeneratorFires,
    int ActiveRelationships,
    int CoverageCount,
    int CoveredCells,
    int UncoveredCells,
    float PhysicalSpread,
    float SemanticSpread);

public sealed record KindSnapshotRow(
    int RunId,
    int Tick,
    int KindId,
    int SubtypeId,
    int ActiveCount,
    int HistoricalCount,
    float AverageHealth,
    float MinimumHealth,
    float MaximumHealth);

public sealed record EntitySnapshotRow(
    int RunId,
    int Tick,
    int EntityId,
    int KindId,
    int SubtypeId,
    int CultureId,
    int StatusCode,
    int PhysicalQ,
    int PhysicalR,
    int SemanticQ,
    int SemanticR,
    int SemanticS);

public sealed record EntityAttributeSnapshotRow(
    int RunId,
    int Tick,
    int EntityId,
    int AttributeId,
    float Value);

public sealed record EntityEventRow(
    int RunId,
    int Tick,
    int EventTypeCode,
    int SourceCodeId,
    int EntityId,
    int KindId,
    int SubtypeId,
    int CultureId,
    int? PreviousStatusCode,
    int? CurrentStatusCode,
    int PhysicalQ,
    int PhysicalR,
    int SemanticQ,
    int SemanticR,
    int SemanticS);

public sealed record TraceRow(
    int RunId,
    int Tick,
    int IntentId,
    int EntityId,
    int TraceSubjectCode,
    int CandidateIndex,
    int ParentCandidateIndex,
    int ActionFamilyCode,
    int ActionId,
    int EffectScopeCode,
    int EffectValueId,
    int EffectKindId,
    int TargetEntityId,
    int StatusCode,
    int AffordanceSourceKindCode,
    int AffordanceSourceId,
    int AffordanceShapeCode,
    int AffordanceSourceKindMask,
    int AffordanceShapeMask,
    int AffordanceContributionCount,
    int GeneratorAffordanceCount,
    int GambitAffordanceCount,
    int HiddenAffordanceCount,
    int SelectionBucketCode,
    float Urgency,
    float RewardOrBaseScore,
    float RawWeight,
    float AdjustedWeight,
    float AffordanceScore,
    float AffordanceBonus);

public sealed record MovementEventRow(
    int RunId,
    int Tick,
    int EntityId,
    int PlaneCode,
    int SourceCodeId,
    int FromQ,
    int FromR,
    int FromS,
    int ToQ,
    int ToR,
    int ToS,
    int Distance,
    int FromZ = 0,
    int ToZ = 0);

public sealed record RelationshipEventRow(
    int RunId,
    int Tick,
    int ChangeCodeId,
    int SourceCodeId,
    int RelationshipKindId,
    int PolarityCode,
    int SourceId,
    int TargetId,
    float PreviousStrength,
    float NewStrength,
    int ActionId = 0,
    int CauseId = 0);

public sealed record GambitEventRow(
    int RunId,
    int Tick,
    int EventTypeCode,
    int GambitId,
    int ActorId,
    int ActorKindId,
    int TargetEntityId,
    int StateCode,
    bool Succeeded,
    int AffectedEntityCount);

public sealed record GeneratorEventRow(
    int RunId,
    int Tick,
    int EventTypeCode,
    int GeneratorId,
    int StatusCode,
    int TargetPoolCount,
    int InactiveRejectedCount,
    int SubtypeRejectedCount,
    int SecondaryTargetRejectedCount,
    int TriggerRejectedCount,
    int CandidateCount,
    int FailedFireCount,
    int? SelectedTargetEntityId,
    int CreatedEntityCount,
    int CreatedRelationshipCount,
    int CauseId = 0,
    int CreatedMapCellCount = 0,
    int RelationshipSpendCount = 0,
    int OutputLockRelationshipCreatedCount = 0);

public sealed record GeneratorOutputRow(
    int RunId,
    int Tick,
    int CauseId,
    int GeneratorId,
    int OutputDomainCode,
    int OutputEntityId,
    int OutputRelationshipKindId,
    int OutputSourceEntityId,
    int OutputTargetEntityId,
    int OutputQ,
    int OutputR,
    int OutputS,
    int PlacementEventId);

public sealed record GeneratedEntityRow(
    int RunId,
    int Tick,
    int GeneratorId,
    int EntityId,
    int KindId,
    int SubtypeId,
    int CultureId,
    int PhysicalQ,
    int PhysicalR,
    int SemanticQ,
    int SemanticR,
    int SemanticS);

public sealed record LifecycleEventRow(
    int RunId,
    int Tick,
    int EventTypeCode,
    int EntityId,
    int KindId,
    int StatusCode);

public sealed record MapGenerationEventRow(
    int RunId,
    int Tick,
    int BaseSeed,
    int Attempt,
    int TerrainSeed,
    int EntitySeed,
    int PhaseId,
    int DescriptionId,
    int ReasonId,
    bool WillRetry);

public sealed record MapCellEventRow(
    int RunId,
    int Tick,
    int EventTypeCode,
    int SourceCodeId,
    int PlaneCode,
    int Q,
    int R,
    int SuperregionId,
    int ContinentId,
    int PreviousTerrainId,
    int CurrentTerrainId,
    int PreviousOwnerEntityId,
    int CurrentOwnerEntityId,
    int CauseId = 0,
    int Z = 0);

public sealed record CultureFieldCellSnapshotRow(
    int RunId,
    int Tick,
    int CultureId,
    int PlaneCode,
    int Q,
    int R,
    float Share,
    float RawInfluence,
    float TotalRawInfluence,
    int Z = 0);

public sealed record MinorActorWorkEventRow(
    int RunId,
    int Tick,
    int ActorOccurrence,
    int SourceEntityId,
    int SourceRuleId,
    int SourceWorkTier,
    int EffectiveRadius,
    int CultureId,
    int RoleCode,
    int ActionCode,
    int TargetingCode,
    int PlaneCode,
    int Q,
    int R,
    int CellKindCode,
    float LocalRawMass,
    float ConnectedRawMass,
    float Strength,
    float Delta);

public sealed record CultureEntityBirthFieldEventRow(
    int RunId,
    int Tick,
    int EntityId,
    int CultureId,
    int PhysicalFieldBandCode,
    int PhysicalCellKindCode,
    float PhysicalLocalRawMass,
    float PhysicalConnectedRawMass,
    int SemanticFieldBandCode,
    int SemanticCellKindCode,
    float SemanticLocalRawMass,
    float SemanticConnectedRawMass);

public sealed record CellRoadEventRow(
    int RunId,
    int Tick,
    int SourceCodeId,
    int PlaneCode,
    int Q,
    int R,
    int TravelCount,
    float PreviousQuality,
    float CurrentQuality,
    float Delta);

public sealed record MapSnapshotRow(
    int RunId,
    int Tick,
    int PlaneCode,
    int TerrainId,
    int CellCount,
    int CoveredCellCount,
    int ActiveEntityCount);

public sealed record CoverageSnapshotRow(
    int RunId,
    int Tick,
    int PlaneCode,
    int OwnerEntityId,
    int CoverageKindCode,
    int CellCount,
    int MinQ,
    int MaxQ,
    int MinR,
    int MaxR,
    int ActiveEntityCount);

public sealed record CoverageTerrainSnapshotRow(
    int RunId,
    int Tick,
    int PlaneCode,
    int OwnerEntityId,
    int TerrainId,
    int CellCount);

public sealed record PlacementEventRow(
    int RunId,
    int Tick,
    int CauseId,
    int AnchorEntityId,
    int PlacementModeCode,
    int PlaneCode,
    int Q,
    int R,
    int S,
    int TerrainId,
    int CoverageOwnerEntityId,
    int RejectionReasonCode,
    bool Selected);
