namespace IceRemembers.Seed;

using System.Numerics;

using Engine.Domain.Spatial;
using Engine.Domain.Startup;
using Engine.WorldGen;

internal static partial class IceRemembersSeedBuilder
{
    private const int NearContinentId = 1;
    private const int FarContinentId = 2;
    private const int EmptyContinentId = 3;

    private const int NorthlandsSuperregionId = 11;
    private const int HeartlandsSuperregionId = 12;
    private const int CoastlandsSuperregionId = 13;
    private const int FarwakeSuperregionId = 21;
    private const int BlackwaterSuperregionId = 22;
    private const int WhiteEmptySuperregionId = 31;
    private const int DeepEmptySuperregionId = 32;

    private const int PhysicalSubdivisionScale = 32;

    private const int SemanticMemoryContinentId = 101;
    private const int SemanticAuthorityContinentId = 102;
    private const int SemanticShadowContinentId = 103;
    private const int SemanticRuptureContinentId = 104;

    private const int SemanticLineageSuperregionId = 111;
    private const int SemanticYokeSuperregionId = 121;
    private const int SemanticWitnessSuperregionId = 112;
    private const int SemanticSilenceSuperregionId = 131;
    private const int SemanticSeveranceSuperregionId = 141;
    private const int SemanticBreakawaySuperregionId = 142;
    private const int SemanticBeaconSpireSuperregionId = 113;
    private const int SemanticTabooRidgeSuperregionId = 132;
    private const int SemanticCavePocketSuperregionId = 133;
    private const int SemanticOpenFieldSuperregionId = 114;

    private static DomainMapSubdivision[] CreateSubdivisionDefinitions() =>
    [
        new(NearContinentId, DomainMapSubdivisionLevel.Continent, "near_shelf", "The Near Shelf"),
        new(FarContinentId, DomainMapSubdivisionLevel.Continent, "farwake_sea", "Farwake Sea"),
        new(EmptyContinentId, DomainMapSubdivisionLevel.Continent, "white_empty_continent", "The White Empty"),
        new(SemanticMemoryContinentId, DomainMapSubdivisionLevel.Continent, "semantic_memory_world", "Memory World"),
        new(SemanticAuthorityContinentId, DomainMapSubdivisionLevel.Continent, "semantic_authority_world", "Authority World"),
        new(SemanticShadowContinentId, DomainMapSubdivisionLevel.Continent, "semantic_shadow_world", "Shadow World"),
        new(SemanticRuptureContinentId, DomainMapSubdivisionLevel.Continent, "semantic_rupture_world", "Rupture World"),
        new(NorthlandsSuperregionId, DomainMapSubdivisionLevel.Superregion, "northlands_march", "Northlands March"),
        new(HeartlandsSuperregionId, DomainMapSubdivisionLevel.Superregion, "heartlands_basin", "Heartlands Basin"),
        new(CoastlandsSuperregionId, DomainMapSubdivisionLevel.Superregion, "coastlands_shelf", "Coastlands Shelf"),
        new(FarwakeSuperregionId, DomainMapSubdivisionLevel.Superregion, "farwake_reach", "Farwake Reach"),
        new(BlackwaterSuperregionId, DomainMapSubdivisionLevel.Superregion, "blackwater_reach", "Blackwater Reach"),
        new(WhiteEmptySuperregionId, DomainMapSubdivisionLevel.Superregion, "white_empty_fields", "White Empty"),
        new(DeepEmptySuperregionId, DomainMapSubdivisionLevel.Superregion, "deep_empty", "Deep Empty"),
        new(SemanticLineageSuperregionId, DomainMapSubdivisionLevel.Superregion, "semantic_lineage_basin", "Lineage Basin"),
        new(SemanticYokeSuperregionId, DomainMapSubdivisionLevel.Superregion, "semantic_yoke_court", "Yoke Court"),
        new(SemanticWitnessSuperregionId, DomainMapSubdivisionLevel.Superregion, "semantic_witness_basin", "Witness Basin"),
        new(SemanticSilenceSuperregionId, DomainMapSubdivisionLevel.Superregion, "semantic_silence_basin", "Silence Basin"),
        new(SemanticSeveranceSuperregionId, DomainMapSubdivisionLevel.Superregion, "semantic_severance_scar", "Severance Scar"),
        new(SemanticBreakawaySuperregionId, DomainMapSubdivisionLevel.Superregion, "semantic_breakaway_field", "Breakaway Field"),
        new(SemanticBeaconSpireSuperregionId, DomainMapSubdivisionLevel.Superregion, "semantic_beacon_spires", "Beacon Spires"),
        new(SemanticTabooRidgeSuperregionId, DomainMapSubdivisionLevel.Superregion, "semantic_taboo_ridges", "Taboo Ridges"),
        new(SemanticCavePocketSuperregionId, DomainMapSubdivisionLevel.Superregion, "semantic_cave_pockets", "Cave Pockets"),
        new(SemanticOpenFieldSuperregionId, DomainMapSubdivisionLevel.Superregion, "semantic_open_fields", "Open Fields"),
    ];

