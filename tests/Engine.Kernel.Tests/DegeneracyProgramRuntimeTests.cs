namespace Engine.Kernel.Tests;

using Engine.Eventing;
using Engine.Kernel;

public sealed class DegeneracyProgramRuntimeTests
{
    [Fact]
    public void AttributeDeclineRatioEmitsWarningWhenEnoughEntitiesDecline()
    {
        const int kindId = 1;
        const int attributeId = 10;
        const int detectorId = 100;
        const int reasonCodeId = 200;
        var simulation = new Simulation();
        simulation.RegisterAttribute(kindId, attributeId, 0f, 100f, defaultValue: 10f);
        var firstEntityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);
        _ = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.Degeneracy.RegisterDetector(
            detectorId,
            DegeneracyMetricCodes.AttributeDeclineRatio,
            DegeneracyThresholdDirectionCodes.HighIsBad,
            DegeneracySeverityCodes.Critical,
            reasonCodeId,
            kindId,
            secondaryKindId: 0,
            attributeId,
            relationshipKindId: 0,
            DegeneracyRelationshipEndpointCodes.Target,
            warningThreshold: 0.5f,
            criticalThreshold: 0.8f);
        simulation.Degeneracy.Tick(simulation, tick: 1);
        simulation.SetAttribute(firstEntityId, attributeId, sourceCodeId: 1, value: 9f, tick: 2);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.Degeneracy.Tick(simulation, tick: 2);

        var degeneracy = ReadDegeneracyEvent(simulation, cursor);
        Assert.Equal(detectorId, degeneracy.DetectorId);
        Assert.Equal(DegeneracySeverityCodes.Warning, degeneracy.SeverityCode);
        Assert.Equal(reasonCodeId, degeneracy.ReasonCodeId);
    }

    [Fact]
    public void RelationshipEndpointDominanceUsesConfiguredTargetEndpoint()
    {
        const int settlementKindId = 1;
        const int factionKindId = 2;
        const int relationshipKindId = 20;
        const int detectorId = 101;
        const int reasonCodeId = 201;
        var simulation = new Simulation();
        var settlements = new int[5];
        for (var i = 0; i < settlements.Length; i++)
        {
            settlements[i] = simulation.CreateEntity(settlementKindId, 1, 1, i, 0, 0, 0, 0, tick: 0);
        }

        var firstFactionId = simulation.CreateEntity(factionKindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);
        var secondFactionId = simulation.CreateEntity(factionKindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.CreateRelationship(relationshipKindId, polarityCode: 0, settlements[0], firstFactionId, 1f, tick: 0);
        simulation.CreateRelationship(relationshipKindId, polarityCode: 0, settlements[1], firstFactionId, 1f, tick: 0);
        simulation.CreateRelationship(relationshipKindId, polarityCode: 0, settlements[2], firstFactionId, 1f, tick: 0);
        simulation.CreateRelationship(relationshipKindId, polarityCode: 0, settlements[3], secondFactionId, 1f, tick: 0);
        simulation.CreateRelationship(relationshipKindId, polarityCode: 0, settlements[4], secondFactionId, 1f, tick: 0);
        simulation.Degeneracy.RegisterDetector(
            detectorId,
            DegeneracyMetricCodes.RelationshipEndpointDominanceRatio,
            DegeneracyThresholdDirectionCodes.HighIsBad,
            DegeneracySeverityCodes.Normal,
            reasonCodeId,
            settlementKindId,
            factionKindId,
            attributeId: 0,
            relationshipKindId,
            DegeneracyRelationshipEndpointCodes.Target,
            warningThreshold: 0.6f,
            criticalThreshold: 0.8f);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.Degeneracy.Tick(simulation, tick: 1);

        var degeneracy = ReadDegeneracyEvent(simulation, cursor);
        Assert.Equal(detectorId, degeneracy.DetectorId);
        Assert.Equal(DegeneracySeverityCodes.Warning, degeneracy.SeverityCode);
        Assert.Equal(reasonCodeId, degeneracy.ReasonCodeId);
    }

    [Fact]
    public void ActiveKindCountUsesLowThresholds()
    {
        const int kindId = 1;
        const int detectorId = 102;
        const int reasonCodeId = 202;
        var simulation = new Simulation();
        for (var i = 0; i < 3; i++)
        {
            simulation.CreateEntity(kindId, 1, 1, i, 0, 0, 0, 0, tick: 0);
        }

        simulation.Degeneracy.RegisterDetector(
            detectorId,
            DegeneracyMetricCodes.ActiveKindCount,
            DegeneracyThresholdDirectionCodes.LowIsBad,
            DegeneracySeverityCodes.Critical,
            reasonCodeId,
            kindId,
            secondaryKindId: 0,
            attributeId: 0,
            relationshipKindId: 0,
            DegeneracyRelationshipEndpointCodes.Target,
            warningThreshold: 5f,
            criticalThreshold: 2f);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.Degeneracy.Tick(simulation, tick: 1);

        var degeneracy = ReadDegeneracyEvent(simulation, cursor);
        Assert.Equal(detectorId, degeneracy.DetectorId);
        Assert.Equal(DegeneracySeverityCodes.Warning, degeneracy.SeverityCode);
        Assert.Equal(reasonCodeId, degeneracy.ReasonCodeId);
    }

    private static DegeneracyDetectedKernelEvent ReadDegeneracyEvent(
        Simulation simulation,
        KernelEventCursor cursor)
    {
        Assert.True(simulation.Events.TryRead(cursor, out var read));
        Assert.Equal(KernelEventType.DegeneracyDetected, read.Event.EventType);
        return read.Event.AsDegeneracyDetected();
    }
}
