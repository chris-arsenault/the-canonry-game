namespace Engine.Messages;

using Engine.Cards;
using Engine.Domain.Config;
using Engine.Graph;
using Engine.Kinds;
using Engine.Processes;
using Engine.Wiring;

/// <summary>A process was executed by an entity.</summary>
public sealed class ProcessExecutedMessage : SimulationMessage
{
    public EntityId ActorId { get; }
    public EntityKind ActorKind { get; }
    public ProcessRuntimeId ProcessId { get; }
    public IReadOnlyList<EntityId> AffectedEntities { get; }

    public ProcessExecutedMessage(int tick, EntityId actorId, EntityKind actorKind,
        ProcessRuntimeId processId, IReadOnlyList<EntityId>? affectedEntities = null)
        : base(tick)
    {
        ActorId = actorId;
        ActorKind = actorKind;
        ProcessId = processId;
        AffectedEntities = affectedEntities ?? [];
    }
}

/// <summary>A card-gated decision was executed by an entity.</summary>
public sealed class DecisionExecutedMessage : SimulationMessage
{
    public EntityId ActorId { get; }
    public EntityKind ActorKind { get; }
    public ProcessRuntimeId DecisionId { get; }
    public CardRuntimeId? CardId { get; }
    public bool Succeeded { get; }
    public IReadOnlyList<EntityId> AffectedEntities { get; }

    public DecisionExecutedMessage(int tick, EntityId actorId, EntityKind actorKind,
        ProcessRuntimeId decisionId, CardRuntimeId? cardId = null,
        bool succeeded = true, IReadOnlyList<EntityId>? affectedEntities = null)
        : base(tick)
    {
        ActorId = actorId;
        ActorKind = actorKind;
        DecisionId = decisionId;
        CardId = cardId;
        Succeeded = succeeded;
        AffectedEntities = affectedEntities ?? [];
    }
}

/// <summary>An entity's attribute changed significantly.</summary>
public sealed class AttributeChangedMessage : SimulationMessage
{
    public EntityId EntityId { get; }
    public string AttributeName { get; }
    public float OldValue { get; }
    public float NewValue { get; }
    public string Source { get; }

    public AttributeChangedMessage(int tick, EntityId entityId, string attributeName,
        float oldValue, float newValue, string source)
        : base(tick)
    {
        EntityId = entityId;
        AttributeName = attributeName;
        OldValue = oldValue;
        NewValue = newValue;
        Source = source;
    }
}

/// <summary>An entity moved to a new location.</summary>
public sealed class EntityMovedMessage : SimulationMessage
{
    public EntityId EntityId { get; }
    public (int Q, int R) From { get; }
    public (int Q, int R) To { get; }

    public EntityMovedMessage(int tick, EntityId entityId, (int Q, int R) from, (int Q, int R) to)
        : base(tick)
    {
        EntityId = entityId;
        From = from;
        To = to;
    }
}

/// <summary>An entity moved on the semantic plane.</summary>
public sealed class EntitySemanticMovedMessage : SimulationMessage
{
    public EntityId EntityId { get; }
    public (int Q, int R, int S) From { get; }
    public (int Q, int R, int S) To { get; }

    public EntitySemanticMovedMessage(
        int tick,
        EntityId entityId,
        (int Q, int R, int S) from,
        (int Q, int R, int S) to)
        : base(tick)
    {
        EntityId = entityId;
        From = from;
        To = to;
    }
}

/// <summary>An entity was created in the world graph.</summary>
public sealed class EntityCreatedMessage : SimulationMessage
{
    public EntityId EntityId { get; }
    public EntityKind Kind { get; }
    public string Subtype { get; }
    public string Name { get; }
    public string Culture { get; }
    public (int Q, int R) Physical { get; }
    public (int Q, int R, int S) Semantic { get; }

