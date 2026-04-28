namespace Engine.Host;

using Engine.Domain;
using Engine.Domain.Compilation;
using Engine.Domain.Startup;
using Engine.Graph;

internal static class DomainStartupSeedInstaller
{
    public static int EstimateEventCount(DomainStartupSeed seed, CompiledDomain compiledDomain)
    {
        ArgumentNullException.ThrowIfNull(seed);
        ArgumentNullException.ThrowIfNull(compiledDomain);

        var attributeRulesByKindId = compiledDomain.KernelData.Entities.Attributes
            .GroupBy(rule => rule.KindId)
            .ToDictionary(group => group.Key, group => group.Count());

        var count = checked(
            seed.Cells.Count +
            seed.Resources.Count(resource => MathF.Abs(resource.Value) > float.Epsilon) +
            seed.Relationships.Count +
            seed.Attributes.Count);

        foreach (var entity in seed.Entities)
        {
            var kindId = compiledDomain.Ids.KindIdOf(entity.Kind).Value;
            count = checked(count + 1 + attributeRulesByKindId.GetValueOrDefault(kindId));
        }

        return count;
    }

    public static void ConfigureKernel(KernelConfigurationContext context, DomainStartupSeed seed)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(seed);
        context.SetMapRadius(seed.MapRadius);
    }

    public static void Install(KernelStartupContext context, DomainStartupSeed seed)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(seed);

        var entityIds = BuildEntityIdMap(seed);
        RegisterEntityNames(context, seed, entityIds);
        SeedEntities(context, seed, entityIds);
        SeedCells(context, seed, entityIds);
        SeedResources(context, seed);
        SeedAttributes(context, seed, entityIds);
        SeedRelationships(context, seed, entityIds);
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
                context.Symbols.RegisterEntityName(entityIds[entity.SeedEntityId], entity.DisplayName);
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
                cell.Q,
                cell.R,
                context.DomainIds.TerrainIdOf(cell.Terrain).Value,
                ResolveOptionalEntityId(entityIds, cell.OwnerSeedEntityId));
        }
    }

    private static void SeedResources(KernelStartupContext context, DomainStartupSeed seed)
    {
        foreach (var resource in seed.Resources)
        {
            if (MathF.Abs(resource.Value) <= float.Epsilon)
            {
                continue;
            }

            context.SeedResource(
                context.DomainIds.ResourceIdOf(resource.Resource).Value,
                resource.Q,
                resource.R,
                resource.Value);
        }
    }

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
        Span<int> factIds = stackalloc int[4];
        foreach (var relationship in seed.Relationships)
        {
            var facts = relationship.Facts ?? [];
            if (facts.Count > 4)
            {
                throw new InvalidOperationException(
                    $"Relationship '{relationship.RelationshipKind}' has {facts.Count} facts; kernel startup supports at most 4.");
            }

            factIds.Clear();
            for (var i = 0; i < facts.Count; i++)
            {
                factIds[i] = context.DomainIds.RelationshipFactIdOf(facts[i]).Value;
            }

            context.SeedRelationship(
                context.DomainIds.RelationshipKindIdOf(relationship.RelationshipKind).Value,
                PolarityCode(relationship.Polarity),
                entityIds[relationship.SourceSeedEntityId],
                entityIds[relationship.TargetSeedEntityId],
                factIds[0],
                factIds[1],
                factIds[2],
                factIds[3],
                facts.Count,
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
}
