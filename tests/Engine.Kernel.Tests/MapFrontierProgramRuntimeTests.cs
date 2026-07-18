namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

public sealed class MapFrontierProgramRuntimeTests
{
    private const int TundraTerrain = 1;
    private const int DeepIceTerrain = 2;
    private const int MountainTerrain = 3;
    private const int ThawAgeId = 900;
    private const int ReclaimAgeId = 901;
    private const int SecondThawAgeId = 902;

    [Fact]
    public void MapFrontier_ThawsGroundWhereCultureMassPressesTheEdge()
    {
        var simulation = CreateSimulation();
        RegisterTerrains(simulation);
        InstallPhysicalBlueprint(simulation, generationRadius: 64, installedRadius: 1);
        simulation.World.SetMapRadius(6);
        simulation.SeedCell(0, 0, TundraTerrain, ownerEntityId: 0);
        OpenIceAge(simulation, ThawAgeId);
        ConfigureFrontier(simulation, ThawAgeId, ReclaimAgeId);

        // No mass at the edge: nothing thaws.
        simulation.MapFrontier.Tick(simulation, tick: 10);
        Assert.False(simulation.World.CellIsKnown(PlaneCodes.Physical, 1, 0));

        // Mass at the edge cell opens its unknown neighbours.
        _ = simulation.ModifyCellCultureInfluenceRaw(1, PlaneCodes.Physical, 0, 0, 40f, tick: 11);
        simulation.MapFrontier.Tick(simulation, tick: 12);

        Assert.True(simulation.World.CellIsKnown(PlaneCodes.Physical, 1, 0));
        Assert.Equal(MountainTerrain, simulation.World.CellTerrainId(PlaneCodes.Physical, 1, 0));
        Assert.Equal(12, simulation.World.CellSuperregionId(PlaneCodes.Physical, 1, 0));
        Assert.Equal(13, simulation.World.CellContinentId(PlaneCodes.Physical, 1, 0));
        Assert.True(simulation.MapFrontier.State.TotalThawed > 0);
    }

    [Fact]
    public void MapFrontier_DoesNotThawBeyondMaxRadius()
    {
        var simulation = CreateSimulation();
        RegisterTerrains(simulation);
        InstallPhysicalBlueprint(simulation, generationRadius: 64, installedRadius: 2);
        simulation.World.SetMapRadius(2);
        simulation.SeedCell(1, 0, TundraTerrain, ownerEntityId: 0);
        OpenIceAge(simulation, ThawAgeId);
        // Max radius 1: the cell at distance 1 may press inward but not past the cap.
        simulation.MapFrontier.SetAgeConfiguration(
            ThawAgeId, ReclaimAgeId, DeepIceTerrain,
            maxRadius: 1, frontierMassThreshold: 10f, reclaimMassCeiling: 1f,
            thawBudgetPerPulse: 24, refreezeBudgetPerPulse: 24);
        _ = simulation.ModifyCellCultureInfluenceRaw(1, PlaneCodes.Physical, 1, 0, 40f, tick: 11);

        simulation.MapFrontier.Tick(simulation, tick: 12);

        // The outward neighbour (2, 0) is beyond the max radius and stays closed.
        Assert.False(simulation.World.CellIsKnown(PlaneCodes.Physical, 2, 0));
    }

    [Fact]
    public void MapFrontier_HonorsAuthoredBudgetAboveTwentyFour()
    {
        var simulation = CreateSimulation(candidateCapacity: 64);
        RegisterTerrains(simulation);
        InstallPhysicalBlueprint(simulation, generationRadius: 64, installedRadius: 6);
        simulation.World.SetMapRadius(5);
        SeedPhysicalDisk(simulation, radius: 5);
        OpenIceAge(simulation, ThawAgeId);
        simulation.MapFrontier.SetAgeConfiguration(
            ThawAgeId, ReclaimAgeId, DeepIceTerrain,
            maxRadius: 64, frontierMassThreshold: 10f, reclaimMassCeiling: 1f,
            thawBudgetPerPulse: 48, refreezeBudgetPerPulse: 24);
        AddPhysicalRingInfluence(simulation, cultureId: 1, radius: 5, raw: 40f);

        simulation.MapFrontier.Tick(simulation, tick: 12);

        Assert.Equal(36, CountKnownPhysicalRing(simulation, radius: 6));
        Assert.True(simulation.MapFrontier.State.TotalThawed > 24);
    }

