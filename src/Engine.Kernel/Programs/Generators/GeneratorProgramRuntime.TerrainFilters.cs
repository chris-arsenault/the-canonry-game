namespace Engine.Kernel;

internal sealed partial class GeneratorProgramRuntime
{
    public void AddProducedPlacementPreferredTerrain(int producedSlot, int planeCode, int terrainId)
    {
        EnsureOpenProduced(producedSlot);
        ValidatePlaneCode(planeCode);
        AddTerrainFilterId(
            terrainId,
            _producedPlacementPreferredTerrainStarts,
            _producedPlacementPreferredTerrainCounts,
            PlacementTerrainSlot(producedSlot, planeCode));
    }

    public void AddProducedPlacementAllowedTerrain(int producedSlot, int planeCode, int terrainId)
    {
        EnsureOpenProduced(producedSlot);
        ValidatePlaneCode(planeCode);
        AddTerrainFilterId(
            terrainId,
            _producedPlacementAllowedTerrainStarts,
            _producedPlacementAllowedTerrainCounts,
            PlacementTerrainSlot(producedSlot, planeCode));
    }

    public void AddProducedPlacementAvoidedTerrain(int producedSlot, int planeCode, int terrainId)
    {
        EnsureOpenProduced(producedSlot);
        ValidatePlaneCode(planeCode);
        AddTerrainFilterId(
            terrainId,
            _producedPlacementAvoidedTerrainStarts,
            _producedPlacementAvoidedTerrainCounts,
            PlacementTerrainSlot(producedSlot, planeCode));
    }

    public void SetProducedPlacementFallbackTerrain(int producedSlot, int planeCode, int terrainId)
    {
        EnsureOpenProduced(producedSlot);
        ValidatePlaneCode(planeCode);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(terrainId);
        _producedPlacementFallbackTerrainIds[PlacementTerrainSlot(producedSlot, planeCode)] = terrainId;
    }

    /// <summary>Restricts FeatureAnchor placement to one feature kind (Any = -1).</summary>
    public void SetProducedFeatureKind(int producedSlot, int featureKind)
    {
        if (producedSlot < 0 || producedSlot >= _producedFeatureKinds.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(producedSlot));
        }

        _producedFeatureKinds[producedSlot] = featureKind;
    }

    public void SetProducedPlacementGeography(
        int producedSlot,
        int siteFlags,
        float warmthAffinity,
        float fireAffinity,
        float auroraAffinity,
        float pressureAffinity)
    {
        EnsureOpenProduced(producedSlot);
        if ((siteFlags & ~PhysicalHexFieldCodes.WordMask) != 0)
        {
            throw new global::System.InvalidOperationException();
        }

        Data.ProducedPlacementSiteFlags[producedSlot] = siteFlags;
        var baseIndex = producedSlot * GeneratorProgramData.PlacementBandAffinityCount;
        var affinities = Data.ProducedPlacementBandAffinities;
        affinities[baseIndex + GeneratorProgramData.PlacementWarmthAffinityOffset] = ClampAffinity(warmthAffinity);
        affinities[baseIndex + GeneratorProgramData.PlacementFireAffinityOffset] = ClampAffinity(fireAffinity);
        affinities[baseIndex + GeneratorProgramData.PlacementAuroraAffinityOffset] = ClampAffinity(auroraAffinity);
        affinities[baseIndex + GeneratorProgramData.PlacementPressureAffinityOffset] = ClampAffinity(pressureAffinity);
    }

    private static float ClampAffinity(float value)
    {
        if (value < -1f)
        {
            return -1f;
        }

        return value > 1f ? 1f : value;
    }

    public void AddProducedCoveragePreferredTerrain(int producedSlot, int terrainId)
    {
        EnsureOpenProduced(producedSlot);
        AddTerrainFilterId(
            terrainId,
            _producedCoveragePreferredTerrainStarts,
            _producedCoveragePreferredTerrainCounts,
            producedSlot);
    }

    public void AddProducedCoverageAllowedTerrain(int producedSlot, int terrainId)
    {
        EnsureOpenProduced(producedSlot);
        AddTerrainFilterId(
            terrainId,
            _producedCoverageAllowedTerrainStarts,
            _producedCoverageAllowedTerrainCounts,
            producedSlot);
    }

    public void AddEffectPreferredTerrain(int effectSlot, int terrainId)
    {
        EnsureOpenEffect(effectSlot);
        AddTerrainFilterId(
            terrainId,
            _effectPreferredTerrainStarts,
            _effectPreferredTerrainCounts,
            effectSlot);
    }

    public void AddEffectAllowedTerrain(int effectSlot, int terrainId)
    {
        EnsureOpenEffect(effectSlot);
        AddTerrainFilterId(
            terrainId,
            _effectAllowedTerrainStarts,
            _effectAllowedTerrainCounts,
            effectSlot);
    }

    private void InitializeProducedPlacementTerrainFilters(int producedSlot)
    {
        InitializeProducedPlanePlacementTerrainFilters(producedSlot, PlaneCodes.Physical);
        InitializeProducedPlanePlacementTerrainFilters(producedSlot, PlaneCodes.Semantic);
    }

    private void InitializeProducedPlanePlacementTerrainFilters(int producedSlot, int planeCode)
    {
        var slot = PlacementTerrainSlot(producedSlot, planeCode);
        _producedPlacementPreferredTerrainStarts[slot] = _terrainFilterCount + 1;
        _producedPlacementAllowedTerrainStarts[slot] = _terrainFilterCount + 1;
        _producedPlacementAvoidedTerrainStarts[slot] = _terrainFilterCount + 1;
    }
}
