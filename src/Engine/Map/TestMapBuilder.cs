namespace Engine.Map;

using Engine.Graph;
using Engine.Kinds;

/// <summary>
/// Builds a fixed test map with varied terrain, regions, and starting entities.
/// This is the Phase 1 "one fixed world state" -- deterministic, same every run.
/// Parameters can be adjusted for different test scenarios.
/// </summary>
public sealed class TestMapBuilder
{
    /// <summary>
    /// Build the default test world: a radius-5 hex grid with 3 regions,
    /// 2 factions, 5 settlements, 3 NPCs, 1 artifact, and relationships.
    /// NPCs spawn at random walkable hex locations.
    /// </summary>
    public static TestWorld BuildDefault(int? seed = null)
    {
        var grid = new HexGrid(5);
        var regionMap = new RegionMap();
        var graph = new WorldGraph();
        var random = seed.HasValue ? new Random(seed.Value) : new Random();

        // --- Terrain setup ---
        PaintTerrain(grid, random);

        // --- Region setup ---
        var regions = CreateRegions(grid, regionMap, graph);

        // --- Entity setup ---
        CreateEntities(graph, grid, regions, random);

        return new TestWorld(grid, regionMap, graph);
    }

    private static void PaintTerrain(HexGrid grid, Random random)
    {
        foreach (var cell in grid.AllCells)
        {
            var (q, r) = (cell.Coord.Q, cell.Coord.R);

            // Mountains along the diagonal (natural region boundary)
            if (q + r is >= 2 and <= 3 && q >= 0)
            {
                SetTerrainAndAmbientResources(grid, cell.Coord, TerrainType.Mountain,
                    elevation: 300f, fertility: 0.1f, random);
                continue;
            }

            // Water in the far south
            if (r >= 4)
            {
                SetTerrainAndAmbientResources(grid, cell.Coord, TerrainType.Water,
                    elevation: 0f, fertility: 0f, random);
                continue;
            }

            // Coastal near the water
            if (r == 3 && q <= 0)
            {
                SetTerrainAndAmbientResources(grid, cell.Coord, TerrainType.Coastal,
                    elevation: 5f, fertility: 0.6f, random);
                continue;
            }

            // Forest in the northwest
            if (q <= -2 && r <= 0)
            {
                SetTerrainAndAmbientResources(grid, cell.Coord, TerrainType.Forest,
                    elevation: 50f, fertility: 0.4f, random);
                continue;
            }

            // Tundra in the far north
            if (r <= -3)
            {
                SetTerrainAndAmbientResources(grid, cell.Coord, TerrainType.Tundra,
                    elevation: 100f, fertility: 0.2f, random);
                continue;
            }

            // Everything else is plains with good fertility
            SetTerrainAndAmbientResources(grid, cell.Coord, TerrainType.Plains,
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

    private static Dictionary<string, Region> CreateRegions(
        HexGrid grid, RegionMap regionMap, WorldGraph graph)
    {
        var northlands = new Region("Northlands",
            graph.CreateEntity(EntityKind.Region, "generic_region", "Northlands", "none", tick: 0).Id);
        var heartlands = new Region("Heartlands",
            graph.CreateEntity(EntityKind.Region, "generic_region", "Heartlands", "none", tick: 0).Id);
        var coastlands = new Region("Coastlands",
            graph.CreateEntity(EntityKind.Region, "generic_region", "Coastlands", "none", tick: 0).Id);

        regionMap.RegisterRegion(northlands);
        regionMap.RegisterRegion(heartlands);
        regionMap.RegisterRegion(coastlands);

        foreach (var cell in grid.AllCells)
        {
            var (q, r) = (cell.Coord.Q, cell.Coord.R);

            if (r <= -1)
            {
                regionMap.AssignCell(cell.Coord, northlands);
            }
            else if (r >= 3 || (q <= -2 && r >= 1))
            {
                regionMap.AssignCell(cell.Coord, coastlands);
            }
            else
            {
                regionMap.AssignCell(cell.Coord, heartlands);
            }
        }

        // Create borders relationships between adjacent regions
        foreach (var (a, b) in regionMap.ComputeAdjacency())
        {
            graph.CreateRelationship("borders", RelationshipPolarity.Neutral, a, b, 1f, tick: 0);
        }

        return new Dictionary<string, Region>
        {
            ["northlands"] = northlands,
            ["heartlands"] = heartlands,
            ["coastlands"] = coastlands,
        };
    }

    private static void CreateEntities(WorldGraph graph, HexGrid grid,
        Dictionary<string, Region> regions, Random random)
    {
        // Every entity gets a distinct starting position on BOTH the physical hex
        // grid and the semantic (autonomy/resonance/tradition) plane. No two
        // entities overlap on either plane at tick 0.
        var takenPhysical = new HashSet<HexCoord>();
        var takenSemantic = new HashSet<SemanticHexCoord>();

        // --- Factions ---
        var factionA = graph.CreateEntity(
            EntityKind.Faction, "generic_faction", "Northern Compact", "culture_a", tick: 0);
        var factionB = graph.CreateEntity(
            EntityKind.Faction, "generic_faction", "Southern League", "culture_b", tick: 0);

        SetPhysical(factionA, new HexCoord(-2, -2), takenPhysical);
        SetPhysical(factionB, new HexCoord(3, 1), takenPhysical);
        // Opposite corners of the semantic plane so mobility verbs visibly chase
        // across autonomy × resonance × tradition space.
        SetSemantic(factionA, new SemanticHexCoord(-3, 3), takenSemantic);
        SetSemantic(factionB, new SemanticHexCoord(3, -3), takenSemantic);

        // --- Settlements (fixed physical positions — they define the geography) ---
        var s1 = graph.CreateEntity(
            EntityKind.Settlement, "village", "Frosthaven", "culture_a", tick: 0,
            coordinates: (0, -2));
        var s2 = graph.CreateEntity(
            EntityKind.Settlement, "town", "Ironhold", "culture_a", tick: 0,
            coordinates: (-1, 0));
        var s3 = graph.CreateEntity(
            EntityKind.Settlement, "village", "Driftwatch", "culture_b", tick: 0,
            coordinates: (1, 1));
        var s4 = graph.CreateEntity(
            EntityKind.Settlement, "village", "Wavecrest", "culture_b", tick: 0,
            coordinates: (-2, 2));
        var s5 = graph.CreateEntity(
            EntityKind.Settlement, "town", "Midpoint", "culture_a", tick: 0,
            coordinates: (0, 0));

        // Settlement physical coords are implicit from the coordinates above; record
        // them as taken so later entities don't collide.
        takenPhysical.Add(new HexCoord(0, -2));
        takenPhysical.Add(new HexCoord(-1, 0));
        takenPhysical.Add(new HexCoord(1, 1));
        takenPhysical.Add(new HexCoord(-2, 2));
        takenPhysical.Add(new HexCoord(0, 0));

        // Distinct semantic anchors per settlement so all five don't cluster at
        // origin on the semantic plane. Chosen for flavor: Midpoint at the
        // neutral center, Frosthaven on the tradition axis, etc.
        SetSemantic(s5, new SemanticHexCoord(0, 0), takenSemantic);
        SetSemantic(s1, new SemanticHexCoord(0, -2), takenSemantic);
        SetSemantic(s2, new SemanticHexCoord(-2, 0), takenSemantic);
        SetSemantic(s3, new SemanticHexCoord(1, -2), takenSemantic);
        SetSemantic(s4, new SemanticHexCoord(-2, 1), takenSemantic);

        // --- Regions: give each a representative physical hex (central-ish cell of
        //     its region) and a semantic anchor. Region.Coordinates defaults to
        //     (0,0) which collides with other entities; set distinct positions. ---
        SetPhysical(graph.GetEntity(regions["northlands"].EntityId)!, new HexCoord(0, -4), takenPhysical);
        SetPhysical(graph.GetEntity(regions["heartlands"].EntityId)!, new HexCoord(2, -1), takenPhysical);
        SetPhysical(graph.GetEntity(regions["coastlands"].EntityId)!, new HexCoord(-3, 4), takenPhysical);
        SetSemantic(graph.GetEntity(regions["northlands"].EntityId)!, new SemanticHexCoord(1, 1), takenSemantic);
        SetSemantic(graph.GetEntity(regions["heartlands"].EntityId)!, new SemanticHexCoord(-1, 2), takenSemantic);
        SetSemantic(graph.GetEntity(regions["coastlands"].EntityId)!, new SemanticHexCoord(2, 2), takenSemantic);

        // Build settlement coordinate lookup for NPC located_at checks
        var settlementsByCoord = new Dictionary<HexCoord, EntityId>
        {
            [new HexCoord(0, -2)] = s1.Id,
            [new HexCoord(-1, 0)] = s2.Id,
            [new HexCoord(1, 1)] = s3.Id,
            [new HexCoord(-2, 2)] = s4.Id,
            [new HexCoord(0, 0)] = s5.Id,
        };

        // Faction A controls northern settlements
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, s1.Id, factionA.Id, 1f, 0);
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, s2.Id, factionA.Id, 1f, 0);
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, s5.Id, factionA.Id, 0.8f, 0);

        // Faction B controls southern settlements
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, s3.Id, factionB.Id, 1f, 0);
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, s4.Id, factionB.Id, 1f, 0);

        // Settlement -> Region (contains)
        graph.CreateRelationship("contains", RelationshipPolarity.Neutral,
            regions["northlands"].EntityId, s1.Id, 1f, 0);
        graph.CreateRelationship("contains", RelationshipPolarity.Neutral,
            regions["heartlands"].EntityId, s2.Id, 1f, 0);
        graph.CreateRelationship("contains", RelationshipPolarity.Neutral,
            regions["heartlands"].EntityId, s3.Id, 1f, 0);
        graph.CreateRelationship("contains", RelationshipPolarity.Neutral,
            regions["coastlands"].EntityId, s4.Id, 1f, 0);
        graph.CreateRelationship("contains", RelationshipPolarity.Neutral,
            regions["heartlands"].EntityId, s5.Id, 1f, 0);

        // --- NPCs (random walkable hex, excluding already-taken positions) ---
        var npcDefs = new (string Subtype, string Name, string Culture, EntityId Faction, float Loyalty)[]
        {
            ("warrior", "Captain Greyfeather", "culture_a", factionA.Id, 0.9f),
            ("merchant", "Trader Shellback", "culture_b", factionB.Id, 0.7f),
            ("scholar", "Elder Deepcurrent", "culture_a", factionA.Id, 0.8f),
        };

        // NPCs get distinct semantic positions so factions have to pick among
        // targets and move between them on the plane. NPCs themselves don't move
        // semantically (their primary plane is physical), so these anchors stay put.
        var npcSemanticSeeds = new[]
        {
            new SemanticHexCoord(2, 0),
            new SemanticHexCoord(0, 2),
            new SemanticHexCoord(-2, -1),
        };

        var npcIndex = 0;
        foreach (var (subtype, name, culture, factionId, loyalty) in npcDefs)
        {
            // Pick from walkable hexes minus anything we've already placed, so no two
            // entities start on the same physical hex.
            var availableHexes = grid.AllCells
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
                EntityKind.Npc, subtype, name, culture, tick: 0,
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
        // Physical hex (1, -3) is adjacent to Frosthaven (0, -2) — close enough for
        // the stored_at story to read as "kept in Frosthaven's treasury one hex over"
        // without overlapping another entity.
        var artifact = graph.CreateEntity(
            EntityKind.Artifact, "weapon", "Stormcleaver", "culture_a", tick: 0,
            coordinates: (1, -3));
        takenPhysical.Add(new HexCoord(1, -3));
        SetSemantic(artifact, new SemanticHexCoord(3, 0), takenSemantic);

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
}

/// <summary>
/// The complete test world state: grid, region mapping, and entity graph.
/// </summary>
public sealed class TestWorld
{
    public HexGrid Grid { get; }
    public RegionMap RegionMap { get; }
    public WorldGraph Graph { get; }

    public TestWorld(HexGrid grid, RegionMap regionMap, WorldGraph graph)
    {
        Grid = grid;
        RegionMap = regionMap;
        Graph = graph;
    }
}
