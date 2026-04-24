namespace Engine.Map;

using Engine.Domain.IceRemembers;
using Engine.Graph;
using Engine.Kinds;
using K = Engine.Domain.IceRemembers.IceRemembersKinds;

/// <summary>
/// Builds a fixed test map with varied terrain, regions, and starting entities.
/// This is the Phase 1 "one fixed world state" -- deterministic, same every run.
/// Parameters can be adjusted for different test scenarios.
/// </summary>
public sealed class TestMapBuilder
{
    private static string C(IceRemembersCulture culture) => culture.ToDomainName();

    /// <summary>
    /// Build the default test world: a radius-20 hex grid with 3 regions,
    /// 2 factions, 5 settlements, 3 NPCs, 1 artifact, and relationships.
    /// NPCs spawn at random walkable hex locations.
    /// </summary>
    public static TestWorld BuildDefault(int? seed = null)
    {
        var random = seed.HasValue ? new Random(seed.Value) : new Random();
        var world = BuildEmpty(random);
        Populate(world, random);
        return world;
    }

    /// <summary>
    /// Build an empty world (grid + terrain + empty graph) so callers can
    /// subscribe systems to <see cref="WorldGraph.EntityCreated"/> before any
    /// entity exists. Follow with <see cref="Populate"/>.
    /// </summary>
    public static TestWorld BuildEmpty(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        var grid = new HexGrid(20);
        var coverageMap = new MapCoverageMap();
        var graph = new WorldGraph();
        PaintTerrain(grid, random);
        return new TestWorld(grid, coverageMap, graph);
    }

