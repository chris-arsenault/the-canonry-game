namespace Engine.Map;

using Engine.Domain.IceRemembers;
using Engine.Graph;
using Engine.Kinds;
using K = Engine.Domain.IceRemembers.IceRemembersKinds;

/// <summary>
/// Builds a seeded procedural test map with named starter regions and anchors.
/// The same seed produces the same world; different seeds should produce
/// meaningfully different terrain and starting layouts.
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

        var maritimeTransforms = new List<(HexCoord Coord, TerrainType Terrain)>();
        foreach (var cell in grid.AllCells)
        {
            if (cell.Terrain == TerrainType.Ocean)
            {
                continue;
            }

            var adjacentOcean = CountAdjacentTerrains(grid, cell.Coord, TerrainType.Ocean);
            if (adjacentOcean == 0)
            {
                continue;
            }

            if (coldness[cell.Coord] >= 0.58f || (adjacentOcean >= 3 && coldness[cell.Coord] >= 0.44f))
            {
                maritimeTransforms.Add((cell.Coord, TerrainType.Shelf));
                continue;
            }

            if (cell.Terrain != TerrainType.Crags &&
                cell.Terrain != TerrainType.HotSprings &&
                (adjacentOcean >= 2 || CellNoise(cell.Coord, maritimeSalt) >= 0.66f))
            {
                maritimeTransforms.Add((cell.Coord, TerrainType.Reef));
            }
        }

        foreach (var (coord, terrain) in maritimeTransforms)
        {
            var (elevation, fertility) = terrain switch
            {
                TerrainType.Shelf => (10f, 0.35f + (CellNoise(coord, fertilitySalt ^ 17) * 0.1f)),
                TerrainType.Reef => (5f, 0.45f + (CellNoise(coord, fertilitySalt ^ 29) * 0.2f)),
                _ => (20f, 0.5f),
            };
            SetTerrainAndAmbientResources(grid, coord, terrain, elevation, fertility, random);
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

    private static void CreateEntities(
        WorldGraph graph,
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

        SetPhysical(graph.GetEntity(regions["northlands"].OwnerEntityId)!, northlandsAnchor, takenPhysical);
        SetPhysical(graph.GetEntity(regions["heartlands"].OwnerEntityId)!, heartlandsAnchor, takenPhysical);
        SetPhysical(graph.GetEntity(regions["coastlands"].OwnerEntityId)!, coastlandsAnchor, takenPhysical);

        SetSemanticProjected(graph.GetEntity(regions["northlands"].OwnerEntityId)!, northlandsAnchor, takenSemantic, bias: new SemanticHexCoord(-2, 2));
        SetSemanticProjected(graph.GetEntity(regions["heartlands"].OwnerEntityId)!, heartlandsAnchor, takenSemantic, bias: new SemanticHexCoord(0, 0));
        SetSemanticProjected(graph.GetEntity(regions["coastlands"].OwnerEntityId)!, coastlandsAnchor, takenSemantic, bias: new SemanticHexCoord(2, -2));

        regions["northlands"] = CreateSeedCoverage(
            grid,
            coverageMap,
            graph,
            regions["northlands"],
            northlandsAnchor,
            desiredCellCount: 96,
            minimumCellCount: 72,
            preferredTerrains: new HashSet<TerrainType> { TerrainType.Ice, TerrainType.Tundra, TerrainType.Shelf });
        regions["heartlands"] = CreateSeedCoverage(
            grid,
            coverageMap,
            graph,
            regions["heartlands"],
            heartlandsAnchor,
            desiredCellCount: 144,
            minimumCellCount: 110,
            preferredTerrains: new HashSet<TerrainType> { TerrainType.Tundra, TerrainType.HotSprings, TerrainType.Wastes });
        regions["coastlands"] = CreateSeedCoverage(
            grid,
            coverageMap,
            graph,
            regions["coastlands"],
            coastlandsAnchor,
            desiredCellCount: 88,
            minimumCellCount: 64,
            preferredTerrains: new HashSet<TerrainType> { TerrainType.Shelf, TerrainType.Reef, TerrainType.Ocean, TerrainType.Tundra });

        var frosthavenCoord = ChooseAnchor(
            grid,
            random,
            takenPhysical,
            minimumDistance: 5,
            cell => FrosthavenScore(grid, coverageMap, regions["northlands"], cell));
        var ironholdCoord = ChooseAnchor(
            grid,
            random,
            takenPhysical,
            minimumDistance: 5,
            cell => IronholdScore(grid, coverageMap, regions["heartlands"], cell));
        var driftwatchCoord = ChooseAnchor(
            grid,
            random,
            takenPhysical,
            minimumDistance: 6,
            cell => DriftwatchScore(grid, coverageMap, regions["coastlands"], cell));
        var wavecrestCoord = ChooseAnchor(
            grid,
            random,
            takenPhysical,
            minimumDistance: 6,
            cell => WavecrestScore(grid, coverageMap, regions["coastlands"], cell));
        var midpointCoord = ChooseAnchor(
            grid,
            random,
            takenPhysical,
            minimumDistance: 5,
            cell => MidpointScore(grid, coverageMap, regions["heartlands"], cell));

        var s1 = graph.CreateEntity(
            K.Settlement, "rookery", "Frosthaven", C(IceRemembersCulture.AuroraStack), tick: 0,
            coordinates: (frosthavenCoord.Q, frosthavenCoord.R));
        var s2 = graph.CreateEntity(
            K.Settlement, "colony", "Ironhold", C(IceRemembersCulture.AuroraStack), tick: 0,
            coordinates: (ironholdCoord.Q, ironholdCoord.R));
        var s3 = graph.CreateEntity(
            K.Settlement, "harbor", "Driftwatch", C(IceRemembersCulture.Nightshelf), tick: 0,
            coordinates: (driftwatchCoord.Q, driftwatchCoord.R));
        var s4 = graph.CreateEntity(
            K.Settlement, "reefhold", "Wavecrest", C(IceRemembersCulture.Nightshelf), tick: 0,
            coordinates: (wavecrestCoord.Q, wavecrestCoord.R));
        var s5 = graph.CreateEntity(
            K.Settlement, "colony", "Midpoint", C(IceRemembersCulture.AuroraStack), tick: 0,
            coordinates: (midpointCoord.Q, midpointCoord.R));

        takenPhysical.Add(frosthavenCoord);
        takenPhysical.Add(ironholdCoord);
        takenPhysical.Add(driftwatchCoord);
        takenPhysical.Add(wavecrestCoord);
        takenPhysical.Add(midpointCoord);

        SetSemanticProjected(s1, frosthavenCoord, takenSemantic, bias: new SemanticHexCoord(-1, -2));
        SetSemanticProjected(s2, ironholdCoord, takenSemantic, bias: new SemanticHexCoord(-2, 1));
        SetSemanticProjected(s3, driftwatchCoord, takenSemantic, bias: new SemanticHexCoord(2, -1));
        SetSemanticProjected(s4, wavecrestCoord, takenSemantic, bias: new SemanticHexCoord(1, 2));
        SetSemanticProjected(s5, midpointCoord, takenSemantic, bias: SemanticHexCoord.Origin);

        var factionA = graph.CreateEntity(
            K.Faction, "compact", "Northern Compact", C(IceRemembersCulture.AuroraStack), tick: 0);
        var factionB = graph.CreateEntity(
            K.Faction, "order", "Southern Order", C(IceRemembersCulture.Nightshelf), tick: 0);

        var factionACoord = ChooseNearbyFreeCell(grid, takenPhysical, Centroid(frosthavenCoord, ironholdCoord, midpointCoord), searchRadius: 8)
            ?? throw new InvalidOperationException("Could not place Northern Compact near its settlements.");
        var factionBCoord = ChooseNearbyFreeCell(grid, takenPhysical, Centroid(driftwatchCoord, wavecrestCoord), searchRadius: 8)
            ?? throw new InvalidOperationException("Could not place Southern Order near its settlements.");
        SetPhysical(factionA, factionACoord, takenPhysical);
        SetPhysical(factionB, factionBCoord, takenPhysical);
        SetSemanticProjected(factionA, factionACoord, takenSemantic, bias: new SemanticHexCoord(-3, 3));
        SetSemanticProjected(factionB, factionBCoord, takenSemantic, bias: new SemanticHexCoord(3, -3));

        var settlementsByCoord = new Dictionary<HexCoord, EntityId>
        {
            [frosthavenCoord] = s1.Id,
            [ironholdCoord] = s2.Id,
            [driftwatchCoord] = s3.Id,
            [wavecrestCoord] = s4.Id,
            [midpointCoord] = s5.Id,
        };
        var npcHostSettlements = new[] { s1, s3, s5 };

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
                SetSemantic(npc, FindNearestFreeSemantic(npcSemanticSeeds[npcIndex], takenSemantic), takenSemantic);
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
        var artifactCoord = ChooseNearbyFreeCell(grid, takenPhysical, frosthavenCoord, searchRadius: 5)
            ?? throw new InvalidOperationException("Could not place Stormcleaver near Frosthaven.");
        var artifact = graph.CreateEntity(
            K.Artifact, "weapon", "Stormcleaver", C(IceRemembersCulture.AuroraStack), tick: 0,
            coordinates: (artifactCoord.Q, artifactCoord.R));
        takenPhysical.Add(artifactCoord);
        SetSemanticProjected(artifact, artifactCoord, takenSemantic, bias: new SemanticHexCoord(4, -1));

        // Artifact stored at Frosthaven (not held by a specific NPC since NPCs are randomly placed)
        graph.CreateRelationship("stored_at", RelationshipPolarity.Neutral, artifact.Id, s1.Id, 1f, 0);
    }

    private static TerrainType DetermineBaseTerrain(
        float oceanInfluence,
        float coldInfluence,
        float geothermalInfluence,
        float aridInfluence,
        float ruggedInfluence)
    {
        if (oceanInfluence >= 0.78f)
        {
            return TerrainType.Ocean;
        }

        if (geothermalInfluence >= 0.76f && oceanInfluence < 0.62f)
        {
            return TerrainType.HotSprings;
        }

        if (ruggedInfluence >= 0.74f && oceanInfluence < 0.65f)
        {
            return TerrainType.Crags;
        }

        if (coldInfluence >= 0.72f && oceanInfluence < 0.66f)
        {
            return TerrainType.Ice;
        }

        if (aridInfluence >= 0.72f && geothermalInfluence < 0.68f && oceanInfluence < 0.64f)
        {
            return TerrainType.Wastes;
        }

        return TerrainType.Tundra;
    }

    private static (float Elevation, float Fertility) TerrainProfileFor(
        TerrainType terrain,
        float oceanInfluence,
        float coldInfluence,
        float geothermalInfluence,
        float ruggedInfluence,
        float fertilityNoise)
    {
        return terrain switch
        {
            TerrainType.Ocean => (0f, 0f),
            TerrainType.Reef => (5f, 0.45f + (fertilityNoise * 0.2f)),
            TerrainType.Shelf => (10f, 0.3f + (fertilityNoise * 0.15f)),
            TerrainType.HotSprings => (12f + (geothermalInfluence * 12f), 0.82f + (fertilityNoise * 0.14f)),
            TerrainType.Crags => (180f + (ruggedInfluence * 220f), 0.08f + (fertilityNoise * 0.12f)),
            TerrainType.Ice => (70f + (coldInfluence * 60f), 0.1f + (fertilityNoise * 0.12f)),
            TerrainType.Wastes => (18f + (oceanInfluence * 10f), 0.12f + (fertilityNoise * 0.16f)),
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
        var cold = CountTerrainsInRadius(grid, cell.Coord, 2, TerrainType.Ice, TerrainType.Shelf) * 5f;
        var tundra = CountTerrainsInRadius(grid, cell.Coord, 2, TerrainType.Tundra) * 2f;
        return TerrainScore(cell.Terrain, TerrainType.Ice, 40f)
            + TerrainScore(cell.Terrain, TerrainType.Tundra, 18f)
            + cold
            + tundra
            + (center * 1.2f);
    }

    private static float RegionHeartlandsScore(HexGrid grid, HexCell cell)
    {
        var centerBias = 25f - (HexCoord.Distance(cell.Coord, new HexCoord(0, 0)) * 1.2f);
        return TerrainScore(cell.Terrain, TerrainType.HotSprings, 36f)
            + TerrainScore(cell.Terrain, TerrainType.Tundra, 24f)
            + TerrainScore(cell.Terrain, TerrainType.Wastes, 8f)
            + (cell.Habitability * 30f)
            + (CountTerrainsInRadius(grid, cell.Coord, 2, TerrainType.HotSprings, TerrainType.Tundra) * 3f)
            + centerBias;
    }

    private static float RegionCoastlandsScore(HexGrid grid, HexCell cell)
    {
        var rimBias = HexCoord.Distance(cell.Coord, new HexCoord(0, 0)) * 1.1f;
        return TerrainScore(cell.Terrain, TerrainType.Shelf, 34f)
            + TerrainScore(cell.Terrain, TerrainType.Reef, 30f)
            + TerrainScore(cell.Terrain, TerrainType.Tundra, 12f)
            + (CountAdjacentTerrains(grid, cell.Coord, TerrainType.Ocean, TerrainType.Reef, TerrainType.Shelf) * 9f)
            + rimBias;
    }

    private static float FrosthavenScore(HexGrid grid, MapCoverageMap coverageMap, MapCoverage northlands, HexCell cell)
    {
        return CoverageAffinity(coverageMap, northlands, cell.Coord, 28f, 12f)
            + TerrainScore(cell.Terrain, TerrainType.Ice, 28f)
            + TerrainScore(cell.Terrain, TerrainType.Tundra, 20f)
            + (CountTerrainsInRadius(grid, cell.Coord, 2, TerrainType.Ice, TerrainType.Shelf) * 4f)
            + (HexCoord.Distance(cell.Coord, new HexCoord(0, 0)) * 0.5f);
    }

    private static float IronholdScore(HexGrid grid, MapCoverageMap coverageMap, MapCoverage heartlands, HexCell cell)
    {
        return CoverageAffinity(coverageMap, heartlands, cell.Coord, 18f, 10f)
            + TerrainScore(cell.Terrain, TerrainType.Tundra, 16f)
            + TerrainScore(cell.Terrain, TerrainType.Crags, 20f)
            + TerrainScore(cell.Terrain, TerrainType.Wastes, 18f)
            + (CountAdjacentTerrains(grid, cell.Coord, TerrainType.Crags, TerrainType.Wastes) * 5f)
            + (cell.Habitability * 18f);
    }

    private static float DriftwatchScore(HexGrid grid, MapCoverageMap coverageMap, MapCoverage coastlands, HexCell cell)
    {
        return CoverageAffinity(coverageMap, coastlands, cell.Coord, 22f, 12f)
            + TerrainScore(cell.Terrain, TerrainType.Shelf, 26f)
            + TerrainScore(cell.Terrain, TerrainType.Reef, 24f)
            + TerrainScore(cell.Terrain, TerrainType.Tundra, 10f)
            + (CountAdjacentTerrains(grid, cell.Coord, TerrainType.Ocean, TerrainType.Reef, TerrainType.Shelf) * 8f);
    }

    private static float WavecrestScore(HexGrid grid, MapCoverageMap coverageMap, MapCoverage coastlands, HexCell cell)
    {
        return CoverageAffinity(coverageMap, coastlands, cell.Coord, 20f, 10f)
            + TerrainScore(cell.Terrain, TerrainType.Reef, 30f)
            + TerrainScore(cell.Terrain, TerrainType.Shelf, 24f)
            + (CountAdjacentTerrains(grid, cell.Coord, TerrainType.Ocean, TerrainType.Reef) * 10f)
            + (HexCoord.Distance(cell.Coord, new HexCoord(0, 0)) * 0.4f);
    }

    private static float MidpointScore(HexGrid grid, MapCoverageMap coverageMap, MapCoverage heartlands, HexCell cell)
    {
        return CoverageAffinity(coverageMap, heartlands, cell.Coord, 22f, 8f)
            + TerrainScore(cell.Terrain, TerrainType.HotSprings, 20f)
            + TerrainScore(cell.Terrain, TerrainType.Tundra, 18f)
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

    private static float TerrainScore(TerrainType actual, TerrainType desired, float value) =>
        actual == desired ? value : 0f;

    private static int CountTerrainsInRadius(HexGrid grid, HexCoord center, int radius, params TerrainType[] terrains)
    {
        var set = terrains.ToHashSet();
        return HexCoord.CoordsInRadius(center, radius)
            .Where(grid.ContainsCoord)
            .Select(grid.GetCell)
            .Where(cell => cell is not null && set.Contains(cell.Terrain))
            .Count();
    }

    private static int CountAdjacentTerrains(HexGrid grid, HexCoord center, params TerrainType[] terrains)
    {
        var set = terrains.ToHashSet();
        return center.Neighbors()
            .Where(grid.ContainsCoord)
            .Select(grid.GetCell)
            .Where(cell => cell is not null && set.Contains(cell.Terrain))
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

    private static void SetSemanticProjected(
        Engine.Graph.Entity entity,
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
                RespectTerrainBoundaries = false,
            });

        var regionEntity = graph.GetEntity(seed.OwnerEntityId)!;
        regionEntity.Coordinates = (result.Anchor.Q, result.Anchor.R);
        return result.Coverage;
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
