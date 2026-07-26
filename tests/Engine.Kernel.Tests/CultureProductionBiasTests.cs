namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

public sealed class CultureProductionBiasTests
{
    private const int KindId = 1;
    private const int BiasedCultureId = 2;
    private const int NeutralCultureId = 3;

    [Fact]
    public void EntityProduction_ScalesByCulturePlaneMultiplier()
    {
        var simulation = CreateSimulation();
        simulation.RegisterCultureProduction(BiasedCultureId, physicalMultiplier: FixedScalar.FromSingle(0.5f), semanticMultiplier: FixedScalar.FromSingle(2f));
        var biased = simulation.CreateEntity(KindId, 1, BiasedCultureId, 0, 0, 0, 0, 0, tick: 0);
        var neutral = simulation.CreateEntity(KindId, 1, NeutralCultureId, 1, 0, 0, 0, 0, tick: 0);

        _ = simulation.ModifyCellCultureInfluenceRawFromEntity(biased, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(10f), tick: 1);
        _ = simulation.ModifyCellCultureInfluenceRawFromEntity(biased, PlaneCodes.Semantic, 0, 0, FixedScalar.FromSingle(10f), tick: 1);
        _ = simulation.ModifyCellCultureInfluenceRawFromEntity(neutral, PlaneCodes.Physical, 1, 0, FixedScalar.FromSingle(10f), tick: 1);

        Assert.Equal(FixedScalar.FromSingle(5f), simulation.World.GetCellCultureInfluenceRaw(BiasedCultureId, PlaneCodes.Physical, 0, 0));
        Assert.Equal(FixedScalar.FromSingle(20f), simulation.World.GetCellCultureInfluenceRaw(BiasedCultureId, PlaneCodes.Semantic, 0, 0));
        Assert.Equal(FixedScalar.FromSingle(10f), simulation.World.GetCellCultureInfluenceRaw(NeutralCultureId, PlaneCodes.Physical, 1, 0));
    }

    [Fact]
    public void NegativeDeltas_AreNotScaled()
    {
        var simulation = CreateSimulation();
        simulation.RegisterCultureProduction(BiasedCultureId, physicalMultiplier: FixedScalar.FromSingle(2f), semanticMultiplier: FixedScalar.FromSingle(1f));
        var entity = simulation.CreateEntity(KindId, 1, BiasedCultureId, 0, 0, 0, 0, 0, tick: 0);

        _ = simulation.ModifyCellCultureInfluenceRawFromEntity(entity, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(10f), tick: 1);
        _ = simulation.ModifyCellCultureInfluenceRawFromEntity(entity, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(-10f), tick: 1);

        Assert.Equal(FixedScalar.FromSingle(10f), simulation.World.GetCellCultureInfluenceRaw(BiasedCultureId, PlaneCodes.Physical, 0, 0));
    }

    private static Simulation CreateSimulation()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions
        {
            EntityCapacity = 8,
            MapCellCapacity = 128,
            MapInfluenceCapacity = 128,
            EventCapacity = 1024,
            SystemCapacity = 24,
            KernelTuning = new KernelTuningOptions
            {
                Programs = new KernelProgramTuningOptions
                {
                    CultureField = new CultureFieldTuningOptions
                    {
                        DecayPerRun = FixedScalar.FromSingle(0f),
                        MinimumDecayPerRun = FixedScalar.FromSingle(0f),
                        RemittanceShare = FixedScalar.FromSingle(0f),
                    },
                },
            },
        });
        simulation.World.SetMapRadius(2);
        simulation.RegisterTerrain(1, boundaryGroupId: 1);
        for (var q = -2; q <= 2; q++)
        {
            var minR = Math.Max(-2, -q - 2);
            var maxR = Math.Min(2, -q + 2);
            for (var r = minR; r <= maxR; r++)
            {
                simulation.World.SetCell(PlaneCodes.Physical, q, r, terrainId: 1);
                simulation.World.SetCell(PlaneCodes.Semantic, q, r, terrainId: 1);
            }
        }

        return simulation;
    }
}
