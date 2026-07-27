namespace Engine.Projection;

using Engine.Constants;
using Engine.Eventing;
using Engine.Kinds;

public sealed partial class UiProjectionState : KernelEventProjector
{
    private const int MinorActorWorkCapacity = 320;

    private readonly Dictionary<int, UiEntityProjection> _entities = [];
    private readonly Dictionary<UiRelationshipKey, UiRelationshipProjection> _relationships = [];
    private readonly Dictionary<int, HashSet<UiRelationshipKey>> _activeOutgoingRelationshipsByEntity = [];
    private readonly Dictionary<int, HashSet<UiRelationshipKey>> _activeIncomingRelationshipsByEntity = [];
    private readonly Dictionary<int, Dictionary<HexCoord, HashSet<int>>> _activeOccupantsByPlane = [];
    private readonly Dictionary<int, HashSet<HexCoord>> _dirtyEntityHexesByPlane = [];
    private readonly Dictionary<int, UiGeneratorProjection> _generators = [];
    private readonly List<UiGeneratorFireProjection> _generatorFires = [];
    private readonly Dictionary<int, List<int>> _createdEntitiesByInvocation = [];
    private readonly Dictionary<int, UiGeneratorFireProjection> _generatorFiresByInvocation = [];
    private readonly Dictionary<int, UiGambitProjection> _gambits = [];
    private IReadOnlyDictionary<UiCultureSlotKey, UiCultureSlotDefinitionProjection> _cultureSlotDefinitions =
        new Dictionary<UiCultureSlotKey, UiCultureSlotDefinitionProjection>();
    private readonly Dictionary<UiCultureSlotKey, UiCultureSlotOccupancyProjection> _cultureSlots = [];
    private readonly Dictionary<int, UiMinorActorProjection> _minorActors = [];
    private readonly List<UiMinorActorWorkProjection> _minorActorWork = [];
    private readonly HashSet<int> _dirtyEntityBrowserRows = [];
    private int _currentProvenanceInvocationId;

    private IReadOnlyDictionary<int, EntityTier> _tierByKindId = new Dictionary<int, EntityTier>();
    private Dictionary<int, UiHealthAttributeDefinitionProjection> _healthAttributeByKindId = [];
    private IReadOnlySet<int> _affiliationRelationshipKindIds = new HashSet<int>();

    public IReadOnlyDictionary<int, UiEntityProjection> Entities => _entities;

    public IReadOnlyDictionary<UiRelationshipKey, UiRelationshipProjection> Relationships => _relationships;

    public IReadOnlyDictionary<int, UiGeneratorProjection> Generators => _generators;

    public IReadOnlyList<UiGeneratorFireProjection> GeneratorFires => _generatorFires;

    public IReadOnlyDictionary<int, UiGambitProjection> Gambits => _gambits;

    public IReadOnlyDictionary<UiCultureSlotKey, UiCultureSlotDefinitionProjection> CultureSlotDefinitions =>
        _cultureSlotDefinitions;

    public IReadOnlyDictionary<UiCultureSlotKey, UiCultureSlotOccupancyProjection> CultureSlots => _cultureSlots;

    public IReadOnlyDictionary<int, UiHealthAttributeDefinitionProjection> HealthAttributesByKindId =>
        _healthAttributeByKindId;

    public IReadOnlyDictionary<int, UiMinorActorProjection> MinorActors => _minorActors;

    public IReadOnlyList<UiMinorActorWorkProjection> MinorActorWork => _minorActorWork;

    public long LastProjectedSequence { get; private set; } = -1;

    public int CurrentTick { get; private set; }

    public long WorldVersion { get; private set; }

    public long PublishedPhaseRevision { get; private set; }

    public int ActiveEntityCount { get; private set; }

    public long TopBarVersion { get; private set; }

    /// <summary>Open age id per world-beat track (0 = blank track).</summary>
    public IReadOnlyList<int> WorldBeatAgeIds => _worldBeatAgeIds;

    /// <summary>Tick each track's current age opened (0 = blank track).</summary>
    public IReadOnlyList<int> WorldBeatOpenedTicks => _worldBeatOpenedTicks;

    /// <summary>Recent age transitions, newest last, for the ages timeline.</summary>
    public IReadOnlyList<UiWorldBeatTransition> WorldBeatTransitions => _worldBeatTransitions;

    public long WorldBeatVersion { get; private set; }

    private const int WorldBeatTransitionCapacity = 64;
    private readonly int[] _worldBeatAgeIds = new int[2];
    private readonly int[] _worldBeatOpenedTicks = new int[2];
    private readonly List<UiWorldBeatTransition> _worldBeatTransitions = [];

    public long EntityMapVersion { get; private set; }

    public long EntityBrowserVersion { get; private set; }

    public long EntityBrowserStructureVersion { get; private set; }

