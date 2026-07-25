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
    private readonly int[] _expiresAtPulses;
    private readonly int[] _createdAtTicks;
    private readonly int[] _nextActionPulses;
    private readonly int[] _actionCadencePulses;
    private readonly int[] _purposeCodes;
    private readonly FixedScalar[] _workDeltas;
    private readonly int[] _activeActorIds;
    private readonly int[] _activeActorPositions;
    private readonly int[] _freeActorIds;
    private readonly int[] _dueHeapActorIds;
    private readonly int[] _dueHeapPulses;
    private readonly int[] _dueHeapPositions;
    private readonly int[] _sourceActiveCounts;
    private readonly int[] _nextEmissionPulses;
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
        _expiresAtPulses = new int[actorCapacity + 1];
        _createdAtTicks = new int[actorCapacity + 1];
        _nextActionPulses = new int[actorCapacity + 1];
        _actionCadencePulses = new int[actorCapacity + 1];
        _purposeCodes = new int[actorCapacity + 1];
        _workDeltas = new FixedScalar[actorCapacity + 1];
        _activeActorIds = new int[actorCapacity];
        _activeActorPositions = new int[actorCapacity + 1];
        _freeActorIds = new int[actorCapacity];
        _dueHeapActorIds = new int[actorCapacity + 1];
        _dueHeapPulses = new int[actorCapacity + 1];
        _dueHeapPositions = new int[actorCapacity + 1];
        _sourceRuleStride = sourceRuleCapacity + 1;
        var sourceStateLength = checked((sourceEntityCapacity + 1) * _sourceRuleStride);
        _sourceActiveCounts = new int[sourceStateLength];
        _nextEmissionPulses = new int[sourceStateLength];
    }

    public int ProgramId => KernelProgramCodes.MinorActors;

    public int SchemaVersion => 5;

    public int ActorCapacity { get; }

    public int HighWatermark { get; private set; }

    public int ActiveCount { get; private set; }

    public int Pulse { get; private set; }

    public int TotalSpawned { get; private set; }

    public int TotalExpired { get; private set; }

    public int AdvancePulse()
    {
        Pulse++;
        return Pulse;
    }

    public bool IsActive(int actorId) =>
        (uint)actorId < (uint)_active.Length && _active[actorId];

    public int ActorKindCode(int actorId) => ValueAt(_actorKindCodes, actorId);

    public int CultureId(int actorId) => ValueAt(_cultureIds, actorId);

    public int SourceEntityId(int actorId) => ValueAt(_sourceEntityIds, actorId);

    public int SourceRuleId(int actorId) => ValueAt(_sourceRuleIds, actorId);

    public int PlaneCode(int actorId) => ValueAt(_planeCodes, actorId);

    public int Q(int actorId) => ValueAt(_q, actorId);

    public int R(int actorId) => ValueAt(_r, actorId);

    public int RemainingPulses(int actorId) =>
        global::System.Math.Max(0, ValueAt(_expiresAtPulses, actorId) - Pulse + 1);

    public int ExpiresAtPulse(int actorId) => ValueAt(_expiresAtPulses, actorId);

    public int CreatedAtTick(int actorId) => ValueAt(_createdAtTicks, actorId);

    public int NextActionPulse(int actorId) => ValueAt(_nextActionPulses, actorId);

    public int ActionCadencePulses(int actorId) => ValueAt(_actionCadencePulses, actorId);

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

    public int NextEmissionPulse(int sourceEntityId, int sourceRuleId)
    {
        if (sourceEntityId <= 0 || sourceRuleId <= 0)
        {
            return 0;
        }

        return _nextEmissionPulses[SourceRuleIndex(sourceEntityId, sourceRuleId)];
    }

    public void SetNextEmissionPulse(int sourceEntityId, int sourceRuleId, int pulse)
    {
        NumericGuard.ThrowIfNegative(pulse);
        _nextEmissionPulses[SourceRuleIndex(sourceEntityId, sourceRuleId)] = pulse;
    }

    public int Spawn(
        int cultureId,
        int sourceEntityId,
        int sourceRuleId,
        int actorKindCode,
        int planeCode,
        int q,
        int r,
        int ttlPulses,
        int actionCadencePulses,
        FixedScalar workDelta,
        int tick,
        int pulse,
        int purposeCode = MinorActorPurposeCodes.CulturalLabor)
    {
        ValidateSpawn(
            cultureId,
            sourceEntityId,
            sourceRuleId,
            purposeCode,
            actorKindCode,
            planeCode,
            ttlPulses,
            actionCadencePulses,
            workDelta,
            tick,
            pulse);

        var actorId = FindFreeActorSlot();
        _active[actorId] = true;
        _actorKindCodes[actorId] = actorKindCode;
        _cultureIds[actorId] = cultureId;
        _sourceEntityIds[actorId] = sourceEntityId;
        _sourceRuleIds[actorId] = sourceRuleId;
        _planeCodes[actorId] = planeCode;
        _q[actorId] = q;
        _r[actorId] = r;
        _expiresAtPulses[actorId] = checked(pulse + ttlPulses - 1);
        _createdAtTicks[actorId] = tick;
        _nextActionPulses[actorId] = pulse;
        _actionCadencePulses[actorId] = actionCadencePulses;
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
        _expiresAtPulses[actorId] = 0;
        TotalExpired++;
        _sourceActiveCounts[SourceRuleIndex(sourceEntityId, sourceRuleId)]--;

        _freeActorIds[_freeActorCount++] = actorId;
        return true;
    }

    public void SetPosition(int actorId, int q, int r)
    {
        EnsureActorId(actorId);
        _q[actorId] = q;
        _r[actorId] = r;
    }

    public void SetNextActionPulse(int actorId, int pulse)
    {
        EnsureActorId(actorId);
        NumericGuard.ThrowIfNegative(pulse);
        _nextActionPulses[actorId] = pulse;
    }

    public bool TryTakeDueActor(int pulse, out int actorId)
    {
        NumericGuard.ThrowIfNegative(pulse);
        if (_dueHeapCount == 0 || _dueHeapPulses[1] > pulse)
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

        var duePulse = global::System.Math.Min(
            _nextActionPulses[actorId],
            _expiresAtPulses[actorId]);
        var position = _dueHeapPositions[actorId];
        if (position > 0)
        {
            _dueHeapPulses[position] = duePulse;
            SiftUp(position);
            SiftDown(_dueHeapPositions[actorId]);
            return;
        }

        var added = ++_dueHeapCount;
        _dueHeapActorIds[added] = actorId;
        _dueHeapPulses[added] = duePulse;
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
        var lastPulse = _dueHeapPulses[_dueHeapCount];
        _dueHeapActorIds[position] = lastActorId;
        _dueHeapPulses[position] = lastPulse;
        _dueHeapPositions[lastActorId] = position;
        _dueHeapActorIds[_dueHeapCount] = 0;
        _dueHeapPulses[_dueHeapCount] = 0;
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
        var leftPulse = _dueHeapPulses[leftPosition];
        var rightPulse = _dueHeapPulses[rightPosition];
        return leftPulse < rightPulse ||
            (leftPulse == rightPulse && _dueHeapActorIds[leftPosition] < _dueHeapActorIds[rightPosition]);
    }

    private void SwapDueEntries(int left, int right)
    {
        (_dueHeapActorIds[left], _dueHeapActorIds[right]) =
            (_dueHeapActorIds[right], _dueHeapActorIds[left]);
        (_dueHeapPulses[left], _dueHeapPulses[right]) =
            (_dueHeapPulses[right], _dueHeapPulses[left]);
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
        int ttlPulses,
        int actionCadencePulses,
        FixedScalar workDelta,
        int tick,
        int pulse)
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

        NumericGuard.ThrowIfNegativeOrZero(ttlPulses);
        NumericGuard.ThrowIfNegativeOrZero(actionCadencePulses);
        NumericGuard.ThrowIfNegative(workDelta);
        NumericGuard.ThrowIfNegative(tick);
        NumericGuard.ThrowIfNegative(pulse);
    }
}
