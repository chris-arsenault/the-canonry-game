namespace Engine.Kernel;

internal sealed partial class MovementProgramData : IKernelProgramData
{
    /// <summary>Relationship kind whose holder is a laden carrier (0 = carriers disabled).</summary>
    internal int CarrierRelationshipKindId;

    private const int DefaultMovementProfileId = 1;
    private readonly PrimitiveIntTable _defaultMovementProfiles;
    private readonly PrimitiveIntTable _subtypeMovementProfiles;
    private readonly int[] _traitProfileKindIds;
    private readonly int[] _traitProfileTraitIds;
    private readonly int[] _traitProfilePlaneCodes;
    private readonly int[] _traitProfileIds;
    private readonly int[] _firstTraitProfileByKind;
    private readonly int[] _lastTraitProfileByKind;
    private readonly int[] _nextTraitProfileByKind;
    // Knowledge enablement gates: a tech rule switches the kind's effective
    // movement profile while the entity's culture claims the tech (1-based
    // tech ids). Applied after trait rules - an earned capability wins.
    private readonly int[] _techProfileKindIds;
    private readonly int[] _techProfileTechIds;
    private readonly int[] _techProfilePlaneCodes;
    private readonly int[] _techProfileIds;
    private readonly int[] _firstTechProfileByKind;
    private readonly int[] _lastTechProfileByKind;
    private readonly int[] _nextTechProfileByKind;
    private readonly PrimitiveIntTable _travelStructurePlaneCodes;
    private readonly PrimitiveScalarTable _travelStructureProfileCosts;
    private int _traitProfileRuleCount;
    private int _techProfileRuleCount;

    public MovementProgramData(
        int profileRuleCapacity = 256,
        FixedScalar? roadBuildPerTravel = null,
        FixedScalar? roadDecayPerTick = null,
        int kindCapacity = 256)
    {
        NumericGuard.ThrowIfNegativeOrZero(profileRuleCapacity);
        NumericGuard.ThrowIfNegativeOrZero(kindCapacity);
        _defaultMovementProfiles = new PrimitiveIntTable(profileRuleCapacity);
        _subtypeMovementProfiles = new PrimitiveIntTable(profileRuleCapacity);
        _traitProfileKindIds = new int[profileRuleCapacity + 1];
        _traitProfileTraitIds = new int[profileRuleCapacity + 1];
        _traitProfilePlaneCodes = new int[profileRuleCapacity + 1];
        _traitProfileIds = new int[profileRuleCapacity + 1];
        _firstTraitProfileByKind = new int[kindCapacity + 1];
        _lastTraitProfileByKind = new int[kindCapacity + 1];
        _nextTraitProfileByKind = new int[profileRuleCapacity + 1];
        _techProfileKindIds = new int[profileRuleCapacity + 1];
        _techProfileTechIds = new int[profileRuleCapacity + 1];
        _techProfilePlaneCodes = new int[profileRuleCapacity + 1];
        _techProfileIds = new int[profileRuleCapacity + 1];
        _firstTechProfileByKind = new int[kindCapacity + 1];
        _lastTechProfileByKind = new int[kindCapacity + 1];
        _nextTechProfileByKind = new int[profileRuleCapacity + 1];
        _travelStructurePlaneCodes = new PrimitiveIntTable(profileRuleCapacity);
        _travelStructureProfileCosts = new PrimitiveScalarTable(profileRuleCapacity);
        RoadBuildPerTravel = roadBuildPerTravel ?? FixedScalar.One;
        RoadDecayPerTick = roadDecayPerTick ?? FixedScalar.FromUnits(999_500L);
    }

    public int ProgramId => KernelProgramCodes.Movement;

    public int SchemaVersion => 4;

    public FixedScalar RoadBuildPerTravel { get; set; }

    /// <summary>Step-cost multiplier per unit of hex roughness (0 disables).</summary>
    public FixedScalar RoughnessCostWeight { get; set; }

    public FixedScalar RoadDecayPerTick { get; set; }