    public long EntityBrowserRowVersion { get; private set; }

    public long EntityDetailVersion { get; private set; }

    public long RelationshipVersion { get; private set; }

    public long TableauVersion { get; private set; }

    public long MinorActorVersion { get; private set; }

    public long DroppedEvents { get; private set; }

    protected override void OnProjectingEvent(in SequencedKernelEvent kernelEvent)
    {
        _currentProvenanceInvocationId = kernelEvent.Event.ProvenanceInvocationId;
    }

    public bool IsComplete => DroppedEvents == 0;

    public IReadOnlyCollection<int> ActiveOccupants(int planeCode, HexCoord coord) =>
        TryGetOccupants(planeCode, coord, out var occupants)
            ? occupants
            : [];

    public IReadOnlyCollection<HexCoord> DirtyEntityHexes(int planeCode) =>
        _dirtyEntityHexesByPlane.TryGetValue(planeCode, out var dirtyHexes)
            ? dirtyHexes
            : [];

    public IReadOnlyCollection<int> DirtyEntityBrowserRows => _dirtyEntityBrowserRows;

    public void ClearDirtyEntityHexes(int planeCode)
    {
        if (_dirtyEntityHexesByPlane.TryGetValue(planeCode, out var dirtyHexes))
        {
            dirtyHexes.Clear();
        }
    }

    public void ClearDirtyEntityBrowserRows() => _dirtyEntityBrowserRows.Clear();

    public IReadOnlyList<UiRelationshipProjection> ActiveOutgoingRelationships(int entityId) =>
        ActiveRelationships(_activeOutgoingRelationshipsByEntity, entityId);

    public IReadOnlyList<UiRelationshipProjection> ActiveIncomingRelationships(int entityId) =>
        ActiveRelationships(_activeIncomingRelationshipsByEntity, entityId);

    /// <summary>
    /// Register the domain's per-kind tier map and the set of relationship-kind
    /// runtime ids that represent affiliation edges. Called once after domain
    /// compilation. Downstream UI uses <see cref="EntityTierOf"/> and
    /// <see cref="IsAffiliationRelationship"/> to read these tags.
    /// </summary>
    public void ConfigureHierarchyTags(
        IReadOnlyDictionary<int, EntityTier> tierByKindId,
        IReadOnlySet<int> affiliationRelationshipKindIds)
    {
        ArgumentNullException.ThrowIfNull(tierByKindId);
        ArgumentNullException.ThrowIfNull(affiliationRelationshipKindIds);
        _tierByKindId = tierByKindId;
        _affiliationRelationshipKindIds = affiliationRelationshipKindIds;
    }

    /// <summary>
    /// Tier classification for the given entity. <see cref="EntityTier.None"/> when
    /// the entity is unknown or its kind has no tier assigned.
    /// </summary>
    public EntityTier EntityTierOf(int entityId) =>
        _entities.TryGetValue(entityId, out var entity) &&
        _tierByKindId.TryGetValue(entity.KindId, out var tier)
            ? tier
            : EntityTier.None;

    /// <summary>
    /// True when the given relationship-kind runtime id was registered as an
    /// affiliation edge through <see cref="ConfigureHierarchyTags"/>.
    /// </summary>
    public bool IsAffiliationRelationship(int relationshipKindId) =>
        _affiliationRelationshipKindIds.Contains(relationshipKindId);

    public void ConfigureCultureSlots(IEnumerable<UiCultureSlotDefinitionProjection> slots)
    {
        ArgumentNullException.ThrowIfNull(slots);
        _cultureSlotDefinitions = slots.ToDictionary(
            slot => new UiCultureSlotKey(slot.CultureId, slot.SlotId));
    }

    public void ConfigureHealthAttributes(IEnumerable<UiHealthAttributeDefinitionProjection> healthAttributes)
    {
        ArgumentNullException.ThrowIfNull(healthAttributes);
        _healthAttributeByKindId = healthAttributes.ToDictionary(attribute => attribute.KindId);
    }

    public float? HealthValueOf(UiEntityProjection entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (!_healthAttributeByKindId.TryGetValue(entity.KindId, out var health))
        {
            return null;
        }

        return entity.Attributes.TryGetValue(health.AttributeId, out var value)
            ? value.NewValue
            : health.DefaultValue;
    }

    public float? HealthRatioOf(UiEntityProjection entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (!_healthAttributeByKindId.TryGetValue(entity.KindId, out var health))
        {
            return null;
        }

        var range = health.Maximum - health.Minimum;
        if (range <= 0f)
        {
            return null;
        }

        var value = HealthValueOf(entity);
        return value is null ? null : Math.Clamp((value.Value - health.Minimum) / range, 0f, 1f);
    }

