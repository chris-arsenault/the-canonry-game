namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class MinorActorProgramState : IKernelProgramState
{
    private readonly bool[] _active;
    private readonly int[] _generations;
    private readonly int[] _cultureIds;
    private readonly int[] _sourceEntityIds;
    private readonly int[] _sourceEntityOccurrenceHandles;
    private readonly int[] _sourceRuleIds;
    private readonly int[] _bindingRelationshipIds;
    private readonly int[] _bindingRelationshipOccurrenceHandles;
    private readonly int[] _roleCodes;
    private readonly int[] _planeCodes;
    private readonly int[] _q;
    private readonly int[] _r;
    private readonly int[] _previousQ;
    private readonly int[] _previousR;
    private readonly int[] _targetQ;
    private readonly int[] _targetR;
    private readonly int[] _targetingCodes;
    private readonly int[] _postureCodes;
    private readonly int[] _createdAtTicks;
    private readonly int[] _nextReplanTicks;
    private readonly int[] _commitmentUntilTicks;
    private readonly FixedScalar[] _strengths;
    private readonly FixedScalar[] _workCharges;
    private readonly int[] _cellIds;
    private readonly int[] _firstActorByCell;
    private readonly int[] _nextActorByCell;
    private readonly int[] _previousActorByCell;
    private readonly int[] _activeActorIds;
    private readonly int[] _activeActorPositions;
    private readonly int[] _freeActorIds;
    private readonly int[] _actorBySourceRule;
    private readonly int[] _actorByRelationship;
    private readonly int _sourceRuleStride;
    private int _freeActorCount;

    public MinorActorProgramState(
        int actorCapacity,
        int sourceRuleCapacity,
        int sourceEntityCapacity,
        int relationshipCapacity,
        int mapCellCapacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(actorCapacity);
        NumericGuard.ThrowIfNegativeOrZero(sourceRuleCapacity);
        NumericGuard.ThrowIfNegativeOrZero(sourceEntityCapacity);
        NumericGuard.ThrowIfNegativeOrZero(relationshipCapacity);
        NumericGuard.ThrowIfNegativeOrZero(mapCellCapacity);
        ActorCapacity = actorCapacity;
        EntityCapacity = sourceEntityCapacity;
        RelationshipCapacity = relationshipCapacity;
        (_active, _generations, _cultureIds, _sourceEntityIds) = (
            new bool[actorCapacity + 1],
            new int[actorCapacity + 1],
            new int[actorCapacity + 1],
            new int[actorCapacity + 1]);
        (_sourceEntityOccurrenceHandles, _sourceRuleIds, _bindingRelationshipIds) = (
            new int[actorCapacity + 1],
            new int[actorCapacity + 1],
            new int[actorCapacity + 1]);
        (_bindingRelationshipOccurrenceHandles, _roleCodes, _planeCodes) = (
            new int[actorCapacity + 1],
            new int[actorCapacity + 1],
            new int[actorCapacity + 1]);
        (_q, _r, _previousQ, _previousR) = (
            new int[actorCapacity + 1],
            new int[actorCapacity + 1],
            new int[actorCapacity + 1],
            new int[actorCapacity + 1]);
        (_targetQ, _targetR, _targetingCodes, _postureCodes) = (
            new int[actorCapacity + 1],
            new int[actorCapacity + 1],
            new int[actorCapacity + 1],
            new int[actorCapacity + 1]);
        (_createdAtTicks, _nextReplanTicks, _commitmentUntilTicks) = (
            new int[actorCapacity + 1],
            new int[actorCapacity + 1],
            new int[actorCapacity + 1]);
        (_strengths, _workCharges) = (
            new FixedScalar[actorCapacity + 1],
            new FixedScalar[actorCapacity + 1]);
        (_cellIds, _nextActorByCell, _previousActorByCell) = (
            new int[actorCapacity + 1],
            new int[actorCapacity + 1],
            new int[actorCapacity + 1]);
        (_firstActorByCell, _activeActorIds, _activeActorPositions, _freeActorIds) = (
            new int[mapCellCapacity + 1],
            new int[actorCapacity],
            new int[actorCapacity + 1],
            new int[actorCapacity]);
        _sourceRuleStride = sourceRuleCapacity + 1;
        _actorBySourceRule = new int[checked((sourceEntityCapacity + 1) * _sourceRuleStride)];
        _actorByRelationship = new int[relationshipCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.MinorActors;

    public int SchemaVersion => 7;

    public int ActorCapacity { get; }

    public int EntityCapacity { get; }

    public int RelationshipCapacity { get; }

    public int HighWatermark { get; private set; }

    public int ActiveCount { get; private set; }

    public int TotalCreated { get; private set; }

    public int TotalRetired { get; private set; }

    public int ActiveActorAt(int index) =>
        (uint)index < (uint)ActiveCount
            ? _activeActorIds[index]
            : throw new global::System.ArgumentOutOfRangeException(nameof(index));

    public bool IsActive(int actorId) =>
        (uint)actorId < (uint)_active.Length && _active[actorId];

    public int OccurrenceHandle(int actorId)
    {
        EnsureActive(actorId);
        return checked((_generations[actorId] * ActorCapacity) + actorId);
    }

    public bool TryResolveOccurrence(int occurrenceHandle, out int actorId)
    {
        if (occurrenceHandle <= 0)
        {
            actorId = 0;
            return false;
        }

        actorId = ((occurrenceHandle - 1) % ActorCapacity) + 1;
        var generation = (occurrenceHandle - actorId) / ActorCapacity;
        return _active[actorId] && _generations[actorId] == generation;
    }

    public int CultureId(int actorId) => ValueAt(_cultureIds, actorId);

    public int SourceEntityId(int actorId) => ValueAt(_sourceEntityIds, actorId);

    public int SourceEntityOccurrenceHandle(int actorId) =>
        ValueAt(_sourceEntityOccurrenceHandles, actorId);

    public int SourceRuleId(int actorId) => ValueAt(_sourceRuleIds, actorId);

    public int BindingRelationshipId(int actorId) =>
        ValueAt(_bindingRelationshipIds, actorId);

    public int BindingRelationshipOccurrenceHandle(int actorId) =>
        ValueAt(_bindingRelationshipOccurrenceHandles, actorId);

    public int RoleCode(int actorId) => ValueAt(_roleCodes, actorId);

    public int PlaneCode(int actorId) => ValueAt(_planeCodes, actorId);

    public int Q(int actorId) => ValueAt(_q, actorId);

    public int R(int actorId) => ValueAt(_r, actorId);

    public int PreviousQ(int actorId) => ValueAt(_previousQ, actorId);

    public int PreviousR(int actorId) => ValueAt(_previousR, actorId);

    public int TargetQ(int actorId) => ValueAt(_targetQ, actorId);

    public int TargetR(int actorId) => ValueAt(_targetR, actorId);

    public int TargetingCode(int actorId) => ValueAt(_targetingCodes, actorId);

    public int PostureCode(int actorId) => ValueAt(_postureCodes, actorId);

    public int CreatedAtTick(int actorId) => ValueAt(_createdAtTicks, actorId);

    public int NextReplanTick(int actorId) => ValueAt(_nextReplanTicks, actorId);

    public int CommitmentUntilTick(int actorId) =>
        ValueAt(_commitmentUntilTicks, actorId);

    public FixedScalar Strength(int actorId)
    {
        EnsureActive(actorId);
        return _strengths[actorId];
    }

    public FixedScalar WorkCharge(int actorId)
    {
        EnsureActive(actorId);
        return _workCharges[actorId];
    }

    public int FirstActorInCell(int cellId) =>
        (uint)cellId < (uint)_firstActorByCell.Length
            ? _firstActorByCell[cellId]
            : 0;

    public int NextActorInCell(int actorId) => ValueAt(_nextActorByCell, actorId);

    public int ActorForSourceRule(int sourceEntityId, int sourceRuleId)
    {
        var index = SourceRuleIndex(sourceEntityId, sourceRuleId);
        var handle = _actorBySourceRule[index];
        if (TryResolveOccurrence(handle, out var actorId))
        {
            return actorId;
        }

        _actorBySourceRule[index] = 0;
        return 0;
    }

    public int ActorForRelationship(int relationshipId)
    {
        EnsureRelationshipId(relationshipId);
        var handle = _actorByRelationship[relationshipId];
        if (TryResolveOccurrence(handle, out var actorId))
        {
            return actorId;
        }

        _actorByRelationship[relationshipId] = 0;
        return 0;
    }

    public int Create(
        int cultureId,
        int sourceEntityId,
        int sourceEntityOccurrenceHandle,
        int sourceRuleId,
        int bindingRelationshipId,
        int bindingRelationshipOccurrenceHandle,
        int roleCode,
        int planeCode,
        int q,
        int r,
        int cellId,
        FixedScalar strength,
        int tick)
    {
        ValidateCreate(
            cultureId,
            sourceEntityId,
            sourceEntityOccurrenceHandle,
            sourceRuleId,
            bindingRelationshipId,
            bindingRelationshipOccurrenceHandle,
            roleCode,
            planeCode,
            cellId,
            strength,
            tick);
        if (roleCode == MinorActorRoleCodes.CampaignColumn)
        {
            if (ActorForRelationship(bindingRelationshipId) > 0)
            {
                throw new global::System.InvalidOperationException();
            }
        }
        else if (ActorForSourceRule(sourceEntityId, sourceRuleId) > 0)
        {
            throw new global::System.InvalidOperationException();
        }

        var actorId = FindFreeActorSlot();
        _active[actorId] = true;
        _cultureIds[actorId] = cultureId;
        _sourceEntityIds[actorId] = sourceEntityId;
        _sourceEntityOccurrenceHandles[actorId] = sourceEntityOccurrenceHandle;
        _sourceRuleIds[actorId] = sourceRuleId;
        _bindingRelationshipIds[actorId] = bindingRelationshipId;
        _bindingRelationshipOccurrenceHandles[actorId] = bindingRelationshipOccurrenceHandle;
        _roleCodes[actorId] = roleCode;
        _planeCodes[actorId] = planeCode;
        _q[actorId] = q;
        _r[actorId] = r;
        _previousQ[actorId] = q;
        _previousR[actorId] = r;
        _createdAtTicks[actorId] = tick;
        _nextReplanTicks[actorId] = tick;
        _postureCodes[actorId] = MinorActorPostureCodes.Unassigned;
        _strengths[actorId] = strength;
        _workCharges[actorId] = strength;
        LinkToCell(actorId, cellId);
        AddActiveActor(actorId);
        TotalCreated++;

        var handle = OccurrenceHandle(actorId);
        if (roleCode == MinorActorRoleCodes.CampaignColumn)
        {
            _actorByRelationship[bindingRelationshipId] = handle;
        }
        else
        {
            _actorBySourceRule[SourceRuleIndex(sourceEntityId, sourceRuleId)] = handle;
        }

        return actorId;
    }

    public bool Retire(int actorId)
    {
        if (!IsActive(actorId))
        {
            return false;
        }

        var sourceIndex = SourceRuleIndex(_sourceEntityIds[actorId], _sourceRuleIds[actorId]);
        var relationshipId = _bindingRelationshipIds[actorId];
        var handle = OccurrenceHandle(actorId);
        if (_actorBySourceRule[sourceIndex] == handle)
        {
            _actorBySourceRule[sourceIndex] = 0;
        }

        if (relationshipId > 0 && _actorByRelationship[relationshipId] == handle)
        {
            _actorByRelationship[relationshipId] = 0;
        }

        UnlinkFromCell(actorId);
        RemoveActiveActor(actorId);
        _active[actorId] = false;
        ClearActor(actorId);
        var maximum = (global::System.Int32.MaxValue - actorId) / ActorCapacity;
        if (_generations[actorId] >= maximum)
        {
            throw new global::System.InvalidOperationException();
        }

        _generations[actorId]++;
        _freeActorIds[_freeActorCount++] = actorId;
        TotalRetired++;
        return true;
    }

    public void SetAssignment(
        int actorId,
        int targetQ,
        int targetR,
        int targetingCode,
        int nextReplanTick,
        int commitmentUntilTick)
    {
        EnsureActive(actorId);
        if (targetingCode is not (
                MinorActorTargetingCodes.ExtendBorder or
                MinorActorTargetingCodes.BridgeGap or
                MinorActorTargetingCodes.ContestCulture or
                MinorActorTargetingCodes.MaintainRoad or
                MinorActorTargetingCodes.AdvanceCampaign))
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(targetingCode));
        }

        NumericGuard.ThrowIfNegative(nextReplanTick);
        NumericGuard.ThrowIfNegative(commitmentUntilTick);
        _targetQ[actorId] = targetQ;
        _targetR[actorId] = targetR;
        _targetingCodes[actorId] = targetingCode;
        _nextReplanTicks[actorId] = nextReplanTick;
        _commitmentUntilTicks[actorId] = commitmentUntilTick;
        _postureCodes[actorId] = _q[actorId] == targetQ && _r[actorId] == targetR
            ? MinorActorPostureCodes.Working
            : MinorActorPostureCodes.Traveling;
    }

    public void ClearAssignment(int actorId, int nextReplanTick)
    {
        EnsureActive(actorId);
        NumericGuard.ThrowIfNegative(nextReplanTick);
        _targetQ[actorId] = 0;
        _targetR[actorId] = 0;
        _targetingCodes[actorId] = 0;
        _nextReplanTicks[actorId] = nextReplanTick;
        _commitmentUntilTicks[actorId] = 0;
        _postureCodes[actorId] = MinorActorPostureCodes.Unassigned;
    }

    public void SetPosition(int actorId, int q, int r, int cellId)
    {
        EnsureActive(actorId);
        if (_cellIds[actorId] != cellId)
        {
            UnlinkFromCell(actorId);
            LinkToCell(actorId, cellId);
        }

        _previousQ[actorId] = _q[actorId];
        _previousR[actorId] = _r[actorId];
        _q[actorId] = q;
        _r[actorId] = r;
        _postureCodes[actorId] = q == _targetQ[actorId] && r == _targetR[actorId]
            ? MinorActorPostureCodes.Working
            : MinorActorPostureCodes.Traveling;
    }

    public void SetPosture(int actorId, int postureCode)
    {
        EnsureActive(actorId);
        if (postureCode is < MinorActorPostureCodes.Unassigned or > MinorActorPostureCodes.Disrupted)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(postureCode));
        }

        _postureCodes[actorId] = postureCode;
    }

    public void SetStrength(int actorId, FixedScalar strength)
    {
        EnsureActive(actorId);
        NumericGuard.ThrowIfNegative(strength);
        _strengths[actorId] = strength;
    }

    public void SetWorkCharge(int actorId, FixedScalar workCharge)
    {
        EnsureActive(actorId);
        NumericGuard.ThrowIfNegative(workCharge);
        _workCharges[actorId] = workCharge;
    }

    private void AddActiveActor(int actorId)
    {
        var handle = OccurrenceHandle(actorId);
        var position = ActiveCount;
        while (position > 0 && OccurrenceHandle(_activeActorIds[position - 1]) > handle)
        {
            var moved = _activeActorIds[position - 1];
            _activeActorIds[position] = moved;
            _activeActorPositions[moved] = position + 1;
            position--;
        }

        _activeActorIds[position] = actorId;
        _activeActorPositions[actorId] = position + 1;
        ActiveCount++;
    }

    private void RemoveActiveActor(int actorId)
    {
        var position = _activeActorPositions[actorId] - 1;
        for (var index = position; index < ActiveCount - 1; index++)
        {
            var moved = _activeActorIds[index + 1];
            _activeActorIds[index] = moved;
            _activeActorPositions[moved] = index + 1;
        }

        _activeActorIds[ActiveCount - 1] = 0;
        _activeActorPositions[actorId] = 0;
        ActiveCount--;
    }

    private int FindFreeActorSlot()
    {
        if (_freeActorCount > 0)
        {
            return _freeActorIds[--_freeActorCount];
        }

        if (HighWatermark >= ActorCapacity)
        {
            throw new global::System.InvalidOperationException();
        }

        return ++HighWatermark;
    }

    private void LinkToCell(int actorId, int cellId)
    {
        if ((uint)cellId == 0u || cellId >= _firstActorByCell.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(cellId));
        }

        var first = _firstActorByCell[cellId];
        _cellIds[actorId] = cellId;
        _previousActorByCell[actorId] = 0;
        _nextActorByCell[actorId] = first;
        if (first > 0)
        {
            _previousActorByCell[first] = actorId;
        }

        _firstActorByCell[cellId] = actorId;
    }

    private void UnlinkFromCell(int actorId)
    {
        var cellId = _cellIds[actorId];
        if (cellId == 0)
        {
            return;
        }

        var previous = _previousActorByCell[actorId];
        var next = _nextActorByCell[actorId];
        if (previous > 0)
        {
            _nextActorByCell[previous] = next;
        }
        else
        {
            _firstActorByCell[cellId] = next;
        }

        if (next > 0)
        {
            _previousActorByCell[next] = previous;
        }

        _cellIds[actorId] = 0;
        _previousActorByCell[actorId] = 0;
        _nextActorByCell[actorId] = 0;
    }

    private void ClearActor(int actorId)
    {
        _cultureIds[actorId] = 0;
        _sourceEntityIds[actorId] = 0;
        _sourceEntityOccurrenceHandles[actorId] = 0;
        _sourceRuleIds[actorId] = 0;
        _bindingRelationshipIds[actorId] = 0;
        _bindingRelationshipOccurrenceHandles[actorId] = 0;
        _roleCodes[actorId] = 0;
        _planeCodes[actorId] = 0;
        _q[actorId] = 0;
        _r[actorId] = 0;
        _previousQ[actorId] = 0;
        _previousR[actorId] = 0;
        _targetQ[actorId] = 0;
        _targetR[actorId] = 0;
        _targetingCodes[actorId] = 0;
        _postureCodes[actorId] = 0;
        _createdAtTicks[actorId] = 0;
        _nextReplanTicks[actorId] = 0;
        _commitmentUntilTicks[actorId] = 0;
        _strengths[actorId] = 0;
        _workCharges[actorId] = 0;
    }

    private int SourceRuleIndex(int sourceEntityId, int sourceRuleId)
    {
        EnsureSourceEntityId(sourceEntityId);
        if ((uint)sourceRuleId == 0u || sourceRuleId >= _sourceRuleStride)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(sourceRuleId));
        }

        return checked((sourceEntityId * _sourceRuleStride) + sourceRuleId);
    }

    private void EnsureSourceEntityId(int sourceEntityId)
    {
        if ((uint)sourceEntityId == 0u || sourceEntityId > EntityCapacity)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(sourceEntityId));
        }
    }

    private void EnsureRelationshipId(int relationshipId)
    {
        if ((uint)relationshipId == 0u || relationshipId > RelationshipCapacity)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(relationshipId));
        }
    }

    private void EnsureActive(int actorId)
    {
        if (!IsActive(actorId))
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(actorId));
        }
    }

    private static int ValueAt(int[] values, int actorId)
    {
        if ((uint)actorId == 0u || actorId >= values.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(actorId));
        }

        return values[actorId];
    }

    private void ValidateCreate(
        int cultureId,
        int sourceEntityId,
        int sourceEntityOccurrenceHandle,
        int sourceRuleId,
        int bindingRelationshipId,
        int bindingRelationshipOccurrenceHandle,
        int roleCode,
        int planeCode,
        int cellId,
        FixedScalar strength,
        int tick)
    {
        NumericGuard.ThrowIfNegativeOrZero(cultureId);
        EnsureSourceEntityId(sourceEntityId);
        NumericGuard.ThrowIfNegativeOrZero(sourceEntityOccurrenceHandle);
        if ((uint)sourceRuleId == 0u || sourceRuleId >= _sourceRuleStride)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(sourceRuleId));
        }

        ValidateCreateRole(roleCode, bindingRelationshipId, bindingRelationshipOccurrenceHandle);
        ValidateCreateLocation(planeCode, cellId);
        NumericGuard.ThrowIfNegativeOrZero(strength);
        NumericGuard.ThrowIfNegative(tick);
    }

    private void ValidateCreateRole(
        int roleCode,
        int bindingRelationshipId,
        int bindingRelationshipOccurrenceHandle)
    {
        if (roleCode is not (
                MinorActorRoleCodes.CulturalFront or
                MinorActorRoleCodes.RoadCrew or
                MinorActorRoleCodes.CampaignColumn))
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(roleCode));
        }

        if (roleCode == MinorActorRoleCodes.CampaignColumn)
        {
            EnsureRelationshipId(bindingRelationshipId);
            NumericGuard.ThrowIfNegativeOrZero(bindingRelationshipOccurrenceHandle);
        }
        else if (bindingRelationshipId != 0 || bindingRelationshipOccurrenceHandle != 0)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(bindingRelationshipId));
        }
    }

    private void ValidateCreateLocation(int planeCode, int cellId)
    {
        if (planeCode is not (PlaneCodes.Physical or PlaneCodes.Semantic))
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(planeCode));
        }

        if ((uint)cellId == 0u || cellId >= _firstActorByCell.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(cellId));
        }
    }
}