    public void RegisterDefaultMovementProfile(
        int kindId,
        int planeCode,
        int movementProfileId)
    {
        ValidateMovementProfileRule(kindId, planeCode, movementProfileId);
        _defaultMovementProfiles.Set(kindId, planeCode, movementProfileId);
    }

    public void RegisterSubtypeMovementProfile(
        int kindId,
        int subtypeId,
        int planeCode,
        int movementProfileId)
    {
        ValidateMovementProfileRule(kindId, planeCode, movementProfileId);
        NumericGuard.ThrowIfNegativeOrZero(subtypeId);
        _subtypeMovementProfiles.Set(kindId, SubtypeProfileKey(subtypeId, planeCode), movementProfileId);
    }

    public void RegisterTraitMovementProfile(
        int kindId,
        int traitId,
        int planeCode,
        int movementProfileId)
    {
        ValidateMovementProfileRule(kindId, planeCode, movementProfileId);
        NumericGuard.ThrowIfNegativeOrZero(traitId);
        if (_traitProfileRuleCount + 1 >= _traitProfileKindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _traitProfileRuleCount++;
        _traitProfileKindIds[_traitProfileRuleCount] = kindId;
        _traitProfileTraitIds[_traitProfileRuleCount] = traitId;
        _traitProfilePlaneCodes[_traitProfileRuleCount] = planeCode;
        _traitProfileIds[_traitProfileRuleCount] = movementProfileId;
        AppendTraitProfileIndex(kindId, _traitProfileRuleCount);
    }

    public void RegisterTechMovementProfile(
        int kindId,
        int techId,
        int planeCode,
        int movementProfileId)
    {
        ValidateMovementProfileRule(kindId, planeCode, movementProfileId);
        NumericGuard.ThrowIfNegativeOrZero(techId);
        if (_techProfileRuleCount + 1 >= _techProfileKindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _techProfileRuleCount++;
        _techProfileKindIds[_techProfileRuleCount] = kindId;
        _techProfileTechIds[_techProfileRuleCount] = techId;
        _techProfilePlaneCodes[_techProfileRuleCount] = planeCode;
        _techProfileIds[_techProfileRuleCount] = movementProfileId;
        AppendTechProfileIndex(kindId, _techProfileRuleCount);
    }

    public void RegisterTravelStructure(
        int travelStructureId,
        int planeCode)
    {
        NumericGuard.ThrowIfNegativeOrZero(travelStructureId);
        ValidatePlaneCode(planeCode);
        _travelStructurePlaneCodes.Set(travelStructureId, 0, planeCode);
    }

    public void RegisterTravelStructureMovementProfile(
        int travelStructureId,
        int movementProfileId,
        FixedScalar movementCost)
    {
        EnsureTravelStructureRegistered(travelStructureId);
        NumericGuard.ThrowIfNegativeOrZero(movementProfileId);
        _travelStructureProfileCosts.Set(
            travelStructureId,
            movementProfileId,
            movementCost < 0 ? 0 : movementCost);
    }

    public bool TravelStructureRegistered(int travelStructureId)
    {
        return travelStructureId > 0 &&
            _travelStructurePlaneCodes.GetOrDefault(travelStructureId, 0) != 0;
    }

    public int TravelStructurePlaneCode(int travelStructureId)
    {
        EnsureTravelStructureRegistered(travelStructureId);
        return _travelStructurePlaneCodes.GetOrDefault(travelStructureId, 0);
    }

    public FixedScalar TravelStructureCost(int travelStructureId, int planeCode, int movementProfileId)
    {
        if (travelStructureId <= 0)
        {
            return 0;
        }

        if (_travelStructurePlaneCodes.GetOrDefault(travelStructureId, 0) != planeCode)
        {
            return 0;
        }

        return _travelStructureProfileCosts.GetOrDefault(travelStructureId, movementProfileId);
    }

    public int MovementProfileFor(
        WorldState world,
        KnowledgeProgramRuntime knowledge,
        int entityId,
        int planeCode)
    {
        global::System.ArgumentNullException.ThrowIfNull(world);
        global::System.ArgumentNullException.ThrowIfNull(knowledge);
        ValidatePlaneCode(planeCode);
        var kindId = world.EntityKindId(entityId);
        var profileId = MovementProfileFor(kindId, world.EntitySubtypeId(entityId), planeCode);
        for (var rule = FirstTraitProfileForKind(kindId);
             rule > 0;
             rule = _nextTraitProfileByKind[rule])
        {
            if (_traitProfileKindIds[rule] == kindId &&
                _traitProfilePlaneCodes[rule] == planeCode &&
                world.HasTrait(entityId, _traitProfileTraitIds[rule]))
            {
                profileId = _traitProfileIds[rule];
            }
        }

        var cultureId = _techProfileRuleCount > 0 ? world.EntityCultureId(entityId) : 0;
        for (var rule = FirstTechProfileForKind(kindId);
             rule > 0;
             rule = _nextTechProfileByKind[rule])
        {
            if (_techProfileKindIds[rule] == kindId &&
                _techProfilePlaneCodes[rule] == planeCode &&
                knowledge.CultureClaimsTech(cultureId, _techProfileTechIds[rule]))
            {
                profileId = _techProfileIds[rule];
            }
        }

        return profileId;
    }

    private void EnsureTravelStructureRegistered(int travelStructureId)
    {
        NumericGuard.ThrowIfNegativeOrZero(travelStructureId);
        if (!TravelStructureRegistered(travelStructureId))
        {
            throw new global::System.InvalidOperationException();
        }
    }

    public int MovementProfileFor(int kindId, int subtypeId, int planeCode)
    {
        NumericGuard.ThrowIfNegativeOrZero(kindId);
        NumericGuard.ThrowIfNegative(subtypeId);
        ValidatePlaneCode(planeCode);
        var profileId = _defaultMovementProfiles.GetOrDefault(kindId, planeCode);
        if (profileId == 0)
        {
            profileId = DefaultMovementProfileId;
        }

        var subtypeProfileId = _subtypeMovementProfiles.GetOrDefault(
            kindId,
            SubtypeProfileKey(subtypeId, planeCode));
        if (subtypeProfileId > 0)
        {
            profileId = subtypeProfileId;
        }

        return profileId;
    }

    private int FirstTraitProfileForKind(int kindId) =>
        (uint)kindId < (uint)_firstTraitProfileByKind.Length
            ? _firstTraitProfileByKind[kindId]
            : 0;

    private int FirstTechProfileForKind(int kindId) =>
        (uint)kindId < (uint)_firstTechProfileByKind.Length
            ? _firstTechProfileByKind[kindId]
            : 0;

    private void AppendTechProfileIndex(int kindId, int rule)
    {
        if ((uint)kindId >= (uint)_firstTechProfileByKind.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        var previous = _lastTechProfileByKind[kindId];
        if (previous > 0)
        {
            _nextTechProfileByKind[previous] = rule;
        }
        else
        {
            _firstTechProfileByKind[kindId] = rule;
        }

        _lastTechProfileByKind[kindId] = rule;
    }

    private void AppendTraitProfileIndex(int kindId, int rule)
    {
        if ((uint)kindId >= (uint)_firstTraitProfileByKind.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        var previous = _lastTraitProfileByKind[kindId];
        if (previous > 0)
        {
            _nextTraitProfileByKind[previous] = rule;
        }
        else
        {
            _firstTraitProfileByKind[kindId] = rule;
        }

        _lastTraitProfileByKind[kindId] = rule;
    }

    private static int SubtypeProfileKey(int subtypeId, int planeCode) =>
        checked((subtypeId << 2) | planeCode);

    private static void ValidateMovementProfileRule(int kindId, int planeCode, int movementProfileId)
    {
        NumericGuard.ThrowIfNegativeOrZero(kindId);
        NumericGuard.ThrowIfNegativeOrZero(movementProfileId);
        ValidatePlaneCode(planeCode);
    }

    private static void ValidatePlaneCode(int planeCode)
    {
        if (planeCode is not PlaneCodes.Physical and not PlaneCodes.Semantic)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(planeCode));
        }
    }
}
