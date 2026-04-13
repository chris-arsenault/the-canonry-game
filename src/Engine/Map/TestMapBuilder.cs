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
    /// 2 nations, 5 settlements, 3 NPCs, 1 artifact, and relationships.
    /// </summary>
    public static TestWorld BuildDefault()
    {
        var grid = new HexGrid(5);
        var regionMap = new RegionMap();
        var graph = new WorldGraph();

        // --- Terrain setup ---
        PaintTerrain(grid);

        // --- Region setup ---
        var regions = CreateRegions(grid, regionMap, graph);

        // --- Entity setup ---
        CreateEntities(graph, regions);

        return new TestWorld(grid, regionMap, graph);
    }

    private static void PaintTerrain(HexGrid grid)
    {
        foreach (var cell in grid.AllCells)
        {
            var (q, r) = (cell.Coord.Q, cell.Coord.R);

            // Mountains along the diagonal (natural region boundary)
            if (q + r is >= 2 and <= 3 && q >= 0)
            {
                grid.SetCell(cell.Coord, TerrainType.Mountain, elevation: 300f, fertility: 0.1f);
                continue;
            }

            // Water in the far south
            if (r >= 4)
            {
                grid.SetCell(cell.Coord, TerrainType.Water, elevation: 0f, fertility: 0f);
                continue;
            }

            // Coastal near the water
            if (r == 3 && q <= 0)
            {
                grid.SetCell(cell.Coord, TerrainType.Coastal, elevation: 5f, fertility: 0.6f);
                continue;
            }

            // Forest in the northwest
            if (q <= -2 && r <= 0)
            {
                grid.SetCell(cell.Coord, TerrainType.Forest, elevation: 50f, fertility: 0.4f);
                continue;
            }

            // Tundra in the far north
            if (r <= -3)
            {
                grid.SetCell(cell.Coord, TerrainType.Tundra, elevation: 100f, fertility: 0.2f);
                continue;
            }

            // Everything else is plains with good fertility
            grid.SetCell(cell.Coord, TerrainType.Plains, elevation: 20f, fertility: 0.7f);
        }
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

    private static void CreateEntities(WorldGraph graph, Dictionary<string, Region> regions)
    {
        // --- Nations ---
        var nationA = graph.CreateEntity(
            EntityKind.Nation, "generic_nation", "Northern Compact", "culture_a", tick: 0);
        var nationB = graph.CreateEntity(
            EntityKind.Nation, "generic_nation", "Southern League", "culture_b", tick: 0);

        // --- Settlements ---
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

        // Nation A controls northern settlements
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, s1.Id, nationA.Id, 1f, 0);
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, s2.Id, nationA.Id, 1f, 0);
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, s5.Id, nationA.Id, 0.8f, 0);

        // Nation B controls southern settlements
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, s3.Id, nationB.Id, 1f, 0);
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, s4.Id, nationB.Id, 1f, 0);

        // Settlement -> Region (located_in)
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

        // --- NPCs ---
        var npc1 = graph.CreateEntity(
            EntityKind.Npc, "warrior", "Captain Greyfeather", "culture_a", tick: 0,
            coordinates: (0, -2));
        var npc2 = graph.CreateEntity(
            EntityKind.Npc, "merchant", "Trader Shellback", "culture_b", tick: 0,
            coordinates: (1, 1));
        var npc3 = graph.CreateEntity(
            EntityKind.Npc, "scholar", "Elder Deepcurrent", "culture_a", tick: 0,
            coordinates: (-1, 0));

        // NPC loyalties
        graph.CreateRelationship("loyal_to", RelationshipPolarity.Positive, npc1.Id, nationA.Id, 0.9f, 0);
        graph.CreateRelationship("loyal_to", RelationshipPolarity.Positive, npc2.Id, nationB.Id, 0.7f, 0);
        graph.CreateRelationship("loyal_to", RelationshipPolarity.Positive, npc3.Id, nationA.Id, 0.8f, 0);

        // NPC locations
        graph.CreateRelationship("located_at", RelationshipPolarity.Neutral, npc1.Id, s1.Id, 1f, 0);
        graph.CreateRelationship("located_at", RelationshipPolarity.Neutral, npc2.Id, s3.Id, 1f, 0);
        graph.CreateRelationship("located_at", RelationshipPolarity.Neutral, npc3.Id, s2.Id, 1f, 0);

        // --- Artifacts ---
        var artifact = graph.CreateEntity(
            EntityKind.Artifact, "weapon", "Stormcleaver", "culture_a", tick: 0,
            coordinates: (0, -2));

        // Artifact held by NPC
        graph.CreateRelationship("holds", RelationshipPolarity.Neutral, npc1.Id, artifact.Id, 1f, 0);
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
