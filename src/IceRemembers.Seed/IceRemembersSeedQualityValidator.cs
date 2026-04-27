namespace IceRemembers.Seed;

using IceRemembers;
using Engine.Graph;
using Engine.Kinds;
using Engine.WorldGen;
using K = IceRemembers.IceRemembersKinds;

public static class IceRemembersSeedQualityValidator
{
    public static SeedWorldQualityReport Validate(TestWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var issues = new List<string>();
        ValidateTerrain(world, issues);
        ValidateCoverage(world, issues);
        ValidateSeedRoster(world, issues);
        ValidatePlacement(world, issues);
        ValidateRelationships(world, issues);
        return new SeedWorldQualityReport(issues);
    }

    public static string TerrainFingerprint(TestWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        return string.Join(
            "|",
            world.Grid.AllCells
                .OrderBy(cell => cell.Coord.Q)
                .ThenBy(cell => cell.Coord.R)
                .Select(cell =>
                    $"{cell.Coord.Q},{cell.Coord.R}:{cell.Terrain.StableName}:" +
                    $"{cell.AmbientResource(IceRemembersResource.Food.ToDomainName()):F2}:" +
                    $"{cell.AmbientResource(IceRemembersResource.Wealth.ToDomainName()):F2}:" +
                    $"{cell.AmbientResource(IceRemembersResource.Population.ToDomainName()):F2}"));
    }

    public static string EntityPlacementFingerprint(TestWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        return string.Join(
            "|",
            world.Graph.AllEntities.Values
                .Where(entity => entity.Status == EntityStatus.Active)
                .OrderBy(entity => entity.Kind.Value, StringComparer.Ordinal)
                .ThenBy(entity => entity.Name, StringComparer.Ordinal)
                .Select(entity => $"{entity.Kind}:{entity.Subtype}:{entity.Name}:{entity.Culture}:{entity.Coordinates.Q},{entity.Coordinates.R}:{entity.SemanticPosition.Q},{entity.SemanticPosition.R}"));
    }

    private static void ValidateTerrain(TestWorld world, List<string> issues)
    {
        var counts = world.Grid.AllCells
            .GroupBy(cell => cell.Terrain.StableName)
            .ToDictionary(group => group.Key, group => group.Count());

        if (counts.Count < 5)
        {
            issues.Add($"terrain variety too low: {counts.Count} terrain types.");
        }

        RequireTerrain(counts, IceRemembersTerrain.Ocean, issues);
        RequireAnyTerrain(counts, issues, IceRemembersTerrain.Reef, IceRemembersTerrain.Shelf);
        RequireTerrain(counts, IceRemembersTerrain.Tundra, issues);
        RequireAnyTerrain(counts, issues, IceRemembersTerrain.Ice, IceRemembersTerrain.Crags, IceRemembersTerrain.Wastes);

        var ocean = counts.GetValueOrDefault(IceRemembersTerrain.Ocean.ToDomainName());
        var maritime = ocean +
            counts.GetValueOrDefault(IceRemembersTerrain.Reef.ToDomainName()) +
            counts.GetValueOrDefault(IceRemembersTerrain.Shelf.ToDomainName());
        var harsh = counts.GetValueOrDefault(IceRemembersTerrain.Ice.ToDomainName()) +
                    counts.GetValueOrDefault(IceRemembersTerrain.Crags.ToDomainName()) +
                    counts.GetValueOrDefault(IceRemembersTerrain.Wastes.ToDomainName());
        var walkable = world.Grid.AllCells.Count(cell => !float.IsPositiveInfinity(cell.MovementCost));

        if (ocean is < 40 or > 460)
        {
            issues.Add($"ocean footprint out of bounds: {ocean} cells.");
        }

        if (maritime is < 90 or > 720)
        {
            issues.Add($"maritime footprint out of bounds: {maritime} cells.");
        }

        if (harsh < 90)
        {
            issues.Add($"harsh terrain footprint too low: {harsh} cells.");
        }

        if (walkable < 700)
        {
            issues.Add($"walkable terrain too low: {walkable} cells.");
        }
    }

    private static void ValidateCoverage(TestWorld world, List<string> issues)
    {
        if (world.CoverageMap.AllCoverages.Count != 3)
        {
            issues.Add($"expected exactly 3 seed region coverages, got {world.CoverageMap.AllCoverages.Count}.");
        }

        foreach (var coverage in world.CoverageMap.AllCoverages)
        {
            if (coverage.CellCount < 50)
            {
                issues.Add($"coverage '{coverage.Name}' too small: {coverage.CellCount} cells.");
            }
        }

        var claimed = world.CoverageMap.AllCoverages.Sum(coverage => coverage.CellCount);
        var uncovered = world.Grid.CellCount - claimed;
        if (uncovered < world.Grid.CellCount / 3)
        {
            issues.Add($"wilderness coverage too low: {uncovered} uncovered cells.");
        }

        if (!world.CoverageMap.ComputeAdjacency().Any())
        {
            issues.Add("seed coverages have no adjacency.");
        }
    }

