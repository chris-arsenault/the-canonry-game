namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class LowAltitudeVerbProgramState : IKernelProgramState
{
    private readonly int[] _activeVerbSlots;
    private readonly int[] _intentIds;
    private readonly int[] _targetPlaneCodes;
    private readonly int[] _targetEntityIds;
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
    private int _nextIntentId;

    public LowAltitudeVerbProgramState(int entityCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entityCapacity);
        _activeVerbSlots = new int[entityCapacity + 1];
        _intentIds = new int[entityCapacity + 1];
        _targetPlaneCodes = new int[entityCapacity + 1];
        _targetEntityIds = new int[entityCapacity + 1];
        _targetQ = new int[entityCapacity + 1];
        _targetR = new int[entityCapacity + 1];
        _targetS = new int[entityCapacity + 1];
        _startedTicks = new int[entityCapacity + 1];
        _expiresTicks = new int[entityCapacity + 1];
        _dwellUntilTicks = new int[entityCapacity + 1];
        _reachedTicks = new int[entityCapacity + 1];
        _intentDwellTicks = new int[entityCapacity + 1];
        _nextStepTicks = new int[entityCapacity + 1];
        _stepIntervalTicks = new int[entityCapacity + 1];
        _pathLoosenessPermille = new int[entityCapacity + 1];
        _initialInfluenceApplied = new bool[entityCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.LowAltitudeVerbs;

    public int SchemaVersion => 3;

    public int ActiveVerbSlot(int entityId) => _activeVerbSlots[entityId];

    public int IntentId(int entityId) => _intentIds[entityId];

    public int TargetPlaneCode(int entityId) => _targetPlaneCodes[entityId];

    public int TargetEntityId(int entityId) => _targetEntityIds[entityId];

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

    public int NextIntentId() => _nextIntentId + 1;

    public void ClearIntent(int entityId)
    {
        EnsureEntity(entityId);
        _activeVerbSlots[entityId] = 0;
        _intentIds[entityId] = 0;
        _targetPlaneCodes[entityId] = 0;
        _targetEntityIds[entityId] = 0;
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

    public int SetIntent(
        int entityId,
        int verbSlot,
        int targetPlaneCode,
        int targetEntityId,
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
        _nextIntentId++;
        _activeVerbSlots[entityId] = verbSlot;
        _intentIds[entityId] = _nextIntentId;
        _targetPlaneCodes[entityId] = targetPlaneCode;
        _targetEntityIds[entityId] = targetEntityId;
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
        return _nextIntentId;
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

    public void MarkStepped(int entityId, int tick, float stepCost = 1f)
    {
        EnsureEntity(entityId);
        var interval = _stepIntervalTicks[entityId];
        var baseInterval = interval <= 0 ? 1 : interval;
        var adjustedInterval = (int)global::System.MathF.Ceiling(baseInterval * global::System.MathF.Max(0.01f, stepCost));
        _nextStepTicks[entityId] = tick + global::System.Math.Max(1, adjustedInterval);
    }

    public void MarkInitialInfluenceApplied(int entityId)
    {
        EnsureEntity(entityId);
        _initialInfluenceApplied[entityId] = true;
    }

    private void EnsureEntity(int entityId)
    {
        if ((uint)entityId == 0u || entityId >= _activeVerbSlots.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(entityId));
        }
    }
}
