namespace Engine.Kernel;

using Engine.Constants;

/// <summary>
/// The authored pool of Ages: the beat treatment applied to the world itself.
/// Each age lives on one of three regime tracks (Ice, Peoples, Memory), opens
/// and closes on recognized macro facts - never timers - and, while open,
/// charges existing authored rates (generator pulse weights, verb weights,
/// gambit scores, the contest decay tide). The framework only modulates what
/// authored content already does; it writes no world state of its own.
/// </summary>
internal sealed class WorldBeatsProgramData : IKernelProgramData
{
    private int[] _ageIds = [];
    private int[] _trackCodes = [];
    private int[] _priorities = [];
    private int[] _minOpenTicks = [];
    private int[] _cooldownTicks = [];
    private int[] _openConditionStarts = [];
    private int[] _openConditionCounts = [];
    private int[] _closeConditionStarts = [];
    private int[] _closeConditionCounts = [];
    private int[] _chargeStarts = [];
    private int[] _chargeCounts = [];
    private int _ageCount;

    private int[] _conditionFactCodes = [];
    private int[] _conditionComparatorCodes = [];
    private int[] _conditionParamIds = [];
    private float[] _conditionValues = [];
    private int _conditionCount;

    private int[] _chargeDomainCodes = [];
    private int[] _chargeTargetIds = [];
    private float[] _chargeMultipliers = [];
    private int _chargeCount;

    public int ProgramId => KernelProgramCodes.WorldBeats;

    public int SchemaVersion => 1;

    public int AgeCount => _ageCount;

    public void Allocate(int ageCapacity, int itemCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ageCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(itemCapacity);
        _ageIds = new int[ageCapacity + 1];
        _trackCodes = new int[ageCapacity + 1];
        _priorities = new int[ageCapacity + 1];
        _minOpenTicks = new int[ageCapacity + 1];
        _cooldownTicks = new int[ageCapacity + 1];
        _openConditionStarts = new int[ageCapacity + 1];
        _openConditionCounts = new int[ageCapacity + 1];
        _closeConditionStarts = new int[ageCapacity + 1];
        _closeConditionCounts = new int[ageCapacity + 1];
        _chargeStarts = new int[ageCapacity + 1];
        _chargeCounts = new int[ageCapacity + 1];
        _conditionFactCodes = new int[itemCapacity];
        _conditionComparatorCodes = new int[itemCapacity];
        _conditionParamIds = new int[itemCapacity];
        _conditionValues = new float[itemCapacity];
        _chargeDomainCodes = new int[itemCapacity];
        _chargeTargetIds = new int[itemCapacity];
        _chargeMultipliers = new float[itemCapacity];
    }

    /// <summary>Registers an age; conditions and charges must follow before the next AddAge.</summary>
    public int AddAge(int ageId, int trackCode, int priority, int minOpenTicks, int cooldownTicks)
    {
        if (_ageCount + 1 >= _ageIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        if (trackCode < 0 || trackCode >= WorldBeatTrackCodes.Count)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(trackCode));
        }

        _ageCount++;
        _ageIds[_ageCount] = ageId;
        _trackCodes[_ageCount] = trackCode;
        _priorities[_ageCount] = priority;
        _minOpenTicks[_ageCount] = minOpenTicks;
        _cooldownTicks[_ageCount] = cooldownTicks;
        _openConditionStarts[_ageCount] = _conditionCount;
        _closeConditionStarts[_ageCount] = _conditionCount;
        _chargeStarts[_ageCount] = _chargeCount;
        return _ageCount;
    }

    public void AddOpenCondition(int ageSlot, int factCode, int comparatorCode, float value, int paramId)
    {
        RequireCurrentAge(ageSlot);
        if (_closeConditionCounts[ageSlot] > 0)
        {
            throw new global::System.InvalidOperationException();
        }

        AppendCondition(factCode, comparatorCode, value, paramId);
        _openConditionCounts[ageSlot]++;
        _closeConditionStarts[ageSlot] = _conditionCount;
    }

    public void AddCloseCondition(int ageSlot, int factCode, int comparatorCode, float value, int paramId)
    {
        RequireCurrentAge(ageSlot);
        AppendCondition(factCode, comparatorCode, value, paramId);
        _closeConditionCounts[ageSlot]++;
    }

    public void AddCharge(int ageSlot, int domainCode, int targetId, float multiplier)
    {
        RequireCurrentAge(ageSlot);
        if (_chargeCount >= _chargeDomainCodes.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _chargeDomainCodes[_chargeCount] = domainCode;
        _chargeTargetIds[_chargeCount] = targetId;
        _chargeMultipliers[_chargeCount] = multiplier;
        _chargeCount++;
        _chargeCounts[ageSlot]++;
    }

    public int AgeId(int ageSlot) => _ageIds[ageSlot];

    public int TrackCode(int ageSlot) => _trackCodes[ageSlot];

    public int Priority(int ageSlot) => _priorities[ageSlot];

    public int MinOpenTicks(int ageSlot) => _minOpenTicks[ageSlot];

    public int CooldownTicks(int ageSlot) => _cooldownTicks[ageSlot];

    public int OpenConditionStart(int ageSlot) => _openConditionStarts[ageSlot];

    public int OpenConditionCount(int ageSlot) => _openConditionCounts[ageSlot];

    public int CloseConditionStart(int ageSlot) => _closeConditionStarts[ageSlot];

    public int CloseConditionCount(int ageSlot) => _closeConditionCounts[ageSlot];

    public int ChargeStart(int ageSlot) => _chargeStarts[ageSlot];

    public int ChargeCount(int ageSlot) => _chargeCounts[ageSlot];

    public int ConditionFactCode(int condition) => _conditionFactCodes[condition];

    public int ConditionComparatorCode(int condition) => _conditionComparatorCodes[condition];

    public int ConditionParamId(int condition) => _conditionParamIds[condition];

    public float ConditionValue(int condition) => _conditionValues[condition];

    public int ChargeDomainCode(int charge) => _chargeDomainCodes[charge];

    public int ChargeTargetId(int charge) => _chargeTargetIds[charge];

    public float ChargeMultiplier(int charge) => _chargeMultipliers[charge];

    private void AppendCondition(int factCode, int comparatorCode, float value, int paramId)
    {
        if (_conditionCount >= _conditionFactCodes.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _conditionFactCodes[_conditionCount] = factCode;
        _conditionComparatorCodes[_conditionCount] = comparatorCode;
        _conditionParamIds[_conditionCount] = paramId;
        _conditionValues[_conditionCount] = value;
        _conditionCount++;
    }

    private void RequireCurrentAge(int ageSlot)
    {
        if (ageSlot != _ageCount)
        {
            throw new global::System.InvalidOperationException();
        }
    }
}
