namespace Engine.Kernel;

using Engine.Constants;

/// <summary>
/// Tuning for the growing map: which Ice-track ages drive thaw and re-freeze,
/// the terrains they lay, the physical extent the world may ever reach, and
/// the mass/budget bounds that keep growth earned and bounded. Configured
/// once at install from domain content.
/// </summary>
internal sealed partial class MapFrontierProgramData : IKernelProgramData
{
    public MapFrontierProgramData(int mapGenerationRadiusCapacity = 0)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(mapGenerationRadiusCapacity);
        InitializeSemanticBlueprint(mapGenerationRadiusCapacity);
    }

    public int ProgramId => KernelProgramCodes.MapFrontier;

    public int SchemaVersion => 3;

    internal const int MaxDriverAges = 4;

    /// <summary>World-track age ids whose holding drives thaw (empty = disabled).</summary>
    public readonly int[] ThawAgeIds = new int[MaxDriverAges];

    /// <summary>World-track age ids whose holding drives re-freeze (empty = disabled).</summary>
    public readonly int[] RefreezeAgeIds = new int[MaxDriverAges];

    public bool IsThawAge(int ageId) => ageId != 0 && ContainsAge(ThawAgeIds, ageId);

    public bool IsRefreezeAge(int ageId) => ageId != 0 && ContainsAge(RefreezeAgeIds, ageId);

    private static bool ContainsAge(int[] ages, int ageId)
    {
        for (var i = 0; i < ages.Length; i++)
        {
            if (ages[i] == ageId)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Terrain laid on reclaimed / un-thawed ground (impassable deep ice).</summary>
    public int FrozenTerrainId;

    /// <summary>The physical world may never grow beyond this radius from origin.</summary>
    public int MaxRadius = 40;

    /// <summary>A frontier cell must hold at least this raw culture mass to open new ground.</summary>
    public float FrontierMassThreshold = 60f;

    /// <summary>A cell with total raw mass at or below this may be reclaimed by the ice.</summary>
    public float ReclaimMassCeiling = 1f;

    /// <summary>Maximum cells thawed per pulse.</summary>
    public int ThawBudgetPerPulse = 24;

    /// <summary>Maximum cells reclaimed per pulse.</summary>
    public int RefreezeBudgetPerPulse = 24;

    // --- Sea ice: the frozen sea breathes with the Ice-track ages ---
    // Freeze ages extend the cover from coasts and existing ice over
    // eligible water; thaw ages melt it back from the fringe. The cover
    // closes sea lanes to swimmers and opens slow walking routes, so the
    // map's connectivity ebbs with the fiction.

    /// <summary>Cover terrain frozen sea wears; 0 disables sea-ice modulation.</summary>
    public int SeaIceCoverTerrainId;

    /// <summary>Maximum eligible water cells frozen per re-freeze pulse.</summary>
    public int SeaIceFreezeBudgetPerPulse = 12;

    /// <summary>Maximum covered cells melted per thaw pulse.</summary>
    public int SeaIceMeltBudgetPerPulse = 12;

    internal const int MaxSeaIceEligibleTerrains = 8;

    /// <summary>Base terrains whose cells may carry sea-ice cover (water family).</summary>
    public readonly int[] SeaIceEligibleTerrainIds = new int[MaxSeaIceEligibleTerrains];

    public int SeaIceEligibleTerrainCount;

    public bool SeaIceEligibleTerrain(int terrainId)
    {
        for (var i = 0; i < SeaIceEligibleTerrainCount; i++)
        {
            if (SeaIceEligibleTerrainIds[i] == terrainId)
            {
                return true;
            }
        }

        return false;
    }

    public void AddSeaIceEligibleTerrain(int terrainId)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(terrainId);
        if (SeaIceEligibleTerrainCount >= MaxSeaIceEligibleTerrains ||
            SeaIceEligibleTerrain(terrainId))
        {
            throw new global::System.InvalidOperationException();
        }

        SeaIceEligibleTerrainIds[SeaIceEligibleTerrainCount++] = terrainId;
    }

    // --- Semantic (idea-space) growth ---
    // Ideas have no ice, so the semantic plane grows continuously wherever a
    // culture's idea-mass presses its frontier - no age gate, no re-freeze.

    /// <summary>Whether the latent semantic terrain field may materialize.</summary>
    public bool SemanticGrowthEnabled;

    /// <summary>The semantic plane may never grow beyond this radius from origin.</summary>
    public int SemanticMaxRadius = 40;

    /// <summary>A semantic frontier cell must hold at least this raw idea-mass to open new ground.</summary>
    public float SemanticFrontierMassThreshold = 30f;

    /// <summary>Maximum semantic cells opened per pulse.</summary>
    public int SemanticBudgetPerPulse = 24;

    public int GenerationRadiusCapacity => _semanticBlueprintRadiusCapacity;
}
