namespace Engine.Kernel;

public sealed partial class KernelDataSet
{
    public KernelDataSet(KernelEntityData entities, KernelTerrainData terrain)
    {
        global::System.ArgumentNullException.ThrowIfNull(entities);
        global::System.ArgumentNullException.ThrowIfNull(terrain);
        Entities = entities;
        Terrain = terrain;
    }

    public KernelEntityData Entities { get; }

    public KernelTerrainData Terrain { get; }
}
