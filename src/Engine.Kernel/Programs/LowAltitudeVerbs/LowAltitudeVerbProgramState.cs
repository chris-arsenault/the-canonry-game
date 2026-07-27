namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class LowAltitudeVerbProgramState : IKernelProgramState
{
    private readonly int[] _activeVerbSlots;
    private readonly int[] _intentIds;
    private readonly int[] _targetPlaneCodes;
    private readonly int[] _targetEntityOccurrenceHandles;
    private readonly int[] _targetQ;
    private readonly int[] _targetR;
    private readonly int[] _targetS;
    private readonly int[] _startedTicks;
    private readonly int[] _expiresTicks;
    private readonly int[] _dwellUntilTicks;
    private readonly int[] _reachedTicks;
    private readonly int[] _intentDwellTicks;
    private readonly int[] _nextStepTicks;
    private readonly int[] _stepIntervalTicks;
    private readonly int[] _pathLoosenessPermille;
    private readonly bool[] _initialInfluenceApplied;
    private readonly long[] _recentTargetKeys;
    private readonly int[] _recentTargetTicks;
    private readonly int[] _recentTargetCounts;
    private readonly int _recentTargetRingSize;

    public LowAltitudeVerbProgramState(int entityCapacity)
        : this(entityCapacity, recentTargetRingSize: 3)
    {
    }

    public LowAltitudeVerbProgramState(int entityCapacity, int recentTargetRingSize)
    {
        NumericGuard.ThrowIfNegativeOrZero(entityCapacity);
        NumericGuard.ThrowIfNegativeOrZero(recentTargetRingSize);
        var length = entityCapacity + 1;
        _recentTargetRingSize = recentTargetRingSize;
        _activeVerbSlots = new int[length];
        _intentIds = new int[length];
        _targetPlaneCodes = new int[length];
        _targetEntityOccurrenceHandles = new int[length];
        _targetQ = new int[length];
        _targetR = new int[length];
        _targetS = new int[length];
        _startedTicks = new int[length];
        _expiresTicks = new int[length];
        _dwellUntilTicks = new int[length];
        _reachedTicks = new int[length];
        _intentDwellTicks = new int[length];
        _nextStepTicks = new int[length];
        _stepIntervalTicks = new int[length];
        _pathLoosenessPermille = new int[length];
        _initialInfluenceApplied = new bool[length];
        _recentTargetKeys = new long[length * _recentTargetRingSize];
        _recentTargetTicks = new int[length * _recentTargetRingSize];
        _recentTargetCounts = new int[length];
    }

    public int ProgramId => KernelProgramCodes.LowAltitudeVerbs;

    public int SchemaVersion => 5;

    public int EntityCapacity => _activeVerbSlots.Length - 1;

    public int ActiveVerbSlot(int entityId) => _activeVerbSlots[entityId];

    public int IntentId(int entityId) => _intentIds[entityId];

    public int TargetPlaneCode(int entityId) => _targetPlaneCodes[entityId];

    public int TargetEntityOccurrenceHandle(int entityId) =>
        _targetEntityOccurrenceHandles[entityId];

    public int TargetQ(int entityId) => _targetQ[entityId];

    public int TargetR(int entityId) => _targetR[entityId];

    public int TargetS(int entityId) => _targetS[entityId];

    public int StartedTick(int entityId) => _startedTicks[entityId];

    public int ExpiresTick(int entityId) => _expiresTicks[entityId];

    public int DwellUntilTick(int entityId) => _dwellUntilTicks[entityId];

    public int ReachedTick(int entityId) => _reachedTicks[entityId];

    public int IntentDwellTicks(int entityId) => _intentDwellTicks[entityId];

    public int NextStepTick(int entityId) => _nextStepTicks[entityId];

    public int StepIntervalTicks(int entityId) => _stepIntervalTicks[entityId];

    public int PathLoosenessPermille(int entityId) => _pathLoosenessPermille[entityId];

    public bool InitialInfluenceApplied(int entityId) => _initialInfluenceApplied[entityId];

    public int NextIntentId(int entityId, int tick)
    {
        EnsureEntity(entityId);
        NumericGuard.ThrowIfNegative(tick);
        unchecked
        {
            var hash = (uint)entityId * 2654435761u;
            hash ^= (uint)tick * 2246822519u;
            hash ^= hash >> 16;
            return (int)(hash % int.MaxValue) + 1;
        }
    }

    public int RecentTargetCount(int entityId) => _recentTargetCounts[entityId];

    public long RecentTargetKey(int entityId, int slot)
    {
        EnsureRecentTargetSlot(slot);
        return _recentTargetKeys[(entityId * _recentTargetRingSize) + slot];
    }

    public int RecentTargetTick(int entityId, int slot)
    {
        EnsureRecentTargetSlot(slot);
        return _recentTargetTicks[(entityId * _recentTargetRingSize) + slot];
    }

    public void ClearIntent(int entityId)
    {
        EnsureEntity(entityId);
        _activeVerbSlots[entityId] = 0;
        _intentIds[entityId] = 0;
        _targetPlaneCodes[entityId] = 0;
        _targetEntityOccurrenceHandles[entityId] = 0;
        _targetQ[entityId] = 0;
        _targetR[entityId] = 0;
        _targetS[entityId] = 0;
        _startedTicks[entityId] = 0;
        _expiresTicks[entityId] = 0;
        _dwellUntilTicks[entityId] = 0;
        _reachedTicks[entityId] = 0;
        _intentDwellTicks[entityId] = 0;
        _nextStepTicks[entityId] = 0;
        _stepIntervalTicks[entityId] = 0;
        _pathLoosenessPermille[entityId] = 0;
    }

    public void ClearEntitySlot(int entityId)
    {
        ClearIntent(entityId);
        _initialInfluenceApplied[entityId] = false;
        _recentTargetCounts[entityId] = 0;
        global::System.Array.Clear(
            _recentTargetKeys,
            entityId * _recentTargetRingSize,
            _recentTargetRingSize);
        global::System.Array.Clear(
            _recentTargetTicks,
            entityId * _recentTargetRingSize,
            _recentTargetRingSize);
    }

    public int SetIntent(
        int entityId,
        int verbSlot,
        int targetPlaneCode,
        int targetEntityOccurrenceHandle,
        int q,
        int r,
        int s,
        int startedTick,
        int expiresTick,
        int dwellTicks,
        int stepIntervalTicks,
        int pathLoosenessPermille)
    {
        EnsureEntity(entityId);
        _activeVerbSlots[entityId] = verbSlot;
        _intentIds[entityId] = NextIntentId(entityId, startedTick);
        _targetPlaneCodes[entityId] = targetPlaneCode;
        _targetEntityOccurrenceHandles[entityId] =
            targetEntityOccurrenceHandle;
        _targetQ[entityId] = q;
        _targetR[entityId] = r;
        _targetS[entityId] = s;
        _startedTicks[entityId] = startedTick;
        _expiresTicks[entityId] = expiresTick;
        _dwellUntilTicks[entityId] = 0;
        _reachedTicks[entityId] = 0;
        _intentDwellTicks[entityId] = dwellTicks;
        _nextStepTicks[entityId] = startedTick;
        _stepIntervalTicks[entityId] = stepIntervalTicks;
        _pathLoosenessPermille[entityId] = pathLoosenessPermille;
        return _intentIds[entityId];
    }

    public void MarkReached(int entityId, int tick, int dwellUntilTick)
    {
        EnsureEntity(entityId);
        if (_reachedTicks[entityId] == 0)
        {
            _reachedTicks[entityId] = tick;
            _dwellUntilTicks[entityId] = dwellUntilTick;
        }
    }

    public bool StepIsReady(int entityId, int tick)
    {
        EnsureEntity(entityId);
        return tick >= _nextStepTicks[entityId];
    }

    public void MarkStepped(int entityId, int tick, FixedScalar? stepCost = null)
    {
        EnsureEntity(entityId);
        var interval = _stepIntervalTicks[entityId];
        var baseInterval = interval <= 0 ? 1 : interval;
        var adjustedInterval = (int)FixedScalar.Ceiling(
            baseInterval * FixedScalar.Max(
                FixedScalar.FromUnits(10_000L),
                stepCost ?? FixedScalar.One));
        _nextStepTicks[entityId] = tick + global::System.Math.Max(1, adjustedInterval);
    }

    public void MarkInitialInfluenceApplied(int entityId)
    {
        EnsureEntity(entityId);
        _initialInfluenceApplied[entityId] = true;
    }

    public void PushRecentTarget(int entityId, long targetKey, int tick)
    {
        EnsureEntity(entityId);
        if (targetKey == 0L)
        {
            return;
        }

        var baseIndex = entityId * _recentTargetRingSize;
        var existingSlot = -1;
        for (var slot = 0; slot < _recentTargetRingSize; slot++)
        {
            if (_recentTargetKeys[baseIndex + slot] == targetKey)
            {
                existingSlot = slot;
                break;
            }
        }

        if (existingSlot >= 0)
        {
            ShiftRingDown(baseIndex, existingSlot);
        }
        else
        {
            ShiftRingDown(baseIndex, _recentTargetRingSize - 1);
        }

        _recentTargetKeys[baseIndex] = targetKey;
        _recentTargetTicks[baseIndex] = tick;
        var existingCount = _recentTargetCounts[entityId];
        if (existingCount < _recentTargetRingSize)
        {
            _recentTargetCounts[entityId] = existingCount + 1;
        }
    }

    public static long PackEntityTargetKey(int targetOccurrenceHandle)
    {
        if (targetOccurrenceHandle <= 0)
        {
            return 0L;
        }

        // Bit 62 marks an entity-target key; lower bits hold the occurrence handle.
        return unchecked(0x4000_0000_0000_0000L | (long)(uint)targetOccurrenceHandle);
    }

    public static long PackCellTargetKey(int planeCode, int q, int r)
    {
        // Bit 61 marks a cell-target key. Pack plane code (1 byte) + signed q/r as 24-bit lanes.
        var qLane = (uint)(q & 0x00FFFFFF);
        var rLane = (uint)(r & 0x00FFFFFF);
        var planeLane = (long)(planeCode & 0xFF) << 48;
        return 0x2000_0000_0000_0000L | planeLane | ((long)qLane << 24) | (long)rLane;
    }

    private void ShiftRingDown(int baseIndex, int fromSlot)
    {
        for (var slot = fromSlot; slot > 0; slot--)
        {
            _recentTargetKeys[baseIndex + slot] = _recentTargetKeys[baseIndex + slot - 1];
            _recentTargetTicks[baseIndex + slot] = _recentTargetTicks[baseIndex + slot - 1];
        }
    }

    private void EnsureRecentTargetSlot(int slot)
    {
        if ((uint)slot >= _recentTargetRingSize)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(slot));
        }
    }

    private void EnsureEntity(int entityId)
    {
        if ((uint)entityId == 0u || entityId >= _activeVerbSlots.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(entityId));
        }
    }
}