    [Fact]
    public void MapFrontier_SemanticBlueprintVariesTerrainAndAdvancesOnlySemanticExtent()
    {
        var simulation = CreateSimulation();
        RegisterTerrains(simulation);
        InstallSemanticBlueprint(simulation, generationRadius: 64, installedRadius: 1);
        simulation.World.SetMapRadius(0);
        simulation.SeedCell(PlaneCodes.Semantic, 0, 0, TundraTerrain, ownerEntityId: 0, superregionId: 21, continentId: 22);
        simulation.MapFrontier.SetSemanticConfiguration(
            enabled: true,
            maxRadius: 64,
            frontierMassThreshold: 10f,
            budgetPerPulse: 48);
        _ = simulation.ModifyCellCultureInfluenceRaw(1, PlaneCodes.Semantic, 0, 0, 40f, tick: 11);

        simulation.MapFrontier.Tick(simulation, tick: 12);

        Assert.Equal(0, simulation.World.PhysicalMapRadius);
        Assert.Equal(1, simulation.World.SemanticMapRadius);
        Assert.Contains(
            Enumerable.Range(0, 6),
            direction => simulation.World.CellTerrainId(
                PlaneCodes.Semantic,
                NeighborQ(direction),
                NeighborR(direction)) == MountainTerrain);
        Assert.Equal(31, simulation.World.CellSuperregionId(PlaneCodes.Semantic, 1, 0));
        Assert.Equal(32, simulation.World.CellContinentId(PlaneCodes.Semantic, 1, 0));
    }

    [Fact]
    public void MapFrontier_DoesNotReclaimStartupHeartCells()
    {
        var simulation = CreateSimulation();
        RegisterTerrains(simulation);
        simulation.World.SetMapRadius(10);
        simulation.SeedCell(7, 0, TundraTerrain, ownerEntityId: 0);
        OpenIceAge(simulation, ReclaimAgeId);
        ConfigureFrontier(simulation, ThawAgeId, ReclaimAgeId);
        _ = simulation.ModifyCellCultureInfluenceRaw(1, PlaneCodes.Physical, 7, 0, 0.5f, tick: 5);

        simulation.MapFrontier.Tick(simulation, tick: 20);

        Assert.Equal(TundraTerrain, simulation.World.CellTerrainId(PlaneCodes.Physical, 7, 0));
        Assert.Equal(0, simulation.World.CellTerrainCoverId(PlaneCodes.Physical, 7, 0));
        Assert.Equal(0, simulation.MapFrontier.State.TotalReclaimed);
    }

    [Fact]
    public void MapFrontier_PreservesDurableTerrainThroughCoverTransformAndThaw()
    {
        var simulation = CreateReclaimedFrontierSimulation();

        Assert.Equal(DeepIceTerrain, simulation.World.CellTerrainId(PlaneCodes.Physical, 1, 0));
        Assert.Equal(MountainTerrain, simulation.World.CellBaseTerrainId(PlaneCodes.Physical, 1, 0));
        Assert.Equal(DeepIceTerrain, simulation.World.CellTerrainCoverId(PlaneCodes.Physical, 1, 0));

        simulation.SetCellTerrain(PlaneCodes.Physical, 1, 0, TundraTerrain, tick: 6);

        Assert.Equal(DeepIceTerrain, simulation.World.CellTerrainId(PlaneCodes.Physical, 1, 0));
        Assert.Equal(TundraTerrain, simulation.World.CellBaseTerrainId(PlaneCodes.Physical, 1, 0));

        _ = simulation.ModifyCellCultureInfluenceRaw(1, PlaneCodes.Physical, 0, 0, 40f, tick: 7);
        simulation.MapFrontier.AddDriverAge(SecondThawAgeId, refreezeAgeId: 0);
        OpenIceAge(simulation, SecondThawAgeId, priority: 30, tick: 8);
        simulation.MapFrontier.Tick(simulation, tick: 9);

        Assert.Equal(TundraTerrain, simulation.World.CellTerrainId(PlaneCodes.Physical, 1, 0));
        Assert.Equal(TundraTerrain, simulation.World.CellBaseTerrainId(PlaneCodes.Physical, 1, 0));
        Assert.Equal(0, simulation.World.CellTerrainCoverId(PlaneCodes.Physical, 1, 0));
    }

    [Fact]
    public void MapFrontier_SnapshotPreservesFrontierHistoryAndTerrainCover()
    {
        var simulation = CreateReclaimedFrontierSimulation();
        var thawedCount = simulation.MapFrontier.State.ThawedCount;
        var reclaimCursor = simulation.MapFrontier.State.ReclaimCursor;
        using var snapshot = new MemoryStream();

        KernelSnapshot.SaveKernelState(simulation, snapshot);
        snapshot.Position = 0;
        var loaded = KernelSnapshot.LoadKernelState(snapshot);

        Assert.Equal(thawedCount, loaded.MapFrontier.State.ThawedCount);
        Assert.Equal(reclaimCursor, loaded.MapFrontier.State.ReclaimCursor);
        Assert.Equal(MountainTerrain, loaded.World.CellBaseTerrainId(PlaneCodes.Physical, 1, 0));
        Assert.Equal(DeepIceTerrain, loaded.World.CellTerrainCoverId(PlaneCodes.Physical, 1, 0));
        Assert.Equal(DeepIceTerrain, loaded.World.CellTerrainId(PlaneCodes.Physical, 1, 0));
    }

