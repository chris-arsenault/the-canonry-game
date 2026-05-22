namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Eventing;
using Engine.Graph;
using Engine.Kernel;

public sealed class RelationshipProgramRuntimeTests
{
    [Fact]
    public void Tick_AppliesConfiguredDistanceRuleOnlyToMatchingKind()
    {
        var simulation = CreateSimulation();
        var source = simulation.CreateEntity(1, 1, cultureId: 7, physicalQ: 0, physicalR: 0, semanticQ: 0, semanticR: 0, semanticS: 0, tick: 0);
        var target = simulation.CreateEntity(1, 1, cultureId: 7, physicalQ: 1, physicalR: 0, semanticQ: 0, semanticR: 0, semanticS: 0, tick: 0);
        var maintained = simulation.CreateRelationship(9, (int)RelationshipPolarity.Positive, source, target, 0.5f, tick: 0);
        var untouched = simulation.CreateRelationship(10, (int)RelationshipPolarity.Positive, source, target, 0.5f, tick: 0);
        simulation.Relationships.AddMaintenanceRule(
            9,
            RelationshipMaintenanceConditionCodes.PhysicalDistanceWithin,
            PlaneCodes.Physical,
            maxDistance: 2,
            threshold: 0f,
            delta: 0.03f);

        simulation.Relationships.Tick(simulation, tick: 1);

        Assert.True(simulation.World.RelationshipStrength(maintained) > 0.5f);
        Assert.Equal(0.5f, simulation.World.RelationshipStrength(untouched));
    }

    [Fact]
    public void Tick_CullsWeakRelationshipsAtRegisteredThreshold()
    {
        var simulation = CreateSimulation();
        var source = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(1, 1, 1, 0, 1, 0, 0, 0, 0);
        var relationship = simulation.CreateRelationship(9, (int)RelationshipPolarity.Positive, source, target, 0.06f, tick: 0);
        simulation.Relationships.RegisterRelationshipKind(9, cullThreshold: 0.05f);
        simulation.Relationships.AddMaintenanceRule(
            9,
            RelationshipMaintenanceConditionCodes.Always,
            PlaneCodes.None,
            maxDistance: 0,
            threshold: 0f,
            delta: -0.02f);

        simulation.Relationships.Tick(simulation, tick: 1);

        Assert.False(simulation.World.RelationshipIsActive(relationship));
    }

