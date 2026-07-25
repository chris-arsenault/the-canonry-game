namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class LowAltitudeVerbProgramData : IKernelProgramData
{
    private int[] _kindIds = [];
    private int[] _verbIds = [];
    private int[] _planeCodes = [];
    private int[] _operationCodes = [];
    private int[] _influenceTargetingCodes = [];
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
    private FixedScalar[] StepScalarArguments = [];
    private int[] _minCommitmentTicks = [];
    private int[] _maxCommitmentTicks = [];
    private int[] _dwellTicks = [];
    private int[] _targetRadii = [];
    private int[] _candidateSampleCounts = [];
    private FixedScalar[] _weights = [];
    private FixedScalar[] _opportunityBonusOverrides = [];
    private FixedScalar[] _fitnessWeights = [];
    private FixedScalar[] _distanceScaleOverrides = [];
    private int[] _foreignInfluenceFilterEnabled = [];
    private int[] _foreignInfluenceBlockPositiveRelationships = [];
    private int[] _foreignInfluenceAllowNegativeRelationships = [];
    private FixedScalar[] _foreignInfluenceMinimumRelationshipStrengths = [];
    private FixedScalar[] _foreignInfluenceAllowUnrelatedInfluenceAtOrAbove = [];
    private int[] _fitnessRelationshipKindIds = [];
    private int[] _fitnessRelationshipDirectionCodes = [];
    private int[] _fitnessPolarityCodes = [];
    private FixedScalar[] _fitnessThresholds = [];
    private FixedScalar[] _influenceDeltas = [];
    private int[] _influenceRadii = [];
    private FixedScalar[] _coverageGrowthChances = [];
    private FixedScalar[] _headquartersMoveChances = [];
    private FixedScalar[] _requiredInfluences = [];
    private FixedScalar[] _relationshipStrengthDeltas = [];
    private int[] _initialInfluenceKindIds = [];
    private int[] _initialInfluencePlaneCodes = [];
    private FixedScalar[] _initialInfluenceValues = [];
    private int[] _affordanceSourceKindCodes = [];
    private int[] _affordanceSourceIds = [];
    private int[] _affordanceShapeCodes = [];
    private int[] _affordanceActorKindIds = [];
    private int[] _affordancePlaneCodes = [];
    private int[] _affordanceCandidateKindIds = [];
    private int[] _affordanceRelationshipKindIds = [];
    private int[] _affordanceRelationshipDirectionCodes = [];
    private int[] _affordanceOperationCodes = [];
    private int[] _affordancePolarityCodes = [];
    private FixedScalar[] _affordanceThresholds = [];
    private FixedScalar[] _affordanceWeights = [];
    private int[] _traitModifierKindIds = [];
    private int[] _traitModifierTraitIds = [];
    private int[] _traitModifierVerbIds = [];
    private FixedScalar[] _traitModifierWeightDeltas = [];
    private FixedScalar[] _traitModifierOpportunityBonusDeltas = [];
    private FixedScalar[] _traitModifierFitnessWeightDeltas = [];
    private int[] _firstRuleByKind = [];
    private int[] _lastRuleByKind = [];
    private int[] _nextRuleByKind = [];
    private int[] _firstInitialInfluenceByKind = [];
    private int[] _lastInitialInfluenceByKind = [];
    private int[] _nextInitialInfluenceByKind = [];
    private int[] _firstAffordanceByActorKind = [];
    private int[] _lastAffordanceByActorKind = [];
    private int[] _nextAffordanceByActorKind = [];
    private int[] _firstTraitModifierByKind = [];
    private int[] _lastTraitModifierByKind = [];
    private int[] _nextTraitModifierByKind = [];
    private int _ruleCount;
    private int _terrainCount;
    private int _stepCount;
    private int _initialInfluenceRuleCount;
    private FixedScalar[] _ageWeightMultipliers = [];
    private int _affordanceCount;
    private int _traitWeightModifierCount;

    internal int RuleCapacity => _kindIds.Length - 1;

    public LowAltitudeVerbProgramData(int ruleCapacity, int itemCapacity, int kindCapacity = 256)
    {
        NumericGuard.ThrowIfNegativeOrZero(ruleCapacity);
        NumericGuard.ThrowIfNegativeOrZero(itemCapacity);
        NumericGuard.ThrowIfNegativeOrZero(kindCapacity);
        var ruleLength = ruleCapacity + 1;
        var itemLength = itemCapacity + 1;
        var kindLength = kindCapacity + 1;
        InitializeRuleIdentityArrays(ruleLength);
        InitializeTargetAndTerrainArrays(ruleLength, itemLength);
        InitializeStepArrays(ruleLength, itemLength);
        InitializeCommitmentArrays(ruleLength);
        InitializeWeightArrays(ruleLength);
        InitializeInfluenceArrays(ruleLength);
        InitializeInitialInfluenceArrays(ruleLength);
        InitializeAffordanceArrays(itemLength);
        InitializeTraitWeightModifierArrays(itemLength);
        InitializeKindIndexes(kindLength, ruleLength, itemLength);
    }

    private void InitializeKindIndexes(int kindLength, int ruleLength, int itemLength)
    {
        _firstRuleByKind = new int[kindLength];
        _lastRuleByKind = new int[kindLength];
        _nextRuleByKind = new int[ruleLength];
        _firstInitialInfluenceByKind = new int[kindLength];
        _lastInitialInfluenceByKind = new int[kindLength];
        _nextInitialInfluenceByKind = new int[ruleLength];
        _firstAffordanceByActorKind = new int[kindLength];
        _lastAffordanceByActorKind = new int[kindLength];
        _nextAffordanceByActorKind = new int[itemLength];
        _firstTraitModifierByKind = new int[kindLength];
        _lastTraitModifierByKind = new int[kindLength];
        _nextTraitModifierByKind = new int[itemLength];
    }

    private void InitializeRuleIdentityArrays(int ruleLength)
    {
        _kindIds = new int[ruleLength];
        _verbIds = new int[ruleLength];
        _ageWeightMultipliers = new FixedScalar[ruleLength];
        global::System.Array.Fill(_ageWeightMultipliers, 1);
        _planeCodes = new int[ruleLength];
        _operationCodes = new int[ruleLength];
        _influenceTargetingCodes = new int[ruleLength];
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
        _minTemperatures = new int[ruleLength];
        _siteFlagFilters = new int[ruleLength];
        _fogAdjacentFilters = new byte[ruleLength];
        _restAtDestination = new byte[ruleLength];
    }

    private int[] _minTemperatures = [];
    private int[] _siteFlagFilters = [];
    private byte[] _fogAdjacentFilters = [];
    private byte[] _restAtDestination = [];

    public void SetRestAtDestination(int rule)
    {
        EnsureRule(rule);
        _restAtDestination[rule] = 1;
    }

    public bool RestsAtDestination(int rule) => _restAtDestination[rule] != 0;

    public void SetGeographyFilter(int rule, int minTemperature, int siteFlags, int fogAdjacent = 0)
    {
        EnsureRule(rule);
        if (minTemperature is < 0 or > 255 ||
            (siteFlags & ~PhysicalHexFieldCodes.WordMask) != 0 ||
            fogAdjacent is < 0 or > 1)
        {
            throw new global::System.InvalidOperationException();
        }

        _minTemperatures[rule] = minTemperature;
        _siteFlagFilters[rule] = siteFlags;
        _fogAdjacentFilters[rule] = (byte)fogAdjacent;
    }

    public int MinTemperature(int rule) => _minTemperatures[rule];

    public int SiteFlagsFilter(int rule) => _siteFlagFilters[rule];

    public bool RequiresFogAdjacent(int rule) => _fogAdjacentFilters[rule] != 0;

    private void InitializeStepArrays(int ruleLength, int itemLength)
    {
        StepStarts = new int[ruleLength];
        StepCounts = new int[ruleLength];
        StepOpcodes = new int[itemLength];
        StepIntArguments = new int[itemLength];
        StepScalarArguments = new FixedScalar[itemLength];
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
        _weights = new FixedScalar[ruleLength];
        _opportunityBonusOverrides = new FixedScalar[ruleLength];
        _fitnessWeights = new FixedScalar[ruleLength];
        _distanceScaleOverrides = new FixedScalar[ruleLength];
        _foreignInfluenceFilterEnabled = new int[ruleLength];
        _foreignInfluenceBlockPositiveRelationships = new int[ruleLength];
        _foreignInfluenceAllowNegativeRelationships = new int[ruleLength];
        _foreignInfluenceMinimumRelationshipStrengths = new FixedScalar[ruleLength];
        _foreignInfluenceAllowUnrelatedInfluenceAtOrAbove = new FixedScalar[ruleLength];
        _fitnessRelationshipKindIds = new int[ruleLength];
        _fitnessRelationshipDirectionCodes = new int[ruleLength];
        _fitnessPolarityCodes = new int[ruleLength];
        _fitnessThresholds = new FixedScalar[ruleLength];
    }

    private void InitializeInfluenceArrays(int ruleLength)
    {
        _influenceDeltas = new FixedScalar[ruleLength];
        _influenceRadii = new int[ruleLength];
        _coverageGrowthChances = new FixedScalar[ruleLength];
        _headquartersMoveChances = new FixedScalar[ruleLength];
        _requiredInfluences = new FixedScalar[ruleLength];
        _relationshipStrengthDeltas = new FixedScalar[ruleLength];
    }

    private void InitializeInitialInfluenceArrays(int ruleLength)
    {
        _initialInfluenceKindIds = new int[ruleLength];
        _initialInfluencePlaneCodes = new int[ruleLength];
        _initialInfluenceValues = new FixedScalar[ruleLength];
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
        _affordancePolarityCodes = new int[itemLength];
        _affordanceThresholds = new FixedScalar[itemLength];
        _affordanceWeights = new FixedScalar[itemLength];
    }

    private void InitializeTraitWeightModifierArrays(int itemLength)
    {
        _traitModifierKindIds = new int[itemLength];
        _traitModifierTraitIds = new int[itemLength];
        _traitModifierVerbIds = new int[itemLength];
        _traitModifierWeightDeltas = new FixedScalar[itemLength];
        _traitModifierOpportunityBonusDeltas = new FixedScalar[itemLength];
        _traitModifierFitnessWeightDeltas = new FixedScalar[itemLength];
    }

    public int ProgramId => KernelProgramCodes.LowAltitudeVerbs;

    public int SchemaVersion => 10;

    public int RuleCount => _ruleCount;

    public FixedScalar AgeWeightMultiplier(int rule) => _ageWeightMultipliers[rule];

    /// <summary>Age charges: neutralize, then apply per open age each world-beat pulse.</summary>
    public void ResetAgeWeightMultipliers()
    {
        for (var rule = 1; rule <= _ruleCount; rule++)
        {
            _ageWeightMultipliers[rule] = 1;
        }
    }

    public void ApplyAgeWeightMultiplier(int verbId, FixedScalar multiplier)
    {
        for (var rule = 1; rule <= _ruleCount; rule++)
        {
            if (verbId == 0 || _verbIds[rule] == verbId)
            {
                _ageWeightMultipliers[rule] *= multiplier;
            }
        }
    }


    public int InitialInfluenceRuleCount => _initialInfluenceRuleCount;

    public int AffordanceCount => _affordanceCount;

    public int TraitWeightModifierCount => _traitWeightModifierCount;

    public int RegisterVerb(
        in LowAltitudeVerbRegistration registration)
    {
        ValidateRegistration(
            registration.KindId,
            registration.VerbId,
            registration.PlaneCode,
            registration.OperationCode,
            registration.InfluenceTargetingCode,
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
        _influenceTargetingCodes[slot] = registration.InfluenceTargetingCode;
        _targetModeCodes[slot] = registration.TargetModeCode;
        _targetKindIds[slot] = registration.TargetKindId;
        _relationshipKindIds[slot] = registration.RelationshipKindId;
        _relationshipDirectionCodes[slot] = registration.RelationshipDirectionCode;
        _minCommitmentTicks[slot] = global::System.Math.Max(1, registration.MinTicks);
        _maxCommitmentTicks[slot] = global::System.Math.Max(_minCommitmentTicks[slot], registration.MaxTicks);
        _dwellTicks[slot] = global::System.Math.Max(0, registration.DwellTicks);
        _targetRadii[slot] = global::System.Math.Max(0, registration.TargetRadius);
        _candidateSampleCounts[slot] = global::System.Math.Max(1, registration.CandidateSampleCount);
        _weights[slot] = FixedScalar.Max(FixedScalar.FromUnits(10000L), registration.Weight);
        _opportunityBonusOverrides[slot] = registration.OpportunityBonusOverride;
        _fitnessWeights[slot] = registration.FitnessWeight;
        _distanceScaleOverrides[slot] = registration.DistanceScaleOverride;
        _foreignInfluenceFilterEnabled[slot] = ClampFlag(registration.ForeignInfluenceFilterEnabled);
        _foreignInfluenceBlockPositiveRelationships[slot] = ClampFlag(registration.ForeignInfluenceBlockPositiveRelationships);
        _foreignInfluenceAllowNegativeRelationships[slot] = ClampFlag(registration.ForeignInfluenceAllowNegativeRelationships);
        _foreignInfluenceMinimumRelationshipStrengths[slot] = FixedScalar.Max(0, registration.ForeignInfluenceMinimumRelationshipStrength);
        _foreignInfluenceAllowUnrelatedInfluenceAtOrAbove[slot] = ClampDisabledInfluence(registration.ForeignInfluenceAllowUnrelatedInfluenceAtOrAbove);
        _fitnessRelationshipKindIds[slot] = registration.FitnessRelationshipKindId;
        _fitnessRelationshipDirectionCodes[slot] = registration.FitnessRelationshipDirectionCode == 0
            ? LowAltitudeVerbRelationshipDirectionCodes.Either
            : registration.FitnessRelationshipDirectionCode;
        _fitnessPolarityCodes[slot] = registration.FitnessPolarityCode;
        _fitnessThresholds[slot] = registration.FitnessThreshold;
        _influenceDeltas[slot] = FixedScalar.Max(0, registration.InfluenceDelta);
        _influenceRadii[slot] = global::System.Math.Max(0, registration.InfluenceRadius);
        _coverageGrowthChances[slot] = Clamp01(registration.CoverageChance);
        _headquartersMoveChances[slot] = Clamp01(registration.RelocationChance);
        _requiredInfluences[slot] = ClampInfluence(registration.RequiredMapValue);
        _relationshipStrengthDeltas[slot] = registration.RelationshipStrengthDelta;
        AppendKindIndex(
            registration.KindId,
            slot,
            _firstRuleByKind,
            _lastRuleByKind,
            _nextRuleByKind);
        return slot;
    }

    public void AddPrimitiveStep(int rule, int opcode, int intArgument, FixedScalar scalarArgument)
    {
        EnsureRule(rule);
        ValidateOperationCode(opcode);
        if (StepCounts[rule] == 0)
        {
            StepStarts[rule] = _stepCount + 1;
        }

        AddStep(opcode, intArgument, scalarArgument);
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

    public int RegisterInitialInfluence(int kindId, int planeCode, FixedScalar influenceValue)
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
        AppendKindIndex(
            kindId,
            _initialInfluenceRuleCount,
            _firstInitialInfluenceByKind,
            _lastInitialInfluenceByKind,
            _nextInitialInfluenceByKind);
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
        int polarityCode,
        FixedScalar threshold,
        FixedScalar weight)
    {
        NumericGuard.ThrowIfNegative(sourceKindCode);
        NumericGuard.ThrowIfNegative(sourceId);
        ValidatePositive(shapeCode);
        ValidatePositive(actorKindId);
        ValidateOptionalPlaneCode(planeCode);
        NumericGuard.ThrowIfNegative(candidateKindId);
        NumericGuard.ThrowIfNegative(relationshipKindId);
        NumericGuard.ThrowIfNegative(relationshipDirectionCode);
        NumericGuard.ThrowIfNegative(operationCode);
        NumericGuard.ThrowIfNegative(polarityCode);
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
        _affordancePolarityCodes[_affordanceCount] = polarityCode;
        _affordanceThresholds[_affordanceCount] = FixedScalar.Max(0, threshold);
        _affordanceWeights[_affordanceCount] = FixedScalar.Max(0, weight);
        AppendKindIndex(
            actorKindId,
            _affordanceCount,
            _firstAffordanceByActorKind,
            _lastAffordanceByActorKind,
            _nextAffordanceByActorKind);
        return _affordanceCount;
    }

    public int RegisterTraitWeightModifier(
        int kindId,
        int traitId,
        int verbId,
        FixedScalar weightDelta,
        FixedScalar opportunityBonusDelta,
        FixedScalar fitnessWeightDelta)
    {
        ValidatePositive(kindId);
        ValidatePositive(traitId);
        NumericGuard.ThrowIfNegative(verbId);
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
        AppendKindIndex(
            kindId,
            _traitWeightModifierCount,
            _firstTraitModifierByKind,
            _lastTraitModifierByKind,
            _nextTraitModifierByKind);
        return _traitWeightModifierCount;
    }

    public int FirstRuleForKind(int kindId) => FirstKindItem(kindId, _firstRuleByKind);

    public int NextRuleForKind(int rule) => _nextRuleByKind[rule];

    public int FirstInitialInfluenceForKind(int kindId) =>
        FirstKindItem(kindId, _firstInitialInfluenceByKind);

    public int NextInitialInfluenceForKind(int rule) => _nextInitialInfluenceByKind[rule];

    public int FirstAffordanceForActorKind(int kindId) =>
        FirstKindItem(kindId, _firstAffordanceByActorKind);

    public int NextAffordanceForActorKind(int affordance) => _nextAffordanceByActorKind[affordance];

    public int FirstTraitModifierForKind(int kindId) =>
        FirstKindItem(kindId, _firstTraitModifierByKind);

    public int NextTraitModifierForKind(int modifier) => _nextTraitModifierByKind[modifier];

    public int KindId(int rule) => _kindIds[rule];

    public int VerbId(int rule) => _verbIds[rule];

    public int PlaneCode(int rule) => _planeCodes[rule];

    public int OperationCode(int rule) => _operationCodes[rule];

    public int InfluenceTargetingCode(int rule) => _influenceTargetingCodes[rule];

    public int TargetModeCode(int rule) => _targetModeCodes[rule];

    public int TargetKindId(int rule) => _targetKindIds[rule];

    public int RelationshipKindId(int rule) => _relationshipKindIds[rule];

    public int RelationshipDirectionCode(int rule) => _relationshipDirectionCodes[rule];

    public int MinCommitmentTicks(int rule) => _minCommitmentTicks[rule];

    public int MaxCommitmentTicks(int rule) => _maxCommitmentTicks[rule];

    public int DwellTicks(int rule) => _dwellTicks[rule];

    public int TargetRadius(int rule) => _targetRadii[rule];

    public int CandidateSampleCount(int rule) => _candidateSampleCounts[rule];

    public FixedScalar Weight(int rule) => _weights[rule];

    public FixedScalar OpportunityBonusOverride(int rule) => _opportunityBonusOverrides[rule];

    public FixedScalar FitnessWeight(int rule) => _fitnessWeights[rule];

    public FixedScalar DistanceScaleOverride(int rule) => _distanceScaleOverrides[rule];

    public bool ForeignInfluenceFilterEnabled(int rule) => _foreignInfluenceFilterEnabled[rule] != 0;

    public bool ForeignInfluenceBlocksPositiveRelationships(int rule) => _foreignInfluenceBlockPositiveRelationships[rule] != 0;

    public bool ForeignInfluenceAllowsNegativeRelationships(int rule) => _foreignInfluenceAllowNegativeRelationships[rule] != 0;

    public FixedScalar ForeignInfluenceMinimumRelationshipStrength(int rule) => _foreignInfluenceMinimumRelationshipStrengths[rule];

    public FixedScalar ForeignInfluenceAllowUnrelatedInfluenceAtOrAbove(int rule) => _foreignInfluenceAllowUnrelatedInfluenceAtOrAbove[rule];

    public int FitnessRelationshipKindId(int rule) => _fitnessRelationshipKindIds[rule];

    public int FitnessRelationshipDirectionCode(int rule) => _fitnessRelationshipDirectionCodes[rule];

    public int FitnessPolarityCode(int rule) => _fitnessPolarityCodes[rule];

    public FixedScalar FitnessThreshold(int rule) => _fitnessThresholds[rule];

    public FixedScalar InfluenceDelta(int rule) => _influenceDeltas[rule];

    public int InfluenceRadius(int rule) => _influenceRadii[rule];

    public FixedScalar CoverageGrowthChance(int rule) => _coverageGrowthChances[rule];

    public FixedScalar HeadquartersMoveChance(int rule) => _headquartersMoveChances[rule];

    public FixedScalar RequiredInfluence(int rule) => _requiredInfluences[rule];

    public FixedScalar RelationshipStrengthDelta(int rule) => _relationshipStrengthDeltas[rule];

    public int InitialInfluenceKindId(int rule) => _initialInfluenceKindIds[rule];

    public int InitialInfluencePlaneCode(int rule) => _initialInfluencePlaneCodes[rule];

    public FixedScalar InitialInfluenceValue(int rule) => _initialInfluenceValues[rule];

    public int StepCount(int rule) => StepCounts[rule];

    public int StepStart(int rule) => StepStarts[rule];

    public int StepOpcode(int step) => StepOpcodes[step];

    public int StepArgument(int step) => StepIntArguments[step];

    public FixedScalar StepScalarArgument(int step) => StepScalarArguments[step];

    public int AffordanceSourceKindCode(int rule) => _affordanceSourceKindCodes[rule];

    public int AffordanceSourceId(int rule) => _affordanceSourceIds[rule];

    public int AffordanceShapeCode(int rule) => _affordanceShapeCodes[rule];

    public int AffordanceActorKindId(int rule) => _affordanceActorKindIds[rule];

    public int AffordancePlaneCode(int rule) => _affordancePlaneCodes[rule];

    public int AffordanceCandidateKindId(int rule) => _affordanceCandidateKindIds[rule];

    public int AffordanceRelationshipKindId(int rule) => _affordanceRelationshipKindIds[rule];

    public int AffordanceRelationshipDirectionCode(int rule) => _affordanceRelationshipDirectionCodes[rule];

    public int AffordanceOperationCode(int rule) => _affordanceOperationCodes[rule];

    public int AffordancePolarityCode(int rule) => _affordancePolarityCodes[rule];

    public FixedScalar AffordanceThreshold(int rule) => _affordanceThresholds[rule];

    public FixedScalar AffordanceWeight(int rule) => _affordanceWeights[rule];

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

    private static int FirstKindItem(int kindId, int[] firstByKind) =>
        (uint)kindId < (uint)firstByKind.Length ? firstByKind[kindId] : 0;

    private static void AppendKindIndex(
        int kindId,
        int item,
        int[] firstByKind,
        int[] lastByKind,
        int[] nextByItem)
    {
        if ((uint)kindId >= (uint)firstByKind.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        var previous = lastByKind[kindId];
        if (previous > 0)
        {
            nextByItem[previous] = item;
        }
        else
        {
            firstByKind[kindId] = item;
        }

        lastByKind[kindId] = item;
    }

    private static void ValidatePositive(int value)
    {
        NumericGuard.ThrowIfNegativeOrZero(value);
    }

    private static void ValidateRegistration(
        int kindId,
        int verbId,
        int planeCode,
        int operationCode,
        int influenceTargetingCode,
        int targetModeCode)
    {
        ValidatePositive(kindId);
        ValidatePositive(verbId);
        ValidatePlaneCode(planeCode);
        ValidateOperationCode(operationCode);
        ValidateInfluenceTargetingCode(influenceTargetingCode);
        ValidateTargetModeCode(targetModeCode);
    }

    private void AddStep(int opcode, int intArgument, FixedScalar scalarArgument)
    {
        if (_stepCount + 1 >= StepOpcodes.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _stepCount++;
        StepOpcodes[_stepCount] = opcode;
        StepIntArguments[_stepCount] = intArgument;
        StepScalarArguments[_stepCount] = scalarArgument;
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
            not TargetSelectorCodes.OtherMapValue and
            not TargetSelectorCodes.ChroniclePursuit and
            not TargetSelectorCodes.AttractorTrek)
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

    private static void ValidateInfluenceTargetingCode(int influenceTargetingCode)
    {
        if (influenceTargetingCode is not LowAltitudeInfluenceTargetingCodes.None and
            not LowAltitudeInfluenceTargetingCodes.Spread and
            not LowAltitudeInfluenceTargetingCodes.Reinforce and
            not LowAltitudeInfluenceTargetingCodes.Contest)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(influenceTargetingCode));
        }
    }

    private static FixedScalar Clamp01(FixedScalar value)
    {
        if (value < 0)
        {
            return 0;
        }

        return value > 1 ? 1 : value;
    }

    private static FixedScalar ClampInfluence(FixedScalar value)
    {
        if (value < 0)
        {
            return 0;
        }

        return value > 100 ? 100 : value;
    }

    private static FixedScalar ClampDisabledInfluence(FixedScalar value)
    {
        if (value < 0)
        {
            return -1;
        }

        return value > 100 ? 100 : value;
    }

    private static int ClampFlag(int value) => value == 0 ? 0 : 1;
}
