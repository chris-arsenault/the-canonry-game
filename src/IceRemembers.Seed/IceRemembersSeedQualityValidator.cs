namespace IceRemembers.Seed;

using System.Globalization;

using Engine.Constants;
using Engine.Domain.Startup;
using Engine.WorldGen;

internal static class IceRemembersSeedQualityValidator
{
    public static SeedWorldQualityReport Validate(DomainStartupSeed seed)
    {
        ArgumentNullException.ThrowIfNull(seed);

        var issues = new List<string>();
        ValidateSeedHasContent(seed, issues);
        ValidateEntityPositions(seed, issues);
        ValidateRelationships(seed, issues);

        return new SeedWorldQualityReport(issues);
    }

    private static void ValidateSeedHasContent(DomainStartupSeed seed, List<string> issues)
    {
        if (seed.Cells.Count == 0)
        {
            issues.Add("startup seed has no cells");
        }

        if (seed.Entities.Count == 0)
        {
            issues.Add("startup seed has no entities");
        }
    }

    private static void ValidateEntityPositions(DomainStartupSeed seed, List<string> issues)
    {
        var physicalCellsByCoord = seed.Cells
            .Where(cell => cell.PlaneCode == PlaneCodes.Physical)
            .ToDictionary(cell => new HexCoord(cell.Q, cell.R));
        var terrainNames = IceRemembersTerrainCatalog.DomainTerrains
            .Select(terrain => terrain.Name)
            .ToHashSet(StringComparer.Ordinal);

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
            ValidateEntityPosition(entity, physicalCellsByCoord, issues);
        }

        foreach (var cell in seed.Cells)
        {
            ValidateCellTerrain(cell, terrainNames, issues);
        }
    }

    private static void ValidateEntityPosition(
        DomainSeedEntity entity,
        Dictionary<HexCoord, DomainSeedCell> cellsByCoord,
        List<string> issues)
    {
        var coord = new HexCoord(entity.PhysicalQ, entity.PhysicalR);
        if (!cellsByCoord.TryGetValue(coord, out var cell))
        {
            issues.Add($"{EntityLabel(entity)} is outside the grid at {coord}");
            return;
        }

        ValidatePhysicalEntityTerrain(coord, cell, issues);
    }

    private static void ValidatePhysicalEntityTerrain(
        HexCoord coord,
        DomainSeedCell cell,
        List<string> issues)
    {
        var terrainCatalog = IceRemembersTerrainCatalog.CreateWorldGenCatalog();
        try
        {
            _ = terrainCatalog.Resolve(cell.Terrain);
        }
        catch (KeyNotFoundException)
        {
            issues.Add($"unknown terrain '{cell.Terrain}' at {coord}");
        }
    }

    private static void ValidateCellTerrain(
        DomainSeedCell cell,
        HashSet<string> terrainNames,
        List<string> issues)
    {
        if (!terrainNames.Contains(cell.Terrain))
        {
            issues.Add($"unknown terrain '{cell.Terrain}' on plane {cell.PlaneCode} at {cell.Q},{cell.R}");
        }
    }

    private static void ValidateRelationships(DomainStartupSeed seed, List<string> issues)
    {
        var entityIds = seed.Entities.Select(entity => entity.SeedEntityId).ToHashSet();
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
                .ThenBy(cell => cell.PlaneCode)
                .Select(cell => $"{cell.PlaneCode}:{cell.Q},{cell.R}:{cell.Terrain}:{cell.OwnerSeedEntityId}"));
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
