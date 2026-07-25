namespace Engine.Kernel.Tests;

using Engine.Kernel;

public sealed class KernelQueryTests
{
    [Fact]
    public void SharedQueryEvaluatesKindSubtypeCultureAdjacencyRelationshipsAndBoundRefs()
    {
        const int targetKindId = 1;
        const int candidateKindId = 2;
        const int acceptedSubtypeId = 20;
        const int blockedRelationshipKindId = 9;
        var simulation = new Simulation(new SimulationOptions
        {
            EntityCapacity = 8,
            RelationshipCapacity = 8,
        });
        var target = simulation.CreateEntity(targetKindId, 1, cultureId: 7, 0, 0, 0, 0, 0, tick: 0);
        var accepted = simulation.CreateEntity(candidateKindId, acceptedSubtypeId, cultureId: 7, 1, 0, 0, 0, 0, tick: 0);
        _ = simulation.CreateEntity(candidateKindId, subtypeId: 21, cultureId: 7, 1, -1, 0, 0, 0, tick: 0);
        _ = simulation.CreateEntity(candidateKindId, acceptedSubtypeId, cultureId: 8, 0, 1, 0, 0, 0, tick: 0);
        _ = simulation.CreateEntity(candidateKindId, acceptedSubtypeId, cultureId: 7, 2, 0, 0, 0, 0, tick: 0);
        var blocked = simulation.CreateEntity(candidateKindId, acceptedSubtypeId, cultureId: 7, -1, 0, 0, 0, 0, tick: 0);
        var alreadyBound = simulation.CreateEntity(candidateKindId, acceptedSubtypeId, cultureId: 7, 0, -1, 0, 0, 0, tick: 0);
        simulation.World.CreateRelationship(
            blockedRelationshipKindId,
            RelationshipPolarityCodes.Negative,
            blocked,
            target,
            strength: FixedScalar.FromSingle(1f),
            tick: 0);

        var query = new SharedQuery(
            candidateKindId,
            new[] { 0, acceptedSubtypeId },
            subtypeStart: 1,
            subtypeCount: 1,
            adjacentToTarget: 1,
            cultureMatchCode: GeneratorCultureMatchCodes.SameAsTarget,
            relationshipKindFilterIds: new[] { 0, blockedRelationshipKindId },
            withoutOutgoingStart: 1,
            withoutOutgoingCount: 1);
        var binding = new QueryBinding(
            actorId: 0,
            target,
            targetOriginQ: 0,
            targetOriginR: 0,
            refEntityIds: new[] { alreadyBound },
            refCount: 1);

        var evaluation = KernelQuery.EvaluateSharedQuery(simulation.World, query, binding, selectedOrdinal: 0);

        Assert.Equal(1, evaluation.Count);
        Assert.Equal(accepted, evaluation.SelectedEntityId);
    }

    [Fact]
    public void QueryPredicateEvaluatesRelationshipStrengthThroughSharedPath()
    {
        const int kindId = 1;
        const int relationshipKindId = 7;
        var simulation = new Simulation(new SimulationOptions
        {
            EntityCapacity = 4,
            RelationshipCapacity = 4,
        });
        var actor = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);
        var target = simulation.CreateEntity(kindId, 1, 1, 1, 0, 0, 0, 0, tick: 0);
        simulation.World.CreateRelationship(
            relationshipKindId,
            RelationshipPolarityCodes.Positive,
            actor,
            target,
            strength: FixedScalar.FromSingle(0.75f),
            tick: 0);
        var binding = new QueryBinding(actor, target);

        var passes = KernelQuery.EvaluatePredicate(
            simulation.World,
            new QueryPredicate(
                BehaviorPreconditionCodes.SelfHasRelationshipToTarget,
                relationshipKindId,
                b: 1,
                x: FixedScalar.FromSingle(0.5f)),
            binding);

        Assert.True(passes);
    }
}
