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
internal sealed class IceRemembersSeedBuilder
{
    private const int MaxAcceptedSeedAttempts = 64;

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

    private static readonly string[] CoastalSettlementSubtypes = ["harbor", "reefhold"];
    private static readonly string[] InteriorSettlementSubtypes = ["rookery", "colony", "holdfast", "sanctuary"];
    private static readonly string[] InteriorCoverageKeys = ["northlands", "heartlands"];
    private static readonly string[] CoastalSettlementNames = ["Brinegate", "Lowwake", "Pearlmark", "Greyreef", "Tidehollow"];
    private static readonly string[] InteriorSettlementNames = ["Palehollow", "Snowcairn", "Embercrease", "Whitebarrow", "Underbell"];
    private static readonly string[] PenguinGivenNames = ["Softstep", "Glass-Eye", "Ashbill", "Keelback", "Mourncrest", "Little Thaw"];
    private static readonly string[] PenguinByNames = ["Underice", "Farcall", "Whitewake", "Coalbeak", "Noonless"];
    private static readonly string[] PenguinSubtypes = ["warden", "broker", "witness", "exile"];
    private static readonly string[] SupportingArtifactSubtypes = ["relic", "instrument", "tome", "seal"];
    private static readonly string[] SupportingArtifactNames = ["Low Bell", "Black Ledger", "Meltglass Needle", "Salted Key", "Moon-Under-Ice"];

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
            20,
            IceRemembersTerrainCatalog.CreateWorldGenCatalog(),
            IceRemembersTerrain.Wastes.ToDomainName());
        var coverageMap = new MapCoverageMap();
        PaintTerrain(grid, new Random(seed.TerrainSeed));
        return new SeedAssembly(grid, coverageMap);
    }

    private static void Populate(SeedAssembly assembly, WorldGenerationSeed seed)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var regions = CreateRegionEntities(assembly);
        CreateEntities(assembly, assembly.Grid, assembly.CoverageMap, regions, new Random(seed.EntitySeed));
    }

    private static void PaintTerrain(HexGrid grid, Random random)
    {
        var oceanSeeds = PickSeedCoords(
            grid,
            random,
            count: 3,
            minDistance: grid.Radius / 2,
            coord => HexCoord.Distance(coord, new HexCoord(0, 0)) >= (grid.Radius * 3) / 4);
        var iceSeeds = PickSeedCoords(
            grid,
            random,
            count: 3,
            minDistance: grid.Radius / 3,
            coord => HexCoord.Distance(coord, new HexCoord(0, 0)) >= grid.Radius / 2);
        var geothermalSeeds = PickSeedCoords(
            grid,
            random,
            count: 3,
            minDistance: grid.Radius / 4,
            coord => HexCoord.Distance(coord, new HexCoord(0, 0)) <= (grid.Radius * 3) / 4);
        var wasteSeeds = PickSeedCoords(
            grid,
            random,
            count: 3,
            minDistance: grid.Radius / 4,
            coord => HexCoord.Distance(coord, new HexCoord(0, 0)) is >= 4 and <= 16);
        var cragSeeds = PickSeedCoords(
            grid,
            random,
            count: 4,
            minDistance: grid.Radius / 4,
            coord => HexCoord.Distance(coord, new HexCoord(0, 0)) is >= 5 and <= 18);

        var oceanSalt = random.Next();
        var coldSalt = random.Next();
        var geothermalSalt = random.Next();
        var wasteSalt = random.Next();
        var cragSalt = random.Next();
        var fertilitySalt = random.Next();
        var maritimeSalt = random.Next();

        var coldness = new Dictionary<HexCoord, float>();

        foreach (var cell in grid.AllCells)
        {
            var radiusBias = HexCoord.Distance(cell.Coord, new HexCoord(0, 0)) / (float)grid.Radius;
            var oceanInfluence = ComputeInfluence(cell.Coord, oceanSeeds, radius: grid.Radius * 0.55f)
                + (radiusBias * 0.18f)
                + (CellNoise(cell.Coord, oceanSalt) * 0.14f);
            var coldInfluence = ComputeInfluence(cell.Coord, iceSeeds, radius: grid.Radius * 0.5f)
                + (radiusBias * 0.2f)
                + (CellNoise(cell.Coord, coldSalt) * 0.12f);
            var geothermalInfluence = ComputeInfluence(cell.Coord, geothermalSeeds, radius: grid.Radius * 0.24f)
                + (CellNoise(cell.Coord, geothermalSalt) * 0.12f);
            var aridInfluence = ComputeInfluence(cell.Coord, wasteSeeds, radius: grid.Radius * 0.3f)
                + (CellNoise(cell.Coord, wasteSalt) * 0.15f);
            var ruggedInfluence = ComputeInfluence(cell.Coord, cragSeeds, radius: grid.Radius * 0.22f)
                + (CellNoise(cell.Coord, cragSalt) * 0.12f);
            var fertilityNoise = CellNoise(cell.Coord, fertilitySalt);

            coldness[cell.Coord] = coldInfluence;

            var terrain = DetermineBaseTerrain(
                oceanInfluence,
                coldInfluence,
                geothermalInfluence,
                aridInfluence,
                ruggedInfluence);
            var (elevation, fertility) = TerrainProfileFor(
                terrain,
                oceanInfluence,
                coldInfluence,
                geothermalInfluence,
                ruggedInfluence,
                fertilityNoise);
            SetTerrainAndAmbientResources(grid, cell.Coord, terrain, elevation, fertility, random);
        }

        var maritimeTransforms = new List<(HexCoord Coord, IceRemembersTerrain Terrain)>();
        foreach (var cell in grid.AllCells)
        {
            if (IsTerrain(cell, IceRemembersTerrain.Ocean))
            {
                continue;
            }

            var adjacentOcean = CountAdjacentTerrains(grid, cell.Coord, IceRemembersTerrain.Ocean);
            if (adjacentOcean == 0)
            {
                continue;
            }

            if (coldness[cell.Coord] >= 0.58f || (adjacentOcean >= 3 && coldness[cell.Coord] >= 0.44f))
            {
                maritimeTransforms.Add((cell.Coord, IceRemembersTerrain.Shelf));
                continue;
            }

            if (!IsTerrain(cell, IceRemembersTerrain.Crags) &&
                !IsTerrain(cell, IceRemembersTerrain.HotSprings) &&
                (adjacentOcean >= 2 || CellNoise(cell.Coord, maritimeSalt) >= 0.66f))
            {
                maritimeTransforms.Add((cell.Coord, IceRemembersTerrain.Reef));
            }
        }

        foreach (var (coord, terrain) in maritimeTransforms)
        {
            var (elevation, fertility) = terrain switch
            {
                IceRemembersTerrain.Shelf => (10f, 0.35f + (CellNoise(coord, fertilitySalt ^ 17) * 0.1f)),
                IceRemembersTerrain.Reef => (5f, 0.45f + (CellNoise(coord, fertilitySalt ^ 29) * 0.2f)),
                _ => (20f, 0.5f),
            };
            SetTerrainAndAmbientResources(grid, coord, terrain, elevation, fertility, random);
        }
    }

    private static void SetTerrainAndAmbientResources(
        HexGrid grid,
        HexCoord coord,
        IceRemembersTerrain terrain,
        float elevation,
        float fertility,
        Random random)
    {
        grid.SetCell(coord, terrain.ToDomainName(), elevation, fertility);
        var cell = grid.GetCell(coord)!;
        TerrainAmbientResources.Seed(cell, random);
    }

    private static Dictionary<string, MapCoverage> CreateRegionEntities(SeedAssembly seed)
    {
        var northlands = new MapCoverage("Northlands",
            CreateEntity(seed, K.Region, "face", "Northlands", "none").SeedEntityId);
        var heartlands = new MapCoverage("Heartlands",
            CreateEntity(seed, K.Region, "reach", "Heartlands", "none").SeedEntityId);
        var coastlands = new MapCoverage("Coastlands",
            CreateEntity(seed, K.Region, "shelf", "Coastlands", "none").SeedEntityId);

        return new Dictionary<string, MapCoverage>
        {
            ["northlands"] = northlands,
            ["heartlands"] = heartlands,
            ["coastlands"] = coastlands,
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

        // Extension point: legendary anchors are still hand-authored entities, but
        // their placement is now procedural and terrain-scored. Additional seed
        // randomization should build on these scoring seams rather than reintroducing
        // fixed coordinates.
        var northlandsAnchor = ChooseAnchor(
            grid,
            random,
            takenPhysical,
            minimumDistance: 10,
            cell => RegionNorthlandsScore(grid, cell));
        var heartlandsAnchor = ChooseAnchor(
            grid,
            random,
            takenPhysical,
            minimumDistance: 10,
            cell => RegionHeartlandsScore(grid, cell));
        var coastlandsAnchor = ChooseAnchor(
            grid,
            random,
            takenPhysical,
            minimumDistance: 10,
            cell => RegionCoastlandsScore(grid, cell));

        SetPhysical(seed.Entity(regions["northlands"].OwnerEntityId), northlandsAnchor, takenPhysical);
        SetPhysical(seed.Entity(regions["heartlands"].OwnerEntityId), heartlandsAnchor, takenPhysical);
        SetPhysical(seed.Entity(regions["coastlands"].OwnerEntityId), coastlandsAnchor, takenPhysical);

        SetSemanticProjected(seed.Entity(regions["northlands"].OwnerEntityId), northlandsAnchor, takenSemantic, bias: new SemanticHexCoord(-2, 2));
        SetSemanticProjected(seed.Entity(regions["heartlands"].OwnerEntityId), heartlandsAnchor, takenSemantic, bias: new SemanticHexCoord(0, 0));
        SetSemanticProjected(seed.Entity(regions["coastlands"].OwnerEntityId), coastlandsAnchor, takenSemantic, bias: new SemanticHexCoord(2, -2));

        regions["northlands"] = CreateSeedCoverage(
            grid,
            coverageMap,
            seed,
            regions["northlands"],
            northlandsAnchor,
            desiredCellCount: 96,
            minimumCellCount: 72,
            preferredTerrains: TerrainSet(IceRemembersTerrain.Ice, IceRemembersTerrain.Tundra, IceRemembersTerrain.Shelf));
        regions["heartlands"] = CreateSeedCoverage(
            grid,
            coverageMap,
            seed,
            regions["heartlands"],
            heartlandsAnchor,
            desiredCellCount: 144,
            minimumCellCount: 110,
            preferredTerrains: TerrainSet(IceRemembersTerrain.Tundra, IceRemembersTerrain.HotSprings, IceRemembersTerrain.Wastes));
        regions["coastlands"] = CreateSeedCoverage(
            grid,
            coverageMap,
            seed,
            regions["coastlands"],
            coastlandsAnchor,
            desiredCellCount: 88,
            minimumCellCount: 64,
            preferredTerrains: TerrainSet(IceRemembersTerrain.Shelf, IceRemembersTerrain.Reef, IceRemembersTerrain.Ocean, IceRemembersTerrain.Tundra));

        var frosthavenCoord = ChooseAnchor(
            grid,
            random,
            takenPhysical,
            minimumDistance: 5,
            cell => FrosthavenScore(grid, coverageMap, regions["northlands"], cell));
        ReservePhysical(frosthavenCoord, takenPhysical, "Frosthaven");
        var ironholdCoord = ChooseAnchor(
            grid,
            random,
            takenPhysical,
            minimumDistance: 5,
            cell => IronholdScore(grid, coverageMap, regions["heartlands"], cell));
        ReservePhysical(ironholdCoord, takenPhysical, "Ironhold");
        var driftwatchCoord = ChooseAnchor(
            grid,
            random,
            takenPhysical,
            minimumDistance: 6,
            cell => DriftwatchScore(grid, coverageMap, regions["coastlands"], cell));
        ReservePhysical(driftwatchCoord, takenPhysical, "Driftwatch");
        var wavecrestCoord = ChooseAnchor(
            grid,
            random,
            takenPhysical,
            minimumDistance: 6,
            cell => WavecrestScore(grid, coverageMap, regions["coastlands"], cell));
        ReservePhysical(wavecrestCoord, takenPhysical, "Wavecrest");
        var midpointCoord = ChooseAnchor(
            grid,
            random,
            takenPhysical,
            minimumDistance: 5,
            cell => MidpointScore(grid, coverageMap, regions["heartlands"], cell));
        ReservePhysical(midpointCoord, takenPhysical, "Midpoint");

        var s1 = CreateEntity(
            seed,
            K.Settlement, "rookery", "Frosthaven", C(IceRemembersCulture.AuroraStack),
            coordinates: (frosthavenCoord.Q, frosthavenCoord.R));
        var s2 = CreateEntity(
            seed,
            K.Settlement, "colony", "Ironhold", C(IceRemembersCulture.AuroraStack),
            coordinates: (ironholdCoord.Q, ironholdCoord.R));
        var s3 = CreateEntity(
            seed,
            K.Settlement, "harbor", "Driftwatch", C(IceRemembersCulture.Nightshelf),
            coordinates: (driftwatchCoord.Q, driftwatchCoord.R));
        var s4 = CreateEntity(
            seed,
            K.Settlement, "reefhold", "Wavecrest", C(IceRemembersCulture.Nightshelf),
            coordinates: (wavecrestCoord.Q, wavecrestCoord.R));
        var s5 = CreateEntity(
            seed,
            K.Settlement, "colony", "Midpoint", C(IceRemembersCulture.AuroraStack),
            coordinates: (midpointCoord.Q, midpointCoord.R));

        SetSemanticProjected(s1, frosthavenCoord, takenSemantic, bias: new SemanticHexCoord(-1, -2));
        SetSemanticProjected(s2, ironholdCoord, takenSemantic, bias: new SemanticHexCoord(-2, 1));
        SetSemanticProjected(s3, driftwatchCoord, takenSemantic, bias: new SemanticHexCoord(2, -1));
        SetSemanticProjected(s4, wavecrestCoord, takenSemantic, bias: new SemanticHexCoord(1, 2));
        SetSemanticProjected(s5, midpointCoord, takenSemantic, bias: SemanticHexCoord.Origin);
        var allSettlements = new List<SeedEntity> { s1, s2, s3, s4, s5 };

        var factionA = CreateEntity(
            seed, K.Faction, "compact", "Northern Compact", C(IceRemembersCulture.AuroraStack));
        var factionB = CreateEntity(
            seed, K.Faction, "order", "Southern Order", C(IceRemembersCulture.Nightshelf));

        var factionACoord = ChooseNearbyFreeCell(grid, takenPhysical, Centroid(frosthavenCoord, ironholdCoord, midpointCoord), searchRadius: 8)
            ?? throw new InvalidOperationException("Could not place Northern Compact near its settlements.");
        SetPhysical(factionA, factionACoord, takenPhysical);
        var factionBCoord = ChooseNearbyFreeCell(grid, takenPhysical, Centroid(driftwatchCoord, wavecrestCoord), searchRadius: 8)
            ?? throw new InvalidOperationException("Could not place Southern Order near its settlements.");
        SetPhysical(factionB, factionBCoord, takenPhysical);
        SetSemanticProjected(factionA, factionACoord, takenSemantic, bias: new SemanticHexCoord(-3, 3));
        SetSemanticProjected(factionB, factionBCoord, takenSemantic, bias: new SemanticHexCoord(3, -3));

        // Faction A controls northern settlements
        CreateRelationship(seed, "controlled_by", RelationshipPolarity.Neutral, s1.SeedEntityId, factionA.SeedEntityId, 1f);
        CreateRelationship(seed, "controlled_by", RelationshipPolarity.Neutral, s2.SeedEntityId, factionA.SeedEntityId, 1f);
        CreateRelationship(seed, "controlled_by", RelationshipPolarity.Neutral, s5.SeedEntityId, factionA.SeedEntityId, 0.8f);

        // Faction B controls southern settlements
        CreateRelationship(seed, "controlled_by", RelationshipPolarity.Neutral, s3.SeedEntityId, factionB.SeedEntityId, 1f);
        CreateRelationship(seed, "controlled_by", RelationshipPolarity.Neutral, s4.SeedEntityId, factionB.SeedEntityId, 1f);

        CreateSupportingSettlements(
            seed,
            grid,
            coverageMap,
            regions,
            random,
            takenPhysical,
            takenSemantic,
            allSettlements,
            factionA,
            factionB);

        var settlementsByCoord = allSettlements.ToDictionary(
            settlement => settlement.Coordinates,
            settlement => settlement.SeedEntityId);
        var npcHostSettlements = allSettlements
            .OrderBy(_ => random.Next())
            .Take(Math.Min(5, allSettlements.Count))
            .ToArray();

        // --- NPCs (random walkable hex, excluding already-taken positions) ---
        var npcDefs = new List<(string Subtype, string Name, string Culture, int Faction, float Loyalty)>
        {
            ("warden", "Captain Greyfeather", C(IceRemembersCulture.AuroraStack), factionA.SeedEntityId, 0.9f),
            ("broker", "Trader Shellback", C(IceRemembersCulture.Nightshelf), factionB.SeedEntityId, 0.7f),
            ("witness", "Elder Deepcurrent", C(IceRemembersCulture.AuroraStack), factionA.SeedEntityId, 0.8f),
        };
        npcDefs.AddRange(CreateSupportingPenguinPlans(random, factionA, factionB));

        // NPCs get distinct semantic positions so factions have to pick among
        // targets and move between them on the plane. NPCs themselves don't move
        // semantically (their primary plane is physical), so these anchors stay put.
        var npcSemanticSeeds = new[]
        {
            new SemanticHexCoord(8, 0),
            new SemanticHexCoord(0, 8),
            new SemanticHexCoord(-8, -4),
        };

        var npcIndex = 0;
        foreach (var (subtype, name, culture, factionId, loyalty) in npcDefs)
        {
            // On the expanded map, fully random agency placement collapses the
            // local process graph: penguins stop interacting with settlements and
            // factions because they spawn in unrelated wilderness. Seed them in
            // the orbit of named places so the initial world remains legible and
            // generator preconditions can actually emerge.
            var hostSettlement = npcHostSettlements[npcIndex % npcHostSettlements.Length];
            var hostCoord = hostSettlement.Coordinates;
            var nearbyHexes = HexCoord.CoordsInRadius(hostCoord, radius: 2)
                .Where(coord => grid.ContainsCoord(coord))
                .Where(coord => HexCoord.Distance(hostCoord, coord) is > 0 and <= 2)
                .Select(coord => grid.GetCell(coord))
                .Where(cell => cell is not null && !float.IsPositiveInfinity(cell.MovementCost))
                .Select(cell => cell!.Coord)
                .Where(coord => !takenPhysical.Contains(coord))
                .OrderBy(coord => HexCoord.Distance(hostCoord, coord))
                .ToList();

            var availableHexes = nearbyHexes.Count > 0
                ? nearbyHexes
                : grid.AllCells
                    .Where(c => !float.IsPositiveInfinity(c.MovementCost))
                    .Select(c => c.Coord)
                    .Where(c => !takenPhysical.Contains(c))
                    .ToList();
            if (availableHexes.Count == 0)
            {
                throw new InvalidOperationException(
                    "No free walkable hexes remain for NPC placement; grid may be too small or too many fixed entities.");
            }

            var hex = availableHexes[random.Next(availableHexes.Count)];
            takenPhysical.Add(hex);

            var npc = CreateEntity(
                seed,
                K.Penguin, subtype, name, culture,
                coordinates: (hex.Q, hex.R));

            if (npcIndex < npcSemanticSeeds.Length)
            {
                SetSemantic(npc, FindNearestFreeSemantic(npcSemanticSeeds[npcIndex], takenSemantic), takenSemantic);
            }
            else
            {
                SetSemanticProjected(npc, hex, takenSemantic, RandomSemanticBias(random));
            }
            npcIndex++;

            // Loyalty to faction
            CreateRelationship(seed, "member_of", RelationshipPolarity.Neutral, npc.SeedEntityId, factionId, loyalty);

            // located_at only if spawning on a settlement hex
            if (settlementsByCoord.TryGetValue(hex, out var settlementId))
            {
                CreateRelationship(seed, "home_base", RelationshipPolarity.Neutral, npc.SeedEntityId, settlementId, 1f);
            }
        }

        // --- Artifacts ---
        // Physical hex (4, -12) sits in Frosthaven's wider orbit without overlapping
        // the stored_at story to read as "kept in Frosthaven's treasury one hex over"
        // without overlapping another entity.
        var artifactCoord = ChooseNearbyFreeCell(grid, takenPhysical, frosthavenCoord, searchRadius: 5)
            ?? throw new InvalidOperationException("Could not place Stormcleaver near Frosthaven.");
        var artifact = CreateEntity(
            seed,
            K.Artifact, "weapon", "Stormcleaver", C(IceRemembersCulture.AuroraStack),
            coordinates: (artifactCoord.Q, artifactCoord.R));
        takenPhysical.Add(artifactCoord);
        SetSemanticProjected(artifact, artifactCoord, takenSemantic, bias: new SemanticHexCoord(4, -1));

        // Artifact stored at Frosthaven (not held by a specific NPC since NPCs are randomly placed)
        CreateRelationship(seed, "anchored", RelationshipPolarity.Neutral, artifact.SeedEntityId, s1.SeedEntityId, 1f);

        CreateSupportingArtifacts(
            seed,
            grid,
            random,
            takenPhysical,
            takenSemantic,
            allSettlements);
    }

    private static void CreateSupportingSettlements(
        SeedAssembly seed,
        HexGrid grid,
        MapCoverageMap coverageMap,
        Dictionary<string, MapCoverage> regions,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic,
        List<SeedEntity> allSettlements,
        SeedEntity factionA,
        SeedEntity factionB)
    {
        var count = 2 + random.Next(2);
        var usedNames = allSettlements.Select(settlement => settlement.Name).ToHashSet(StringComparer.Ordinal);

        for (var i = 0; i < count; i++)
        {
            var coastal = i == 0 ? random.NextSingle() < 0.65f : random.NextSingle() < 0.35f;
            var culture = Pick(
                coastal ? CoastalCultures : InteriorCultures,
                random);
            var subtype = coastal
                ? Pick(CoastalSettlementSubtypes, random)
                : Pick(InteriorSettlementSubtypes, random);
            var coverageKey = coastal
                ? "coastlands"
                : Pick(InteriorCoverageKeys, random);
            var controlledBy = coverageKey == "coastlands" || culture == IceRemembersCulture.Nightshelf
                ? factionB
                : factionA;
            var coverage = regions[coverageKey];
            var coord = ChooseAnchor(
                grid,
                random,
                takenPhysical,
                minimumDistance: 4,
                cell => SupportingSettlementScore(grid, coverageMap, coverage, cell, coastal));
            var name = UniqueName(
                Pick(
                    coastal
                        ? CoastalSettlementNames
                        : InteriorSettlementNames,
                    random),
                usedNames);

            var settlement = CreateEntity(
                seed,
                K.Settlement,
                subtype,
                name,
                C(culture),
                coordinates: (coord.Q, coord.R));
            takenPhysical.Add(coord);
            SetSemanticProjected(settlement, coord, takenSemantic, RandomSemanticBias(random));
            allSettlements.Add(settlement);
            CreateRelationship(
                seed,
                "controlled_by",
                RelationshipPolarity.Neutral,
                settlement.SeedEntityId,
                controlledBy.SeedEntityId,
                0.65f);
        }
    }

    private static IEnumerable<(string Subtype, string Name, string Culture, int Faction, float Loyalty)>
        CreateSupportingPenguinPlans(Random random, SeedEntity factionA, SeedEntity factionB)
    {
        var count = 2 + random.Next(3);
        var names = new HashSet<string>(StringComparer.Ordinal)
        {
            "Captain Greyfeather",
            "Trader Shellback",
            "Elder Deepcurrent",
        };

        for (var i = 0; i < count; i++)
        {
            var faction = random.NextSingle() < 0.55f ? factionA : factionB;
            var culture = Pick(
                faction == factionA ? NorthernPenguinCultures : SouthernPenguinCultures,
                random);
            var name = UniqueName(
                $"{Pick(PenguinGivenNames, random)} {Pick(PenguinByNames, random)}",
                names);

            yield return (
                Pick(PenguinSubtypes, random),
                name,
                C(culture),
                faction.SeedEntityId,
                0.45f + ((float)random.NextDouble() * 0.45f));
        }
    }

    private static void CreateSupportingArtifacts(
        SeedAssembly seed,
        HexGrid grid,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic,
        List<SeedEntity> allSettlements)
    {
        var count = 1 + random.Next(2);
        var usedNames = new HashSet<string>(StringComparer.Ordinal) { "Stormcleaver" };

        for (var i = 0; i < count; i++)
        {
            var host = allSettlements[random.Next(allSettlements.Count)];
            var hostCoord = host.Coordinates;
            var coord = ChooseNearbyFreeCell(grid, takenPhysical, hostCoord, searchRadius: 5)
                ?? throw new InvalidOperationException($"Could not place supporting artifact near {host.Name}.");
            var subtype = Pick(SupportingArtifactSubtypes, random);
            var name = UniqueName(
                Pick(SupportingArtifactNames, random),
                usedNames);

            var artifact = CreateEntity(
                seed,
                K.Artifact,
                subtype,
                name,
                host.Culture,
                coordinates: (coord.Q, coord.R));
            takenPhysical.Add(coord);
            SetSemanticProjected(artifact, coord, takenSemantic, RandomSemanticBias(random));
            CreateRelationship(seed, "anchored", RelationshipPolarity.Neutral, artifact.SeedEntityId, host.SeedEntityId, 0.7f);
        }
    }

    private static float SupportingSettlementScore(
        HexGrid grid,
        MapCoverageMap coverageMap,
        MapCoverage coverage,
        HexCell cell,
        bool coastal)
    {
        var affinity = CoverageAffinity(coverageMap, coverage, cell.Coord, 24f, 10f);
        var terrain = coastal
            ? TerrainScore(cell.Terrain, IceRemembersTerrain.Reef, 24f) +
              TerrainScore(cell.Terrain, IceRemembersTerrain.Shelf, 22f) +
              TerrainScore(cell.Terrain, IceRemembersTerrain.Tundra, 8f) +
              (CountAdjacentTerrains(grid, cell.Coord, IceRemembersTerrain.Ocean, IceRemembersTerrain.Reef, IceRemembersTerrain.Shelf) * 8f)
            : TerrainScore(cell.Terrain, IceRemembersTerrain.Tundra, 20f) +
              TerrainScore(cell.Terrain, IceRemembersTerrain.HotSprings, 14f) +
              TerrainScore(cell.Terrain, IceRemembersTerrain.Ice, 10f) +
              TerrainScore(cell.Terrain, IceRemembersTerrain.Wastes, 8f);

        return affinity + terrain + (cell.Habitability * 18f);
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

        for (var i = 2; ; i++)
        {
            var candidate = $"{baseName} {i}";
            if (usedNames.Add(candidate))
            {
                return candidate;
            }
        }
    }

    private static IceRemembersTerrain DetermineBaseTerrain(
        float oceanInfluence,
        float coldInfluence,
        float geothermalInfluence,
        float aridInfluence,
        float ruggedInfluence)
    {
        if (oceanInfluence >= 0.78f)
        {
            return IceRemembersTerrain.Ocean;
        }

        if (geothermalInfluence >= 0.76f && oceanInfluence < 0.62f)
        {
            return IceRemembersTerrain.HotSprings;
        }

        if (ruggedInfluence >= 0.74f && oceanInfluence < 0.65f)
        {
            return IceRemembersTerrain.Crags;
        }

        if (coldInfluence >= 0.72f && oceanInfluence < 0.66f)
        {
            return IceRemembersTerrain.Ice;
        }

        if (aridInfluence >= 0.72f && geothermalInfluence < 0.68f && oceanInfluence < 0.64f)
        {
            return IceRemembersTerrain.Wastes;
        }

        return IceRemembersTerrain.Tundra;
    }

    private static (float Elevation, float Fertility) TerrainProfileFor(
        IceRemembersTerrain terrain,
        float oceanInfluence,
        float coldInfluence,
        float geothermalInfluence,
        float ruggedInfluence,
        float fertilityNoise)
    {
        return terrain switch
        {
            IceRemembersTerrain.Ocean => (0f, 0f),
            IceRemembersTerrain.Reef => (5f, 0.45f + (fertilityNoise * 0.2f)),
            IceRemembersTerrain.Shelf => (10f, 0.3f + (fertilityNoise * 0.15f)),
            IceRemembersTerrain.HotSprings => (12f + (geothermalInfluence * 12f), 0.82f + (fertilityNoise * 0.14f)),
            IceRemembersTerrain.Crags => (180f + (ruggedInfluence * 220f), 0.08f + (fertilityNoise * 0.12f)),
            IceRemembersTerrain.Ice => (70f + (coldInfluence * 60f), 0.1f + (fertilityNoise * 0.12f)),
            IceRemembersTerrain.Wastes => (18f + (oceanInfluence * 10f), 0.12f + (fertilityNoise * 0.16f)),
            _ => (18f + ((1f - oceanInfluence) * 8f), 0.48f + (fertilityNoise * 0.26f)),
        };
    }

    private static List<HexCoord> PickSeedCoords(
        HexGrid grid,
        Random random,
        int count,
        int minDistance,
        Func<HexCoord, bool> predicate)
    {
        var candidates = grid.AllCells
            .Select(cell => cell.Coord)
            .Where(predicate)
            .OrderBy(_ => random.Next())
            .ToList();

        var chosen = new List<HexCoord>();
        foreach (var candidate in candidates)
        {
            if (chosen.All(existing => HexCoord.Distance(existing, candidate) >= minDistance))
            {
                chosen.Add(candidate);
            }

            if (chosen.Count == count)
            {
                break;
            }
        }

        if (chosen.Count == 0)
        {
            chosen.Add(new HexCoord(0, 0));
        }

        return chosen;
    }

    private static float ComputeInfluence(HexCoord coord, List<HexCoord> seeds, float radius)
    {
        if (seeds.Count == 0 || radius <= 0f)
        {
            return 0f;
        }

        var strongest = 0f;
        foreach (var seed in seeds)
        {
            var influence = 1f - (HexCoord.Distance(coord, seed) / radius);
            if (influence > strongest)
            {
                strongest = influence;
            }
        }

        return Math.Clamp(strongest, 0f, 1f);
    }

    private static float CellNoise(HexCoord coord, int salt)
    {
        unchecked
        {
            var hash = salt;
            hash = (hash * 397) ^ coord.Q;
            hash = (hash * 397) ^ coord.R;
            hash ^= (hash >> 13);
            hash *= 16777619;
            return ((hash & 0x7fffffff) / (float)int.MaxValue);
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
            .Where(cell => !float.IsPositiveInfinity(cell.MovementCost))
            .Where(cell => !takenPhysical.Contains(cell.Coord))
            .Where(cell => takenPhysical.All(existing => HexCoord.Distance(existing, cell.Coord) >= minimumDistance))
            .Select(cell => (Cell: cell, Score: scorer(cell)))
            .OrderByDescending(candidate => candidate.Score)
            .Take(24)
            .ToList();

        if (candidates.Count == 0)
        {
            candidates = grid.AllCells
                .Where(cell => !float.IsPositiveInfinity(cell.MovementCost))
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
            + (cell.Habitability * 30f)
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
            + (CountAdjacentTerrains(grid, cell.Coord, IceRemembersTerrain.Crags, IceRemembersTerrain.Wastes) * 5f)
            + (cell.Habitability * 18f);
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
            + (cell.Habitability * 28f)
            + (20f - HexCoord.Distance(cell.Coord, new HexCoord(0, 0)));
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

    private static float TerrainScore(TerrainDefinition actual, IceRemembersTerrain desired, float value) =>
        string.Equals(actual.StableName, desired.ToDomainName(), StringComparison.Ordinal) ? value : 0f;

    private static bool IsTerrain(HexCell cell, IceRemembersTerrain terrain) =>
        string.Equals(cell.Terrain.StableName, terrain.ToDomainName(), StringComparison.Ordinal);

    private static HashSet<string> TerrainSet(params IceRemembersTerrain[] terrains) =>
        terrains.Select(terrain => terrain.ToDomainName()).ToHashSet(StringComparer.Ordinal);

    private static int CountTerrainsInRadius(HexGrid grid, HexCoord center, int radius, params IceRemembersTerrain[] terrains)
    {
        var set = TerrainSet(terrains);
        return HexCoord.CoordsInRadius(center, radius)
            .Where(grid.ContainsCoord)
            .Select(grid.GetCell)
            .Where(cell => cell is not null && set.Contains(cell.Terrain.StableName))
            .Count();
    }

    private static int CountAdjacentTerrains(HexGrid grid, HexCoord center, params IceRemembersTerrain[] terrains)
    {
        var set = TerrainSet(terrains);
        return center.Neighbors()
            .Where(grid.ContainsCoord)
            .Select(grid.GetCell)
            .Where(cell => cell is not null && set.Contains(cell.Terrain.StableName))
            .Count();
    }

    private static HexCoord Centroid(params HexCoord[] coords)
    {
        var q = (int)Math.Round(coords.Average(coord => coord.Q));
        var r = (int)Math.Round(coords.Average(coord => coord.R));
        return new HexCoord(q, r);
    }

    private static HexCoord? ChooseNearbyFreeCell(HexGrid grid, HashSet<HexCoord> taken, HexCoord origin, int searchRadius)
    {
        for (var radius = 0; radius <= searchRadius; radius++)
        {
            var candidates = HexCoord.CoordsInRadius(origin, radius)
                .Where(coord => grid.ContainsCoord(coord))
                .Where(coord => !taken.Contains(coord))
                .Select(grid.GetCell)
                .Where(cell => cell is not null && !float.IsPositiveInfinity(cell.MovementCost))
                .OrderByDescending(cell => cell!.Habitability)
                .Select(cell => cell!.Coord)
                .ToList();

            if (candidates.Count > 0)
            {
                return candidates[0];
            }
        }

        return null;
    }

    private static void SetPhysical(SeedEntity entity, HexCoord coord, HashSet<HexCoord> taken)
    {
        ReservePhysical(coord, taken, entity.Name);
        entity.Coordinates = coord;
    }

    private static void ReservePhysical(HexCoord coord, HashSet<HexCoord> taken, string label)
    {
        if (!taken.Add(coord))
        {
            throw new InvalidOperationException(
                $"Physical position ({coord.Q},{coord.R}) already taken while placing {label}; duplicate starting coords.");
        }
    }

    private static void SetSemantic(SeedEntity entity, SemanticHexCoord coord, HashSet<SemanticHexCoord> taken)
    {
        if (!taken.Add(coord))
        {
            throw new InvalidOperationException(
                $"Semantic position ({coord.Q},{coord.R}) already taken; duplicate starting coords.");
        }
        entity.SemanticPosition = coord;
    }

    private static void SetSemanticProjected(
        SeedEntity entity,
        HexCoord physicalCoord,
        HashSet<SemanticHexCoord> taken,
        SemanticHexCoord bias)
    {
        var projected = new SemanticHexCoord(
            (int)Math.Round(physicalCoord.Q / 2f) + bias.Q,
            (int)Math.Round(physicalCoord.R / 2f) + bias.R);

        var resolved = FindNearestFreeSemantic(projected, taken);
        SetSemantic(entity, resolved, taken);
    }

    private static SemanticHexCoord FindNearestFreeSemantic(
        SemanticHexCoord origin,
        HashSet<SemanticHexCoord> taken)
    {
        if (!taken.Contains(origin))
        {
            return origin;
        }

        for (var radius = 1; radius <= 24; radius++)
        {
            foreach (var coord in SemanticHexCoord.CoordsInRadius(origin, radius)
                         .Where(candidate => SemanticHexCoord.Distance(candidate, origin) == radius))
            {
                if (!taken.Contains(coord))
                {
                    return coord;
                }
            }
        }

        throw new InvalidOperationException("Could not find a free semantic coordinate for seed placement.");
    }

    private static MapCoverage CreateSeedCoverage(
        HexGrid grid,
        MapCoverageMap coverageMap,
        SeedAssembly assembly,
        MapCoverage seed,
        HexCoord origin,
        int desiredCellCount,
        int minimumCellCount,
        IReadOnlySet<string> preferredTerrains)
    {
        var result = coverageMap.CreateCoverage(
            grid,
            new MapCoveragePlacementRequest
            {
                Name = seed.Name,
                OwnerEntityId = seed.OwnerEntityId,
                Origin = origin,
                Mode = CoveragePlacementMode.CreateNew,
                DesiredCellCount = desiredCellCount,
                MinimumCellCount = minimumCellCount,
                PreferredTerrains = preferredTerrains,
                RespectTerrainBoundaries = false,
            });

        assembly.Entity(seed.OwnerEntityId).Coordinates = result.Anchor;
        return result.Coverage;
    }
}

internal sealed record IceRemembersStartupSeedResult(
    WorldGenerationSeed Seed,
    DomainStartupSeed StartupSeed);

internal sealed class SeedAssembly
{
    public HexGrid Grid { get; }

    public MapCoverageMap CoverageMap { get; }

    private readonly List<SeedEntity> _entities = [];
    private readonly List<DomainSeedRelationship> _relationships = [];

    public SeedAssembly(HexGrid grid, MapCoverageMap coverageMap)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(coverageMap);
        Grid = grid;
        CoverageMap = coverageMap;
    }

    public IReadOnlyList<SeedEntity> Entities => _entities;

    public IReadOnlyList<DomainSeedRelationship> Relationships => _relationships;

    public SeedEntity CreateEntity(
        EntityKind kind,
        string subtype,
        string name,
        string culture,
        (int Q, int R)? coordinates = null)
    {
        var entity = new SeedEntity(
            _entities.Count,
            kind,
            subtype,
            name,
            culture,
            coordinates is { } coord ? new HexCoord(coord.Q, coord.R) : new HexCoord(0, 0));
        _entities.Add(entity);
        return entity;
    }

    public SeedEntity Entity(int seedEntityId)
    {
        if (seedEntityId < 0 || seedEntityId >= _entities.Count)
        {
            throw new InvalidOperationException($"Unknown seed entity {seedEntityId}.");
        }

        return _entities[seedEntityId];
    }

    public void CreateRelationship(
        string kind,
        RelationshipPolarity polarity,
        int sourceEntityId,
        int targetEntityId,
        float strength,
        IReadOnlyList<string>? facts = null)
    {
        _ = Entity(sourceEntityId);
        _ = Entity(targetEntityId);
        _relationships.Add(new DomainSeedRelationship(
            kind,
            polarity,
            sourceEntityId,
            targetEntityId,
            Math.Clamp(strength, 0f, 1f),
            facts));
    }

    public DomainStartupSeed ToStartupSeed()
    {
        var entities = _entities
            .OrderBy(entity => entity.SeedEntityId)
            .Select(entity => new DomainSeedEntity(
                entity.SeedEntityId,
                entity.Kind.Value,
                entity.Subtype,
                entity.Culture,
                entity.Coordinates.Q,
                entity.Coordinates.R,
                entity.SemanticPosition.Q,
                entity.SemanticPosition.R,
                SemanticS(entity.SemanticPosition),
                entity.Name))
            .ToArray();

        var cells = Grid.AllCells
            .OrderBy(cell => cell.Coord.Q)
            .ThenBy(cell => cell.Coord.R)
            .Select(cell => new DomainSeedCell(
                cell.Coord.Q,
                cell.Coord.R,
                cell.Terrain.StableName,
                CoverageMap.GetCoverageForCell(cell.Coord)?.OwnerEntityId))
            .ToArray();

        var resources = Grid.AllCells
            .OrderBy(cell => cell.Coord.Q)
            .ThenBy(cell => cell.Coord.R)
            .SelectMany(cell => cell.AmbientResources
                .OrderBy(resource => resource.Key, StringComparer.Ordinal)
                .Select(resource => new DomainSeedResource(
                    resource.Key,
                    cell.Coord.Q,
                    cell.Coord.R,
                    resource.Value)))
            .ToArray();

        var relationships = _relationships
            .OrderBy(relationship => relationship.SourceSeedEntityId)
            .ThenBy(relationship => relationship.RelationshipKind, StringComparer.Ordinal)
            .ThenBy(relationship => relationship.TargetSeedEntityId)
            .ThenBy(relationship => relationship.Strength)
            .ToArray();

        return new DomainStartupSeed(
            Grid.Radius,
            entities,
            cells,
            resources,
            relationships,
            Attributes: []);
    }

    private static int SemanticS(SemanticHexCoord coord) => -coord.Q - coord.R;
}

internal sealed class SeedEntity(
    int seedEntityId,
    EntityKind kind,
    string subtype,
    string name,
    string culture,
    HexCoord coordinates)
{
    public int SeedEntityId { get; } = seedEntityId;

    public EntityKind Kind { get; } = kind;

    public string Subtype { get; } = subtype;

    public string Name { get; } = name;

    public string Culture { get; } = culture;

    public HexCoord Coordinates { get; set; } = coordinates;

    public SemanticHexCoord SemanticPosition { get; set; } = SemanticHexCoord.Origin;
}
