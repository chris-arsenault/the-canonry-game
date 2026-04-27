namespace IceRemembers.Seed;

using System.Globalization;
using Engine.Domain.Startup;
using Engine.WorldGen;

internal static class IceRemembersSeedQualityValidator
{
    public static SeedWorldQualityReport Validate(DomainStartupSeed seed)
    {
        ArgumentNullException.ThrowIfNull(seed);

        var issues = new List<string>();
        if (seed.Cells.Count == 0)
        {
            issues.Add("startup seed has no cells");
        }

        if (seed.Entities.Count == 0)
        {
            issues.Add("startup seed has no entities");
        }

        var cellsByCoord = seed.Cells.ToDictionary(cell => new HexCoord(cell.Q, cell.R));
        var terrainCatalog = IceRemembersTerrainCatalog.CreateWorldGenCatalog();
        var entityIds = seed.Entities.Select(entity => entity.SeedEntityId).ToHashSet();

        var duplicatePhysical = seed.Entities
            .GroupBy(entity => new HexCoord(entity.PhysicalQ, entity.PhysicalR))
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicatePhysical.Length > 0)
        {
            issues.Add($"duplicate physical positions: {duplicatePhysical.Length}");
        }

        foreach (var entity in seed.Entities)
        {
            var coord = new HexCoord(entity.PhysicalQ, entity.PhysicalR);
            if (!cellsByCoord.TryGetValue(coord, out var cell))
            {
                issues.Add($"{EntityLabel(entity)} is outside the grid at {coord}");
                continue;
            }

            try
            {
                var terrain = terrainCatalog.Resolve(cell.Terrain);
                if (float.IsPositiveInfinity(terrain.MovementCost))
                {
                    issues.Add($"{EntityLabel(entity)} is on blocked terrain at {coord}");
                }
            }
            catch (KeyNotFoundException)
            {
                issues.Add($"unknown terrain '{cell.Terrain}' at {coord}");
            }
        }

        foreach (var relationship in seed.Relationships)
        {
            if (!entityIds.Contains(relationship.SourceSeedEntityId))
            {
                issues.Add($"relationship source {relationship.SourceSeedEntityId} is missing");
            }

            if (!entityIds.Contains(relationship.TargetSeedEntityId))
            {
                issues.Add($"relationship target {relationship.TargetSeedEntityId} is missing");
            }
        }

        return new SeedWorldQualityReport(issues);
    }

    private static string EntityLabel(DomainSeedEntity entity) =>
        entity.DisplayName ?? entity.SeedEntityId.ToString(CultureInfo.InvariantCulture);

    public static string TerrainFingerprint(DomainStartupSeed seed)
    {
        ArgumentNullException.ThrowIfNull(seed);
        return string.Join(
            '|',
            seed.Cells
                .OrderBy(cell => cell.Q)
                .ThenBy(cell => cell.R)
                .Select(cell => $"{cell.Q},{cell.R}:{cell.Terrain}:{cell.OwnerSeedEntityId}"));
    }

    public static string EntityPlacementFingerprint(DomainStartupSeed seed)
    {
        ArgumentNullException.ThrowIfNull(seed);
        return string.Join(
            '|',
            seed.Entities
                .OrderBy(entity => entity.SeedEntityId)
                .Select(entity =>
                    $"{entity.SeedEntityId}:{entity.Kind}:{entity.DisplayName}:{entity.PhysicalQ},{entity.PhysicalR}:{entity.SemanticQ},{entity.SemanticR},{entity.SemanticS}"));
    }
}

internal sealed class SeedWorldQualityReport
{
    public SeedWorldQualityReport(IReadOnlyList<string> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);
        Issues = issues;
    }

    public IReadOnlyList<string> Issues { get; }

    public bool IsValid => Issues.Count == 0;
}
