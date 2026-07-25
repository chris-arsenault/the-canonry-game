namespace Engine.Kernel;

internal sealed partial class KernelTerrainData
{
    private readonly bool[] _registered;
    private readonly int[] _boundaryGroupIds;
    private readonly FixedScalar[] _movementCosts;
    private readonly FixedScalar[] _influenceMultipliers;
    private readonly FixedScalar[] _claimCostMultipliers;
    private readonly FixedScalar[] _claimChanceMultipliers;
    private readonly FixedScalar[] _roadBuildMultipliers;
    private readonly FixedScalar[] _roadSpeedMultipliers;
    private readonly PrimitiveScalarTable _movementProfileCosts;
    private readonly PrimitiveScalarTable _crossingFactors;
    private readonly bool[] _crossingRoadNegated;
    private readonly bool[] _isWater;
    private readonly PrimitiveIntTable _kindProfileFlags;
    private readonly PrimitiveScalarTable _kindInfluenceMultipliers;
    private readonly PrimitiveScalarTable _kindClaimCostMultipliers;
    private readonly PrimitiveScalarTable _kindClaimChanceMultipliers;

    public KernelTerrainData(int terrainIdCapacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(terrainIdCapacity);
        _registered = new bool[terrainIdCapacity + 1];
        _boundaryGroupIds = new int[terrainIdCapacity + 1];
        _movementCosts = new FixedScalar[terrainIdCapacity + 1];
        _influenceMultipliers = new FixedScalar[terrainIdCapacity + 1];
        _claimCostMultipliers = new FixedScalar[terrainIdCapacity + 1];
        _claimChanceMultipliers = new FixedScalar[terrainIdCapacity + 1];
        _roadBuildMultipliers = new FixedScalar[terrainIdCapacity + 1];
        _roadSpeedMultipliers = new FixedScalar[terrainIdCapacity + 1];
        var overrideCapacity = global::System.Math.Max(16, terrainIdCapacity * 8);
        _movementProfileCosts = new PrimitiveScalarTable(overrideCapacity);
        _crossingFactors = new PrimitiveScalarTable(overrideCapacity);
        _crossingRoadNegated = new bool[PhysicalCrossingCodes.ClassMask + 1];
        _isWater = new bool[terrainIdCapacity + 1];
        _kindProfileFlags = new PrimitiveIntTable(overrideCapacity);
        _kindInfluenceMultipliers = new PrimitiveScalarTable(overrideCapacity);
        _kindClaimCostMultipliers = new PrimitiveScalarTable(overrideCapacity);
        _kindClaimChanceMultipliers = new PrimitiveScalarTable(overrideCapacity);
    }

    public void RegisterTerrain(int terrainId, int boundaryGroupId) =>
        RegisterTerrain(
            terrainId,
            boundaryGroupId,
            movementCost: 1,
            influenceMultiplier: 1,
            claimCostMultiplier: 1,
            claimChanceMultiplier: 1,
            roadBuildMultiplier: 1,
            roadSpeedMultiplier: 1);

