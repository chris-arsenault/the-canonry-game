namespace Engine.Host;

using Engine.Constants;
using Engine.Domain;
using Engine.Domain.Compilation;
using Engine.Domain.Startup;
using Engine.Gameplay;
using Engine.Graph;
using Engine.Projection;

internal static class DomainStartupSeedInstaller
{
    public static int EstimateEventCount(DomainStartupSeed seed, CompiledDomain compiledDomain)
    {
        ArgumentNullException.ThrowIfNull(seed);
        ArgumentNullException.ThrowIfNull(compiledDomain);

        var attributeRulesByKindId = compiledDomain.KernelData.Entities.Attributes
            .GroupBy(rule => rule.KindId)
            .ToDictionary(group => group.Key, group => group.Count());

        var cellEventCount = seed.Cells.Sum(StartupCellEventCount);
        var count = checked(
            cellEventCount +
            seed.Relationships.Count +
            seed.Attributes.Count);

        foreach (var entity in seed.Entities)
        {
            var kindId = compiledDomain.Ids.KindIdOf(entity.Kind).Value;
            count = checked(count + 1 + attributeRulesByKindId.GetValueOrDefault(kindId));
        }

        return count;
    }

    public static void ConfigureKernel(
        KernelConfigurationContext context,
        DomainStartupSeed seed,
        bool configureMap)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(seed);
        if (configureMap)
        {
            context.SetMapRadius(seed.MapRadius);
            InstallPhysicalGeography(context, seed.PhysicalGeography);
            InstallSemanticTerrain(context, seed.SemanticTerrain);
        }

