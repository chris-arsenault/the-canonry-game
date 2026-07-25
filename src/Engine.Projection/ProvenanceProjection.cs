namespace Engine.Projection;

using Engine.Constants;
using Engine.Eventing;

public sealed class ProvenanceProjection : KernelEventProjector
{
    private const int RecentInvocationCapacity = 4096;
    private const int RecentInvocationTrimCount = 1024;

    private readonly Dictionary<int, ProvenanceInvocationProjection> _invocations = [];
    private readonly Dictionary<int, List<ProvenanceOutputProjection>> _outputsByInvocation = [];
    private readonly Dictionary<int, int> _invocationReferenceCounts = [];
    private readonly Dictionary<int, int> _entityCreatedByInvocation = [];
    private readonly Dictionary<int, int> _relationshipCreatedByInvocation = [];
    private readonly Dictionary<int, int> _relationshipLastChangedByInvocation = [];
    private readonly Dictionary<int, ProvenanceRelationshipProjection> _relationships = [];
    private readonly List<ProvenanceInvocationProjection> _invocationsInProjectionOrder = [];
    private int _currentInvocationId;

    public IReadOnlyDictionary<int, ProvenanceInvocationProjection> Invocations => _invocations;

    public IReadOnlyList<ProvenanceInvocationProjection> InvocationsInProjectionOrder => _invocationsInProjectionOrder;

    public long InvocationProjectionStartIndex { get; private set; }

    public long LastProjectedSequence { get; private set; } = -1;

    public IReadOnlyDictionary<int, int> EntityCreatedByInvocation => _entityCreatedByInvocation;

    public IReadOnlyDictionary<int, int> RelationshipCreatedByInvocation => _relationshipCreatedByInvocation;

    public IReadOnlyDictionary<int, int> RelationshipLastChangedByInvocation => _relationshipLastChangedByInvocation;

    protected override void OnProjectingEvent(in SequencedKernelEvent kernelEvent)
    {
        _currentInvocationId = kernelEvent.Event.ProvenanceInvocationId;
        LastProjectedSequence = kernelEvent.Sequence;
    }

    protected override void OnEntityCreated(long sequence, in EntityCreatedKernelEvent evt)
    {
        if (_currentInvocationId == 0)
        {
            return;
        }

        AddOutput(
            sequence,
            evt.Tick,
            ProvenanceOutputKindCodes.EntityCreated,
            evt.EntityId,
            evt.EntityId,
            0,
            evt.KindId,
            ProvenanceOutputKindCodes.EntityCreated);
        _entityCreatedByInvocation[evt.EntityId] = _currentInvocationId;
        AddInvocationReference(_currentInvocationId);
    }

    protected override void OnEntityStatusChanged(long sequence, in EntityStatusChangedKernelEvent evt)
    {
        if (evt.CurrentStatusCode == EntityStatusCodes.Active ||
            !_entityCreatedByInvocation.Remove(evt.EntityId, out var invocationId))
        {
            return;
        }

        RemoveInvocationReference(invocationId);
    }

    protected override void OnRelationshipChanged(long sequence, in RelationshipChangedKernelEvent evt)
    {
        if (_currentInvocationId == 0)
        {
            if (evt.IsActive == 0)
            {
                RemoveRelationshipReferences(evt.RelationshipId);
            }
            return;
        }

        var created = !_relationshipCreatedByInvocation.ContainsKey(evt.RelationshipId) &&
            evt.PreviousStrengthUnits <= 0 &&
            evt.IsActive != 0;
        if (created)
        {
            _relationshipCreatedByInvocation[evt.RelationshipId] = _currentInvocationId;
            AddInvocationReference(_currentInvocationId);
            AddOutput(
                sequence,
                evt.Tick,
                ProvenanceOutputKindCodes.RelationshipChanged,
                evt.RelationshipId,
                evt.SourceEntityId,
                evt.TargetEntityId,
                evt.RelationshipKindId,
                evt.ChangeCodeId);
        }

        if (_relationshipLastChangedByInvocation.Remove(evt.RelationshipId, out var previousInvocationId))
        {
            RemoveInvocationReference(previousInvocationId);
        }
        _relationshipLastChangedByInvocation[evt.RelationshipId] = _currentInvocationId;
        AddInvocationReference(_currentInvocationId);
        _relationships[evt.RelationshipId] = new ProvenanceRelationshipProjection(
            evt.RelationshipId,
            evt.RelationshipKindId,
            evt.PolarityCode,
            evt.SourceEntityId,
            evt.TargetEntityId,
            KernelFixedPoint.RelationshipStrengthValue(evt.NewStrengthUnits),
            evt.IsActive != 0);

        if (evt.IsActive != 0)
        {
            return;
        }

        RemoveRelationshipReferences(evt.RelationshipId);
    }

