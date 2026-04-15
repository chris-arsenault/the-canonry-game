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
        // --- Factions ---
        var factionA = graph.CreateEntity(
            EntityKind.Faction, "generic_faction", "Northern Compact", "culture_a", tick: 0);
        var factionB = graph.CreateEntity(
            EntityKind.Faction, "generic_faction", "Southern League", "culture_b", tick: 0);

        // --- Settlements (fixed positions — they define the geography) ---
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

        // --- Walkable hex pool for random NPC placement ---
        var walkableHexes = grid.AllCells
            .Where(c => !float.IsPositiveInfinity(c.MovementCost))
            .Select(c => c.Coord)
            .ToList();

        // --- NPCs (random hex positions) ---
        var npcDefs = new (string Subtype, string Name, string Culture, EntityId Faction, float Loyalty)[]
        {
            ("warrior", "Captain Greyfeather", "culture_a", factionA.Id, 0.9f),
            ("merchant", "Trader Shellback", "culture_b", factionB.Id, 0.7f),
            ("scholar", "Elder Deepcurrent", "culture_a", factionA.Id, 0.8f),
        };

        foreach (var (subtype, name, culture, factionId, loyalty) in npcDefs)
        {
            var hex = walkableHexes[random.Next(walkableHexes.Count)];
            var npc = graph.CreateEntity(
                EntityKind.Npc, subtype, name, culture, tick: 0,
                coordinates: (hex.Q, hex.R));

            // Loyalty to faction
            graph.CreateRelationship("loyal_to", RelationshipPolarity.Positive, npc.Id, factionId, loyalty, 0);

            // located_at only if spawning on a settlement hex
            if (settlementsByCoord.TryGetValue(hex, out var settlementId))
            {
                graph.CreateRelationship("located_at", RelationshipPolarity.Neutral, npc.Id, settlementId, 1f, 0);
            }
        }

        // --- Artifacts ---
        var artifact = graph.CreateEntity(
            EntityKind.Artifact, "weapon", "Stormcleaver", "culture_a", tick: 0,
            coordinates: (0, -2));

        // Artifact stored at Frosthaven (not held by a specific NPC since NPCs are randomly placed)
        graph.CreateRelationship("stored_at", RelationshipPolarity.Neutral, artifact.Id, s1.Id, 1f, 0);
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
