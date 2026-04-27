namespace Engine.Kernel.Tests;

using Engine.Eventing;
using Engine.Graph;
using Engine.Kernel;

public sealed class RelationshipProgramRuntimeTests
{
    [Fact]
    public void Tick_IncreasesStrengthForNearbySameCultureEntities()
    {
        var simulation = CreateSimulation();
        var source = simulation.CreateEntity(1, 1, cultureId: 7, physicalQ: 0, physicalR: 0, semanticQ: 0, semanticR: 0, semanticS: 0, tick: 0);
        var target = simulation.CreateEntity(1, 1, cultureId: 7, physicalQ: 1, physicalR: 0, semanticQ: 0, semanticR: 0, semanticS: 0, tick: 0);
        var relationship = simulation.CreateRelationship(9, (int)RelationshipPolarity.Positive, source, target, 0.5f, tick: 0);
        simulation.Relationships.ProximityMaxDistance = 10;
        simulation.Relationships.ProximityMaxDelta = 0.01f;
        simulation.Relationships.SharedCultureMatchDelta = 0.005f;
        simulation.Relationships.SharedCultureMismatchDelta = -0.005f;
        simulation.Relationships.NeglectDelta = 0f;

        simulation.Relationships.Tick(simulation, tick: 1);

        Assert.True(simulation.World.RelationshipStrength(relationship) > 0.5f);
    }

    [Fact]
    public void Tick_DistantDifferentCultureOnlyAppliesConfiguredDecay()
    {
        var simulation = CreateSimulation();
        var source = simulation.CreateEntity(1, 1, cultureId: 7, physicalQ: 0, physicalR: 0, semanticQ: 0, semanticR: 0, semanticS: 0, tick: 0);
        var target = simulation.CreateEntity(1, 1, cultureId: 8, physicalQ: 10, physicalR: 0, semanticQ: 0, semanticR: 0, semanticS: 0, tick: 0);
        var relationship = simulation.CreateRelationship(9, (int)RelationshipPolarity.Positive, source, target, 0.5f, tick: 0);
        simulation.Relationships.ProximityMaxDistance = 3;
        simulation.Relationships.ProximityMaxDelta = 0.01f;
        simulation.Relationships.SharedCultureMatchDelta = 0.005f;
        simulation.Relationships.SharedCultureMismatchDelta = -0.01f;
        simulation.Relationships.NeglectDelta = -0.01f;

        simulation.Relationships.Tick(simulation, tick: 1);

        Assert.Equal(0.48f, simulation.World.RelationshipStrength(relationship), 4);
    }

    [Fact]
    public void Tick_CullsWeakRelationshipsAtRegisteredThreshold()
    {
        var simulation = CreateSimulation();
        var source = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(1, 1, 1, 0, 1, 0, 0, 0, 0);
        var relationship = simulation.CreateRelationship(9, (int)RelationshipPolarity.Positive, source, target, 0.02f, tick: 0);
        simulation.Relationships.RegisterRelationshipKind(9, cullThreshold: 0.05f);
        simulation.Relationships.ProximityMaxDistance = 0;
        simulation.Relationships.SharedCultureMatchDelta = 0f;
        simulation.Relationships.SharedCultureMismatchDelta = 0f;
        simulation.Relationships.NeglectDelta = -0.02f;

        simulation.Relationships.Tick(simulation, tick: 1);

        Assert.False(simulation.World.RelationshipIsActive(relationship));
    }

    [Fact]
    public void AdvanceTick_AttributesRelationshipMutationsToRelationshipContext()
    {
        var simulation = CreateSimulation();
        var source = simulation.CreateEntity(1, 1, cultureId: 7, physicalQ: 0, physicalR: 0, semanticQ: 0, semanticR: 0, semanticS: 0, tick: 0);
        var target = simulation.CreateEntity(1, 1, cultureId: 7, physicalQ: 1, physicalR: 0, semanticQ: 0, semanticR: 0, semanticS: 0, tick: 0);
        _ = simulation.CreateRelationship(9, (int)RelationshipPolarity.Positive, source, target, 0.5f, tick: 0);
        simulation.Relationships.ProximityMaxDistance = 10;
        simulation.Relationships.ProximityMaxDelta = 0.01f;
        simulation.Relationships.SharedCultureMatchDelta = 0f;
        simulation.Relationships.SharedCultureMismatchDelta = 0f;
        simulation.Relationships.NeglectDelta = 0f;
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.AdvanceTicks(1);

        var sawRelationshipMutation = false;
        while (simulation.Events.TryRead(cursor, out var read))
        {
            if (read.Event.EventType != KernelEventType.RelationshipChanged)
            {
                continue;
            }

            var relationship = read.Event.AsRelationshipChanged();
            Assert.Equal(simulation.RelationshipSourceCodeId, relationship.SourceCodeId);
            sawRelationshipMutation = true;
        }

        Assert.True(sawRelationshipMutation);
    }

    private static Simulation CreateSimulation() => new(new SimulationOptions
    {
        EntityCapacity = 8,
        RelationshipCapacity = 8,
        RelationshipKindIdCapacity = 16,
        RelationshipRuleCapacity = 4,
        EventCapacity = 128,
    });
}