    private static MapSubdivisionMap CreatePhysicalSubdivisions(
        PhysicalKernelMapGeometry geometry,
        ReadOnlySpan<PhysicalKernelCellAggregate> aggregates,
        int salt)
    {
        var continentSeeds = CreateContinentSeeds(geometry, aggregates);
        var continents = GrowConnectedRegions(
            geometry,
            aggregates,
            continentSeeds,
            [],
            salt ^ 0x2a91);
        var superregionSeeds = CreateSuperregionSeeds(geometry, aggregates, continents);
        var superregions = GrowConnectedRegions(
            geometry,
            aggregates,
            superregionSeeds,
            continents,
            salt ^ 0x6f17);
        return BuildSubdivisionMap(geometry, continents, superregions);
    }

    private static RegionSeed[] CreateContinentSeeds(
        PhysicalKernelMapGeometry geometry,
        ReadOnlySpan<PhysicalKernelCellAggregate> aggregates)
    {
        var scale = PhysicalHeartRadius;
        return
        [
            new(
                NearContinentId,
                SelectAnchor(
                    geometry,
                    aggregates,
                    [],
                    0,
                    new HexCoord(-scale / 2, 0),
                    AnchorPreference.Land,
                    PhysicalHeartRadius)),
            new(
                FarContinentId,
                SelectAnchor(
                    geometry,
                    aggregates,
                    [],
                    0,
                    new HexCoord((scale * 2) / 3, -(scale * 3) / 5),
                    AnchorPreference.Coast,
                    PhysicalHeartRadius)),
            new(
                EmptyContinentId,
                SelectAnchor(
                    geometry,
                    aggregates,
                    [],
                    0,
                    new HexCoord(-(scale * 2) / 5, (scale * 3) / 4),
                    AnchorPreference.ColdLand,
                    PhysicalHeartRadius)),
        ];
    }

    private static RegionSeed[] CreateSuperregionSeeds(
        PhysicalKernelMapGeometry geometry,
        ReadOnlySpan<PhysicalKernelCellAggregate> aggregates,
        ReadOnlySpan<int> continents)
    {
        var scale = PhysicalSubdivisionScale;
        return
        [
            SuperregionSeed(geometry, aggregates, continents, NearContinentId, NorthlandsSuperregionId,
                new HexCoord(-(scale * 3) / 4, -2), AnchorPreference.ColdLand),
            SuperregionSeed(geometry, aggregates, continents, NearContinentId, HeartlandsSuperregionId,
                new HexCoord(-scale / 4, scale / 3), AnchorPreference.Land),
            SuperregionSeed(geometry, aggregates, continents, NearContinentId, CoastlandsSuperregionId,
                new HexCoord(scale / 4, -(scale * 3) / 5), AnchorPreference.Coast),
            SuperregionSeed(geometry, aggregates, continents, FarContinentId, FarwakeSuperregionId,
                new HexCoord((scale * 2) / 3, -(scale * 3) / 5), AnchorPreference.Ocean),
            SuperregionSeed(geometry, aggregates, continents, FarContinentId, BlackwaterSuperregionId,
                new HexCoord((scale * 7) / 8, 0), AnchorPreference.DeepOcean),
            SuperregionSeed(geometry, aggregates, continents, EmptyContinentId, WhiteEmptySuperregionId,
                new HexCoord(-(scale * 2) / 5, (scale * 3) / 4), AnchorPreference.ColdLand),
            SuperregionSeed(geometry, aggregates, continents, EmptyContinentId, DeepEmptySuperregionId,
                new HexCoord(scale / 4, (scale * 3) / 4), AnchorPreference.Highland),
        ];
    }

    private static RegionSeed SuperregionSeed(
        PhysicalKernelMapGeometry geometry,
        ReadOnlySpan<PhysicalKernelCellAggregate> aggregates,
        ReadOnlySpan<int> continents,
        int continentId,
        int superregionId,
        HexCoord target,
        AnchorPreference preference) =>
        new(
            superregionId,
            SelectAnchor(geometry, aggregates, continents, continentId, target, preference),
            continentId);

