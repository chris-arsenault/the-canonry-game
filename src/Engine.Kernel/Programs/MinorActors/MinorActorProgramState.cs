namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class MinorActorProgramState : IKernelProgramState
{
    private readonly bool[] _active;
    private readonly int[] _actorKindCodes;
    private readonly int[] _cultureIds;
    private readonly int[] _sourceEntityIds;
    private readonly int[] _sourceRuleIds;
    private readonly int[] _planeCodes;
    private readonly int[] _q;
    private readonly int[] _r;
    private readonly int[] _expiresAtTicks;
    private readonly int[] _createdAtTicks;
    private readonly int[] _nextActionTicks;
    private readonly int[] _actionCadenceTicks;
    private readonly int[] _purposeCodes;
    private readonly FixedScalar[] _workDeltas;
    private readonly int[] _activeActorIds;
    private readonly int[] _activeActorPositions;
    private readonly int[] _freeActorIds;
    private readonly int[] _dueHeapActorIds;
    private readonly int[] _dueHeapTicks;
    private readonly int[] _dueHeapPositions;
    private readonly int[] _sourceActiveCounts;
    private readonly int[] _nextEmissionTicks;
    private readonly int _sourceRuleStride;
    private int _freeActorCount;
    private int _dueHeapCount;

    public MinorActorProgramState(
        int actorCapacity,
        int sourceRuleCapacity,
        int sourceEntityCapacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(actorCapacity);
        NumericGuard.ThrowIfNegativeOrZero(sourceRuleCapacity);
        NumericGuard.ThrowIfNegativeOrZero(sourceEntityCapacity);
        ActorCapacity = actorCapacity;
        _active = new bool[actorCapacity + 1];
        _actorKindCodes = new int[actorCapacity + 1];
        _cultureIds = new int[actorCapacity + 1];
        _sourceEntityIds = new int[actorCapacity + 1];
        _sourceRuleIds = new int[actorCapacity + 1];
        _planeCodes = new int[actorCapacity + 1];
        _q = new int[actorCapacity + 1];
        _r = new int[actorCapacity + 1];
        _expiresAtTicks = new int[actorCapacity + 1];
        _createdAtTicks = new int[actorCapacity + 1];
        _nextActionTicks = new int[actorCapacity + 1];
        _actionCadenceTicks = new int[actorCapacity + 1];
        _purposeCodes = new int[actorCapacity + 1];
        _workDeltas = new FixedScalar[actorCapacity + 1];
        _activeActorIds = new int[actorCapacity];
        _activeActorPositions = new int[actorCapacity + 1];
        _freeActorIds = new int[actorCapacity];
        _dueHeapActorIds = new int[actorCapacity + 1];
        _dueHeapTicks = new int[actorCapacity + 1];
        _dueHeapPositions = new int[actorCapacity + 1];
        _sourceRuleStride = sourceRuleCapacity + 1;
        var sourceStateLength = checked((sourceEntityCapacity + 1) * _sourceRuleStride);
        _sourceActiveCounts = new int[sourceStateLength];
        _nextEmissionTicks = new int[sourceStateLength];
    }

    public int ProgramId => KernelProgramCodes.MinorActors;

    public int SchemaVersion => 6;

    public int ActorCapacity { get; }

    public int HighWatermark { get; private set; }

    public int ActiveCount { get; private set; }

    public int TotalSpawned { get; private set; }

    public int TotalExpired { get; private set; }

    public bool IsActive(int actorId) =>
        (uint)actorId < (uint)_active.Length && _active[actorId];

    public int ActorKindCode(int actorId) => ValueAt(_actorKindCodes, actorId);

    public int CultureId(int actorId) => ValueAt(_cultureIds, actorId);

    public int SourceEntityId(int actorId) => ValueAt(_sourceEntityIds, actorId);

    public int SourceRuleId(int actorId) => ValueAt(_sourceRuleIds, actorId);

    public int PlaneCode(int actorId) => ValueAt(_planeCodes, actorId);

    public int Q(int actorId) => ValueAt(_q, actorId);

    public int R(int actorId) => ValueAt(_r, actorId);

    public int ExpiresAtTick(int actorId) => ValueAt(_expiresAtTicks, actorId);

    public int CreatedAtTick(int actorId) => ValueAt(_createdAtTicks, actorId);

    public int NextActionTick(int actorId) => ValueAt(_nextActionTicks, actorId);

    public int ActionCadenceTicks(int actorId) => ValueAt(_actionCadenceTicks, actorId);

    public int PurposeCode(int actorId) => ValueAt(_purposeCodes, actorId);

    public FixedScalar WorkDelta(int actorId)
    {
        EnsureActorId(actorId);
        return _workDeltas[actorId];
    }

    public int SourceActiveCount(int sourceEntityId, int sourceRuleId)
    {
        if (sourceEntityId <= 0 || sourceRuleId <= 0)
        {
            return 0;
        }

        return _sourceActiveCounts[SourceRuleIndex(sourceEntityId, sourceRuleId)];
    }

    public int NextEmissionTick(int sourceEntityId, int sourceRuleId)
    {
        if (sourceEntityId <= 0 || sourceRuleId <= 0)
        {
            return 0;
        }

        return _nextEmissionTicks[SourceRuleIndex(sourceEntityId, sourceRuleId)];
    }

    public void SetNextEmissionTick(int sourceEntityId, int sourceRuleId, int tick)
    {
        NumericGuard.ThrowIfNegative(tick);
        _nextEmissionTicks[SourceRuleIndex(sourceEntityId, sourceRuleId)] = tick;
    }

    public int Spawn(
        int cultureId,
        int sourceEntityId,
        int sourceRuleId,
        int actorKindCode,
        int planeCode,
        int q,
        int r,
        int ttlTicks,
        int actionCadenceTicks,
        FixedScalar workDelta,
        int tick,
        int purposeCode = MinorActorPurposeCodes.CulturalLabor)
    {
        ValidateSpawn(
            cultureId,
            sourceEntityId,
            sourceRuleId,
            purposeCode,
            actorKindCode,
            planeCode,
            ttlTicks,
            actionCadenceTicks,
            workDelta,
            tick);

        var actorId = FindFreeActorSlot();
        _active[actorId] = true;
        _actorKindCodes[actorId] = actorKindCode;
        _cultureIds[actorId] = cultureId;
        _sourceEntityIds[actorId] = sourceEntityId;
        _sourceRuleIds[actorId] = sourceRuleId;
        _planeCodes[actorId] = planeCode;
        _q[actorId] = q;
        _r[actorId] = r;
        _expiresAtTicks[actorId] = checked(tick + ttlTicks - 1);
        _createdAtTicks[actorId] = tick;
        _nextActionTicks[actorId] = tick;
        _actionCadenceTicks[actorId] = actionCadenceTicks;
        _purposeCodes[actorId] = purposeCode;
        _workDeltas[actorId] = workDelta;
        AddActiveActor(actorId);
        Schedule(actorId);
        TotalSpawned++;
        _sourceActiveCounts[SourceRuleIndex(sourceEntityId, sourceRuleId)]++;

        return actorId;
    }

    public bool Deactivate(int actorId)
    {
        if (!IsActive(actorId))
        {
            return false;
        }

        var sourceEntityId = _sourceEntityIds[actorId];
        var sourceRuleId = _sourceRuleIds[actorId];
        RemoveDueActor(actorId);
        RemoveActiveActor(actorId);
        _active[actorId] = false;
        _actorKindCodes[actorId] = 0;
        _cultureIds[actorId] = 0;
        _sourceEntityIds[actorId] = 0;
        _sourceRuleIds[actorId] = 0;
        _planeCodes[actorId] = 0;
        _q[actorId] = 0;
        _r[actorId] = 0;
        _expiresAtTicks[actorId] = 0;
        _createdAtTicks[actorId] = 0;
        _nextActionTicks[actorId] = 0;
        _actionCadenceTicks[actorId] = 0;
        _purposeCodes[actorId] = 0;
        _workDeltas[actorId] = 0;
        TotalExpired++;
        _sourceActiveCounts[SourceRuleIndex(sourceEntityId, sourceRuleId)]--;

        _freeActorIds[_freeActorCount++] = actorId;
        return true;
    }

    public void DeactivateSource(int sourceEntityId)
    {
        EnsureSourceEntity(sourceEntityId);
        for (var position = 0; position < ActiveCount;)
        {
            var actorId = _activeActorIds[position];
            if (_sourceEntityIds[actorId] == sourceEntityId)
            {
                Deactivate(actorId);
                continue;
            }

            position++;
        }

        global::System.Array.Clear(
            _nextEmissionTicks,
            sourceEntityId * _sourceRuleStride,
            _sourceRuleStride);
    }

    public void SetPosition(int actorId, int q, int r)
    {
        EnsureActorId(actorId);
        _q[actorId] = q;
        _r[actorId] = r;
    }

    public void SetNextActionTick(int actorId, int tick)
    {
        EnsureActorId(actorId);
        NumericGuard.ThrowIfNegative(tick);
        _nextActionTicks[actorId] = tick;
    }

    public bool TryTakeDueActor(int tick, out int actorId)
    {
        NumericGuard.ThrowIfNegative(tick);
        if (_dueHeapCount == 0 || _dueHeapTicks[1] > tick)
        {
            actorId = 0;
            return false;
        }

        actorId = _dueHeapActorIds[1];
        RemoveDueActor(actorId);
        return IsActive(actorId);
    }

    public void Schedule(int actorId)
    {
        EnsureActorId(actorId);
        if (!IsActive(actorId))
        {
            return;
        }

        var dueTick = global::System.Math.Min(
            _nextActionTicks[actorId],
            _expiresAtTicks[actorId]);
        var position = _dueHeapPositions[actorId];
        if (position > 0)
        {
            _dueHeapTicks[position] = dueTick;
            SiftUp(position);
            SiftDown(_dueHeapPositions[actorId]);
            return;
        }

        var added = ++_dueHeapCount;
        _dueHeapActorIds[added] = actorId;
        _dueHeapTicks[added] = dueTick;
        _dueHeapPositions[actorId] = added;
        SiftUp(added);
    }

    private void AddActiveActor(int actorId)
    {
        _activeActorIds[ActiveCount] = actorId;
        _activeActorPositions[actorId] = ActiveCount + 1;
        ActiveCount++;
    }

    private void RemoveActiveActor(int actorId)
    {
        var position = _activeActorPositions[actorId] - 1;
        var lastPosition = ActiveCount - 1;
        var movedActorId = _activeActorIds[lastPosition];
        _activeActorIds[position] = movedActorId;
        _activeActorPositions[movedActorId] = position + 1;
        _activeActorIds[lastPosition] = 0;
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

        HighWatermark++;
        return HighWatermark;
    }

    private void RemoveDueActor(int actorId)
    {
        var position = _dueHeapPositions[actorId];
        if (position == 0)
        {
            return;
        }

        var lastActorId = _dueHeapActorIds[_dueHeapCount];
        var lastTick = _dueHeapTicks[_dueHeapCount];
        _dueHeapActorIds[position] = lastActorId;
        _dueHeapTicks[position] = lastTick;
        _dueHeapPositions[lastActorId] = position;
        _dueHeapActorIds[_dueHeapCount] = 0;
        _dueHeapTicks[_dueHeapCount] = 0;
        _dueHeapPositions[actorId] = 0;
        _dueHeapCount--;
        if (position <= _dueHeapCount)
        {
            SiftUp(position);
            SiftDown(_dueHeapPositions[lastActorId]);
        }
    }

    private void SiftUp(int position)
    {
        while (position > 1)
        {
            var parent = position / 2;
            if (!EntryComesBefore(position, parent))
            {
                return;
            }

            SwapDueEntries(position, parent);
            position = parent;
        }
    }

    private void SiftDown(int position)
    {
        while (position > 0)
        {
            var left = position * 2;
            if (left > _dueHeapCount)
            {
                return;
            }

            var right = left + 1;
            var selected = right <= _dueHeapCount && EntryComesBefore(right, left) ? right : left;
            if (!EntryComesBefore(selected, position))
            {
                return;
            }

            SwapDueEntries(position, selected);
            position = selected;
        }
    }

    private bool EntryComesBefore(int leftPosition, int rightPosition)
    {
        var leftTick = _dueHeapTicks[leftPosition];
        var rightTick = _dueHeapTicks[rightPosition];
        return leftTick < rightTick ||
            (leftTick == rightTick && _dueHeapActorIds[leftPosition] < _dueHeapActorIds[rightPosition]);
    }

    private void SwapDueEntries(int left, int right)
    {
        (_dueHeapActorIds[left], _dueHeapActorIds[right]) =
            (_dueHeapActorIds[right], _dueHeapActorIds[left]);
        (_dueHeapTicks[left], _dueHeapTicks[right]) =
            (_dueHeapTicks[right], _dueHeapTicks[left]);
        _dueHeapPositions[_dueHeapActorIds[left]] = left;
        _dueHeapPositions[_dueHeapActorIds[right]] = right;
    }

    private int SourceRuleIndex(int sourceEntityId, int sourceRuleId)
    {
        EnsureSourceEntity(sourceEntityId);
        if ((uint)sourceRuleId >= (uint)_sourceRuleStride)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(sourceRuleId));
        }

        return checked((sourceEntityId * _sourceRuleStride) + sourceRuleId);
    }

    private void EnsureSourceEntity(int sourceEntityId)
    {
        if ((uint)sourceEntityId == 0u ||
            sourceEntityId >= _sourceActiveCounts.Length / _sourceRuleStride)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(sourceEntityId));
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

    private void EnsureActorId(int actorId)
    {
        if ((uint)actorId == 0u || actorId >= _active.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(actorId));
        }
    }

    private static void ValidateSpawn(
        int cultureId,
        int sourceEntityId,
        int sourceRuleId,
        int purposeCode,
        int actorKindCode,
        int planeCode,
        int ttlTicks,
        int actionCadenceTicks,
        FixedScalar workDelta,
        int tick)
    {
        NumericGuard.ThrowIfNegativeOrZero(cultureId);
        NumericGuard.ThrowIfNegativeOrZero(sourceEntityId);
        NumericGuard.ThrowIfNegativeOrZero(sourceRuleId);
        if (purposeCode is not (
                MinorActorPurposeCodes.CulturalLabor or
                MinorActorPurposeCodes.RoadLabor or
                MinorActorPurposeCodes.CampaignLabor))
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(purposeCode));
        }

        NumericGuard.ThrowIfNegativeOrZero(actorKindCode);
        if (planeCode is not (Engine.Constants.PlaneCodes.Physical or Engine.Constants.PlaneCodes.Semantic))
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(planeCode));
        }

        NumericGuard.ThrowIfNegativeOrZero(ttlTicks);
        NumericGuard.ThrowIfNegativeOrZero(actionCadenceTicks);
        NumericGuard.ThrowIfNegative(workDelta);
        NumericGuard.ThrowIfNegative(tick);
    }
}
