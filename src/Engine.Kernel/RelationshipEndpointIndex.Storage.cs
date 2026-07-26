namespace Engine.Kernel;

internal partial struct RelationshipEndpointIndex
{
    private readonly int[] _firstIncidentRelationship;
    private readonly int[] _nextIncidentRelationshipFromSource;
    private readonly int[] _previousIncidentRelationshipFromSource;
    private readonly int[] _nextIncidentRelationshipFromTarget;
    private readonly int[] _previousIncidentRelationshipFromTarget;
    private readonly bool[] _relationshipInDeltaGraph;
    private readonly int[] _outgoingFrozenOffsets;
    private readonly int[] _incomingFrozenOffsets;
    private readonly int[] _outgoingFrozenRelationships;
    private readonly int[] _incomingFrozenRelationships;
    private readonly int[] _firstOutgoingDeltaRelationshipByRow;
    private readonly int[] _firstIncomingDeltaRelationshipByRow;
    private readonly int[] _nextOutgoingDeltaRelationship;
    private readonly int[] _nextIncomingDeltaRelationship;
    private readonly int[] _relationshipBuildCursors;
    private readonly bool[] _entityAllocated;
    private readonly int[] _entityStatus;
    private readonly bool[] _relationshipActive;
    private readonly int[] _relationshipKindId;
    private readonly int[] _relationshipSourceId;
    private readonly int[] _relationshipTargetId;
    private readonly int[] _relationshipGenerations;
    private readonly int _entityCapacity;
    private readonly int _relationshipCapacity;
    private readonly int _graphDeltaMergeThreshold;
    private int _graphDeltaCount;

    internal RelationshipEndpointIndex(
        bool[] entityAllocated,
        int[] entityStatus,
        bool[] relationshipActive,
        int[] relationshipKindId,
        int[] relationshipSourceId,
        int[] relationshipTargetId,
        int[] relationshipGenerations,
        int relationshipKindCapacity,
        int graphDeltaMergeThreshold)
    {
        ValidateStorage(
            entityAllocated,
            entityStatus,
            relationshipActive,
            relationshipKindId,
            relationshipSourceId,
            relationshipTargetId,
            relationshipGenerations,
            relationshipKindCapacity,
            graphDeltaMergeThreshold);
        var entityCapacity = entityAllocated.Length - 1;
        var relationshipCapacity = relationshipActive.Length - 1;
        _entityAllocated = entityAllocated;
        _entityStatus = entityStatus;
        _relationshipActive = relationshipActive;
        _relationshipKindId = relationshipKindId;
        _relationshipSourceId = relationshipSourceId;
        _relationshipTargetId = relationshipTargetId;
        _relationshipGenerations = relationshipGenerations;
        _entityCapacity = entityCapacity;
        _relationshipCapacity = relationshipCapacity;
        _graphDeltaMergeThreshold = graphDeltaMergeThreshold;
        _firstIncidentRelationship = new int[entityCapacity + 1];
        _nextIncidentRelationshipFromSource = new int[relationshipCapacity + 1];
        _previousIncidentRelationshipFromSource = new int[relationshipCapacity + 1];
        _nextIncidentRelationshipFromTarget = new int[relationshipCapacity + 1];
        _previousIncidentRelationshipFromTarget = new int[relationshipCapacity + 1];
        _relationshipInDeltaGraph = new bool[relationshipCapacity + 1];
        var relationshipRowCount = checked(entityCapacity * relationshipKindCapacity);
        _outgoingFrozenOffsets = new int[relationshipRowCount + 1];
        _incomingFrozenOffsets = new int[relationshipRowCount + 1];
        _outgoingFrozenRelationships = new int[relationshipCapacity];
        _incomingFrozenRelationships = new int[relationshipCapacity];
        _firstOutgoingDeltaRelationshipByRow = new int[relationshipRowCount];
        _firstIncomingDeltaRelationshipByRow = new int[relationshipRowCount];
        _nextOutgoingDeltaRelationship = new int[relationshipCapacity + 1];
        _nextIncomingDeltaRelationship = new int[relationshipCapacity + 1];
        _relationshipBuildCursors = new int[relationshipRowCount + 1];
    }

