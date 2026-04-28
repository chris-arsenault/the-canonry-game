namespace Engine.Host.Tests;

using Engine.Domain;
using Engine.Domain.Compilation;
using Engine.Eventing.Schema;
using Engine.Host;
using Engine.Kernel;
using Engine.Kernel.Programs;
using Engine.Projection;

using IceRemembers;
using IceRemembers.Seed;

public sealed class KernelHostBuilderTests
{
    [Fact]
    public void Build_CanAttachExistingKernelWhenNoSeedIsEmitted()
    {
        var kernel = new Simulation();
        kernel.RegisterTerrain(1, boundaryGroupId: 1);
        kernel.World.SetCell(0, 0, terrainId: 1);

        using var host = new KernelHostBuilder()
            .WithKernel(kernel)
            .Build();

        Assert.Same(kernel.Events, host.Events);
        Assert.True(host.BootstrapCompleted);
    }

    [Fact]
    public void Build_CanRestoreKernelState()
    {
        var kernel = new Simulation();
        kernel.World.SetMapRadius(1);
        kernel.RegisterTerrain(1, boundaryGroupId: 1);
        kernel.CreateCell(0, 0, terrainId: 1, ownerEntityId: 0);
        _ = kernel.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, tick: 0);

        using var state = new MemoryStream();
        KernelSnapshot.SaveKernelState(kernel, state);
        state.Position = 0;
        var map = new HexMapProjection();
        var ui = new UiProjectionState();

        using var host = new KernelHostBuilder()
            .WithKernelState(state)
            .RegisterProjection("map", _ => map)
            .RegisterProjection("ui", _ => ui)
            .Build();

