namespace Engine.Host.Tests;

using Engine.Domain;
using Engine.Domain.Compilation;
using Engine.Eventing.Schema;
using Engine.Host;
using Engine.Kernel;
using Engine.Kernel.Programs;
using Engine.Projection;
using IceRemembers;

public sealed class KernelHostBuilderTests
{
    [Fact]
    public void Build_CanAttachExistingKernelWhenNoSeedIsEmitted()
    {
        var kernel = new Simulation();
        kernel.World.RegisterTerrain(1, passable: true, boundaryGroupId: 1);
        kernel.World.SetCell(0, 0, terrainId: 1, passable: true);

        using var host = new KernelHostBuilder()
            .WithKernel(kernel)
            .Build();

        Assert.Same(kernel, host.Kernel);
        Assert.Same(kernel.Events, host.Events);
        Assert.True(host.BootstrapCompleted);
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
        Assert.Equal(
            host.DomainIds.SourceCodeIdOf(DomainRuntimeNames.ProcessSourceCode).Value,
            host.Kernel.ProcessSourceCodeId);
        Assert.Equal(
            host.DomainIds.SourceCodeIdOf(DomainRuntimeNames.UpkeepSourceCode).Value,
            host.Kernel.UpkeepSourceCodeId);
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
        Assert.Equal(
            host.CompiledDomain.Programs.EntityData.Attributes.Count,
            host.Kernel.Upkeep.AttributeRuleCount);
        Assert.Equal(
            host.CompiledDomain.Programs.Upkeep.AttributeRules.Count,
            host.Kernel.Upkeep.UpkeepRuleCount);
        Assert.Equal(
            host.CompiledDomain.Programs.Upkeep.LifecycleRules.Count,
            host.Kernel.Upkeep.LifecycleRuleCount);
        Assert.Equal(
            host.CompiledDomain.Programs.Relationships.Relationships.Count,
            host.Kernel.Relationships.Count);
        Assert.Equal(
            host.CompiledDomain.Programs.Pressures.Pressures.Count,
            host.Kernel.Pressures.Count);
        Assert.True(host.CompiledDomain.Programs.Generators.Generators.Count > 0);
        Assert.Equal(
            host.CompiledDomain.Programs.Generators.Generators.Count,
            host.Kernel.Generators.Count);
        Assert.Equal(
            host.CompiledDomain.Programs.Degeneracy.Detectors.Count,
            host.Kernel.Degeneracy.Count);
        Assert.Equal(
            host.CompiledDomain.Programs.Processes.Objectives.Count,
            host.Kernel.Processes.ObjectiveCount);
        Assert.Equal(
            host.CompiledDomain.Programs.Processes.Processes.Count,
            host.Kernel.Processes.Count);
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
        Assert.False(host.Kernel.World.TerrainIsPassable(
            host.DomainIds.TerrainIdOf(IceRemembersTerrain.Ocean.ToDomainName()).Value));
        Assert.True(host.ProgramIsEnabled(KernelProgramCodes.Upkeep));
        Assert.True(host.ProgramIsEnabled(KernelProgramCodes.Generators));
        Assert.True(host.ProgramIsEnabled(KernelProgramCodes.Degeneracy));
        host.SetProgramEnabled(KernelProgramCodes.Pressures, enabled: false);
        Assert.False(host.ProgramIsEnabled(KernelProgramCodes.Pressures));
        host.SetProgramEnabled(KernelProgramCodes.Pressures, enabled: true);
        Assert.True(host.ProgramIsEnabled(KernelProgramCodes.Pressures));
    }

