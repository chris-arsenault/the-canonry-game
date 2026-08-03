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
            ValidateKnowledgeSiteOrder(context.DomainIds, site.TechId, siteIndex);
            foreach (var grant in site.Grants)
            {
                ValidateKnowledgeGrant(grant);
                context.RegisterKnowledgeGrant(
                    siteIndex,
                    KnowledgeGrantDomainCode(grant.Domain),
                    ResolveKnowledgeGrantTarget(context.DomainIds, grant),
                    FixedScalar.FromSingle(grant.Multiplier));
            }
        }
    }

    // Content gates resolve tech ids through the compiled catalog, whose order
    // defines the kernel site index (id - 1). A seed listing sites in a
    // different order would silently point every gate at the wrong tech.
    private static void ValidateKnowledgeSiteOrder(
        DomainIdRegistry ids,
        string techId,
        int siteIndex)
    {
        if (ids.KnowledgeTechCount == 0)
        {
            return;
        }

        var catalogId = ids.KnowledgeTechIdOf(techId).Value;
        if (catalogId != siteIndex + 1)
        {
            throw new InvalidOperationException(
                $"Knowledge site '{techId}' installs at site index {siteIndex} but the compiled catalog places it at {catalogId - 1}.");
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
        context.BeginPhysicalGeographyInstall(blueprint.Radius);
        var facts = new byte[PhysicalHexFactCodes.Count];
        foreach (var cell in blueprint.Cells)
        {
            DomainPhysicalGeographyFacts.Quantize(cell, facts);
            context.InstallPhysicalGeographyCell(
                cell.Q,
                cell.R,
                context.DomainIds.TerrainIdOf(cell.BaseTerrain).Value,
                cell.SuperregionId,
                cell.ContinentId,
                cell.CrossingWord,
                cell.FieldWord,
                cell.InitialCoverTerrain is null
                    ? 0
                    : context.DomainIds.TerrainIdOf(cell.InitialCoverTerrain).Value,
                facts);
        }

        InstallGeologicalFeatures(context, blueprint);
    }

    private static void InstallGeologicalFeatures(
        KernelConfigurationContext context,
        DomainPhysicalGeographyBlueprint blueprint)
    {
        foreach (var feature in blueprint.GeologicalFeatures)
        {
            context.InstallGeologicalFeature(
                feature.Kind,
                feature.AnchorQ,
                feature.AnchorR,
                feature.Magnitude,
                feature.HexCount);
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
            KnowledgeGrantDomain.GeneratorFireWeight => ids.GeneratorIdOf(RequiredTarget(grant)).Value,
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
            // Physical cells emit state-changed plus cell-created, and cells
            // whose latent geography declares an initial cover emit one more
            // cover change; estimate the ceiling.
            PlaneCodes.Physical => 3,
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
