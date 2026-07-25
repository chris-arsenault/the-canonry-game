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
    public Collection<KnowledgeSiteSymbolRow> KnowledgeSiteSymbols { get; } = Rows<KnowledgeSiteSymbolRow>();
    public Collection<RunRow> Runs { get; } = Rows<RunRow>();
    public Collection<TickSnapshotRow> TickSnapshots { get; } = Rows<TickSnapshotRow>();
    public Collection<KindSnapshotRow> KindSnapshots { get; } = Rows<KindSnapshotRow>();
    public Collection<EntitySnapshotRow> EntitySnapshots { get; } = Rows<EntitySnapshotRow>();
    public Collection<EntityAttributeSnapshotRow> EntityAttributeSnapshots { get; } = Rows<EntityAttributeSnapshotRow>();
    public Collection<EntityEventRow> EntityEvents { get; } = Rows<EntityEventRow>();
    public Collection<ExperimentVariantRow> ExperimentVariants { get; } = Rows<ExperimentVariantRow>();
    public Collection<TraceRow> TraceEvents { get; } = Rows<TraceRow>();
    public Collection<AttributeEventRow> AttributeEvents { get; } = Rows<AttributeEventRow>();
    public Collection<MovementEventRow> MovementEvents { get; } = Rows<MovementEventRow>();
    public Collection<RelationshipEventRow> RelationshipEvents { get; } = Rows<RelationshipEventRow>();
    public Collection<RelationshipSnapshotRow> RelationshipSnapshots { get; } = Rows<RelationshipSnapshotRow>();
    public Collection<GambitEventRow> GambitEvents { get; } = Rows<GambitEventRow>();
    public Collection<GeneratorEventRow> GeneratorEvents { get; } = Rows<GeneratorEventRow>();
    public Collection<GeneratorTargetEvaluationRow> GeneratorTargetEvaluations { get; } = Rows<GeneratorTargetEvaluationRow>();
    public Collection<GeneratorOutputRow> GeneratorOutputs { get; } = Rows<GeneratorOutputRow>();
    public Collection<GeneratorStateSnapshotRow> GeneratorStateSnapshots { get; } = Rows<GeneratorStateSnapshotRow>();
    public Collection<GeneratedEntityRow> GeneratedEntities { get; } = Rows<GeneratedEntityRow>();
    public Collection<LifecycleEventRow> LifecycleEvents { get; } = Rows<LifecycleEventRow>();
    public Collection<LifecycleReasonEventRow> LifecycleReasonEvents { get; } = Rows<LifecycleReasonEventRow>();
    public Collection<MapGenerationEventRow> MapGenerationEvents { get; } = Rows<MapGenerationEventRow>();
    public Collection<MapCellEventRow> MapCellEvents { get; } = Rows<MapCellEventRow>();
    public Collection<CellInfluenceEventRow> CellInfluenceEvents { get; } = Rows<CellInfluenceEventRow>();
    public Collection<CultureFieldCellSnapshotRow> CultureFieldCellSnapshots { get; } = Rows<CultureFieldCellSnapshotRow>();
    public Collection<CultureSlotOccupancyEventRow> CultureSlotOccupancyEvents { get; } = Rows<CultureSlotOccupancyEventRow>();
    public Collection<MinorActorVerbEventRow> MinorActorVerbEvents { get; } = Rows<MinorActorVerbEventRow>();
    public Collection<CultureFieldComponentSnapshotRow> CultureFieldComponentSnapshots { get; } = Rows<CultureFieldComponentSnapshotRow>();
    public Collection<CulturePowerSnapshotRow> CulturePowerSnapshots { get; } = Rows<CulturePowerSnapshotRow>();
    public Collection<KnowledgeSnapshotRow> KnowledgeSnapshots { get; } = Rows<KnowledgeSnapshotRow>();
    public Collection<KnowledgeClaimSnapshotRow> KnowledgeClaimSnapshots { get; } = Rows<KnowledgeClaimSnapshotRow>();
    public Collection<EntityTagEventRow> EntityTagEvents { get; } = Rows<EntityTagEventRow>();
    public Collection<ChronicleEventRow> ChronicleEvents { get; } = Rows<ChronicleEventRow>();

    public Collection<WorldBeatEventRow> WorldBeatEvents { get; } = Rows<WorldBeatEventRow>();
    public Collection<CultureEntityFieldSnapshotRow> CultureEntityFieldSnapshots { get; } = Rows<CultureEntityFieldSnapshotRow>();
    public Collection<CultureEntityBirthFieldEventRow> CultureEntityBirthFieldEvents { get; } = Rows<CultureEntityBirthFieldEventRow>();
    public Collection<CultureInfluenceActionFieldEventRow> CultureInfluenceActionFieldEvents { get; } = Rows<CultureInfluenceActionFieldEventRow>();
    public Collection<CellRoadEventRow> CellRoadEvents { get; } = Rows<CellRoadEventRow>();
    public Collection<MapSnapshotRow> MapSnapshots { get; } = Rows<MapSnapshotRow>();
    public Collection<CoverageSnapshotRow> CoverageSnapshots { get; } = Rows<CoverageSnapshotRow>();
    public Collection<CoverageTerrainSnapshotRow> CoverageTerrainSnapshots { get; } = Rows<CoverageTerrainSnapshotRow>();
    public Collection<PlacementEventRow> PlacementEvents { get; } = Rows<PlacementEventRow>();

    public void ClearRows()
    {
        Symbols.Clear();
        EntityNameSymbols.Clear();
        KnowledgeSiteSymbols.Clear();
        Runs.Clear();
        TickSnapshots.Clear();
        KindSnapshots.Clear();
        EntitySnapshots.Clear();
        EntityAttributeSnapshots.Clear();
        EntityEvents.Clear();
        ExperimentVariants.Clear();
        TraceEvents.Clear();
        AttributeEvents.Clear();
        MovementEvents.Clear();
        RelationshipEvents.Clear();
        RelationshipSnapshots.Clear();
        GambitEvents.Clear();
        GeneratorEvents.Clear();
        GeneratorTargetEvaluations.Clear();
        GeneratorOutputs.Clear();
        GeneratorStateSnapshots.Clear();
        GeneratedEntities.Clear();
        LifecycleEvents.Clear();
        LifecycleReasonEvents.Clear();
        MapGenerationEvents.Clear();
        MapCellEvents.Clear();
        CellInfluenceEvents.Clear();
        CultureFieldCellSnapshots.Clear();
        CultureSlotOccupancyEvents.Clear();
        MinorActorVerbEvents.Clear();
        ClearCultureAndNarrativeRows();
    }

    private void ClearCultureAndNarrativeRows()
    {
        CultureFieldComponentSnapshots.Clear();
        CulturePowerSnapshots.Clear();
        KnowledgeSnapshots.Clear();
        KnowledgeClaimSnapshots.Clear();
        EntityTagEvents.Clear();
        ChronicleEvents.Clear();
        WorldBeatEvents.Clear();
        CultureEntityFieldSnapshots.Clear();
        CultureEntityBirthFieldEvents.Clear();
        CultureInfluenceActionFieldEvents.Clear();
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

public sealed record KnowledgeSiteSymbolRow(
    int RunId,
    int SiteIndex,
    string StableName,
    int Tier);

public sealed record RunRow(
    int RunId,
    int? Seed,
    int DomainId,
    long StartedUnixMilliseconds);

public sealed record ExperimentVariantRow(
    int RunId,
    int ExperimentId,
    int VariantId,
    int ConfigHashId,
    int GitShaId,
    bool TraceEnabled,
    int EntitySnapshotCadence,
    int MapSnapshotCadence,
    int GeneratorSnapshotCadence);

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

public sealed record AttributeEventRow(
    int RunId,
    int Tick,
    int EntityId,
    int AttributeId,
    float OldValue,
    float NewValue,
    float Delta,
    int SourceCodeId);

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

public sealed record RelationshipSnapshotRow(
    int RunId,
    int Tick,
    int RelationshipKindId,
    int ActiveCount,
    float AverageStrength,
    float MinimumStrength,
    float MaximumStrength);

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

public sealed record GeneratorTargetEvaluationRow(
    int RunId,
    int Tick,
    int GeneratorId,
    int TargetEntityId,
    int TargetKindId,
    int TargetSubtypeId,
    int TargetCultureId,
    int PhysicalQ,
    int PhysicalR,
    int SemanticQ,
    int SemanticR,
    int SemanticS,
    int StatusCode,
    int RejectionReasonCode,
    int PhysicalFieldBandCode,
    int PhysicalCellKindCode,
    float PhysicalLocalRawMass,
    float PhysicalConnectedRawMass,
    int SemanticFieldBandCode,
    int SemanticCellKindCode,
    float SemanticLocalRawMass,
    float SemanticConnectedRawMass);

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

public sealed record GeneratorStateSnapshotRow(
    int RunId,
    int Tick,
    int GeneratorId,
    bool Enabled,
    int Cadence,
    int CooldownRemaining,
    int RunCount,
    int LastRunTick);

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

public sealed record LifecycleReasonEventRow(
    int RunId,
    int Tick,
    int EntityId,
    int ReasonCodeId,
    int CauseId,
    int SourceCodeId);

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

public sealed record CellInfluenceEventRow(
    int RunId,
    int Tick,
    int SourceCodeId,
    int ActionId,
    int EntityId,
    int PlaneCode,
    int Q,
    int R,
    float PreviousInfluence,
    float CurrentInfluence,
    float Delta);

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

public sealed record CultureSlotOccupancyEventRow(
    int RunId,
    int Tick,
    int SourceCodeId,
    int CultureId,
    int SlotId,
    int PolarityCode,
    int PreviousEntityId,
    int CurrentEntityId,
    float PreviousScore,
    float CurrentScore);

public sealed record MinorActorVerbEventRow(
    int RunId,
    int Tick,
    int ActorId,
    int SourceEntityId,
    int SourceRuleId,
    int SourceWorkTier,
    int EffectiveRadius,
    int CultureId,
    int VerbId,
    int PurposeCode,
    int ActionCode,
    int TargetingCode,
    int PlaneCode,
    int Q,
    int R,
    int StatusCode,
    int CellKindCode,
    float LocalRawMass,
    float ConnectedRawMass,
    float Score,
    float Delta);

public sealed record WorldBeatEventRow(
    int RunId,
    int Tick,
    int TrackCode,
    int AgeId,
    int PhaseCode);

public sealed record ChronicleEventRow(
    int RunId,
    int Tick,
    int ChronicleSequence,
    int ArchetypeId,
    int EventTypeCode,
    int ActIndex,
    int OutcomeCodeId,
    int RoleEntityId0,
    int RoleEntityId1,
    int RoleEntityId2,
    int RoleEntityId3);

public sealed record EntityTagEventRow(
    int RunId,
    int Tick,
    int EntityId,
    int TagId,
    bool Assigned,
    int SourceCodeId);

public sealed record CulturePowerSnapshotRow(
    int RunId,
    int Tick,
    int CultureId,
    float WitnessShare,
    float ShadowShare,
    float WakeShare,
    float WitnessMass,
    float HiddenMass,
    float SemanticMass,
    float CustodyStrength,
    float ClaimStrength,
    float WarStrength,
    float OfferingStrength);

/// <summary>
/// Per-culture knowledge state at a snapshot tick: how many of the authored
/// technologies the culture has claimed (its idea-mass holds the tech's hex) and
/// the highest tier it has reached.
/// </summary>
public sealed record KnowledgeSnapshotRow(
    int RunId,
    int Tick,
    int CultureId,
    int ClaimedTechs,
    int MaxTier);

public sealed record KnowledgeClaimSnapshotRow(
    int RunId,
    int Tick,
    int CultureId,
    int SiteIndex,
    int Tier);

public sealed record CultureFieldComponentSnapshotRow(
    int RunId,
    int Tick,
    int PlaneCode,
    int CultureId,
    int InfluencedCellCount,
    int ComponentCount,
    int LargestComponentCellCount,
    float LargestComponentShare,
    int InteriorCellCount,
    int BorderCellCount,
    int IsolatedCellCount,
    int ContestedCellCount,
    int HoleCellCount = 0);

public sealed record CultureEntityFieldSnapshotRow(
    int RunId,
    int Tick,
    int EntityId,
    int CultureId,
    int PlaneCode,
    int CellKindCode,
    bool SlotOccupied,
    float LocalRawMass,
    float ConnectedRawMass,
    float InfluenceProduction,
    int StrengthBandCode = 0,
    float RawMass = 0f,
    float TotalRawMass = 0f,
    int ForeignCultureId = 0,
    float ForeignShare = 0f,
    float ForeignRawMass = 0f);

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

public sealed record CultureInfluenceActionFieldEventRow(
    int RunId,
    int Tick,
    int EntityId,
    int CultureId,
    int ActionId,
    int PlaneCode,
    int CellKindCode,
    bool SlotOccupied,
    float LocalRawMass,
    float ConnectedRawMass,
    float PreviousInfluence,
    float CurrentInfluence,
    float Delta);

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
