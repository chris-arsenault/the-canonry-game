namespace Engine.Kernel.Tests;

using Engine.Kernel;

public sealed partial class ProcessProgramRuntimeTests
{
    private static void RegisterNeed(Simulation simulation, int kindId, int attributeId, float defaultValue)
    {
        simulation.RegisterAttribute(kindId, attributeId, 0f, 100f, defaultValue);
        simulation.Processes.RegisterAttributeObjective(
            kindId,
            attributeId,
            ProcessObjectiveUrgencyCodes.StandardNeed,
            0f,
            100f,
            defaultValue,
            baseline: 0f);
    }

    private static void RegisterTargetMarkerProcess(
        Simulation simulation,
        int actorKindId,
        int targetKindId,
        int processId,
        int effectAttributeId,
        int markerAttributeId,
        float reward)
    {
        var process = simulation.Processes.RegisterProcess(
            actorKindId,
            processId,
            targetMode: ProcessTargetModeCodes.KindOther,
            targetKindId: targetKindId);
        simulation.Processes.SetGoalEffect(process, ProcessEffectScopeCodes.TargetAttribute, effectAttributeId, targetKindId);
        simulation.Processes.AddProviderSelector(
            process,
            ProcessProviderSelectorCodes.TargetAttribute,
            targetKindId,
            effectAttributeId);
        simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.TargetIsNotSelf);
        simulation.Processes.AddExpressionOp(process, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(process, ProcessExpressionCodes.Literal, x: reward);
        simulation.Processes.AddStep(process, ProcessOperationCodes.AddTargetAttribute, markerAttributeId, x: 1f);
    }

    private static int RegisterSelfObjectiveTargetMarkerProcess(
        Simulation simulation,
        int actorKindId,
        int effectAttributeId,
        int processId,
        int targetKindId,
        int markerAttributeId)
    {
        var process = simulation.Processes.RegisterProcess(
            actorKindId,
            processId,
            targetMode: ProcessTargetModeCodes.KindOther,
            targetKindId: targetKindId);
        simulation.Processes.SetGoalEffect(process, ProcessEffectScopeCodes.SelfAttribute, effectAttributeId, actorKindId);
        simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.TargetIsNotSelf);
        simulation.Processes.AddExpressionOp(process, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(process, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddStep(process, ProcessOperationCodes.AddTargetAttribute, markerAttributeId, x: 1f);
        return process;
    }

    private static void RegisterKnownMap(Simulation simulation)
    {
        simulation.World.SetMapRadius(2);
        simulation.RegisterTerrain(1, boundaryGroupId: 1);
        for (var q = -2; q <= 2; q++)
        {
            var minR = Math.Max(-2, -q - 2);
            var maxR = Math.Min(2, -q + 2);
            for (var r = minR; r <= maxR; r++)
            {
                simulation.World.SetCell(q, r, terrainId: 1);
            }
        }
    }

    private static int HexDistance(int leftQ, int leftR, int rightQ, int rightR)
    {
        var leftS = -leftQ - leftR;
        var rightS = -rightQ - rightR;
        return Math.Max(
            Math.Abs(leftQ - rightQ),
            Math.Max(Math.Abs(leftR - rightR), Math.Abs(leftS - rightS)));
    }

    private static void SetSetupAttribute(
        Simulation simulation,
        int entityId,
        int attributeId,
        float value)
    {
        using var scope = simulation.EnterExecutionContext(simulation.ProcessSourceCodeId, programId: 0);
        simulation.SetAttribute(entityId, attributeId, value, tick: 0);
    }

    private static void SetSetupResource(
        Simulation simulation,
        int resourceId,
        int q,
        int r,
        float value)
    {
        using var scope = simulation.EnterExecutionContext(simulation.ProcessSourceCodeId, programId: 0);
        simulation.SetResource(resourceId, q, r, value, tick: 0);
    }

    private static Simulation CreateSimulation(KernelTuningOptions? tuning = null)
    {
        return new Simulation(new SimulationOptions
        {
            EntityCapacity = 8,
            RelationshipCapacity = 8,
            AttributeCapacity = 64,
            ResourceCapacity = 32,
            MapCellCapacity = 128,
            EventCapacity = 256,
            ProcessCapacity = 32,
            ProcessStepCapacity = 256,
            ProcessCooldownCapacity = 32,
            GeneratorCapacity = 4,
            GeneratorItemCapacity = 32,
            ProcessObjectiveCapacity = 16,
            PressureCapacity = 4,
            PressureValueCapacity = 16,
            DegeneracyDetectorCapacity = 4,
            DegeneracyStateCapacity = 16,
            LifecycleRuleCapacity = 8,
            EntityAttributeCapacity = 8,
            UpkeepRuleCapacity = 8,
            RelationshipRuleCapacity = 8,
            KindIdCapacity = 8,
            TerrainIdCapacity = 8,
            KernelTuning = tuning ?? new KernelTuningOptions(),
            Seed = 1,
        });
    }
}