    [Fact]
    public void Tick_IgnoresStoredRelationshipsWithHistoricalEndpoints()
    {
        var simulation = CreateSimulation();
        var source = simulation.CreateEntity(1, 1, 1, 0, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(1, 1, 1, 0, 1, 0, 0, 0, 0);
        var relationship = simulation.CreateRelationship(9, (int)RelationshipPolarity.Positive, source, target, 0.5f, tick: 0);
        simulation.Relationships.RegisterRelationshipKind(9, cullThreshold: 0.05f);
        simulation.Relationships.AddMaintenanceRule(
            9,
            RelationshipMaintenanceConditionCodes.Always,
            PlaneCodes.None,
            maxDistance: 0,
            threshold: 0f,
            delta: 0.1f);
        simulation.SetEntityStatus(target, EntityStatusCodes.Historical, tick: 1);

        simulation.Relationships.Tick(simulation, tick: 2);

        Assert.True(simulation.World.RelationshipIsStoredActive(relationship));
        Assert.False(simulation.World.RelationshipIsActive(relationship));
        Assert.Equal(0.5f, simulation.World.RelationshipStrength(relationship));
    }

    [Fact]
    public void Tick_MovesAttachedTargetToSourcePhysicalLocation()
    {
        var simulation = CreateSimulation();
        var source = simulation.CreateEntity(1, 1, cultureId: 1, physicalQ: -1, physicalR: 0, semanticQ: 0, semanticR: 0, semanticS: 0, tick: 0);
        var target = simulation.CreateEntity(2, 1, cultureId: 1, physicalQ: 2, physicalR: 0, semanticQ: 0, semanticR: 0, semanticS: 0, tick: 0);
        simulation.Relationships.RegisterRelationshipKind(9, cullThreshold: 0.05f, targetFollowsSourcePhysical: true);
        simulation.CreateRelationship(9, (int)RelationshipPolarity.Neutral, source, target, 0.5f, tick: 0);

        simulation.Relationships.Tick(simulation, tick: 1);

        Assert.Equal(simulation.World.EntityPhysicalQ(source), simulation.World.EntityPhysicalQ(target));
        Assert.Equal(simulation.World.EntityPhysicalR(source), simulation.World.EntityPhysicalR(target));
    }

    [Fact]
    public void Tick_DoesNotMoveTargetForNonAttachedRelationship()
    {
        var simulation = CreateSimulation();
        var source = simulation.CreateEntity(1, 1, cultureId: 1, physicalQ: -1, physicalR: 0, semanticQ: 0, semanticR: 0, semanticS: 0, tick: 0);
        var target = simulation.CreateEntity(2, 1, cultureId: 1, physicalQ: 2, physicalR: 0, semanticQ: 0, semanticR: 0, semanticS: 0, tick: 0);
        simulation.Relationships.RegisterRelationshipKind(9, cullThreshold: 0.05f);
        simulation.CreateRelationship(9, (int)RelationshipPolarity.Neutral, source, target, 0.5f, tick: 0);

        simulation.Relationships.Tick(simulation, tick: 1);

        Assert.Equal(2, simulation.World.EntityPhysicalQ(target));
        Assert.Equal(0, simulation.World.EntityPhysicalR(target));
    }

    [Fact]
    public void Tick_StrengthensRelationshipFromInfluenceAtEndpoint()
    {
        var simulation = CreateSimulation();
        var source = simulation.CreateEntity(1, 1, cultureId: 1, physicalQ: 0, physicalR: 0, semanticQ: 0, semanticR: 0, semanticS: 0, tick: 0);
        var target = simulation.CreateEntity(2, 1, cultureId: 1, physicalQ: 5, physicalR: 0, semanticQ: 0, semanticR: 0, semanticS: 0, tick: 0);
        var relationship = simulation.CreateRelationship(9, (int)RelationshipPolarity.Neutral, source, target, 0.5f, tick: 0);
        simulation.SetCellInfluence(target, PlaneCodes.Physical, 0, 0, 60f, tick: 0);
        simulation.Relationships.AddMaintenanceRule(
            9,
            RelationshipMaintenanceConditionCodes.TargetInfluenceAtSource,
            PlaneCodes.Physical,
            maxDistance: 0,
            threshold: 60f,
            delta: 0.02f);

        simulation.Relationships.Tick(simulation, tick: 1);

        Assert.Equal(0.52f, simulation.World.RelationshipStrength(relationship), 4);
    }

    [Fact]
    public void Tick_StrengthensRelationshipFromSemanticInfluenceAtEndpoint()
    {
        var simulation = CreateSimulation();
        var source = simulation.CreateEntity(1, 1, cultureId: 1, physicalQ: 0, physicalR: 0, semanticQ: 2, semanticR: -1, semanticS: -1, tick: 0);
        var target = simulation.CreateEntity(2, 1, cultureId: 1, physicalQ: 5, physicalR: 0, semanticQ: -3, semanticR: 1, semanticS: 2, tick: 0);
        var relationship = simulation.CreateRelationship(9, (int)RelationshipPolarity.Neutral, source, target, 0.5f, tick: 0);
        simulation.SetCellInfluence(target, PlaneCodes.Physical, 1, 0, 60f, tick: 0);
        simulation.SetCellInfluence(target, PlaneCodes.Semantic, 2, -1, 60f, tick: 0);
        simulation.Relationships.AddMaintenanceRule(
            9,
            RelationshipMaintenanceConditionCodes.TargetInfluenceAtSource,
            PlaneCodes.Semantic,
            maxDistance: 0,
            threshold: 60f,
            delta: 0.02f);

        simulation.Relationships.Tick(simulation, tick: 1);

        Assert.Equal(0.52f, simulation.World.RelationshipStrength(relationship), 4);
    }

    [Fact]
    public void Tick_UsesAccumulatedContactMemoryAsMaintenanceEvidence()
    {
        var simulation = CreateSimulation();
        var source = simulation.CreateEntity(1, 1, cultureId: 1, physicalQ: 0, physicalR: 0, semanticQ: 0, semanticR: 0, semanticS: 0, tick: 0);
        var target = simulation.CreateEntity(1, 1, cultureId: 1, physicalQ: 1, physicalR: 0, semanticQ: 4, semanticR: 0, semanticS: -4, tick: 0);
        var relationship = simulation.CreateRelationship(9, (int)RelationshipPolarity.Positive, source, target, 0.5f, tick: 0);
        simulation.Relationships.AddMaintenanceRule(
            9,
            RelationshipMaintenanceConditionCodes.ContactMemory,
            PlaneCodes.Physical,
            maxDistance: 0,
            threshold: 2f,
            delta: 0.02f);

        simulation.Relationships.Tick(simulation, tick: 1);
        simulation.Relationships.Tick(simulation, tick: 2);

        Assert.Equal(2, simulation.Relationships.State.ContactTicks(relationship));
        Assert.True(simulation.World.RelationshipStrength(relationship) > 0.529f);
    }

    [Fact]
    public void AdvanceTick_AttributesRelationshipMutationsToRelationshipContext()
    {
        var simulation = CreateSimulation();
        var source = simulation.CreateEntity(1, 1, cultureId: 7, physicalQ: 0, physicalR: 0, semanticQ: 0, semanticR: 0, semanticS: 0, tick: 0);
        var target = simulation.CreateEntity(1, 1, cultureId: 7, physicalQ: 1, physicalR: 0, semanticQ: 0, semanticR: 0, semanticS: 0, tick: 0);
        _ = simulation.CreateRelationship(9, (int)RelationshipPolarity.Positive, source, target, 0.5f, tick: 0);
        simulation.Relationships.AddMaintenanceRule(
            9,
            RelationshipMaintenanceConditionCodes.PhysicalDistanceWithin,
            PlaneCodes.Physical,
            maxDistance: 10,
            threshold: 0f,
            delta: 0.01f);
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

    private static Simulation CreateSimulation() => KernelTestSimulation.Create(new SimulationOptions
    {
        EntityCapacity = 8,
        RelationshipCapacity = 8,
        RelationshipKindIdCapacity = 16,
        RelationshipRuleCapacity = 4,
        EventCapacity = 128,
    });
}