    private static int SelectAnchor(
        PhysicalKernelMapGeometry geometry,
        ReadOnlySpan<PhysicalKernelCellAggregate> aggregates,
        ReadOnlySpan<int> parentRegions,
        int parentRegionId,
        HexCoord target,
        AnchorPreference preference,
        int maximumOriginDistance = int.MaxValue)
    {
        var bestCellIndex = -1;
        var bestScore = long.MaxValue;
        for (var cellIndex = 0; cellIndex < geometry.CellCount; cellIndex++)
        {
            var coord = geometry.CoordAt(cellIndex);
            if (parentRegions.Length != 0 && parentRegions[cellIndex] != parentRegionId)
            {
                continue;
            }

            if (HexCoord.Distance(new HexCoord(0, 0), coord) > maximumOriginDistance)
            {
                continue;
            }

            var score = (HexCoord.Distance(coord, target) * 100L) +
                AnchorPenalty(aggregates[cellIndex], preference);
            if (score < bestScore || (score == bestScore && cellIndex < bestCellIndex))
            {
                bestCellIndex = cellIndex;
                bestScore = score;
            }
        }

        return bestCellIndex >= 0
            ? bestCellIndex
            : throw new InvalidOperationException("Physical subdivision has no eligible anchor.");
    }

    private static long AnchorPenalty(
        PhysicalKernelCellAggregate aggregate,
        AnchorPreference preference) =>
        preference switch
        {
            AnchorPreference.Land => aggregate.WaterCoverage * 100L,
            AnchorPreference.Ocean => aggregate.LandCoverage * 100L,
            AnchorPreference.DeepOcean =>
                (aggregate.LandCoverage * 120L) + Math.Max(0, 24 - aggregate.OceanCoverage) * 30L,
            AnchorPreference.ColdLand =>
                (aggregate.WaterCoverage * 100L) + aggregate.MeanTemperature,
            AnchorPreference.Coast =>
                (aggregate.CoastEdgeMask == 0 ? 1800L : 0L) +
                (Math.Abs(12 - aggregate.WaterCoverage) * 40L),
            AnchorPreference.Highland =>
                (aggregate.WaterCoverage * 100L) + Math.Max(0, 300 - aggregate.MeanElevation),
            _ => 0L,
        };

    private static int[] GrowConnectedRegions(
        PhysicalKernelMapGeometry geometry,
        ReadOnlySpan<PhysicalKernelCellAggregate> aggregates,
        ReadOnlySpan<RegionSeed> seeds,
        ReadOnlySpan<int> parentRegions,
        int salt)
    {
        ValidateRegionSeeds(seeds);
        var assigned = new int[geometry.CellCount];
        var bestCosts = new long[checked(geometry.CellCount * seeds.Length)];
        Array.Fill(bestCosts, long.MaxValue);
        var queue = new PriorityQueue<GrowthFrontier, (long Cost, int RegionId, int CellIndex)>();
        SeedRegionGrowth(seeds, geometry.CellCount, bestCosts, queue);
        while (queue.TryDequeue(out var frontier, out var priority))
        {
            if (assigned[frontier.CellIndex] != 0 ||
                priority.Cost != bestCosts[(frontier.SeedIndex * geometry.CellCount) + frontier.CellIndex])
            {
                continue;
            }

            assigned[frontier.CellIndex] = seeds[frontier.SeedIndex].RegionId;
            ExpandRegionGrowth(
                geometry,
                aggregates,
                seeds,
                parentRegions,
                salt,
                assigned,
                bestCosts,
                queue,
                frontier,
                priority.Cost);
        }

        ValidateRegionCoverage(geometry, seeds, assigned);
        return assigned;
    }

    private static void ValidateRegionSeeds(ReadOnlySpan<RegionSeed> seeds)
    {
        var anchors = new HashSet<(int ParentRegionId, int CellIndex)>();
        var regionIds = new HashSet<int>();
        foreach (var seed in seeds)
        {
            if (!anchors.Add((seed.ParentRegionId, seed.CellIndex)) || !regionIds.Add(seed.RegionId))
            {
                throw new InvalidOperationException("Physical subdivision seeds must be distinct.");
            }
        }
    }

    private static void SeedRegionGrowth(
        ReadOnlySpan<RegionSeed> seeds,
        int cellCount,
        Span<long> bestCosts,
        PriorityQueue<GrowthFrontier, (long Cost, int RegionId, int CellIndex)> queue)
    {
        for (var seedIndex = 0; seedIndex < seeds.Length; seedIndex++)
        {
            var seed = seeds[seedIndex];
            bestCosts[(seedIndex * cellCount) + seed.CellIndex] = 0;
            queue.Enqueue(
                new GrowthFrontier(seedIndex, seed.CellIndex),
                (0, seed.RegionId, seed.CellIndex));
        }
    }

