namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class LowAltitudeVerbProgramData : IKernelProgramData
{
    private readonly int[] _kindIds;
    private readonly int[] _verbIds;
    private readonly int[] _planeCodes;
    private readonly int[] _operationCodes;
    private readonly int[] _targetModeCodes;
    private readonly int[] _targetKindIds;
    private readonly int[] _relationshipKindIds;
    private readonly int[] _relationshipDirectionCodes;
    private readonly int[] _terrainStarts;
    private readonly int[] _terrainCounts;
    private readonly int[] _terrainIds;
    private readonly int[] _minCommitmentTicks;
    private readonly int[] _maxCommitmentTicks;
    private readonly int[] _dwellTicks;
    private readonly int[] _targetRadii;
    private readonly int[] _candidateSampleCounts;
    private readonly float[] _weights;
    private readonly float[] _exploreWeights;
    private readonly float[] _stalenessWeights;
    private readonly float[] _hotspotWeights;
    private readonly float[] _crowdingWeights;
    private readonly float[] _generatorAffordanceWeights;
    private readonly int[] _affinityRelationshipKindIds;
    private readonly int[] _affinityRelationshipDirectionCodes;
    private readonly float[] _relationshipAffinityWeights;
    private readonly float[] _influenceDeltas;
    private readonly int[] _influenceRadii;
    private readonly float[] _coverageGrowthChances;
    private readonly float[] _headquartersMoveChances;
    private readonly float[] _requiredInfluences;
    private int _ruleCount;
    private int _terrainCount;

    public LowAltitudeVerbProgramData(int ruleCapacity, int itemCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ruleCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(itemCapacity);
        _kindIds = new int[ruleCapacity + 1];
        _verbIds = new int[ruleCapacity + 1];
        _planeCodes = new int[ruleCapacity + 1];
        _operationCodes = new int[ruleCapacity + 1];
        _targetModeCodes = new int[ruleCapacity + 1];
        _targetKindIds = new int[ruleCapacity + 1];
        _relationshipKindIds = new int[ruleCapacity + 1];
        _relationshipDirectionCodes = new int[ruleCapacity + 1];
        _terrainStarts = new int[ruleCapacity + 1];
        _terrainCounts = new int[ruleCapacity + 1];
        _terrainIds = new int[itemCapacity + 1];
        _minCommitmentTicks = new int[ruleCapacity + 1];
        _maxCommitmentTicks = new int[ruleCapacity + 1];
        _dwellTicks = new int[ruleCapacity + 1];
        _targetRadii = new int[ruleCapacity + 1];
        _candidateSampleCounts = new int[ruleCapacity + 1];
        _weights = new float[ruleCapacity + 1];
        _exploreWeights = new float[ruleCapacity + 1];
        _stalenessWeights = new float[ruleCapacity + 1];
        _hotspotWeights = new float[ruleCapacity + 1];
        _crowdingWeights = new float[ruleCapacity + 1];
        _generatorAffordanceWeights = new float[ruleCapacity + 1];
        _affinityRelationshipKindIds = new int[ruleCapacity + 1];
        _affinityRelationshipDirectionCodes = new int[ruleCapacity + 1];
        _relationshipAffinityWeights = new float[ruleCapacity + 1];
        _influenceDeltas = new float[ruleCapacity + 1];
        _influenceRadii = new int[ruleCapacity + 1];
        _coverageGrowthChances = new float[ruleCapacity + 1];
        _headquartersMoveChances = new float[ruleCapacity + 1];
        _requiredInfluences = new float[ruleCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.LowAltitudeVerbs;

    public int SchemaVersion => 3;

    public int RuleCount => _ruleCount;

    public int RegisterVerb(
        int kindId,
        int verbId,
        int planeCode,
        int operationCode,
        int targetModeCode,
        int targetKindId,
        int relationshipKindId,
        int relationshipDirectionCode,
        int minCommitmentTicks,
        int maxCommitmentTicks,
        int dwellTicks,
        int targetRadius,
        int candidateSampleCount,
        float weight,
        float exploreWeight,
        float stalenessWeight,
        float hotspotWeight,
        float crowdingWeight,
        float generatorAffordanceWeight,
        int affinityRelationshipKindId,
        int affinityRelationshipDirectionCode,
        float relationshipAffinityWeight,
        float influenceDelta,
        int influenceRadius,
        float coverageGrowthChance,
        float headquartersMoveChance,
        float requiredInfluence)
    {
        ValidateRegistration(kindId, verbId, planeCode, operationCode, targetModeCode);

        if (_ruleCount + 1 >= _kindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _ruleCount++;
        var slot = _ruleCount;
        _kindIds[slot] = kindId;
        _verbIds[slot] = verbId;
        _planeCodes[slot] = planeCode;
        _operationCodes[slot] = operationCode;
        _targetModeCodes[slot] = targetModeCode;
        _targetKindIds[slot] = targetKindId;
        _relationshipKindIds[slot] = relationshipKindId;
        _relationshipDirectionCodes[slot] = relationshipDirectionCode;
        _minCommitmentTicks[slot] = global::System.Math.Max(1, minCommitmentTicks);
        _maxCommitmentTicks[slot] = global::System.Math.Max(_minCommitmentTicks[slot], maxCommitmentTicks);
        _dwellTicks[slot] = global::System.Math.Max(0, dwellTicks);
        _targetRadii[slot] = global::System.Math.Max(0, targetRadius);
        _candidateSampleCounts[slot] = global::System.Math.Max(1, candidateSampleCount);
        _weights[slot] = global::System.Math.Max(0.01f, weight);
        _exploreWeights[slot] = exploreWeight;
        _stalenessWeights[slot] = stalenessWeight;
        _hotspotWeights[slot] = hotspotWeight;
        _crowdingWeights[slot] = crowdingWeight;
        _generatorAffordanceWeights[slot] = generatorAffordanceWeight;
        _affinityRelationshipKindIds[slot] = affinityRelationshipKindId;
        _affinityRelationshipDirectionCodes[slot] = affinityRelationshipDirectionCode == 0
            ? LowAltitudeVerbRelationshipDirectionCodes.Either
            : affinityRelationshipDirectionCode;
        _relationshipAffinityWeights[slot] = relationshipAffinityWeight;
        _influenceDeltas[slot] = global::System.Math.Max(0f, influenceDelta);
        _influenceRadii[slot] = global::System.Math.Max(0, influenceRadius);
        _coverageGrowthChances[slot] = Clamp01(coverageGrowthChance);
        _headquartersMoveChances[slot] = Clamp01(headquartersMoveChance);
        _requiredInfluences[slot] = ClampInfluence(requiredInfluence);
        return slot;
    }

    public void AddTerrainFilter(int verbSlot, int terrainId)
    {
        EnsureRule(verbSlot);
        ValidatePositive(terrainId);
        if (_terrainCount + 1 >= _terrainIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        if (_terrainCounts[verbSlot] == 0)
        {
            _terrainStarts[verbSlot] = _terrainCount + 1;
        }

        _terrainCount++;
        _terrainIds[_terrainCount] = terrainId;
        _terrainCounts[verbSlot]++;
    }

    public int KindId(int rule) => _kindIds[rule];

    public int VerbId(int rule) => _verbIds[rule];

    public int PlaneCode(int rule) => _planeCodes[rule];

    public int OperationCode(int rule) => _operationCodes[rule];

    public int TargetModeCode(int rule) => _targetModeCodes[rule];

    public int TargetKindId(int rule) => _targetKindIds[rule];

    public int RelationshipKindId(int rule) => _relationshipKindIds[rule];

    public int RelationshipDirectionCode(int rule) => _relationshipDirectionCodes[rule];

    public int MinCommitmentTicks(int rule) => _minCommitmentTicks[rule];

    public int MaxCommitmentTicks(int rule) => _maxCommitmentTicks[rule];

    public int DwellTicks(int rule) => _dwellTicks[rule];

    public int TargetRadius(int rule) => _targetRadii[rule];

    public int CandidateSampleCount(int rule) => _candidateSampleCounts[rule];

    public float Weight(int rule) => _weights[rule];

    public float ExploreWeight(int rule) => _exploreWeights[rule];

    public float StalenessWeight(int rule) => _stalenessWeights[rule];

    public float HotspotWeight(int rule) => _hotspotWeights[rule];

    public float CrowdingWeight(int rule) => _crowdingWeights[rule];

    public float GeneratorAffordanceWeight(int rule) => _generatorAffordanceWeights[rule];

    public int AffinityRelationshipKindId(int rule) => _affinityRelationshipKindIds[rule];

    public int AffinityRelationshipDirectionCode(int rule) => _affinityRelationshipDirectionCodes[rule];

    public float RelationshipAffinityWeight(int rule) => _relationshipAffinityWeights[rule];

    public float InfluenceDelta(int rule) => _influenceDeltas[rule];

    public int InfluenceRadius(int rule) => _influenceRadii[rule];

    public float CoverageGrowthChance(int rule) => _coverageGrowthChances[rule];

    public float HeadquartersMoveChance(int rule) => _headquartersMoveChances[rule];

    public float RequiredInfluence(int rule) => _requiredInfluences[rule];

    public bool TerrainAllowed(int rule, int terrainId)
    {
        var count = _terrainCounts[rule];
        if (count == 0)
        {
            return true;
        }

        var start = _terrainStarts[rule];
        for (var i = 0; i < count; i++)
        {
            if (_terrainIds[start + i] == terrainId)
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureRule(int rule)
    {
        if ((uint)rule == 0u || rule > _ruleCount)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(rule));
        }
    }

    private static void ValidatePositive(int value)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
    }

    private static void ValidateRegistration(
        int kindId,
        int verbId,
        int planeCode,
        int operationCode,
        int targetModeCode)
    {
        ValidatePositive(kindId);
        ValidatePositive(verbId);
        ValidatePlaneCode(planeCode);
        ValidateOperationCode(operationCode);
        ValidateTargetModeCode(targetModeCode);
    }

    private static void ValidatePlaneCode(int planeCode)
    {
        if (planeCode is not PlaneCodes.Physical and not PlaneCodes.Semantic)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(planeCode));
        }
    }

    private static void ValidateTargetModeCode(int targetModeCode)
    {
        if (targetModeCode is not LowAltitudeVerbTargetModeCodes.Explore and
            not LowAltitudeVerbTargetModeCodes.EntityKind and
            not LowAltitudeVerbTargetModeCodes.RelationshipTarget and
            not LowAltitudeVerbTargetModeCodes.Terrain and
            not LowAltitudeVerbTargetModeCodes.OwnInfluence and
            not LowAltitudeVerbTargetModeCodes.ForeignInfluence)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(targetModeCode));
        }
    }

    private static void ValidateOperationCode(int operationCode)
    {
        if (operationCode is not LowAltitudeVerbOperationCodes.Move and
            not LowAltitudeVerbOperationCodes.GrowInfluence and
            not LowAltitudeVerbOperationCodes.ConsolidateInfluence and
            not LowAltitudeVerbOperationCodes.ReduceInfluence and
            not LowAltitudeVerbOperationCodes.ExpandCoverage and
            not LowAltitudeVerbOperationCodes.MoveHeadquarters)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(operationCode));
        }
    }

    private static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }

    private static float ClampInfluence(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        return value > 100f ? 100f : value;
    }
}
