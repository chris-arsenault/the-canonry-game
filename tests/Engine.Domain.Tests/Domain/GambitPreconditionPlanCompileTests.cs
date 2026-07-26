namespace Engine.Domain.Tests.Domain;

using Engine.Behaviors;
using Engine.Constants;
using Engine.Domain.Compilation;
using Engine.Domain.Compilation.KernelPrograms;

using IceRemembers;

public sealed class GambitPreconditionPlanCompileTests
{
    [Fact]
    public void CompilerPreservesNestedBooleanLogicAndPublishesExecutionMetadata()
    {
        var config = IceRemembersDefinition.Create();
        var gambit = config.Gambits[0];
        var relationshipKind = IceRemembersRelationship.TradeRoute.ToDomainName();
        gambit.Precondition = new AndPrecondition(
        [
            new OrPrecondition([new TargetIsSelf(), new TargetIsNotSelf()]),
            new NotPrecondition(new HasColocatedNamedEntityPrecondition()),
            new SelfHasRelationshipToTarget(relationshipKind, minStrength: 0.4f),
        ]);

        var compiled = DomainCompiler.Compile(config);
        var gambitId = compiled.Ids.GambitIdOf(gambit.Id).Value;
        var relationshipKindId = compiled.Ids.RelationshipKindIdOf(relationshipKind).Value;
        var plan = Assert.Single(
            compiled.Programs.Gambits.Gambits,
            rule => rule.GambitId == gambitId).PreconditionPlan;

        AssertCandidateTruthTable(plan);
        Assert.Equal(1, plan.ActorValueCount);
        Assert.Single(plan.ActorInstructions);
        Assert.Contains(
            plan.CandidateInstructions,
            instruction =>
                instruction.Code == BehaviorProgramPrecondition.HasColocatedEntity &&
                instruction.ActorValueSlot == 1);

        var targetSource = Assert.IsType<BehaviorProgramRelationshipTargetSource>(
            plan.RelationshipTargetSource);
        Assert.Equal(
            GambitRelationshipTargetDirectionCodes.Outgoing,
            targetSource.DirectionCode);
        Assert.Equal(relationshipKindId, targetSource.RelationshipKindId);
        Assert.True(targetSource.HasMinimumStrength);
        Assert.Contains(
            plan.CandidateInstructions,
            instruction =>
                instruction.Code == BehaviorProgramPrecondition.SelfHasRelationshipToTarget &&
                instruction.RelationshipTargetSource);
    }

    private static void AssertCandidateTruthTable(BehaviorProgramPreconditionPlan plan)
    {
        for (var mask = 0; mask < 16; mask++)
        {
            var targetIsSelf = (mask & 1) != 0;
            var targetIsNotSelf = (mask & 2) != 0;
            var colocated = (mask & 4) != 0;
            var relationshipExists = (mask & 8) != 0;
            var actual = Evaluate(
                plan,
                code => code switch
                {
                    BehaviorProgramPrecondition.TargetIsSelf => targetIsSelf,
                    BehaviorProgramPrecondition.TargetIsNotSelf => targetIsNotSelf,
                    BehaviorProgramPrecondition.HasColocatedEntity => colocated,
                    BehaviorProgramPrecondition.SelfHasRelationshipToTarget =>
                        relationshipExists,
                    _ => throw new InvalidOperationException(
                        $"Unexpected candidate instruction '{code}'."),
                });
            var expected =
                (targetIsSelf || targetIsNotSelf) &&
                !colocated &&
                relationshipExists;
            Assert.Equal(expected, actual);
        }
    }

    private static bool Evaluate(
        BehaviorProgramPreconditionPlan plan,
        Func<BehaviorProgramPrecondition, bool> evaluate)
    {
        var destination = plan.CandidateEntry;
        while (destination > 0)
        {
            var instruction = plan.CandidateInstructions[destination - 1];
            destination = evaluate(instruction.Code)
                ? instruction.TrueDestination
                : instruction.FalseDestination;
        }

        return destination == GambitPreconditionDestinationCodes.Passed;
    }
}
