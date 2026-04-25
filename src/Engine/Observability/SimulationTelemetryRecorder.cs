namespace Engine.Observability;

using System.Globalization;

using Engine.Domain.Config;
using Engine.Graph;
using Engine.Map;
using Engine.Messages;
using Engine.Processes;
using Engine.Resources;
using Engine.Simulation;

/// <summary>
/// Structured observer for batch simulation analysis. It subscribes to the
/// existing message bus and samples aggregate state at tick boundaries.
/// </summary>
public sealed class SimulationTelemetryRecorder : IDisposable
{
    private readonly SimulationRunner _sim;
    private readonly IDisposable _subscription;
    private readonly int _entitySnapshotCadence;
    private readonly Dictionary<EntityId, EntityStateSnapshot> _lastEntityState = [];
    private readonly HashSet<(int Tick, int EntityId, string AttributeName)> _attributeEventsSeen = [];
    private readonly HashSet<(int Tick, int EntityId, string Plane)> _movementEventsSeen = [];
    private bool _disposed;

    public string RunId { get; }
    public TelemetryBatch Batch { get; } = new();

    public SimulationTelemetryRecorder(
        SimulationRunner sim,
        string runId,
        int? seed = null,
        string domain = "IceRemembers",
        int entitySnapshotCadence = 25,
        IEnumerable<SimulationMessage>? bootstrapMessages = null)
    {
        ArgumentNullException.ThrowIfNull(sim);
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        _sim = sim;
        RunId = runId;
        _entitySnapshotCadence = entitySnapshotCadence;
        _subscription = sim.MessageBus.Subscribe(OnMessage);

        Batch.Runs.Add(new RunRow(
            runId,
            seed,
            domain,
            DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)));

        if (bootstrapMessages is not null)
        {
            foreach (var message in bootstrapMessages)
            {
                OnMessage(message);
            }
        }

