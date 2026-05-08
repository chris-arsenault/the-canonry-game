namespace Engine.Projection;

using Engine.Eventing;

public sealed class ProvenanceProjection : KernelEventProjector
{
    private readonly Dictionary<int, ProvenanceInvocationProjection> _invocations = [];
    private readonly Dictionary<int, List<ProvenanceOutputProjection>> _outputsByInvocation = [];
    private readonly Dictionary<int, int> _entityCreatedByInvocation = [];
    private readonly Dictionary<int, int> _relationshipCreatedByInvocation = [];
    private readonly Dictionary<int, int> _relationshipLastChangedByInvocation = [];
    private readonly Dictionary<int, ProvenanceRelationshipProjection> _relationships = [];
    private readonly List<ProvenanceInvocationProjection> _invocationsInProjectionOrder = [];
    private int _currentInvocationId;

    public IReadOnlyDictionary<int, ProvenanceInvocationProjection> Invocations => _invocations;

    public IReadOnlyList<ProvenanceInvocationProjection> InvocationsInProjectionOrder => _invocationsInProjectionOrder;

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
    }

    protected override void OnEntityStatusChanged(long sequence, in EntityStatusChangedKernelEvent evt)
    {
        AddEntityOutput(sequence, evt.Tick, ProvenanceOutputKindCodes.EntityStatusChanged, evt.EntityId, evt.CurrentStatusCode);
    }

    protected override void OnEntityMoved(long sequence, in EntityMovedKernelEvent evt)
    {
        AddEntityOutput(sequence, evt.Tick, ProvenanceOutputKindCodes.EntityMoved, evt.EntityId, evt.PlaneCode);
    }

    protected override void OnAttributeChanged(long sequence, in AttributeChangedKernelEvent evt)
    {
        AddEntityOutput(sequence, evt.Tick, ProvenanceOutputKindCodes.AttributeChanged, evt.EntityId, evt.AttributeId);
    }

    protected override void OnTraitAssigned(long sequence, in TraitAssignedKernelEvent evt)
    {
        AddEntityOutput(sequence, evt.Tick, ProvenanceOutputKindCodes.TraitAssigned, evt.EntityId, evt.TraitId);
    }

    protected override void OnRelationshipChanged(long sequence, in RelationshipChangedKernelEvent evt)
    {
        if (_currentInvocationId == 0)
        {
            return;
        }

        AddOutput(
            sequence,
            evt.Tick,
            ProvenanceOutputKindCodes.RelationshipChanged,
            evt.RelationshipId,
            evt.SourceEntityId,
            evt.TargetEntityId,
            evt.RelationshipKindId,
            evt.ChangeCodeId);

        if (!_relationshipCreatedByInvocation.ContainsKey(evt.RelationshipId) &&
            evt.PreviousStrength <= 0f &&
            evt.NewStrength > 0f)
        {
            _relationshipCreatedByInvocation[evt.RelationshipId] = _currentInvocationId;
        }

        _relationshipLastChangedByInvocation[evt.RelationshipId] = _currentInvocationId;
        _relationships[evt.RelationshipId] = new ProvenanceRelationshipProjection(
            evt.RelationshipId,
            evt.RelationshipKindId,
            evt.PolarityCode,
            evt.SourceEntityId,
            evt.TargetEntityId,
            evt.NewStrength,
            evt.NewStrength > 0f);
    }

    protected override void OnCellChanged(long sequence, in CellChangedKernelEvent evt)
    {
        if (_currentInvocationId == 0)
        {
            return;
        }

        AddOutput(
            sequence,
            evt.Tick,
            ProvenanceOutputKindCodes.CellChanged,
            CoordinateKey(evt.Q, evt.R),
            evt.CurrentOwnerEntityId,
            0,
            evt.CurrentTerrainId,
            ProvenanceOutputKindCodes.CellChanged);
    }

    protected override void OnCellInfluenceChanged(long sequence, in CellInfluenceChangedKernelEvent evt)
    {
        if (_currentInvocationId == 0)
        {
            return;
        }

        AddOutput(
            sequence,
            evt.Tick,
            ProvenanceOutputKindCodes.CellInfluenceChanged,
            CoordinateKey(evt.Q, evt.R),
            evt.EntityId,
            0,
            evt.PlaneCode,
            ProvenanceOutputKindCodes.CellInfluenceChanged);
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

    private void AddEntityOutput(long sequence, int tick, int outputKindCode, int entityId, int valueId)
    {
        if (_currentInvocationId == 0)
        {
            return;
        }

        AddOutput(
            sequence,
            tick,
            outputKindCode,
            entityId,
            entityId,
            0,
            valueId,
            outputKindCode);
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

    private static int CoordinateKey(int q, int r)
    {
        unchecked
        {
            var hash = (uint)q * 2654435761u;
            hash ^= (uint)r * 2246822519u;
            var value = (int)(hash & 2147483647u);
            return value == 0 ? 1 : value;
        }
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