    [Fact]
    public void MapFrontier_DoesNotReclaimCellsHoldingCultureMass()
    {
        var simulation = CreateSimulation();
        RegisterTerrains(simulation);
        InstallPhysicalBlueprint(simulation, generationRadius: 64, installedRadius: 1);
        simulation.SeedCell(0, 0, TundraTerrain, ownerEntityId: 0);
        OpenIceAge(simulation, ThawAgeId);
        ConfigureFrontier(simulation, ThawAgeId, ReclaimAgeId);
        _ = simulation.ModifyCellCultureInfluenceRaw(1, PlaneCodes.Physical, 0, 0, 40f, tick: 2);
        simulation.MapFrontier.Tick(simulation, tick: 3);
        _ = simulation.ModifyCellCultureInfluenceRaw(1, PlaneCodes.Physical, 1, 0, 20f, tick: 4);
        OpenIceAge(simulation, ReclaimAgeId, priority: 20, tick: 5);

        simulation.MapFrontier.Tick(simulation, tick: 6);

        Assert.Equal(MountainTerrain, simulation.World.CellTerrainId(PlaneCodes.Physical, 1, 0));
        Assert.Equal(0, simulation.World.CellTerrainCoverId(PlaneCodes.Physical, 1, 0));
    }

    private static void RegisterTerrains(Simulation simulation)
    {
        simulation.RegisterTerrain(
            TundraTerrain, boundaryGroupId: 1, movementCost: 3f,
            influenceMultiplier: 1f, claimCostMultiplier: 1f, claimChanceMultiplier: 1f,
            roadBuildMultiplier: 4f, roadSpeedMultiplier: 2f);
        simulation.RegisterTerrain(
            DeepIceTerrain, boundaryGroupId: 1, movementCost: 9f,
            influenceMultiplier: 0.2f, claimCostMultiplier: 3f, claimChanceMultiplier: 0.05f,
            roadBuildMultiplier: 0f, roadSpeedMultiplier: 1f);
        simulation.RegisterTerrain(
            MountainTerrain, boundaryGroupId: 2, movementCost: 6f,
            influenceMultiplier: 0.8f, claimCostMultiplier: 1.5f, claimChanceMultiplier: 0.7f,
            roadBuildMultiplier: 6f, roadSpeedMultiplier: 2f);
    }

    private static void OpenIceAge(Simulation simulation, int ageId, int priority = 10, int tick = 1)
    {
        // An unconditional Ice-track age so OpenAgeId(Ice) equals ageId.
        _ = simulation.WorldBeats.AddAge(ageId, WorldBeatTrackCodes.World, priority, minOpenTicks: 0, cooldownTicks: 0);
        simulation.WorldBeats.Tick(simulation, tick);
    }

    private static Simulation CreateReclaimedFrontierSimulation()
    {
        var simulation = CreateSimulation();
        RegisterTerrains(simulation);
        InstallPhysicalBlueprint(simulation, generationRadius: 64, installedRadius: 1);
        simulation.SeedCell(0, 0, TundraTerrain, ownerEntityId: 0);
        OpenIceAge(simulation, ThawAgeId);
        ConfigureFrontier(simulation, ThawAgeId, ReclaimAgeId);
        _ = simulation.ModifyCellCultureInfluenceRaw(1, PlaneCodes.Physical, 0, 0, 40f, tick: 2);
        simulation.MapFrontier.Tick(simulation, tick: 3);
        _ = simulation.ModifyCellCultureInfluenceRaw(1, PlaneCodes.Physical, 0, 0, -40f, tick: 4);
        OpenIceAge(simulation, ReclaimAgeId, priority: 20, tick: 5);
        simulation.MapFrontier.Tick(simulation, tick: 6);
        return simulation;
    }

    private static void ConfigureFrontier(Simulation simulation, int thawAgeId, int reclaimAgeId) =>
        simulation.MapFrontier.SetAgeConfiguration(
            thawAgeId, reclaimAgeId, DeepIceTerrain,
            maxRadius: 40, frontierMassThreshold: 10f, reclaimMassCeiling: 1f,
            thawBudgetPerPulse: 24, refreezeBudgetPerPulse: 24);