    public EntityCreatedMessage(
        int tick,
        EntityId entityId,
        EntityKind kind,
        string subtype,
        string name,
        string culture,
        (int Q, int R) physical,
        (int Q, int R, int S) semantic)
        : base(tick)
    {
        EntityId = entityId;
        Kind = kind;
        Subtype = subtype;
        Name = name;
        Culture = culture;
        Physical = physical;
        Semantic = semantic;
    }
}

/// <summary>An entity changed active/historical state.</summary>
public sealed class EntityStatusChangedMessage : SimulationMessage
{
    public EntityId EntityId { get; }
    public EntityKind Kind { get; }
    public string Subtype { get; }
    public string Name { get; }
    public string PreviousStatus { get; }
    public string CurrentStatus { get; }

    public EntityStatusChangedMessage(
        int tick,
        EntityId entityId,
        EntityKind kind,
        string subtype,
        string name,
        string previousStatus,
        string currentStatus)
        : base(tick)
    {
        EntityId = entityId;
        Kind = kind;
        Subtype = subtype;
        Name = name;
        PreviousStatus = previousStatus;
        CurrentStatus = currentStatus;
    }
}

/// <summary>An entity's subtype changed.</summary>
public sealed class SubtypeChangedMessage : SimulationMessage
{
    public EntityId EntityId { get; }
    public string OldSubtype { get; }
    public string NewSubtype { get; }

    public SubtypeChangedMessage(int tick, EntityId entityId, string oldSubtype, string newSubtype)
        : base(tick)
    {
        EntityId = entityId;
        OldSubtype = oldSubtype;
        NewSubtype = newSubtype;
    }
}

/// <summary>A resource stockpile changed at a map coordinate.</summary>
public sealed class ResourceChangedMessage : SimulationMessage
{
    public string ResourceType { get; }
    public (int Q, int R) Coordinates { get; }
    public float OldValue { get; }
    public float NewValue { get; }
    public string Source { get; }

    public ResourceChangedMessage(
        int tick,
        string resourceType,
        (int Q, int R) coordinates,
        float oldValue,
        float newValue,
        string source)
        : base(tick)
    {
        ResourceType = resourceType;
        Coordinates = coordinates;
        OldValue = oldValue;
        NewValue = newValue;
        Source = source;
    }
}

/// <summary>A relationship was created, culled, or changed.</summary>
public sealed class RelationshipChangedMessage : SimulationMessage
{
    public string ChangeType { get; }
    public string RelationshipKind { get; }
    public string Polarity { get; }
    public EntityId SourceId { get; }
    public EntityId TargetId { get; }
    public float PreviousStrength { get; }
    public float NewStrength { get; }
    public IReadOnlyList<string> Facts { get; }

    public RelationshipChangedMessage(
        int tick,
        string changeType,
        string relationshipKind,
        string polarity,
        EntityId sourceId,
        EntityId targetId,
        float previousStrength,
        float newStrength,
        IReadOnlyList<string>? facts = null)
        : base(tick)
    {
        ChangeType = changeType;
        RelationshipKind = relationshipKind;
        Polarity = polarity;
        SourceId = sourceId;
        TargetId = targetId;
        PreviousStrength = previousStrength;
        NewStrength = newStrength;
        Facts = facts ?? [];
    }
}

/// <summary>An entity died (transitioned to historical).</summary>
public sealed class EntityDiedMessage : SimulationMessage
{
    public EntityId EntityId { get; }
    public EntityKind Kind { get; }
    public string Name { get; }

    public EntityDiedMessage(int tick, EntityId entityId, EntityKind kind, string name)
        : base(tick)
    {
        EntityId = entityId;
        Kind = kind;
        Name = name;
    }
}

/// <summary>Planner goals were regenerated for an entity.</summary>
public sealed class GoalGenerationMessage : SimulationMessage
{
    public EntityId EntityId { get; }
    public EntityKind Kind { get; }
    public int GoalCount { get; }
    public int ActionableGoalCount { get; }
    public IReadOnlyList<GoalSnapshot> Goals { get; }

