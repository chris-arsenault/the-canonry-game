namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

public sealed class GambitActorPreconditionPlanTests
{
    [Fact]
    public void TerminalActorFeasibilityStillPreparesValuesUsedByCandidateBranches()
    {
        var simulation = CreateSimulation(out var actor);
        RegisterGambit(simulation);

        simulation.AdvanceTicks(1);

        Assert.Equal(
            FixedScalar.FromSingle(30f),
            simulation.World.GetAttribute(actor, attributeId: 10));
    }

    private static Simulation CreateSimulation(out int actor)
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions
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
        simulation.RegisterAttribute(
            kindId: 1,
            attributeId: 10,
            FixedScalar.Zero,
            FixedScalar.FromSingle(100f),
            FixedScalar.FromSingle(10f));
        actor = simulation.CreateEntity(kindId: 1, 1, 1, 0, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(kindId: 2, 1, 1, 1, 0, 0, 0, 0, 0);
        simulation.CreateRelationship(
            relationshipKindId: 40,
            RelationshipPolarityCodes.Positive,
            actor,
            target,
            FixedScalar.One,
            tick: 0);
        return simulation;
    }

    private static void RegisterGambit(Simulation simulation)
    {
        var gambit = simulation.Gambits.RegisterGambit(
            kindId: 1,
            gambitId: 100,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: 2,
            successChance: FixedScalar.One);
        simulation.Gambits.AddCandidatePrecondition(
            gambit,
            BehaviorPreconditionCodes.SelfHasOutgoingRelationship,
            a: 40,
            b: 0,
            c: 0,
            d: 0,
            x: FixedScalar.Zero,
            y: FixedScalar.Zero,
            actorValueSlot: 1,
            relationshipTargetSource: 0,
            trueDestination: GambitPreconditionDestinationCodes.Passed,
            falseDestination: GambitPreconditionDestinationCodes.Failed);
        simulation.Gambits.ConfigurePreconditionPlan(
            gambit,
            candidateEntry: 1,
            actorEntry: GambitPreconditionDestinationCodes.Passed,
            actorValueCount: 1,
            relationshipTargetDirectionCode: 0,
            relationshipTargetKindId: 0,
            relationshipTargetHasMinimumStrength: 0,
            relationshipTargetMinimumStrength: FixedScalar.Zero);
        simulation.Gambits.AddExpressionOp(
            gambit,
            BehaviorExpressionCodes.Literal,
            x: FixedScalar.One);
        simulation.Gambits.AddStep(
            gambit,
            BehaviorOperationCodes.AddAttribute,
            a: 10,
            x: FixedScalar.FromSingle(20f));
    }
}
