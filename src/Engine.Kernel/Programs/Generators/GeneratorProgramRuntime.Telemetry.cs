namespace Engine.Kernel;

internal sealed partial class GeneratorProgramRuntime
{
    private void PublishGeneratorTargetEvaluation(
        Simulation simulation,
        int generatorSlot,
        int targetId,
        int statusCode,
        int rejectionReasonCode)
    {
        var world = simulation.World;
        var cultureId = world.EntityCultureId(targetId);
        var physicalQ = world.EntityPhysicalQ(targetId);
        var physicalR = world.EntityPhysicalR(targetId);
        var semanticQ = world.EntitySemanticQ(targetId);
        var semanticR = world.EntitySemanticR(targetId);
        var semanticS = world.EntitySemanticS(targetId);
        var physicalField = cultureId <= 0
            ? default
            : simulation.CultureLocality.CellAt(world, cultureId, PlaneCodes.Physical, physicalQ, physicalR);
        var semanticField = cultureId <= 0
            ? default
            : simulation.CultureLocality.CellAt(world, cultureId, PlaneCodes.Semantic, semanticQ, semanticR);
        simulation.EmitGeneratorTargetEvaluated(
            simulation.ExecutionTick,
            _generatorIds[generatorSlot],
            targetId,
            world.EntityKindId(targetId),
            world.EntitySubtypeId(targetId),
            cultureId,
            physicalQ,
            physicalR,
            semanticQ,
            semanticR,
            semanticS,
            statusCode,
            rejectionReasonCode,
            CultureFieldBand(physicalField),
            physicalField.CellKindCode,
            CultureFieldBand(semanticField),
            semanticField.CellKindCode,
            physicalField.LocalRawMass,
            physicalField.ConnectedRawMass,
            semanticField.LocalRawMass,
            semanticField.ConnectedRawMass);
    }

    private static int CultureFieldBand(in CultureLocalityCell field) =>
        field.StrengthBandCode;
}
