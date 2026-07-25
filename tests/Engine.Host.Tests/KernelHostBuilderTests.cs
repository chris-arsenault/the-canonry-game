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

// Slow: builds a full startup seed and kernel host; excluded from the fast loop.
[Trait("Category", "Slow")]
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

        Assert.True(map.TryGetHex(PlaneCodes.Physical, new HexCoord(0, 0), out var origin));
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
            host.DomainIds.AttributeIdOf(IceRemembersAttributeId.TempHp.ToDomainName()).Value,
            out var attributeSymbol));
        Assert.Equal("Temp HP", attributeSymbol.DisplayName);
        Assert.NotEmpty(host.CompiledDomain.Programs.Generators.Generators);
        var verbSymbolSeed = host.CompiledDomain.Symbols.First(symbol => symbol.Kind == DomainSymbolKind.Verb);
        Assert.True(host.Symbols.TryResolve(
            ProjectionSymbolKind.Verb,
            verbSymbolSeed.Id,
            out var verbSymbol));
        Assert.Equal(verbSymbolSeed.StableName, verbSymbol.StableName);
        var structuralFactSeed = host.CompiledDomain.Symbols.First(symbol => symbol.Kind == DomainSymbolKind.StructuralFact);
        Assert.True(host.Symbols.TryResolve(
            ProjectionSymbolKind.StructuralFact,
            structuralFactSeed.Id,
            out var structuralFactSymbol));
        Assert.Equal(structuralFactSeed.StableName, structuralFactSymbol.StableName);
        var causewayId = host.DomainIds.TravelStructureIdOf(IceRemembersTravelStructure.IceCauseway.ToDomainName()).Value;
        Assert.True(host.Symbols.TryResolve(
            ProjectionSymbolKind.TravelStructure,
            causewayId,
            out var travelStructureSymbol));
        Assert.Equal(IceRemembersTravelStructure.IceCauseway.ToDomainName(), travelStructureSymbol.StableName);
        Assert.True(host.ProgramIsEnabled(KernelProgramCodes.LowAltitudeVerbs));
        Assert.True(host.ProgramIsEnabled(KernelProgramCodes.Effects));
        Assert.True(host.ProgramIsEnabled(KernelProgramCodes.Gambits));
        Assert.True(host.ProgramIsEnabled(KernelProgramCodes.Generators));
        Assert.NotEmpty(host.CompiledDomain.Programs.Effects.AttributeDeltaEffects);
    }

    [Fact]
    public void Build_AppliesProgramOverridesBeforeStartup()
    {
        using var host = new KernelHostBuilder()
            .WithDomainConfig(IceRemembersDefinition.Create())
            .ConfigurePrograms(programs => programs.Disable(KernelProgramCodes.Effects))
            .Build();

        Assert.False(host.ProgramIsEnabled(KernelProgramCodes.Effects));
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
            .WithStartupSeeder(estimatedEventCount: 5, seed: SeedGenericStartup)
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
        _ = domain.RegisterRelationshipKind("test_relationship");
        _ = domain.RegisterSourceCode("behavior");
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
        kernel.RegisterTerrain(
            1,
            1,
            FixedScalar.One,
            FixedScalar.One,
            FixedScalar.One,
            FixedScalar.One,
            FixedScalar.One,
            FixedScalar.One);
        kernel.RegisterAttribute(
            kindId: kernel.DomainIds.KindIdOf("test_kind").Value,
            attributeId: kernel.DomainIds.AttributeIdOf("test_attribute"),
            minimum: FixedScalar.FromSingle(0f),
            maximum: FixedScalar.FromSingle(100f),
            defaultValue: FixedScalar.FromSingle(10f));
        kernel.RegisterRelationshipKind(
            kernel.DomainIds.RelationshipKindIdOf("test_relationship"),
            cullThreshold: FixedScalar.FromSingle(0.05f));
    }

    private static void SeedGenericStartup(KernelStartupContext startup)
    {
        var terrainId = startup.DomainIds.TerrainIdOf("test_terrain").Value;
        var kindId = startup.DomainIds.KindIdOf("test_kind").Value;
        var subtypeId = startup.DomainIds.SubtypeIdOf("test_subtype").Value;
        var cultureId = startup.DomainIds.CultureIdOf("test_culture").Value;
        var relationshipKindId = startup.DomainIds.RelationshipKindIdOf("test_relationship").Value;

        startup.SeedCell(0, 0, terrainId, ownerEntityId: 1);
        startup.SeedEntity(1, kindId, subtypeId, cultureId, physicalQ: 0, physicalR: 0, semanticQ: 0, semanticR: 0, semanticS: 0);
        startup.SeedAttribute(1, startup.DomainIds.AttributeIdOf("test_attribute").Value, value: 25f);
        startup.SeedRelationship(
            relationshipKindId,
            RelationshipPolarityCodes.Positive,
            sourceEntityId: 1,
            targetEntityId: 1,
            strength: 0.5f);
    }

    private static void AssertGenericStartupProjection(
        KernelHost host,
        HexMapProjection map,
        UiProjectionState ui)
    {
        Assert.True(map.TryGetHex(PlaneCodes.Physical, new HexCoord(0, 0), out var origin));
        Assert.Equal(host.DomainIds.TerrainIdOf("test_terrain").Value, origin.TerrainId);
        Assert.Equal(1, origin.OwnerEntityId);

        Assert.True(ui.Entities.TryGetValue(1, out var entity));
        Assert.Equal(1, entity.KindId);
        Assert.Equal(25f, entity.Attributes[host.DomainIds.AttributeIdOf("test_attribute").Value].NewValue);
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

        Assert.Equal(
            startupSeed.Cells.Count(cell => cell.PlaneCode == PlaneCodes.Physical),
            map.HexesById(PlaneCodes.Physical).Count);
        Assert.Equal(
            startupSeed.Cells.Count(cell => cell.PlaneCode == PlaneCodes.Semantic),
            map.HexesById(PlaneCodes.Semantic).Count);
        Assert.Equal(host.SeedEventCount, host.Events.NextSequence);
        Assert.True(host.Symbols.TryResolve(ProjectionSymbolKind.Continent, 1, out var continentSymbol));
        Assert.Equal("The Near Shelf", continentSymbol.DisplayName);
        Assert.True(host.Symbols.TryResolve(ProjectionSymbolKind.Superregion, 21, out var superregionSymbol));
        Assert.Equal("Farwake Reach", superregionSymbol.DisplayName);

        var auroraStackSeed = startupSeed.Entities.Single(entity => entity.DisplayName == "Aurora Stack");
        var auroraStackEntityId = StartupEntityId(startupSeed, auroraStackSeed.SeedEntityId);
        Assert.True(ui.Entities.TryGetValue(auroraStackEntityId, out var auroraStack));
        Assert.Equal(host.DomainIds.KindIdOf(IceRemembersKinds.Settlement).Value, auroraStack.KindId);

        var displayName = host.Symbols.ResolveEntityName(new ProjectionEntityNameKey(
            auroraStack.EntityId,
            auroraStack.KindId,
            auroraStack.SubtypeId,
            auroraStack.CultureId,
            auroraStack.NameSeed));
        Assert.Equal("Aurora Stack", displayName);
        var referenceName = host.Symbols.ResolveEntityReferenceName(new ProjectionEntityNameKey(
            auroraStack.EntityId,
            auroraStack.KindId,
            auroraStack.SubtypeId,
            auroraStack.CultureId,
            auroraStack.NameSeed));
        Assert.Equal("Aurora", referenceName);

        var startupSourceCodeId = host.DomainIds.SourceCodeIdOf(DomainRuntimeNames.StartupSourceCode).Value;
        Assert.NotEmpty(auroraStack.Attributes);
        Assert.All(auroraStack.Attributes.Values, attribute =>
            Assert.Equal(startupSourceCodeId, attribute.SourceCodeId));
        Assert.InRange(
            auroraStack.Attributes[host.DomainIds.AttributeIdOf(IceRemembersAttributeId.TempHp.ToDomainName()).Value].NewValue,
            80f,
            100f);
        Assert.Single(auroraStack.Attributes);
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