    internal bool[] RelationshipInDeltaGraph => _relationshipInDeltaGraph;

    internal int[] OutgoingFrozenOffsets => _outgoingFrozenOffsets;

    internal int[] IncomingFrozenOffsets => _incomingFrozenOffsets;

    internal int[] OutgoingFrozenRelationships => _outgoingFrozenRelationships;

    internal int[] IncomingFrozenRelationships => _incomingFrozenRelationships;

    internal int[] FirstOutgoingDeltaRelationshipByRow =>
        _firstOutgoingDeltaRelationshipByRow;

    internal int[] FirstIncomingDeltaRelationshipByRow =>
        _firstIncomingDeltaRelationshipByRow;

    internal int[] NextOutgoingDeltaRelationship => _nextOutgoingDeltaRelationship;

    internal int[] NextIncomingDeltaRelationship => _nextIncomingDeltaRelationship;

    private static void ValidateStorage(
        bool[] entityAllocated,
        int[] entityStatus,
        bool[] relationshipActive,
        int[] relationshipKindId,
        int[] relationshipSourceId,
        int[] relationshipTargetId,
        int[] relationshipGenerations,
        int relationshipKindCapacity,
        int graphDeltaMergeThreshold)
    {
        global::System.ArgumentNullException.ThrowIfNull(entityAllocated);
        global::System.ArgumentNullException.ThrowIfNull(entityStatus);
        global::System.ArgumentNullException.ThrowIfNull(relationshipActive);
        global::System.ArgumentNullException.ThrowIfNull(relationshipKindId);
        global::System.ArgumentNullException.ThrowIfNull(relationshipSourceId);
        global::System.ArgumentNullException.ThrowIfNull(relationshipTargetId);
        global::System.ArgumentNullException.ThrowIfNull(relationshipGenerations);
        NumericGuard.ThrowIfNegativeOrZero(entityAllocated.Length - 1);
        NumericGuard.ThrowIfNegativeOrZero(relationshipActive.Length - 1);
        NumericGuard.ThrowIfNegativeOrZero(relationshipKindCapacity);
        NumericGuard.ThrowIfNegative(graphDeltaMergeThreshold);
        if (entityStatus.Length != entityAllocated.Length ||
            relationshipKindId.Length != relationshipActive.Length ||
            relationshipSourceId.Length != relationshipActive.Length ||
            relationshipTargetId.Length != relationshipActive.Length ||
            relationshipGenerations.Length != relationshipActive.Length)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private bool RelationshipIsHotActive(int relationshipId)
    {
        return (uint)relationshipId < (uint)_relationshipActive.Length &&
            _relationshipActive[relationshipId] &&
            EntityIsActive(_relationshipSourceId[relationshipId]) &&
            EntityIsActive(_relationshipTargetId[relationshipId]);
    }

    private bool EntityIsActive(int entityId)
    {
        return (uint)entityId < (uint)_entityAllocated.Length &&
            _entityAllocated[entityId] &&
            _entityStatus[entityId] == EntityStatusCodes.Active;
    }

    private int RelationshipOccurrenceHandle(int relationshipId) =>
        checked((_relationshipGenerations[relationshipId] * _relationshipCapacity) + relationshipId);

    private int RelationshipRowIndex(int entityId, int kindId) =>
        (kindId - 1) * _entityCapacity + entityId - 1;

    private static bool FrozenOccurrenceComesBefore(
        int relationshipCapacity,
        int leftOccurrenceHandle,
        int rightOccurrenceHandle)
    {
        var leftRelationshipId = ((leftOccurrenceHandle - 1) % relationshipCapacity) + 1;
        var rightRelationshipId = ((rightOccurrenceHandle - 1) % relationshipCapacity) + 1;
        return leftRelationshipId < rightRelationshipId;
    }
}