    private static void ExpandRegionGrowth(
        PhysicalKernelMapGeometry geometry,
        ReadOnlySpan<PhysicalKernelCellAggregate> aggregates,
        ReadOnlySpan<RegionSeed> seeds,
        ReadOnlySpan<int> parentRegions,
        int salt,
        ReadOnlySpan<int> assigned,
        Span<long> bestCosts,
        PriorityQueue<GrowthFrontier, (long Cost, int RegionId, int CellIndex)> queue,
        GrowthFrontier frontier,
        long currentCost)
    {
        var seed = seeds[frontier.SeedIndex];
        var coord = geometry.CoordAt(frontier.CellIndex);
        foreach (var neighborCoord in coord.Neighbors())
        {
            if (!geometry.TryGetCellIndex(neighborCoord, out var neighborCellIndex) ||
                assigned[neighborCellIndex] != 0 ||
                (parentRegions.Length != 0 && parentRegions[neighborCellIndex] != seed.ParentRegionId))
            {
                continue;
            }

            EnqueueGrowthCandidate(
                geometry,
                aggregates,
                seed,
                salt,
                bestCosts,
                queue,
                frontier.SeedIndex,
                neighborCellIndex,
                currentCost);
        }
    }

    private static void EnqueueGrowthCandidate(
        PhysicalKernelMapGeometry geometry,
        ReadOnlySpan<PhysicalKernelCellAggregate> aggregates,
        RegionSeed seed,
        int salt,
        Span<long> bestCosts,
        PriorityQueue<GrowthFrontier, (long Cost, int RegionId, int CellIndex)> queue,
        int seedIndex,
        int cellIndex,
        long currentCost)
    {
        var bestCostIndex = (seedIndex * geometry.CellCount) + cellIndex;
        var nextCost = currentCost + RegionStepCost(
            seed.RegionId,
            aggregates[cellIndex],
            geometry.CoordAt(cellIndex),
            salt);
        if (nextCost >= bestCosts[bestCostIndex])
        {
            return;
        }

        bestCosts[bestCostIndex] = nextCost;
        queue.Enqueue(
            new GrowthFrontier(seedIndex, cellIndex),
            (nextCost, seed.RegionId, cellIndex));
    }

    private static int RegionStepCost(
        int regionId,
        PhysicalKernelCellAggregate aggregate,
        HexCoord coord,
        int salt)
    {
        var geographicBarrier = (aggregate.Relief / 8) +
            (BitOperations.PopCount((uint)aggregate.CliffEdgeMask) * 18);
        var affinity = regionId switch
        {
            NearContinentId => aggregate.OceanCoverage * 11,
            FarContinentId => Math.Abs(12 - aggregate.WaterCoverage) * 5,
            EmptyContinentId =>
                (aggregate.WaterCoverage * 7) + Math.Max(0, aggregate.MeanTemperature - 300) / 3,
            _ => aggregate.WaterCoverage * 2,
        };
        var variation = (int)(CellNoise(coord, salt ^ regionId) * 22f);
        return 80 + geographicBarrier + affinity + variation;
    }

    private static void ValidateRegionCoverage(
        PhysicalKernelMapGeometry geometry,
        ReadOnlySpan<RegionSeed> seeds,
        ReadOnlySpan<int> assigned)
    {
        for (var cellIndex = 0; cellIndex < geometry.CellCount; cellIndex++)
        {
            if (assigned[cellIndex] == 0)
            {
                throw new InvalidOperationException("Physical subdivision growth left an unassigned hex.");
            }
        }

        foreach (var seed in seeds)
        {
            if (!assigned.Contains(seed.RegionId))
            {
                throw new InvalidOperationException("Physical subdivision growth lost an authored region.");
            }
        }
    }

    private static MapSubdivisionMap BuildSubdivisionMap(
        PhysicalKernelMapGeometry geometry,
        ReadOnlySpan<int> continents,
        ReadOnlySpan<int> superregions)
    {
        var map = new MapSubdivisionMap();
        for (var cellIndex = 0; cellIndex < geometry.CellCount; cellIndex++)
        {
            map.AssignCell(
                geometry.CoordAt(cellIndex),
                superregions[cellIndex],
                continents[cellIndex]);
        }

        return map;
    }

    private enum AnchorPreference
    {
        Land,
        Ocean,
        DeepOcean,
        ColdLand,
        Coast,
        Highland,
    }

    private readonly record struct RegionSeed(
        int RegionId,
        int CellIndex,
        int ParentRegionId = 0);

    private readonly record struct GrowthFrontier(
        int SeedIndex,
        int CellIndex);

    private static float SuperregionAffinity(
        MapSubdivisionMap subdivisionMap,
        int superregionId,
        HexCoord coord,
        float bonus)
    {
        var subdivision = subdivisionMap.GetCell(coord);
        return subdivision.SuperregionId == superregionId ? bonus : 0f;
    }
}