        foreach (var site in seed.KnowledgeSites)
        {
            var siteIndex = context.RegisterKnowledgeSite(site.Q, site.R, site.Tier);
            foreach (var grant in site.Grants)
            {
                ValidateKnowledgeGrant(grant);
                context.RegisterKnowledgeGrant(
                    siteIndex,
                    KnowledgeGrantDomainCode(grant.Domain),
                    ResolveKnowledgeGrantTarget(context.DomainIds, grant),
                    grant.Multiplier);
            }
        }
    }

    private static void InstallPhysicalGeography(
        KernelConfigurationContext context,
        DomainPhysicalGeographyBlueprint? blueprint)
    {
        if (blueprint is null || blueprint.Radius == 0)
        {
            return;
        }

        blueprint.Validate();
        var materialIds = blueprint.MaterialPalette
            .Select(name => context.DomainIds.TerrainIdOf(name).Value)
            .ToArray();
        context.BeginPhysicalGeographyInstall(blueprint.Radius);
        global::System.Span<int> cellMaterialIds = stackalloc int[PhysicalGeographyCodes.MicrotrianglesPerHex];
        global::System.Span<int> elevations = stackalloc int[PhysicalGeographyCodes.MicrotrianglesPerHex];
        global::System.Span<int> hydrologyFlags = stackalloc int[PhysicalGeographyCodes.MicrotrianglesPerHex];
        foreach (var cell in blueprint.Cells)
        {
            for (var triangleIndex = 0; triangleIndex < PhysicalGeographyCodes.MicrotrianglesPerHex; triangleIndex++)
            {
                var triangle = blueprint.Microtriangles[cell.MicrotriangleOffset + triangleIndex];
                cellMaterialIds[triangleIndex] = materialIds[triangle.MaterialIndex];
                elevations[triangleIndex] = triangle.Elevation;
                hydrologyFlags[triangleIndex] = triangle.HydrologyFlags;
            }

            context.InstallPhysicalGeographyCell(
                cell.Q,
                cell.R,
                context.DomainIds.TerrainIdOf(cell.BaseTerrain).Value,
                cell.SuperregionId,
                cell.ContinentId,
                cell.MeanElevation,
                cell.MaxElevation,
                cell.Roughness,
                cell.WaterCoverage,
                cell.RiverEdgeMask,
                cell.CoastEdgeMask,
                cellMaterialIds,
                elevations,
                hydrologyFlags);
        }
    }

    private static void InstallSemanticTerrain(
        KernelConfigurationContext context,
        DomainSemanticTerrainBlueprint? blueprint)
    {
        if (blueprint is null || blueprint.Radius == 0)
        {
            return;
        }

        blueprint.Validate();
        context.BeginSemanticTerrainBlueprintInstall(blueprint.Radius);
        foreach (var cell in blueprint.Cells)
        {
            context.InstallSemanticTerrainBlueprintCell(
                cell.Q,
                cell.R,
                context.DomainIds.TerrainIdOf(cell.Terrain).Value,
                cell.SuperregionId,
                cell.ContinentId);
        }
    }

    private static void ValidateKnowledgeGrant(KnowledgeGrantConfig grant)
    {
        ArgumentNullException.ThrowIfNull(grant);
        if (grant.Multiplier <= 0f ||
            (grant.Target is not null && grant.TargetCode != 0) ||
            (grant.Target is not null && string.IsNullOrWhiteSpace(grant.Target)))
        {
            throw new InvalidOperationException($"Invalid knowledge grant for {grant.Domain}.");
        }
    }

    internal static int ResolveKnowledgeGrantTarget(DomainIdRegistry ids, KnowledgeGrantConfig grant) =>
        grant.Domain switch
        {
            KnowledgeGrantDomain.VerbWeight => ids.VerbIdOf(RequiredTarget(grant)).Value,
            KnowledgeGrantDomain.GeneratorPulseWeight => ids.GeneratorIdOf(RequiredTarget(grant)).Value,
            KnowledgeGrantDomain.GambitScore or KnowledgeGrantDomain.GambitSuccess =>
                ids.GambitIdOf(RequiredTarget(grant)).Value,
            KnowledgeGrantDomain.RelationshipMaintenance =>
                ids.RelationshipKindIdOf(RequiredTarget(grant)).Value,
            KnowledgeGrantDomain.MovementStepRate or KnowledgeGrantDomain.RoadWork when grant.Target is not null =>
                ids.KindIdOf(grant.Target).Value,
            _ when grant.Target is null => grant.TargetCode,
            _ => throw new InvalidOperationException($"Knowledge grant {grant.Domain} does not accept a named target."),
        };

    private static string RequiredTarget(KnowledgeGrantConfig grant) =>
        grant.Target ?? throw new InvalidOperationException($"Knowledge grant {grant.Domain} requires a target.");

    internal static int KnowledgeGrantDomainCode(KnowledgeGrantDomain domain)
    {
        if (domain <= KnowledgeGrantDomain.None || domain > KnowledgeGrantDomain.DecayTideResistance)
        {
            throw new ArgumentOutOfRangeException(nameof(domain), domain, "Unknown knowledge grant domain.");
        }

        return (int)domain;
    }

    public static void Install(KernelStartupContext context, DomainStartupSeed seed)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(seed);

        var entityIds = BuildEntityIdMap(seed);
        RegisterMapSubdivisionSymbols(context, seed);
        RegisterEntityNames(context, seed, entityIds);
        SeedEntities(context, seed, entityIds);
        SeedCells(context, seed, entityIds);
        SeedAttributes(context, seed, entityIds);
        SeedRelationships(context, seed, entityIds);
    }

    private static void RegisterMapSubdivisionSymbols(KernelStartupContext context, DomainStartupSeed seed)
    {
        foreach (var subdivision in seed.MapSubdivisions)
        {
            context.Symbols.RegisterSymbol(
                ToProjectionSymbolKind(subdivision.Level),
                subdivision.Id,
                subdivision.StableName,
                subdivision.DisplayName);
        }
    }

    private static void RegisterEntityNames(
        KernelStartupContext context,
        DomainStartupSeed seed,
        Dictionary<int, int> entityIds)
    {
        foreach (var entity in seed.Entities)
        {
            if (!string.IsNullOrWhiteSpace(entity.DisplayName))
            {
                context.Symbols.RegisterEntityName(
                    entityIds[entity.SeedEntityId],
                    entity.DisplayName,
                    entity.ReferenceName);
            }
        }
    }

    private static void SeedEntities(
        KernelStartupContext context,
        DomainStartupSeed seed,
        Dictionary<int, int> entityIds)
    {
        foreach (var entity in seed.Entities)
        {
            context.SeedEntity(
                entityIds[entity.SeedEntityId],
                context.DomainIds.KindIdOf(entity.Kind).Value,
                context.DomainIds.SubtypeIdOf(entity.Subtype).Value,
                context.DomainIds.CultureIdOf(entity.Culture).Value,
                entity.PhysicalQ,
                entity.PhysicalR,
                entity.SemanticQ,
                entity.SemanticR,
                entity.SemanticS);
        }
    }

    private static void SeedCells(
        KernelStartupContext context,
        DomainStartupSeed seed,
        Dictionary<int, int> entityIds)
    {
        foreach (var cell in seed.Cells)
        {
            context.SeedCell(
                cell.PlaneCode,
                cell.Q,
                cell.R,
                context.DomainIds.TerrainIdOf(cell.Terrain).Value,
                ResolveOptionalEntityId(entityIds, cell.OwnerSeedEntityId),
                cell.SuperregionId,
                cell.ContinentId);
        }
    }

    private static int StartupCellEventCount(DomainSeedCell cell) =>
        cell.PlaneCode switch
        {
            PlaneCodes.Physical => 2,
            PlaneCodes.Semantic => 1,
            _ => throw new InvalidOperationException($"Unknown seed cell plane code {cell.PlaneCode}."),
        };

    private static void SeedAttributes(
        KernelStartupContext context,
        DomainStartupSeed seed,
        Dictionary<int, int> entityIds)
    {
        foreach (var attribute in seed.Attributes)
        {
            context.SeedAttribute(
                entityIds[attribute.SeedEntityId],
                context.DomainIds.AttributeIdOf(attribute.Attribute).Value,
                attribute.Value);
        }
    }

    private static void SeedRelationships(
        KernelStartupContext context,
        DomainStartupSeed seed,
        Dictionary<int, int> entityIds)
    {
        foreach (var relationship in seed.Relationships)
        {
            context.SeedRelationship(
                context.DomainIds.RelationshipKindIdOf(relationship.RelationshipKind).Value,
                PolarityCode(relationship.Polarity),
                entityIds[relationship.SourceSeedEntityId],
                entityIds[relationship.TargetSeedEntityId],
                relationship.Strength);
        }
    }

    private static Dictionary<int, int> BuildEntityIdMap(DomainStartupSeed seed)
    {
        var entityIds = new Dictionary<int, int>();
        foreach (var entity in seed.Entities.OrderBy(entity => entity.SeedEntityId))
        {
            if (entity.SeedEntityId < 0)
            {
                throw new InvalidOperationException($"Seed entity IDs must be non-negative: {entity.SeedEntityId}.");
            }

            if (!entityIds.TryAdd(entity.SeedEntityId, entityIds.Count + 1))
            {
                throw new InvalidOperationException($"Duplicate seed entity ID {entity.SeedEntityId}.");
            }
        }

        return entityIds;
    }

    private static int ResolveOptionalEntityId(
        Dictionary<int, int> entityIds,
        int? seedEntityId)
    {
        if (seedEntityId is null)
        {
            return 0;
        }

        if (entityIds.TryGetValue(seedEntityId.Value, out var entityId))
        {
            return entityId;
        }

        throw new InvalidOperationException($"Seed references unknown entity ID {seedEntityId.Value}.");
    }

    private static int PolarityCode(RelationshipPolarity polarity) => (int)polarity;

    private static ProjectionSymbolKind ToProjectionSymbolKind(DomainMapSubdivisionLevel level) =>
        level switch
        {
            DomainMapSubdivisionLevel.Continent => ProjectionSymbolKind.Continent,
            DomainMapSubdivisionLevel.Superregion => ProjectionSymbolKind.Superregion,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown map subdivision level."),
        };
}