    private static void ValidateSeedRoster(TestWorld world, List<string> issues)
    {
        var regions = Active(world, K.Region).ToList();
        var settlements = Active(world, K.Settlement).ToList();
        var factions = Active(world, K.Faction).ToList();
        var penguins = Active(world, K.Penguin).ToList();
        var artifacts = Active(world, K.Artifact).ToList();

        ExpectCount(regions.Count, 3, 3, "regions", issues);
        ExpectCount(settlements.Count, 7, 8, "settlements", issues);
        ExpectCount(factions.Count, 2, 2, "factions", issues);
        ExpectCount(penguins.Count, 5, 7, "penguins", issues);
        ExpectCount(artifacts.Count, 2, 3, "artifacts", issues);

        RequireNames(regions, issues, "Northlands", "Heartlands", "Coastlands");
        RequireNames(settlements, issues, "Frosthaven", "Ironhold", "Driftwatch", "Wavecrest", "Midpoint");
        RequireNames(factions, issues, "Northern Compact", "Southern Order");
        RequireNames(artifacts, issues, "Stormcleaver");
    }

    private static void ValidatePlacement(TestWorld world, List<string> issues)
    {
        var settlements = Active(world, K.Settlement).ToList();
        var penguins = Active(world, K.Penguin).ToList();
        var factions = Active(world, K.Faction).ToList();
        var artifacts = Active(world, K.Artifact).ToList();

        foreach (var entity in settlements.Concat(penguins).Concat(factions).Concat(artifacts))
        {
            var coord = Coord(entity);
            var cell = world.Grid.GetCell(coord);
            if (cell is null)
            {
                issues.Add($"{entity.Kind} '{entity.Name}' is outside the grid at {coord}.");
                continue;
            }

            if (float.IsPositiveInfinity(cell.MovementCost))
            {
                issues.Add($"{entity.Kind} '{entity.Name}' is on blocked terrain cost {cell.Terrain} at {coord}.");
            }
        }

        var activeCoords = Active(world)
            .Where(entity => entity.Kind != K.Region)
            .GroupBy(Coord)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}: {string.Join(", ", group.Select(entity => entity.Name))}")
            .ToList();
        if (activeCoords.Count > 0)
        {
            issues.Add($"non-region seed entities overlap physical coordinates: {string.Join("; ", activeCoords)}.");
        }

        if (settlements.Count > 0)
        {
            var coords = settlements.Select(Coord).ToList();
            var qSpan = coords.Max(coord => coord.Q) - coords.Min(coord => coord.Q);
            var rSpan = coords.Max(coord => coord.R) - coords.Min(coord => coord.R);
            var sSpan = coords.Max(coord => coord.S) - coords.Min(coord => coord.S);
            var maxDistance = coords.SelectMany(a => coords.Select(b => HexCoord.Distance(a, b))).Max();

            if (qSpan < 8 || rSpan < 8 || sSpan < 8)
            {
                issues.Add($"settlements are too corridor-like: spans q={qSpan}, r={rSpan}, s={sSpan}.");
            }

            if (maxDistance < 14)
            {
                issues.Add($"settlements do not span enough physical distance: max distance {maxDistance}.");
            }

            if (!settlements.Any(settlement => IsOnOrNear(world.Grid, Coord(settlement), IceRemembersTerrain.Ocean, IceRemembersTerrain.Reef, IceRemembersTerrain.Shelf)))
            {
                issues.Add("no settlement is on or near maritime terrain.");
            }

            if (!settlements.Any(settlement => DistanceToNearestTerrain(world.Grid, Coord(settlement), IceRemembersTerrain.Ocean, IceRemembersTerrain.Reef, IceRemembersTerrain.Shelf) >= 3))
            {
                issues.Add("no settlement is meaningfully interior.");
            }

            if (!settlements.Any(settlement => IsOnOrNear(world.Grid, Coord(settlement), IceRemembersTerrain.Ice, IceRemembersTerrain.Shelf, IceRemembersTerrain.Tundra)))
            {
                issues.Add("no settlement is anchored in cold terrain.");
            }
        }

        foreach (var penguin in penguins)
        {
            var nearest = DistanceToNearest(penguin, settlements);
            if (nearest is < 1 or > 2)
            {
                issues.Add($"penguin '{penguin.Name}' should start 1-2 hexes from a settlement, got {nearest}.");
            }
        }