    private void RemoveRelationshipReferences(int relationshipId)
    {
        _relationships.Remove(relationshipId);
        if (_relationshipCreatedByInvocation.Remove(relationshipId, out var createdByInvocationId))
        {
            RemoveInvocationReference(createdByInvocationId);
        }
        if (_relationshipLastChangedByInvocation.Remove(relationshipId, out var lastChangedByInvocationId))
        {
            RemoveInvocationReference(lastChangedByInvocationId);
        }
    }

    protected override void OnProvenanceInvocationRecorded(long sequence, in ProvenanceInvocationRecordedKernelEvent evt)
    {
        var invocation = new ProvenanceInvocationProjection(
            evt.InvocationId,
            sequence,
            evt.Tick,
            evt.ProducerKindCode,
            evt.ProducerId,
            evt.PrimaryEntityId,
            evt.SecondaryEntityId,
            evt.ParentInvocationId,
            evt.TriggerKindCode,
            evt.TriggerId,
            evt.OutputCount);
        _invocations[evt.InvocationId] = invocation;
        _invocationsInProjectionOrder.Add(invocation);
        if (_invocationReferenceCounts.GetValueOrDefault(evt.InvocationId) > 0 &&
            evt.ParentInvocationId > 0)
        {
            AddInvocationReference(evt.ParentInvocationId);
        }
        TrimRecentInvocations();
    }

    public bool TryGetInvocation(int invocationId, out ProvenanceInvocationProjection invocation) =>
        _invocations.TryGetValue(invocationId, out invocation);

    public IReadOnlyList<ProvenanceOutputProjection> OutputsOfInvocation(int invocationId) =>
        _outputsByInvocation.TryGetValue(invocationId, out var outputs)
            ? outputs
            : Array.Empty<ProvenanceOutputProjection>();

    public bool TryGetEntityCreatedByInvocation(int entityId, out int invocationId) =>
        _entityCreatedByInvocation.TryGetValue(entityId, out invocationId);

    public bool TryGetRelationshipCreatedByInvocation(int relationshipId, out int invocationId) =>
        _relationshipCreatedByInvocation.TryGetValue(relationshipId, out invocationId);

    public bool TryGetRelationshipLastChangedByInvocation(int relationshipId, out int invocationId) =>
        _relationshipLastChangedByInvocation.TryGetValue(relationshipId, out invocationId);

    public bool TryGetParentInvocation(
        int invocationId,
        out ProvenanceInvocationProjection parentInvocation)
    {
        parentInvocation = default;
        if (!_invocations.TryGetValue(invocationId, out var invocation) ||
            invocation.ParentInvocationId == 0)
        {
            return false;
        }

        return _invocations.TryGetValue(invocation.ParentInvocationId, out parentInvocation);
    }

    public IEnumerable<ProvenanceInvocationProjection> EnumerateParentChain(int invocationId)
    {
        var currentId = invocationId;
        while (TryGetParentInvocation(currentId, out var parent))
        {
            yield return parent;
            currentId = parent.InvocationId;
        }
    }

    public bool TryGetRelationshipTrigger(
        int invocationId,
        out ProvenanceRelationshipTriggerProjection trigger)
    {
        trigger = default;
        if (!_invocations.TryGetValue(invocationId, out var invocation) ||
            invocation.TriggerKindCode != ProvenanceTriggerKindCodes.Relationship ||
            invocation.TriggerId <= 0)
        {
            return false;
        }

        _relationships.TryGetValue(invocation.TriggerId, out var relationship);
        _relationshipCreatedByInvocation.TryGetValue(invocation.TriggerId, out var createdByInvocationId);
        _relationshipLastChangedByInvocation.TryGetValue(invocation.TriggerId, out var lastChangedByInvocationId);

        trigger = new ProvenanceRelationshipTriggerProjection(
            invocation.TriggerId,
            relationship.RelationshipKindId,
            relationship.PolarityCode,
            relationship.SourceEntityId,
            relationship.TargetEntityId,
            createdByInvocationId,
            lastChangedByInvocationId);
        return true;
    }

