namespace Engine.Kernel.Tests;

using Engine.Eventing;
using Engine.Kernel;

public sealed class KernelProvenanceCapacityTests
{
    [Fact]
    public void EnterProvenanceInvocation_GrowsInvocationStorageAndPreservesHistory()
    {
        var simulation = CreateSimulation(provenanceInvocationCapacity: 1, provenanceOutputCapacity: 4);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);
        using var executionScope = simulation.EnterExecutionContext(simulation.StartupSourceCodeId, programId: 0);
        var firstScope = simulation.EnterProvenanceInvocation(
            ProvenanceProducerKindCodes.System,
            producerId: 1,
            primaryEntityId: 0);
        firstScope.Dispose();

        var secondScope = simulation.EnterProvenanceInvocation(
            ProvenanceProducerKindCodes.System,
            producerId: 2,
            primaryEntityId: 0);
        secondScope.Dispose();

        Assert.Equal(2, simulation.World.ProvenanceInvocationHighWatermark);
        Assert.True(simulation.World.ProvenanceInvocationCapacity >= 2);
        Assert.Equal(1, simulation.World.ProvenanceInvocationProducerId(1));
        Assert.Equal(2, simulation.World.ProvenanceInvocationProducerId(2));

        var growth = ReadStorageGrowthEvent(simulation, cursor, KernelStorageCodes.ProvenanceInvocation);
        Assert.Equal(KernelStorageGrowthReasonCodes.Reserve, growth.ReasonCode);
        Assert.Equal(1, growth.PreviousCapacity);
        Assert.True(growth.CurrentCapacity >= 2);
    }

    [Fact]
    public void Mutation_GrowsOutputStorageBeforeChangingWorld()
    {
        var simulation = CreateSimulation(provenanceInvocationCapacity: 1, provenanceOutputCapacity: 1);
        simulation.RegisterAttribute(kindId: 1, attributeId: 10, minimum: 0f, maximum: 100f, defaultValue: 0f);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);
        using var executionScope = simulation.EnterExecutionContext(simulation.StartupSourceCodeId, programId: 0);
        var entity = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, tick: 0);
        using var provenanceScope = simulation.EnterProvenanceInvocation(
            ProvenanceProducerKindCodes.System,
            producerId: 1,
            primaryEntityId: entity);

        simulation.SetAttribute(entity, 10, 1f, tick: 0);
        simulation.SetAttribute(entity, 10, 2f, tick: 0);

        Assert.Equal(2f, simulation.World.GetAttribute(entity, 10));
        Assert.Equal(2, simulation.World.ProvenanceOutputHighWatermark);
        Assert.True(simulation.World.ProvenanceOutputCapacity >= 2);

        var growth = ReadStorageGrowthEvent(simulation, cursor, KernelStorageCodes.ProvenanceOutput);
        Assert.Equal(KernelStorageGrowthReasonCodes.Reserve, growth.ReasonCode);
        Assert.Equal(1, growth.PreviousCapacity);
        Assert.True(growth.CurrentCapacity >= 2);
    }

    [Fact]
    public void AdvanceTicks_GrowsProvenanceStorageAtConfiguredCadence()
    {
        var simulation = CreateSimulation(provenanceInvocationCapacity: 10, provenanceOutputCapacity: 16);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);
        using var executionScope = simulation.EnterExecutionContext(simulation.StartupSourceCodeId, programId: 0);
        for (var producerId = 1; producerId <= 10; producerId++)
        {
            var scope = simulation.EnterProvenanceInvocation(
                ProvenanceProducerKindCodes.System,
                producerId,
                primaryEntityId: 0);
            scope.Dispose();
        }

        Assert.Equal(10, simulation.World.ProvenanceInvocationCapacity);

        _ = simulation.AdvanceTicks(100);

        Assert.True(simulation.World.ProvenanceInvocationCapacity >= 20);
        var growth = ReadStorageGrowthEvent(simulation, cursor, KernelStorageCodes.ProvenanceInvocation);
        Assert.Equal(KernelStorageGrowthReasonCodes.Threshold, growth.ReasonCode);
        Assert.Equal(10, growth.HighWatermark);
        Assert.Equal(10, growth.PreviousCapacity);
        Assert.True(growth.CurrentCapacity >= 20);
    }

    [Fact]
    public void SaveAndLoadKernelStateRestoresGrownProvenanceStorage()
    {
        var simulation = CreateSimulation(provenanceInvocationCapacity: 1, provenanceOutputCapacity: 1);
        simulation.RegisterAttribute(kindId: 1, attributeId: 10, minimum: 0f, maximum: 100f, defaultValue: 0f);
        using var executionScope = simulation.EnterExecutionContext(simulation.StartupSourceCodeId, programId: 0);
        var entity = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, tick: 0);
        using (var provenanceScope = simulation.EnterProvenanceInvocation(
                   ProvenanceProducerKindCodes.System,
                   producerId: 1,
                   primaryEntityId: entity))
        {
            simulation.SetAttribute(entity, 10, 1f, tick: 0);
            simulation.SetAttribute(entity, 10, 2f, tick: 0);
        }

        var invocationId = simulation.World.ProvenanceInvocationHighWatermark;
        using var stream = new MemoryStream();
        KernelSnapshot.SaveKernelState(simulation, stream);

        stream.Position = 0;
        var loaded = KernelSnapshot.LoadKernelState(stream);

        Assert.True(loaded.World.ProvenanceOutputCapacity >= 2);
        Assert.Equal(2, loaded.World.ProvenanceInvocationOutputCount(invocationId));
        Assert.Equal(2f, loaded.World.GetAttribute(entity, 10));
    }

    [Fact]
    public void EnterProvenanceInvocation_ThrowsCapacityErrorWhenInvocationSpaceIsExhausted()
    {
        var simulation = CreateSimulation(
            provenanceInvocationCapacity: 1,
            provenanceOutputCapacity: 4,
            growableStorageModeCode: KernelGrowableStorageModeCodes.Fixed);
        using var executionScope = simulation.EnterExecutionContext(simulation.StartupSourceCodeId, programId: 0);
        var provenanceScope = simulation.EnterProvenanceInvocation(
            ProvenanceProducerKindCodes.System,
            producerId: 1,
            primaryEntityId: 0);
        provenanceScope.Dispose();

        var exception = Assert.Throws<KernelCapacityExceededException>(() =>
            simulation.EnterProvenanceInvocation(ProvenanceProducerKindCodes.System, producerId: 2, primaryEntityId: 0));

        Assert.Equal(KernelCapacityCodes.ProvenanceInvocation, exception.CapacityCode);
        Assert.Equal(2, exception.RequestedIndex);
        Assert.Equal(1, exception.Capacity);
    }

    [Fact]
    public void Mutation_ThrowsCapacityErrorBeforeChangingWorldWhenProvenanceOutputSpaceIsExhausted()
    {
        var simulation = CreateSimulation(
            provenanceInvocationCapacity: 1,
            provenanceOutputCapacity: 1,
            growableStorageModeCode: KernelGrowableStorageModeCodes.Fixed);
        simulation.RegisterAttribute(kindId: 1, attributeId: 10, minimum: 0f, maximum: 100f, defaultValue: 0f);
        using var executionScope = simulation.EnterExecutionContext(simulation.StartupSourceCodeId, programId: 0);
        var entity = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, tick: 0);
        using var provenanceScope = simulation.EnterProvenanceInvocation(
            ProvenanceProducerKindCodes.System,
            producerId: 1,
            primaryEntityId: entity);
        simulation.SetAttribute(entity, 10, 1f, tick: 0);

        var exception = Assert.Throws<KernelCapacityExceededException>(() =>
            simulation.SetAttribute(entity, 10, 2f, tick: 0));

        Assert.Equal(KernelCapacityCodes.ProvenanceOutput, exception.CapacityCode);
        Assert.Equal(2, exception.RequestedIndex);
        Assert.Equal(1, exception.Capacity);
        Assert.Equal(1f, simulation.World.GetAttribute(entity, 10));
        Assert.Equal(1, simulation.World.ProvenanceOutputHighWatermark);
    }

    private static Simulation CreateSimulation(
        int provenanceInvocationCapacity,
        int provenanceOutputCapacity,
        int growableStorageModeCode = KernelGrowableStorageModeCodes.Enabled) =>
        new(new SimulationOptions
        {
            EntityCapacity = 4,
            RelationshipCapacity = 4,
            AttributeCapacity = 8,
            ResourceCapacity = 8,
            ProvenanceInvocationCapacity = provenanceInvocationCapacity,
            ProvenanceOutputCapacity = provenanceOutputCapacity,
            GrowableStorageModeCode = growableStorageModeCode,
            EventCapacity = 16,
            KindIdCapacity = 4,
            RelationshipKindIdCapacity = 4,
        });

    private static KernelStorageGrownKernelEvent ReadStorageGrowthEvent(
        Simulation simulation,
        KernelEventCursor cursor,
        int storageCode)
    {
        while (simulation.Events.TryRead(cursor, out var read))
        {
            if (read.Event.EventType != KernelEventType.KernelStorageGrown)
            {
                continue;
            }

            var growth = read.Event.AsKernelStorageGrown();
            if (growth.StorageCode == storageCode)
            {
                return growth;
            }
        }

        throw new InvalidOperationException("Expected kernel storage growth event was not emitted.");
    }
}
