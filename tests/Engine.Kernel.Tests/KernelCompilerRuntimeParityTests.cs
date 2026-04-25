using System.Collections;
using System.Reflection;
using Engine.Domain;
using Engine.Graph;
using Engine.Kernel;
using Engine.Kinds;
using Engine.Processes;
using Engine.Resources;
using Engine.Simulation;

namespace Engine.Kernel.Tests;

public sealed class KernelCompilerRuntimeParityTests
{
    private const int ActorKindId = 1;
    private const int TargetKindId = 2;
    private const int RelatedKindId = 3;
    private const int CreatedKindId = 4;
    private const int ValueAttributeId = 1;
    private const int TargetAttributeId = 2;
    private const int MarkerAttributeId = 3;
    private const int RelatedAttributeId = 4;
    private const int PressureId = 1;
    private const int RelationshipKindId = 1;
    private const int ProcessIdBase = 9000;

    [Theory]
    [MemberData(nameof(CompositePreconditionCases))]
    public void CompositePreconditionsUseKernelBooleanRuntime(
        int caseId,
        int expressionMode,
        float selfValue,
        float targetValue,
        bool expectedExecuted)
    {
        var simulation = CreateSimulation();
        var actor = CreateEntity(simulation, ActorKindId, 0, 0);
        var target = CreateEntity(simulation, TargetKindId, 1, 0);
        simulation.World.SetAttribute(actor, ValueAttributeId, simulation.ProcessSourceCodeId, selfValue, 0);
        simulation.World.SetAttribute(target, TargetAttributeId, simulation.ProcessSourceCodeId, targetValue, 0);

        var process = RegisterTargetProcess(simulation, ProcessIdBase + caseId);
        AddCompositePreconditions(simulation, process, expressionMode);
        simulation.Processes.AddStep(process, ProcessOperationCodes.AddAttribute, MarkerAttributeId, x: 1f);

        simulation.Tick();

        Assert.Equal(expectedExecuted ? 1f : 0f, simulation.World.GetAttribute(actor, MarkerAttributeId));
    }

    [Theory]
    [MemberData(nameof(EntityCountCases))]
    public void EntityCountPreconditionCountsOnlyActiveKernelEntities(
        int caseId,
        int activeCount,
        int historicalCount,
        int minimum,
        bool expectedExecuted)
    {
        var simulation = CreateSimulation(entityCapacity: 96);
        var actor = CreateEntity(simulation, ActorKindId, 0, 0);
        for (var i = 0; i < activeCount; i++)
        {
            _ = CreateEntity(simulation, TargetKindId, i + 1, 0);
        }

        for (var i = 0; i < historicalCount; i++)
        {
            var historical = CreateEntity(simulation, TargetKindId, i + 20, 0);
            simulation.World.SetEntityStatus(historical, EntityStatusCodes.Historical, 0);
        }

        var process = RegisterSelfProcess(simulation, ProcessIdBase + caseId);
        simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.EntityCountAtLeast, TargetKindId, minimum);
        simulation.Processes.AddStep(process, ProcessOperationCodes.AddAttribute, MarkerAttributeId, x: 1f);

        simulation.Tick();

