namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class LowAltitudeVerbProgramData : IKernelProgramData
{
    private int[] _kindIds = [];
    private int[] _verbIds = [];
    private int[] _planeCodes = [];
    private int[] _operationCodes = [];
    private int[] _targetModeCodes = [];
    private int[] _targetKindIds = [];
    private int[] _relationshipKindIds = [];
    private int[] _relationshipDirectionCodes = [];
    private int[] _terrainStarts = [];
    private int[] _terrainCounts = [];
    private int[] _terrainIds = [];
    private int[] StepStarts = [];
    private int[] StepCounts = [];
    private int[] StepOpcodes = [];
    private int[] StepIntArguments = [];
    private float[] StepFloatArguments = [];
    private int[] _minCommitmentTicks = [];
    private int[] _maxCommitmentTicks = [];
    private int[] _dwellTicks = [];
    private int[] _targetRadii = [];
    private int[] _candidateSampleCounts = [];
    private float[] _weights = [];
    private float[] _exploreWeights = [];
    private float[] _stalenessWeights = [];
    private float[] _hotspotWeights = [];
    private float[] _crowdingWeights = [];
    private float[] _generatorAffordanceWeights = [];
    private int[] _affinityRelationshipKindIds = [];
    private int[] _affinityRelationshipDirectionCodes = [];
    private float[] _relationshipAffinityWeights = [];
    private float[] _influenceDeltas = [];
    private int[] _influenceRadii = [];
    private float[] _coverageGrowthChances = [];
    private float[] _headquartersMoveChances = [];
    private float[] _requiredInfluences = [];
    private int[] _initialInfluenceKindIds = [];
    private int[] _initialInfluencePlaneCodes = [];
    private float[] _initialInfluenceValues = [];
    private int[] _affordanceActorKindIds = [];
    private int[] _affordanceCandidateKindIds = [];
    private int[] _affordanceBindingKindIds = [];
    private float[] _affordanceScores = [];
    private int _ruleCount;
    private int _terrainCount;
    private int _stepCount;
    private int _initialInfluenceRuleCount;
    private int _affordanceKindPairCount;
    private int _semanticMinQ = -24;
    private int _semanticMaxQ = 24;
    private int _semanticMinR = -24;
    private int _semanticMaxR = 24;

    public LowAltitudeVerbProgramData(int ruleCapacity, int itemCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ruleCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(itemCapacity);
        var ruleLength = ruleCapacity + 1;
        var itemLength = itemCapacity + 1;
        InitializeRuleIdentityArrays(ruleLength);
        InitializeTargetAndTerrainArrays(ruleLength, itemLength);
        InitializeStepArrays(ruleLength, itemLength);
        InitializeCommitmentArrays(ruleLength);
        InitializeWeightArrays(ruleLength);
        InitializeInfluenceArrays(ruleLength);
        InitializeInitialInfluenceArrays(ruleLength);
        InitializeAffordanceArrays(itemLength);
    }

    private void InitializeRuleIdentityArrays(int ruleLength)
    {
        _kindIds = new int[ruleLength];
        _verbIds = new int[ruleLength];
        _planeCodes = new int[ruleLength];
        _operationCodes = new int[ruleLength];
    }

    private void InitializeTargetAndTerrainArrays(int ruleLength, int itemLength)
    {
        _targetModeCodes = new int[ruleLength];
        _targetKindIds = new int[ruleLength];
        _relationshipKindIds = new int[ruleLength];
        _relationshipDirectionCodes = new int[ruleLength];
        _terrainStarts = new int[ruleLength];
        _terrainCounts = new int[ruleLength];
        _terrainIds = new int[itemLength];
    }

    private void InitializeStepArrays(int ruleLength, int itemLength)
    {
        StepStarts = new int[ruleLength];
        StepCounts = new int[ruleLength];
        StepOpcodes = new int[itemLength];
        StepIntArguments = new int[itemLength];
        StepFloatArguments = new float[itemLength];
    }

    private void InitializeCommitmentArrays(int ruleLength)
    {
        _minCommitmentTicks = new int[ruleLength];
        _maxCommitmentTicks = new int[ruleLength];
        _dwellTicks = new int[ruleLength];
        _targetRadii = new int[ruleLength];
        _candidateSampleCounts = new int[ruleLength];
    }

    private void InitializeWeightArrays(int ruleLength)
    {
        _weights = new float[ruleLength];
        _exploreWeights = new float[ruleLength];
        _stalenessWeights = new float[ruleLength];
        _hotspotWeights = new float[ruleLength];
        _crowdingWeights = new float[ruleLength];
        _generatorAffordanceWeights = new float[ruleLength];
        _affinityRelationshipKindIds = new int[ruleLength];
        _affinityRelationshipDirectionCodes = new int[ruleLength];
        _relationshipAffinityWeights = new float[ruleLength];
    }

    private void InitializeInfluenceArrays(int ruleLength)
    {
        _influenceDeltas = new float[ruleLength];
        _influenceRadii = new int[ruleLength];
        _coverageGrowthChances = new float[ruleLength];
        _headquartersMoveChances = new float[ruleLength];
        _requiredInfluences = new float[ruleLength];
    }

    private void InitializeInitialInfluenceArrays(int ruleLength)
    {
        _initialInfluenceKindIds = new int[ruleLength];
        _initialInfluencePlaneCodes = new int[ruleLength];
        _initialInfluenceValues = new float[ruleLength];
    }

    private void InitializeAffordanceArrays(int itemLength)
    {
        _affordanceActorKindIds = new int[itemLength];
        _affordanceCandidateKindIds = new int[itemLength];
        _affordanceBindingKindIds = new int[itemLength];
        _affordanceScores = new float[itemLength];
    }

    public int ProgramId => KernelProgramCodes.LowAltitudeVerbs;

    public int SchemaVersion => 4;

    public int RuleCount => _ruleCount;

    public int InitialInfluenceRuleCount => _initialInfluenceRuleCount;

    public int AffordanceKindPairCount => _affordanceKindPairCount;

    public int SemanticMinQ => _semanticMinQ;

    public int SemanticMaxQ => _semanticMaxQ;

    public int SemanticMinR => _semanticMinR;

    public int SemanticMaxR => _semanticMaxR;

    public void ConfigureSemanticBounds(int minQ, int maxQ, int minR, int maxR)
    {
        if (minQ > maxQ || minR > maxR)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(minQ));
        }

        _semanticMinQ = minQ;
        _semanticMaxQ = maxQ;
        _semanticMinR = minR;
        _semanticMaxR = maxR;
    }

    public int RegisterVerb(
        in LowAltitudeVerbRegistration registration)
    {
        ValidateRegistration(
            registration.KindId,
            registration.VerbId,
            registration.PlaneCode,
            registration.OperationCode,
            registration.TargetModeCode);

        if (_ruleCount + 1 >= _kindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _ruleCount++;
        var slot = _ruleCount;
        _kindIds[slot] = registration.KindId;
        _verbIds[slot] = registration.VerbId;
        _planeCodes[slot] = registration.PlaneCode;
        _operationCodes[slot] = registration.OperationCode;
        _targetModeCodes[slot] = registration.TargetModeCode;
        _targetKindIds[slot] = registration.TargetKindId;
        _relationshipKindIds[slot] = registration.RelationshipKindId;
        _relationshipDirectionCodes[slot] = registration.RelationshipDirectionCode;
        _minCommitmentTicks[slot] = global::System.Math.Max(1, registration.MinTicks);
        _maxCommitmentTicks[slot] = global::System.Math.Max(_minCommitmentTicks[slot], registration.MaxTicks);
        _dwellTicks[slot] = global::System.Math.Max(0, registration.DwellTicks);
        _targetRadii[slot] = global::System.Math.Max(0, registration.TargetRadius);
        _candidateSampleCounts[slot] = global::System.Math.Max(1, registration.CandidateSampleCount);
        _weights[slot] = global::System.Math.Max(0.01f, registration.Weight);
        _exploreWeights[slot] = registration.ExploreWeight;
        _stalenessWeights[slot] = registration.StalenessWeight;
        _hotspotWeights[slot] = registration.HotspotWeight;
        _crowdingWeights[slot] = registration.CrowdingWeight;
        _generatorAffordanceWeights[slot] = registration.GeneratorAffordanceWeight;
        _affinityRelationshipKindIds[slot] = registration.AffinityRelationshipKindId;
        _affinityRelationshipDirectionCodes[slot] = registration.AffinityRelationshipDirectionCode == 0
            ? LowAltitudeVerbRelationshipDirectionCodes.Either
            : registration.AffinityRelationshipDirectionCode;
        _relationshipAffinityWeights[slot] = registration.RelationshipAffinityWeight;
        _influenceDeltas[slot] = global::System.Math.Max(0f, registration.InfluenceDelta);
        _influenceRadii[slot] = global::System.Math.Max(0, registration.InfluenceRadius);
        _coverageGrowthChances[slot] = Clamp01(registration.CoverageChance);
        _headquartersMoveChances[slot] = Clamp01(registration.RelocationChance);
        _requiredInfluences[slot] = ClampInfluence(registration.RequiredMapValue);
        return slot;
    }

    public void AddPrimitiveStep(int rule, int opcode, int intArgument, float floatArgument)
    {
        EnsureRule(rule);
        ValidateOperationCode(opcode);
        if (StepCounts[rule] == 0)
        {
            StepStarts[rule] = _stepCount + 1;
        }

        AddStep(opcode, intArgument, floatArgument);
        StepCounts[rule]++;
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

    public int RegisterInitialInfluence(int kindId, int planeCode, float influenceValue)
    {
        ValidatePositive(kindId);
        ValidatePlaneCode(planeCode);
        if (_initialInfluenceRuleCount + 1 >= _initialInfluenceKindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _initialInfluenceRuleCount++;
        _initialInfluenceKindIds[_initialInfluenceRuleCount] = kindId;
        _initialInfluencePlaneCodes[_initialInfluenceRuleCount] = planeCode;
        _initialInfluenceValues[_initialInfluenceRuleCount] = ClampInfluence(influenceValue);
        return _initialInfluenceRuleCount;
    }

    public int RegisterGeneratorAffordance(
        int actorKindId,
        int candidateKindId,
        int bindingKindId,
        float affordanceScore)
    {
        ValidatePositive(actorKindId);
        ValidatePositive(candidateKindId);
        ValidatePositive(bindingKindId);
        if (_affordanceKindPairCount + 1 >= _affordanceActorKindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _affordanceKindPairCount++;
        _affordanceActorKindIds[_affordanceKindPairCount] = actorKindId;
        _affordanceCandidateKindIds[_affordanceKindPairCount] = candidateKindId;
        _affordanceBindingKindIds[_affordanceKindPairCount] = bindingKindId;
        _affordanceScores[_affordanceKindPairCount] = global::System.Math.Max(0f, affordanceScore);
        return _affordanceKindPairCount;
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

    public int InitialInfluenceKindId(int rule) => _initialInfluenceKindIds[rule];

    public int InitialInfluencePlaneCode(int rule) => _initialInfluencePlaneCodes[rule];

    public float InitialInfluenceValue(int rule) => _initialInfluenceValues[rule];

    public int StepCount(int rule) => StepCounts[rule];

    public int StepStart(int rule) => StepStarts[rule];

    public int StepOpcode(int step) => StepOpcodes[step];

    public int StepArgument(int step) => StepIntArguments[step];

    public float StepFloatArgument(int step) => StepFloatArguments[step];

    public int AffordanceActorKindId(int rule) => _affordanceActorKindIds[rule];

    public int AffordanceCandidateKindId(int rule) => _affordanceCandidateKindIds[rule];

    public int AffordanceBindingKindId(int rule) => _affordanceBindingKindIds[rule];

    public float AffordanceScore(int rule) => _affordanceScores[rule];

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

    private void AddStep(int opcode, int intArgument, float floatArgument)
    {
        if (_stepCount + 1 >= StepOpcodes.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _stepCount++;
        StepOpcodes[_stepCount] = opcode;
        StepIntArguments[_stepCount] = intArgument;
        StepFloatArguments[_stepCount] = floatArgument;
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
        if (targetModeCode is not TargetSelectorCodes.Explore and
            not TargetSelectorCodes.EntityKind and
            not TargetSelectorCodes.RelationshipTarget and
            not TargetSelectorCodes.Terrain and
            not TargetSelectorCodes.SelfMapValue and
            not TargetSelectorCodes.OtherMapValue)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(targetModeCode));
        }
    }

    private static void ValidateOperationCode(int operationCode)
    {
        if (operationCode is not ProgramOpcodeCodes.MoveActor and
            not ProgramOpcodeCodes.AddOwnedCellScalar and
            not ProgramOpcodeCodes.SubtractOtherCellScalar and
            not ProgramOpcodeCodes.TryClaimCell and
            not ProgramOpcodeCodes.TryRelocateEntity)
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
