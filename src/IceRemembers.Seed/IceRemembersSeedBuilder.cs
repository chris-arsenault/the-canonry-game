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

        return new Dictionary<string, MapCoverage>(StringComparer.Ordinal)
        {
            ["northlands"] = northlands,
            ["heartlands"] = heartlands,
            ["coastlands"] = coastlands,
            ["farwake"] = farwake,
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

        CreatePenguins(seed, grid, random, takenPhysical, takenSemantic, allSettlements, factions);
        CreateStormcleaver(seed, grid, random, takenPhysical, takenSemantic, allSettlements, coreSettlements.AuroraStack);

        CreateOrcas(seed, grid, random, takenPhysical, takenSemantic, factions.WakeSingers);
        CreatePointsOfInterest(seed, grid, random, takenPhysical, takenSemantic);
        CreateAbilities(seed, grid, random, takenPhysical, takenSemantic, regions);
        CreateTransports(seed, grid, random, takenPhysical, takenSemantic, regions, factions.Council);
        CreateOccurrences(seed, grid, random, takenPhysical, takenSemantic, coreSettlements.AuroraStack);

        CreateLeadershipRelationships(seed, factions);
        CreateCanonicalLinks(seed, grid, takenPhysical, takenSemantic, factions);
    }

    private static void CreateCanonicalLinks(
        SeedAssembly seed,
        HexGrid grid,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic,
        (SeedEntity Council, SeedEntity Exchange, SeedEntity Claws, SeedEntity WakeSingers) factions)
    {
        var hollows = FindEntityByName(seed, "Keeper Hollows");
        var drownedLedge = FindEntityByName(seed, "The Drowned Ledge");
        CreateRelationship(seed, "home_base", RelationshipPolarity.Neutral, hollows.SeedEntityId, drownedLedge.SeedEntityId, 1f);

        var hollowsChartCoord = ChooseNearbyFreeCell(grid, takenPhysical, drownedLedge.Coordinates, searchRadius: 4)
            ?? throw new InvalidOperationException("Could not place The Hollows Chart near The Drowned Ledge.");
        var hollowsChart = CreateEntity(
            seed,
            K.Artifact, "tome", "The Hollows Chart", C(IceRemembersCulture.Nightshelf),
            coordinates: (hollowsChartCoord.Q, hollowsChartCoord.R));
        takenPhysical.Add(hollowsChartCoord);
        SetSemanticProjected(hollowsChart, hollowsChartCoord, takenSemantic, bias: new SemanticHexCoord(4, 1));
        CreateRelationship(seed, "anchored", RelationshipPolarity.Neutral, hollowsChart.SeedEntityId, drownedLedge.SeedEntityId, 1f);
        CreateRelationship(seed, "holds", RelationshipPolarity.Neutral, hollows.SeedEntityId, hollowsChart.SeedEntityId, 1f);

        var corpseCurrent = FindEntityByName(seed, "The Corpse Current");
        CreateRelationship(seed, "controlled_by", RelationshipPolarity.Neutral, corpseCurrent.SeedEntityId, factions.WakeSingers.SeedEntityId, 1f);

        var grandmotherDeep = FindEntityByName(seed, "Grandmother Deep");
        var tidescream = FindEntityByName(seed, "Tidescream");
        CreateRelationship(seed, "home_base", RelationshipPolarity.Neutral, grandmotherDeep.SeedEntityId, corpseCurrent.SeedEntityId, 1f);
        CreateRelationship(seed, "home_base", RelationshipPolarity.Neutral, tidescream.SeedEntityId, corpseCurrent.SeedEntityId, 1f);

        var dreamingToothCoord = ChooseNearbyFreeCell(grid, takenPhysical, corpseCurrent.Coordinates, searchRadius: 4)
            ?? throw new InvalidOperationException("Could not place The Dreaming Tooth near The Corpse Current.");
        var dreamingTooth = CreateEntity(
            seed,
            K.Artifact, "relic", "The Dreaming Tooth", C(IceRemembersCulture.WakeSingers),
            coordinates: (dreamingToothCoord.Q, dreamingToothCoord.R));
        takenPhysical.Add(dreamingToothCoord);
        SetSemanticProjected(dreamingTooth, dreamingToothCoord, takenSemantic, bias: new SemanticHexCoord(8, -7));
        CreateRelationship(seed, "anchored", RelationshipPolarity.Neutral, dreamingTooth.SeedEntityId, corpseCurrent.SeedEntityId, 1f);
        CreateRelationship(seed, "holds", RelationshipPolarity.Neutral, factions.WakeSingers.SeedEntityId, dreamingTooth.SeedEntityId, 1f);
    }

    private static void CreateLeadershipRelationships(
        SeedAssembly seed,
        (SeedEntity Council, SeedEntity Exchange, SeedEntity Claws, SeedEntity WakeSingers) factions)
    {
        var selka = FindEntityByName(seed, "High-Beak Auditor Selka");
        var nyla = FindEntityByName(seed, "Shadow-Broker Nyla");
        var grandmotherDeep = FindEntityByName(seed, "Grandmother Deep");

        CreateRelationship(seed, "led_by", RelationshipPolarity.Neutral, factions.Council.SeedEntityId, selka.SeedEntityId, 1f);
        CreateRelationship(seed, "led_by", RelationshipPolarity.Neutral, factions.Exchange.SeedEntityId, selka.SeedEntityId, 1f);

        CreateRelationship(seed, "led_by", RelationshipPolarity.Neutral, factions.Claws.SeedEntityId, nyla.SeedEntityId, 1f);

        CreateRelationship(seed, "led_by", RelationshipPolarity.Neutral, factions.WakeSingers.SeedEntityId, grandmotherDeep.SeedEntityId, 1f);
    }

    private static SeedEntity FindEntityByName(SeedAssembly seed, string name) =>
        seed.Entities.FirstOrDefault(entity =>
            string.Equals(entity.Name, name, StringComparison.Ordinal))
        ?? throw new InvalidOperationException($"Seed entity '{name}' not found.");

    private static (
        HexCoord Northlands,
        HexCoord Heartlands,
        HexCoord Coastlands,
        HexCoord Farwake) PlaceRegionAnchors(
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

        SetPhysical(seed.Entity(regions["northlands"].OwnerEntityId), northlands, takenPhysical);
        SetPhysical(seed.Entity(regions["heartlands"].OwnerEntityId), heartlands, takenPhysical);
        SetPhysical(seed.Entity(regions["coastlands"].OwnerEntityId), coastlands, takenPhysical);
        SetPhysical(seed.Entity(regions["farwake"].OwnerEntityId), farwake, takenPhysical);

        SetSemanticProjected(seed.Entity(regions["northlands"].OwnerEntityId), northlands, takenSemantic, new SemanticHexCoord(-2, 2));
        SetSemanticProjected(seed.Entity(regions["heartlands"].OwnerEntityId), heartlands, takenSemantic, new SemanticHexCoord(0, 0));
        SetSemanticProjected(seed.Entity(regions["coastlands"].OwnerEntityId), coastlands, takenSemantic, new SemanticHexCoord(2, -2));
        SetSemanticProjected(seed.Entity(regions["farwake"].OwnerEntityId), farwake, takenSemantic, new SemanticHexCoord(6, -6));

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
            HexCoord Farwake) anchors)
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
    }

    private static (
        HexCoord AuroraStack,
        HexCoord NightfallShelf,
        HexCoord DrownedLedge,
        HexCoord StillPools) ChooseCoreSettlementCoords(
            HexGrid grid,
            MapCoverageMap coverageMap,
            Dictionary<string, MapCoverage> regions,
            Random random,
            HashSet<HexCoord> takenPhysical)
    {
        var auroraStack = ChooseNamedSettlementCoord(
            grid,
            random,
            takenPhysical,
            "Aurora Stack",
            5,
            cell => AuroraStackScore(grid, coverageMap, regions["northlands"], cell));
        var nightfallShelf = ChooseNamedSettlementCoord(
            grid,
            random,
            takenPhysical,
            "Nightfall Shelf",
            6,
            cell => NightfallShelfScore(grid, coverageMap, regions["coastlands"], cell));
        var drownedLedge = ChooseNamedSettlementCoord(
            grid,
            random,
            takenPhysical,
            "The Drowned Ledge",
            6,
            cell => DrownedLedgeScore(grid, coverageMap, regions["coastlands"], cell));
        var stillPools = ChooseNamedSettlementCoord(
            grid,
            random,
            takenPhysical,
            "The Still Pools",
            14,
            cell => StillPoolsScore(grid, cell));

        return (auroraStack, nightfallShelf, drownedLedge, stillPools);
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

    private static (List<SeedEntity> All, SeedEntity AuroraStack) CreateCoreSettlements(
        SeedAssembly seed,
        (
            HexCoord AuroraStack,
            HexCoord NightfallShelf,
            HexCoord DrownedLedge,
            HexCoord StillPools) coords,
        HashSet<SemanticHexCoord> takenSemantic)
    {
        var auroraStack = CreateEntity(seed, K.Settlement, "rookery", "Aurora Stack", C(IceRemembersCulture.AuroraStack), (coords.AuroraStack.Q, coords.AuroraStack.R));
        var nightfallShelf = CreateEntity(seed, K.Settlement, "harbor", "Nightfall Shelf", C(IceRemembersCulture.Nightshelf), (coords.NightfallShelf.Q, coords.NightfallShelf.R));
        var drownedLedge = CreateEntity(seed, K.Settlement, "holdfast", "The Drowned Ledge", C(IceRemembersCulture.Nightshelf), (coords.DrownedLedge.Q, coords.DrownedLedge.R));
        var stillPools = CreateEntity(seed, K.Settlement, "sanctuary", "The Still Pools", C(IceRemembersCulture.AuroraStack), (coords.StillPools.Q, coords.StillPools.R));

        SetSemanticProjected(auroraStack, coords.AuroraStack, takenSemantic, new SemanticHexCoord(-1, -2));
        SetSemanticProjected(nightfallShelf, coords.NightfallShelf, takenSemantic, new SemanticHexCoord(2, -1));
        SetSemanticProjected(drownedLedge, coords.DrownedLedge, takenSemantic, new SemanticHexCoord(3, 1));
        SetSemanticProjected(stillPools, coords.StillPools, takenSemantic, new SemanticHexCoord(-9, 9));

        return (new List<SeedEntity> { auroraStack, nightfallShelf, drownedLedge, stillPools }, auroraStack);
    }

    private static (SeedEntity Council, SeedEntity Exchange, SeedEntity Claws, SeedEntity WakeSingers) CreateCoreFactions(
        SeedAssembly seed,
        HexGrid grid,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic,
        (
            HexCoord AuroraStack,
            HexCoord NightfallShelf,
            HexCoord DrownedLedge,
            HexCoord StillPools) settlementCoords)
    {
        var council = CreateEntity(seed, K.Faction, "compact", "Council of Luminaries", C(IceRemembersCulture.AuroraStack));
        var exchange = CreateEntity(seed, K.Faction, "compact", "The Icebound Exchange", C(IceRemembersCulture.AuroraStack));
        var claws = CreateEntity(seed, K.Faction, "order", "Midnight Claws", C(IceRemembersCulture.Nightshelf));
        var wakeSingers = CreateEntity(seed, K.Faction, "choir", "The Wake-Singers", C(IceRemembersCulture.WakeSingers));

        var councilCoord = ChooseNearbyFreeCell(grid, takenPhysical, settlementCoords.AuroraStack, searchRadius: 8)
            ?? throw new InvalidOperationException("Could not place Council of Luminaries near Aurora Stack.");
        SetPhysical(council, councilCoord, takenPhysical);
        var exchangeCoord = ChooseNearbyFreeCell(grid, takenPhysical, settlementCoords.AuroraStack, searchRadius: 6)
            ?? throw new InvalidOperationException("Could not place The Icebound Exchange near Aurora Stack.");
        SetPhysical(exchange, exchangeCoord, takenPhysical);
        var clawsCoord = ChooseNearbyFreeCell(grid, takenPhysical, settlementCoords.NightfallShelf, searchRadius: 8)
            ?? throw new InvalidOperationException("Could not place Midnight Claws near Nightfall Shelf.");
        SetPhysical(claws, clawsCoord, takenPhysical);
        var wakeSingersCoord = ChooseNearbyFreeCell(grid, takenPhysical, new HexCoord(32, -32), searchRadius: 8)
            ?? throw new InvalidOperationException("Could not place The Wake-Singers in deep water.");
        SetPhysical(wakeSingers, wakeSingersCoord, takenPhysical);

        SetSemanticProjected(council, councilCoord, takenSemantic, new SemanticHexCoord(-3, 3));
        SetSemanticProjected(exchange, exchangeCoord, takenSemantic, new SemanticHexCoord(-2, 4));
        SetSemanticProjected(claws, clawsCoord, takenSemantic, new SemanticHexCoord(3, -3));
        SetSemanticProjected(wakeSingers, wakeSingersCoord, takenSemantic, new SemanticHexCoord(9, -8));
        return (council, exchange, claws, wakeSingers);
    }

    private static void CreateCoreControlRelationships(
        SeedAssembly seed,
        (List<SeedEntity> All, SeedEntity AuroraStack) settlements,
        (SeedEntity Council, SeedEntity Exchange, SeedEntity Claws, SeedEntity WakeSingers) factions)
    {
        CreateRelationship(seed, "controlled_by", RelationshipPolarity.Neutral, settlements.All[0].SeedEntityId, factions.Council.SeedEntityId, 1f);
        CreateRelationship(seed, "claimed_by", RelationshipPolarity.Neutral, settlements.All[0].SeedEntityId, factions.Exchange.SeedEntityId, 0.7f);
        CreateRelationship(seed, "controlled_by", RelationshipPolarity.Neutral, settlements.All[1].SeedEntityId, factions.Claws.SeedEntityId, 1f);
        CreateRelationship(seed, "controlled_by", RelationshipPolarity.Neutral, settlements.All[2].SeedEntityId, factions.Claws.SeedEntityId, 0.4f);
        CreateRelationship(seed, "controlled_by", RelationshipPolarity.Neutral, settlements.All[3].SeedEntityId, factions.Council.SeedEntityId, 0.1f);
    }

    private static void CreatePenguins(
        SeedAssembly seed,
        HexGrid grid,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic,
        List<SeedEntity> allSettlements,
        (SeedEntity Council, SeedEntity Exchange, SeedEntity Claws, SeedEntity WakeSingers) factions)
    {
        var settlementsByCoord = allSettlements.ToDictionary(
            settlement => settlement.Coordinates,
            settlement => settlement.SeedEntityId);
        _ = random;
        var hostSettlementsByFaction = new Dictionary<int, SeedEntity[]>(2)
        {
            [factions.Council.SeedEntityId] = [allSettlements[0]],
            [factions.Claws.SeedEntityId] = [allSettlements[1], allSettlements[2]],
        };
        var plans = CreateBasePenguinPlans(factions.Council, factions.Claws);

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
        SeedEntity factionB) =>
        new()
        {
            ("witness", "High-Beak Auditor Selka", C(IceRemembersCulture.AuroraStack), factionA.SeedEntityId, 1f),
            ("broker", "Shadow-Broker Nyla", C(IceRemembersCulture.Nightshelf), factionB.SeedEntityId, 1f),
            ("exile", "Keeper Hollows", C(IceRemembersCulture.Nightshelf), factionB.SeedEntityId, 0.4f),
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
                "No free hexes remain for penguin placement; grid may be too small or too many fixed entities.");
        }

        return availableHexes[random.Next(availableHexes.Count)];
    }

    private static List<HexCoord> FindNearbyPenguinHexes(
        HexGrid grid,
        HashSet<HexCoord> takenPhysical,
        HexCoord hostCoord) =>
        HexCoord.CoordsInRadius(hostCoord, radius: 2)
            .Where(coord =>
                grid.ContainsCoord(coord) &&
                HexCoord.Distance(hostCoord, coord) is > 0 and <= 2 &&
                !takenPhysical.Contains(coord))
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
        SeedEntity auroraStack)
    {
        _ = random;
        _ = allSettlements;
        var artifactCoord = ChooseNearbyFreeCell(grid, takenPhysical, auroraStack.Coordinates, searchRadius: 5)
            ?? throw new InvalidOperationException("Could not place The Radiant Ring near Aurora Stack.");
        var artifact = CreateEntity(
            seed,
            K.Artifact, "weapon", "The Radiant Ring", C(IceRemembersCulture.AuroraStack),
            coordinates: (artifactCoord.Q, artifactCoord.R));
        takenPhysical.Add(artifactCoord);
        SetSemanticProjected(artifact, artifactCoord, takenSemantic, bias: new SemanticHexCoord(4, -1));

        CreateRelationship(seed, "anchored", RelationshipPolarity.Neutral, artifact.SeedEntityId, auroraStack.SeedEntityId, 1f);
    }

    private static SemanticHexCoord RandomSemanticBias(Random random) =>
        new(random.Next(-5, 6), random.Next(-5, 6));

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

    private static float AuroraStackScore(HexGrid grid, MapCoverageMap coverageMap, MapCoverage northlands, HexCell cell)
    {
        return CoverageAffinity(coverageMap, northlands, cell.Coord, 28f, 12f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Ice, 28f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Tundra, 20f)
            + (CountTerrainsInRadius(grid, cell.Coord, 2, IceRemembersTerrain.Ice, IceRemembersTerrain.Shelf) * 4f)
            + (HexCoord.Distance(cell.Coord, new HexCoord(0, 0)) * 0.5f);
    }

    private static float NightfallShelfScore(HexGrid grid, MapCoverageMap coverageMap, MapCoverage coastlands, HexCell cell)
    {
        return CoverageAffinity(coverageMap, coastlands, cell.Coord, 22f, 12f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Shelf, 26f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Reef, 24f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Tundra, 10f)
            + (CountAdjacentTerrains(grid, cell.Coord, IceRemembersTerrain.Ocean, IceRemembersTerrain.Reef, IceRemembersTerrain.Shelf) * 8f);
    }

    private static float DrownedLedgeScore(HexGrid grid, MapCoverageMap coverageMap, MapCoverage coastlands, HexCell cell)
    {
        return CoverageAffinity(coverageMap, coastlands, cell.Coord, 22f, 10f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Shelf, 22f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Crags, 14f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Tundra, 6f)
            + (CountAdjacentTerrains(grid, cell.Coord, IceRemembersTerrain.Ocean, IceRemembersTerrain.Reef) * 6f);
    }

    private static float StillPoolsScore(HexGrid grid, HexCell cell)
    {
        var rimDistance = HexCoord.Distance(cell.Coord, new HexCoord(0, 0));
        return TerrainScore(cell.Terrain, IceRemembersTerrain.HotSprings, 50f)
            + TerrainScore(cell.Terrain, IceRemembersTerrain.Tundra, 18f)
            + (CountTerrainsInRadius(grid, cell.Coord, 2, IceRemembersTerrain.HotSprings) * 12f)
            + (rimDistance * 1.6f);
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

}
