namespace Engine.Kernel;

using Engine.Kernel.Programs;

public sealed partial class RelationshipProgramData : IKernelProgramData
{
    private readonly int[] _kindIds;
    private readonly float[] _cullThresholds;
    private int _ruleCount;

    public RelationshipProgramData(int relationshipRuleCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(relationshipRuleCapacity);
        _kindIds = new int[relationshipRuleCapacity + 1];
        _cullThresholds = new float[relationshipRuleCapacity + 1];
        ProximityMaxDistance = 10;
        ProximityMaxDelta = 0.005f;
        SharedCultureMatchDelta = 0.003f;
        SharedCultureMismatchDelta = -0.001f;
        NeglectDelta = -0.001f;
    }

    public int ProgramId => KernelProgramCodes.Relationships;

    public int SchemaVersion => 1;

    public int Count => _ruleCount;

    public int RuleCapacity => _kindIds.Length - 1;

    public int ProximityMaxDistance { get; set; }

    public float ProximityMaxDelta { get; set; }

    public float SharedCultureMatchDelta { get; set; }

    public float SharedCultureMismatchDelta { get; set; }

    public float NeglectDelta { get; set; }

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
}
