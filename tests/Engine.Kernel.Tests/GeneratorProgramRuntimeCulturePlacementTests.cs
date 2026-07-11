namespace Engine.Kernel.Tests;

using Engine.Kernel;
using Engine.Kernel.Programs;

public sealed class GeneratorProgramRuntimeCulturePlacementTests
{
    private const int TargetKindId = 1;
    private const int CreatedKindId = 2;
    private const int PatronKindId = 3;
    private const int TargetSubtypeId = 10;
    private const int CreatedSubtypeId = 11;
    private const int PatronSubtypeId = 12;
    private const int CultureId = 20;
    private const int GeneratorId = 100;
    private const int CreatedRef = 2;
    private const int PatronRef = 3;

    [Fact]
    public void TargetSelection_WithCultureInteriorPlacement_FavorsSupportedTarget()
    {
        var supportedSelections = 0;
        const int sampleCount = 8;
        for (var seed = 1; seed <= sampleCount; seed++)
        {
            var simulation = CreateSimulation(seed);
            SeedMap(simulation, radius: 3);
            simulation.CreateEntity(TargetKindId, TargetSubtypeId, CultureId, -2, 0, -2, 0, -2, tick: 0);
            simulation.CreateEntity(TargetKindId, TargetSubtypeId, CultureId, 2, 0, 2, 0, 2, tick: 0);
            AddCulturePatch(simulation, CultureId, PlaneCodes.Physical, -2, 0, WorldState.CultureEntrenchedRawMass);
            AddCulturePatch(simulation, CultureId, PlaneCodes.Semantic, -2, 0, WorldState.CultureEntrenchedRawMass);
            var slot = RegisterGenerator(simulation);
            AddCreatedEntity(
                simulation,
                slot,
                physicalMinRadius: 1,
                physicalMaxRadius: 1,
                semanticMinRadius: 1,
                semanticMaxRadius: 1,
                culturePlacementCode: GeneratorCulturePlacementCodes.CultureInterior);

            simulation.AdvanceTicks(1);

            var created = simulation.World.EntityHighWatermark;
            if (simulation.World.EntityPhysicalQ(created) < 0)
            {
                supportedSelections++;
            }
        }

        Assert.True(
            supportedSelections >= 6,
            $"Expected supported target bias; selected {supportedSelections}/{sampleCount}.");
    }