        foreach (var faction in factions)
        {
            var nearest = DistanceToNearest(faction, settlements);
            if (nearest > 8)
            {
                issues.Add($"faction '{faction.Name}' too far from settlement interaction: {nearest}.");
            }
        }

        foreach (var artifact in artifacts)
        {
            var nearest = DistanceToNearest(artifact, settlements);
            if (nearest > 5)
            {
                issues.Add($"artifact '{artifact.Name}' too far from settlement custody: {nearest}.");
            }
        }
    }

    private static void ValidateRelationships(TestWorld world, List<string> issues)
    {
        foreach (var settlement in Active(world, K.Settlement))
        {
            if (!world.Graph.OutgoingRelationships(settlement.Id, "controlled_by").Any())
            {
                issues.Add($"settlement '{settlement.Name}' has no controlled_by relationship.");
            }
        }

        foreach (var penguin in Active(world, K.Penguin))
        {
            if (!world.Graph.OutgoingRelationships(penguin.Id, "member_of").Any())
            {
                issues.Add($"penguin '{penguin.Name}' has no member_of relationship.");
            }
        }

        foreach (var artifact in Active(world, K.Artifact))
        {
            if (!world.Graph.OutgoingRelationships(artifact.Id, "anchored").Any())
            {
                issues.Add($"artifact '{artifact.Name}' has no anchored relationship.");
            }
        }
    }

    private static void RequireTerrain(
        IReadOnlyDictionary<string, int> counts,
        IceRemembersTerrain terrain,
        List<string> issues)
    {
        if (counts.GetValueOrDefault(terrain.ToDomainName()) == 0)
        {
            issues.Add($"missing required terrain {terrain}.");
        }
    }

    private static void RequireAnyTerrain(
        IReadOnlyDictionary<string, int> counts,
        List<string> issues,
        params IceRemembersTerrain[] terrains)
    {
        if (!terrains.Any(terrain => counts.GetValueOrDefault(terrain.ToDomainName()) > 0))
        {
            issues.Add($"missing any of required terrain set [{string.Join(", ", terrains)}].");
        }
    }

    private static void ExpectCount(int actual, int min, int max, string label, List<string> issues)
    {
        if (actual < min || actual > max)
        {
            issues.Add($"expected {label} count in [{min}, {max}], got {actual}.");
        }
    }

    private static void RequireNames(IReadOnlyCollection<Entity> entities, List<string> issues, params string[] names)
    {
        var actual = entities.Select(entity => entity.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var name in names)
        {
            if (!actual.Contains(name))
            {
                issues.Add($"missing fixed seed entity '{name}'.");
            }
        }
    }

    private static IEnumerable<Entity> Active(TestWorld world) =>
        world.Graph.AllEntities.Values.Where(entity => entity.Status == EntityStatus.Active);

    private static IEnumerable<Entity> Active(TestWorld world, EntityKind kind) =>
        world.Graph.GetEntitiesByKind(kind)
            .Select(world.Graph.GetEntity)
            .Where(entity => entity is { Status: EntityStatus.Active })
            .Cast<Entity>();

    private static HexCoord Coord(Entity entity) =>
        new(entity.Coordinates.Q, entity.Coordinates.R);

    private static int DistanceToNearest(Entity entity, List<Entity> others)
    {
        if (others.Count == 0)
        {
            return int.MaxValue;
        }

        var coord = Coord(entity);
        return others.Min(other => HexCoord.Distance(coord, Coord(other)));
    }

    private static bool IsOnOrNear(HexGrid grid, HexCoord coord, params IceRemembersTerrain[] terrains) =>
        DistanceToNearestTerrain(grid, coord, terrains) <= 1;

    private static int DistanceToNearestTerrain(HexGrid grid, HexCoord origin, params IceRemembersTerrain[] terrains)
    {
        var set = terrains.Select(terrain => terrain.ToDomainName()).ToHashSet(StringComparer.Ordinal);
        for (var radius = 0; radius <= grid.Radius; radius++)
        {
            var found = HexCoord.CoordsInRadius(origin, radius)
                .Where(coord => HexCoord.Distance(origin, coord) == radius)
                .Where(grid.ContainsCoord)
                .Select(grid.GetCell)
                .Any(cell => cell is not null && set.Contains(cell.Terrain.StableName));

            if (found)
            {
                return radius;
            }
        }

        return int.MaxValue;
    }
}

public sealed class SeedWorldQualityReport
{
    private readonly List<string> _issues;

    public SeedWorldQualityReport(IEnumerable<string> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);
        _issues = issues.ToList();
    }

    public bool IsValid => _issues.Count == 0;
    public IReadOnlyList<string> Issues => _issues;
}
