namespace Engine.Kernel;

internal sealed partial class KernelTerrainData
{
    private readonly bool[] _registered;
    private readonly int[] _boundaryGroupIds;
    private readonly float[] _movementCosts;
    private readonly float[] _influenceMultipliers;
    private readonly float[] _claimCostMultipliers;
    private readonly float[] _claimChanceMultipliers;
    private readonly float[] _roadBuildMultipliers;
    private readonly float[] _roadSpeedMultipliers;
    private readonly PrimitiveIntTable _kindProfileFlags;
    private readonly PrimitiveFloatTable _kindMovementCosts;
    private readonly PrimitiveFloatTable _kindInfluenceMultipliers;
    private readonly PrimitiveFloatTable _kindClaimCostMultipliers;
    private readonly PrimitiveFloatTable _kindClaimChanceMultipliers;

    public KernelTerrainData(int terrainIdCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(terrainIdCapacity);
        _registered = new bool[terrainIdCapacity + 1];
        _boundaryGroupIds = new int[terrainIdCapacity + 1];
        _movementCosts = new float[terrainIdCapacity + 1];
        _influenceMultipliers = new float[terrainIdCapacity + 1];
        _claimCostMultipliers = new float[terrainIdCapacity + 1];
        _claimChanceMultipliers = new float[terrainIdCapacity + 1];
        _roadBuildMultipliers = new float[terrainIdCapacity + 1];
        _roadSpeedMultipliers = new float[terrainIdCapacity + 1];
        var overrideCapacity = global::System.Math.Max(16, terrainIdCapacity * 8);
        _kindProfileFlags = new PrimitiveIntTable(overrideCapacity);
        _kindMovementCosts = new PrimitiveFloatTable(overrideCapacity);
        _kindInfluenceMultipliers = new PrimitiveFloatTable(overrideCapacity);
        _kindClaimCostMultipliers = new PrimitiveFloatTable(overrideCapacity);
        _kindClaimChanceMultipliers = new PrimitiveFloatTable(overrideCapacity);
    }

    public void RegisterTerrain(int terrainId, int boundaryGroupId) =>
        RegisterTerrain(
            terrainId,
            boundaryGroupId,
            movementCost: 1f,
            influenceMultiplier: 1f,
            claimCostMultiplier: 1f,
            claimChanceMultiplier: 1f,
            roadBuildMultiplier: 1f,
            roadSpeedMultiplier: 1f);

    public void RegisterTerrain(
        int terrainId,
        int boundaryGroupId,
        float movementCost,
        float influenceMultiplier,
        float claimCostMultiplier,
        float claimChanceMultiplier,
        float roadBuildMultiplier,
        float roadSpeedMultiplier)
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
        _roadSpeedMultipliers[terrainId] = ClampAtLeast(roadSpeedMultiplier, 1f);
    }

    public void RegisterTerrainKindProfile(
        int terrainId,
        int kindId,
        int fieldFlags,
        float movementCost,
        float influenceMultiplier,
        float claimCostMultiplier,
        float claimChanceMultiplier)
    {
        EnsureTerrain(terrainId);
        ValidatePositive(kindId);
        if (fieldFlags == 0)
        {
            return;
        }

        _kindProfileFlags.Set(terrainId, kindId, fieldFlags);
        if ((fieldFlags & TerrainKindProfileFieldCodes.MovementCost) != 0)
        {
            _kindMovementCosts.Set(terrainId, kindId, ClampNonNegative(movementCost));
        }

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

    public int BoundaryGroupId(int terrainId)
    {
        EnsureTerrain(terrainId);
        return _boundaryGroupIds[terrainId];
    }

    public bool CanEnter(int terrainId, int kindId) => TravelCost(terrainId, kindId) > 0f;

    public float TravelCost(int terrainId, int kindId)
    {
        EnsureTerrain(terrainId);
        return KindProfileValue(
            terrainId,
            kindId,
            TerrainKindProfileFieldCodes.MovementCost,
            _kindMovementCosts,
            _movementCosts[terrainId]);
    }

    public float RoadAdjustedTravelCost(int terrainId, int kindId, float roadQuality)
    {
        var travelCost = TravelCost(terrainId, kindId);
        if (travelCost <= 0f)
        {
            return float.PositiveInfinity;
        }

        var normalizedRoadQuality = Clamp01(roadQuality / 100f);
        var roadSpeed = 1f + ((RoadSpeedMultiplier(terrainId) - 1f) * normalizedRoadQuality);
        return travelCost / roadSpeed;
    }

    public float InfluenceMultiplier(int terrainId, int kindId)
    {
        EnsureTerrain(terrainId);
        return KindProfileValue(
            terrainId,
            kindId,
            TerrainKindProfileFieldCodes.InfluenceMultiplier,
            _kindInfluenceMultipliers,
            _influenceMultipliers[terrainId]);
    }

    public float ClaimCostMultiplier(int terrainId, int kindId)
    {
        EnsureTerrain(terrainId);
        return KindProfileValue(
            terrainId,
            kindId,
            TerrainKindProfileFieldCodes.ClaimCostMultiplier,
            _kindClaimCostMultipliers,
            _claimCostMultipliers[terrainId]);
    }

    public float ClaimChanceMultiplier(int terrainId, int kindId)
    {
        EnsureTerrain(terrainId);
        return KindProfileValue(
            terrainId,
            kindId,
            TerrainKindProfileFieldCodes.ClaimChanceMultiplier,
            _kindClaimChanceMultipliers,
            _claimChanceMultipliers[terrainId]);
    }

    public float RoadBuildMultiplier(int terrainId)
    {
        EnsureTerrain(terrainId);
        return _roadBuildMultipliers[terrainId];
    }

    public float RoadSpeedMultiplier(int terrainId)
    {
        EnsureTerrain(terrainId);
        return _roadSpeedMultipliers[terrainId];
    }

    private float KindProfileValue(
        int terrainId,
        int kindId,
        int fieldFlag,
        PrimitiveFloatTable values,
        float fallback)
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
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
    }

    private static float ClampNonNegative(float value) => value < 0f ? 0f : value;

    private static float ClampAtLeast(float value, float minimum) => value < minimum ? minimum : value;

    private static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }
}
