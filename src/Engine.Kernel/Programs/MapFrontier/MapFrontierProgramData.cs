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

    /// <summary>Ice-track age id whose opening drives thaw (0 = thaw disabled).</summary>
    public int ThawAgeId;

    /// <summary>Ice-track age id whose opening drives re-freeze (0 = re-freeze disabled).</summary>
    public int RefreezeAgeId;

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
}