        Assert.True(map.TryGetHex(new HexCoord(0, 0), out var origin));
        Assert.Equal(1, origin.TerrainId);
        Assert.True(ui.Entities.ContainsKey(1));
        Assert.True(host.SeedEventCount > 0);
    }

    [Fact]
    public void Build_WithDomainConfigUsesCompiledCatalogForKernelCodesAndSymbols()
    {
        using var host = new KernelHostBuilder()
            .WithDomainConfig(IceRemembersDefinition.Create())
            .Build();

        Assert.NotNull(host.CompiledDomain);
        Assert.Equal(
            host.CompiledDomain.Ids.KindIdOf(IceRemembersKinds.Settlement),
            host.DomainIds.KindIdOf(IceRemembersKinds.Settlement));
        Assert.True(host.Symbols.TryResolve(
            ProjectionSymbolKind.Kind,
            host.DomainIds.KindIdOf(IceRemembersKinds.Settlement).Value,
            out var symbol));
        Assert.Equal(IceRemembersKinds.Settlement.Value, symbol.StableName);
        Assert.True(host.Symbols.TryResolve(
            ProjectionSymbolKind.Attribute,
            host.DomainIds.AttributeIdOf(IceRemembersAttributeId.Residents.ToDomainName()).Value,
            out var attributeSymbol));
        Assert.Equal("Residents", attributeSymbol.DisplayName);
        Assert.True(host.Symbols.TryResolve(
            ProjectionSymbolKind.SourceCode,
            host.DomainIds.SourceCodeIdOf(DomainRuntimeNames.UpkeepSourceCode).Value,
            out var upkeepSymbol));
        Assert.Equal(DomainRuntimeNames.UpkeepSourceCode, upkeepSymbol.StableName);
        Assert.True(host.CompiledDomain.Programs.Generators.Generators.Count > 0);
        var processSymbolSeed = host.CompiledDomain.Symbols.First(symbol => symbol.Kind == DomainSymbolKind.Process);
        Assert.True(host.Symbols.TryResolve(
            ProjectionSymbolKind.Process,
            processSymbolSeed.Id,
            out var processSymbol));
        Assert.Equal(processSymbolSeed.StableName, processSymbol.StableName);
        var detectorSymbolSeed = host.CompiledDomain.Symbols.First(symbol => symbol.Kind == DomainSymbolKind.Detector);
        Assert.True(host.Symbols.TryResolve(
            ProjectionSymbolKind.Detector,
            detectorSymbolSeed.Id,
            out var detectorSymbol));
        Assert.Equal(detectorSymbolSeed.StableName, detectorSymbol.StableName);
        var structuralFactSeed = host.CompiledDomain.Symbols.First(symbol => symbol.Kind == DomainSymbolKind.StructuralFact);
        Assert.True(host.Symbols.TryResolve(
            ProjectionSymbolKind.StructuralFact,
            structuralFactSeed.Id,
            out var structuralFactSymbol));
        Assert.Equal(structuralFactSeed.StableName, structuralFactSymbol.StableName);
        Assert.True(host.ProgramIsEnabled(KernelProgramCodes.Upkeep));
        Assert.True(host.ProgramIsEnabled(KernelProgramCodes.Generators));
        Assert.True(host.ProgramIsEnabled(KernelProgramCodes.Degeneracy));
    }

    [Fact]
    public void Build_AppliesProgramOverridesBeforeStartup()
    {
        using var host = new KernelHostBuilder()
            .WithDomainConfig(IceRemembersDefinition.Create())
            .ConfigurePrograms(programs => programs.Disable(KernelProgramCodes.Pressures))
            .Build();

        Assert.False(host.ProgramIsEnabled(KernelProgramCodes.Pressures));
    }

    [Fact]
    public void Build_RunsGenericStartupSeederAfterConsumersSubscribe()
    {
        var map = new HexMapProjection();
        var ui = new UiProjectionState();

        using var host = BuildGenericStartupHost(map, ui);

        Assert.True(host.SeedEventCount > 0);
        Assert.Equal(host.SeedEventCount, host.Events.NextSequence);
        Assert.True(host.BootstrapCompleted);

        AssertGenericStartupProjection(host, map, ui);
        AssertConsumersSeeded(host);
        Assert.Throws<InvalidOperationException>(() =>
            host.Consumers.RegisterProjection("late-map", new HexMapProjection()));
    }

    private static KernelHost BuildGenericStartupHost(HexMapProjection map, UiProjectionState ui) =>
        new KernelHostBuilder()
            .RegisterDomain(RegisterGenericStartupDomain)
            .ConfigureSymbols(ConfigureGenericStartupSymbols)
            .ConfigureKernel(ConfigureGenericStartupKernel)
            .WithSimulationOptions(new SimulationOptions
            {
                EventCapacity = 32,
                EventCursorCapacity = 8,
                EventSchemaValidator = KernelEventSchemaRegistry.Validator,
            })
            .WithSeedHeadroom(0)
            .WithStartupSeeder(estimatedEventCount: 6, seed: SeedGenericStartup)
            .RegisterConsumers(context =>
            {
                context.RegisterProjection("map", map);
                context.RegisterProjection("ui", ui);
            })
            .Build();

    private static void RegisterGenericStartupDomain(DomainIdRegistry domain)
    {
        _ = domain.RegisterKind("test_kind");
        _ = domain.RegisterTerrain("test_terrain");
        _ = domain.RegisterSubtype("test_subtype");
        _ = domain.RegisterCulture("test_culture");
        _ = domain.RegisterAttribute("test_attribute");
        _ = domain.RegisterResource("test_resource");
        _ = domain.RegisterRelationshipKind("test_relationship");
        _ = domain.RegisterSourceCode("process");
    }

    private static void ConfigureGenericStartupSymbols(KernelSymbolContext symbols)
    {
        symbols.RegisterSymbol(ProjectionSymbolKind.Kind, 1, "test_kind", "Test Kind");
        symbols.RegisterSymbol(ProjectionSymbolKind.Terrain, 1, "test_terrain", "Test Terrain");
        symbols.RegisterEntityName(1, "Seeded Entity");
    }

    private static void ConfigureGenericStartupKernel(KernelConfigurationContext kernel)
    {
        kernel.SetMapRadius(1);
        kernel.RegisterTerrain(1, boundaryGroupId: 1);
        kernel.RegisterAttribute(
            kindId: kernel.DomainIds.KindIdOf("test_kind").Value,
            attributeId: kernel.DomainIds.AttributeIdOf("test_attribute"),
            minimum: 0f,
            maximum: 100f,
            defaultValue: 10f);
        kernel.RegisterRelationshipKind(
            kernel.DomainIds.RelationshipKindIdOf("test_relationship"),
            cullThreshold: 0.05f);
    }

    private static void SeedGenericStartup(KernelStartupContext startup)
    {
        var terrainId = startup.DomainIds.TerrainIdOf("test_terrain").Value;
        var resourceId = startup.DomainIds.ResourceIdOf("test_resource").Value;
        var kindId = startup.DomainIds.KindIdOf("test_kind").Value;
        var subtypeId = startup.DomainIds.SubtypeIdOf("test_subtype").Value;
        var cultureId = startup.DomainIds.CultureIdOf("test_culture").Value;
        var relationshipKindId = startup.DomainIds.RelationshipKindIdOf("test_relationship").Value;

        startup.SeedCell(0, 0, terrainId, ownerEntityId: 1, [new CellResourceSeed(resourceId, 3f)]);
        startup.SeedEntity(1, kindId, subtypeId, cultureId, physicalQ: 0, physicalR: 0, semanticQ: 0, semanticR: 0, semanticS: 0);
        startup.SeedAttribute(1, startup.DomainIds.AttributeIdOf("test_attribute").Value, value: 25f);
        startup.SeedRelationship(
            relationshipKindId,
            RelationshipPolarityCodes.Positive,
            sourceEntityId: 1,
            targetEntityId: 1,
            factId0: 0,
            factId1: 0,
            factId2: 0,
            factId3: 0,
            factCount: 0,
            strength: 0.5f);
    }

    private static void AssertGenericStartupProjection(
        KernelHost host,
        HexMapProjection map,
        UiProjectionState ui)
    {
        Assert.True(map.TryGetHex(new HexCoord(0, 0), out var origin));
        Assert.Equal(host.DomainIds.TerrainIdOf("test_terrain").Value, origin.TerrainId);
        Assert.Equal(1, origin.OwnerEntityId);

        Assert.True(ui.Entities.TryGetValue(1, out var entity));
        Assert.Equal(1, entity.KindId);
        Assert.Equal(25f, entity.Attributes[host.DomainIds.AttributeIdOf("test_attribute").Value].NewValue);
        Assert.True(ui.Resources.ContainsKey(new UiResourceKey(host.DomainIds.ResourceIdOf("test_resource").Value, 0, 0)));
        Assert.True(ui.Relationships.ContainsKey(new UiRelationshipKey(
            1,
            1,
            host.DomainIds.RelationshipKindIdOf("test_relationship").Value)));
    }

    private static void AssertConsumersSeeded(KernelHost host)
    {
        Assert.All(host.Consumers.Registrations, registration =>
        {
            Assert.Equal(host.SeedEventCount, registration.NextSequence);
            Assert.Equal(0, registration.DroppedEvents);
        });
        Assert.True(host.Consumers.IsSealed);
    }

    [Fact]
    public void Build_RejectsEventLogTooSmallForStartupSeedBurst()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new KernelHostBuilder()
                .WithSimulationOptions(new SimulationOptions
                {
                    EventCapacity = 1,
                    EventCursorCapacity = 4,
                    EventSchemaValidator = KernelEventSchemaRegistry.Validator,
                })
                .WithSeedHeadroom(0)
                .WithStartupSeeder(estimatedEventCount: 2, seed: _ => { })
                .Build());

        Assert.Contains("startup seed burst", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_WithDomainStartupSeedHydratesKernelAndSeedProjections()
    {
        var startupSeed = IceRemembersSeedBuilder.BuildStartupSeed(seed: 42);
        var map = new HexMapProjection();
        var ui = new UiProjectionState();

        using var host = new KernelHostBuilder()
            .WithDomainConfig(IceRemembersDefinition.Create())
            .WithDomainStartupSeed(startupSeed)
            .RegisterProjection("map", _ => map)
            .RegisterProjection("ui", _ => ui)
            .Build();

        Assert.Equal(startupSeed.Cells.Count, map.HexesById.Count);
        Assert.Equal(host.SeedEventCount, host.Events.NextSequence);

        var frosthavenSeed = startupSeed.Entities.Single(entity => entity.DisplayName == "Frosthaven");
        var frosthavenEntityId = StartupEntityId(startupSeed, frosthavenSeed.SeedEntityId);
        Assert.True(ui.Entities.TryGetValue(frosthavenEntityId, out var frosthaven));
        Assert.Equal(host.DomainIds.KindIdOf(IceRemembersKinds.Settlement).Value, frosthaven.KindId);

        var displayName = host.Symbols.ResolveEntityName(new ProjectionEntityNameKey(
            frosthaven.EntityId,
            frosthaven.KindId,
            frosthaven.SubtypeId,
            frosthaven.CultureId,
            frosthaven.NameSeed));
        Assert.Equal("Frosthaven", displayName);

        var startupSourceCodeId = host.DomainIds.SourceCodeIdOf(DomainRuntimeNames.StartupSourceCode).Value;
        Assert.NotEmpty(frosthaven.Attributes);
        Assert.All(frosthaven.Attributes.Values, attribute =>
            Assert.Equal(startupSourceCodeId, attribute.SourceCodeId));
        Assert.Contains(ui.Resources.Values, resource =>
            resource.SourceCodeId == startupSourceCodeId && resource.NewValue > 0f);
        Assert.Contains(ui.Relationships.Keys, relationship =>
            relationship.RelationshipKindId == host.DomainIds
                .RelationshipKindIdOf(IceRemembersRelationship.ControlledBy.ToDomainName())
                .Value);
    }

    [Fact]
    public void Build_DomainStartupSeedCannotBeCombinedWithLegacyStartupSeeder()
    {
        var seed = Engine.Domain.Startup.DomainStartupSeed.Empty(1);

        var builder = new KernelHostBuilder()
            .WithDomainStartupSeed(seed);

        Assert.Throws<InvalidOperationException>(() =>
            builder.WithStartupSeeder(estimatedEventCount: 0, seed: _ => { }));

        Assert.Throws<InvalidOperationException>(() =>
            new KernelHostBuilder()
                .WithStartupSeeder(estimatedEventCount: 0, seed: _ => { })
                .WithDomainStartupSeed(seed));
    }

    [Fact]
    public void Build_DomainStartupSeedRequiresCompiledDomain()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new KernelHostBuilder()
                .WithDomainStartupSeed(Engine.Domain.Startup.DomainStartupSeed.Empty(1))
                .Build());

        Assert.Contains("compiled domain", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StartupSeedApisDoNotExposeSourceCodeOverloads()
    {
        var forbidden = typeof(KernelStartupContext)
            .GetMethods()
.Where(method => method.Name.StartsWith("Seed", StringComparison.Ordinal) && method.GetParameters().Any(parameter =>
                string.Equals(parameter.Name, "sourceCodeId", StringComparison.Ordinal) ||
                string.Equals(parameter.Name, "attributeSourceCodeId", StringComparison.Ordinal))).Select(method => method.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(forbidden);
    }

    private static int StartupEntityId(Engine.Domain.Startup.DomainStartupSeed seed, int seedEntityId)
    {
        var orderedSeedIds = seed.Entities
            .Select(entity => entity.SeedEntityId)
            .Order()
            .ToArray();
        var index = Array.IndexOf(orderedSeedIds, seedEntityId);
        Assert.True(index >= 0, $"Seed entity ID {seedEntityId} was not present.");
        return index + 1;
    }
}
