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
