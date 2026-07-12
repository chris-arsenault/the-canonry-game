namespace IceRemembers.Seed;

using Engine.Domain.Startup;
using Engine.Graph;
using Engine.WorldGen;

using IceRemembers;

using K = IceRemembers.IceRemembersKinds;

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

    private static void CreateEntities(
        SeedAssembly seed,
        HexGrid grid,
        MapCoverageMap coverageMap,
        Dictionary<string, MapCoverage> regions,
        Random random)
    {
        var takenPhysical = new HashSet<HexCoord>();
        var takenSemantic = new HashSet<SemanticHexCoord>();
        var regionAnchors = PlaceRegionAnchors(seed, regions, random, takenPhysical, takenSemantic);
        CreateRegionCoverage(seed, grid, coverageMap, regions, regionAnchors);

        var settlementCoords = ChooseCoreSettlementCoords(seed, coverageMap, regions, random, takenPhysical);
        var coreSettlements = CreateCoreSettlements(seed, settlementCoords, random, takenSemantic);
        var allSettlements = coreSettlements.All;

        var factions = CreateCoreFactions(
            seed,
            random,
            takenPhysical,
            takenSemantic,
            settlementCoords,
            coreSettlements.AuroraStack,
            coreSettlements.All[1]);
        CreateCoreControlRelationships(seed, coreSettlements, factions);

        CreatePenguins(seed, random, takenPhysical, takenSemantic, allSettlements, factions);
        CreateStormcleaver(seed, random, takenPhysical, takenSemantic, coreSettlements.AuroraStack);
        CreateNightshelfWeapon(seed, random, takenPhysical, takenSemantic, coreSettlements.All[1]);

        CreateOrcas(seed, random, takenPhysical, takenSemantic, factions.WakeSingers);
        CreatePointsOfInterest(seed, random, takenPhysical, takenSemantic);
        var wakeReefhold = CreateWakeReefhold(seed, random, takenPhysical, takenSemantic, factions.WakeSingers);
        CreateAbilities(seed, random, takenPhysical, takenSemantic, regions);
        CreateTransports(seed, random, takenPhysical, takenSemantic, coreSettlements, factions, wakeReefhold);
        CreateOccurrences(seed, random, takenPhysical, takenSemantic, coreSettlements, wakeReefhold);

        CreateLeadershipRelationships(seed, factions);
        CreateCanonicalLinks(seed, random, takenPhysical, takenSemantic, factions);
        CreateNightshelfCustodyAssets(seed, random, takenPhysical, takenSemantic);
    }


    private static void CreateCanonicalLinks(
        SeedAssembly seed,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic,
        (SeedEntity Council, SeedEntity Exchange, SeedEntity Claws, SeedEntity WakeSingers) factions)
    {
        var hollows = FindEntityByName(seed, "Smokeheld Luscoa☽");
        var drownedLedge = FindEntityByName(seed, "Codex∴lockhold");
        CreateRelationship(seed, "home_base", RelationshipPolarity.Neutral, hollows.SeedEntityId, drownedLedge.SeedEntityId, 1f);

        var hollowsChartCoord = ChoosePhysicalCoordinate(
            seed,
            random,
            takenPhysical,
            K.Artifact,
            "tome",
            C(IceRemembersCulture.Nightshelf),
            minimumDistance: 0,
            origin: drownedLedge.Coordinates,
            searchRadius: 4);
        var hollowsChart = CreateEntity(
            seed,
            K.Artifact, "tome", "The Silence-fed Compendium", "Compendium", C(IceRemembersCulture.Nightshelf),
            coordinates: (hollowsChartCoord.Q, hollowsChartCoord.R));
        takenPhysical.Add(hollowsChartCoord);
        SetSemanticNaturalNear(seed, hollowsChart, drownedLedge, random, takenSemantic, searchRadius: 10);
        CreateRelationship(seed, "anchored", RelationshipPolarity.Neutral, hollowsChart.SeedEntityId, drownedLedge.SeedEntityId, 1f);
        CreateRelationship(seed, "holds", RelationshipPolarity.Neutral, hollows.SeedEntityId, hollowsChart.SeedEntityId, 1f);

        var corpseCurrent = FindEntityByName(seed, "Gulletbloom");
        CreateRelationship(seed, "controlled_by", RelationshipPolarity.Neutral, corpseCurrent.SeedEntityId, factions.WakeSingers.SeedEntityId, 1f);
        CreateRelationship(seed, "claimed_by", RelationshipPolarity.Neutral, corpseCurrent.SeedEntityId, factions.WakeSingers.SeedEntityId, 0.88f);

        var grandmotherDeep = FindEntityByName(seed, "Grandmother Deep");
        var tidescream = FindEntityByName(seed, "Khrarnd Grendbrund");
        CreateRelationship(seed, "home_base", RelationshipPolarity.Neutral, grandmotherDeep.SeedEntityId, corpseCurrent.SeedEntityId, 1f);
        CreateRelationship(seed, "home_base", RelationshipPolarity.Neutral, tidescream.SeedEntityId, corpseCurrent.SeedEntityId, 1f);

        var dreamingToothCoord = ChoosePhysicalCoordinate(
            seed,
            random,
            takenPhysical,
            K.Artifact,
            "relic",
            C(IceRemembersCulture.WakeSingers),
            minimumDistance: 0,
            origin: corpseCurrent.Coordinates,
            searchRadius: 4);
        var dreamingTooth = CreateEntity(
            seed,
            K.Artifact, "relic", "Firstkilltooth Of Grund", "Firstkilltooth", C(IceRemembersCulture.WakeSingers),
            coordinates: (dreamingToothCoord.Q, dreamingToothCoord.R));
        takenPhysical.Add(dreamingToothCoord);
        SetSemanticNaturalNear(seed, dreamingTooth, corpseCurrent, random, takenSemantic, searchRadius: 12);
        CreateRelationship(seed, "anchored", RelationshipPolarity.Neutral, dreamingTooth.SeedEntityId, corpseCurrent.SeedEntityId, 1f);
        CreateRelationship(seed, "claimed_by", RelationshipPolarity.Neutral, dreamingTooth.SeedEntityId, factions.WakeSingers.SeedEntityId, 1f);

        // The Corpse Current starts as a living sacred site: devotion edges
        // from the choir and its elders, the Firstkilltooth already given to
        // the deep, and one live grievance so the blood channel can boot.
        CreateRelationship(seed, "offered_to", RelationshipPolarity.Positive, factions.WakeSingers.SeedEntityId, corpseCurrent.SeedEntityId, 0.85f);
        CreateRelationship(seed, "offered_to", RelationshipPolarity.Positive, grandmotherDeep.SeedEntityId, corpseCurrent.SeedEntityId, 0.9f);
        CreateRelationship(seed, "offered_to", RelationshipPolarity.Positive, tidescream.SeedEntityId, corpseCurrent.SeedEntityId, 0.7f);
        CreateRelationship(seed, "consigned_to", RelationshipPolarity.Neutral, dreamingTooth.SeedEntityId, corpseCurrent.SeedEntityId, 0.9f);
        CreateRelationship(seed, "at_war_with", RelationshipPolarity.Negative, factions.WakeSingers.SeedEntityId, factions.Exchange.SeedEntityId, 0.6f);
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
        SeedAssembly seed,
        MapCoverageMap coverageMap,
        Dictionary<string, MapCoverage> regions,
        Random random,
        HashSet<HexCoord> takenPhysical)
    {
        var auroraStack = ChooseNamedSettlementCoord(
            seed,
            random,
            takenPhysical,
            "Aurora Stack",
            "rookery",
            C(IceRemembersCulture.AuroraStack),
            5,
            cell => AuroraStackContextScore(coverageMap, regions["northlands"], cell));
        var nightfallShelf = ChooseNamedSettlementCoord(
            seed,
            random,
            takenPhysical,
            "Nightshelf",
            "harbor",
            C(IceRemembersCulture.Nightshelf),
            6,
            cell => SettlementCoverageContextScore(coverageMap, regions["coastlands"], cell, 22f, 12f)
                + NamedDistanceBandContext(cell.Coord, auroraStack, idealDistance: 20, maxBonus: 70f, step: 3.5f));
        var drownedLedge = ChooseNamedSettlementCoord(
            seed,
            random,
            takenPhysical,
            "Codex∴lockhold",
            "holdfast",
            C(IceRemembersCulture.Nightshelf),
            6,
            cell => SettlementCoverageContextScore(coverageMap, regions["coastlands"], cell, 22f, 10f)
                + NamedDistanceBandContext(cell.Coord, nightfallShelf, idealDistance: 8, maxBonus: 50f, step: 5f));
        var stillPools = ChooseNamedSettlementCoord(
            seed,
            random,
            takenPhysical,
            "The Heihua Rampart Hearth (frost)",
            "sanctuary",
            C(IceRemembersCulture.AuroraStack),
            14,
            cell => StillPoolsContextScore(cell)
                + NamedRemoteContext(cell.Coord, auroraStack, minimumDistance: 28, step: 3f));

        return (auroraStack, nightfallShelf, drownedLedge, stillPools);
    }

    private static HexCoord ChooseNamedSettlementCoord(
        SeedAssembly seed,
        Random random,
        HashSet<HexCoord> takenPhysical,
        string name,
        string subtype,
        string culture,
        int minimumDistance,
        Func<HexCell, float> contextScorer)
    {
        var coord = ChooseAnchor(
            seed,
            random,
            takenPhysical,
            K.Settlement,
            subtype,
            culture,
            minimumDistance,
            contextScorer);
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
        Random random,
        HashSet<SemanticHexCoord> takenSemantic)
    {
        var auroraStack = CreateEntity(seed, K.Settlement, "rookery", "Aurora Stack", "Aurora", C(IceRemembersCulture.AuroraStack), (coords.AuroraStack.Q, coords.AuroraStack.R));
        var nightfallShelf = CreateEntity(seed, K.Settlement, "harbor", "Nightshelf", "Nightshelf", C(IceRemembersCulture.Nightshelf), (coords.NightfallShelf.Q, coords.NightfallShelf.R));
        var drownedLedge = CreateEntity(seed, K.Settlement, "holdfast", "Codex∴lockhold", "Codex", C(IceRemembersCulture.Nightshelf), (coords.DrownedLedge.Q, coords.DrownedLedge.R));
        var stillPools = CreateEntity(seed, K.Settlement, "sanctuary", "The Heihua Rampart Hearth (frost)", "Heihua Hearth", C(IceRemembersCulture.AuroraStack), (coords.StillPools.Q, coords.StillPools.R));

        SetSemanticNatural(seed, auroraStack, random, takenSemantic, minimumDistance: 6);
        SetSemanticNatural(seed, nightfallShelf, random, takenSemantic, minimumDistance: 6);
        SetSemanticNatural(seed, drownedLedge, random, takenSemantic, minimumDistance: 6);
        SetSemanticNatural(seed, stillPools, random, takenSemantic, minimumDistance: 6);

        return (new List<SeedEntity> { auroraStack, nightfallShelf, drownedLedge, stillPools }, auroraStack);
    }

    private static (SeedEntity Council, SeedEntity Exchange, SeedEntity Claws, SeedEntity WakeSingers) CreateCoreFactions(
        SeedAssembly seed,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic,
        (
            HexCoord AuroraStack,
            HexCoord NightfallShelf,
            HexCoord DrownedLedge,
            HexCoord StillPools) settlementCoords,
        SeedEntity auroraStack,
        SeedEntity nightfallShelf)
    {
        var council = CreateEntity(seed, K.Faction, "compact", "The Meridian (starlight-bound)", "Meridian", C(IceRemembersCulture.AuroraStack));
        var exchange = CreateEntity(seed, K.Faction, "compact", "The Icebound Exchange", "Icebound Exchange", C(IceRemembersCulture.AuroraStack));
        var claws = CreateEntity(seed, K.Faction, "order", "Midnight Claws", "Midnight Claws", C(IceRemembersCulture.Nightshelf));
        var wakeSingers = CreateEntity(seed, K.Faction, "choir", "The Wake-Singers", "Wake-Singers", C(IceRemembersCulture.WakeSingers));

        SetPhysicalNaturalNear(seed, council, settlementCoords.AuroraStack, random, takenPhysical, searchRadius: 8);
        SetPhysicalNaturalNear(seed, exchange, settlementCoords.AuroraStack, random, takenPhysical, searchRadius: 6);
        SetPhysicalNaturalNear(seed, claws, settlementCoords.NightfallShelf, random, takenPhysical, searchRadius: 8);
        SetPhysicalNaturalNear(seed, wakeSingers, new HexCoord(32, -32), random, takenPhysical, searchRadius: 8);

        SetSemanticNaturalNear(seed, council, auroraStack, random, takenSemantic, searchRadius: 12);
        SetSemanticNaturalNear(seed, exchange, auroraStack, random, takenSemantic, searchRadius: 12);
        SetSemanticNaturalNear(seed, claws, nightfallShelf, random, takenSemantic, searchRadius: 12);
        SetSemanticNatural(seed, wakeSingers, random, takenSemantic, minimumDistance: 6);
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
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic,
        List<SeedEntity> allSettlements,
        (SeedEntity Council, SeedEntity Exchange, SeedEntity Claws, SeedEntity WakeSingers) factions)
    {
        var settlementsByCoord = allSettlements.ToDictionary(
            settlement => settlement.Coordinates,
            settlement => settlement.SeedEntityId);
        var hostSettlementsByFaction = new Dictionary<int, SeedEntity[]>(2)
        {
            [factions.Council.SeedEntityId] = [allSettlements[0]],
            [factions.Claws.SeedEntityId] = [allSettlements[1], allSettlements[2]],
        };
        var plans = CreateBasePenguinPlans(factions.Council, factions.Claws);

        var npcIndex = 0;
        foreach (var (subtype, name, referenceName, culture, factionId, loyalty) in plans)
        {
            var hostSettlements = hostSettlementsByFaction[factionId];
            var host = hostSettlements[npcIndex % hostSettlements.Length];
            var hex = SelectPenguinHex(seed, random, takenPhysical, host.Coordinates, subtype, culture);
            takenPhysical.Add(hex);

            var npc = CreateEntity(
                seed,
                K.Penguin, subtype, name, referenceName, culture,
                coordinates: (hex.Q, hex.R));

            AssignPenguinSemanticPosition(seed, npc, seed.Entity(factionId), random, takenSemantic);
            npcIndex++;
            CreateRelationship(seed, "member_of", RelationshipPolarity.Neutral, npc.SeedEntityId, factionId, loyalty);

            if (settlementsByCoord.TryGetValue(hex, out var settlementId))
            {
                CreateRelationship(seed, "home_base", RelationshipPolarity.Neutral, npc.SeedEntityId, settlementId, 1f);
            }
        }
    }

    private static List<(string Subtype, string Name, string ReferenceName, string Culture, int Faction, float Loyalty)> CreateBasePenguinPlans(
        SeedEntity factionA,
        SeedEntity factionB) =>
        new()
        {
            ("witness", "High-Beak Auditor Selka", "Selka", C(IceRemembersCulture.AuroraStack), factionA.SeedEntityId, 1f),
            ("broker", "Shadow-Broker Nyla", "Nyla", C(IceRemembersCulture.Nightshelf), factionB.SeedEntityId, 1f),
            ("exile", "Smokeheld Luscoa☽", "Luscoa", C(IceRemembersCulture.Nightshelf), factionB.SeedEntityId, 0.4f),
            ("broker", "Factor Roum☽-vel", "Roum", C(IceRemembersCulture.Nightshelf), factionB.SeedEntityId, 0.9f),
            ("broker", "Veilmonger Tutris☽", "Tutris", C(IceRemembersCulture.Nightshelf), factionB.SeedEntityId, 0.8f),
        };

    private static HexCoord SelectPenguinHex(
        SeedAssembly seed,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HexCoord hostCoord,
        string subtype,
        string culture) =>
        ChoosePhysicalCoordinate(
            seed,
            random,
            takenPhysical,
            K.Penguin,
            subtype,
            culture,
            minimumDistance: 0,
            origin: hostCoord,
            searchRadius: 2);

    private static void AssignPenguinSemanticPosition(
        SeedAssembly seed,
        SeedEntity npc,
        SeedEntity faction,
        Random random,
        HashSet<SemanticHexCoord> takenSemantic)
    {
        SetSemanticNaturalNear(seed, npc, faction, random, takenSemantic, searchRadius: 10, minimumDistance: 2);
    }

    private static void CreateStormcleaver(
        SeedAssembly seed,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic,
        SeedEntity auroraStack)
    {
        var artifactCoord = ChoosePhysicalCoordinate(
            seed,
            random,
            takenPhysical,
            K.Artifact,
            "weapon",
            C(IceRemembersCulture.AuroraStack),
            minimumDistance: 0,
            origin: auroraStack.Coordinates,
            searchRadius: 5);
        var artifact = CreateEntity(
            seed,
            K.Artifact, "weapon", "The Celestial Lightlance Of Haven", "Lightlance", C(IceRemembersCulture.AuroraStack),
            coordinates: (artifactCoord.Q, artifactCoord.R));
        takenPhysical.Add(artifactCoord);
        SetSemanticNaturalNear(seed, artifact, auroraStack, random, takenSemantic, searchRadius: 10);

        CreateRelationship(seed, "anchored", RelationshipPolarity.Neutral, artifact.SeedEntityId, auroraStack.SeedEntityId, 1f);
    }

    private static void CreateNightshelfWeapon(
        SeedAssembly seed,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic,
        SeedEntity nightfallShelf)
    {
        var nyla = FindEntityByName(seed, "Shadow-Broker Nyla");
        var artifactCoord = ChoosePhysicalCoordinate(
            seed,
            random,
            takenPhysical,
            K.Artifact,
            "weapon",
            C(IceRemembersCulture.Nightshelf),
            minimumDistance: 0,
            origin: nightfallShelf.Coordinates,
            searchRadius: 5);
        var artifact = CreateEntity(
            seed,
            K.Artifact, "weapon", "The Hushed Romphaea", "Romphaea", C(IceRemembersCulture.Nightshelf),
            coordinates: (artifactCoord.Q, artifactCoord.R));
        takenPhysical.Add(artifactCoord);
        SetSemanticNaturalNear(seed, artifact, nightfallShelf, random, takenSemantic, searchRadius: 10);

        CreateRelationship(seed, "anchored", RelationshipPolarity.Neutral, artifact.SeedEntityId, nightfallShelf.SeedEntityId, 1f);
        CreateRelationship(seed, "holds", RelationshipPolarity.Neutral, nyla.SeedEntityId, artifact.SeedEntityId, 1f);
    }

    private static HexCoord ChooseAnchor(
        SeedAssembly seed,
        Random random,
        HashSet<HexCoord> takenPhysical,
        EntityKind kind,
        string subtype,
        string culture,
        int minimumDistance,
        Func<HexCell, float> contextScorer) =>
        ChoosePhysicalCoordinate(
            seed,
            random,
            takenPhysical,
            kind,
            subtype,
            culture,
            minimumDistance,
            origin: null,
            searchRadius: 0,
            contextScorer);

    private static float RegionNorthlandsContextScore(HexCell cell) =>
        HexCoord.Distance(cell.Coord, new HexCoord(0, 0)) * 1.2f;

    private static float RegionHeartlandsContextScore(HexCell cell) =>
        25f - (HexCoord.Distance(cell.Coord, new HexCoord(0, 0)) * 1.2f);

    private static float RegionCoastlandsContextScore(HexCell cell) =>
        HexCoord.Distance(cell.Coord, new HexCoord(0, 0)) * 1.1f;

    private static float AuroraStackContextScore(MapCoverageMap coverageMap, MapCoverage northlands, HexCell cell) =>
        CoverageAffinity(coverageMap, northlands, cell.Coord, 28f, 12f)
        + (HexCoord.Distance(cell.Coord, new HexCoord(0, 0)) * 0.5f);

    private static float SettlementCoverageContextScore(
        MapCoverageMap coverageMap,
        MapCoverage coverage,
        HexCell cell,
        float insideBonus,
        float nearbyBonus) =>
        CoverageAffinity(coverageMap, coverage, cell.Coord, insideBonus, nearbyBonus);

    private static float NamedDistanceBandContext(
        HexCoord coord,
        HexCoord anchor,
        int idealDistance,
        float maxBonus,
        float step) =>
        Math.Max(0f, maxBonus - (Math.Abs(HexCoord.Distance(coord, anchor) - idealDistance) * step));

    private static float NamedRemoteContext(
        HexCoord coord,
        HexCoord anchor,
        int minimumDistance,
        float step) =>
        Math.Max(0f, HexCoord.Distance(coord, anchor) - minimumDistance) * step;

    private static float StillPoolsContextScore(HexCell cell) =>
        HexCoord.Distance(cell.Coord, new HexCoord(0, 0)) * 1.6f;

    private static float CoverageAffinity(MapCoverageMap coverageMap, MapCoverage coverage, HexCoord coord, float insideBonus, float nearbyBonus)
    {
        if (coverage.ContainsCell(coord))
        {
            return insideBonus;
        }

        var nearest = coverage.Cells.Min(existing => HexCoord.Distance(existing, coord));
        return Math.Max(0f, nearbyBonus - nearest);
    }

    private static readonly (string Name, string ReferenceName, string Subtype)[] OrcaPlans =
    [
        ("Grandmother Deep", "Grandmother Deep", "matriarch"),
        ("Ghakhrath Gnurlgath Skarn", "Ghakhrath", "elder"),
        ("Khrarnd Grendbrund", "Khrarnd", "scout"),
    ];

    private readonly record struct PointOfInterestPlan(
        string Name,
        string ReferenceName,
        string Subtype,
        IceRemembersTerrain Terrain,
        IceRemembersCulture Culture);

    private static readonly PointOfInterestPlan[] PointOfInterestPlans =
    [
        new("Hroolhroos, Where The Ice Breathes", "Hroolhroos", "vent", IceRemembersTerrain.HotSprings, IceRemembersCulture.TheIce),
        new("Roorrsthaa Warmcut", "Roorrsthaa", "vent", IceRemembersTerrain.Crags, IceRemembersCulture.TheIce),
        new("Starved Hrah'moon, Where The Water Remembers", "Hrah'moon", "shoal", IceRemembersTerrain.Shelf, IceRemembersCulture.TheIce),
        new("Gulletbloom", "Gulletbloom", "bloom", IceRemembersTerrain.Ocean, IceRemembersCulture.WakeSingers),
    ];

    private readonly record struct AbilityPlan(
        string Name,
        string ReferenceName,
        string Subtype,
        IceRemembersCulture Culture,
        string AnchorRegionKey,
        string PrimaryHolderName,
        string SecondaryHolderName);

    private static readonly AbilityPlan[] AbilityPlans =
    [
        new("Zhei's Compass Freeze", "Compass Freeze", "craft", IceRemembersCulture.AuroraStack, "heartlands", "High-Beak Auditor Selka", "Grandmother Deep"),
        new("Wane∴chill", "Wane", "craft", IceRemembersCulture.Nightshelf, "coastlands", "Shadow-Broker Nyla", "Smokeheld Luscoa☽"),
        new("Flay∴descend", "Descend", "rite", IceRemembersCulture.Nightshelf, "coastlands", "Factor Roum☽-vel", "Veilmonger Tutris☽"),
        new("Kzundkhrrak Way", "Kzundkhrrak", "rite", IceRemembersCulture.WakeSingers, "farwake", "Khrarnd Grendbrund", "Grandmother Deep"),
    ];

    private static void CreateOrcas(
        SeedAssembly seed,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic,
        SeedEntity farwakeFaction)
    {
        foreach (var (name, referenceName, subtype) in OrcaPlans)
        {
            var coord = ChooseAnchor(
                seed,
                random,
                takenPhysical,
                K.Orca,
                subtype,
                C(IceRemembersCulture.WakeSingers),
                minimumDistance: 4,
                OrcaContextScore);
            ReservePhysical(coord, takenPhysical, name);

            var orca = CreateEntity(
                seed,
                K.Orca,
                subtype,
                name,
                referenceName,
                C(IceRemembersCulture.WakeSingers),
                coordinates: (coord.Q, coord.R));
            SetSemanticNaturalNear(seed, orca, farwakeFaction, random, takenSemantic, searchRadius: 12);

            CreateRelationship(
                seed,
                "member_of",
                RelationshipPolarity.Neutral,
                orca.SeedEntityId,
                farwakeFaction.SeedEntityId,
                0.85f);
        }
    }

    private static float OrcaContextScore(HexCell cell) =>
        HexCoord.Distance(cell.Coord, new HexCoord(0, 0)) * 1.4f;

    private static void CreatePointsOfInterest(
        SeedAssembly seed,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic)
    {
        foreach (var plan in PointOfInterestPlans)
        {
            var coord = ChooseAnchor(
                seed,
                random,
                takenPhysical,
                K.PointOfInterest,
                plan.Subtype,
                C(plan.Culture),
                minimumDistance: 3,
                cell => NamedTerrainAffinity(cell, plan.Terrain));
            ReservePhysical(coord, takenPhysical, plan.Name);

            var poi = CreateEntity(
                seed,
                K.PointOfInterest,
                plan.Subtype,
                plan.Name,
                plan.ReferenceName,
                C(plan.Culture),
                coordinates: (coord.Q, coord.R));
            SetSemanticNatural(seed, poi, random, takenSemantic);
        }
    }

    private static SeedEntity CreateWakeReefhold(
        SeedAssembly seed,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic,
        SeedEntity wakeSingers)
    {
        var corpseCurrent = FindEntityByName(seed, "Gulletbloom");
        var coord = ChoosePhysicalCoordinate(
            seed,
            random,
            takenPhysical,
            K.Settlement,
            "reefhold",
            C(IceRemembersCulture.WakeSingers),
            minimumDistance: 0,
            origin: corpseCurrent.Coordinates,
            searchRadius: 6);
        ReservePhysical(coord, takenPhysical, "Singgrundreef");

        var reefhold = CreateEntity(
            seed,
            K.Settlement,
            "reefhold",
            "Singgrundreef",
            "Singgrundreef",
            C(IceRemembersCulture.WakeSingers),
            coordinates: (coord.Q, coord.R));
        SetSemanticNaturalNear(seed, reefhold, wakeSingers, random, takenSemantic, searchRadius: 10);

        CreateRelationship(seed, "controlled_by", RelationshipPolarity.Neutral, reefhold.SeedEntityId, wakeSingers.SeedEntityId, 0.86f);
        CreateRelationship(seed, "founded_from", RelationshipPolarity.Positive, reefhold.SeedEntityId, corpseCurrent.SeedEntityId, 0.90f);
        CreateRelationship(seed, "home_base", RelationshipPolarity.Neutral, wakeSingers.SeedEntityId, reefhold.SeedEntityId, 1f);
        return reefhold;
    }

    private static float NamedTerrainAffinity(HexCell cell, IceRemembersTerrain preferredTerrain) =>
        string.Equals(cell.Terrain.StableName, preferredTerrain.ToDomainName(), StringComparison.Ordinal) ? 40f : 0f;

    private static void CreateAbilities(
        SeedAssembly seed,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic,
        Dictionary<string, MapCoverage> regions)
    {
        foreach (var plan in AbilityPlans)
        {
            var anchorRegion = regions[plan.AnchorRegionKey];
            var anchorCoord = seed.Entity(anchorRegion.OwnerEntityId).Coordinates;
            var coord = ChoosePhysicalCoordinate(
                seed,
                random,
                takenPhysical,
                K.Ability,
                plan.Subtype,
                C(plan.Culture),
                minimumDistance: 0,
                origin: anchorCoord,
                searchRadius: 6);
            ReservePhysical(coord, takenPhysical, plan.Name);

            var ability = CreateEntity(
                seed,
                K.Ability,
                plan.Subtype,
                plan.Name,
                plan.ReferenceName,
                C(plan.Culture),
                coordinates: (coord.Q, coord.R));
            SetSemanticNatural(seed, ability, random, takenSemantic);

            CreateRelationship(
                seed,
                "anchored",
                RelationshipPolarity.Neutral,
                ability.SeedEntityId,
                anchorRegion.OwnerEntityId,
                0.8f);

            CreateAbilityPractitioner(seed, ability, plan.PrimaryHolderName, 0.85f);
            CreateAbilityPractitioner(seed, ability, plan.SecondaryHolderName, 0.7f);
            CreateTeachingLineage(seed, plan.SecondaryHolderName, plan.PrimaryHolderName, 0.75f);
        }
    }

    private static void CreateAbilityPractitioner(
        SeedAssembly seed,
        SeedEntity ability,
        string holderName,
        float strength)
    {
        var holder = seed.Entities.FirstOrDefault(entity =>
            string.Equals(entity.Name, holderName, StringComparison.Ordinal));
        if (holder is null)
        {
            throw new InvalidOperationException(
                $"Ability '{ability.Name}' references unknown holder '{holderName}'.");
        }

        seed.CreateRelationship(
            IceRemembersRelationship.PracticedBy.ToDomainName(),
            RelationshipPolarity.Positive,
            ability.SeedEntityId,
            holder.SeedEntityId,
            strength);
    }

    private static void CreateTeachingLineage(
        SeedAssembly seed,
        string studentName,
        string teacherName,
        float strength)
    {
        var student = seed.Entities.FirstOrDefault(entity =>
            string.Equals(entity.Name, studentName, StringComparison.Ordinal));
        if (student is null)
        {
            throw new InvalidOperationException(
                $"Teaching lineage references unknown student '{studentName}'.");
        }

        var teacher = seed.Entities.FirstOrDefault(entity =>
            string.Equals(entity.Name, teacherName, StringComparison.Ordinal));
        if (teacher is null)
        {
            throw new InvalidOperationException(
                $"Teaching lineage references unknown teacher '{teacherName}'.");
        }

        seed.CreateRelationship(
            IceRemembersRelationship.TaughtBy.ToDomainName(),
            RelationshipPolarity.Positive,
            student.SeedEntityId,
            teacher.SeedEntityId,
            strength);
    }

    private static void CreateTransports(
        SeedAssembly seed,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic,
        (List<SeedEntity> All, SeedEntity AuroraStack) settlements,
        (SeedEntity Council, SeedEntity Exchange, SeedEntity Claws, SeedEntity WakeSingers) factions,
        SeedEntity wakeReefhold)
    {
        CreateSeedTransport(
            seed,
            random,
            takenPhysical,
            takenSemantic,
            "Xialong Sanctum-shield (sun)",
            "Xialong Shield",
            "battleship",
            IceRemembersCulture.AuroraStack,
            factions.Council,
            settlements.AuroraStack,
            searchRadius: 8);
        CreateSeedTransport(
            seed,
            random,
            takenPhysical,
            takenSemantic,
            "Shade Skiff☽",
            "Shade Skiff",
            "scout",
            IceRemembersCulture.Nightshelf,
            factions.Claws,
            settlements.All[1],
            searchRadius: 8);
        CreateSeedTransport(
            seed,
            random,
            takenPhysical,
            takenSemantic,
            "Mawwakerunner",
            "Mawwakerunner",
            "scout",
            IceRemembersCulture.WakeSingers,
            factions.WakeSingers,
            wakeReefhold,
            searchRadius: 8);
    }

    private static void CreateSeedTransport(
        SeedAssembly seed,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic,
        string name,
        string referenceName,
        string subtype,
        IceRemembersCulture culture,
        SeedEntity holder,
        SeedEntity anchor,
        int searchRadius)
    {
        var coord = ChoosePhysicalCoordinate(
            seed,
            random,
            takenPhysical,
            K.Transport,
            subtype,
            C(culture),
            minimumDistance: 0,
            origin: anchor.Coordinates,
            searchRadius);
        ReservePhysical(coord, takenPhysical, name);

        var transport = CreateEntity(
            seed,
            K.Transport,
            subtype,
            name,
            referenceName,
            C(culture),
            coordinates: (coord.Q, coord.R));
        SetSemanticNaturalNear(seed, transport, holder, random, takenSemantic, searchRadius: 10);

        CreateRelationship(
            seed,
            "holds",
            RelationshipPolarity.Neutral,
            holder.SeedEntityId,
            transport.SeedEntityId,
            1f);
        CreateRelationship(
            seed,
            "native_to",
            RelationshipPolarity.Positive,
            transport.SeedEntityId,
            anchor.SeedEntityId,
            0.9f);
    }

    private static void CreateOccurrences(
        SeedAssembly seed,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic,
        (List<SeedEntity> All, SeedEntity AuroraStack) settlements,
        SeedEntity wakeReefhold)
    {
        CreateSeedOccurrence(
            seed,
            random,
            takenPhysical,
            takenSemantic,
            "Confluence (aurora-touched)",
            "Confluence",
            "festival",
            IceRemembersCulture.AuroraStack,
            settlements.AuroraStack,
            searchRadius: 6);
        CreateSeedOccurrence(
            seed,
            random,
            takenPhysical,
            takenSemantic,
            "☽'whisper Vigil",
            "Whisper Vigil",
            "festival",
            IceRemembersCulture.Nightshelf,
            settlements.All[1],
            searchRadius: 6);
        CreateSeedOccurrence(
            seed,
            random,
            takenPhysical,
            takenSemantic,
            "Zrag Crush Feast",
            "Zrag Feast",
            "festival",
            IceRemembersCulture.WakeSingers,
            wakeReefhold,
            searchRadius: 6);
    }

    private static void CreateSeedOccurrence(
        SeedAssembly seed,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic,
        string name,
        string referenceName,
        string subtype,
        IceRemembersCulture culture,
        SeedEntity anchor,
        int searchRadius)
    {
        var coord = ChoosePhysicalCoordinate(
            seed,
            random,
            takenPhysical,
            K.Occurrence,
            subtype,
            C(culture),
            minimumDistance: 0,
            origin: anchor.Coordinates,
            searchRadius);
        ReservePhysical(coord, takenPhysical, name);

        var occurrence = CreateEntity(
            seed,
            K.Occurrence,
            subtype,
            name,
            referenceName,
            C(culture),
            coordinates: (coord.Q, coord.R));
        SetSemanticNaturalNear(seed, occurrence, anchor, random, takenSemantic, searchRadius: 10);

        CreateRelationship(
            seed,
            "home_base",
            RelationshipPolarity.Neutral,
            occurrence.SeedEntityId,
            anchor.SeedEntityId,
            1f);
    }
}
