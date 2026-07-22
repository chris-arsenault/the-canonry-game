namespace Engine.Kernel;

internal sealed partial class KernelDataSet
{
    public KernelDataSet(
        KernelEntityData entities,
        KernelTerrainData terrain,
        KernelCultureData cultures,
        KernelPhysicalGeographyData physicalGeography,
        KernelPlacementTasteData placementTaste,
        KernelGeologicalFeatureData geologicalFeatures)
    {
        global::System.ArgumentNullException.ThrowIfNull(entities);
        global::System.ArgumentNullException.ThrowIfNull(terrain);
        global::System.ArgumentNullException.ThrowIfNull(cultures);
        global::System.ArgumentNullException.ThrowIfNull(physicalGeography);
        global::System.ArgumentNullException.ThrowIfNull(placementTaste);
        global::System.ArgumentNullException.ThrowIfNull(geologicalFeatures);
        Entities = entities;
        Terrain = terrain;
        Cultures = cultures;
        PhysicalGeography = physicalGeography;
        PlacementTaste = placementTaste;
        GeologicalFeatures = geologicalFeatures;
    }

    public KernelEntityData Entities { get; }

    public KernelTerrainData Terrain { get; }

    public KernelCultureData Cultures { get; }

    public KernelPhysicalGeographyData PhysicalGeography { get; }

    public KernelPlacementTasteData PlacementTaste { get; }

    public KernelGeologicalFeatureData GeologicalFeatures { get; }
}
