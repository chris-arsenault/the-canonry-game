namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class RelationshipProgramData : IKernelProgramData
{
    private readonly int[] _kindIds;
    private readonly float[] _cullThresholds;
    private readonly int[] _maintenanceKindIds;
    private readonly int[] _maintenanceConditionCodes;
    private readonly int[] _maintenancePlaneCodes;
    private readonly int[] _maintenanceMaxDistances;
    private readonly float[] _maintenanceThresholds;
    private readonly float[] _maintenanceDeltas;
    private int _ruleCount;
    private int _maintenanceRuleCount;

    public RelationshipProgramData(int relationshipRuleCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(relationshipRuleCapacity);
        _kindIds = new int[relationshipRuleCapacity + 1];
        _cullThresholds = new float[relationshipRuleCapacity + 1];
        var maintenanceCapacity = relationshipRuleCapacity * 4;
        _maintenanceKindIds = new int[maintenanceCapacity + 1];
        _maintenanceConditionCodes = new int[maintenanceCapacity + 1];
        _maintenancePlaneCodes = new int[maintenanceCapacity + 1];
        _maintenanceMaxDistances = new int[maintenanceCapacity + 1];
        _maintenanceThresholds = new float[maintenanceCapacity + 1];
        _maintenanceDeltas = new float[maintenanceCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.Relationships;

    public int SchemaVersion => 3;

    public int Count => _ruleCount;

    public int MaintenanceRuleCount => _maintenanceRuleCount;

    public int RuleCapacity => _kindIds.Length - 1;

    public int RegisterRelationshipKind(int kindId, float cullThreshold)
    {
        ValidatePositive(kindId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(cullThreshold);
        if (_ruleCount + 1 >= _kindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _ruleCount++;
        var slot = _ruleCount;
        _kindIds[slot] = kindId;
        _cullThresholds[slot] = cullThreshold;
        return slot;
    }

    public int AddMaintenanceRule(
        int kindId,
        int conditionCode,
        int planeCode,
        int maxDistance,
        float threshold,
        float delta)
    {
        ValidatePositive(kindId);
        ValidateMaintenanceCondition(conditionCode);
        ValidatePlaneCode(planeCode);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(maxDistance);
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
        return slot;
    }

    public int RuleKindId(int ruleSlot)
    {
        EnsureRule(ruleSlot);
        return _kindIds[ruleSlot];
    }

    public float RuleCullThreshold(int ruleSlot)
    {
        EnsureRule(ruleSlot);
        return _cullThresholds[ruleSlot];
    }

    public int MaintenanceKindId(int ruleSlot) => _maintenanceKindIds[ruleSlot];

    public int MaintenanceConditionCode(int ruleSlot) => _maintenanceConditionCodes[ruleSlot];

    public int MaintenancePlaneCode(int ruleSlot) => _maintenancePlaneCodes[ruleSlot];

    public int MaintenanceMaxDistance(int ruleSlot) => _maintenanceMaxDistances[ruleSlot];

    public float MaintenanceThreshold(int ruleSlot) => _maintenanceThresholds[ruleSlot];

    public float MaintenanceDelta(int ruleSlot) => _maintenanceDeltas[ruleSlot];

    public float CullThreshold(int kindId)
    {
        for (var rule = 1; rule <= _ruleCount; rule++)
        {
            if (_kindIds[rule] == kindId)
            {
                return _cullThresholds[rule];
            }
        }

        return 0.05f;
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
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
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
