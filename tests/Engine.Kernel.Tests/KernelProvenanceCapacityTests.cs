namespace Engine.Kernel.Tests;

using Engine.Kernel;

public sealed class KernelProvenanceCapacityTests
{
    [Fact]
    public void EnterProvenanceInvocation_ThrowsCapacityErrorWhenInvocationSpaceIsExhausted()
    {
        var simulation = CreateSimulation(provenanceInvocationCapacity: 1, provenanceOutputCapacity: 4);
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
        var simulation = CreateSimulation(provenanceInvocationCapacity: 1, provenanceOutputCapacity: 1);
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

    private static Simulation CreateSimulation(int provenanceInvocationCapacity, int provenanceOutputCapacity) =>
        new(new SimulationOptions
        {
            EntityCapacity = 4,
            RelationshipCapacity = 4,
            AttributeCapacity = 8,
            ResourceCapacity = 8,
            ProvenanceInvocationCapacity = provenanceInvocationCapacity,
            ProvenanceOutputCapacity = provenanceOutputCapacity,
            EventCapacity = 16,
            KindIdCapacity = 4,
            RelationshipKindIdCapacity = 4,
        });
}
