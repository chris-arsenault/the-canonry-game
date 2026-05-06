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
    private float[] _opportunityBonusOverrides = [];
    private float[] _fitnessWeights = [];
    private float[] _distanceScaleOverrides = [];
    private int[] _foreignInfluenceFilterEnabled = [];
    private int[] _foreignInfluenceBlockPositiveRelationships = [];
    private int[] _foreignInfluenceAllowNegativeRelationships = [];
    private float[] _foreignInfluenceMinimumRelationshipStrengths = [];
    private float[] _foreignInfluenceAllowUnrelatedInfluenceAtOrAbove = [];
    private int[] _fitnessRelationshipKindIds = [];
    private int[] _fitnessRelationshipDirectionCodes = [];
    private int[] _fitnessPolarityCodes = [];
    private float[] _fitnessThresholds = [];
    private float[] _influenceDeltas = [];
    private int[] _influenceRadii = [];
    private float[] _coverageGrowthChances = [];
    private float[] _headquartersMoveChances = [];
    private float[] _requiredInfluences = [];
    private float[] _relationshipStrengthDeltas = [];
    private int[] _initialInfluenceKindIds = [];
    private int[] _initialInfluencePlaneCodes = [];
    private float[] _initialInfluenceValues = [];
    private int[] _affordanceSourceKindCodes = [];
    private int[] _affordanceSourceIds = [];
    private int[] _affordanceShapeCodes = [];
    private int[] _affordanceActorKindIds = [];
    private int[] _affordancePlaneCodes = [];
    private int[] _affordanceCandidateKindIds = [];
    private int[] _affordanceRelationshipKindIds = [];
    private int[] _affordanceRelationshipDirectionCodes = [];
    private int[] _affordanceOperationCodes = [];
    private int[] _affordanceTerrainIds = [];
    private int[] _affordancePolarityCodes = [];
    private float[] _affordanceThresholds = [];
    private float[] _affordanceWeights = [];
    private int[] _traitModifierKindIds = [];
    private int[] _traitModifierTraitIds = [];
    private int[] _traitModifierVerbIds = [];
    private float[] _traitModifierWeightDeltas = [];
    private float[] _traitModifierOpportunityBonusDeltas = [];
    private float[] _traitModifierFitnessWeightDeltas = [];
    private int _ruleCount;
    private int _terrainCount;
    private int _stepCount;
    private int _initialInfluenceRuleCount;
    private int _affordanceCount;
    private int _traitWeightModifierCount;
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
        InitializeTraitWeightModifierArrays(itemLength);
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
        _opportunityBonusOverrides = new float[ruleLength];
        _fitnessWeights = new float[ruleLength];
        _distanceScaleOverrides = new float[ruleLength];
        _foreignInfluenceFilterEnabled = new int[ruleLength];
        _foreignInfluenceBlockPositiveRelationships = new int[ruleLength];
        _foreignInfluenceAllowNegativeRelationships = new int[ruleLength];
        _foreignInfluenceMinimumRelationshipStrengths = new float[ruleLength];
        _foreignInfluenceAllowUnrelatedInfluenceAtOrAbove = new float[ruleLength];
        _fitnessRelationshipKindIds = new int[ruleLength];
        _fitnessRelationshipDirectionCodes = new int[ruleLength];
        _fitnessPolarityCodes = new int[ruleLength];
        _fitnessThresholds = new float[ruleLength];
    }

    private void InitializeInfluenceArrays(int ruleLength)
    {
        _influenceDeltas = new float[ruleLength];
        _influenceRadii = new int[ruleLength];
        _coverageGrowthChances = new float[ruleLength];
        _headquartersMoveChances = new float[ruleLength];
        _requiredInfluences = new float[ruleLength];
        _relationshipStrengthDeltas = new float[ruleLength];
    }

    private void InitializeInitialInfluenceArrays(int ruleLength)
    {
        _initialInfluenceKindIds = new int[ruleLength];
        _initialInfluencePlaneCodes = new int[ruleLength];
        _initialInfluenceValues = new float[ruleLength];
    }

    private void InitializeAffordanceArrays(int itemLength)
    {
        _affordanceSourceKindCodes = new int[itemLength];
        _affordanceSourceIds = new int[itemLength];
        _affordanceShapeCodes = new int[itemLength];
        _affordanceActorKindIds = new int[itemLength];
        _affordancePlaneCodes = new int[itemLength];
        _affordanceCandidateKindIds = new int[itemLength];
        _affordanceRelationshipKindIds = new int[itemLength];
        _affordanceRelationshipDirectionCodes = new int[itemLength];
        _affordanceOperationCodes = new int[itemLength];
        _affordanceTerrainIds = new int[itemLength];
        _affordancePolarityCodes = new int[itemLength];
        _affordanceThresholds = new float[itemLength];
        _affordanceWeights = new float[itemLength];
    }

    private void InitializeTraitWeightModifierArrays(int itemLength)
    {
        _traitModifierKindIds = new int[itemLength];
        _traitModifierTraitIds = new int[itemLength];
        _traitModifierVerbIds = new int[itemLength];
        _traitModifierWeightDeltas = new float[itemLength];
        _traitModifierOpportunityBonusDeltas = new float[itemLength];
        _traitModifierFitnessWeightDeltas = new float[itemLength];
    }

    public int ProgramId => KernelProgramCodes.LowAltitudeVerbs;

    public int SchemaVersion => 8;

    public int RuleCount => _ruleCount;

    public int InitialInfluenceRuleCount => _initialInfluenceRuleCount;

    public int AffordanceCount => _affordanceCount;

    public int TraitWeightModifierCount => _traitWeightModifierCount;

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
        _opportunityBonusOverrides[slot] = registration.OpportunityBonusOverride;
        _fitnessWeights[slot] = registration.FitnessWeight;
        _distanceScaleOverrides[slot] = registration.DistanceScaleOverride;
        _foreignInfluenceFilterEnabled[slot] = ClampFlag(registration.ForeignInfluenceFilterEnabled);
        _foreignInfluenceBlockPositiveRelationships[slot] = ClampFlag(registration.ForeignInfluenceBlockPositiveRelationships);
        _foreignInfluenceAllowNegativeRelationships[slot] = ClampFlag(registration.ForeignInfluenceAllowNegativeRelationships);
        _foreignInfluenceMinimumRelationshipStrengths[slot] = global::System.Math.Max(0f, registration.ForeignInfluenceMinimumRelationshipStrength);
        _foreignInfluenceAllowUnrelatedInfluenceAtOrAbove[slot] = ClampDisabledInfluence(registration.ForeignInfluenceAllowUnrelatedInfluenceAtOrAbove);
        _fitnessRelationshipKindIds[slot] = registration.FitnessRelationshipKindId;
        _fitnessRelationshipDirectionCodes[slot] = registration.FitnessRelationshipDirectionCode == 0
            ? LowAltitudeVerbRelationshipDirectionCodes.Either
            : registration.FitnessRelationshipDirectionCode;
        _fitnessPolarityCodes[slot] = registration.FitnessPolarityCode;
        _fitnessThresholds[slot] = registration.FitnessThreshold;
        _influenceDeltas[slot] = global::System.Math.Max(0f, registration.InfluenceDelta);
        _influenceRadii[slot] = global::System.Math.Max(0, registration.InfluenceRadius);
        _coverageGrowthChances[slot] = Clamp01(registration.CoverageChance);
        _headquartersMoveChances[slot] = Clamp01(registration.RelocationChance);
        _requiredInfluences[slot] = ClampInfluence(registration.RequiredMapValue);
        _relationshipStrengthDeltas[slot] = registration.RelationshipStrengthDelta;
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

    public int RegisterAffordance(
        int sourceKindCode,
        int sourceId,
        int shapeCode,
        int actorKindId,
        int planeCode,
        int candidateKindId,
        int relationshipKindId,
        int relationshipDirectionCode,
        int operationCode,
        int terrainId,
        int polarityCode,
        float threshold,
        float weight)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(sourceKindCode);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(sourceId);
        ValidatePositive(shapeCode);
        ValidatePositive(actorKindId);
        ValidateOptionalPlaneCode(planeCode);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(candidateKindId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(relationshipKindId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(relationshipDirectionCode);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(operationCode);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(terrainId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(polarityCode);
        if (_affordanceCount + 1 >= _affordanceActorKindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _affordanceCount++;
        _affordanceSourceKindCodes[_affordanceCount] = sourceKindCode;
        _affordanceSourceIds[_affordanceCount] = sourceId;
        _affordanceShapeCodes[_affordanceCount] = shapeCode;
        _affordanceActorKindIds[_affordanceCount] = actorKindId;
        _affordancePlaneCodes[_affordanceCount] = planeCode;
        _affordanceCandidateKindIds[_affordanceCount] = candidateKindId;
        _affordanceRelationshipKindIds[_affordanceCount] = relationshipKindId;
        _affordanceRelationshipDirectionCodes[_affordanceCount] = relationshipDirectionCode == 0
            ? LowAltitudeVerbRelationshipDirectionCodes.Either
            : relationshipDirectionCode;
        _affordanceOperationCodes[_affordanceCount] = operationCode;
        _affordanceTerrainIds[_affordanceCount] = terrainId;
        _affordancePolarityCodes[_affordanceCount] = polarityCode;
        _affordanceThresholds[_affordanceCount] = global::System.Math.Max(0f, threshold);
        _affordanceWeights[_affordanceCount] = global::System.Math.Max(0f, weight);
        return _affordanceCount;
    }

    public int RegisterTraitWeightModifier(
        int kindId,
        int traitId,
        int verbId,
        float weightDelta,
        float opportunityBonusDelta,
        float fitnessWeightDelta)
    {
        ValidatePositive(kindId);
        ValidatePositive(traitId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(verbId);
        if (_traitWeightModifierCount + 1 >= _traitModifierKindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _traitWeightModifierCount++;
        _traitModifierKindIds[_traitWeightModifierCount] = kindId;
        _traitModifierTraitIds[_traitWeightModifierCount] = traitId;
        _traitModifierVerbIds[_traitWeightModifierCount] = verbId;
        _traitModifierWeightDeltas[_traitWeightModifierCount] = weightDelta;
        _traitModifierOpportunityBonusDeltas[_traitWeightModifierCount] = opportunityBonusDelta;
        _traitModifierFitnessWeightDeltas[_traitWeightModifierCount] = fitnessWeightDelta;
        return _traitWeightModifierCount;
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

    public float OpportunityBonusOverride(int rule) => _opportunityBonusOverrides[rule];

    public float FitnessWeight(int rule) => _fitnessWeights[rule];

    public float DistanceScaleOverride(int rule) => _distanceScaleOverrides[rule];

    public bool ForeignInfluenceFilterEnabled(int rule) => _foreignInfluenceFilterEnabled[rule] != 0;

    public bool ForeignInfluenceBlocksPositiveRelationships(int rule) => _foreignInfluenceBlockPositiveRelationships[rule] != 0;

    public bool ForeignInfluenceAllowsNegativeRelationships(int rule) => _foreignInfluenceAllowNegativeRelationships[rule] != 0;

    public float ForeignInfluenceMinimumRelationshipStrength(int rule) => _foreignInfluenceMinimumRelationshipStrengths[rule];

    public float ForeignInfluenceAllowUnrelatedInfluenceAtOrAbove(int rule) => _foreignInfluenceAllowUnrelatedInfluenceAtOrAbove[rule];

    public int FitnessRelationshipKindId(int rule) => _fitnessRelationshipKindIds[rule];

    public int FitnessRelationshipDirectionCode(int rule) => _fitnessRelationshipDirectionCodes[rule];

    public int FitnessPolarityCode(int rule) => _fitnessPolarityCodes[rule];

    public float FitnessThreshold(int rule) => _fitnessThresholds[rule];

    public float InfluenceDelta(int rule) => _influenceDeltas[rule];

    public int InfluenceRadius(int rule) => _influenceRadii[rule];

    public float CoverageGrowthChance(int rule) => _coverageGrowthChances[rule];

    public float HeadquartersMoveChance(int rule) => _headquartersMoveChances[rule];

    public float RequiredInfluence(int rule) => _requiredInfluences[rule];

    public float RelationshipStrengthDelta(int rule) => _relationshipStrengthDeltas[rule];

    public int InitialInfluenceKindId(int rule) => _initialInfluenceKindIds[rule];

    public int InitialInfluencePlaneCode(int rule) => _initialInfluencePlaneCodes[rule];

    public float InitialInfluenceValue(int rule) => _initialInfluenceValues[rule];

    public int StepCount(int rule) => StepCounts[rule];

    public int StepStart(int rule) => StepStarts[rule];

    public int StepOpcode(int step) => StepOpcodes[step];

    public int StepArgument(int step) => StepIntArguments[step];

    public float StepFloatArgument(int step) => StepFloatArguments[step];

    public int AffordanceSourceKindCode(int rule) => _affordanceSourceKindCodes[rule];

    public int AffordanceSourceId(int rule) => _affordanceSourceIds[rule];

    public int AffordanceShapeCode(int rule) => _affordanceShapeCodes[rule];

    public int AffordanceActorKindId(int rule) => _affordanceActorKindIds[rule];

    public int AffordancePlaneCode(int rule) => _affordancePlaneCodes[rule];

    public int AffordanceCandidateKindId(int rule) => _affordanceCandidateKindIds[rule];

    public int AffordanceRelationshipKindId(int rule) => _affordanceRelationshipKindIds[rule];

    public int AffordanceRelationshipDirectionCode(int rule) => _affordanceRelationshipDirectionCodes[rule];

    public int AffordanceOperationCode(int rule) => _affordanceOperationCodes[rule];

    public int AffordanceTerrainId(int rule) => _affordanceTerrainIds[rule];

    public int AffordancePolarityCode(int rule) => _affordancePolarityCodes[rule];

    public float AffordanceThreshold(int rule) => _affordanceThresholds[rule];

    public float AffordanceWeight(int rule) => _affordanceWeights[rule];

    public DecisionModifier ReadDecisionModifier(int rule) =>
        new(
            _traitModifierVerbIds[rule],
            _traitModifierKindIds[rule],
            _traitModifierTraitIds[rule],
            TraitEffectBearerCodes.Actor,
            _traitModifierWeightDeltas[rule],
            _traitModifierOpportunityBonusDeltas[rule],
            _traitModifierFitnessWeightDeltas[rule]);

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

    private static void ValidateOptionalPlaneCode(int planeCode)
    {
        if (planeCode is not PlaneCodes.None and not PlaneCodes.Physical and not PlaneCodes.Semantic)
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
            not ProgramOpcodeCodes.TryRelocateEntity and
            not ProgramOpcodeCodes.AdjustRelationshipStrength)
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

    private static float ClampDisabledInfluence(float value)
    {
        if (value < 0f)
        {
            return -1f;
        }

        return value > 100f ? 100f : value;
    }

    private static int ClampFlag(int value) => value == 0 ? 0 : 1;
}
