namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class MovementProgramData : IKernelProgramData
{
    private readonly int[] _kindIds;
    private readonly int[] _planeCodes;
    private readonly float[] _moveChances;
    private readonly float[] _exploreWeights;
    private readonly float[] _stalenessWeights;
    private readonly float[] _crowdingWeights;
    private int _ruleCount;

    public MovementProgramData(int ruleCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ruleCapacity);
        _kindIds = new int[ruleCapacity + 1];
        _planeCodes = new int[ruleCapacity + 1];
        _moveChances = new float[ruleCapacity + 1];
        _exploreWeights = new float[ruleCapacity + 1];
        _stalenessWeights = new float[ruleCapacity + 1];
        _crowdingWeights = new float[ruleCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.Movement;

    public int SchemaVersion => 1;

    public int RuleCount => _ruleCount;

    public int RegisterRule(
        int kindId,
        int planeCode,
        float moveChance,
        float exploreWeight,
        float stalenessWeight,
        float crowdingWeight)
    {
        ValidatePositive(kindId);
        if (planeCode is not PlaneCodes.Physical and not PlaneCodes.Semantic)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(planeCode));
        }

        if (_ruleCount + 1 >= _kindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _ruleCount++;
        var slot = _ruleCount;
        _kindIds[slot] = kindId;
        _planeCodes[slot] = planeCode;
        _moveChances[slot] = Clamp01(moveChance);
        _exploreWeights[slot] = exploreWeight;
        _stalenessWeights[slot] = stalenessWeight;
        _crowdingWeights[slot] = crowdingWeight;
        return slot;
    }

    public int RuleForKind(int kindId)
    {
        for (var rule = 1; rule <= _ruleCount; rule++)
        {
            if (_kindIds[rule] == kindId)
            {
                return rule;
            }
        }

        return 0;
    }

    public int PlaneCode(int rule) => _planeCodes[rule];

    public float MoveChance(int rule) => _moveChances[rule];

    public float ExploreWeight(int rule) => _exploreWeights[rule];

    public float StalenessWeight(int rule) => _stalenessWeights[rule];

    public float CrowdingWeight(int rule) => _crowdingWeights[rule];

    private static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }

    private static void ValidatePositive(int value)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
    }
}

