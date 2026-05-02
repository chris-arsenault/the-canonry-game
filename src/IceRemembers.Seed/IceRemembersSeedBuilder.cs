namespace IceRemembers.Seed;

using Engine.Domain.Startup;
using Engine.Graph;
using Engine.Kinds;
using Engine.WorldGen;

using IceRemembers;

using K = IceRemembers.IceRemembersKinds;

/// <summary>
/// Builds a seeded procedural map with named starter regions and anchors.
/// The same seed produces the same world; different seeds should produce
/// meaningfully different terrain and starting layouts.
/// </summary>
internal static partial class IceRemembersSeedBuilder
{
    private const int MaxAcceptedSeedAttempts = 64;
    private const int StartupMapRadius = 40;

    private static readonly IceRemembersCulture[] CoastalCultures =
    [
        IceRemembersCulture.Nightshelf,
        IceRemembersCulture.WakeSingers,
    ];

    private static readonly IceRemembersCulture[] InteriorCultures =
    [
        IceRemembersCulture.AuroraStack,
        IceRemembersCulture.Nightshelf,
        IceRemembersCulture.WakeSingers,
    ];

    private static readonly IceRemembersCulture[] NorthernPenguinCultures =
    [
        IceRemembersCulture.AuroraStack,
        IceRemembersCulture.WakeSingers,
    ];

    private static readonly IceRemembersCulture[] SouthernPenguinCultures =
    [
        IceRemembersCulture.Nightshelf,
        IceRemembersCulture.WakeSingers,
    ];

    private static readonly IceRemembersCulture[] FarPenguinCultures =
    [
        IceRemembersCulture.WakeSingers,
        IceRemembersCulture.AuroraStack,
    ];

    private static readonly string[] CoastalSettlementSubtypes = ["harbor", "reefhold"];
    private static readonly string[] InteriorSettlementSubtypes = ["rookery", "colony", "holdfast", "sanctuary"];
    private static readonly string[] InteriorCoverageKeys = ["northlands", "heartlands", "whiteempty"];
    private static readonly string[] FarSettlementNames = ["Stillwake", "Deepcall", "Hushwater", "Underchoir"];
    private static readonly string[] CoastalSettlementNames = ["Brinegate", "Lowwake", "Pearlmark", "Greyreef", "Tidehollow"];
    private static readonly string[] InteriorSettlementNames = ["Palehollow", "Snowcairn", "Embercrease", "Whitebarrow", "Underbell"];
    private static readonly string[] PenguinGivenNames = ["Softstep", "Glass-Eye", "Ashbill", "Keelback", "Mourncrest", "Little Thaw"];
    private static readonly string[] PenguinByNames = ["Underice", "Farcall", "Whitewake", "Coalbeak", "Noonless"];
    private static readonly string[] PenguinSubtypes = ["warden", "broker", "witness", "exile"];
    private static readonly string[] SupportingArtifactSubtypes = ["relic", "instrument", "tome", "seal"];
    private static readonly string[] SupportingArtifactNames = ["Low Bell", "Black Ledger", "Meltglass Needle", "Salted Key", "Moon-Under-Ice"];

    private readonly record struct SupportingSettlementPlan(
        bool Coastal,
        IceRemembersCulture Culture,
        string Subtype,
        string CoverageKey,
        SeedEntity ControlledBy,
        string[] NamePool);

    private static string C(IceRemembersCulture culture) => culture.ToDomainName();

    private static SeedEntity CreateEntity(
        SeedAssembly seed,
        EntityKind kind,
        string subtype,
        string name,
        string culture,
        (int Q, int R)? coordinates = null) =>
        seed.CreateEntity(
            kind,
            subtype,
            name,
            culture,
            coordinates);

    private static void CreateRelationship(
        SeedAssembly seed,
        string kind,
        RelationshipPolarity polarity,
        int sourceEntityId,
        int targetEntityId,
        float strength) =>
        seed.CreateRelationship(
            kind,
            polarity,
            sourceEntityId,
            targetEntityId,
            strength);

    /// <summary>
    /// Build the default startup seed from an accepted procedural seed. A fixed
    /// input seed always resolves to the same accepted terrain and seed roster.
    /// </summary>
    public static DomainStartupSeed BuildDefault(int? seed = null)
    {
        var baseSeed = seed ?? Random.Shared.Next();
        return BuildAcceptedStartupSeed(baseSeed).StartupSeed;
    }

    /// <summary>
    /// Build the Ice Remembers startup seed in the domain-stable shape consumed by Engine.Host.
    /// This is a migration boundary: the source remains the existing procedural seed builder,
    /// while the output avoids kernel runtime IDs and legacy host coupling.
    /// </summary>
    public static DomainStartupSeed BuildStartupSeed(int? seed = null)
    {
        var baseSeed = seed ?? Random.Shared.Next();
        return BuildAcceptedStartupSeed(baseSeed).StartupSeed;
    }

    public static DomainStartupSeed BuildStartupSeed(WorldGenerationSeed seed)
    {
        var assembly = BuildEmptyAssembly(seed);
        Populate(assembly, seed);
        return assembly.ToStartupSeed();
    }