    [Fact]
    public void Fire_WithCultureInteriorPlacement_PrefersInteriorCandidate()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation, radius: 2);
        simulation.RegisterTerrain(2, boundaryGroupId: 1);
        simulation.World.SetCellTerrain(1, 0, terrainId: 2);
        var target = simulation.CreateEntity(TargetKindId, TargetSubtypeId, CultureId, 0, 0, 0, 0, 0, tick: 0);
        simulation.SetCellInfluence(target, PlaneCodes.Physical, 1, 0, WorldState.CultureTraceRawMass, tick: 0);
        simulation.SetCellInfluence(target, PlaneCodes.Physical, -1, 0, WorldState.CultureEntrenchedRawMass, tick: 0);
        simulation.SetCellInfluence(target, PlaneCodes.Semantic, -1, 0, WorldState.CultureEntrenchedRawMass, tick: 0);
        var slot = RegisterGenerator(simulation);
        var producedSlot = AddCreatedEntity(
            simulation,
            slot,
            physicalMinRadius: 1,
            physicalMaxRadius: 1,
            semanticMinRadius: 1,
            semanticMaxRadius: 1,
            culturePlacementCode: GeneratorCulturePlacementCodes.CultureInterior);
        simulation.Generators.AddProducedPlacementPreferredTerrain(producedSlot, PlaneCodes.Physical, terrainId: 2);

        simulation.AdvanceTicks(1);

        var created = simulation.World.EntityHighWatermark;
        Assert.Equal(-1, simulation.World.EntityPhysicalQ(created));
        Assert.Equal(0, simulation.World.EntityPhysicalR(created));
    }

    [Fact]
    public void Fire_WithCultureInteriorPlacement_RejectsPlacementWithoutOwnCultureSupport()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation, radius: 2);
        var target = simulation.CreateEntity(TargetKindId, TargetSubtypeId, CultureId, 0, 0, 0, 0, 0, tick: 0);
        var slot = RegisterGenerator(simulation);
        _ = AddCreatedEntity(
            simulation,
            slot,
            physicalMinRadius: 1,
            physicalMaxRadius: 1,
            semanticMinRadius: 1,
            semanticMaxRadius: 1,
            culturePlacementCode: GeneratorCulturePlacementCodes.CultureInterior);

        simulation.AdvanceTicks(1);

        // No own-culture support exists anywhere, so the interior-mode output
        // must not be founded; the target remains the newest entity.
        Assert.Equal(target, simulation.World.EntityHighWatermark);
    }

    [Fact]
    public void SecondaryBinding_WithSameCultureComponent_RejectsRemoteSameCultureEntity()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation, radius: 6);
        simulation.CreateEntity(TargetKindId, TargetSubtypeId, CultureId, 0, 0, 0, 0, 0, tick: 0);
        simulation.CreateEntity(PatronKindId, PatronSubtypeId, CultureId, 4, 0, 4, 0, 4, tick: 0);
        simulation.CreateEntity(PatronKindId, PatronSubtypeId, CultureId, 1, 0, 1, 0, 1, tick: 0);
        AddCulturePatch(simulation, CultureId, PlaneCodes.Physical, 0, 0, WorldState.CultureEntrenchedRawMass);
        var slot = RegisterGenerator(simulation);
        simulation.Generators.AddSecondaryBinding(
            slot,
            PatronRef,
            PatronKindId,
            adjacentToTarget: 0,
            cultureMatchCode: GeneratorCultureMatchCodes.SameAsTarget,
            samePhysicalCultureComponentAsTarget: 1);
        AddCreatedEntity(
            simulation,
            slot,
            physicalMinRadius: 0,
            physicalMaxRadius: 0,
            semanticMinRadius: 0,
            semanticMaxRadius: 0,
            placementRefCode: PatronRef,
            cultureRefCode: PatronRef,
            culturePlacementCode: GeneratorCulturePlacementCodes.CultureInterior);

        simulation.AdvanceTicks(1);

        var created = simulation.World.EntityHighWatermark;
        Assert.Equal(1, simulation.World.EntityPhysicalQ(created));
        Assert.Equal(0, simulation.World.EntityPhysicalR(created));
    }

    [Fact]
    public void CoveragePlacement_WithCultureInteriorPlacement_PrefersSupportedSeedCell()
    {
        var simulation = CreateSimulation();
        SeedMap(simulation, radius: 2);
        var target = simulation.CreateEntity(TargetKindId, TargetSubtypeId, CultureId, 0, 0, 0, 0, 0, tick: 0);
        simulation.World.SetCoverageOwner(0, 0, target);
        simulation.SetCellInfluence(target, PlaneCodes.Physical, 1, 0, WorldState.CultureEntrenchedRawMass, tick: 0);
        simulation.SetCellInfluence(target, PlaneCodes.Semantic, 1, 0, WorldState.CultureEntrenchedRawMass, tick: 0);
        var slot = RegisterGenerator(simulation);
        AddCreatedEntity(
            simulation,
            slot,
            physicalMinRadius: 1,
            physicalMaxRadius: 1,
            semanticMinRadius: 1,
            semanticMaxRadius: 1,
            culturePlacementCode: GeneratorCulturePlacementCodes.CultureInterior,
            coverageModeCode: GeneratorCoverageModeCodes.CreateNew,
            coverageDesiredCellCount: 1,
            coverageMinimumCellCount: 1);

        simulation.AdvanceTicks(1);

        var created = simulation.World.EntityHighWatermark;
        Assert.Equal(1, simulation.World.EntityPhysicalQ(created));
        Assert.Equal(0, simulation.World.EntityPhysicalR(created));
        Assert.Equal(created, simulation.World.CoverageOwnerAt(1, 0));
    }

    private static int RegisterGenerator(Simulation simulation)
    {
        var slot = simulation.Generators.RegisterGenerator(
            GeneratorId,
            TargetKindId,
            pulseWeight: 1,
            maxRuns: 1);
        simulation.Generators.AddTrigger(slot, TargetKindId);
        return slot;
    }

    private static int AddCreatedEntity(
        Simulation simulation,
        int generatorSlot,
        int physicalMinRadius,
        int physicalMaxRadius,
        int semanticMinRadius,
        int semanticMaxRadius,
        int placementRefCode = GeneratorProgramConstants.TargetReferenceCode,
        int cultureRefCode = GeneratorProgramConstants.TargetReferenceCode,
        int culturePlacementCode = GeneratorCulturePlacementCodes.PreferInheritedCulture,
        int coverageModeCode = GeneratorCoverageModeCodes.None,
        int coverageDesiredCellCount = 0,
        int coverageMinimumCellCount = 0) =>
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
            placementRefCode: placementRefCode,
            cultureRefCode: cultureRefCode,
            createChance: 1f,
            coverageModeCode: coverageModeCode,
            coverageSourceRefCode: 0,
            coverageDesiredCellCount: coverageDesiredCellCount,
            coverageMinimumCellCount: coverageMinimumCellCount,
            coverageMinimumSourceRemainingCellCount: 0,
            coverageRespectTerrainBoundaries: 0,
            coverageTransformTerrainId: 0,
            physicalPlacementMinRadius: physicalMinRadius,
            physicalPlacementMaxRadius: physicalMaxRadius,
            semanticPlacementMinRadius: semanticMinRadius,
            semanticPlacementMaxRadius: semanticMaxRadius,
            culturePlacementCode: culturePlacementCode);

    private static void AddCulturePatch(Simulation simulation, int cultureId, int planeCode, int q, int r, float rawMass)
    {
        simulation.ModifyCellCultureInfluenceRaw(cultureId, planeCode, q, r, rawMass, tick: 0);
        foreach (var (neighborQ, neighborR) in NeighborCells(q, r))
        {
            simulation.ModifyCellCultureInfluenceRaw(
                cultureId,
                planeCode,
                neighborQ,
                neighborR,
                WorldState.CultureControlledRawMass,
                tick: 0);
        }
    }

    private static void SeedMap(Simulation simulation, int radius)
    {
        simulation.RegisterTerrain(1, boundaryGroupId: 1);
        simulation.World.SetMapRadius(radius);
        for (var q = -radius; q <= radius; q++)
        {
            var minR = Math.Max(-radius, -q - radius);
            var maxR = Math.Min(radius, -q + radius);
            for (var r = minR; r <= maxR; r++)
            {
                simulation.CreateCell(q, r, terrainId: 1, ownerEntityId: 0);
                simulation.SetCellState(PlaneCodes.Semantic, q, r, terrainId: 1, ownerEntityId: 0, tick: 0);
            }
        }
    }

    private static (int Q, int R)[] NeighborCells(int q, int r) =>
    [
        (q + 1, r),
        (q + 1, r - 1),
        (q, r - 1),
        (q - 1, r),
        (q - 1, r + 1),
        (q, r + 1),
    ];

    private static Simulation CreateSimulation(int seed = 1) => KernelTestSimulation.Create(new SimulationOptions
    {
        EntityCapacity = 32,
        RelationshipCapacity = 32,
        AttributeCapacity = 32,
        MapCellCapacity = 256,
        EventCapacity = 512,
        GeneratorCapacity = 4,
        GeneratorItemCapacity = 64,
        RelationshipKindIdCapacity = 64,
        TerrainIdCapacity = 8,
        SystemCapacity = 16,
        Seed = seed,
    });
}
