namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

/// <summary>
/// Geography pulls generator placement: a preferred site flag or a band
/// affinity outweighs otherwise-identical candidates, with zero effect on
/// producers that author neither.
/// </summary>
public sealed class GeneratorPlacementGeographyTests
{
    private const int OpenTerrain = 1;
    private const int WaterTerrain = 2;
    private const int GeneratorId = 900;
    private const int TargetKindId = 1;
    private const int CreatedKindId = 2;
    private const int CreatedSubtypeId = 1;
    private const int CreatedRef = 1;

    [Fact]
    public void PreferredSiteFlag_WinsOtherwiseEqualPlacement()
    {
        var simulation = CreateSimulation();
        InstallGeography(simulation, flaggedQ: 1, flaggedR: 0, fieldWord: PhysicalHexFieldCodes.SummitFlag, auroraMean: 0);
        var slot = RegisterGeneratorWithGeography(
            simulation,
            siteFlags: PhysicalHexFieldCodes.SummitFlag,
            auroraAffinity: 0f);
        _ = slot;
        _ = simulation.CreateEntity(TargetKindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);

        simulation.AdvanceTicks(1);

        var createdId = FindCreated(simulation);
        Assert.Equal(1, simulation.World.EntityPhysicalQ(createdId));
        Assert.Equal(0, simulation.World.EntityPhysicalR(createdId));
    }

    [Fact]
    public void BandAffinity_PullsPlacementTowardTheBand()
    {
        var simulation = CreateSimulation();
        InstallGeography(
            simulation,
            flaggedQ: 0,
            flaggedR: 1,
            fieldWord: 0,
            auroraMean: 255);
        _ = RegisterGeneratorWithGeography(simulation, siteFlags: 0, auroraAffinity: 1f);
        _ = simulation.CreateEntity(TargetKindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);

        simulation.AdvanceTicks(1);

        var createdId = FindCreated(simulation);
        Assert.Equal(0, simulation.World.EntityPhysicalQ(createdId));
        Assert.Equal(1, simulation.World.EntityPhysicalR(createdId));
    }

    [Fact]
    public void AvoidedTerrain_IsAHardWall_PlacementWidensPastAnAllWaterRing()
    {
        // The anchor stands on land, its entire radius-1 ring is water, and
        // land resumes at radius 2. The produced entity avoids water, so
        // placement must skip the whole ring and land at distance 2 - the
        // partition is never outscored by proximity.
        var simulation = CreateSimulation();
        InstallPartitionedGeography(simulation);
        var producedSlot = RegisterGeneratorProduced(simulation);
        simulation.Generators.AddProducedPlacementAvoidedTerrain(
            producedSlot, PlaneCodes.Physical, WaterTerrain);
        _ = simulation.CreateEntity(TargetKindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);

        simulation.AdvanceTicks(1);

        var createdId = FindCreated(simulation);
        var q = simulation.World.EntityPhysicalQ(createdId);
        var r = simulation.World.EntityPhysicalR(createdId);
        var distance = (global::System.Math.Abs(q) + global::System.Math.Abs(r) + global::System.Math.Abs(q + r)) / 2;
        Assert.Equal(2, distance);
        Assert.Equal(OpenTerrain, simulation.World.CellTerrainId(PlaneCodes.Physical, q, r));
    }

    private static void InstallPartitionedGeography(Simulation simulation)
    {
        simulation.RegisterTerrain(
            OpenTerrain, boundaryGroupId: 1, movementCost: 3f,
            influenceMultiplier: 1f, claimCostMultiplier: 1f, claimChanceMultiplier: 1f,
            roadBuildMultiplier: 1f, roadSpeedMultiplier: 1f);
        simulation.RegisterTerrain(
            WaterTerrain, boundaryGroupId: 2, movementCost: 4f,
            influenceMultiplier: 1f, claimCostMultiplier: 1f, claimChanceMultiplier: 1f,
            roadBuildMultiplier: 0f, roadSpeedMultiplier: 1f);
        simulation.KernelData.PhysicalGeography.BeginInstall(4);
        simulation.World.SetMapRadius(4);
        ForEachHex(2, (q, r) =>
        {
            var distance = (global::System.Math.Abs(q) + global::System.Math.Abs(r) + global::System.Math.Abs(q + r)) / 2;
            var terrainId = distance == 1 ? WaterTerrain : OpenTerrain;
            var facts = new byte[PhysicalHexFactCodes.Count];
            simulation.KernelData.PhysicalGeography.InstallCell(
                q, r, terrainId, superregionId: 1, continentId: 1,
                crossingWord: 0, fieldWord: 0, initialCoverTerrainId: 0, facts);
            simulation.SeedCell(q, r, terrainId, ownerEntityId: 0);
            if (!simulation.World.CellIsKnown(PlaneCodes.Semantic, q, r))
            {
                simulation.CreateCell(PlaneCodes.Semantic, q, r, OpenTerrain, ownerEntityId: 0);
            }
        });
    }