    public GoalGenerationMessage(
        int tick,
        EntityId entityId,
        EntityKind kind,
        int goalCount,
        int actionableGoalCount,
        IReadOnlyList<GoalSnapshot>? goals = null)
        : base(tick)
    {
        EntityId = entityId;
        Kind = kind;
        GoalCount = goalCount;
        ActionableGoalCount = actionableGoalCount;
        Goals = goals ?? [];
    }
}

public sealed record GoalSnapshot(
    GoalRuntimeId GoalId,
    AttributeRef DesiredEffect,
    EntityId TargetEntityId,
    float Urgency,
    ProcessRuntimeId? PlannedProcessId,
    GoalRuntimeId? ParentGoalId,
    bool IsActionable,
    int Depth);

/// <summary>Process/planner evaluation result for one entity on one tick.</summary>
public sealed class ProcessEvaluationMessage : SimulationMessage
{
    public EntityId EntityId { get; }
    public EntityKind Kind { get; }
    public GoalRuntimeId? SelectedGoalId { get; }
    public ProcessRuntimeId? ExecutedProcessId { get; }
    public bool IsStale { get; }
    public string? StatusNote { get; }
    public int CandidateCount { get; }
    public IReadOnlyList<ProcessCandidateSnapshot> Candidates { get; }
    public LocalitySelectionSnapshot? SelectedLocality { get; }
    public IReadOnlyList<LocalityOpportunitySnapshot> LocalityOpportunities { get; }

    public ProcessEvaluationMessage(
        int tick,
        EntityId entityId,
        EntityKind kind,
        GoalRuntimeId? selectedGoalId,
        ProcessRuntimeId? executedProcessId,
        bool isStale,
        string? statusNote,
        IReadOnlyList<ProcessCandidateSnapshot>? candidates = null,
        LocalitySelectionSnapshot? selectedLocality = null,
        IReadOnlyList<LocalityOpportunitySnapshot>? localityOpportunities = null,
        int? candidateCount = null)
        : base(tick)
    {
        EntityId = entityId;
        Kind = kind;
        SelectedGoalId = selectedGoalId;
        ExecutedProcessId = executedProcessId;
        IsStale = isStale;
        StatusNote = statusNote;
        Candidates = candidates ?? [];
        SelectedLocality = selectedLocality;
        LocalityOpportunities = localityOpportunities ?? [];
        CandidateCount = candidateCount ?? Candidates.Count;
    }
}

public sealed record ProcessCandidateSnapshot(
    ProcessRuntimeId ProcessId,
    GoalRuntimeId? GoalId,
    EntityId ProviderId,
    float Score,
    bool PreconditionPassed,
    string PreconditionDiagnostic,
    bool OnCooldown,
    bool WasSelected);

public sealed record LocalitySelectionSnapshot(
    LocalityVisitId VisitId,
    LocalityRuntimeKey LocalityKey,
    PrimaryPlane Plane,
    int Q,
    int R,
    int? S,
    EntityId AnchorEntityId,
    string AnchorName,
    float BundleScore,
    int GoalCount,
    int DistinctTargetCount,
    int DistinctEffectCount,
    int Distance,
    bool IsCurrentLocality,
    bool IsCommitted,
    bool Arrived,
    int ActionsRemaining,
    int RealizedActions,
    LocalitySelectionReason SelectionReason);

public sealed record LocalityOpportunitySnapshot(
    LocalityRuntimeKey LocalityKey,
    PrimaryPlane Plane,
    int Q,
    int R,
    int? S,
    EntityId AnchorEntityId,
    string AnchorName,
    float BundleScore,
    int GoalCount,
    int DistinctTargetCount,
    int DistinctEffectCount,
    int Distance,
    bool IsCurrentLocality,
    bool IsCommitted,
    bool WasSelected,
    LocalitySelectionReason SelectionReason);