    [Fact]
    public void Build_RunsGenericStartupSeederAfterConsumersSubscribe()
    {
        var map = new HexMapProjection();
        var ui = new UiProjectionState();

        using var host = new KernelHostBuilder()
            .RegisterDomain(domain =>
            {
                _ = domain.RegisterKind("test_kind");
                _ = domain.RegisterTerrain("test_terrain");
                _ = domain.RegisterSubtype("test_subtype");
                _ = domain.RegisterCulture("test_culture");
                _ = domain.RegisterAttribute("test_attribute");
                _ = domain.RegisterResource("test_resource");
                _ = domain.RegisterRelationshipKind("test_relationship");
                _ = domain.RegisterSourceCode("process");
            })
            .ConfigureSymbols(symbols =>
            {
                symbols.RegisterSymbol(ProjectionSymbolKind.Kind, 1, "test_kind", "Test Kind");
                symbols.RegisterSymbol(ProjectionSymbolKind.Terrain, 1, "test_terrain", "Test Terrain");
                symbols.RegisterEntityName(1, "Seeded Entity");
            })
            .ConfigureKernel(kernel =>
            {
                kernel.SetMapRadius(1);
                kernel.RegisterTerrain(1, passable: true, boundaryGroupId: 1);
                kernel.RegisterAttribute(
                    kindId: kernel.DomainIds.KindIdOf("test_kind").Value,
                    attributeId: kernel.DomainIds.AttributeIdOf("test_attribute"),
                    minimum: 0f,
                    maximum: 100f,
                    defaultValue: 10f);
                kernel.RegisterRelationshipKind(
                    kernel.DomainIds.RelationshipKindIdOf("test_relationship"),
                    cullThreshold: 0.05f);
            })
            .WithSimulationOptions(new SimulationOptions
            {
                EventCapacity = 32,
                EventCursorCapacity = 8,
                EventSchemaValidator = KernelEventSchemaRegistry.Validator,
            })
            .WithSeedHeadroom(0)
            .WithStartupSeeder(estimatedEventCount: 6, seed: startup =>
            {
                var terrainId = startup.DomainIds.TerrainIdOf("test_terrain").Value;
                var resourceId = startup.DomainIds.ResourceIdOf("test_resource").Value;
                var kindId = startup.DomainIds.KindIdOf("test_kind").Value;
                var subtypeId = startup.DomainIds.SubtypeIdOf("test_subtype").Value;
                var cultureId = startup.DomainIds.CultureIdOf("test_culture").Value;
                var sourceCodeId = startup.DomainIds.SourceCodeIdOf("process").Value;
                var relationshipKindId = startup.DomainIds.RelationshipKindIdOf("test_relationship").Value;
                var resources = new[] { new CellResourceSeed(resourceId, 3f) };

                startup.SeedCell(
                    q: 0,
                    r: 0,
                    terrainId: terrainId,
                    ownerEntityId: 1,
                    resources: resources);
                startup.SeedEntity(
                    entityId: 1,
                    kindId: kindId,
                    subtypeId: subtypeId,
                    cultureId: cultureId,
                    physicalQ: 0,
                    physicalR: 0,
                    semanticQ: 0,
                    semanticR: 0,
                    semanticS: 0);
                startup.SeedAttribute(
                    entityId: 1,
                    attributeId: startup.DomainIds.AttributeIdOf("test_attribute").Value,
                    sourceCodeId: sourceCodeId,
                    value: 25f);
                startup.SeedRelationship(
                    relationshipKindId: relationshipKindId,
                    polarityCode: 0,
                    sourceEntityId: 1,
                    targetEntityId: 1,
                    factId0: 0,
                    factId1: 0,
                    factId2: 0,
                    factId3: 0,
                    factCount: 0,
                    strength: 0.5f);
            })
            .RegisterConsumers(context =>
            {
                context.RegisterProjection("map", map);
                context.RegisterProjection("ui", ui);
            })
            .Build();

        Assert.True(host.SeedEventCount > 0);
        Assert.Equal(host.SeedEventCount, host.Events.NextSequence);
        Assert.True(host.BootstrapCompleted);

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

        Assert.All(host.Consumers.Registrations, registration =>
        {
            Assert.Equal(host.SeedEventCount, registration.NextSequence);
            Assert.Equal(0, registration.DroppedEvents);
        });
        Assert.True(host.Consumers.IsSealed);
        Assert.Throws<InvalidOperationException>(() =>
            host.Consumers.RegisterProjection("late-map", new HexMapProjection()));
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
}
