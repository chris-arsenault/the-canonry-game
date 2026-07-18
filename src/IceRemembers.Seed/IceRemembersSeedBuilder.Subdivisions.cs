namespace IceRemembers.Seed;

using System.Numerics;

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
        PhysicalGeographyField field,
        ReadOnlySpan<PhysicalHexAggregate> aggregates,
        int salt)
    {
        var continentSeeds = CreateContinentSeeds(field, aggregates);
        var continents = GrowConnectedRegions(field, aggregates, continentSeeds, [], salt ^ 0x2a91);
        var superregionSeeds = CreateSuperregionSeeds(field, aggregates, continents);
        var superregions = GrowConnectedRegions(
            field,
            aggregates,
            superregionSeeds,
            continents,
            salt ^ 0x6f17);
        return BuildSubdivisionMap(field, continents, superregions);
    }

    private static RegionSeed[] CreateContinentSeeds(
        PhysicalGeographyField field,
        ReadOnlySpan<PhysicalHexAggregate> aggregates)
    {
        var scale = PhysicalSubdivisionScale;
        return
        [
            new(
                NearContinentId,
                SelectAnchor(
                    field,
                    aggregates,
                    [],
                    0,
                    new HexCoord(-scale / 2, 0),
                    AnchorPreference.Land)),
            new(
                FarContinentId,
                SelectAnchor(
                    field,
                    aggregates,
                    [],
                    0,
                    new HexCoord((scale * 2) / 3, -(scale * 3) / 5),
                    AnchorPreference.Ocean)),
            new(
                EmptyContinentId,
                SelectAnchor(
                    field,
                    aggregates,
                    [],
                    0,
                    new HexCoord(-(scale * 2) / 5, (scale * 3) / 4),
                    AnchorPreference.ColdLand)),
        ];
    }

    private static RegionSeed[] CreateSuperregionSeeds(
        PhysicalGeographyField field,
        ReadOnlySpan<PhysicalHexAggregate> aggregates,
        ReadOnlySpan<int> continents)
    {
        var scale = PhysicalSubdivisionScale;
        return
        [
            SuperregionSeed(field, aggregates, continents, NearContinentId, NorthlandsSuperregionId,
                new HexCoord(-(scale * 3) / 4, -2), AnchorPreference.ColdLand),
            SuperregionSeed(field, aggregates, continents, NearContinentId, HeartlandsSuperregionId,
                new HexCoord(-scale / 4, scale / 3), AnchorPreference.Land),
            SuperregionSeed(field, aggregates, continents, NearContinentId, CoastlandsSuperregionId,
                new HexCoord(scale / 4, -(scale * 3) / 5), AnchorPreference.Coast),
            SuperregionSeed(field, aggregates, continents, FarContinentId, FarwakeSuperregionId,
                new HexCoord((scale * 2) / 3, -(scale * 3) / 5), AnchorPreference.Ocean),
            SuperregionSeed(field, aggregates, continents, FarContinentId, BlackwaterSuperregionId,
                new HexCoord((scale * 7) / 8, 0), AnchorPreference.DeepOcean),
            SuperregionSeed(field, aggregates, continents, EmptyContinentId, WhiteEmptySuperregionId,
                new HexCoord(-(scale * 2) / 5, (scale * 3) / 4), AnchorPreference.ColdLand),
            SuperregionSeed(field, aggregates, continents, EmptyContinentId, DeepEmptySuperregionId,
                new HexCoord(scale / 4, (scale * 3) / 4), AnchorPreference.Highland),
        ];
    }

    private static RegionSeed SuperregionSeed(
        PhysicalGeographyField field,
        ReadOnlySpan<PhysicalHexAggregate> aggregates,
        ReadOnlySpan<int> continents,
        int continentId,
        int superregionId,
        HexCoord target,
        AnchorPreference preference) =>
        new(
            superregionId,
            SelectAnchor(field, aggregates, continents, continentId, target, preference),
            continentId);

    private static int SelectAnchor(
        PhysicalGeographyField field,
        ReadOnlySpan<PhysicalHexAggregate> aggregates,
        ReadOnlySpan<int> parentRegions,
        int parentRegionId,
        HexCoord target,
        AnchorPreference preference)
    {
        var bestHexIndex = -1;
        var bestScore = long.MaxValue;
        for (var configuredIndex = 0;
            configuredIndex < field.Grid.ConfiguredHexCount;
            configuredIndex++)
        {
            var hexIndex = field.Grid.ConfiguredHexIndexAt(configuredIndex);
            if (parentRegions.Length != 0 && parentRegions[hexIndex] != parentRegionId)
            {
                continue;
            }

            var coord = field.Grid.HexCoordAt(hexIndex);
            var score = (HexCoord.Distance(coord, target) * 100L) +
                AnchorPenalty(aggregates[hexIndex], preference);
            if (score < bestScore || (score == bestScore && hexIndex < bestHexIndex))
            {
                bestHexIndex = hexIndex;
                bestScore = score;
            }
        }

        return bestHexIndex >= 0
            ? bestHexIndex
            : throw new InvalidOperationException("Physical subdivision has no eligible anchor.");
    }

    private static long AnchorPenalty(PhysicalHexAggregate aggregate, AnchorPreference preference) =>
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
        PhysicalGeographyField field,
        ReadOnlySpan<PhysicalHexAggregate> aggregates,
        ReadOnlySpan<RegionSeed> seeds,
        ReadOnlySpan<int> parentRegions,
        int salt)
    {
        ValidateRegionSeeds(seeds);
        var assigned = new int[field.Grid.HexCount];
        var bestCosts = new long[checked(field.Grid.HexCount * seeds.Length)];
        Array.Fill(bestCosts, long.MaxValue);
        var queue = new PriorityQueue<GrowthFrontier, (long Cost, int RegionId, int HexIndex)>();
        SeedRegionGrowth(seeds, field.Grid.HexCount, bestCosts, queue);
        while (queue.TryDequeue(out var frontier, out var priority))
        {
            if (assigned[frontier.HexIndex] != 0 ||
                priority.Cost != bestCosts[(frontier.SeedIndex * field.Grid.HexCount) + frontier.HexIndex])
            {
                continue;
            }

            assigned[frontier.HexIndex] = seeds[frontier.SeedIndex].RegionId;
            ExpandRegionGrowth(
                field,
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

        ValidateRegionCoverage(field, seeds, assigned);
        return assigned;
    }

    private static void ValidateRegionSeeds(ReadOnlySpan<RegionSeed> seeds)
    {
        var anchors = new HashSet<(int ParentRegionId, int HexIndex)>();
        var regionIds = new HashSet<int>();
        foreach (var seed in seeds)
        {
            if (!anchors.Add((seed.ParentRegionId, seed.HexIndex)) || !regionIds.Add(seed.RegionId))
            {
                throw new InvalidOperationException("Physical subdivision seeds must be distinct.");
            }
        }
    }

    private static void SeedRegionGrowth(
        ReadOnlySpan<RegionSeed> seeds,
        int hexCount,
        Span<long> bestCosts,
        PriorityQueue<GrowthFrontier, (long Cost, int RegionId, int HexIndex)> queue)
    {
        for (var seedIndex = 0; seedIndex < seeds.Length; seedIndex++)
        {
            var seed = seeds[seedIndex];
            bestCosts[(seedIndex * hexCount) + seed.HexIndex] = 0;
            queue.Enqueue(
                new GrowthFrontier(seedIndex, seed.HexIndex),
                (0, seed.RegionId, seed.HexIndex));
        }
    }

    private static void ExpandRegionGrowth(
        PhysicalGeographyField field,
        ReadOnlySpan<PhysicalHexAggregate> aggregates,
        ReadOnlySpan<RegionSeed> seeds,
        ReadOnlySpan<int> parentRegions,
        int salt,
        ReadOnlySpan<int> assigned,
        Span<long> bestCosts,
        PriorityQueue<GrowthFrontier, (long Cost, int RegionId, int HexIndex)> queue,
        GrowthFrontier frontier,
        long currentCost)
    {
        var seed = seeds[frontier.SeedIndex];
        var coord = field.Grid.HexCoordAt(frontier.HexIndex);
        foreach (var neighborCoord in coord.Neighbors())
        {
            if (!field.Grid.TryGetHexIndex(neighborCoord, out var neighborHexIndex) ||
                !field.Grid.TriangleIsInsideConfiguredDisk(field.Grid.TriangleIndex(neighborHexIndex, 0)) ||
                assigned[neighborHexIndex] != 0 ||
                (parentRegions.Length != 0 && parentRegions[neighborHexIndex] != seed.ParentRegionId))
            {
                continue;
            }

            EnqueueGrowthCandidate(
                field,
                aggregates,
                seed,
                salt,
                bestCosts,
                queue,
                frontier.SeedIndex,
                neighborHexIndex,
                currentCost);
        }
    }

    private static void EnqueueGrowthCandidate(
        PhysicalGeographyField field,
        ReadOnlySpan<PhysicalHexAggregate> aggregates,
        RegionSeed seed,
        int salt,
        Span<long> bestCosts,
        PriorityQueue<GrowthFrontier, (long Cost, int RegionId, int HexIndex)> queue,
        int seedIndex,
        int hexIndex,
        long currentCost)
    {
        var bestCostIndex = (seedIndex * field.Grid.HexCount) + hexIndex;
        var nextCost = currentCost + RegionStepCost(
            seed.RegionId,
            aggregates[hexIndex],
            field.Grid.HexCoordAt(hexIndex),
            salt);
        if (nextCost >= bestCosts[bestCostIndex])
        {
            return;
        }

        bestCosts[bestCostIndex] = nextCost;
        queue.Enqueue(
            new GrowthFrontier(seedIndex, hexIndex),
            (nextCost, seed.RegionId, hexIndex));
    }

    private static int RegionStepCost(
        int regionId,
        PhysicalHexAggregate aggregate,
        HexCoord coord,
        int salt)
    {
        var geographicBarrier = (aggregate.Relief / 8) +
            (BitOperations.PopCount((uint)aggregate.CliffEdgeMask) * 18);
        var affinity = regionId switch
        {
            NearContinentId => aggregate.OceanCoverage * 11,
            FarContinentId => aggregate.LandCoverage * 10,
            EmptyContinentId =>
                (aggregate.WaterCoverage * 7) + Math.Max(0, aggregate.MeanTemperature - 300) / 3,
            _ => aggregate.WaterCoverage * 2,
        };
        var variation = (int)(CellNoise(coord, salt ^ regionId) * 22f);
        return 80 + geographicBarrier + affinity + variation;
    }

    private static void ValidateRegionCoverage(
        PhysicalGeographyField field,
        ReadOnlySpan<RegionSeed> seeds,
        ReadOnlySpan<int> assigned)
    {
        for (var configuredIndex = 0;
            configuredIndex < field.Grid.ConfiguredHexCount;
            configuredIndex++)
        {
            if (assigned[field.Grid.ConfiguredHexIndexAt(configuredIndex)] == 0)
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
        PhysicalGeographyField field,
        ReadOnlySpan<int> continents,
        ReadOnlySpan<int> superregions)
    {
        var map = new MapSubdivisionMap();
        for (var configuredIndex = 0;
            configuredIndex < field.Grid.ConfiguredHexCount;
            configuredIndex++)
        {
            var hexIndex = field.Grid.ConfiguredHexIndexAt(configuredIndex);
            map.AssignCell(
                field.Grid.HexCoordAt(hexIndex),
                superregions[hexIndex],
                continents[hexIndex]);
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
        int HexIndex,
        int ParentRegionId = 0);

    private readonly record struct GrowthFrontier(
        int SeedIndex,
        int HexIndex);

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
