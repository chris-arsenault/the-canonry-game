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
    public MapFrontierProgramData(int mapGenerationRadiusCapacity = 0, int kindIdCapacity = 0)
    {
        NumericGuard.ThrowIfNegative(mapGenerationRadiusCapacity);
        NumericGuard.ThrowIfNegative(kindIdCapacity);
        InitializeSemanticBlueprint(mapGenerationRadiusCapacity);
        _kindRevealRadius = new int[kindIdCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.MapFrontier;

    public int SchemaVersion => 4;

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

    // --- Reveal: fog of war retreats at entity speed, one-way ---

    /// <summary>Reveal radius for kinds without an authored override.</summary>
    public int BaseRevealRadius = 1;

    private readonly int[] _kindRevealRadius;

    public int RevealRadiusOf(int kindId) =>
        kindId > 0 && kindId < _kindRevealRadius.Length && _kindRevealRadius[kindId] > 0
            ? _kindRevealRadius[kindId]
            : BaseRevealRadius;

    public void RegisterKindRevealRadius(int kindId, int radius)
    {
        NumericGuard.ThrowIfNegativeOrZero(radius);
        if (kindId <= 0 || kindId >= _kindRevealRadius.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(kindId));
        }

        _kindRevealRadius[kindId] = radius;
    }

    // --- Ice: cold pressure vs mortal resistance, breathing with ages ---

    /// <summary>Byte-scale temperature under which ground can hold cold pressure.</summary>
    public int FrostTemperatureCeiling = 64;

    /// <summary>Pressure added per byte of resident ice thickness.</summary>
    public FixedScalar IceThicknessPressureWeight = FixedScalar.FromUnits(250000L);

    /// <summary>Pressure relieved per byte of geothermal heat.</summary>
    public FixedScalar GeothermalRefugeWeight = FixedScalar.FromUnits(500000L);

    /// <summary>Pressure scalar while a freeze age is open (advance pass).</summary>
    public FixedScalar FreezeAgePressureScalar = 1;

    /// <summary>Pressure scalar while a thaw age is open (retreat pass).</summary>
    public FixedScalar ThawAgePressureScalar = FixedScalar.FromUnits(500000L);

    /// <summary>Resistance every known cell holds against the ice.</summary>
    public FixedScalar BaseIceResistance = 8;

    /// <summary>Resistance per point of strongest-culture raw mass.</summary>
    public FixedScalar CultureMassResistanceScale = 4;

    /// <summary>Resistance per road crossing the cell.</summary>
    public FixedScalar RoadIceResistance = 40;

    /// <summary>Maximum cells the ice covers per advance run.</summary>
    public int IceAdvanceBudgetPerRun = 24;

    /// <summary>Maximum covered cells melted per retreat run.</summary>
    public int IceRetreatBudgetPerRun = 24;

    // --- Sea ice: the frozen sea breathes with the Ice-track ages ---
    // Freeze ages extend the cover from coasts and existing ice over
    // eligible water; thaw ages melt it back from the fringe. The cover
    // closes sea lanes to swimmers and opens slow walking routes, so the
    // map's connectivity ebbs with the fiction.

    /// <summary>Cover terrain frozen sea wears; 0 disables sea-ice modulation.</summary>
    public int SeaIceCoverTerrainId;

    /// <summary>Maximum eligible water cells frozen per re-freeze run.</summary>
    public int SeaIceFreezeBudgetPerRun = 12;

    /// <summary>Maximum covered cells melted per thaw run.</summary>
    public int SeaIceMeltBudgetPerRun = 12;

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
        NumericGuard.ThrowIfNegativeOrZero(terrainId);
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
    public FixedScalar SemanticFrontierMassThreshold = 30;

    /// <summary>Maximum semantic cells opened per run.</summary>
    public int SemanticBudgetPerRun = 24;

    public int GenerationRadiusCapacity => _semanticBlueprintRadiusCapacity;
}