    private static Simulation CreateSimulation(int candidateCapacity = 128) => KernelTestSimulation.Create(new SimulationOptions
    {
        EntityCapacity = 8,
        RelationshipCapacity = 8,
        RelationshipKindIdCapacity = 16,
        TerrainIdCapacity = 8,
        EventCapacity = 4096,
        MapGenerationRadiusCapacity = 64,
        MapFrontierCandidateCapacity = candidateCapacity,
    });

    private static void InstallPhysicalBlueprint(Simulation simulation, int generationRadius, int installedRadius)
    {
        simulation.KernelData.PhysicalGeography.BeginInstall(generationRadius);
        var materialIds = Enumerable.Repeat(TundraTerrain, PhysicalGeographyCodes.MicrotrianglesPerHex).ToArray();
        var landforms = Enumerable.Repeat(PhysicalLandformCodes.Plain, PhysicalGeographyCodes.MicrotrianglesPerHex).ToArray();
        var zeros = new int[PhysicalGeographyCodes.MicrotrianglesPerHex];
        var flowEdges = Enumerable.Repeat(PhysicalGeographyCodes.NoFlowEdge, PhysicalGeographyCodes.MicrotrianglesPerHex).ToArray();
        ForEachHex(installedRadius, (q, r) =>
        {
            var summary = new PhysicalGeographyCellSummary(
                q == 0 && r == 0 ? TundraTerrain : MountainTerrain,
                SuperregionId: 12,
                ContinentId: 13,
                DominantMaterialId: TundraTerrain,
                MeanElevation: 10,
                MinElevation: 0,
                MaxElevation: 20,
                Relief: 20,
                Roughness: 5,
                MeanTemperature: 200,
                MeanMoisture: 500,
                MeanGeothermal: 0,
                LandCoverage: PhysicalGeographyCodes.MicrotrianglesPerHex,
                WaterCoverage: 0,
                OceanCoverage: 0,
                LakeCoverage: 0,
                PermanentIceCoverage: 0,
                RiverCoverage: 0,
                PeakCount: 0,
                RiverEdgeMask: 0,
                CoastEdgeMask: 0,
                RidgeEdgeMask: 0,
                CliffEdgeMask: 0,
                PassEdgeMask: 0);
            simulation.KernelData.PhysicalGeography.InstallCell(
                q,
                r,
                summary,
                materialIds,
                landforms,
                zeros,
                zeros,
                zeros,
                zeros,
                zeros,
                zeros,
                zeros,
                zeros,
                flowEdges,
                zeros,
                zeros,
                zeros);
        });
    }

    private static void InstallSemanticBlueprint(Simulation simulation, int generationRadius, int installedRadius)
    {
        simulation.MapFrontier.Data.BeginSemanticBlueprintInstall(generationRadius);
        ForEachHex(installedRadius, (q, r) => simulation.MapFrontier.Data.InstallSemanticBlueprintCell(
            q,
            r,
            SemanticBlueprintTerrain(q, r),
            superregionId: 31,
            continentId: 32));
    }

    private static int SemanticBlueprintTerrain(int q, int r) =>
        (q != 0 || r != 0) && q > 0 ? MountainTerrain : TundraTerrain;

    private static void SeedPhysicalDisk(Simulation simulation, int radius) =>
        ForEachHex(radius, (q, r) => simulation.SeedCell(q, r, TundraTerrain, ownerEntityId: 0));

    private static void AddPhysicalRingInfluence(Simulation simulation, int cultureId, int radius, float raw) =>
        ForEachHex(radius, (q, r) =>
        {
            if (HexDistance(q, r) == radius)
            {
                _ = simulation.ModifyCellCultureInfluenceRaw(cultureId, PlaneCodes.Physical, q, r, raw, tick: 11);
            }
        });

    private static int CountKnownPhysicalRing(Simulation simulation, int radius)
    {
        var count = 0;
        ForEachHex(radius, (q, r) =>
        {
            if (HexDistance(q, r) == radius && simulation.World.CellIsKnown(PlaneCodes.Physical, q, r))
            {
                count++;
            }
        });
        return count;
    }

    private static void ForEachHex(int radius, Action<int, int> action)
    {
        for (var q = -radius; q <= radius; q++)
        {
            var minR = Math.Max(-radius, -q - radius);
            var maxR = Math.Min(radius, -q + radius);
            for (var r = minR; r <= maxR; r++)
            {
                action(q, r);
            }
        }
    }

    private static int HexDistance(int q, int r) =>
        (Math.Abs(q) + Math.Abs(r) + Math.Abs(q + r)) / 2;

    private static int NeighborQ(int direction) => direction switch
    {
        0 or 1 => 1,
        2 or 5 => 0,
        _ => -1,
    };

    private static int NeighborR(int direction) => direction switch
    {
        0 or 5 => 0,
        1 or 2 => -1,
        _ => 1,
    };
}