    private void AddInvocationReference(int invocationId)
    {
        if (invocationId <= 0)
        {
            return;
        }

        var previous = _invocationReferenceCounts.GetValueOrDefault(invocationId);
        _invocationReferenceCounts[invocationId] = previous + 1;
        if (previous == 0 &&
            _invocations.TryGetValue(invocationId, out var invocation) &&
            invocation.ParentInvocationId > 0)
        {
            AddInvocationReference(invocation.ParentInvocationId);
        }
    }

    private void RemoveInvocationReference(int invocationId)
    {
        if (!_invocationReferenceCounts.TryGetValue(invocationId, out var count))
        {
            return;
        }

        if (count > 1)
        {
            _invocationReferenceCounts[invocationId] = count - 1;
            return;
        }

        _invocationReferenceCounts.Remove(invocationId);
        if (_invocations.TryGetValue(invocationId, out var invocation) &&
            invocation.ParentInvocationId > 0)
        {
            RemoveInvocationReference(invocation.ParentInvocationId);
        }

        TryPruneInvocation(invocationId);
    }

    private void TrimRecentInvocations()
    {
        if (_invocationsInProjectionOrder.Count <= RecentInvocationCapacity)
        {
            return;
        }

        var removedIds = new int[RecentInvocationTrimCount];
        for (var index = 0; index < RecentInvocationTrimCount; index++)
        {
            removedIds[index] = _invocationsInProjectionOrder[index].InvocationId;
        }
        _invocationsInProjectionOrder.RemoveRange(0, RecentInvocationTrimCount);
        InvocationProjectionStartIndex += RecentInvocationTrimCount;
        foreach (var invocationId in removedIds)
        {
            TryPruneInvocation(invocationId);
        }
    }

    private void TryPruneInvocation(int invocationId)
    {
        if (_invocationReferenceCounts.ContainsKey(invocationId) ||
            _invocationsInProjectionOrder.Count == 0 ||
            invocationId >= _invocationsInProjectionOrder[0].InvocationId)
        {
            return;
        }

        _invocations.Remove(invocationId);
        _outputsByInvocation.Remove(invocationId);
    }

    private void AddOutput(
        long sequence,
        int tick,
        int outputKindCode,
        int outputId,
        int subjectEntityId,
        int secondaryEntityId,
        int valueId,
        int changeCodeId)
    {
        if (!_outputsByInvocation.TryGetValue(_currentInvocationId, out var outputs))
        {
            outputs = [];
            _outputsByInvocation[_currentInvocationId] = outputs;
        }

        outputs.Add(new ProvenanceOutputProjection(
            _currentInvocationId,
            sequence,
            tick,
            outputKindCode,
            outputId,
            subjectEntityId,
            secondaryEntityId,
            valueId,
            changeCodeId));
    }

}

public readonly record struct ProvenanceInvocationProjection(
    int InvocationId,
    long Sequence,
    int Tick,
    int ProducerKindCode,
    int ProducerId,
    int PrimaryEntityId,
    int SecondaryEntityId,
    int ParentInvocationId,
    int TriggerKindCode,
    int TriggerId,
    int OutputCount);

public readonly record struct ProvenanceOutputProjection(
    int InvocationId,
    long Sequence,
    int Tick,
    int OutputKindCode,
    int OutputId,
    int SubjectEntityId,
    int SecondaryEntityId,
    int ValueId,
    int ChangeCodeId);

public readonly record struct ProvenanceRelationshipProjection(
    int RelationshipId,
    int RelationshipKindId,
    int PolarityCode,
    int SourceEntityId,
    int TargetEntityId,
    float Strength,
    bool IsActive);

public readonly record struct ProvenanceRelationshipTriggerProjection(
    int RelationshipId,
    int RelationshipKindId,
    int PolarityCode,
    int SourceEntityId,
    int TargetEntityId,
    int CreatedByInvocationId,
    int LastChangedByInvocationId);