    public static IceRemembersStartupSeedResult BuildAcceptedStartupSeed(int baseSeed)
    {
        return BuildAcceptedStartupSeed(
            baseSeed,
            BuildStartupSeed,
            IceRemembersSeedQualityValidator.Validate,
            MaxAcceptedSeedAttempts);
    }

    public static WorldGenerationSeed SelectAcceptedSeed(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        return SelectAcceptedSeed(random.Next());
    }

    public static WorldGenerationSeed SelectAcceptedSeed(int baseSeed)
        => BuildAcceptedStartupSeed(baseSeed).Seed;

    internal static IceRemembersStartupSeedResult BuildAcceptedStartupSeed(
        int baseSeed,
        Func<WorldGenerationSeed, DomainStartupSeed> buildSeed,
        Func<DomainStartupSeed, SeedWorldQualityReport> validateSeed,
        int maxAttempts)
    {
        ArgumentNullException.ThrowIfNull(buildSeed);
        ArgumentNullException.ThrowIfNull(validateSeed);

        IReadOnlyList<string> lastIssues = [];
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var seed = WorldGenerationSeed.FromBaseSeed(baseSeed, attempt);
            try
            {
                var startupSeed = buildSeed(seed);
                var report = validateSeed(startupSeed);
                if (report.IsValid)
                {
                    return new IceRemembersStartupSeedResult(seed, startupSeed);
                }

                lastIssues = report.Issues;
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                lastIssues =
                [
                    $"{ex.GetType().Name}: {ex.Message}",
                ];
            }
        }