    public void RegisterTerrain(
        int terrainId,
        int boundaryGroupId,
        FixedScalar movementCost,
        FixedScalar influenceMultiplier,
        FixedScalar claimCostMultiplier,
        FixedScalar claimChanceMultiplier,
        FixedScalar roadBuildMultiplier,
        FixedScalar roadSpeedMultiplier)
    {
        ValidatePositive(terrainId);
        ValidatePositive(boundaryGroupId);
        if ((uint)terrainId >= (uint)_registered.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _registered[terrainId] = true;
        _boundaryGroupIds[terrainId] = boundaryGroupId;
        _movementCosts[terrainId] = ClampNonNegative(movementCost);
        _influenceMultipliers[terrainId] = ClampNonNegative(influenceMultiplier);
        _claimCostMultipliers[terrainId] = ClampNonNegative(claimCostMultiplier);
        _claimChanceMultipliers[terrainId] = ClampNonNegative(claimChanceMultiplier);
        _roadBuildMultipliers[terrainId] = ClampNonNegative(roadBuildMultiplier);
        _roadSpeedMultipliers[terrainId] = ClampAtLeast(roadSpeedMultiplier, 1);
    }

    public void RegisterTerrainMovementProfile(
        int terrainId,
        int movementProfileId,
        FixedScalar movementCost)
    {
        EnsureTerrain(terrainId);
        ValidatePositive(movementProfileId);
        _movementProfileCosts.Set(terrainId, movementProfileId, ClampNonNegative(movementCost));
    }

    public void RegisterTerrainKindProfile(
        int terrainId,
        int kindId,
        int fieldFlags,
        FixedScalar influenceMultiplier,
        FixedScalar claimCostMultiplier,
        FixedScalar claimChanceMultiplier)
    {
        EnsureTerrain(terrainId);
        ValidatePositive(kindId);
        if (fieldFlags == 0)
        {
            return;
        }

        _kindProfileFlags.Set(terrainId, kindId, fieldFlags);
        if ((fieldFlags & TerrainKindProfileFieldCodes.InfluenceMultiplier) != 0)
        {
            _kindInfluenceMultipliers.Set(terrainId, kindId, ClampNonNegative(influenceMultiplier));
        }

        if ((fieldFlags & TerrainKindProfileFieldCodes.ClaimCostMultiplier) != 0)
        {
            _kindClaimCostMultipliers.Set(terrainId, kindId, ClampNonNegative(claimCostMultiplier));
        }

        if ((fieldFlags & TerrainKindProfileFieldCodes.ClaimChanceMultiplier) != 0)
        {
            _kindClaimChanceMultipliers.Set(terrainId, kindId, ClampNonNegative(claimChanceMultiplier));
        }
    }

    /// <summary>
    /// Registers the step-cost factor a movement profile pays to cross an
    /// edge of the given crossing class. Unregistered pairs default to 1
    /// (crossings only ever surcharge; they never block or discount).
    /// </summary>
    public void RegisterCrossingProfile(
        int crossingClass,
        int movementProfileId,
        FixedScalar factor,
        bool roadNegated)
    {
        if (crossingClass <= PhysicalCrossingCodes.Open ||
            crossingClass > PhysicalCrossingCodes.ClassMask)
        {
            throw new global::System.InvalidOperationException();
        }

        ValidatePositive(movementProfileId);
        _crossingFactors.Set(crossingClass, movementProfileId, ClampAtLeast(factor, 1));
        _crossingRoadNegated[crossingClass] = roadNegated;
    }

    public FixedScalar CrossingFactor(int crossingClass, int movementProfileId) =>
        _crossingFactors.GetOrDefault(crossingClass, movementProfileId, 1);

    public bool CrossingRoadNegated(int crossingClass) =>
        (uint)crossingClass < (uint)_crossingRoadNegated.Length && _crossingRoadNegated[crossingClass];

    /// <summary>
    /// Marks a terrain as open water. Authored repaints never cross the
    /// land/water divide; the executor consults this before applying them.
    /// </summary>
    public void SetWater(int terrainId)
    {
        EnsureTerrain(terrainId);
        _isWater[terrainId] = true;
    }

    public bool IsWater(int terrainId) =>
        (uint)terrainId < (uint)_isWater.Length && _isWater[terrainId];

    public int BoundaryGroupId(int terrainId)
    {
        EnsureTerrain(terrainId);
        return _boundaryGroupIds[terrainId];
    }

    public bool CanEnter(int terrainId, int movementProfileId) => TravelCost(terrainId, movementProfileId) > 0;

    public FixedScalar TravelCost(int terrainId, int movementProfileId)
    {
        EnsureTerrain(terrainId);
        ValidatePositive(movementProfileId);
        return _movementProfileCosts.GetOrDefault(terrainId, movementProfileId, _movementCosts[terrainId]);
    }

    public FixedScalar RoadAdjustedTravelCost(int terrainId, int movementProfileId, FixedScalar roadQuality)
    {
        var travelCost = TravelCost(terrainId, movementProfileId);
        if (travelCost <= 0)
        {
            return FixedScalar.PositiveInfinity;
        }

        var normalizedRoadQuality = Clamp01(roadQuality / 100);
        var roadSpeed = 1 + ((RoadSpeedMultiplier(terrainId) - 1) * normalizedRoadQuality);
        return travelCost / roadSpeed;
    }

    public FixedScalar InfluenceMultiplier(int terrainId, int kindId)
    {
        EnsureTerrain(terrainId);
        return KindProfileValue(
            terrainId,
            kindId,
            TerrainKindProfileFieldCodes.InfluenceMultiplier,
            _kindInfluenceMultipliers,
            _influenceMultipliers[terrainId]);
    }

    public FixedScalar ClaimCostMultiplier(int terrainId, int kindId)
    {
        EnsureTerrain(terrainId);
        return KindProfileValue(
            terrainId,
            kindId,
            TerrainKindProfileFieldCodes.ClaimCostMultiplier,
            _kindClaimCostMultipliers,
            _claimCostMultipliers[terrainId]);
    }

    public FixedScalar ClaimChanceMultiplier(int terrainId, int kindId)
    {
        EnsureTerrain(terrainId);
        return KindProfileValue(
            terrainId,
            kindId,
            TerrainKindProfileFieldCodes.ClaimChanceMultiplier,
            _kindClaimChanceMultipliers,
            _claimChanceMultipliers[terrainId]);
    }

    public FixedScalar RoadBuildMultiplier(int terrainId)
    {
        EnsureTerrain(terrainId);
        return _roadBuildMultipliers[terrainId];
    }

    public FixedScalar RoadSpeedMultiplier(int terrainId)
    {
        EnsureTerrain(terrainId);
        return _roadSpeedMultipliers[terrainId];
    }

    private FixedScalar KindProfileValue(
        int terrainId,
        int kindId,
        int fieldFlag,
        PrimitiveScalarTable values,
        FixedScalar fallback)
    {
        if (kindId <= 0 ||
            (_kindProfileFlags.GetOrDefault(terrainId, kindId) & fieldFlag) == 0)
        {
            return fallback;
        }

        return values.GetOrDefault(terrainId, kindId, fallback);
    }

    private void EnsureTerrain(int terrainId)
    {
        if ((uint)terrainId >= (uint)_registered.Length || !_registered[terrainId])
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private static void ValidatePositive(int value)
    {
        NumericGuard.ThrowIfNegativeOrZero(value);
    }

    private static FixedScalar ClampNonNegative(FixedScalar value) => value < 0 ? 0 : value;

    private static FixedScalar ClampAtLeast(FixedScalar value, FixedScalar minimum) => value < minimum ? minimum : value;

    private static FixedScalar Clamp01(FixedScalar value)
    {
        if (value < 0)
        {
            return 0;
        }

        return value > 1 ? 1 : value;
    }
}