    /// <summary>
    /// Populate the world with regions, factions, settlements, NPCs, and the
    /// starter artifact. Every <c>graph.CreateEntity</c> invocation fires
    /// <see cref="WorldGraph.EntityCreated"/>, so any subscribers wired on the
    /// empty graph will initialize these entities as they are created.
    /// </summary>
    public static void Populate(TestWorld world, Random random)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(random);
        var regions = CreateRegionEntities(world.Graph);
        CreateEntities(world.Graph, world.Grid, world.CoverageMap, regions, random);
    }

    private static void PaintTerrain(HexGrid grid, Random random)
    {
        foreach (var cell in grid.AllCells)
        {
            var (q, r) = (cell.Coord.Q, cell.Coord.R);

            // Crags along the diagonal (natural region boundary)
            if (q + r is >= 8 and <= 12 && q >= 0)
            {
                SetTerrainAndAmbientResources(grid, cell.Coord, TerrainType.Crags,
                    elevation: 300f, fertility: 0.1f, random);
                continue;
            }

            // Deep ocean in the far south
            if (r >= 17)
            {
                SetTerrainAndAmbientResources(grid, cell.Coord, TerrainType.Ocean,
                    elevation: 0f, fertility: 0f, random);
                continue;
            }

            // Reef belt where the sea starts to break up.
            if (r >= 13 && q <= 2)
            {
                SetTerrainAndAmbientResources(grid, cell.Coord, TerrainType.Reef,
                    elevation: 5f, fertility: 0.6f, random);
                continue;
            }

            // Shelves where coast and ice grind together.
            if (r >= 10 && q <= -4)
            {
                SetTerrainAndAmbientResources(grid, cell.Coord, TerrainType.Shelf,
                    elevation: 10f, fertility: 0.35f, random);
                continue;
            }

            // Ancient ice in the far north.
            if (r <= -12)
            {
                SetTerrainAndAmbientResources(grid, cell.Coord, TerrainType.Ice,
                    elevation: 100f, fertility: 0.15f, random);
                continue;
            }

            // Wastes in the northwest interior.
            if (q <= -8 && r <= 2)
            {
                SetTerrainAndAmbientResources(grid, cell.Coord, TerrainType.Wastes,
                    elevation: 30f, fertility: 0.25f, random);
                continue;
            }

            // Small warm pockets where habitation can really gather.
            if (q is >= -2 and <= 2 && r is >= 4 and <= 8)
            {
                SetTerrainAndAmbientResources(grid, cell.Coord, TerrainType.HotSprings,
                    elevation: 15f, fertility: 0.9f, random);
                continue;
            }

            // Everything else is workable tundra.
            SetTerrainAndAmbientResources(grid, cell.Coord, TerrainType.Tundra,
                elevation: 20f, fertility: 0.7f, random);
        }
    }

    private static void SetTerrainAndAmbientResources(
        HexGrid grid,
        HexCoord coord,
        TerrainType terrain,
        float elevation,
        float fertility,
        Random random)
    {
        grid.SetCell(coord, terrain, elevation, fertility);
        var cell = grid.GetCell(coord)!;
        TerrainAmbientResources.Seed(cell, random);
    }

    private static Dictionary<string, MapCoverage> CreateRegionEntities(WorldGraph graph)
    {
        var northlands = new MapCoverage("Northlands",
            graph.CreateEntity(K.Region, "generic_region", "Northlands", "none", tick: 0).Id);
        var heartlands = new MapCoverage("Heartlands",
            graph.CreateEntity(K.Region, "generic_region", "Heartlands", "none", tick: 0).Id);
        var coastlands = new MapCoverage("Coastlands",
            graph.CreateEntity(K.Region, "generic_region", "Coastlands", "none", tick: 0).Id);

        return new Dictionary<string, MapCoverage>
        {
            ["northlands"] = northlands,
            ["heartlands"] = heartlands,
            ["coastlands"] = coastlands,
        };
    }

    private static void CreateEntities(WorldGraph graph, HexGrid grid, MapCoverageMap coverageMap,
        Dictionary<string, MapCoverage> regions, Random random)
    {
        // Every entity gets a distinct starting position on BOTH the physical hex
        // grid and the semantic (autonomy/resonance/tradition) plane. No two
        // entities overlap on either plane at tick 0.
        var takenPhysical = new HashSet<HexCoord>();
        var takenSemantic = new HashSet<SemanticHexCoord>();

        // --- Factions ---
        var factionA = graph.CreateEntity(
            K.Faction, "compact", "Northern Compact", C(IceRemembersCulture.AuroraStack), tick: 0);
        var factionB = graph.CreateEntity(
            K.Faction, "order", "Southern Order", C(IceRemembersCulture.Nightshelf), tick: 0);

        SetPhysical(factionA, new HexCoord(-8, -8), takenPhysical);
        SetPhysical(factionB, new HexCoord(12, 4), takenPhysical);
        // Opposite corners of the semantic plane so mobility verbs visibly chase
        // across autonomy × resonance × tradition space.
        SetSemantic(factionA, new SemanticHexCoord(-12, 12), takenSemantic);
        SetSemantic(factionB, new SemanticHexCoord(12, -12), takenSemantic);

        // --- Settlements (fixed physical positions — they define the geography) ---
        var s1 = graph.CreateEntity(
            K.Settlement, "rookery", "Frosthaven", C(IceRemembersCulture.AuroraStack), tick: 0,
            coordinates: (0, -8));
        var s2 = graph.CreateEntity(
            K.Settlement, "colony", "Ironhold", C(IceRemembersCulture.AuroraStack), tick: 0,
            coordinates: (-4, 0));
        var s3 = graph.CreateEntity(
            K.Settlement, "harbor", "Driftwatch", C(IceRemembersCulture.Nightshelf), tick: 0,
            coordinates: (4, 4));
        var s4 = graph.CreateEntity(
            K.Settlement, "reefhold", "Wavecrest", C(IceRemembersCulture.Nightshelf), tick: 0,
            coordinates: (-8, 8));
        var s5 = graph.CreateEntity(
            K.Settlement, "colony", "Midpoint", C(IceRemembersCulture.AuroraStack), tick: 0,
            coordinates: (0, 0));

        // Settlement physical coords are implicit from the coordinates above; record
        // them as taken so later entities don't collide.
        takenPhysical.Add(new HexCoord(0, -8));
        takenPhysical.Add(new HexCoord(-4, 0));
        takenPhysical.Add(new HexCoord(4, 4));
        takenPhysical.Add(new HexCoord(-8, 8));
        takenPhysical.Add(new HexCoord(0, 0));

        // Distinct semantic anchors per settlement so all five don't cluster at
        // origin on the semantic plane. Chosen for flavor: Midpoint at the
        // neutral center, Frosthaven on the tradition axis, etc.
        SetSemantic(s5, new SemanticHexCoord(0, 0), takenSemantic);
        SetSemantic(s1, new SemanticHexCoord(0, -8), takenSemantic);
        SetSemantic(s2, new SemanticHexCoord(-8, 0), takenSemantic);
        SetSemantic(s3, new SemanticHexCoord(4, -8), takenSemantic);
        SetSemantic(s4, new SemanticHexCoord(-8, 4), takenSemantic);

        // --- Region seeds: sparse named coverage, not full-map ownership. The
        //     region entity position is the seed/anchor for terrain-aware coverage
        //     growth, and the same coverage system is used later by generators. ---
        SetPhysical(graph.GetEntity(regions["northlands"].OwnerEntityId)!, new HexCoord(0, -10), takenPhysical);
        SetPhysical(graph.GetEntity(regions["heartlands"].OwnerEntityId)!, new HexCoord(0, 2), takenPhysical);
        SetPhysical(graph.GetEntity(regions["coastlands"].OwnerEntityId)!, new HexCoord(-4, 12), takenPhysical);
        SetSemantic(graph.GetEntity(regions["northlands"].OwnerEntityId)!, new SemanticHexCoord(4, 4), takenSemantic);
        SetSemantic(graph.GetEntity(regions["heartlands"].OwnerEntityId)!, new SemanticHexCoord(-4, 8), takenSemantic);
        SetSemantic(graph.GetEntity(regions["coastlands"].OwnerEntityId)!, new SemanticHexCoord(8, 8), takenSemantic);

        CreateSeedCoverage(grid, coverageMap, graph, regions["northlands"], new HexCoord(0, -10),
            desiredCellCount: 96, minimumCellCount: 72, preferredTerrains: new HashSet<TerrainType> { TerrainType.Ice, TerrainType.Tundra });
        CreateSeedCoverage(grid, coverageMap, graph, regions["heartlands"], new HexCoord(0, 2),
            desiredCellCount: 144, minimumCellCount: 110, preferredTerrains: new HashSet<TerrainType> { TerrainType.Tundra, TerrainType.HotSprings });
        CreateSeedCoverage(grid, coverageMap, graph, regions["coastlands"], new HexCoord(-4, 12),
            desiredCellCount: 88, minimumCellCount: 64, preferredTerrains: new HashSet<TerrainType> { TerrainType.Shelf, TerrainType.Reef, TerrainType.Ocean });

        // Build settlement coordinate lookup for NPC located_at checks
        var settlementsByCoord = new Dictionary<HexCoord, EntityId>
        {
            [new HexCoord(0, -8)] = s1.Id,
            [new HexCoord(-4, 0)] = s2.Id,
            [new HexCoord(4, 4)] = s3.Id,
            [new HexCoord(-8, 8)] = s4.Id,
            [new HexCoord(0, 0)] = s5.Id,
        };
        var npcHostSettlements = new[]
        {
            s1,
            s3,
            s5,
        };

        // Faction A controls northern settlements
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, s1.Id, factionA.Id, 1f, 0);
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, s2.Id, factionA.Id, 1f, 0);
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, s5.Id, factionA.Id, 0.8f, 0);

        // Faction B controls southern settlements
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, s3.Id, factionB.Id, 1f, 0);
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, s4.Id, factionB.Id, 1f, 0);

        // --- NPCs (random walkable hex, excluding already-taken positions) ---
        var npcDefs = new (string Subtype, string Name, string Culture, EntityId Faction, float Loyalty)[]
        {
            ("warden", "Captain Greyfeather", C(IceRemembersCulture.AuroraStack), factionA.Id, 0.9f),
            ("broker", "Trader Shellback", C(IceRemembersCulture.Nightshelf), factionB.Id, 0.7f),
            ("witness", "Elder Deepcurrent", C(IceRemembersCulture.AuroraStack), factionA.Id, 0.8f),
        };

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
            var hostCoord = new HexCoord(hostSettlement.Coordinates.Q, hostSettlement.Coordinates.R);
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

            var npc = graph.CreateEntity(
                K.Penguin, subtype, name, culture, tick: 0,
                coordinates: (hex.Q, hex.R));

            if (npcIndex < npcSemanticSeeds.Length)
            {
                SetSemantic(npc, npcSemanticSeeds[npcIndex], takenSemantic);
            }
            npcIndex++;

            // Loyalty to faction
            graph.CreateRelationship("loyal_to", RelationshipPolarity.Positive, npc.Id, factionId, loyalty, 0);

            // located_at only if spawning on a settlement hex
            if (settlementsByCoord.TryGetValue(hex, out var settlementId))
            {
                graph.CreateRelationship("located_at", RelationshipPolarity.Neutral, npc.Id, settlementId, 1f, 0);
            }
        }

        // --- Artifacts ---
        // Physical hex (4, -12) sits in Frosthaven's wider orbit without overlapping
        // the stored_at story to read as "kept in Frosthaven's treasury one hex over"
        // without overlapping another entity.
        var artifact = graph.CreateEntity(
            K.Artifact, "weapon", "Stormcleaver", C(IceRemembersCulture.AuroraStack), tick: 0,
            coordinates: (4, -12));
        takenPhysical.Add(new HexCoord(4, -12));
        SetSemantic(artifact, new SemanticHexCoord(12, 0), takenSemantic);

        // Artifact stored at Frosthaven (not held by a specific NPC since NPCs are randomly placed)
        graph.CreateRelationship("stored_at", RelationshipPolarity.Neutral, artifact.Id, s1.Id, 1f, 0);
    }

    private static void SetPhysical(Engine.Graph.Entity entity, HexCoord coord, HashSet<HexCoord> taken)
    {
        if (!taken.Add(coord))
        {
            throw new InvalidOperationException(
                $"Physical position ({coord.Q},{coord.R}) already taken; duplicate starting coords.");
        }
        entity.Coordinates = (coord.Q, coord.R);
    }

    private static void SetSemantic(Engine.Graph.Entity entity, SemanticHexCoord coord, HashSet<SemanticHexCoord> taken)
    {
        if (!taken.Add(coord))
        {
            throw new InvalidOperationException(
                $"Semantic position ({coord.Q},{coord.R}) already taken; duplicate starting coords.");
        }
        entity.SemanticPosition = coord;
    }

    private static void CreateSeedCoverage(
        HexGrid grid,
        MapCoverageMap coverageMap,
        WorldGraph graph,
        MapCoverage seed,
        HexCoord origin,
        int desiredCellCount,
        int minimumCellCount,
        IReadOnlySet<TerrainType> preferredTerrains)
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
                AllowedTerrains = preferredTerrains,
                RespectTerrainBoundaries = true,
            });

        var regionEntity = graph.GetEntity(seed.OwnerEntityId)!;
        regionEntity.Coordinates = (result.Anchor.Q, result.Anchor.R);
    }
}

/// <summary>
/// The complete test world state: grid, region mapping, and entity graph.
/// </summary>
public sealed class TestWorld
{
    public HexGrid Grid { get; }
    public MapCoverageMap CoverageMap { get; }
    public WorldGraph Graph { get; }

    public TestWorld(HexGrid grid, MapCoverageMap coverageMap, WorldGraph graph)
    {
        Grid = grid;
        CoverageMap = coverageMap;
        Graph = graph;
    }
}
