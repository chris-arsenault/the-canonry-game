namespace Engine.Kernel;

internal sealed partial class KernelDataSet
{
    public KernelDataSet(KernelEntityData entities, KernelTerrainData terrain, KernelCultureData cultures)
    {
        global::System.ArgumentNullException.ThrowIfNull(entities);
        global::System.ArgumentNullException.ThrowIfNull(terrain);
        global::System.ArgumentNullException.ThrowIfNull(cultures);
        Entities = entities;
        Terrain = terrain;
        Cultures = cultures;
    }

    public KernelEntityData Entities { get; }

    public KernelTerrainData Terrain { get; }

    public KernelCultureData Cultures { get; }
}
