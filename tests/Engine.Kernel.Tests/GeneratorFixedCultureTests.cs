namespace Engine.Kernel.Tests;

using Engine.Kernel;
using Engine.Kernel.Programs;

public sealed class GeneratorFixedCultureTests
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

    private static int CreateTarget(Simulation simulation) =>
        simulation.CreateEntity(TargetKindId, TargetSubtypeId, TargetCultureId, 0, 0, 0, 0, 0, tick: 0);

    private static void RegisterGenerator(
        Simulation simulation,
        int fixedCultureId = 0)
    {
        var slot = simulation.Generators.RegisterGenerator(
            GeneratorId,
            TargetKindId,
            fireWeight: 1,
            maxRuns: 0);
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

    private static Simulation CreateSimulation() =>
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
            Seed = 1,
        });
}
