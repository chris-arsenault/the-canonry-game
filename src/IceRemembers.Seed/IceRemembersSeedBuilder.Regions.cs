namespace IceRemembers.Seed;

using Engine.Domain.Spatial;

using IceRemembers;

using K = IceRemembers.IceRemembersKinds;

// The four named regions: their entities, their anchors, and the coverages
// that give them shape.
internal static partial class IceRemembersSeedBuilder
{
    private static Dictionary<string, MapCoverage> CreateRegionEntities(SeedAssembly seed)
    {
        var northlands = new MapCoverage("Haakkaa, Where The Ice Returns",
            CreateEntity(seed, K.Region, "face", "Haakkaa, Where The Ice Returns", "Haakkaa", C(IceRemembersCulture.TheIce)).SeedEntityId);
        var heartlands = new MapCoverage("Ha'rroothoor, Where The Ice Waits",
            CreateEntity(seed, K.Region, "reach", "Ha'rroothoor, Where The Ice Waits", "Ha'rroothoor", C(IceRemembersCulture.TheIce)).SeedEntityId);
        var coastlands = new MapCoverage("Thookarraa, The Ledge",
            CreateEntity(seed, K.Region, "shelf", "Thookarraa, The Ledge", "Thookarraa", C(IceRemembersCulture.TheIce)).SeedEntityId);
        var farwake = new MapCoverage("Undertow Thoomvlu, Where Blackwater Answers",
            CreateEntity(seed, K.Region, "sea", "Undertow Thoomvlu, Where Blackwater Answers", "Thoomvlu", C(IceRemembersCulture.TheIce)).SeedEntityId);

        return new Dictionary<string, MapCoverage>(StringComparer.Ordinal)
        {
            ["northlands"] = northlands,
            ["heartlands"] = heartlands,
            ["coastlands"] = coastlands,
            ["farwake"] = farwake,
        };
    }

    private static (
        HexCoord Northlands,
        HexCoord Heartlands,
        HexCoord Coastlands,
        HexCoord Farwake) PlaceRegionAnchors(
        SeedAssembly seed,
        Dictionary<string, MapCoverage> regions,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic)
    {
        var northlands = ChooseAnchor(
            seed,
            random,
            takenPhysical,
            K.Region,
            "face",
            C(IceRemembersCulture.TheIce),
            minimumDistance: 10,
            cell => RegionNorthlandsContextScore(cell) +
                SuperregionAffinity(seed.SubdivisionMap, NorthlandsSuperregionId, cell.Coord, 80f));
        var heartlands = ChooseAnchor(
            seed,
            random,
            takenPhysical,
            K.Region,
            "reach",
            C(IceRemembersCulture.TheIce),
            minimumDistance: 10,
            cell => RegionHeartlandsContextScore(cell) +
                SuperregionAffinity(seed.SubdivisionMap, HeartlandsSuperregionId, cell.Coord, 80f));
        var coastlands = ChooseAnchor(
            seed,
            random,
            takenPhysical,
            K.Region,
            "shelf",
            C(IceRemembersCulture.TheIce),
            minimumDistance: 10,
            cell => RegionCoastlandsContextScore(cell) +
                SuperregionAffinity(seed.SubdivisionMap, CoastlandsSuperregionId, cell.Coord, 80f));
        var farwake = ChooseAnchor(
            seed,
            random,
            takenPhysical,
            K.Region,
            "sea",
            C(IceRemembersCulture.TheIce),
            minimumDistance: 14,
            cell => RegionCoastlandsContextScore(cell) +
                SuperregionAffinity(seed.SubdivisionMap, FarwakeSuperregionId, cell.Coord, 100f));

        SetPhysical(seed.Entity(regions["northlands"].OwnerEntityId), northlands, takenPhysical);
        SetPhysical(seed.Entity(regions["heartlands"].OwnerEntityId), heartlands, takenPhysical);
        SetPhysical(seed.Entity(regions["coastlands"].OwnerEntityId), coastlands, takenPhysical);
        SetPhysical(seed.Entity(regions["farwake"].OwnerEntityId), farwake, takenPhysical);

        SetSemanticNatural(seed, seed.Entity(regions["northlands"].OwnerEntityId), random, takenSemantic, minimumDistance: 8);
        SetSemanticNatural(seed, seed.Entity(regions["heartlands"].OwnerEntityId), random, takenSemantic, minimumDistance: 8);
        SetSemanticNatural(seed, seed.Entity(regions["coastlands"].OwnerEntityId), random, takenSemantic, minimumDistance: 8);
        SetSemanticNatural(seed, seed.Entity(regions["farwake"].OwnerEntityId), random, takenSemantic, minimumDistance: 8);

        return (northlands, heartlands, coastlands, farwake);
    }

    private static void CreateRegionCoverage(
        SeedAssembly seed,
        HexGrid grid,
        MapCoverageMap coverageMap,
        Dictionary<string, MapCoverage> regions,
        (
            HexCoord Northlands,
            HexCoord Heartlands,
            HexCoord Coastlands,
            HexCoord Farwake) anchors,
        HashSet<HexCoord> takenPhysical)
    {
        regions["northlands"] = CreateSeedCoverage(
            grid,
            coverageMap,
            seed,
            regions["northlands"],
            anchors.Northlands,
            desiredCellCount: 96,
            minimumCellCount: 72,
            preferredTerrains: TerrainSet(IceRemembersTerrain.Ice, IceRemembersTerrain.Tundra, IceRemembersTerrain.Shelf),
            takenPhysical);
        regions["heartlands"] = CreateSeedCoverage(
            grid,
            coverageMap,
            seed,
            regions["heartlands"],
            anchors.Heartlands,
            desiredCellCount: 144,
            minimumCellCount: 110,
            preferredTerrains: TerrainSet(IceRemembersTerrain.Tundra, IceRemembersTerrain.HotSprings, IceRemembersTerrain.Wastes),
            takenPhysical);
        regions["coastlands"] = CreateSeedCoverage(
            grid,
            coverageMap,
            seed,
            regions["coastlands"],
            anchors.Coastlands,
            desiredCellCount: 88,
            minimumCellCount: 64,
            preferredTerrains: TerrainSet(IceRemembersTerrain.Shelf, IceRemembersTerrain.Reef, IceRemembersTerrain.Ocean, IceRemembersTerrain.Tundra),
            takenPhysical);
        regions["farwake"] = CreateSeedCoverage(
            grid,
            coverageMap,
            seed,
            regions["farwake"],
            anchors.Farwake,
            desiredCellCount: 112,
            minimumCellCount: 72,
            preferredTerrains: TerrainSet(IceRemembersTerrain.Ocean, IceRemembersTerrain.Reef, IceRemembersTerrain.Shelf),
            takenPhysical);
    }
}
