namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Eventing;
using Engine.Kernel;

public sealed class GambitRelationshipTargetPlanTests
{
    private const int ActorKindId = 1;
    private const int TargetKindId = 2;
    private const int AttributeId = 10;
    private const int RelationshipKindId = 40;

    [Fact]
    public void RelationshipTargetPlanEnumeratesOnlyQualifyingRelationshipEndpoints()
    {
        var simulation = CreateSimulation();
        var (actor, qualifyingTarget, weakTarget) = CreateScenario(simulation);
        RegisterRelationshipTargetGambit(simulation, actor);

        simulation.AdvanceTicks(1);

        Assert.Equal(
            FixedScalar.FromSingle(30f),
            simulation.World.GetAttribute(qualifyingTarget, AttributeId));
        Assert.Equal(
            FixedScalar.FromSingle(10f),
            simulation.World.GetAttribute(weakTarget, AttributeId));
        Assert.Equal(1, LastGambitEvaluation(simulation).CandidateCount);
    }

    private static (int Actor, int QualifyingTarget, int WeakTarget) CreateScenario(
        Simulation simulation)
    {
        simulation.RegisterAttribute(
            TargetKindId,
            AttributeId,
            FixedScalar.Zero,
            FixedScalar.FromSingle(100f),
            FixedScalar.FromSingle(10f));
        var actor = simulation.CreateEntity(ActorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var qualifyingTarget = simulation.CreateEntity(TargetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        var weakTarget = simulation.CreateEntity(TargetKindId, 1, 1, -1, 0, 0, 0, 0, 0);
        simulation.CreateRelationship(
            RelationshipKindId,
            RelationshipPolarityCodes.Positive,
            actor,
            qualifyingTarget,
            FixedScalar.FromSingle(0.8f),
            tick: 0);
        simulation.CreateRelationship(
            RelationshipKindId,
            RelationshipPolarityCodes.Positive,
            actor,
            weakTarget,
            FixedScalar.FromSingle(0.2f),
            tick: 0);
        return (actor, qualifyingTarget, weakTarget);
    }

    private static void RegisterRelationshipTargetGambit(
        Simulation simulation,
        int actor)
    {
        Assert.True(simulation.World.EntityIsActive(actor));
        var threshold = FixedScalar.FromSingle(0.5f);
        var gambit = simulation.Gambits.RegisterGambit(
            ActorKindId,
            gambitId: 113,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: TargetKindId,
            successChance: FixedScalar.One);
        simulation.Gambits.AddCandidatePrecondition(
            gambit,
            BehaviorPreconditionCodes.SelfHasRelationshipToTarget,
            RelationshipKindId,
            b: 1,
            c: 0,
            d: 0,
            x: threshold,
            y: FixedScalar.Zero,
            actorValueSlot: 0,
            relationshipTargetSource: 1,
            trueDestination: GambitPreconditionDestinationCodes.Passed,
            falseDestination: GambitPreconditionDestinationCodes.Failed);
        simulation.Gambits.ConfigurePreconditionPlan(
            gambit,
            candidateEntry: 1,
            actorEntry: GambitPreconditionDestinationCodes.Passed,
            actorValueCount: 0,
            relationshipTargetDirectionCode: GambitRelationshipTargetDirectionCodes.Outgoing,
            relationshipTargetKindId: RelationshipKindId,
            relationshipTargetHasMinimumStrength: 1,
            relationshipTargetMinimumStrength: threshold);
        simulation.Gambits.AddExpressionOp(
            gambit,
            BehaviorExpressionCodes.Literal,
            x: FixedScalar.One);
        simulation.Gambits.AddStep(
            gambit,
            BehaviorOperationCodes.AddTargetAttribute,
            AttributeId,
            x: FixedScalar.FromSingle(20f));
    }

    private static GambitEvaluatedKernelEvent LastGambitEvaluation(Simulation simulation)
    {
        using var cursor = simulation.Events.CreateCursor(
            KernelEventCursorStart.EarliestAvailable);
        var found = default(GambitEvaluatedKernelEvent);
        while (simulation.Events.TryRead(cursor, out var sequenced))
        {
            if (sequenced.Event.EventType == KernelEventType.GambitEvaluated)
            {
                found = sequenced.Event.AsGambitEvaluated();
            }
        }

        return found;
    }

    private static Simulation CreateSimulation() =>
        KernelTestSimulation.Create(new SimulationOptions
        {
            EntityCapacity = 8,
            RelationshipCapacity = 8,
            AttributeCapacity = 64,
            MapCellCapacity = 128,
            EventCapacity = 256,
            GambitCapacity = 4,
            GambitStepCapacity = 32,
            GeneratorCapacity = 4,
            GeneratorItemCapacity = 32,
            LifecycleRuleCapacity = 8,
            EntityAttributeCapacity = 8,
            EffectRuleCapacity = 8,
            RelationshipRuleCapacity = 8,
            KindIdCapacity = 8,
            TerrainIdCapacity = 8,
            Seed = 1,
        });
}
