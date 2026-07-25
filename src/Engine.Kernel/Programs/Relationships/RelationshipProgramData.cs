namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class RelationshipProgramData : IKernelProgramData
{
    private readonly int[] _kindIds;
    private readonly int[] _targetFollowsSourcePhysical;
    private readonly int[] _isExclusive;
    private readonly int[] _isCultureAnchor;
    private readonly FixedScalar[] _cullThresholds;
    private readonly int[] _maintenanceKindIds;
    private readonly int[] _maintenanceConditionCodes;
    private readonly int[] _maintenancePlaneCodes;
    private readonly int[] _maintenanceMaxDistances;
    private readonly FixedScalar[] _maintenanceThresholds;
    private readonly FixedScalar[] _maintenanceDeltas;
    private readonly int[] _ruleSlotByKind;
    private readonly int[] _firstMaintenanceRuleByKind;
    private readonly int[] _lastMaintenanceRuleByKind;
    private readonly int[] _nextMaintenanceRule;
    private readonly int[] _followKindIds;
    private int _ruleCount;
    private int _maintenanceRuleCount;
    private int _followKindCount;

    public RelationshipProgramData(int relationshipRuleCapacity, int relationshipKindCapacity = 256)
    {
        NumericGuard.ThrowIfNegativeOrZero(relationshipRuleCapacity);
        NumericGuard.ThrowIfNegativeOrZero(relationshipKindCapacity);
        _kindIds = new int[relationshipRuleCapacity + 1];
        _targetFollowsSourcePhysical = new int[relationshipRuleCapacity + 1];
        _isExclusive = new int[relationshipRuleCapacity + 1];
        _isCultureAnchor = new int[relationshipRuleCapacity + 1];
        _cullThresholds = new FixedScalar[relationshipRuleCapacity + 1];
        var maintenanceCapacity = relationshipRuleCapacity * 4;
        _maintenanceKindIds = new int[maintenanceCapacity + 1];
        _maintenanceConditionCodes = new int[maintenanceCapacity + 1];
        _maintenancePlaneCodes = new int[maintenanceCapacity + 1];
        _maintenanceMaxDistances = new int[maintenanceCapacity + 1];
        _maintenanceThresholds = new FixedScalar[maintenanceCapacity + 1];
        _maintenanceDeltas = new FixedScalar[maintenanceCapacity + 1];
        _ruleSlotByKind = new int[relationshipKindCapacity + 1];
        _firstMaintenanceRuleByKind = new int[relationshipKindCapacity + 1];
        _lastMaintenanceRuleByKind = new int[relationshipKindCapacity + 1];
        _nextMaintenanceRule = new int[maintenanceCapacity + 1];
        _followKindIds = new int[relationshipRuleCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.Relationships;

    public int SchemaVersion => 6;

    public int Count => _ruleCount;

    public int MaintenanceRuleCount => _maintenanceRuleCount;

    public int RuleCapacity => _kindIds.Length - 1;

    public int RegisterRelationshipKind(
        int kindId,
        FixedScalar cullThreshold,
        bool targetFollowsSourcePhysical = false,
        bool isExclusive = false,
        bool isCultureAnchor = false)
    {
        ValidatePositive(kindId);
        EnsureKindCapacity(kindId);
        NumericGuard.ThrowIfNegative(cullThreshold);
        if (_ruleCount + 1 >= _kindIds.Length || _ruleSlotByKind[kindId] != 0)
        {
            throw new global::System.InvalidOperationException();
        }

        _ruleCount++;
        var slot = _ruleCount;
        _kindIds[slot] = kindId;
        _targetFollowsSourcePhysical[slot] = targetFollowsSourcePhysical ? 1 : 0;
        _isExclusive[slot] = isExclusive ? 1 : 0;
        _isCultureAnchor[slot] = isCultureAnchor ? 1 : 0;
        _cullThresholds[slot] = cullThreshold;
        _ruleSlotByKind[kindId] = slot;
        if (targetFollowsSourcePhysical)
        {
            _followKindIds[++_followKindCount] = kindId;
        }

        return slot;
    }

    public int AddMaintenanceRule(
        int kindId,
        int conditionCode,
        int planeCode,
        int maxDistance,
        FixedScalar threshold,
        FixedScalar delta)
    {
        ValidatePositive(kindId);
        EnsureKindCapacity(kindId);
        ValidateMaintenanceCondition(conditionCode);
        ValidatePlaneCode(planeCode);
        NumericGuard.ThrowIfNegative(maxDistance);
        if (_maintenanceRuleCount + 1 >= _maintenanceKindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _maintenanceRuleCount++;
        var slot = _maintenanceRuleCount;
        _maintenanceKindIds[slot] = kindId;
        _maintenanceConditionCodes[slot] = conditionCode;
        _maintenancePlaneCodes[slot] = planeCode;
        _maintenanceMaxDistances[slot] = maxDistance;
        _maintenanceThresholds[slot] = threshold;
        _maintenanceDeltas[slot] = delta;
        var previous = _lastMaintenanceRuleByKind[kindId];
        if (previous > 0)
        {
            _nextMaintenanceRule[previous] = slot;
        }
        else
        {
            _firstMaintenanceRuleByKind[kindId] = slot;
        }

        _lastMaintenanceRuleByKind[kindId] = slot;
        return slot;
    }

    public int RuleKindId(int ruleSlot)
    {
        EnsureRule(ruleSlot);
        return _kindIds[ruleSlot];
    }

    public FixedScalar RuleCullThreshold(int ruleSlot)
    {
        EnsureRule(ruleSlot);
        return _cullThresholds[ruleSlot];
    }

    public bool RuleTargetFollowsSourcePhysical(int ruleSlot)
    {
        EnsureRule(ruleSlot);
        return _targetFollowsSourcePhysical[ruleSlot] != 0;
    }

    public bool RuleIsExclusive(int ruleSlot)
    {
        EnsureRule(ruleSlot);
        return _isExclusive[ruleSlot] != 0;
    }

    public int MaintenanceKindId(int ruleSlot) => _maintenanceKindIds[ruleSlot];

    public int MaintenanceConditionCode(int ruleSlot) => _maintenanceConditionCodes[ruleSlot];

    public int MaintenancePlaneCode(int ruleSlot) => _maintenancePlaneCodes[ruleSlot];

    public int MaintenanceMaxDistance(int ruleSlot) => _maintenanceMaxDistances[ruleSlot];

    public FixedScalar MaintenanceThreshold(int ruleSlot) => _maintenanceThresholds[ruleSlot];

    public FixedScalar MaintenanceDelta(int ruleSlot) => _maintenanceDeltas[ruleSlot];

    public int FirstMaintenanceRule(int kindId) =>
        (uint)kindId < (uint)_firstMaintenanceRuleByKind.Length
            ? _firstMaintenanceRuleByKind[kindId]
            : 0;

    public int NextMaintenanceRule(int ruleSlot) => _nextMaintenanceRule[ruleSlot];

    public int FollowKindCount => _followKindCount;

    public int FollowKindId(int index) => _followKindIds[index];

    public FixedScalar CullThreshold(int kindId)
    {
        var rule = RuleForKind(kindId);
        return rule > 0 ? _cullThresholds[rule] : FixedScalar.FromUnits(50000L);
    }

    public bool TargetFollowsSourcePhysical(int kindId)
    {
        var rule = RuleForKind(kindId);
        return rule > 0 && _targetFollowsSourcePhysical[rule] != 0;
    }

    public bool IsCultureAnchor(int kindId)
    {
        var rule = RuleForKind(kindId);
        return rule > 0 && _isCultureAnchor[rule] != 0;
    }

    public bool KindIsExclusive(int kindId)
    {
        var rule = RuleForKind(kindId);
        return rule > 0 && _isExclusive[rule] != 0;
    }

    private int RuleForKind(int kindId) =>
        (uint)kindId < (uint)_ruleSlotByKind.Length ? _ruleSlotByKind[kindId] : 0;

    private void EnsureKindCapacity(int kindId)
    {
        if ((uint)kindId >= (uint)_ruleSlotByKind.Length)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void EnsureRule(int ruleSlot)
    {
        if ((uint)ruleSlot == 0u || ruleSlot > _ruleCount)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private static void ValidatePositive(int value)
    {
        NumericGuard.ThrowIfNegativeOrZero(value);
    }

    private static void ValidateMaintenanceCondition(int conditionCode)
    {
        if (conditionCode is >= RelationshipMaintenanceConditionCodes.Always
            and <= RelationshipMaintenanceConditionCodes.ContactMemory)
        {
            return;
        }

        throw new global::System.ArgumentOutOfRangeException(nameof(conditionCode));
    }

    private static void ValidatePlaneCode(int planeCode)
    {
        if (planeCode is PlaneCodes.None or PlaneCodes.Physical or PlaneCodes.Semantic)
        {
            return;
        }

        throw new global::System.ArgumentOutOfRangeException(nameof(planeCode));
    }
}
