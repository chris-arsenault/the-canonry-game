namespace Engine.Kernel;

using Engine.Constants;

/// <summary>
/// Tuning for the growing map: which Ice-track ages drive thaw and re-freeze,
/// the terrains they lay, the physical extent the world may ever reach, and
/// the mass/budget bounds that keep growth earned and bounded. Configured
/// once at install from domain content.
/// </summary>
internal sealed class MapFrontierProgramData : IKernelProgramData
{
    public int ProgramId => KernelProgramCodes.MapFrontier;

    public int SchemaVersion => 1;

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

    /// <summary>Terrain laid on newly thawed ground.</summary>
    public int ThawTerrainId;

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

    // --- Semantic (idea-space) growth ---
    // Ideas have no ice, so the semantic plane grows continuously wherever a
    // culture's idea-mass presses its frontier - no age gate, no re-freeze.

    /// <summary>Terrain laid on newly opened semantic cells (0 = semantic growth disabled).</summary>
    public int SemanticOpenTerrainId;

    /// <summary>The semantic plane may never grow beyond this radius from origin.</summary>
    public int SemanticMaxRadius = 40;

    /// <summary>A semantic frontier cell must hold at least this raw idea-mass to open new ground.</summary>
    public float SemanticFrontierMassThreshold = 30f;
}