        throw new InvalidOperationException(
            $"Could not build an accepted procedural seed for base seed {baseSeed}. " +
            $"Last issues: {string.Join("; ", lastIssues)}");
    }

    private static SeedAssembly BuildEmptyAssembly(WorldGenerationSeed seed)
    {
        var grid = new HexGrid(
            StartupMapRadius,
            IceRemembersTerrainCatalog.CreateWorldGenCatalog(),
            IceRemembersTerrain.Wastes.ToDomainName());
        var coverageMap = new MapCoverageMap();
        var subdivisionMap = CreateSubdivisions(grid, seed.TerrainSeed);
        PaintTerrain(grid, new Random(seed.TerrainSeed), subdivisionMap);
        return new SeedAssembly(grid, coverageMap, subdivisionMap, CreateSubdivisionDefinitions());
    }

    private static void Populate(SeedAssembly assembly, WorldGenerationSeed seed)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var regions = CreateRegionEntities(assembly);
        CreateEntities(assembly, assembly.Grid, assembly.CoverageMap, regions, new Random(seed.EntitySeed));
    }

    private static Dictionary<string, MapCoverage> CreateRegionEntities(SeedAssembly seed)
    {
        var northlands = new MapCoverage("Northlands",
            CreateEntity(seed, K.Region, "face", "Northlands", "none").SeedEntityId);
        var heartlands = new MapCoverage("Heartlands",
            CreateEntity(seed, K.Region, "reach", "Heartlands", "none").SeedEntityId);
        var coastlands = new MapCoverage("Coastlands",
            CreateEntity(seed, K.Region, "shelf", "Coastlands", "none").SeedEntityId);
        var farwake = new MapCoverage("Farwake",
            CreateEntity(seed, K.Region, "sea", "Farwake", "none").SeedEntityId);
        var blackwater = new MapCoverage("Blackwater",
            CreateEntity(seed, K.Region, "depths", "Blackwater", "none").SeedEntityId);
        var whiteempty = new MapCoverage("White Empty",
            CreateEntity(seed, K.Region, "face", "White Empty", "none").SeedEntityId);

        return new Dictionary<string, MapCoverage>
(StringComparer.Ordinal)
        {
            ["northlands"] = northlands,
            ["heartlands"] = heartlands,
            ["coastlands"] = coastlands,
            ["farwake"] = farwake,
            ["blackwater"] = blackwater,
            ["whiteempty"] = whiteempty,
        };
    }

    private static void CreateEntities(
        SeedAssembly seed,
        HexGrid grid,
        MapCoverageMap coverageMap,
        Dictionary<string, MapCoverage> regions,
        Random random)
    {
        var takenPhysical = new HashSet<HexCoord>();
        var takenSemantic = new HashSet<SemanticHexCoord>();
        var regionAnchors = PlaceRegionAnchors(seed, grid, regions, random, takenPhysical, takenSemantic);
        CreateRegionCoverage(seed, grid, coverageMap, regions, regionAnchors);

        var settlementCoords = ChooseCoreSettlementCoords(grid, coverageMap, regions, random, takenPhysical);
        var coreSettlements = CreateCoreSettlements(seed, settlementCoords, takenSemantic);
        var allSettlements = coreSettlements.All;

        var factions = CreateCoreFactions(seed, grid, takenPhysical, takenSemantic, settlementCoords);
        CreateCoreControlRelationships(seed, coreSettlements, factions);

        CreateSupportingSettlements(
            seed,
            grid,
            coverageMap,
            regions,
            random,
            takenPhysical,
            takenSemantic,
            allSettlements,
            factions.Northern,
            factions.Southern,
            factions.Farwake);

        CreatePenguins(seed, grid, random, takenPhysical, takenSemantic, allSettlements, factions);
        CreateStormcleaver(seed, grid, random, takenPhysical, takenSemantic, allSettlements, coreSettlements.Frosthaven);
    }

    private static (
        HexCoord Northlands,
        HexCoord Heartlands,
        HexCoord Coastlands,
        HexCoord Farwake,
        HexCoord Blackwater,
        HexCoord WhiteEmpty) PlaceRegionAnchors(
        SeedAssembly seed,
        HexGrid grid,
        Dictionary<string, MapCoverage> regions,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic)
    {
        var northlands = ChooseAnchor(
            grid,
            random,
            takenPhysical,
            minimumDistance: 10,
            cell => RegionNorthlandsScore(grid, cell) +
                SuperregionAffinity(seed.SubdivisionMap, NorthlandsSuperregionId, cell.Coord, 80f));
        var heartlands = ChooseAnchor(
            grid,
            random,
            takenPhysical,
            minimumDistance: 10,
            cell => RegionHeartlandsScore(grid, cell) +
                SuperregionAffinity(seed.SubdivisionMap, HeartlandsSuperregionId, cell.Coord, 80f));
        var coastlands = ChooseAnchor(
            grid,
            random,
            takenPhysical,
            minimumDistance: 10,
            cell => RegionCoastlandsScore(grid, cell) +
                SuperregionAffinity(seed.SubdivisionMap, CoastlandsSuperregionId, cell.Coord, 80f));
        var farwake = ChooseAnchor(
            grid,
            random,
            takenPhysical,
            minimumDistance: 14,
            cell => RegionCoastlandsScore(grid, cell) +
                SuperregionAffinity(seed.SubdivisionMap, FarwakeSuperregionId, cell.Coord, 100f));
        var blackwater = ChooseAnchor(
            grid,
            random,
            takenPhysical,
            minimumDistance: 14,
            cell => RegionBlackwaterScore(grid, cell) +
                SuperregionAffinity(seed.SubdivisionMap, BlackwaterSuperregionId, cell.Coord, 100f));
        var whiteempty = ChooseAnchor(
            grid,
            random,
            takenPhysical,
            minimumDistance: 14,
            cell => RegionWhiteEmptyScore(grid, cell) +
                SuperregionAffinity(seed.SubdivisionMap, WhiteEmptySuperregionId, cell.Coord, 100f));

        SetPhysical(seed.Entity(regions["northlands"].OwnerEntityId), northlands, takenPhysical);
        SetPhysical(seed.Entity(regions["heartlands"].OwnerEntityId), heartlands, takenPhysical);
        SetPhysical(seed.Entity(regions["coastlands"].OwnerEntityId), coastlands, takenPhysical);
        SetPhysical(seed.Entity(regions["farwake"].OwnerEntityId), farwake, takenPhysical);
        SetPhysical(seed.Entity(regions["blackwater"].OwnerEntityId), blackwater, takenPhysical);
        SetPhysical(seed.Entity(regions["whiteempty"].OwnerEntityId), whiteempty, takenPhysical);

        SetSemanticProjected(seed.Entity(regions["northlands"].OwnerEntityId), northlands, takenSemantic, new SemanticHexCoord(-2, 2));
        SetSemanticProjected(seed.Entity(regions["heartlands"].OwnerEntityId), heartlands, takenSemantic, new SemanticHexCoord(0, 0));
        SetSemanticProjected(seed.Entity(regions["coastlands"].OwnerEntityId), coastlands, takenSemantic, new SemanticHexCoord(2, -2));
        SetSemanticProjected(seed.Entity(regions["farwake"].OwnerEntityId), farwake, takenSemantic, new SemanticHexCoord(6, -6));
        SetSemanticProjected(seed.Entity(regions["blackwater"].OwnerEntityId), blackwater, takenSemantic, new SemanticHexCoord(7, -4));
        SetSemanticProjected(seed.Entity(regions["whiteempty"].OwnerEntityId), whiteempty, takenSemantic, new SemanticHexCoord(-7, 5));

        return (northlands, heartlands, coastlands, farwake, blackwater, whiteempty);
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
            HexCoord Farwake,
            HexCoord Blackwater,
            HexCoord WhiteEmpty) anchors)
    {
        regions["northlands"] = CreateSeedCoverage(
            grid,
            coverageMap,
            seed,
            regions["northlands"],
            anchors.Northlands,
            desiredCellCount: 96,
            minimumCellCount: 72,
            preferredTerrains: TerrainSet(IceRemembersTerrain.Ice, IceRemembersTerrain.Tundra, IceRemembersTerrain.Shelf));
        regions["heartlands"] = CreateSeedCoverage(
            grid,
            coverageMap,
            seed,
            regions["heartlands"],
            anchors.Heartlands,
            desiredCellCount: 144,
            minimumCellCount: 110,
            preferredTerrains: TerrainSet(IceRemembersTerrain.Tundra, IceRemembersTerrain.HotSprings, IceRemembersTerrain.Wastes));
        regions["coastlands"] = CreateSeedCoverage(
            grid,
            coverageMap,
            seed,
            regions["coastlands"],
            anchors.Coastlands,
            desiredCellCount: 88,
            minimumCellCount: 64,
            preferredTerrains: TerrainSet(IceRemembersTerrain.Shelf, IceRemembersTerrain.Reef, IceRemembersTerrain.Ocean, IceRemembersTerrain.Tundra));
        regions["farwake"] = CreateSeedCoverage(
            grid,
            coverageMap,
            seed,
            regions["farwake"],
            anchors.Farwake,
            desiredCellCount: 112,
            minimumCellCount: 72,
            preferredTerrains: TerrainSet(IceRemembersTerrain.Ocean, IceRemembersTerrain.Reef, IceRemembersTerrain.Shelf));
        regions["blackwater"] = CreateSeedCoverage(
            grid,
            coverageMap,
            seed,
            regions["blackwater"],
            anchors.Blackwater,
            desiredCellCount: 96,
            minimumCellCount: 64,
            preferredTerrains: TerrainSet(IceRemembersTerrain.Ocean, IceRemembersTerrain.Reef, IceRemembersTerrain.Shelf));
        regions["whiteempty"] = CreateSeedCoverage(
            grid,
            coverageMap,
            seed,
            regions["whiteempty"],
            anchors.WhiteEmpty,
            desiredCellCount: 144,
            minimumCellCount: 96,
            preferredTerrains: TerrainSet(IceRemembersTerrain.Ice, IceRemembersTerrain.Wastes, IceRemembersTerrain.Crags, IceRemembersTerrain.Tundra));
    }

    private static (
        HexCoord Frosthaven,
        HexCoord Ironhold,
        HexCoord Driftwatch,
        HexCoord Wavecrest,
        HexCoord Midpoint,
        HexCoord Stillwake,
        HexCoord Deepcall) ChooseCoreSettlementCoords(
            HexGrid grid,
            MapCoverageMap coverageMap,
            Dictionary<string, MapCoverage> regions,
            Random random,
            HashSet<HexCoord> takenPhysical)
    {
        var frosthaven = ChooseNamedSettlementCoord(
            grid,
            random,
            takenPhysical,
            "Frosthaven",
            5,
            cell => FrosthavenScore(grid, coverageMap, regions["northlands"], cell));
        var ironhold = ChooseNamedSettlementCoord(
            grid,
            random,
            takenPhysical,
            "Ironhold",
            5,
            cell => IronholdScore(grid, coverageMap, regions["heartlands"], cell));
        var driftwatch = ChooseNamedSettlementCoord(
            grid,
            random,
            takenPhysical,
            "Driftwatch",
            6,
            cell => DriftwatchScore(grid, coverageMap, regions["coastlands"], cell));
        var wavecrest = ChooseNamedSettlementCoord(
            grid,
            random,
            takenPhysical,
            "Wavecrest",
            6,
            cell => WavecrestScore(grid, coverageMap, regions["coastlands"], cell));
        var midpoint = ChooseNamedSettlementCoord(
            grid,
            random,
            takenPhysical,
            "Midpoint",
            5,
            cell => MidpointScore(grid, coverageMap, regions["heartlands"], cell));
        var stillwake = ChooseNamedSettlementCoord(
            grid,
            random,
            takenPhysical,
            "Stillwake",
            6,
            cell => FarSettlementScore(grid, coverageMap, regions["farwake"], cell));
        var deepcall = ChooseNamedSettlementCoord(
            grid,
            random,
            takenPhysical,
            "Deepcall",
            6,
            cell => FarSettlementScore(grid, coverageMap, regions["blackwater"], cell));

        return (frosthaven, ironhold, driftwatch, wavecrest, midpoint, stillwake, deepcall);
    }

    private static HexCoord ChooseNamedSettlementCoord(
        HexGrid grid,
        Random random,
        HashSet<HexCoord> takenPhysical,
        string name,
        int minimumDistance,
        Func<HexCell, float> scorer)
    {
        var coord = ChooseAnchor(grid, random, takenPhysical, minimumDistance, scorer);
        ReservePhysical(coord, takenPhysical, name);
        return coord;
    }

    private static (List<SeedEntity> All, SeedEntity Frosthaven) CreateCoreSettlements(
        SeedAssembly seed,
        (
            HexCoord Frosthaven,
            HexCoord Ironhold,
            HexCoord Driftwatch,
            HexCoord Wavecrest,
            HexCoord Midpoint,
            HexCoord Stillwake,
            HexCoord Deepcall) coords,
        HashSet<SemanticHexCoord> takenSemantic)
    {
        var frosthaven = CreateEntity(seed, K.Settlement, "rookery", "Frosthaven", C(IceRemembersCulture.AuroraStack), (coords.Frosthaven.Q, coords.Frosthaven.R));
        var ironhold = CreateEntity(seed, K.Settlement, "colony", "Ironhold", C(IceRemembersCulture.AuroraStack), (coords.Ironhold.Q, coords.Ironhold.R));
        var driftwatch = CreateEntity(seed, K.Settlement, "harbor", "Driftwatch", C(IceRemembersCulture.Nightshelf), (coords.Driftwatch.Q, coords.Driftwatch.R));
        var wavecrest = CreateEntity(seed, K.Settlement, "reefhold", "Wavecrest", C(IceRemembersCulture.Nightshelf), (coords.Wavecrest.Q, coords.Wavecrest.R));
        var midpoint = CreateEntity(seed, K.Settlement, "colony", "Midpoint", C(IceRemembersCulture.AuroraStack), (coords.Midpoint.Q, coords.Midpoint.R));
        var stillwake = CreateEntity(seed, K.Settlement, "reefhold", "Stillwake", C(IceRemembersCulture.WakeSingers), (coords.Stillwake.Q, coords.Stillwake.R));
        var deepcall = CreateEntity(seed, K.Settlement, "harbor", "Deepcall", C(IceRemembersCulture.WakeSingers), (coords.Deepcall.Q, coords.Deepcall.R));

        SetSemanticProjected(frosthaven, coords.Frosthaven, takenSemantic, new SemanticHexCoord(-1, -2));
        SetSemanticProjected(ironhold, coords.Ironhold, takenSemantic, new SemanticHexCoord(-2, 1));
        SetSemanticProjected(driftwatch, coords.Driftwatch, takenSemantic, new SemanticHexCoord(2, -1));
        SetSemanticProjected(wavecrest, coords.Wavecrest, takenSemantic, new SemanticHexCoord(1, 2));
        SetSemanticProjected(midpoint, coords.Midpoint, takenSemantic, SemanticHexCoord.Origin);
        SetSemanticProjected(stillwake, coords.Stillwake, takenSemantic, new SemanticHexCoord(7, -7));
        SetSemanticProjected(deepcall, coords.Deepcall, takenSemantic, new SemanticHexCoord(8, -6));

        return (new List<SeedEntity> { frosthaven, ironhold, driftwatch, wavecrest, midpoint, stillwake, deepcall }, frosthaven);
    }

    private static (SeedEntity Northern, SeedEntity Southern, SeedEntity Farwake) CreateCoreFactions(
        SeedAssembly seed,
        HexGrid grid,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic,
        (
            HexCoord Frosthaven,
            HexCoord Ironhold,
            HexCoord Driftwatch,
            HexCoord Wavecrest,
            HexCoord Midpoint,
            HexCoord Stillwake,
            HexCoord Deepcall) settlementCoords)
    {
        var northern = CreateEntity(seed, K.Faction, "compact", "Northern Compact", C(IceRemembersCulture.AuroraStack));
        var southern = CreateEntity(seed, K.Faction, "order", "Southern Order", C(IceRemembersCulture.Nightshelf));
        var farwake = CreateEntity(seed, K.Faction, "choir", "Farwake Choir", C(IceRemembersCulture.WakeSingers));

        var northernCoord = ChooseNearbyFreeCell(grid, takenPhysical, Centroid(settlementCoords.Frosthaven, settlementCoords.Ironhold, settlementCoords.Midpoint), searchRadius: 8)
            ?? throw new InvalidOperationException("Could not place Northern Compact near its settlements.");
        SetPhysical(northern, northernCoord, takenPhysical);
        var southernCoord = ChooseNearbyFreeCell(grid, takenPhysical, Centroid(settlementCoords.Driftwatch, settlementCoords.Wavecrest), searchRadius: 8)
            ?? throw new InvalidOperationException("Could not place Southern Order near its settlements.");
        SetPhysical(southern, southernCoord, takenPhysical);
        var farwakeCoord = ChooseNearbyFreeCell(grid, takenPhysical, Centroid(settlementCoords.Stillwake, settlementCoords.Deepcall), searchRadius: 8)
            ?? throw new InvalidOperationException("Could not place Farwake Choir near its settlements.");
        SetPhysical(farwake, farwakeCoord, takenPhysical);

        SetSemanticProjected(northern, northernCoord, takenSemantic, new SemanticHexCoord(-3, 3));
        SetSemanticProjected(southern, southernCoord, takenSemantic, new SemanticHexCoord(3, -3));
        SetSemanticProjected(farwake, farwakeCoord, takenSemantic, new SemanticHexCoord(9, -8));
        return (northern, southern, farwake);
    }

    private static void CreateCoreControlRelationships(
        SeedAssembly seed,
        (List<SeedEntity> All, SeedEntity Frosthaven) settlements,
        (SeedEntity Northern, SeedEntity Southern, SeedEntity Farwake) factions)
    {
        CreateRelationship(seed, "controlled_by", RelationshipPolarity.Neutral, settlements.All[0].SeedEntityId, factions.Northern.SeedEntityId, 1f);
        CreateRelationship(seed, "controlled_by", RelationshipPolarity.Neutral, settlements.All[1].SeedEntityId, factions.Northern.SeedEntityId, 1f);
        CreateRelationship(seed, "controlled_by", RelationshipPolarity.Neutral, settlements.All[4].SeedEntityId, factions.Northern.SeedEntityId, 0.8f);
        CreateRelationship(seed, "controlled_by", RelationshipPolarity.Neutral, settlements.All[2].SeedEntityId, factions.Southern.SeedEntityId, 1f);
        CreateRelationship(seed, "controlled_by", RelationshipPolarity.Neutral, settlements.All[3].SeedEntityId, factions.Southern.SeedEntityId, 1f);
        CreateRelationship(seed, "controlled_by", RelationshipPolarity.Neutral, settlements.All[5].SeedEntityId, factions.Farwake.SeedEntityId, 1f);
        CreateRelationship(seed, "controlled_by", RelationshipPolarity.Neutral, settlements.All[6].SeedEntityId, factions.Farwake.SeedEntityId, 1f);
    }

    private static void CreatePenguins(
        SeedAssembly seed,
        HexGrid grid,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic,
        List<SeedEntity> allSettlements,
        (SeedEntity Northern, SeedEntity Southern, SeedEntity Farwake) factions)
    {
        var settlementsByCoord = allSettlements.ToDictionary(
            settlement => settlement.Coordinates,
            settlement => settlement.SeedEntityId);
        var hostSettlementsByFaction = new Dictionary<int, SeedEntity[]>(3)
        {
            [factions.Northern.SeedEntityId] = [allSettlements[0], allSettlements[1], allSettlements[4]],
            [factions.Southern.SeedEntityId] = [allSettlements[2], allSettlements[3]],
            [factions.Farwake.SeedEntityId] = [allSettlements[5], allSettlements[6]],
        };
        var plans = CreateBasePenguinPlans(factions.Northern, factions.Southern, factions.Farwake);
        plans.AddRange(CreateSupportingPenguinPlans(random, factions.Northern, factions.Southern, factions.Farwake));

        var npcIndex = 0;
        foreach (var (subtype, name, culture, factionId, loyalty) in plans)
        {
            var hostSettlements = hostSettlementsByFaction[factionId];
            var host = hostSettlements[npcIndex % hostSettlements.Length];
            var hex = SelectPenguinHex(grid, random, takenPhysical, host.Coordinates);
            takenPhysical.Add(hex);

            var npc = CreateEntity(
                seed,
                K.Penguin, subtype, name, culture,
                coordinates: (hex.Q, hex.R));

            AssignPenguinSemanticPosition(npc, hex, npcIndex, random, takenSemantic);
            npcIndex++;
            CreateRelationship(seed, "member_of", RelationshipPolarity.Neutral, npc.SeedEntityId, factionId, loyalty);

            if (settlementsByCoord.TryGetValue(hex, out var settlementId))
            {
                CreateRelationship(seed, "home_base", RelationshipPolarity.Neutral, npc.SeedEntityId, settlementId, 1f);
            }
        }
    }

    private static List<(string Subtype, string Name, string Culture, int Faction, float Loyalty)> CreateBasePenguinPlans(
        SeedEntity factionA,
        SeedEntity factionB,
        SeedEntity factionC) =>
        new()
        {
            ("warden", "Captain Greyfeather", C(IceRemembersCulture.AuroraStack), factionA.SeedEntityId, 0.9f),
            ("broker", "Trader Shellback", C(IceRemembersCulture.Nightshelf), factionB.SeedEntityId, 0.7f),
            ("witness", "Elder Deepcurrent", C(IceRemembersCulture.AuroraStack), factionA.SeedEntityId, 0.8f),
            ("witness", "Singer Underwake", C(IceRemembersCulture.WakeSingers), factionC.SeedEntityId, 0.85f),
        };

    private static HexCoord SelectPenguinHex(
        HexGrid grid,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HexCoord hostCoord)
    {
        var availableHexes = FindNearbyPenguinHexes(grid, takenPhysical, hostCoord);
        if (availableHexes.Count == 0)
        {
            availableHexes = FindFallbackPenguinHexes(grid, takenPhysical);
        }

        if (availableHexes.Count == 0)
        {
            throw new InvalidOperationException(
                "No free walkable hexes remain for penguin placement; grid may be too small or too many fixed entities.");
        }

        return availableHexes[random.Next(availableHexes.Count)];
    }

    private static List<HexCoord> FindNearbyPenguinHexes(
        HexGrid grid,
        HashSet<HexCoord> takenPhysical,
        HexCoord hostCoord) =>
        HexCoord.CoordsInRadius(hostCoord, radius: 2)
            .Where(coord => grid.ContainsCoord(coord) && HexCoord.Distance(hostCoord, coord) is > 0 and <= 2)
            .Where(coord => !takenPhysical.Contains(coord))
            .OrderBy(coord => HexCoord.Distance(hostCoord, coord))
            .ThenBy(coord => coord.Q)
            .ThenBy(coord => coord.R)
            .ToList();

    private static List<HexCoord> FindFallbackPenguinHexes(
        HexGrid grid,
        HashSet<HexCoord> takenPhysical) =>
        grid.AllCells
            .Select(cell => cell.Coord)
            .Where(coord => !takenPhysical.Contains(coord))
            .OrderBy(coord => coord.Q)
            .ThenBy(coord => coord.R)
            .ToList();

    private static void AssignPenguinSemanticPosition(
        SeedEntity npc,
        HexCoord hex,
        int npcIndex,
        Random random,
        HashSet<SemanticHexCoord> takenSemantic)
    {
        var semanticSeeds = new[]
        {
            new SemanticHexCoord(8, 0),
            new SemanticHexCoord(0, 8),
            new SemanticHexCoord(-8, -4),
        };

        if (npcIndex < semanticSeeds.Length)
        {
            SetSemantic(npc, FindNearestFreeSemantic(semanticSeeds[npcIndex], takenSemantic), takenSemantic);
            return;
        }

        SetSemanticProjected(npc, hex, takenSemantic, RandomSemanticBias(random));
    }

    private static void CreateStormcleaver(
        SeedAssembly seed,
        HexGrid grid,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic,
        List<SeedEntity> allSettlements,
        SeedEntity frosthaven)
    {
        var artifactCoord = ChooseNearbyFreeCell(grid, takenPhysical, frosthaven.Coordinates, searchRadius: 5)
            ?? throw new InvalidOperationException("Could not place Stormcleaver near Frosthaven.");
        var artifact = CreateEntity(
            seed,
            K.Artifact, "weapon", "Stormcleaver", C(IceRemembersCulture.AuroraStack),
            coordinates: (artifactCoord.Q, artifactCoord.R));
        takenPhysical.Add(artifactCoord);
        SetSemanticProjected(artifact, artifactCoord, takenSemantic, bias: new SemanticHexCoord(4, -1));

        CreateRelationship(seed, "anchored", RelationshipPolarity.Neutral, artifact.SeedEntityId, frosthaven.SeedEntityId, 1f);

        CreateSupportingArtifacts(
            seed,
            grid,
            random,
            takenPhysical,
            takenSemantic,
            allSettlements);
    }

    private static SemanticHexCoord RandomSemanticBias(Random random) =>
        new(random.Next(-5, 6), random.Next(-5, 6));

    private static T Pick<T>(IReadOnlyList<T> options, Random random) =>
        options[random.Next(options.Count)];

    private static string UniqueName(string baseName, HashSet<string> usedNames)
    {
        if (usedNames.Add(baseName))
        {
            return baseName;
        }

        var suffix = 2;
        while (true)
        {
            var candidate = $"{baseName} {suffix}";
            if (usedNames.Add(candidate))
            {
                return candidate;
            }

            suffix++;
        }
    }

    private static HexCoord ChooseAnchor(
        HexGrid grid,
        Random random,
        HashSet<HexCoord> takenPhysical,
        int minimumDistance,
        Func<HexCell, float> scorer)
    {
        var candidates = grid.AllCells
            .Where(cell =>
                !takenPhysical.Contains(cell.Coord) &&
                takenPhysical.All(existing => HexCoord.Distance(existing, cell.Coord) >= minimumDistance))
            .Select(cell => (Cell: cell, Score: scorer(cell)))
            .OrderByDescending(candidate => candidate.Score)
            .Take(24)
            .ToList();

        if (candidates.Count == 0)
        {
            candidates = grid.AllCells
                .Where(cell => !takenPhysical.Contains(cell.Coord))
                .Select(cell => (Cell: cell, Score: scorer(cell)))
                .OrderByDescending(candidate => candidate.Score)
                .Take(8)
                .ToList();
        }

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("Could not find a valid anchor candidate on the procedural map.");
        }

        return candidates[random.Next(Math.Min(4, candidates.Count))].Cell.Coord;
    }

    private static float RegionNorthlandsScore(HexGrid grid, HexCell cell)
    {
        var center = HexCoord.Distance(cell.Coord, new HexCoord(0, 0));
        var cold = CountTerrainsInRadius(grid, cell.Coord, 2, IceRemembersTerrain.Ice, IceRemembersTerrain.Shelf) * 5f;
        var tundra = CountTerrainsInRadius(grid, cell.Coord, 2, IceRemembersTerrain.Tundra) * 2f;
        return TerrainScore(cell.Terrain, IceRemembersTerrain.Ice, 40f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Tundra, 18f)
            + cold
            + tundra
            + (center * 1.2f);
    }

    private static float RegionHeartlandsScore(HexGrid grid, HexCell cell)
    {
        var centerBias = 25f - (HexCoord.Distance(cell.Coord, new HexCoord(0, 0)) * 1.2f);
        return TerrainScore(cell.Terrain, IceRemembersTerrain.HotSprings, 36f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Tundra, 24f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Wastes, 8f)
            + (CountTerrainsInRadius(grid, cell.Coord, 2, IceRemembersTerrain.HotSprings, IceRemembersTerrain.Tundra) * 3f)
            + centerBias;
    }

    private static float RegionCoastlandsScore(HexGrid grid, HexCell cell)
    {
        var rimBias = HexCoord.Distance(cell.Coord, new HexCoord(0, 0)) * 1.1f;
        return TerrainScore(cell.Terrain, IceRemembersTerrain.Shelf, 34f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Reef, 30f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Tundra, 12f)
            + (CountAdjacentTerrains(grid, cell.Coord, IceRemembersTerrain.Ocean, IceRemembersTerrain.Reef, IceRemembersTerrain.Shelf) * 9f)
            + rimBias;
    }

    private static float RegionBlackwaterScore(HexGrid grid, HexCell cell)
    {
        var rimBias = HexCoord.Distance(cell.Coord, new HexCoord(0, 0)) * 1.3f;
        return TerrainScore(cell.Terrain, IceRemembersTerrain.Ocean, 42f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Reef, 18f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Shelf, 14f)
            + (CountAdjacentTerrains(grid, cell.Coord, IceRemembersTerrain.Ocean, IceRemembersTerrain.Reef) * 8f)
            + rimBias;
    }

    private static float RegionWhiteEmptyScore(HexGrid grid, HexCell cell)
    {
        var rimBias = HexCoord.Distance(cell.Coord, new HexCoord(0, 0)) * 1.4f;
        return TerrainScore(cell.Terrain, IceRemembersTerrain.Ice, 34f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Wastes, 26f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Crags, 18f)
            + (CountTerrainsInRadius(grid, cell.Coord, 2, IceRemembersTerrain.Ice, IceRemembersTerrain.Wastes, IceRemembersTerrain.Crags) * 4f)
            + rimBias;
    }

    private static float FrosthavenScore(HexGrid grid, MapCoverageMap coverageMap, MapCoverage northlands, HexCell cell)
    {
        return CoverageAffinity(coverageMap, northlands, cell.Coord, 28f, 12f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Ice, 28f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Tundra, 20f)
            + (CountTerrainsInRadius(grid, cell.Coord, 2, IceRemembersTerrain.Ice, IceRemembersTerrain.Shelf) * 4f)
            + (HexCoord.Distance(cell.Coord, new HexCoord(0, 0)) * 0.5f);
    }

    private static float IronholdScore(HexGrid grid, MapCoverageMap coverageMap, MapCoverage heartlands, HexCell cell)
    {
        return CoverageAffinity(coverageMap, heartlands, cell.Coord, 18f, 10f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Tundra, 16f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Crags, 20f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Wastes, 18f)
            + (CountAdjacentTerrains(grid, cell.Coord, IceRemembersTerrain.Crags, IceRemembersTerrain.Wastes) * 5f);
    }

    private static float DriftwatchScore(HexGrid grid, MapCoverageMap coverageMap, MapCoverage coastlands, HexCell cell)
    {
        return CoverageAffinity(coverageMap, coastlands, cell.Coord, 22f, 12f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Shelf, 26f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Reef, 24f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Tundra, 10f)
            + (CountAdjacentTerrains(grid, cell.Coord, IceRemembersTerrain.Ocean, IceRemembersTerrain.Reef, IceRemembersTerrain.Shelf) * 8f);
    }

    private static float WavecrestScore(HexGrid grid, MapCoverageMap coverageMap, MapCoverage coastlands, HexCell cell)
    {
        return CoverageAffinity(coverageMap, coastlands, cell.Coord, 20f, 10f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Reef, 30f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Shelf, 24f)
            + (CountAdjacentTerrains(grid, cell.Coord, IceRemembersTerrain.Ocean, IceRemembersTerrain.Reef) * 10f)
            + (HexCoord.Distance(cell.Coord, new HexCoord(0, 0)) * 0.4f);
    }

    private static float MidpointScore(HexGrid grid, MapCoverageMap coverageMap, MapCoverage heartlands, HexCell cell)
    {
        return CoverageAffinity(coverageMap, heartlands, cell.Coord, 22f, 8f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.HotSprings, 20f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Tundra, 18f)
            + (20f - HexCoord.Distance(cell.Coord, new HexCoord(0, 0)));
    }

    private static float FarSettlementScore(HexGrid grid, MapCoverageMap coverageMap, MapCoverage coverage, HexCell cell)
    {
        return CoverageAffinity(coverageMap, coverage, cell.Coord, 26f, 12f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Reef, 28f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Shelf, 22f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Ocean, 12f)
            + (CountAdjacentTerrains(grid, cell.Coord, IceRemembersTerrain.Ocean, IceRemembersTerrain.Reef, IceRemembersTerrain.Shelf) * 8f)
            + (HexCoord.Distance(cell.Coord, new HexCoord(0, 0)) * 0.7f);
    }

    private static float CoverageAffinity(MapCoverageMap coverageMap, MapCoverage coverage, HexCoord coord, float insideBonus, float nearbyBonus)
    {
        if (coverage.ContainsCell(coord))
        {
            return insideBonus;
        }

        var nearest = coverage.Cells.Min(existing => HexCoord.Distance(existing, coord));
        return Math.Max(0f, nearbyBonus - nearest);
    }

    private static HexCoord Centroid(params HexCoord[] coords)
    {
        var q = (int)Math.Round(coords.Average(coord => coord.Q), MidpointRounding.ToEven);
        var r = (int)Math.Round(coords.Average(coord => coord.R), MidpointRounding.ToEven);
        return new HexCoord(q, r);
    }

}