        RecordTickSnapshot();
    }

    public void OnTickEnded()
    {
        if (_disposed) return;
        RecordTickSnapshot();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _subscription.Dispose();
    }

    private void OnMessage(SimulationMessage message)
    {
        switch (message)
        {
            case EntityCreatedMessage created:
                Batch.EntityEvents.Add(new EntityEventRow(
                    RunId,
                    created.Tick,
                    "created",
                    created.EntityId.Value,
                    created.Kind.Value,
                    created.Subtype,
                    created.Name,
                    created.Culture,
                    PreviousStatus: null,
                    CurrentStatus: "Active",
                    created.Physical.Q,
                    created.Physical.R,
                    created.Semantic.Q,
                    created.Semantic.R,
                    created.Semantic.S));
                break;

            case EntityStatusChangedMessage status:
                Batch.EntityEvents.Add(new EntityEventRow(
                    RunId,
                    status.Tick,
                    "status_changed",
                    status.EntityId.Value,
                    status.Kind.Value,
                    status.Subtype,
                    status.Name,
                    CultureOf(status.EntityId),
                    status.PreviousStatus,
                    status.CurrentStatus,
                    PhysicalQOf(status.EntityId),
                    PhysicalROf(status.EntityId),
                    SemanticQOf(status.EntityId),
                    SemanticROf(status.EntityId),
                    SemanticSOf(status.EntityId)));
                break;

            case GoalGenerationMessage goals:
                foreach (var goal in goals.Goals)
                {
                    Batch.PlannerGoals.Add(new PlannerGoalRow(
                        RunId,
                        goals.Tick,
                        goals.EntityId.Value,
                        goals.Kind.Value,
                        GoalKey(goal.GoalId),
                        goal.DesiredEffect.ToString(),
                        goal.TargetEntityId.Value,
                        goal.Urgency,
                        ProcessNameOf(goal.PlannedProcessId),
                        GoalKey(goal.ParentGoalId),
                        goal.IsActionable,
                        goal.Depth));
                }
                break;

            case ProcessEvaluationMessage process:
                Batch.ProcessEvents.Add(new ProcessEventRow(
                    RunId,
                    process.Tick,
                    process.EntityId.Value,
                    process.Kind.Value,
                    GoalKey(process.SelectedGoalId),
                    ProcessNameOf(process.ExecutedProcessId),
                    process.IsStale,
                    process.StatusNote,
                    process.CandidateCount,
                    VisitKey(process.SelectedLocality?.VisitId),
                    LocalityKeyText(process.SelectedLocality?.LocalityKey),
                    PlaneLabel(process.SelectedLocality?.Plane),
                    process.SelectedLocality?.Q,
                    process.SelectedLocality?.R,
                    process.SelectedLocality?.S,
                    process.SelectedLocality?.BundleScore,
                    process.SelectedLocality?.GoalCount,
                    process.SelectedLocality?.DistinctTargetCount,
                    process.SelectedLocality?.DistinctEffectCount,
                    process.SelectedLocality?.Distance,
                    process.SelectedLocality?.IsCurrentLocality,
                    process.SelectedLocality?.IsCommitted,
                    process.SelectedLocality?.Arrived,
                    process.SelectedLocality?.ActionsRemaining,
                    process.SelectedLocality?.RealizedActions,
                    ReasonLabel(process.SelectedLocality?.SelectionReason)));
                foreach (var candidate in process.Candidates)
                {
                    Batch.ProcessCandidates.Add(new ProcessCandidateRow(
                        RunId,
                        process.Tick,
                        process.EntityId.Value,
                        process.Kind.Value,
                        ProcessNameOf(candidate.ProcessId) ?? string.Empty,
                        GoalKey(candidate.GoalId) ?? string.Empty,
                        ProviderNameOf(candidate.ProviderId),
                        candidate.Score,
                        candidate.PreconditionPassed,
                        candidate.PreconditionDiagnostic,
                        candidate.OnCooldown,
                        candidate.WasSelected));
                }
                foreach (var locality in process.LocalityOpportunities)
                {
                    Batch.LocalityOpportunities.Add(new LocalityOpportunityRow(
                        RunId,
                        process.Tick,
                        process.EntityId.Value,
                        process.Kind.Value,
                        VisitKey(process.SelectedLocality?.VisitId),
                        LocalityKeyText(locality.LocalityKey),
                        PlaneLabel(locality.Plane),
                        locality.Q,
                        locality.R,
                        locality.S,
                        locality.AnchorEntityId.Value,
                        locality.AnchorName,
                        locality.BundleScore,
                        locality.GoalCount,
                        locality.DistinctTargetCount,
                        locality.DistinctEffectCount,
                        locality.Distance,
                        locality.IsCurrentLocality,
                        locality.IsCommitted,
                        locality.WasSelected,
                        ReasonLabel(locality.SelectionReason)));
                }
                break;

            case AttributeChangedMessage attribute:
                _attributeEventsSeen.Add((attribute.Tick, attribute.EntityId.Value, attribute.AttributeName));
                Batch.AttributeEvents.Add(new AttributeEventRow(
                    RunId,
                    attribute.Tick,
                    attribute.EntityId.Value,
                    attribute.AttributeName,
                    attribute.OldValue,
                    attribute.NewValue,
                    attribute.NewValue - attribute.OldValue,
                    attribute.Source));
                break;

            case EntityMovedMessage moved:
                _movementEventsSeen.Add((moved.Tick, moved.EntityId.Value, "physical"));
                Batch.MovementEvents.Add(new MovementEventRow(
                    RunId,
                    moved.Tick,
                    moved.EntityId.Value,
                    "physical",
                    moved.From.Q,
                    moved.From.R,
                    null,
                    moved.To.Q,
                    moved.To.R,
                    null,
                    HexCoord.Distance(new HexCoord(moved.From.Q, moved.From.R), new HexCoord(moved.To.Q, moved.To.R))));
                break;

            case EntitySemanticMovedMessage moved:
                _movementEventsSeen.Add((moved.Tick, moved.EntityId.Value, "semantic"));
                Batch.MovementEvents.Add(new MovementEventRow(
                    RunId,
                    moved.Tick,
                    moved.EntityId.Value,
                    "semantic",
                    moved.From.Q,
                    moved.From.R,
                    moved.From.S,
                    moved.To.Q,
                    moved.To.R,
                    moved.To.S,
                    SemanticDistance(
                        new SemanticHexCoord(moved.From.Q, moved.From.R),
                        new SemanticHexCoord(moved.To.Q, moved.To.R))));
                break;

            case ResourceChangedMessage resource:
                Batch.ResourceEvents.Add(new ResourceEventRow(
                    RunId,
                    resource.Tick,
                    resource.ResourceType,
                    resource.Coordinates.Q,
                    resource.Coordinates.R,
                    resource.OldValue,
                    resource.NewValue,
                    resource.NewValue - resource.OldValue,
                    resource.Source));
                break;

            case RelationshipChangedMessage relationship:
                Batch.RelationshipEvents.Add(new RelationshipEventRow(
                    RunId,
                    relationship.Tick,
                    relationship.ChangeType,
                    relationship.RelationshipKind,
                    relationship.Polarity,
                    relationship.SourceId.Value,
                    relationship.TargetId.Value,
                    relationship.PreviousStrength,
                    relationship.NewStrength,
                    string.Join('|', relationship.Facts.OrderBy(value => value, StringComparer.Ordinal))));
                break;

            case EntityDiedMessage death:
                Batch.LifecycleEvents.Add(new LifecycleEventRow(
                    RunId,
                    death.Tick,
                    "death",
                    death.EntityId.Value,
                    death.Kind.Value,
                    death.Name));
                break;

            case MapGenerationDegenerateMessage mapDegenerate:
                Batch.MapGenerationEvents.Add(new MapGenerationEventRow(
                    RunId,
                    mapDegenerate.Tick,
                    mapDegenerate.BaseSeed,
                    mapDegenerate.Attempt,
                    mapDegenerate.TerrainSeed,
                    mapDegenerate.EntitySeed,
                    mapDegenerate.Phase,
                    mapDegenerate.Description,
                    string.Join('|', mapDegenerate.Reasons),
                    mapDegenerate.WillRetry));
                break;

            case GeneratorPulseMessage pulse:
                Batch.GeneratorEvents.Add(new GeneratorEventRow(
                    RunId,
                    pulse.Tick,
                    "pulse",
                    pulse.GeneratorId,
                    pulse.Status,
                    pulse.TargetPoolCount,
                    pulse.InactiveRejectedCount,
                    pulse.SubtypeRejectedCount,
                    pulse.SaturationRejectedCount,
                    pulse.SecondaryTargetRejectedCount,
                    pulse.WildernessRejectedCount,
                    pulse.TriggerRejectedCount,
                    pulse.CandidateCount,
                    pulse.FailedFireCount,
                    pulse.SelectedTargetEntityId?.Value,
                    CreatedEntityCount: 0,
                    CreatedRelationshipCount: 0));
                break;

            case GeneratorFiredMessage fired:
                Batch.GeneratorEvents.Add(new GeneratorEventRow(
                    RunId,
                    fired.Tick,
                    "fired",
                    fired.GeneratorId,
                    "fired",
                    TargetPoolCount: 0,
                    InactiveRejectedCount: 0,
                    SubtypeRejectedCount: 0,
                    SaturationRejectedCount: 0,
                    SecondaryTargetRejectedCount: 0,
                    WildernessRejectedCount: 0,
                    TriggerRejectedCount: 0,
                    CandidateCount: 0,
                    FailedFireCount: 0,
                    fired.TargetEntityId?.Value,
                    fired.CreatedEntityIds.Count,
                    fired.CreatedRelationshipCount));
                foreach (var entityId in fired.CreatedEntityIds)
                {
                    var entity = _sim.Graph.GetEntity(entityId);
                    if (entity is null) continue;
                    var sem = entity.SemanticPosition;
                    Batch.GeneratedEntities.Add(new GeneratedEntityRow(
                        RunId,
                        fired.Tick,
                        fired.GeneratorId,
                        entity.Id.Value,
                        entity.Kind.Value,
                        entity.Subtype,
                        entity.Culture,
                        entity.Coordinates.Q,
                        entity.Coordinates.R,
                        sem.Q,
                        sem.R,
                        sem.S));
                }
                break;
        }
    }

    private string? ProcessNameOf(ProcessRuntimeId? processId)
    {
        if (processId is null || processId.Value.IsNone)
        {
            return null;
        }

        return _sim.BehaviorDefinitions.TryGetDefinition(processId.Value, out var definition) &&
               definition is not null
            ? definition.Name
            : processId.Value.Value.ToString(CultureInfo.InvariantCulture);
    }

    private static string GoalKey(GoalRuntimeId goalId) =>
        FormattableString.Invariant($"goal_{goalId.Value}");

    private static string? GoalKey(GoalRuntimeId? goalId) =>
        goalId is null ? null : GoalKey(goalId.Value);

    private string ProviderNameOf(EntityId providerId)
    {
        var entity = _sim.WorldState.QueryCache.GetEntity(providerId);
        return entity?.Name ?? providerId.Value.ToString(CultureInfo.InvariantCulture);
    }

    private static string? VisitKey(LocalityVisitId? visitId) =>
        visitId is null || visitId.Value.IsNone
            ? null
            : FormattableString.Invariant($"visit_{visitId.Value.Value}");

    private static string? LocalityKeyText(LocalityRuntimeKey? key) =>
        key is null ? null : LocalityKeyText(key.Value);

    private static string LocalityKeyText(LocalityRuntimeKey key) =>
        key.Plane == PrimaryPlane.Physical
            ? FormattableString.Invariant($"physical:{key.Q}:{key.R}")
            : FormattableString.Invariant($"semantic:{key.Q}:{key.R}:{key.S}");

    private static string? PlaneLabel(PrimaryPlane? plane) =>
        plane is null ? null : PlaneLabel(plane.Value);

    private static string PlaneLabel(PrimaryPlane plane) =>
        plane == PrimaryPlane.Physical ? "physical" : "semantic";

    private static string? ReasonLabel(LocalitySelectionReason? reason) =>
        reason is null ? null : ReasonLabel(reason.Value);

    private static string ReasonLabel(LocalitySelectionReason reason) =>
        reason switch
        {
            LocalitySelectionReason.BestBundle => "best_bundle",
            LocalitySelectionReason.KeptCommitment => "kept_commitment",
            LocalitySelectionReason.ContinuedCommitment => "continued_commitment",
            LocalitySelectionReason.SwitchedCommitment => "switched_commitment",
            _ => string.Empty,
        };

    private void RecordTickSnapshot()
    {
        var tick = _sim.Clock.CurrentTick;
        var entities = _sim.Graph.AllEntities.Values.ToList();
        var active = entities.Where(entity => entity.Status == EntityStatus.Active).ToList();
        var historicalCount = entities.Count(entity => entity.Status == EntityStatus.Historical);
        var activeRelationships = ActiveRelationships().ToList();
        var coveredCells = _sim.CoverageMap.AllCoverages.Sum(coverage => coverage.CellCount);

        Batch.TickSnapshots.Add(new TickSnapshotRow(
            RunId,
            tick,
            active.Count,
            historicalCount,
            _sim.GeneratorSystem.TotalFired,
            activeRelationships.Count,
            _sim.CoverageMap.AllCoverages.Count,
            coveredCells,
            _sim.Grid.CellCount - coveredCells,
            ComputePhysicalSpread(active),
            ComputeSemanticSpread(active)));

        RecordSnapshotDerivedEvents(tick, entities);
        RecordKindSnapshots(tick, entities);
        RecordRelationshipSnapshots(tick, activeRelationships);
        RecordResourceSnapshots(tick);
        RecordMapSnapshots(tick, active);

        if (_entitySnapshotCadence > 0 && tick % _entitySnapshotCadence == 0)
        {
            RecordEntitySnapshots(tick, entities);
        }

        UpdateEntityStateSnapshots(entities);
        _attributeEventsSeen.RemoveWhere(entry => entry.Tick < tick);
        _movementEventsSeen.RemoveWhere(entry => entry.Tick < tick);
    }

    private void RecordSnapshotDerivedEvents(int tick, IReadOnlyCollection<Entity> entities)
    {
        foreach (var entity in entities)
        {
            if (!_lastEntityState.TryGetValue(entity.Id, out var previous))
            {
                continue;
            }

            foreach (var attributeName in entity.Attributes.Names.OrderBy(name => name, StringComparer.Ordinal))
            {
                var current = entity.Attributes.GetValue(attributeName);
                var hadPrevious = previous.Attributes.TryGetValue(attributeName, out var prior);
                if (!hadPrevious || Math.Abs(current - prior) < float.Epsilon)
                {
                    continue;
                }

                var key = (tick, entity.Id.Value, attributeName);
                if (_attributeEventsSeen.Contains(key))
                {
                    continue;
                }

                Batch.AttributeEvents.Add(new AttributeEventRow(
                    RunId,
                    tick,
                    entity.Id.Value,
                    attributeName,
                    prior,
                    current,
                    current - prior,
                    "tick_snapshot"));
            }

            if (previous.Semantic != entity.SemanticPosition)
            {
                if (_movementEventsSeen.Contains((tick, entity.Id.Value, "semantic")))
                {
                    continue;
                }

                Batch.MovementEvents.Add(new MovementEventRow(
                    RunId,
                    tick,
                    entity.Id.Value,
                    "semantic",
                    previous.Semantic.Q,
                    previous.Semantic.R,
                    previous.Semantic.S,
                    entity.SemanticPosition.Q,
                    entity.SemanticPosition.R,
                    entity.SemanticPosition.S,
                    SemanticDistance(previous.Semantic, entity.SemanticPosition)));
            }
        }
    }

    private void RecordKindSnapshots(int tick, IReadOnlyCollection<Entity> entities)
    {
        foreach (var group in entities.GroupBy(entity => (entity.Kind, entity.Subtype)))
        {
            var active = group.Where(entity => entity.Status == EntityStatus.Active).ToList();
            var healthAttribute = _sim.DomainConfig.Kinds.TryGetValue(group.Key.Kind, out var kindConfig)
                ? kindConfig.HealthAttribute
                : null;
            var healthValues = healthAttribute is null
                ? []
                : active
                    .Select(entity => entity.Attributes.GetScalar(healthAttribute)?.Value)
                    .Where(value => value is not null)
                    .Select(value => value!.Value)
                    .ToList();

            Batch.KindSnapshots.Add(new KindSnapshotRow(
                RunId,
                tick,
                group.Key.Kind.Value,
                group.Key.Subtype,
                active.Count,
                group.Count(entity => entity.Status == EntityStatus.Historical),
                healthValues.Count == 0 ? 0f : healthValues.Average(),
                healthValues.Count == 0 ? 0f : healthValues.Min(),
                healthValues.Count == 0 ? 0f : healthValues.Max()));
        }
    }

    private void RecordEntitySnapshots(int tick, IReadOnlyCollection<Entity> entities)
    {
        foreach (var entity in entities)
        {
            var sem = entity.SemanticPosition;
            var attributes = string.Join(
                '|',
                entity.Attributes.Names
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .Select(name => FormattableString.Invariant($"{name}={entity.Attributes.GetValue(name):F3}")));
            Batch.EntitySnapshots.Add(new EntitySnapshotRow(
                RunId,
                tick,
                entity.Id.Value,
                entity.Kind.Value,
                entity.Subtype,
                entity.Culture,
                entity.Status.ToString(),
                entity.Coordinates.Q,
                entity.Coordinates.R,
                sem.Q,
                sem.R,
                sem.S,
                attributes));
        }
    }

    private void RecordRelationshipSnapshots(int tick, IReadOnlyCollection<Relationship> relationships)
    {
        foreach (var group in relationships.GroupBy(relationship => relationship.Kind))
        {
            var strengths = group.Select(relationship => relationship.Strength).ToList();
            Batch.RelationshipSnapshots.Add(new RelationshipSnapshotRow(
                RunId,
                tick,
                group.Key,
                strengths.Count,
                strengths.Count == 0 ? 0f : strengths.Average(),
                strengths.Count == 0 ? 0f : strengths.Min(),
                strengths.Count == 0 ? 0f : strengths.Max()));
        }
    }

    private void RecordResourceSnapshots(int tick)
    {
        if (_sim.WorldState.Resources is null) return;
        RecordResourceSnapshot(tick, ResourceType.Food, _sim.WorldState.Resources.Food);
        RecordResourceSnapshot(tick, ResourceType.Wealth, _sim.WorldState.Resources.Wealth);
        RecordResourceSnapshot(tick, ResourceType.Population, _sim.WorldState.Resources.PopulationLayer);
    }

    private void RecordResourceSnapshot(int tick, ResourceType resourceType, ResourceLayer layer)
    {
        var values = _sim.Grid.AllCells
            .Select(cell => layer.GetStockpile(cell.Coord))
            .ToList();
        Batch.ResourceSnapshots.Add(new ResourceSnapshotRow(
            RunId,
            tick,
            resourceType.ToString(),
            values.Sum(),
            values.Count == 0 ? 0f : values.Average(),
            values.Count == 0 ? 0f : values.Min(),
            values.Count == 0 ? 0f : values.Max()));
    }

    private void RecordMapSnapshots(int tick, IReadOnlyCollection<Entity> activeEntities)
    {
        var activeByCoord = activeEntities
            .GroupBy(entity => new HexCoord(entity.Coordinates.Q, entity.Coordinates.R))
            .ToDictionary(group => group.Key, group => group.Count());

        foreach (var group in _sim.Grid.AllCells.GroupBy(cell => cell.Terrain))
        {
            var cells = group.ToList();
            Batch.MapSnapshots.Add(new MapSnapshotRow(
                RunId,
                tick,
                group.Key.ToString(),
                cells.Count,
                cells.Count(cell => _sim.CoverageMap.GetCoverageForCell(cell.Coord) is not null),
                cells.Sum(cell => activeByCoord.GetValueOrDefault(cell.Coord))));
        }
    }

    private IEnumerable<Relationship> ActiveRelationships()
    {
        foreach (var (entityId, _) in _sim.Graph.AllEntities)
        {
            foreach (var relationship in _sim.Graph.OutgoingRelationships(entityId))
            {
                if (relationship.IsActive)
                {
                    yield return relationship;
                }
            }
        }
    }

    private static float ComputePhysicalSpread(List<Entity> activeEntities)
    {
        var max = 0;
        for (var i = 0; i < activeEntities.Count; i++)
        {
            var a = new HexCoord(activeEntities[i].Coordinates.Q, activeEntities[i].Coordinates.R);
            for (var j = i + 1; j < activeEntities.Count; j++)
            {
                var b = new HexCoord(activeEntities[j].Coordinates.Q, activeEntities[j].Coordinates.R);
                max = Math.Max(max, HexCoord.Distance(a, b));
            }
        }

        return max;
    }

    private static float ComputeSemanticSpread(List<Entity> activeEntities)
    {
        var max = 0;
        for (var i = 0; i < activeEntities.Count; i++)
        {
            var a = activeEntities[i].SemanticPosition;
            for (var j = i + 1; j < activeEntities.Count; j++)
            {
                var b = activeEntities[j].SemanticPosition;
                var distance = SemanticDistance(a, b);
                max = Math.Max(max, distance);
            }
        }

        return max;
    }

    private void UpdateEntityStateSnapshots(IEnumerable<Entity> entities)
    {
        foreach (var entity in entities)
        {
            var attributes = entity.Attributes.Names.ToDictionary(
                name => name,
                name => entity.Attributes.GetValue(name),
                StringComparer.Ordinal);
            _lastEntityState[entity.Id] = new EntityStateSnapshot(
                attributes,
                entity.Coordinates,
                entity.SemanticPosition);
        }
    }

    private static int SemanticDistance(SemanticHexCoord a, SemanticHexCoord b) =>
        (Math.Abs(a.Q - b.Q) + Math.Abs(a.R - b.R) + Math.Abs(a.S - b.S)) / 2;

    private string CultureOf(EntityId entityId) => _sim.Graph.GetEntity(entityId)?.Culture ?? string.Empty;
    private int PhysicalQOf(EntityId entityId) => _sim.Graph.GetEntity(entityId)?.Coordinates.Q ?? 0;
    private int PhysicalROf(EntityId entityId) => _sim.Graph.GetEntity(entityId)?.Coordinates.R ?? 0;
    private int SemanticQOf(EntityId entityId) => _sim.Graph.GetEntity(entityId)?.SemanticPosition.Q ?? 0;
    private int SemanticROf(EntityId entityId) => _sim.Graph.GetEntity(entityId)?.SemanticPosition.R ?? 0;
    private int SemanticSOf(EntityId entityId) => _sim.Graph.GetEntity(entityId)?.SemanticPosition.S ?? 0;

    private sealed record EntityStateSnapshot(
        IReadOnlyDictionary<string, float> Attributes,
        (int Q, int R) Physical,
        SemanticHexCoord Semantic);
}
