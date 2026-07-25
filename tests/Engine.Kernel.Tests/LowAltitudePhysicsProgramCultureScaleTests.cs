namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

public sealed partial class LowAltitudePhysicsProgramTests
{
    [Fact]
    public void LowAltitudeVerbProgram_CultureInfluenceActionScaleDoesNotScaleInitialInfluence()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions
        {
            KernelTuning = new KernelTuningOptions
            {
                Programs = new KernelProgramTuningOptions
                {
                    CultureInfluenceActionScale = FixedScalar.FromSingle(0.25f),
                },
            },
        });
        const int kindId = 1;
        simulation.World.SetCell(PlaneCodes.Physical, 0, 0, terrainId: 1);
        simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.LowAltitudeVerbs.RegisterInitialInfluence(kindId, PlaneCodes.Physical, FixedScalar.FromSingle(10f));

        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(10f), simulation.World.GetCellCultureInfluenceRaw(1, PlaneCodes.Physical, 0, 0));
    }

    [Fact]
    public void LowAltitudeVerbProgram_CultureInfluenceActionScaleScalesActionInfluence()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions
        {
            KernelTuning = new KernelTuningOptions
            {
                Programs = new KernelProgramTuningOptions
                {
                    CultureInfluenceActionScale = FixedScalar.FromSingle(0.25f),
                },
            },
        });
        const int kindId = 1;
        const int verbId = 91;
        simulation.RegisterTerrain(1, boundaryGroupId: 1);
        simulation.World.SetCell(PlaneCodes.Physical, 0, 0, terrainId: 1);
        var entityId = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.SetCellInfluence(entityId, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(10f), tick: 0);
        var verbSlot = simulation.LowAltitudeVerbs.RegisterVerb(
            kindId,
            verbId,
            PlaneCodes.Physical,
            ProgramOpcodeCodes.AddOwnedCellScalar,
            TargetSelectorCodes.SelfMapValue,
            targetKindId: 0,
            relationshipKindId: 0,
            relationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Either,
            minCommitmentTicks: 1,
            maxCommitmentTicks: 1,
            dwellTicks: 1,
            targetRadius: 0,
            candidateSampleCount: 1,
            weight: FixedScalar.FromSingle(1f),
            influenceDelta: FixedScalar.FromSingle(4f),
            influenceRadius: 0,
            coverageGrowthChance: FixedScalar.FromSingle(0f),
            headquartersMoveChance: FixedScalar.FromSingle(0f),
            requiredInfluence: FixedScalar.FromSingle(0f));
        AddPrimitiveSteps(simulation, verbSlot, ProgramOpcodeCodes.AddOwnedCellScalar);

        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(11f), simulation.World.GetCellInfluence(entityId, PlaneCodes.Physical, 0, 0));
    }
}
