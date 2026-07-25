namespace Engine.Kernel.Tests;

using Engine.Kernel;
using Engine.Kernel.Programs;

public sealed class GeneratorCadenceAndFixedCultureTests
{
    private const int TargetKindId = 1;
    private const int CreatedKindId = 2;
    private const int TargetSubtypeId = 10;
    private const int CreatedSubtypeId = 11;
    private const int TargetCultureId = 20;
    private const int FixedCultureId = 21;
    private const int GeneratorId = 100;
    private const int CreatedRef = 2;

    [Fact]
    public void FixedCulture_ProducedEntityIgnoresTargetCulture()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        var target = CreateTarget(simulation);
        RegisterGenerator(simulation, fixedCultureId: FixedCultureId);

        simulation.AdvanceTicks(1);

        var created = simulation.World.EntityHighWatermark;
        Assert.True(created > target, "Generator did not create an entity.");
        Assert.Equal(TargetCultureId, simulation.World.EntityCultureId(target));
        Assert.Equal(FixedCultureId, simulation.World.EntityCultureId(created));
    }

    [Fact]
    public void CadenceGenerator_FiresOnItsOwnTimelineOutsidePulseBudget()
    {
        // Pool budget of zero fires: only the cadence timeline can create.
        var simulation = CreateSimulation(firesPerPulse: 0);
        SeedMap(simulation);
        CreateTarget(simulation);
        RegisterGenerator(simulation, cadenceTicks: 10);

        simulation.AdvanceTicks(60);

        var created = simulation.World.EntityHighWatermark - 1;
        Assert.InRange(created, 3, 7);
    }

    [Fact]
    public void CadenceGenerator_DoesNotCompeteInTheSharedPulsePool()
    {
        var simulation = CreateSimulation(firesPerPulse: 1);
        SeedMap(simulation);
        CreateTarget(simulation);
        // In the pool (width 25, 1 fire/pulse) this generator would fire twice
        // over 40 ticks. On its own sparse timeline it fires at most the one
        // desynchronized initial time.
        RegisterGenerator(simulation, cadenceTicks: 5000);

        simulation.AdvanceTicks(40);

        Assert.InRange(simulation.World.EntityHighWatermark - 1, 0, 1);
    }

    private static int CreateTarget(Simulation simulation) =>
        simulation.CreateEntity(TargetKindId, TargetSubtypeId, TargetCultureId, 0, 0, 0, 0, 0, tick: 0);

    private static void RegisterGenerator(
        Simulation simulation,
        int cadenceTicks = 0,
        int fixedCultureId = 0)
    {
        var slot = simulation.Generators.RegisterGenerator(
            GeneratorId,
            TargetKindId,
            pulseWeight: 1,
            maxRuns: 0,
            cadenceTicks,
            cadenceJitterTicks: 0);
        simulation.Generators.AddTrigger(slot, TargetKindId);
        simulation.Generators.AddProduced(
            slot,
            CreatedRef,
            CreatedKindId,
            CreatedSubtypeId,
            createIfMissing: 1,
            hasQuery: 0,
            queryAdjacentToTarget: 0,
            queryCultureMatchCode: GeneratorCultureMatchCodes.Any,
            physicalPlacementCode: GeneratorPlacementCodes.Area,
            semanticPlacementCode: GeneratorPlacementCodes.Area,
            placementRefCode: GeneratorProgramConstants.TargetReferenceCode,
            cultureRefCode: fixedCultureId > 0 ? 0 : GeneratorProgramConstants.TargetReferenceCode,
            createChance: FixedScalar.FromSingle(1f),
            coverageModeCode: GeneratorCoverageModeCodes.None,
            coverageSourceRefCode: 0,
            coverageDesiredCellCount: 0,
            coverageMinimumCellCount: 0,
            coverageMinimumSourceRemainingCellCount: 0,
            coverageRespectTerrainBoundaries: 0,
            coverageTransformTerrainId: 0,
            fixedCultureId: fixedCultureId);
    }

    private static void SeedMap(Simulation simulation)
    {
        simulation.RegisterTerrain(1, boundaryGroupId: 1);
        simulation.World.SetMapRadius(2);
        for (var q = -2; q <= 2; q++)
        {
            var minR = Math.Max(-2, -q - 2);
            var maxR = Math.Min(2, -q + 2);
            for (var r = minR; r <= maxR; r++)
            {
                simulation.CreateCell(q, r, terrainId: 1, ownerEntityId: 0);
                simulation.CreateCell(PlaneCodes.Semantic, q, r, terrainId: 1, ownerEntityId: 0);
            }
        }
    }

    private static Simulation CreateSimulation(int firesPerPulse = GeneratorProgramConstants.DefaultFiresPerPulse) =>
        KernelTestSimulation.Create(new SimulationOptions
        {
            EntityCapacity = 16,
            RelationshipCapacity = 16,
            AttributeCapacity = 32,
            MapCellCapacity = 128,
            EventCapacity = 512,
            GeneratorCapacity = 4,
            GeneratorItemCapacity = 64,
            RelationshipKindIdCapacity = 64,
            TerrainIdCapacity = 8,
            SystemCapacity = 24,
            KernelTuning = new KernelTuningOptions
            {
                Programs = new KernelProgramTuningOptions
                {
                    Generators = new GeneratorProgramTuningOptions
                    {
                        FiresPerPulse = firesPerPulse,
                    },
                },
            },
            Seed = 1,
        });
}