/// <summary>A degeneracy warning was triggered or resolved.</summary>
public sealed class DegeneracyWarningMessage : SimulationMessage
{
    public string DetectorName { get; }
    public Degeneracy.Severity Severity { get; }
    public string Description { get; }

    public DegeneracyWarningMessage(int tick, string detectorName,
        Degeneracy.Severity severity, string description)
        : base(tick)
    {
        DetectorName = detectorName;
        Severity = severity;
        Description = description;
    }
}

/// <summary>A procedural map-generation attempt was rejected as degenerate.</summary>
public sealed class MapGenerationDegenerateMessage : SimulationMessage
{
    public int BaseSeed { get; }
    public int Attempt { get; }
    public int TerrainSeed { get; }
    public int EntitySeed { get; }
    public string Phase { get; }
    public string Description { get; }
    public IReadOnlyList<string> Reasons { get; }
    public bool WillRetry { get; }

    public MapGenerationDegenerateMessage(
        int tick,
        int baseSeed,
        int attempt,
        int terrainSeed,
        int entitySeed,
        string phase,
        string description,
        IReadOnlyList<string>? reasons,
        bool willRetry)
        : base(tick)
    {
        BaseSeed = baseSeed;
        Attempt = attempt;
        TerrainSeed = terrainSeed;
        EntitySeed = entitySeed;
        Phase = phase;
        Description = description;
        Reasons = reasons ?? [];
        WillRetry = willRetry;
    }
}

/// <summary>A generator was evaluated during a pulse, whether or not it fired.</summary>
public sealed class GeneratorPulseMessage : SimulationMessage
{
    public string GeneratorId { get; }
    public string Status { get; }
    public int TargetPoolCount { get; }
    public int InactiveRejectedCount { get; }
    public int SubtypeRejectedCount { get; }
    public int SaturationRejectedCount { get; }
    public int SecondaryTargetRejectedCount { get; }
    public int WildernessRejectedCount { get; }
    public int TriggerRejectedCount { get; }
    public int CandidateCount { get; }
    public int FailedFireCount { get; }
    public EntityId? SelectedTargetEntityId { get; }

    public GeneratorPulseMessage(
        int tick,
        string generatorId,
        string status,
        int targetPoolCount,
        int inactiveRejectedCount,
        int subtypeRejectedCount,
        int saturationRejectedCount,
        int secondaryTargetRejectedCount,
        int wildernessRejectedCount,
        int triggerRejectedCount,
        int candidateCount,
        int failedFireCount,
        EntityId? selectedTargetEntityId)
        : base(tick)
    {
        GeneratorId = generatorId;
        Status = status;
        TargetPoolCount = targetPoolCount;
        InactiveRejectedCount = inactiveRejectedCount;
        SubtypeRejectedCount = subtypeRejectedCount;
        SaturationRejectedCount = saturationRejectedCount;
        SecondaryTargetRejectedCount = secondaryTargetRejectedCount;
        WildernessRejectedCount = wildernessRejectedCount;
        TriggerRejectedCount = triggerRejectedCount;
        CandidateCount = candidateCount;
        FailedFireCount = failedFireCount;
        SelectedTargetEntityId = selectedTargetEntityId;
    }
}

/// <summary>A generator fired and created a package of entities/relationships.</summary>
public sealed class GeneratorFiredMessage : SimulationMessage
{
    public string GeneratorId { get; }
    public EntityId? TargetEntityId { get; }
    public IReadOnlyList<EntityId> CreatedEntityIds { get; }
    public int CreatedRelationshipCount { get; }

    public GeneratorFiredMessage(
        int tick,
        string generatorId,
        EntityId? targetEntityId,
        IReadOnlyList<EntityId>? createdEntityIds,
        int createdRelationshipCount)
        : base(tick)
    {
        GeneratorId = generatorId;
        TargetEntityId = targetEntityId;
        CreatedEntityIds = createdEntityIds ?? [];
        CreatedRelationshipCount = createdRelationshipCount;
    }
}
