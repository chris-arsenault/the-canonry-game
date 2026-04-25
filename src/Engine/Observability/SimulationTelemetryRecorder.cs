namespace Engine.Observability;

using System.Globalization;

using Engine.Domain;
using Engine.Domain.Config;
using Engine.Eventing;
using Engine.Graph;
using Engine.Map;
using Engine.Processes;
using Engine.Resources;
using Engine.Simulation;

/// <summary>
/// Structured observer for batch simulation analysis. It consumes primitive
/// kernel events and samples aggregate state at tick boundaries.
/// </summary>
public sealed class SimulationTelemetryRecorder : IDisposable
{
    private readonly SimulationRunner _sim;
    private readonly KernelEventCursor _kernelCursor;
    private readonly SequencedKernelEvent[] _kernelEventBuffer = new SequencedKernelEvent[512];
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
        int entitySnapshotCadence = 25)
    {
        ArgumentNullException.ThrowIfNull(sim);
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        _sim = sim;
        RunId = runId;
        _entitySnapshotCadence = entitySnapshotCadence;
        _kernelCursor = sim.KernelEvents.CreateCursor(KernelEventCursorStart.EarliestAvailable);

        Batch.Runs.Add(new RunRow(
            runId,
            seed,
            domain,
            DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)));

        DrainKernelEvents(sim.Clock.CurrentTick);
        RecordTickSnapshot();
    }

    public void OnTickEnded()
    {
        if (_disposed) return;
        DrainKernelEvents(_sim.Clock.CurrentTick);
        RecordTickSnapshot();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _kernelCursor.Dispose();
    }

    private void DrainKernelEvents(int tick)
    {
        int read;
        do
        {
            read = _sim.KernelEvents.Read(_kernelCursor, _kernelEventBuffer);
            for (var index = 0; index < read; index++)
            {
                var kernelEvent = _kernelEventBuffer[index].Event;
                if (kernelEvent.Tick <= tick)
                {
                    ProjectKernelEvent(kernelEvent);
                }
            }
        } while (read == _kernelEventBuffer.Length);
    }

    private void ProjectKernelEvent(KernelEvent kernelEvent)
    {
        switch (kernelEvent.EventType)
        {
            case KernelEventType.EntityCreated:
                ProjectEntityCreated(kernelEvent.AsEntityCreated());
                break;
            case KernelEventType.EntityStatusChanged:
                ProjectEntityStatusChanged(kernelEvent.AsEntityStatusChanged());
                break;
            case KernelEventType.EntityMoved:
                ProjectEntityMoved(kernelEvent.AsEntityMoved());
                break;
            case KernelEventType.AttributeChanged:
                ProjectAttributeChanged(kernelEvent.AsAttributeChanged());
                break;
            case KernelEventType.ResourceChanged:
                ProjectResourceChanged(kernelEvent.AsResourceChanged());
                break;
            case KernelEventType.RelationshipChanged:
                ProjectRelationshipChanged(kernelEvent.AsRelationshipChanged());
                break;
            case KernelEventType.ProcessEvaluated:
                ProjectProcessEvaluated(kernelEvent.AsProcessEvaluated());
                break;
            case KernelEventType.GoalGenerated:
                ProjectGoalGenerated(kernelEvent.AsGoalGenerated());
                break;
            case KernelEventType.GeneratorEvaluated:
                ProjectGeneratorEvaluated(kernelEvent.AsGeneratorEvaluated());
                break;
            case KernelEventType.GeneratorFired:
                ProjectGeneratorFired(kernelEvent.AsGeneratorFired());
                break;
        }
    }

    private void ProjectEntityCreated(EntityCreatedKernelEvent created)
    {
        var entity = RequiredEntity(created.EntityId);
        Batch.EntityEvents.Add(new EntityEventRow(
            RunId,
            created.Tick,
            "created",
            created.EntityId,
            entity.Kind.Value,
            _sim.DomainIds.SubtypeNameOf(new SubtypeRuntimeId(created.SubtypeId)),
            entity.Name,
            _sim.DomainIds.CultureNameOf(new CultureRuntimeId(created.CultureId)),
            PreviousStatus: null,
            CurrentStatus: EntityStatus.Active.ToString(),
            created.PhysicalQ,
            created.PhysicalR,
            created.SemanticQ,
            created.SemanticR,
            created.SemanticS));
    }

    private void ProjectEntityStatusChanged(EntityStatusChangedKernelEvent status)
    {
        var entity = RequiredEntity(status.EntityId);
        var previousStatus = (EntityStatus)status.PreviousStatusCode;
        var currentStatus = (EntityStatus)status.CurrentStatusCode;
        var sem = entity.SemanticPosition;
        Batch.EntityEvents.Add(new EntityEventRow(
            RunId,
            status.Tick,
            "status_changed",
            status.EntityId,
            entity.Kind.Value,
            _sim.DomainIds.SubtypeNameOf(new SubtypeRuntimeId(status.SubtypeId)),
            entity.Name,
            entity.Culture,
            previousStatus.ToString(),
            currentStatus.ToString(),
            entity.Coordinates.Q,
            entity.Coordinates.R,
            sem.Q,
            sem.R,
            sem.S));

        if (currentStatus == EntityStatus.Historical)
        {
            Batch.LifecycleEvents.Add(new LifecycleEventRow(
                RunId,
                status.Tick,
                "death",
                status.EntityId,
                entity.Kind.Value,
                entity.Name));
        }
    }

    private void ProjectEntityMoved(EntityMovedKernelEvent moved)
    {
        var plane = moved.PlaneCode == (int)PrimaryPlane.Physical ? "physical" : "semantic";
        _movementEventsSeen.Add((moved.Tick, moved.EntityId, plane));
        var distance = moved.PlaneCode == (int)PrimaryPlane.Physical
            ? HexCoord.Distance(new HexCoord(moved.FromQ, moved.FromR), new HexCoord(moved.ToQ, moved.ToR))
            : SemanticDistance(
                new SemanticHexCoord(moved.FromQ, moved.FromR),
                new SemanticHexCoord(moved.ToQ, moved.ToR));
        Batch.MovementEvents.Add(new MovementEventRow(
            RunId,
            moved.Tick,
            moved.EntityId,
            plane,
            moved.FromQ,
            moved.FromR,
            moved.PlaneCode == (int)PrimaryPlane.Physical ? null : moved.FromS,
            moved.ToQ,
            moved.ToR,
            moved.PlaneCode == (int)PrimaryPlane.Physical ? null : moved.ToS,
            distance));
    }

    private void ProjectAttributeChanged(AttributeChangedKernelEvent attribute)
    {
        var attributeName = _sim.DomainIds.AttributeNameOf(new AttributeRuntimeId(attribute.AttributeId));
        var source = _sim.DomainIds.SourceCodeNameOf(new SourceCodeId(attribute.SourceCodeId));
        _attributeEventsSeen.Add((attribute.Tick, attribute.EntityId, attributeName));
        Batch.AttributeEvents.Add(new AttributeEventRow(
            RunId,
            attribute.Tick,
            attribute.EntityId,
            attributeName,
            attribute.OldValue,
            attribute.NewValue,
            attribute.NewValue - attribute.OldValue,
            source));
    }

    private void ProjectResourceChanged(ResourceChangedKernelEvent resource)
    {
        var resourceName = _sim.DomainIds.ResourceNameOf(new ResourceRuntimeId(resource.ResourceId));
        var source = _sim.DomainIds.SourceCodeNameOf(new SourceCodeId(resource.SourceCodeId));
        Batch.ResourceEvents.Add(new ResourceEventRow(
            RunId,
            resource.Tick,
            resourceName,
            resource.Q,
            resource.R,
            resource.OldValue,
            resource.NewValue,
            resource.NewValue - resource.OldValue,
            source));
    }

    private void ProjectRelationshipChanged(RelationshipChangedKernelEvent relationship)
    {
        Batch.RelationshipEvents.Add(new RelationshipEventRow(
            RunId,
            relationship.Tick,
            _sim.DomainIds.EventCodeNameOf(new EventCodeId(relationship.ChangeCodeId)),
            _sim.DomainIds.RelationshipKindNameOf(new RelationshipKindRuntimeId(relationship.RelationshipKindId)),
            ((RelationshipPolarity)relationship.PolarityCode).ToString(),
            relationship.SourceEntityId,
            relationship.TargetEntityId,
            relationship.PreviousStrength,
            relationship.NewStrength,
            RelationshipFacts(relationship)));
    }

    private void ProjectGoalGenerated(GoalGeneratedKernelEvent goals)
    {
        var entity = RequiredEntity(goals.EntityId);
        foreach (var goal in entity.Goals)
        {
            Batch.PlannerGoals.Add(new PlannerGoalRow(
                RunId,
                goals.Tick,
                goals.EntityId,
                entity.Kind.Value,
                GoalKey(goal.Id),
                goal.DesiredEffect.ToString(),
                goal.TargetEntityId.Value,
                goal.Urgency,
                ProcessNameOf(goal.PlannedProcessId),
                GoalKey(goal.ParentGoalId),
                goal.IsActionable,
                goal.Depth));
        }
    }

    private void ProjectProcessEvaluated(ProcessEvaluatedKernelEvent process)
    {
        var entity = RequiredEntity(process.EntityId);
        if (entity.LastEvaluation is { } report && report.Tick == process.Tick)
        {
            ProjectProcessReport(process.Tick, entity, report);
            return;
        }

        var selectedGoal = process.SelectedGoalId > 0 ? new GoalRuntimeId(process.SelectedGoalId) : (GoalRuntimeId?)null;
        var executedProcess = process.ExecutedProcessId > 0
            ? new ProcessRuntimeId(process.ExecutedProcessId)
            : (ProcessRuntimeId?)null;
        Batch.ProcessEvents.Add(new ProcessEventRow(
            RunId,
            process.Tick,
            process.EntityId,
            entity.Kind.Value,
            GoalKey(selectedGoal),
            ProcessNameOf(executedProcess),
            process.State.HasFlag(KernelEventState.Stale),
            StatusNote: null,
            process.CandidateCount,
            VisitId: null,
            LocalityKey: null,
            LocalityPlane: null,
            LocalityQ: null,
            LocalityR: null,
            LocalityS: null,
            LocalityBundleScore: null,
            LocalityGoalCount: null,
            LocalityDistinctTargetCount: null,
            LocalityDistinctEffectCount: null,
            LocalityDistance: null,
            LocalityIsCurrent: null,
            LocalityIsCommitted: null,
            LocalityArrived: null,
            LocalityActionsRemaining: null,
            LocalityRealizedActions: null,
            LocalitySelectionReason: null));
    }

    private void ProjectProcessReport(int tick, Entity entity, EvaluationReport report)
    {
        Batch.ProcessEvents.Add(new ProcessEventRow(
            RunId,
            tick,
            entity.Id.Value,
            entity.Kind.Value,
            GoalKey(report.SelectedGoalId),
            ProcessNameOf(report.ExecutedProcessId),
            report.IsStale,
            report.StatusNote,
            report.CandidateCount,
            VisitKey(report.SelectedLocality?.VisitId),
            LocalityKeyText(report.SelectedLocality?.LocalityKey),
            PlaneLabel(report.SelectedLocality?.Plane),
            report.SelectedLocality?.Q,
            report.SelectedLocality?.R,
            report.SelectedLocality?.S,
            report.SelectedLocality?.BundleScore,
            report.SelectedLocality?.GoalCount,
            report.SelectedLocality?.DistinctTargetCount,
            report.SelectedLocality?.DistinctEffectCount,
            report.SelectedLocality?.Distance,
            report.SelectedLocality?.IsCurrentLocality,
            report.SelectedLocality?.IsCommitted,
            report.SelectedLocality?.Arrived,
            report.SelectedLocality?.ActionsRemaining,
            report.SelectedLocality?.RealizedActions,
            ReasonLabel(report.SelectedLocality?.SelectionReason)));
        foreach (var candidate in report.Candidates)
        {
            Batch.ProcessCandidates.Add(new ProcessCandidateRow(
                RunId,
                tick,
                entity.Id.Value,
                entity.Kind.Value,
                ProcessNameOf(candidate.ProcessId) ?? string.Empty,
                GoalKey(candidate.GoalId) ?? string.Empty,
                ProviderNameOf(candidate.ProviderId),
                candidate.Score,
                candidate.PreconditionPassed,
                candidate.PreconditionDiagnostic,
                candidate.OnCooldown,
                candidate.WasSelected));
        }
        foreach (var locality in report.LocalityOpportunities)
        {
            Batch.LocalityOpportunities.Add(new LocalityOpportunityRow(
                RunId,
                tick,
                entity.Id.Value,
                entity.Kind.Value,
                VisitKey(report.SelectedLocality?.VisitId),
                LocalityKeyText(locality.LocalityKey),
                PlaneLabel(locality.Plane),
                locality.Q,
                locality.R,
                locality.S,
                locality.AnchorEntityId,
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
    }

    private void ProjectGeneratorEvaluated(GeneratorEvaluatedKernelEvent generator)
    {
        Batch.GeneratorEvents.Add(new GeneratorEventRow(
            RunId,
            generator.Tick,
            "pulse",
            GeneratorNameOf(generator.GeneratorId),
            GeneratorStatusName(generator.StatusCode),
            TargetPoolCount: 0,
            InactiveRejectedCount: 0,
            SubtypeRejectedCount: 0,
            SaturationRejectedCount: 0,
            SecondaryTargetRejectedCount: 0,
            WildernessRejectedCount: 0,
            TriggerRejectedCount: 0,
            generator.CandidateCount,
            generator.FailedFireCount,
            generator.SelectedTargetEntityId == 0 ? null : generator.SelectedTargetEntityId,
            CreatedEntityCount: 0,
            CreatedRelationshipCount: 0));
    }

    private void ProjectGeneratorFired(GeneratorFiredKernelEvent generator)
    {
        var generatorName = GeneratorNameOf(generator.GeneratorId);
        Batch.GeneratorEvents.Add(new GeneratorEventRow(
            RunId,
            generator.Tick,
            "fired",
            generatorName,
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
            generator.TargetEntityId == 0 ? null : generator.TargetEntityId,
            generator.CreatedEntityCount,
            generator.CreatedRelationshipCount));
        RecordGeneratedEntities(generator.Tick, generatorName, generator.CreatedEntityCount);
    }

    private void RecordGeneratedEntities(int tick, string generatorName, int expectedCount)
    {
        if (expectedCount == 0)
        {
            return;
        }

        var rows = 0;
        foreach (var entity in _sim.Graph.AllEntities.Values.Where(entity => entity.CreatedAt == tick))
        {
            var sem = entity.SemanticPosition;
            Batch.GeneratedEntities.Add(new GeneratedEntityRow(
                RunId,
                tick,
                generatorName,
                entity.Id.Value,
                entity.Kind.Value,
                entity.Subtype,
                entity.Culture,
                entity.Coordinates.Q,
                entity.Coordinates.R,
                sem.Q,
                sem.R,
                sem.S));
            rows++;
        }

        if (rows < expectedCount)
        {
            throw new InvalidOperationException(
                $"Generator '{generatorName}' reported {expectedCount} created entities at tick {tick}, but projection found {rows}.");
        }
    }

    private Entity RequiredEntity(int entityId) =>
        _sim.Graph.GetEntity(new EntityId(entityId))
        ?? throw new InvalidOperationException(
            $"Kernel event referenced missing entity {entityId}.");

    private string RelationshipFacts(RelationshipChangedKernelEvent relationship)
    {
        if (relationship.FactCount == 0)
        {
            return string.Empty;
        }

        var facts = new List<string>(relationship.FactCount);
        if (relationship.FactCount > 0)
        {
            facts.Add(_sim.DomainIds.RelationshipFactNameOf(new RelationshipFactRuntimeId(relationship.FactId0)));
        }

        if (relationship.FactCount > 1)
        {
            facts.Add(_sim.DomainIds.RelationshipFactNameOf(new RelationshipFactRuntimeId(relationship.FactId1)));
        }

        if (relationship.FactCount > 2)
        {
            facts.Add(_sim.DomainIds.RelationshipFactNameOf(new RelationshipFactRuntimeId(relationship.FactId2)));
        }

        if (relationship.FactCount > 3)
        {
            facts.Add(_sim.DomainIds.RelationshipFactNameOf(new RelationshipFactRuntimeId(relationship.FactId3)));
        }

        facts.Sort(StringComparer.Ordinal);
        return string.Join('|', facts);
    }

    private string GeneratorNameOf(int generatorId) =>
        _sim.DomainIds.GeneratorNameOf(new GeneratorRuntimeId(generatorId));

    private static string GeneratorStatusName(int statusCode) => statusCode switch
    {
        1 => "eligible",
        2 => "fired",
        3 => "eligible_not_selected",
        4 => "blocked",
        5 => "fire_failed",
        6 => "missing_definition",
        7 => "max_runs",
        8 => "zero_weight",
        9 => "not_evaluated",
        _ => throw new InvalidOperationException(
            $"Unknown primitive generator status code {statusCode}."),
    };

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