    public override void OnEventsDropped(long droppedEventCount, long nextSequence)
    {
        DroppedEvents += droppedEventCount;
        LastProjectedSequence = Math.Max(LastProjectedSequence, nextSequence - 1);
        _createdEntitiesByInvocation.Clear();
        _generatorFiresByInvocation.Clear();
    }

    protected override void OnWorldBeatChanged(long sequence, in WorldBeatChangedKernelEvent evt)
    {
        if (evt.TrackCode < 0 || evt.TrackCode >= _worldBeatAgeIds.Length)
        {
            return;
        }

        var opened = evt.PhaseCode == 1;
        _worldBeatAgeIds[evt.TrackCode] = opened ? evt.AgeId : 0;
        _worldBeatOpenedTicks[evt.TrackCode] = opened ? evt.Tick : 0;
        _worldBeatTransitions.Add(new UiWorldBeatTransition(evt.Tick, evt.TrackCode, evt.AgeId, opened));
        if (_worldBeatTransitions.Count > WorldBeatTransitionCapacity)
        {
            _worldBeatTransitions.RemoveAt(0);
        }

        WorldBeatVersion++;
        TopBarVersion++;
    }

    protected override void OnEntityCreated(long sequence, in EntityCreatedKernelEvent evt)
    {
        var entity = GetOrCreateEntity(evt.EntityId);
        entity.KindId = evt.KindId;
        entity.SubtypeId = evt.SubtypeId;
        entity.CultureId = evt.CultureId;
        entity.NameSeed = evt.NameSeed;
        entity.StatusCode = EntityStatusCodes.Active;
        entity.PhysicalQ = evt.PhysicalQ;
        entity.PhysicalR = evt.PhysicalR;
        entity.SemanticQ = evt.SemanticQ;
        entity.SemanticR = evt.SemanticR;
        entity.SemanticS = evt.SemanticS;
        entity.CreatedTick = evt.Tick;
        entity.LastUpdatedTick = evt.Tick;
        entity.WorldVersion = evt.WorldVersion;
        entity.WasCreatedFromEvent = true;
        AddActiveOccupants(entity);
        ActiveEntityCount++;
        TopBarVersion++;
        EntityMapVersion++;
        MarkEntityBrowserStructureChanged(evt.EntityId);
        EntityDetailVersion++;
        ReindexRelationshipsForEntity(evt.EntityId);
        TrackCreatedEntityForGeneratorAttribution(evt);
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnEntityCultureChanged(long sequence, in EntityCultureChangedKernelEvent evt)
    {
        if (!_entities.TryGetValue(evt.EntityId, out var entity))
        {
            MarkProjected(sequence, evt.Tick, evt.WorldVersion);
            return;
        }

        entity.KindId = evt.KindId;
        entity.SubtypeId = evt.SubtypeId;
        entity.CultureId = evt.CurrentCultureId;
        entity.LastUpdatedTick = evt.Tick;
        entity.WorldVersion = evt.WorldVersion;
        MarkEntityBrowserStructureChanged(evt.EntityId);
        EntityDetailVersion++;
        EntityMapVersion++;
        MarkEntityVisualDirty(entity);
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnEntityStatusChanged(long sequence, in EntityStatusChangedKernelEvent evt)
    {
        if (!_entities.TryGetValue(evt.EntityId, out var entity) ||
            evt.CurrentStatusCode == EntityStatusCodes.Active)
        {
            MarkProjected(sequence, evt.Tick, evt.WorldVersion);
            return;
        }

        var retiredEntityId = evt.EntityId;
        RemoveActiveOccupants(entity);
        foreach (var key in _relationships.Keys
            .Where(key => key.SourceEntityId == retiredEntityId || key.TargetEntityId == retiredEntityId)
            .ToArray())
        {
            RemoveIndexedRelationship(key);
            _relationships.Remove(key);
        }

        foreach (var slot in _cultureSlots
            .Where(pair => pair.Value.CurrentEntityId == retiredEntityId)
            .Select(pair => pair.Key)
            .ToArray())
        {
            _cultureSlots.Remove(slot);
        }

        _entities.Remove(evt.EntityId);
        ActiveEntityCount--;
        TopBarVersion++;
        EntityMapVersion++;
        RelationshipVersion++;
        EntityDetailVersion++;
        MarkEntityBrowserStructureChanged(evt.EntityId);
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnEntityMoved(long sequence, in EntityMovedKernelEvent evt)
    {
        if (!_entities.TryGetValue(evt.EntityId, out var entity))
        {
            MarkProjected(sequence, evt.Tick, evt.WorldVersion);
            return;
        }

        var wasActive = entity.StatusCode == EntityStatusCodes.Active;
        var fromCoord = new HexCoord(evt.FromQ, evt.FromR);
        var toCoord = new HexCoord(evt.ToQ, evt.ToR);
        if (evt.PlaneCode == PlaneCodes.Physical)
        {
            entity.PhysicalQ = evt.ToQ;
            entity.PhysicalR = evt.ToR;
        }
        else
        {
            entity.SemanticQ = evt.ToQ;
            entity.SemanticR = evt.ToR;
            entity.SemanticS = evt.ToS;
        }

        entity.LastMove = new UiMovementProjection(
            evt.Tick,
            evt.PlaneCode,
            evt.FromQ,
            evt.FromR,
            evt.FromS,
            evt.ToQ,
            evt.ToR,
            evt.ToS);
        entity.LastUpdatedTick = evt.Tick;
        entity.WorldVersion = evt.WorldVersion;
        if (wasActive)
        {
            MoveActiveOccupant(evt.PlaneCode, evt.EntityId, fromCoord, toCoord);
            EntityMapVersion++;
        }

        EntityDetailVersion++;
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnCultureSlotOccupancyChanged(long sequence, in CultureSlotOccupancyChangedKernelEvent evt)
    {
        var key = new UiCultureSlotKey(evt.CultureId, evt.SlotId);
        if (_cultureSlots.TryGetValue(key, out var existing) &&
            existing.CurrentEntityId > 0 &&
            existing.CurrentEntityId != evt.PreviousEntityId)
        {
            _cultureSlots.Remove(key);
            UpdateEntitySlotFlags(existing.CurrentEntityId);
        }

        if (evt.CurrentEntityId > 0)
        {
            _cultureSlots[key] = new UiCultureSlotOccupancyProjection(
                key,
                evt.PolarityCode,
                evt.CurrentEntityId,
                FixedScalar.FromUnits(evt.CurrentScoreUnits).ToSingle(),
                evt.Tick,
                evt.WorldVersion);
        }
        else
        {
            _cultureSlots.Remove(key);
        }

        if (evt.PreviousEntityId > 0)
        {
            UpdateEntitySlotFlags(evt.PreviousEntityId);
        }

        if (evt.CurrentEntityId > 0)
        {
            UpdateEntitySlotFlags(evt.CurrentEntityId);
        }

        EntityMapVersion++;
        MarkEntityVisualDirty(evt.PreviousEntityId);
        MarkEntityVisualDirty(evt.CurrentEntityId);
        MarkEntityBrowserStructureChanged(evt.PreviousEntityId, evt.CurrentEntityId);
        EntityDetailVersion++;
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnAttributeChanged(long sequence, in AttributeChangedKernelEvent evt)
    {
        if (!_entities.TryGetValue(evt.EntityId, out var entity))
        {
            MarkProjected(sequence, evt.Tick, evt.WorldVersion);
            return;
        }

        entity.Attributes[evt.AttributeId] = new UiAttributeProjection(
            evt.AttributeId,
            evt.SourceCodeId,
            KernelFixedPoint.AttributeValue(evt.OldValueUnits),
            KernelFixedPoint.AttributeValue(evt.NewValueUnits),
            evt.Tick,
            evt.WorldVersion);
        entity.LastUpdatedTick = evt.Tick;
        entity.WorldVersion = evt.WorldVersion;
        EntityDetailVersion++;
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnTraitAssigned(long sequence, in TraitAssignedKernelEvent evt)
    {
        if (!_entities.TryGetValue(evt.EntityId, out var entity))
        {
            MarkProjected(sequence, evt.Tick, evt.WorldVersion);
            return;
        }

        entity.Traits.Add(evt.TraitId);
        entity.LastUpdatedTick = evt.Tick;
        entity.WorldVersion = evt.WorldVersion;
        EntityDetailVersion++;
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnRelationshipChanged(long sequence, in RelationshipChangedKernelEvent evt)
    {
        var key = new UiRelationshipKey(
            evt.SourceEntityId,
            evt.TargetEntityId,
            evt.RelationshipKindId);
        if (evt.IsActive != 0 &&
            _entities.ContainsKey(evt.SourceEntityId) &&
            _entities.ContainsKey(evt.TargetEntityId))
        {
            _relationships[key] = new UiRelationshipProjection(
                key,
                evt.ChangeCodeId,
                evt.SourceCodeId,
                evt.PolarityCode,
                KernelFixedPoint.RelationshipStrengthValue(evt.PreviousStrengthUnits),
                KernelFixedPoint.RelationshipStrengthValue(evt.NewStrengthUnits),
                true,
                evt.Tick,
                evt.WorldVersion);
            ReindexRelationship(key);
        }
        else
        {
            RemoveIndexedRelationship(key);
            _relationships.Remove(key);
        }
        RelationshipVersion++;
        EntityDetailVersion++;
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnGoalGenerated(long sequence, in GoalGeneratedKernelEvent evt)
    {
        if (!_entities.TryGetValue(evt.EntityId, out var entity))
        {
            MarkProjected(sequence, evt.Tick, evt.WorldVersion);
            return;
        }

        entity.KindId = evt.KindId;
        entity.LastGoalGeneration = new UiGoalGenerationProjection(
            evt.Tick,
            evt.GoalCount,
            evt.ActionableGoalCount,
            evt.State,
            evt.WorldVersion);
        entity.LastUpdatedTick = evt.Tick;
        entity.WorldVersion = evt.WorldVersion;
        EntityDetailVersion++;
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnGeneratorEvaluated(long sequence, in GeneratorEvaluatedKernelEvent evt)
    {
        var generator = GetOrCreateGenerator(evt.GeneratorId);
        generator.LastEvaluation = new UiGeneratorEvaluationProjection(
            evt.Tick,
            evt.StatusCode,
            evt.SelectedTargetEntityId,
            evt.CandidateCount,
            evt.FailedFireCount,
            evt.WorldVersion);
        TableauVersion++;
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnGeneratorFired(long sequence, in GeneratorFiredKernelEvent evt)
    {
        var generator = GetOrCreateGenerator(evt.GeneratorId);
        var fire = new UiGeneratorFireProjection(
            evt.Tick,
            evt.GeneratorId,
            evt.TargetEntityId,
            evt.CreatedEntityCount,
            evt.CreatedRelationshipCount,
            evt.WorldVersion);
        generator.TotalFired++;
        generator.LastFire = fire;
        _generatorFires.Add(fire);
        if (_currentProvenanceInvocationId > 0)
        {
            _generatorFiresByInvocation[_currentProvenanceInvocationId] = fire;
        }

        TopBarVersion++;
        TableauVersion++;
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnGambitExecuted(long sequence, in GambitExecutedKernelEvent evt)
    {
        var entity = GetOrCreateEntity(evt.ActorId);
        entity.KindId = evt.ActorKindId;
        entity.LastGambitExecution = new UiGambitExecutionProjection(
            evt.Tick,
            evt.GambitId,
            evt.AffectedEntityId0,
            evt.AffectedEntityId1,
            evt.AffectedEntityCount,
            evt.State,
            evt.WorldVersion);
        entity.LastUpdatedTick = evt.Tick;
        entity.WorldVersion = evt.WorldVersion;

        if ((evt.State & KernelEventState.Succeeded) != KernelEventState.None)
        {
            var gambit = GetOrCreateGambit(evt.GambitId);
            gambit.TotalSucceeded++;
            gambit.LastSucceededTick = evt.Tick;
            TableauVersion++;
        }

        EntityDetailVersion++;
        EntityMapVersion++;
        MarkEntityVisualDirty(entity);
        MarkEntityBrowserRowChanged(evt.ActorId);
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnTickCompleted(long sequence, in TickCompletedKernelEvent evt)
    {
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnWatermarkAdvanced(
        long sequence,
        in WatermarkAdvancedKernelEvent evt)
    {
        CurrentTick = evt.LowWatermark;
        TopBarVersion++;
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnPhaseCompleted(long sequence, in PhaseCompletedKernelEvent evt)
    {
        PublishedPhaseRevision = evt.PhaseRevision;
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnGeneratorEnabledChanged(long sequence, in GeneratorEnabledChangedKernelEvent evt)
    {
        var generator = GetOrCreateGenerator(evt.GeneratorId);
        generator.IsEnabled = evt.Enabled != 0;
        generator.LastEnabledChanged = new UiGeneratorEnabledProjection(
            evt.Tick,
            evt.Enabled != 0,
            evt.WorldVersion);
        TableauVersion++;
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnGambitEnabledChanged(long sequence, in GambitEnabledChangedKernelEvent evt)
    {
        var gambit = GetOrCreateGambit(evt.GambitId);
        gambit.IsEnabled = evt.Enabled != 0;
        TableauVersion++;
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    private UiEntityProjection GetOrCreateEntity(int entityId)
    {
        if (_entities.TryGetValue(entityId, out var entity))
        {
            return entity;
        }

        entity = new UiEntityProjection(entityId);
        _entities.Add(entityId, entity);
        return entity;
    }

    private UiGeneratorProjection GetOrCreateGenerator(int generatorId)
    {
        if (_generators.TryGetValue(generatorId, out var generator))
        {
            return generator;
        }

        generator = new UiGeneratorProjection(generatorId);
        _generators.Add(generatorId, generator);
        return generator;
    }

    private UiGambitProjection GetOrCreateGambit(int gambitId)
    {
        if (_gambits.TryGetValue(gambitId, out var gambit))
        {
            return gambit;
        }

        gambit = new UiGambitProjection(gambitId);
        _gambits.Add(gambitId, gambit);
        return gambit;
    }

    private List<UiRelationshipProjection> ActiveRelationships(
        Dictionary<int, HashSet<UiRelationshipKey>> index,
        int entityId)
    {
        if (!index.TryGetValue(entityId, out var keys) || keys.Count == 0)
        {
            return [];
        }

        var relationships = new List<UiRelationshipProjection>(keys.Count);
        foreach (var key in keys)
        {
            if (_relationships.TryGetValue(key, out var relationship))
            {
                relationships.Add(relationship);
            }
        }

        return relationships;
    }

    private void ReindexRelationshipsForEntity(int entityId)
    {
        foreach (var key in _relationships.Keys
            .Where(key => key.SourceEntityId == entityId || key.TargetEntityId == entityId)
            .ToArray())
        {
            ReindexRelationship(key);
        }
    }

    private void ReindexRelationship(UiRelationshipKey key)
    {
        RemoveIndexedRelationship(key);
        if (!_relationships.TryGetValue(key, out var relationship) ||
            !relationship.IsActive ||
            EntityIsHistorical(key.SourceEntityId) ||
            EntityIsHistorical(key.TargetEntityId))
        {
            return;
        }

        AddIndexedRelationship(_activeOutgoingRelationshipsByEntity, key.SourceEntityId, key);
        AddIndexedRelationship(_activeIncomingRelationshipsByEntity, key.TargetEntityId, key);
    }

    private void RemoveIndexedRelationship(UiRelationshipKey key)
    {
        RemoveIndexedRelationship(_activeOutgoingRelationshipsByEntity, key.SourceEntityId, key);
        RemoveIndexedRelationship(_activeIncomingRelationshipsByEntity, key.TargetEntityId, key);
    }

    private static void AddIndexedRelationship(
        Dictionary<int, HashSet<UiRelationshipKey>> index,
        int entityId,
        UiRelationshipKey key)
    {
        if (!index.TryGetValue(entityId, out var keys))
        {
            keys = [];
            index[entityId] = keys;
        }

        keys.Add(key);
    }

    private static void RemoveIndexedRelationship(
        Dictionary<int, HashSet<UiRelationshipKey>> index,
        int entityId,
        UiRelationshipKey key)
    {
        if (!index.TryGetValue(entityId, out var keys))
        {
            return;
        }

        keys.Remove(key);
        if (keys.Count == 0)
        {
            index.Remove(entityId);
        }
    }

    private bool EntityIsHistorical(int entityId) =>
        !_entities.TryGetValue(entityId, out var entity) ||
        entity.StatusCode != EntityStatusCodes.Active;

    private void AddActiveOccupants(UiEntityProjection entity)
    {
        AddActiveOccupant(PlaneCodes.Physical, entity.EntityId, new HexCoord(entity.PhysicalQ, entity.PhysicalR));
        AddActiveOccupant(PlaneCodes.Semantic, entity.EntityId, new HexCoord(entity.SemanticQ, entity.SemanticR));
    }

    private void RemoveActiveOccupants(UiEntityProjection entity)
    {
        RemoveActiveOccupant(PlaneCodes.Physical, entity.EntityId, new HexCoord(entity.PhysicalQ, entity.PhysicalR));
        RemoveActiveOccupant(PlaneCodes.Semantic, entity.EntityId, new HexCoord(entity.SemanticQ, entity.SemanticR));
    }

    private void MoveActiveOccupant(int planeCode, int entityId, HexCoord fromCoord, HexCoord toCoord)
    {
        RemoveActiveOccupant(planeCode, entityId, fromCoord);
        AddActiveOccupant(planeCode, entityId, toCoord);
    }

    private void AddActiveOccupant(int planeCode, int entityId, HexCoord coord)
    {
        var occupantsByCoord = OccupantsByCoord(planeCode);
        if (!occupantsByCoord.TryGetValue(coord, out var occupants))
        {
            occupants = [];
            occupantsByCoord[coord] = occupants;
        }

        occupants.Add(entityId);
        MarkEntityHexDirty(planeCode, coord);
    }

    private void RemoveActiveOccupant(int planeCode, int entityId, HexCoord coord)
    {
        if (TryGetOccupants(planeCode, coord, out var occupants))
        {
            occupants.Remove(entityId);
            if (occupants.Count == 0)
            {
                _activeOccupantsByPlane[planeCode].Remove(coord);
            }
        }

        MarkEntityHexDirty(planeCode, coord);
    }

    private Dictionary<HexCoord, HashSet<int>> OccupantsByCoord(int planeCode)
    {
        if (!_activeOccupantsByPlane.TryGetValue(planeCode, out var occupantsByCoord))
        {
            occupantsByCoord = [];
            _activeOccupantsByPlane[planeCode] = occupantsByCoord;
        }

        return occupantsByCoord;
    }

    private bool TryGetOccupants(int planeCode, HexCoord coord, out HashSet<int> occupants)
    {
        if (_activeOccupantsByPlane.TryGetValue(planeCode, out var occupantsByCoord) &&
            occupantsByCoord.TryGetValue(coord, out occupants!))
        {
            return true;
        }

        occupants = null!;
        return false;
    }

    private void MarkEntityHexDirty(int planeCode, HexCoord coord)
    {
        if (!_dirtyEntityHexesByPlane.TryGetValue(planeCode, out var dirtyHexes))
        {
            dirtyHexes = [];
            _dirtyEntityHexesByPlane[planeCode] = dirtyHexes;
        }

        dirtyHexes.Add(coord);
    }

    protected override void OnProvenanceInvocationRecorded(
        long sequence,
        in ProvenanceInvocationRecordedKernelEvent evt)
    {
        if (evt.ProducerKindCode != ProvenanceProducerKindCodes.Generator ||
            !_createdEntitiesByInvocation.Remove(evt.InvocationId, out var createdEntities))
        {
            _generatorFiresByInvocation.Remove(evt.InvocationId);
            MarkProjected(sequence, evt.Tick, evt.WorldVersion);
            return;
        }

        _generatorFiresByInvocation.Remove(evt.InvocationId, out var fire);
        foreach (var entityId in createdEntities)
        {
            RecordGeneratedEntity(
                evt.Tick,
                evt.ProducerId,
                evt.PrimaryEntityId,
                fire,
                entityId);
        }
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    private void TrackCreatedEntityForGeneratorAttribution(in EntityCreatedKernelEvent evt)
    {
        if (_currentProvenanceInvocationId <= 0)
        {
            return;
        }

        if (!_createdEntitiesByInvocation.TryGetValue(
                _currentProvenanceInvocationId,
                out var createdEntities))
        {
            createdEntities = [];
            _createdEntitiesByInvocation[_currentProvenanceInvocationId] = createdEntities;
        }

        createdEntities.Add(evt.EntityId);
    }

    private void RecordGeneratedEntity(
        int tick,
        int generatorId,
        int targetEntityId,
        UiGeneratorFireProjection? fire,
        int entityId)
    {
        fire?.AddCreatedEntityId(entityId);
        if (!_entities.TryGetValue(entityId, out var entity))
        {
            return;
        }

        var nullableTargetEntityId = targetEntityId == 0 ? (int?)null : targetEntityId;
        if (entity.CreatedByGeneratorId == generatorId &&
            entity.CreatedByGeneratorTick == tick &&
            entity.CreatedByGeneratorTargetEntityId == nullableTargetEntityId)
        {
            return;
        }

        entity.CreatedByGeneratorId = generatorId;
        entity.CreatedByGeneratorTick = tick;
        entity.CreatedByGeneratorTargetEntityId = nullableTargetEntityId;
        MarkEntityHexDirty(PlaneCodes.Physical, new HexCoord(entity.PhysicalQ, entity.PhysicalR));
        MarkEntityHexDirty(PlaneCodes.Semantic, new HexCoord(entity.SemanticQ, entity.SemanticR));
        EntityMapVersion++;
        MarkEntityBrowserStructureChanged(entityId);
        EntityDetailVersion++;
    }

    private void UpdateEntitySlotFlags(int entityId)
    {
        if (!_entities.TryGetValue(entityId, out var entity))
        {
            return;
        }

        var count = 0;
        var polarityCode = 0;
        foreach (var slot in _cultureSlots.Values)
        {
            if (slot.CurrentEntityId != entityId)
            {
                continue;
            }

            count++;
            polarityCode = count == 1
                ? slot.PolarityCode
                : PreferredSlotPolarity(polarityCode, slot.PolarityCode);
        }

        entity.CultureSlotCount = count;
        entity.CultureSlotPolarityCode = count == 0 ? 0 : polarityCode;
    }

    private static int PreferredSlotPolarity(int current, int candidate)
    {
        return SlotPolarityPriority(candidate) > SlotPolarityPriority(current)
            ? candidate
            : current;
    }

    private static int SlotPolarityPriority(int polarityCode) => polarityCode switch
    {
        RelationshipPolarityCodes.Negative => 3,
        RelationshipPolarityCodes.Positive => 2,
        RelationshipPolarityCodes.Neutral => 1,
        _ => 0,
    };

    private void MarkProjected(long sequence, int tick, long worldVersion)
    {
        LastProjectedSequence = sequence;
        WorldVersion = Math.Max(WorldVersion, worldVersion);
    }

}

public sealed class UiEntityProjection(int entityId)
{
    public int EntityId { get; } = entityId;

    public int KindId { get; internal set; }

    public int SubtypeId { get; internal set; }

    public int CultureId { get; internal set; }

    public int NameSeed { get; internal set; }

    public int? PreviousStatusCode { get; internal set; }

    public int StatusCode { get; internal set; }

    public int PhysicalQ { get; internal set; }

    public int PhysicalR { get; internal set; }

    public int SemanticQ { get; internal set; }

    public int SemanticR { get; internal set; }

    public int SemanticS { get; internal set; }

    public int? CreatedTick { get; internal set; }

    public int LastUpdatedTick { get; internal set; }

    public long WorldVersion { get; internal set; }

    public bool WasCreatedFromEvent { get; internal set; }

    public int? CreatedByGeneratorId { get; internal set; }

    public int? CreatedByGeneratorTick { get; internal set; }

    public int? CreatedByGeneratorTargetEntityId { get; internal set; }

    public Dictionary<int, UiAttributeProjection> Attributes { get; } = [];

    public HashSet<int> Traits { get; } = [];

    public UiMovementProjection? LastMove { get; internal set; }

    public UiGoalGenerationProjection? LastGoalGeneration { get; internal set; }

    public UiGambitExecutionProjection? LastGambitExecution { get; internal set; }

    public int CultureSlotCount { get; internal set; }

    public int CultureSlotPolarityCode { get; internal set; }

    public int WorkTier { get; internal set; }
}

public sealed class UiGeneratorProjection(int generatorId)
{
    public int GeneratorId { get; } = generatorId;

    public bool IsEnabled { get; internal set; }

    public int TotalFired { get; internal set; }

    public UiGeneratorEnabledProjection? LastEnabledChanged { get; internal set; }

    public UiGeneratorEvaluationProjection? LastEvaluation { get; internal set; }

    public UiGeneratorFireProjection? LastFire { get; internal set; }
}

public sealed class UiGambitProjection(int gambitId)
{
    public int GambitId { get; } = gambitId;

    public int TotalSucceeded { get; internal set; }

    public int? LastSucceededTick { get; internal set; }

    public bool IsEnabled { get; internal set; } = true;
}

public readonly record struct UiRelationshipKey(
    int SourceEntityId,
    int TargetEntityId,
    int RelationshipKindId);

public readonly record struct UiCultureSlotKey(int CultureId, int SlotId);

public sealed record UiHealthAttributeDefinitionProjection(
    int KindId,
    int AttributeId,
    float Minimum,
    float Maximum,
    float DefaultValue);

/// <summary>One world-beat age transition for the ages timeline (opened or closed).</summary>
public sealed record UiWorldBeatTransition(int Tick, int TrackCode, int AgeId, bool Opened);

public sealed record UiCultureSlotDefinitionProjection(
    UiCultureSlotKey Key,
    int CultureId,
    int SlotId,
    string StableName,
    string DisplayName,
    string? Description,
    int PolarityCode);

public sealed record UiCultureSlotOccupancyProjection(
    UiCultureSlotKey Key,
    int PolarityCode,
    int CurrentEntityId,
    float CurrentScore,
    int Tick,
    long WorldVersion);

public sealed record UiAttributeProjection(
    int AttributeId,
    int SourceCodeId,
    float OldValue,
    float NewValue,
    int Tick,
    long WorldVersion);

public sealed record UiMovementProjection(
    int Tick,
    int PlaneCode,
    int FromQ,
    int FromR,
    int FromS,
    int ToQ,
    int ToR,
    int ToS);

public sealed record UiRelationshipProjection(
    UiRelationshipKey Key,
    int ChangeCodeId,
    int SourceCodeId,
    int PolarityCode,
    float PreviousStrength,
    float NewStrength,
    bool IsActive,
    int Tick,
    long WorldVersion);

public sealed record UiGeneratorEnabledProjection(
    int Tick,
    bool IsEnabled,
    long WorldVersion);

public sealed record UiGoalGenerationProjection(
    int Tick,
    int GoalCount,
    int ActionableGoalCount,
    KernelEventState State,
    long WorldVersion);

public sealed record UiGambitExecutionProjection(
    int Tick,
    int GambitId,
    int AffectedEntityId0,
    int AffectedEntityId1,
    int AffectedEntityCount,
    KernelEventState State,
    long WorldVersion);

public sealed record UiGeneratorEvaluationProjection(
    int Tick,
    int StatusCode,
    int SelectedTargetEntityId,
    int CandidateCount,
    int FailedFireCount,
    long WorldVersion);

public sealed record UiGeneratorFireProjection(
    int Tick,
    int GeneratorId,
    int TargetEntityId,
    int CreatedEntityCount,
    int CreatedRelationshipCount,
    long WorldVersion)
{
    private readonly List<int> _createdEntityIds = [];

    public IReadOnlyList<int> CreatedEntityIds => _createdEntityIds;

    internal void AddCreatedEntityId(int entityId)
    {
        if (!_createdEntityIds.Contains(entityId))
        {
            _createdEntityIds.Add(entityId);
        }
    }
}
