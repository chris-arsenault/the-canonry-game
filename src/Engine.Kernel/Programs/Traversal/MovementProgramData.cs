namespace Engine.Kernel;

internal sealed partial class MovementProgramData : IKernelProgramData
{
    /// <summary>Relationship kind whose holder is a laden carrier (0 = carriers disabled).</summary>
    internal int CarrierRelationshipKindId;

    private const int DefaultMovementProfileId = 1;
    private readonly PrimitiveIntTable _defaultMovementProfiles;
    private readonly int[] _subtypeProfileKindIds;
    private readonly int[] _subtypeProfileSubtypeIds;
    private readonly int[] _subtypeProfilePlaneCodes;
    private readonly int[] _subtypeProfileIds;
    private readonly int[] _traitProfileKindIds;
    private readonly int[] _traitProfileTraitIds;
    private readonly int[] _traitProfilePlaneCodes;
    private readonly int[] _traitProfileIds;
    private readonly PrimitiveIntTable _travelStructurePlaneCodes;
    private readonly PrimitiveFloatTable _travelStructureProfileCosts;
    private int _subtypeProfileRuleCount;
    private int _traitProfileRuleCount;

    public MovementProgramData(
        int profileRuleCapacity = 256,
        float roadBuildPerTravel = 1f,
        float roadDecayPerTick = 0.9995f)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(profileRuleCapacity);
        _defaultMovementProfiles = new PrimitiveIntTable(profileRuleCapacity);
        _subtypeProfileKindIds = new int[profileRuleCapacity + 1];
        _subtypeProfileSubtypeIds = new int[profileRuleCapacity + 1];
        _subtypeProfilePlaneCodes = new int[profileRuleCapacity + 1];
        _subtypeProfileIds = new int[profileRuleCapacity + 1];
        _traitProfileKindIds = new int[profileRuleCapacity + 1];
        _traitProfileTraitIds = new int[profileRuleCapacity + 1];
        _traitProfilePlaneCodes = new int[profileRuleCapacity + 1];
        _traitProfileIds = new int[profileRuleCapacity + 1];
        _travelStructurePlaneCodes = new PrimitiveIntTable(profileRuleCapacity);
        _travelStructureProfileCosts = new PrimitiveFloatTable(profileRuleCapacity);
        RoadBuildPerTravel = roadBuildPerTravel;
        RoadDecayPerTick = roadDecayPerTick;
    }

    public int ProgramId => KernelProgramCodes.Movement;

    public int SchemaVersion => 3;

    public float RoadBuildPerTravel { get; set; }

    public float RoadDecayPerTick { get; set; }

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
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(subtypeId);
        if (_subtypeProfileRuleCount + 1 >= _subtypeProfileKindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _subtypeProfileRuleCount++;
        _subtypeProfileKindIds[_subtypeProfileRuleCount] = kindId;
        _subtypeProfileSubtypeIds[_subtypeProfileRuleCount] = subtypeId;
        _subtypeProfilePlaneCodes[_subtypeProfileRuleCount] = planeCode;
        _subtypeProfileIds[_subtypeProfileRuleCount] = movementProfileId;
    }

    public void RegisterTraitMovementProfile(
        int kindId,
        int traitId,
        int planeCode,
        int movementProfileId)
    {
        ValidateMovementProfileRule(kindId, planeCode, movementProfileId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(traitId);
        if (_traitProfileRuleCount + 1 >= _traitProfileKindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _traitProfileRuleCount++;
        _traitProfileKindIds[_traitProfileRuleCount] = kindId;
        _traitProfileTraitIds[_traitProfileRuleCount] = traitId;
        _traitProfilePlaneCodes[_traitProfileRuleCount] = planeCode;
        _traitProfileIds[_traitProfileRuleCount] = movementProfileId;
    }

    public void RegisterTravelStructure(
        int travelStructureId,
        int planeCode)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(travelStructureId);
        ValidatePlaneCode(planeCode);
        _travelStructurePlaneCodes.Set(travelStructureId, 0, planeCode);
    }

    public void RegisterTravelStructureMovementProfile(
        int travelStructureId,
        int movementProfileId,
        float movementCost)
    {
        EnsureTravelStructureRegistered(travelStructureId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(movementProfileId);
        _travelStructureProfileCosts.Set(
            travelStructureId,
            movementProfileId,
            movementCost < 0f ? 0f : movementCost);
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

    public float TravelStructureCost(int travelStructureId, int planeCode, int movementProfileId)
    {
        if (travelStructureId <= 0)
        {
            return 0f;
        }

        if (_travelStructurePlaneCodes.GetOrDefault(travelStructureId, 0) != planeCode)
        {
            return 0f;
        }

        return _travelStructureProfileCosts.GetOrDefault(travelStructureId, movementProfileId);
    }

    public int MovementProfileFor(WorldState world, int entityId, int planeCode)
    {
        global::System.ArgumentNullException.ThrowIfNull(world);
        ValidatePlaneCode(planeCode);
        var kindId = world.EntityKindId(entityId);
        var profileId = MovementProfileFor(kindId, world.EntitySubtypeId(entityId), planeCode);
        for (var rule = 1; rule <= _traitProfileRuleCount; rule++)
        {
            if (_traitProfileKindIds[rule] == kindId &&
                _traitProfilePlaneCodes[rule] == planeCode &&
                world.HasTrait(entityId, _traitProfileTraitIds[rule]))
            {
                profileId = _traitProfileIds[rule];
            }
        }

        return profileId;
    }

    private void EnsureTravelStructureRegistered(int travelStructureId)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(travelStructureId);
        if (!TravelStructureRegistered(travelStructureId))
        {
            throw new global::System.InvalidOperationException();
        }
    }

    public int MovementProfileFor(int kindId, int subtypeId, int planeCode)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(kindId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(subtypeId);
        ValidatePlaneCode(planeCode);
        var profileId = _defaultMovementProfiles.GetOrDefault(kindId, planeCode);
        if (profileId == 0)
        {
            profileId = DefaultMovementProfileId;
        }

        for (var rule = 1; rule <= _subtypeProfileRuleCount; rule++)
        {
            if (_subtypeProfileKindIds[rule] == kindId &&
                _subtypeProfileSubtypeIds[rule] == subtypeId &&
                _subtypeProfilePlaneCodes[rule] == planeCode)
            {
                profileId = _subtypeProfileIds[rule];
            }
        }

        return profileId;
    }

    private static void ValidateMovementProfileRule(int kindId, int planeCode, int movementProfileId)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(kindId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(movementProfileId);
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