        Assert.Equal(expectedExecuted ? 1f : 0f, simulation.World.GetAttribute(actor, MarkerAttributeId));
    }

    [Theory]
    [MemberData(nameof(ColocatedCases))]
    public void ColocatedPreconditionUsesKernelOccupancyIndex(
        int caseId,
        bool otherAtSameCell,
        bool otherHistorical,
        bool otherAtDifferentCell,
        bool expectedExecuted)
    {
        var simulation = CreateSimulation();
        var actor = CreateEntity(simulation, ActorKindId, 0, 0);
        if (otherAtSameCell)
        {
            var other = CreateEntity(simulation, TargetKindId, 0, 0);
            if (otherHistorical)
            {
                simulation.World.SetEntityStatus(other, EntityStatusCodes.Historical, 0);
            }
        }

        if (otherAtDifferentCell)
        {
            _ = CreateEntity(simulation, TargetKindId, 1, 0);
        }

        var process = RegisterSelfProcess(simulation, ProcessIdBase + caseId);
        simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.HasColocatedEntity);
        simulation.Processes.AddStep(process, ProcessOperationCodes.AddAttribute, MarkerAttributeId, x: 1f);

        simulation.Tick();

        Assert.Equal(expectedExecuted ? 1f : 0f, simulation.World.GetAttribute(actor, MarkerAttributeId));
    }

    [Theory]
    [MemberData(nameof(VariableExpressionCases))]
    public void VariableCalculationMutationsUseKernelExpressionRuntime(
        int caseId,
        int expressionMode,
        float selfValue,
        float targetValue,
        float literal,
        bool writeTarget,
        float expectedDelta)
    {
        var simulation = CreateSimulation();
        var actor = CreateEntity(simulation, ActorKindId, 0, 0);
        var target = CreateEntity(simulation, TargetKindId, 3, 0);
        simulation.World.SetAttribute(actor, ValueAttributeId, simulation.ProcessSourceCodeId, selfValue, 0);
        simulation.World.SetAttribute(target, TargetAttributeId, simulation.ProcessSourceCodeId, targetValue, 0);

        var process = RegisterTargetProcess(simulation, ProcessIdBase + caseId);
        var calculate = simulation.Processes.AddStep(
            process,
            ProcessOperationCodes.CalculateVariable,
            StableRuntimeCode("delta"));
        AddMutationExpression(simulation, calculate, expressionMode, literal);
        simulation.Processes.AddStep(
            process,
            writeTarget
                ? ProcessOperationCodes.AddTargetAttributeFromVariable
                : ProcessOperationCodes.AddAttributeFromVariable,
            MarkerAttributeId,
            StableRuntimeCode("delta"),
            x: 1f);

        simulation.Tick();

        var writtenEntity = writeTarget ? target : actor;
        Assert.Equal(expectedDelta, simulation.World.GetAttribute(writtenEntity, MarkerAttributeId), 4);
    }

    [Theory]
    [MemberData(nameof(MutationOperationCases))]
    public void KernelMutationOpcodesPreserveLegacyMutationEffects(
        int caseId,
        int mutationMode,
        float amount,
        bool useTarget,
        bool incoming)
    {
        var simulation = CreateSimulation(entityCapacity: 96, relationshipCapacity: 128);
        simulation.Pressures.RegisterPressure(PressureId, PressureScopeCodes.Entity, 0f, 0f, 1f);
        var actor = CreateEntity(simulation, ActorKindId, 0, 0);
        var target = CreateEntity(simulation, TargetKindId, 1, 0);
        var related = CreateEntity(simulation, RelatedKindId, incoming ? 2 : 0, 0);
        simulation.World.SetAttribute(actor, MarkerAttributeId, simulation.ProcessSourceCodeId, 0f, 0);
        simulation.World.SetAttribute(target, MarkerAttributeId, simulation.ProcessSourceCodeId, 0f, 0);
        simulation.World.SetAttribute(related, RelatedAttributeId, simulation.ProcessSourceCodeId, 10f, 0);

        if (incoming)
        {
            simulation.World.CreateRelationship(RelationshipKindId, (int)RelationshipPolarity.Neutral, related, target, 1f, 0);
        }
        else
        {
            simulation.World.CreateRelationship(RelationshipKindId, (int)RelationshipPolarity.Neutral, actor, related, 1f, 0);
        }

        var process = RegisterTargetProcess(simulation, ProcessIdBase + caseId);
        AddMutationOperation(simulation, process, mutationMode, actor, target, amount, useTarget, incoming);

        simulation.Tick();
        simulation.Pressures.Tick(simulation, simulation.CurrentTick);

        AssertMutationOperation(simulation, mutationMode, actor, target, related, amount, useTarget, incoming);
    }

    [Theory]
    [MemberData(nameof(FactoryPreconditionCases))]
    public void KernelFactoryCompilesUnsupportedPreconditionCases(
        int caseId,
        int preconditionMode,
        bool expectedExecuted)
    {
        var simulation = CreateSimulation(entityCapacity: 96);
        var ids = CreateDomainIds();
        var actor = CreateEntity(simulation, ActorKindId, 0, 0);
        var target = CreateEntity(simulation, TargetKindId, 1, 0);
        simulation.World.SetAttribute(actor, ValueAttributeId, simulation.ProcessSourceCodeId, preconditionMode is 0 or 3 ? 8f : 2f, 0);
        simulation.World.SetAttribute(target, TargetAttributeId, simulation.ProcessSourceCodeId, 2f, 0);
        if (preconditionMode is 4 or 5)
        {
            var countedKindId = KindId.Register("target").Id;
            _ = CreateEntity(simulation, countedKindId, 2, 0);
            _ = CreateEntity(simulation, countedKindId, 3, 0);
        }

        if (preconditionMode is 6)
        {
            _ = CreateEntity(simulation, TargetKindId, 0, 0);
        }

        var process = RegisterTargetProcess(simulation, ProcessIdBase + caseId);
        InvokeFactoryPrecondition(simulation, process, CreatePrecondition(preconditionMode), ids);
        simulation.Processes.AddStep(process, ProcessOperationCodes.AddAttribute, MarkerAttributeId, x: 1f);

        simulation.Tick();

        Assert.Equal(expectedExecuted ? 1f : 0f, simulation.World.GetAttribute(actor, MarkerAttributeId));
    }

    [Theory]
    [MemberData(nameof(FactoryMutationCases))]
    public void KernelFactoryCompilesUnsupportedMutationCases(
        int caseId,
        int mutationMode,
        float amount,
        bool useTarget,
        bool incoming)
    {
        var simulation = CreateSimulation(entityCapacity: 96, relationshipCapacity: 128);
        var ids = CreateDomainIds();
        simulation.Pressures.RegisterPressure(ids.PressureIdOf("heat").Value, PressureScopeCodes.Entity, 0f, 0f, 1f);
        var actor = CreateEntity(simulation, ActorKindId, 0, 0);
        var target = CreateEntity(simulation, TargetKindId, 1, 0);
        var related = CreateEntity(simulation, RelatedKindId, incoming ? 2 : 0, 0);
        simulation.World.SetAttribute(actor, MarkerAttributeId, simulation.ProcessSourceCodeId, 0f, 0);
        simulation.World.SetAttribute(target, MarkerAttributeId, simulation.ProcessSourceCodeId, 0f, 0);
        simulation.World.SetAttribute(related, RelatedAttributeId, simulation.ProcessSourceCodeId, 10f, 0);
        if (incoming)
        {
            simulation.World.CreateRelationship(ids.RelationshipKindIdOf("link").Value, (int)RelationshipPolarity.Neutral, related, target, 1f, 0);
        }
        else
        {
            simulation.World.CreateRelationship(ids.RelationshipKindIdOf("link").Value, (int)RelationshipPolarity.Neutral, actor, related, 1f, 0);
        }

        var process = RegisterTargetProcess(simulation, ProcessIdBase + caseId);
        InvokeFactoryMutation(simulation, process, CreateMutation(mutationMode, amount, useTarget, incoming), ids);
        if (mutationMode == 5)
        {
            InvokeFactoryMutation(simulation, process, new ModifySelfByVariableMutation("marker", "delta"), ids);
        }

        simulation.Tick();
        simulation.Pressures.Tick(simulation, simulation.CurrentTick);

        AssertFactoryMutation(simulation, ids, mutationMode, actor, target, related, amount, useTarget, incoming);
    }

    public static TheoryData<int, int, float, float, bool> CompositePreconditionCases()
    {
        var data = new TheoryData<int, int, float, float, bool>();
        var id = 0;
        for (var mode = 0; mode < 8; mode++)
        {
            foreach (var (selfValue, targetValue) in ThresholdPairs())
            {
                data.Add(id++, mode, selfValue, targetValue, CompositeExpected(mode, selfValue, targetValue));
            }
        }

        return data;
    }

    public static TheoryData<int, int, int, int, bool> EntityCountCases()
    {
        var data = new TheoryData<int, int, int, int, bool>();
        var id = 1000;
        for (var active = 0; active <= 5; active++)
        {
            for (var historical = 0; historical <= 2; historical++)
            {
                for (var min = 0; min <= 5; min += 2)
                {
                    data.Add(id++, active, historical, min, active >= min);
                }
            }
        }

        return data;
    }

    public static TheoryData<int, bool, bool, bool, bool> ColocatedCases()
    {
        var data = new TheoryData<int, bool, bool, bool, bool>();
        var id = 2000;
        foreach (var same in new[] { false, true })
        foreach (var historical in new[] { false, true })
        foreach (var different in new[] { false, true })
        {
            data.Add(id++, same, historical, different, same && !historical);
        }

        return data;
    }

    public static TheoryData<int, int, float, float, float, bool, float> VariableExpressionCases()
    {
        var data = new TheoryData<int, int, float, float, float, bool, float>();
        var id = 3000;
        for (var mode = 0; mode < 12; mode++)
        {
            foreach (var values in new[] { (9f, 3f, 4f), (5f, 2f, -6f), (12f, 4f, 7f) })
            {
                data.Add(id++, mode, values.Item1, values.Item2, values.Item3, false, ExpressionExpected(mode, values.Item1, values.Item2, values.Item3));
                data.Add(id++, mode, values.Item1, values.Item2, values.Item3, true, ExpressionExpected(mode, values.Item1, values.Item2, values.Item3));
            }
        }

        return data;
    }

    public static TheoryData<int, int, float, bool, bool> MutationOperationCases()
    {
        var data = new TheoryData<int, int, float, bool, bool>();
        var id = 4000;
        for (var mode = 0; mode < 10; mode++)
        {
            foreach (var amount in new[] { 1f, 3f, 7f, -2f })
            {
                data.Add(id++, mode, amount, false, false);
                data.Add(id++, mode, amount, true, true);
            }
        }

        return data;
    }

    public static TheoryData<int, int, bool> FactoryPreconditionCases()
    {
        var data = new TheoryData<int, int, bool>();
        var id = 5000;
        for (var mode = 0; mode < 8; mode++)
        {
            data.Add(id++, mode, mode is 0 or 2 or 4 or 6);
            data.Add(id++, mode, mode is 0 or 2 or 4 or 6);
        }

        return data;
    }

    public static TheoryData<int, int, float, bool, bool> FactoryMutationCases()
    {
        var data = new TheoryData<int, int, float, bool, bool>();
        var id = 6000;
        for (var mode = 0; mode < 9; mode++)
        {
            foreach (var amount in new[] { 2f, 5f, -3f })
            {
                data.Add(id++, mode, amount, false, false);
                data.Add(id++, mode, amount, true, true);
            }
        }

        return data;
    }

    private static void AddCompositePreconditions(Simulation simulation, int process, int mode)
    {
        switch (mode)
        {
            case 0:
                AddSelfMin(simulation, process, 5f);
                AddTargetMin(simulation, process, 5f);
                simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.Or);
                break;
            case 1:
                AddSelfMin(simulation, process, 5f);
                simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.Not);
                break;
            case 2:
                AddSelfMin(simulation, process, 5f);
                AddTargetMin(simulation, process, 5f);
                simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.And);
                break;
            case 3:
                AddSelfMin(simulation, process, 5f);
                AddTargetMin(simulation, process, 5f);
                simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.And);
                simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.Not);
                break;
            case 4:
                AddSelfMin(simulation, process, 5f);
                simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.Not);
                AddTargetMin(simulation, process, 5f);
                simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.Or);
                break;
            case 5:
                AddSelfMin(simulation, process, 5f);
                AddTargetMin(simulation, process, 5f);
                simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.Not);
                simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.And);
                break;
            case 6:
                AddSelfMin(simulation, process, 5f);
                AddTargetMin(simulation, process, 5f);
                simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.Or);
                simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.Not);
                break;
            default:
                AddSelfMin(simulation, process, 5f);
                AddTargetMin(simulation, process, 5f);
                simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.Or);
                AddSelfMin(simulation, process, 10f);
                simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.And);
                break;
        }
    }

    private static void AddMutationExpression(Simulation simulation, int step, int mode, float literal)
    {
        switch (mode)
        {
            case 0:
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.Literal, x: literal);
                break;
            case 1:
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.SelfAttribute, ValueAttributeId);
                break;
            case 2:
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.TargetAttribute, TargetAttributeId);
                break;
            case 3:
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.SelfAttribute, ValueAttributeId);
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.TargetAttribute, TargetAttributeId);
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.Add);
                break;
            case 4:
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.SelfAttribute, ValueAttributeId);
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.TargetAttribute, TargetAttributeId);
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.Subtract);
                break;
            case 5:
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.SelfAttribute, ValueAttributeId);
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.TargetAttribute, TargetAttributeId);
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.Multiply);
                break;
            case 6:
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.SelfAttribute, ValueAttributeId);
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.TargetAttribute, TargetAttributeId);
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.Divide);
                break;
            case 7:
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.SelfAttribute, ValueAttributeId);
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.TargetAttribute, TargetAttributeId);
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.Min);
                break;
            case 8:
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.SelfAttribute, ValueAttributeId);
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.TargetAttribute, TargetAttributeId);
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.Max);
                break;
            case 9:
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.SelfAttribute, ValueAttributeId);
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.Literal, x: 3f);
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.Literal, x: 8f);
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.Clamp);
                break;
            case 10:
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.Literal, x: literal);
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.Abs);
                break;
            default:
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.Distance);
                break;
        }
    }

    private static void AddMutationOperation(
        Simulation simulation,
        int process,
        int mutationMode,
        int actor,
        int target,
        float amount,
        bool useTarget,
        bool incoming)
    {
        switch (mutationMode)
        {
            case 0:
                simulation.Processes.AddStep(process, ProcessOperationCodes.CreateEntity, CreatedKindId, 1, 1, 1, amount, amount + 1f);
                break;
            case 1:
                simulation.Processes.AddStep(process, ProcessOperationCodes.AddPressure, PressureId, actor, x: amount);
                break;
            case 2:
                simulation.Processes.AddStep(process, ProcessOperationCodes.SetEntityTag, actor, 101, 201 + (int)amount);
                break;
            case 3:
                simulation.Processes.AddStep(process, ProcessOperationCodes.SetHistorical, useTarget ? target : actor);
                break;
            case 4:
                simulation.Processes.AddStep(
                    process,
                    ProcessOperationCodes.AddRelatedEntitiesAttribute,
                    RelationshipKindId,
                    RelatedAttributeId,
                    incoming ? RelationshipTraversalDirectionCodes.Incoming : RelationshipTraversalDirectionCodes.Outgoing,
                    useTarget ? 1 : 0,
                    amount);
                break;
            case 5:
            {
                var step = simulation.Processes.AddStep(process, ProcessOperationCodes.CalculateVariable, StableRuntimeCode("delta"));
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.Literal, x: amount);
                simulation.Processes.AddStep(process, ProcessOperationCodes.AddAttributeFromVariable, MarkerAttributeId, StableRuntimeCode("delta"), x: 1f);
                break;
            }
            case 6:
            {
                var step = simulation.Processes.AddStep(process, ProcessOperationCodes.CalculateVariable, StableRuntimeCode("delta"));
                simulation.Processes.AddStepExpressionOp(step, ProcessExpressionCodes.Literal, x: amount);
                simulation.Processes.AddStep(process, ProcessOperationCodes.AddTargetAttributeFromVariable, MarkerAttributeId, StableRuntimeCode("delta"), x: 1f);
                break;
            }
            case 7:
                simulation.Processes.AddStep(
                    process,
                    ProcessOperationCodes.CreateFixedRelationship,
                    RelationshipKindId,
                    actor,
                    target,
                    (int)RelationshipPolarity.Positive,
                    0.75f);
                break;
            case 8:
                simulation.Processes.AddStep(process, ProcessOperationCodes.CreateEntity, CreatedKindId, 1, 1, 0);
                break;
            default:
                simulation.Processes.AddStep(process, ProcessOperationCodes.AddTargetAttribute, MarkerAttributeId, x: amount);
                break;
        }
    }

    private static void AssertMutationOperation(
        Simulation simulation,
        int mutationMode,
        int actor,
        int target,
        int related,
        float amount,
        bool useTarget,
        bool incoming)
    {
        switch (mutationMode)
        {
            case 0:
                Assert.Equal(4, simulation.World.EntityHighWatermark);
                Assert.Equal(CreatedKindId, simulation.World.EntityKindId(simulation.World.EntityHighWatermark));
                Assert.Equal((int)amount, simulation.World.EntityPhysicalQ(simulation.World.EntityHighWatermark));
                break;
            case 1:
                Assert.Equal(amount, simulation.Pressures.GetValue(PressureId, actor), 4);
                break;
            case 2:
                Assert.Equal(201 + (int)amount, simulation.World.EntityTagValue(actor, 101));
                break;
            case 3:
                Assert.Equal(EntityStatusCodes.Historical, simulation.World.EntityStatus(useTarget ? target : actor));
                break;
            case 4:
                Assert.Equal(10f + amount, simulation.World.GetAttribute(related, RelatedAttributeId), 4);
                break;
            case 5:
                Assert.Equal(amount, simulation.World.GetAttribute(actor, MarkerAttributeId), 4);
                break;
            case 6:
                Assert.Equal(amount, simulation.World.GetAttribute(target, MarkerAttributeId), 4);
                break;
            case 7:
                Assert.True(simulation.World.FindRelationship(actor, target, RelationshipKindId) > 0);
                break;
            case 8:
                Assert.Equal(simulation.World.EntityPhysicalQ(actor), simulation.World.EntityPhysicalQ(simulation.World.EntityHighWatermark));
                break;
            default:
                Assert.Equal(amount, simulation.World.GetAttribute(target, MarkerAttributeId), 4);
                break;
        }
    }

    private static IPrecondition CreatePrecondition(int mode)
    {
        return mode switch
        {
            0 => new OrPrecondition([
                new SelfAttributeThreshold("value", 5f, null),
                new TargetAttributeThreshold("target_value", 5f, null)]),
            1 => new OrPrecondition([
                new SelfAttributeThreshold("value", 5f, null),
                new TargetAttributeThreshold("target_value", 5f, null)]),
            2 => new NotPrecondition(new SelfAttributeThreshold("value", 5f, null)),
            3 => new NotPrecondition(new SelfAttributeThreshold("value", 5f, null)),
            4 => new EntityCountCheck(KindId.Register("target"), 2),
            5 => new EntityCountCheck(KindId.Register("target"), 4),
            6 => new HasColocatedNamedEntityPrecondition(),
            _ => new HasColocatedNamedEntityPrecondition(),
        };
    }

    private static IMutation CreateMutation(int mode, float amount, bool useTarget, bool incoming)
    {
        return mode switch
        {
            0 => new CreateEntityMutation(KindId.Register("created"), "created_subtype", "created_name", "none", ((int)amount, (int)amount + 1)),
            1 => new ModifyPressureMutation("heat", new EntityId(1), amount),
            2 => new SetTagMutation(new EntityId(1), "flag", $"value_{(int)amount}"),
            3 => new SetHistoricalMutation(new EntityId(useTarget ? 2 : 1)),
            4 => new ModifyRelatedEntitiesAttributeMutation(
                "link",
                incoming ? RelationshipTraversalDirection.Incoming : RelationshipTraversalDirection.Outgoing,
                "related_value",
                amount,
                useTargetEntity: useTarget),
            5 => new CalculateMutation("delta", new Literal(amount)),
            6 => new ModifySelfByVariableMutation("marker", "delta"),
            7 => new ModifyTargetByVariableMutation("marker", "delta"),
            _ => new CreateRelationshipMutation(new EntityId(1), new EntityId(2), "link", RelationshipPolarity.Positive, 0.75f),
        };
    }

    private static void AssertFactoryMutation(
        Simulation simulation,
        DomainIdRegistry ids,
        int mutationMode,
        int actor,
        int target,
        int related,
        float amount,
        bool useTarget,
        bool incoming)
    {
        switch (mutationMode)
        {
            case 0:
                Assert.Equal(4, simulation.World.EntityHighWatermark);
                Assert.Equal(KindId.Register("created").Id, simulation.World.EntityKindId(simulation.World.EntityHighWatermark));
                break;
            case 1:
                Assert.Equal(amount, simulation.Pressures.GetValue(ids.PressureIdOf("heat").Value, actor), 4);
                break;
            case 2:
                Assert.True(simulation.World.EntityTagValue(actor, StableRuntimeCode("flag")) > 0);
                break;
            case 3:
                Assert.Equal(EntityStatusCodes.Historical, simulation.World.EntityStatus(useTarget ? target : actor));
                break;
            case 4:
                Assert.Equal(10f + amount, simulation.World.GetAttribute(related, RelatedAttributeId), 4);
                break;
            case 5:
                Assert.Equal(amount, simulation.World.GetAttribute(actor, MarkerAttributeId), 4);
                break;
            case 6:
                Assert.Equal(0f, simulation.World.GetAttribute(actor, MarkerAttributeId));
                break;
            case 7:
                Assert.Equal(0f, simulation.World.GetAttribute(target, MarkerAttributeId));
                break;
            default:
                Assert.True(simulation.World.FindRelationship(actor, target, ids.RelationshipKindIdOf("link").Value) > 0);
                break;
        }
    }

    private static void InvokeFactoryPrecondition(
        Simulation simulation,
        int process,
        IPrecondition precondition,
        DomainIdRegistry ids)
    {
        var method = typeof(KernelSimulationFactory).GetMethod(
            "RegisterPreconditions",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        method.Invoke(null, [simulation, process, precondition, ids]);
    }

    private static void InvokeFactoryMutation(
        Simulation simulation,
        int process,
        IMutation mutation,
        DomainIdRegistry ids)
    {
        var method = typeof(KernelSimulationFactory).GetMethod(
            "RegisterMutation",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        method.Invoke(null, [simulation, process, mutation, ids]);
    }

    private static int RegisterSelfProcess(Simulation simulation, int processId)
    {
        var process = simulation.Processes.RegisterProcess(ActorKindId, processId);
        simulation.Processes.AddExpressionOp(process, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(process, ProcessExpressionCodes.Literal, x: 1f);
        return process;
    }

    private static int RegisterTargetProcess(Simulation simulation, int processId)
    {
        var process = simulation.Processes.RegisterProcess(
            ActorKindId,
            processId,
            targetMode: ProcessTargetModeCodes.KindOther,
            targetKindId: TargetKindId);
        simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.TargetIsNotSelf);
        simulation.Processes.AddExpressionOp(process, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(process, ProcessExpressionCodes.Literal, x: 1f);
        return process;
    }

    private static int CreateEntity(Simulation simulation, int kindId, int q, int r)
    {
        return simulation.CreateEntity(kindId, 1, 1, q, r, q, r, -q - r, 0);
    }

    private static void AddSelfMin(Simulation simulation, int process, float minimum)
    {
        simulation.Processes.AddPrecondition(
            process,
            ProcessPreconditionCodes.SelfAttributeRange,
            ValueAttributeId,
            1,
            0,
            x: minimum);
    }

    private static void AddTargetMin(Simulation simulation, int process, float minimum)
    {
        simulation.Processes.AddPrecondition(
            process,
            ProcessPreconditionCodes.TargetAttributeRange,
            TargetAttributeId,
            1,
            0,
            x: minimum);
    }

    private static IEnumerable<(float Self, float Target)> ThresholdPairs()
    {
        yield return (2f, 2f);
        yield return (2f, 6f);
        yield return (6f, 2f);
        yield return (6f, 6f);
        yield return (10f, 1f);
        yield return (10f, 10f);
    }

    private static bool CompositeExpected(int mode, float selfValue, float targetValue)
    {
        var selfPass = selfValue >= 5f;
        var targetPass = targetValue >= 5f;
        return mode switch
        {
            0 => selfPass || targetPass,
            1 => !selfPass,
            2 => selfPass && targetPass,
            3 => !(selfPass && targetPass),
            4 => !selfPass || targetPass,
            5 => selfPass && !targetPass,
            6 => !(selfPass || targetPass),
            _ => (selfPass || targetPass) && selfValue >= 10f,
        };
    }

    private static float ExpressionExpected(int mode, float selfValue, float targetValue, float literal)
    {
        return mode switch
        {
            0 => literal,
            1 => selfValue,
            2 => targetValue,
            3 => selfValue + targetValue,
            4 => selfValue - targetValue,
            5 => selfValue * targetValue,
            6 => targetValue == 0f ? 0f : selfValue / targetValue,
            7 => Math.Min(selfValue, targetValue),
            8 => Math.Max(selfValue, targetValue),
            9 => Math.Clamp(selfValue, 3f, 8f),
            10 => Math.Abs(literal),
            _ => 3f,
        };
    }

    private static DomainIdRegistry CreateDomainIds()
    {
        var ids = new DomainIdRegistry();
        ids.RegisterAttribute("value");
        ids.RegisterAttribute("target_value");
        ids.RegisterAttribute("marker");
        ids.RegisterAttribute("related_value");
        ids.RegisterPressure("heat");
        ids.RegisterRelationshipKind("link");
        ids.RegisterSubtype("created_subtype");
        ids.RegisterSubtype("default");
        ids.RegisterCulture("none");
        ids.RegisterResource(ResourceType.Food.ToDomainName());
        return ids;
    }

    private static Simulation CreateSimulation(int entityCapacity = 64, int relationshipCapacity = 64)
    {
        return new Simulation(new SimulationOptions
        {
            EntityCapacity = entityCapacity,
            RelationshipCapacity = relationshipCapacity,
            AttributeCapacity = 512,
            ResourceCapacity = 128,
            TagCapacity = 256,
            MapCellCapacity = 256,
            EventCapacity = 2048,
            ProcessCapacity = 128,
            ProcessStepCapacity = 2048,
            ProcessCooldownCapacity = 128,
            GeneratorCapacity = 4,
            GeneratorItemCapacity = 32,
            PlannerDemandCapacity = 64,
            PlannerProviderSelectorCapacity = 64,
            PlannerSupportRuleCapacity = 64,
            PlannerCommitmentCapacity = 256,
            PressureCapacity = 16,
            PressureValueCapacity = 128,
            DegeneracyDetectorCapacity = 4,
            DegeneracyStateCapacity = 16,
            LifecycleRuleCapacity = 8,
            AttributeRuleCapacity = 8,
            RelationshipRuleCapacity = 8,
            KindIdCapacity = 512,
            TerrainIdCapacity = 8,
            RelationshipKindIdCapacity = 512,
            Seed = 1,
        });
    }

    private static int StableRuntimeCode(string stableName)
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var character in stableName)
            {
                hash ^= character;
                hash *= 16777619u;
            }

            var value = (int)(hash & 2147483647u);
            return value == 0 ? 1 : value;
        }
    }
}
