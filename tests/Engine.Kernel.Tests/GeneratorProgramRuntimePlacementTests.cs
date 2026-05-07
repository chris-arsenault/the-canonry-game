namespace Engine.Kernel.Tests;

using Engine.Kernel;
using Engine.Kernel.Programs;

public sealed class GeneratorProgramRuntimePlacementTests
{
    private const int TargetKindId = 1;
    private const int CreatedKindId = 2;
    private const int TargetSubtypeId = 10;
    private const int CreatedSubtypeId = 11;
    private const int CultureId = 20;
    private const int GeneratorId = 100;
    private const int CreatedRef = 2;

    [Fact]
    public void Fire_WithAreaProducedPlacement_RespectsConfiguredRadiusBand()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        simulation.World.SetMapRadius(2);
        CreateTarget(simulation);
        var slot = RegisterGenerator(simulation, includeProduced: false);
        AddAreaProducedEntity(
            simulation,
            slot,
            physicalMinRadius: 2,
            physicalMaxRadius: 2,
            semanticMinRadius: 2,
            semanticMaxRadius: 2);

        simulation.AdvanceTicks(1);

        var created = simulation.World.EntityHighWatermark;
        Assert.Equal(2, HexDistance(0, 0, simulation.World.EntityPhysicalQ(created), simulation.World.EntityPhysicalR(created)));
        Assert.Equal(2, HexDistance(0, 0, simulation.World.EntitySemanticQ(created), simulation.World.EntitySemanticR(created)));
    }

    [Fact]
    public void Fire_WithAreaProducedPlacement_TreatsAllowedTerrainAsHardFilter()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        simulation.RegisterTerrain(2, boundaryGroupId: 1);
        simulation.World.SetCellTerrain(2, 0, terrainId: 2);
        CreateTarget(simulation);
        var slot = RegisterGenerator(simulation, includeProduced: false);
        var producedSlot = AddAreaProducedEntity(
            simulation,
            slot,
            physicalMinRadius: 1,
            physicalMaxRadius: 2,
            semanticMinRadius: 1,
            semanticMaxRadius: 1);
        simulation.Generators.AddProducedPlacementAllowedTerrain(producedSlot, PlaneCodes.Physical, terrainId: 2);

        simulation.AdvanceTicks(1);

        var created = simulation.World.EntityHighWatermark;
        Assert.Equal(2, simulation.World.EntityPhysicalQ(created));
        Assert.Equal(0, simulation.World.EntityPhysicalR(created));
    }

    [Fact]
    public void Fire_WithNoAllowedPlacementCandidate_FailsWithoutConsumingPulseBudget()
    {
        var simulation = CreateSimulation(pulseWidthTicks: 25, firesPerPulse: 1);
        SeedMap(simulation);
        simulation.RegisterTerrain(2, boundaryGroupId: 1);
        CreateTarget(simulation);
        var slot = RegisterGenerator(simulation, includeProduced: false);
        var producedSlot = AddAreaProducedEntity(
            simulation,
            slot,
            physicalMinRadius: 1,
            physicalMaxRadius: 1,
            semanticMinRadius: 1,
            semanticMaxRadius: 1);
        simulation.Generators.AddProducedPlacementAllowedTerrain(producedSlot, PlaneCodes.Physical, terrainId: 2);

        simulation.AdvanceTicks(1);

        Assert.Equal(1, simulation.World.EntityHighWatermark);

        simulation.World.SetCellTerrain(1, 0, terrainId: 2);
        simulation.AdvanceTicks(1);

        Assert.Equal(2, simulation.World.EntityHighWatermark);
        Assert.Equal(1, simulation.World.EntityPhysicalQ(2));
        Assert.Equal(0, simulation.World.EntityPhysicalR(2));
    }

    [Fact]
    public void Fire_WithAreaProducedPlacement_SelectsBestTerrainAcrossWholeRadiusBand()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        simulation.RegisterTerrain(2, boundaryGroupId: 1);
        simulation.World.SetCellTerrain(2, 0, terrainId: 2);
        CreateTarget(simulation);
        var slot = RegisterGenerator(simulation, includeProduced: false);
        var producedSlot = AddAreaProducedEntity(
            simulation,
            slot,
            physicalMinRadius: 1,
            physicalMaxRadius: 2,
            semanticMinRadius: 1,
            semanticMaxRadius: 1);
        simulation.Generators.AddProducedPlacementPreferredTerrain(producedSlot, PlaneCodes.Physical, terrainId: 2);

        simulation.AdvanceTicks(1);

        var created = simulation.World.EntityHighWatermark;
        Assert.Equal(2, simulation.World.EntityPhysicalQ(created));
        Assert.Equal(0, simulation.World.EntityPhysicalR(created));
    }

    [Fact]
    public void Fire_WithPhysicalPlacement_UsesSeedStyleTerrainPreferenceInsteadOfTravelCost()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        simulation.RegisterTerrain(
            terrainId: 2,
            boundaryGroupId: 1,
            movementCost: 20f,
            influenceMultiplier: 1f,
            claimCostMultiplier: 1f,
            claimChanceMultiplier: 1f,
            roadBuildMultiplier: 1f,
            roadSpeedMultiplier: 1f);
        simulation.World.SetCellTerrain(1, 0, terrainId: 1);
        simulation.World.SetCellTerrain(-1, 0, terrainId: 2);
        CreateTarget(simulation);
        var slot = RegisterGenerator(simulation, includeProduced: false);
        var producedSlot = AddAreaProducedEntity(
            simulation,
            slot,
            physicalMinRadius: 1,
            physicalMaxRadius: 1,
            semanticMinRadius: 1,
            semanticMaxRadius: 1);
        simulation.Generators.AddProducedPlacementPreferredTerrain(producedSlot, PlaneCodes.Physical, terrainId: 2);

        simulation.AdvanceTicks(1);

        var created = simulation.World.EntityHighWatermark;
        Assert.Equal(-1, simulation.World.EntityPhysicalQ(created));
        Assert.Equal(0, simulation.World.EntityPhysicalR(created));
    }

    [Fact]
    public void Fire_WithSemanticPlacement_UsesSeedStyleTerrainPreferenceInsteadOfTravelCost()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        simulation.World.SetMapRadius(2);
        simulation.RegisterTerrain(
            terrainId: 2,
            boundaryGroupId: 2,
            movementCost: 20f,
            influenceMultiplier: 1f,
            claimCostMultiplier: 1f,
            claimChanceMultiplier: 1f,
            roadBuildMultiplier: 1f,
            roadSpeedMultiplier: 1f);
        simulation.World.SetCellTerrain(PlaneCodes.Semantic, 1, 0, terrainId: 1);
        simulation.World.SetCellTerrain(PlaneCodes.Semantic, -1, 0, terrainId: 2);
        CreateTarget(simulation);
        var slot = RegisterGenerator(simulation, includeProduced: false);
        var producedSlot = AddAreaProducedEntity(
            simulation,
            slot,
            physicalMinRadius: 1,
            physicalMaxRadius: 1,
            semanticMinRadius: 1,
            semanticMaxRadius: 1);
        simulation.Generators.AddProducedPlacementPreferredTerrain(producedSlot, PlaneCodes.Semantic, terrainId: 2);

        simulation.AdvanceTicks(1);

        var created = simulation.World.EntityHighWatermark;
        Assert.Equal(-1, simulation.World.EntitySemanticQ(created));
        Assert.Equal(0, simulation.World.EntitySemanticR(created));
    }

    [Fact]
    public void Fire_WithLocalityProducedPlacement_PrefersOwnedCoverageTerrain()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        simulation.RegisterTerrain(2, boundaryGroupId: 1);
        simulation.World.SetCellTerrain(-2, 0, terrainId: 2);
        var target = CreateTarget(simulation);
        simulation.World.SetCoverageOwner(2, 0, target);
        simulation.World.SetCoverageOwner(-2, 0, target);
        var slot = RegisterGenerator(simulation, includeProduced: false);
        var producedSlot = simulation.Generators.AddProduced(
            slot,
            CreatedRef,
            CreatedKindId,
            CreatedSubtypeId,
            createIfMissing: 1,
            hasQuery: 0,
            queryAdjacentToTarget: 0,
            queryCultureMatchCode: GeneratorCultureMatchCodes.Any,
            physicalPlacementCode: GeneratorPlacementCodes.Locality,
            semanticPlacementCode: GeneratorPlacementCodes.Area,
            placementRefCode: GeneratorProgramConstants.TargetReferenceCode,
            cultureRefCode: GeneratorProgramConstants.TargetReferenceCode,
            createChance: 1f,
            coverageModeCode: GeneratorCoverageModeCodes.None,
            coverageSourceRefCode: 0,
            coverageDesiredCellCount: 0,
            coverageMinimumCellCount: 0,
            coverageMinimumSourceRemainingCellCount: 0,
            coverageRespectTerrainBoundaries: 0,
            coverageTransformTerrainId: 0,
            physicalPlacementMinRadius: 2,
            physicalPlacementMaxRadius: 2,
            semanticPlacementMinRadius: 1,
            semanticPlacementMaxRadius: 1);
        simulation.Generators.AddProducedPlacementPreferredTerrain(producedSlot, PlaneCodes.Physical, terrainId: 2);

        simulation.AdvanceTicks(1);

        var created = simulation.World.EntityHighWatermark;
        Assert.Equal(-2, simulation.World.EntityPhysicalQ(created));
        Assert.Equal(0, simulation.World.EntityPhysicalR(created));
    }

    [Fact]
    public void Fire_WithSemanticProducedPlacement_UsesSemanticTerrainFilter()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        simulation.World.SetMapRadius(2);
        simulation.RegisterTerrain(2, boundaryGroupId: 2);
        simulation.World.SetCellTerrain(PlaneCodes.Semantic, 1, 0, terrainId: 2);
        CreateTarget(simulation);
        var slot = RegisterGenerator(simulation, includeProduced: false);
        var producedSlot = AddAreaProducedEntity(
            simulation,
            slot,
            physicalMinRadius: 1,
            physicalMaxRadius: 1,
            semanticMinRadius: 1,
            semanticMaxRadius: 1);
        simulation.Generators.AddProducedPlacementAllowedTerrain(producedSlot, PlaneCodes.Semantic, terrainId: 2);

        simulation.AdvanceTicks(1);

        var created = simulation.World.EntityHighWatermark;
        var q = simulation.World.EntitySemanticQ(created);
        var r = simulation.World.EntitySemanticR(created);
        Assert.True(
            q == 1 && r == 0,
            $"Expected semantic placement at 1,0; actual {q},{r} terrain {simulation.World.CellTerrainId(PlaneCodes.Semantic, q, r)}.");
    }

    [Fact]
    public void Fire_WithLocalityProducedPlacement_UsesOwnedCoverageCellsWithinRadiusBand()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        simulation.World.SetMapRadius(2);
        var target = CreateTarget(simulation);
        simulation.World.SetCoverageOwner(2, 0, target);
        simulation.SetCellState(PlaneCodes.Semantic, -2, 0, terrainId: 1, ownerEntityId: target, tick: 0);
        var slot = RegisterGenerator(simulation, includeProduced: false);
        simulation.Generators.AddProduced(
            slot,
            CreatedRef,
            CreatedKindId,
            CreatedSubtypeId,
            createIfMissing: 1,
            hasQuery: 0,
            queryAdjacentToTarget: 0,
            queryCultureMatchCode: GeneratorCultureMatchCodes.Any,
            physicalPlacementCode: GeneratorPlacementCodes.Locality,
            semanticPlacementCode: GeneratorPlacementCodes.Locality,
            placementRefCode: GeneratorProgramConstants.TargetReferenceCode,
            cultureRefCode: GeneratorProgramConstants.TargetReferenceCode,
            createChance: 1f,
            coverageModeCode: GeneratorCoverageModeCodes.None,
            coverageSourceRefCode: 0,
            coverageDesiredCellCount: 0,
            coverageMinimumCellCount: 0,
            coverageMinimumSourceRemainingCellCount: 0,
            coverageRespectTerrainBoundaries: 0,
            coverageTransformTerrainId: 0,
            physicalPlacementMinRadius: 2,
            physicalPlacementMaxRadius: 2,
            semanticPlacementMinRadius: 2,
            semanticPlacementMaxRadius: 2);

        simulation.AdvanceTicks(1);

        var created = simulation.World.EntityHighWatermark;
        Assert.Equal(2, simulation.World.EntityPhysicalQ(created));
        Assert.Equal(0, simulation.World.EntityPhysicalR(created));
        Assert.Equal(-2, simulation.World.EntitySemanticQ(created));
        Assert.Equal(0, simulation.World.EntitySemanticR(created));
    }

    [Fact]
    public void Fire_UsesWorldMapRadiusForSemanticProducedPlacement()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation);
        simulation.World.SetMapRadius(30);
        simulation.CreateEntity(TargetKindId, TargetSubtypeId, CultureId, 0, 0, 25, 0, -25, tick: 0);
        var slot = RegisterGenerator(simulation, includeProduced: false);
        AddAreaProducedEntity(
            simulation,
            slot,
            physicalMinRadius: 1,
            physicalMaxRadius: 1,
            semanticMinRadius: 0,
            semanticMaxRadius: 0);

        simulation.AdvanceTicks(1);

        var created = simulation.World.EntityHighWatermark;
        var q = simulation.World.EntitySemanticQ(created);
        var r = simulation.World.EntitySemanticR(created);
        Assert.Equal(25, q);
        Assert.Equal(0, r);
        Assert.True(HexDistance(0, 0, q, r) <= simulation.World.MapRadius);
    }

    private static int RegisterGenerator(
        Simulation simulation,
        bool includeProduced = true)
    {
        var slot = simulation.Generators.RegisterGenerator(
            GeneratorId,
            TargetKindId,
            pulseWeight: 1,
            maxRuns: 0);
        simulation.Generators.AddTrigger(slot, TargetKindId);
        if (includeProduced)
        {
            AddAreaProducedEntity(
                simulation,
                slot,
                physicalMinRadius: 0,
                physicalMaxRadius: 5,
                semanticMinRadius: 0,
                semanticMaxRadius: 5);
        }

        return slot;
    }

    private static int AddAreaProducedEntity(
        Simulation simulation,
        int generatorSlot,
        int physicalMinRadius,
        int physicalMaxRadius,
        int semanticMinRadius,
        int semanticMaxRadius) =>
        simulation.Generators.AddProduced(
            generatorSlot,
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
            cultureRefCode: GeneratorProgramConstants.TargetReferenceCode,
            createChance: 1f,
            coverageModeCode: GeneratorCoverageModeCodes.None,
            coverageSourceRefCode: 0,
            coverageDesiredCellCount: 0,
            coverageMinimumCellCount: 0,
            coverageMinimumSourceRemainingCellCount: 0,
            coverageRespectTerrainBoundaries: 0,
            coverageTransformTerrainId: 0,
            physicalPlacementMinRadius: physicalMinRadius,
            physicalPlacementMaxRadius: physicalMaxRadius,
            semanticPlacementMinRadius: semanticMinRadius,
            semanticPlacementMaxRadius: semanticMaxRadius);

    private static int CreateTarget(Simulation simulation) =>
        simulation.CreateEntity(TargetKindId, TargetSubtypeId, CultureId, 0, 0, 0, 0, 0, tick: 0);

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
                simulation.SetCellState(PlaneCodes.Semantic, q, r, terrainId: 1, ownerEntityId: 0, tick: 0);
            }
        }
    }

    private static int HexDistance(int aq, int ar, int bq, int br)
    {
        var asCoord = -aq - ar;
        var bsCoord = -bq - br;
        return (Math.Abs(aq - bq) + Math.Abs(ar - br) + Math.Abs(asCoord - bsCoord)) / 2;
    }

    private static Simulation CreateSimulation(
        int seed = 1,
        int pulseWidthTicks = GeneratorProgramConstants.DefaultPulseWidthTicks,
        int firesPerPulse = GeneratorProgramConstants.DefaultFiresPerPulse) => new(new SimulationOptions
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
            SystemCapacity = 16,
            KernelTuning = new KernelTuningOptions
            {
                Programs = new KernelProgramTuningOptions
                {
                    Generators = new GeneratorProgramTuningOptions
                    {
                        PulseWidthTicks = pulseWidthTicks,
                        FiresPerPulse = firesPerPulse,
                    },
                },
            },
            Seed = seed,
        });
}