    private static int RegisterGeneratorProduced(Simulation simulation)
    {
        var slot = simulation.Generators.RegisterGenerator(
            GeneratorId,
            TargetKindId,
            pulseWeight: 1,
            maxRuns: 0);
        simulation.Generators.AddTrigger(slot, TargetKindId);
        return simulation.Generators.AddProduced(
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
            cultureRefCode: GeneratorProgramConstants.TargetReferenceCode,
            createChance: 1f,
            coverageModeCode: GeneratorCoverageModeCodes.None,
            coverageSourceRefCode: 0,
            coverageDesiredCellCount: 0,
            coverageMinimumCellCount: 0,
            coverageMinimumSourceRemainingCellCount: 0,
            coverageRespectTerrainBoundaries: 0,
            coverageTransformTerrainId: 0);
    }

    private static int FindCreated(Simulation simulation)
    {
        for (var entityId = 1; entityId <= simulation.World.EntityHighWatermark; entityId++)
        {
            if (simulation.World.EntityKindId(entityId) == CreatedKindId)
            {
                return entityId;
            }
        }

        throw new global::System.InvalidOperationException();
    }

    private static int RegisterGeneratorWithGeography(
        Simulation simulation,
        int siteFlags,
        float auroraAffinity)
    {
        var slot = simulation.Generators.RegisterGenerator(
            GeneratorId,
            TargetKindId,
            pulseWeight: 1,
            maxRuns: 0);
        simulation.Generators.AddTrigger(slot, TargetKindId);
        var producedSlot = simulation.Generators.AddProduced(
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
            cultureRefCode: GeneratorProgramConstants.TargetReferenceCode,
            createChance: 1f,
            coverageModeCode: GeneratorCoverageModeCodes.None,
            coverageSourceRefCode: 0,
            coverageDesiredCellCount: 0,
            coverageMinimumCellCount: 0,
            coverageMinimumSourceRemainingCellCount: 0,
            coverageRespectTerrainBoundaries: 0,
            coverageTransformTerrainId: 0);
        simulation.Generators.SetProducedPlacementGeography(
            producedSlot,
            siteFlags,
            warmthAffinity: 0f,
            fireAffinity: 0f,
            auroraAffinity: auroraAffinity,
            pressureAffinity: 0f);
        return slot;
    }

    private static void InstallGeography(
        Simulation simulation,
        int flaggedQ,
        int flaggedR,
        int fieldWord,
        byte auroraMean)
    {
        simulation.RegisterTerrain(
            OpenTerrain, boundaryGroupId: 1, movementCost: 3f,
            influenceMultiplier: 1f, claimCostMultiplier: 1f, claimChanceMultiplier: 1f,
            roadBuildMultiplier: 1f, roadSpeedMultiplier: 1f);
        simulation.KernelData.PhysicalGeography.BeginInstall(4);
        simulation.World.SetMapRadius(4);
        ForEachHex(1, (q, r) =>
        {
            var flagged = q == flaggedQ && r == flaggedR;
            var facts = new byte[PhysicalHexFactCodes.Count];
            if (flagged)
            {
                facts[PhysicalHexFactCodes.AuroraMean] = auroraMean;
            }

            simulation.KernelData.PhysicalGeography.InstallCell(
                q, r, OpenTerrain, superregionId: 1, continentId: 1,
                crossingWord: 0,
                fieldWord: flagged ? fieldWord : 0,
                initialCoverTerrainId: 0,
                facts);
            simulation.SeedCell(q, r, OpenTerrain, ownerEntityId: 0);
        });
        ForEachHex(2, (q, r) =>
        {
            if (!simulation.World.CellIsKnown(PlaneCodes.Semantic, q, r))
            {
                simulation.CreateCell(PlaneCodes.Semantic, q, r, OpenTerrain, ownerEntityId: 0);
            }
        });
    }

    private static void ForEachHex(int radius, global::System.Action<int, int> visit)
    {
        for (var q = -radius; q <= radius; q++)
        {
            for (var r = -radius; r <= radius; r++)
            {
                if ((global::System.Math.Abs(q) + global::System.Math.Abs(r) + global::System.Math.Abs(q + r)) / 2 <= radius)
                {
                    visit(q, r);
                }
            }
        }
    }

    private static Simulation CreateSimulation() => KernelTestSimulation.Create(new SimulationOptions
    {
        EntityCapacity = 16,
        RelationshipCapacity = 16,
        AttributeCapacity = 32,
        MapCellCapacity = 1024,
        EventCapacity = 512,
        GeneratorCapacity = 4,
        GeneratorItemCapacity = 64,
        RelationshipKindIdCapacity = 64,
        TerrainIdCapacity = 8,
        SystemCapacity = 24,
        MapGenerationRadiusCapacity = 8,
    });
}
