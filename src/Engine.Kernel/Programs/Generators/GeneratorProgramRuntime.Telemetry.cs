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
            : simulation.CultureBoundaries.CellAt(world, cultureId, PlaneCodes.Physical, physicalQ, physicalR);
        var semanticField = cultureId <= 0
            ? default
            : simulation.CultureBoundaries.CellAt(world, cultureId, PlaneCodes.Semantic, semanticQ, semanticR);
        simulation.EmitGeneratorTargetEvaluated(
            simulation.CurrentTick,
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
            semanticField.ConnectedRawMass,
            physicalField.ComponentId,
            physicalField.ComponentCellCount,
            physicalField.ComponentRawMass,
            semanticField.ComponentId,
            semanticField.ComponentCellCount,
            semanticField.ComponentRawMass);
    }

    private static int CultureFieldBand(in CultureBoundaryCell field) =>
        field.StrengthBandCode;
}
