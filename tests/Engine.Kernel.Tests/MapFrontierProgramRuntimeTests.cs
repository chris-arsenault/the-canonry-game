namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

/// <summary>
/// The three decoupled frontier layers: reveal (entity presence opens latent
/// ground, one-way), the ice (cold pressure vs mortal resistance, breathing
/// with the ages, never un-revealing), and semantic growth (untouched by
/// either). Fog of war moves at entity speed, not culture speed.
/// </summary>
public sealed class MapFrontierProgramRuntimeTests
{
    private const int TundraTerrain = 1;
    private const int DeepIceTerrain = 2;
    private const int MountainTerrain = 3;
    private const int SeaIceTerrain = 4;
    private const int KindId = 1;
    private const int RangingKindId = 2;
    private const int ThawAgeId = 900;
    private const int ReclaimAgeId = 901;

    [Fact]
    public void Reveal_OpensLatentGroundAroundAnEntity_WithoutAnyCultureMass()
    {
        var simulation = CreateSimulation();
        RegisterTerrains(simulation);
        InstallPhysicalBlueprint(simulation, generationRadius: 64, installedRadius: 2);
        simulation.World.SetMapRadius(6);
        simulation.SeedCell(0, 0, TundraTerrain, ownerEntityId: 0);
        ConfigureFrontier(simulation, ThawAgeId, ReclaimAgeId);
        _ = simulation.CreateEntity(KindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);

        // No age open, no culture mass anywhere: presence alone reveals.
        simulation.MapFrontier.Tick(simulation, tick: 10);

        Assert.True(simulation.World.CellIsKnown(PlaneCodes.Physical, 1, 0));
        Assert.Equal(MountainTerrain, simulation.World.CellTerrainId(PlaneCodes.Physical, 1, 0));
        Assert.Equal(12, simulation.World.CellSuperregionId(PlaneCodes.Physical, 1, 0));
        Assert.Equal(13, simulation.World.CellContinentId(PlaneCodes.Physical, 1, 0));
        Assert.True(simulation.MapFrontier.State.TotalThawed > 0);
    }

    [Fact]
    public void Reveal_HonorsPerKindRadius_RangingKindsSeeFarther()
    {
        var simulation = CreateSimulation();
        RegisterTerrains(simulation);
        InstallPhysicalBlueprint(simulation, generationRadius: 64, installedRadius: 3);
        simulation.World.SetMapRadius(6);
        simulation.SeedCell(0, 0, TundraTerrain, ownerEntityId: 0);
        simulation.MapFrontier.SetAgeConfiguration(
            ThawAgeId, ReclaimAgeId, DeepIceTerrain,
            maxRadius: 40,
            iceAdvanceBudgetPerPulse: 24, iceRetreatBudgetPerPulse: 24);
        simulation.MapFrontier.RegisterKindRevealRadius(RangingKindId, 3);
        _ = simulation.CreateEntity(RangingKindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);

        simulation.MapFrontier.Tick(simulation, tick: 10);

        Assert.True(simulation.World.CellIsKnown(PlaneCodes.Physical, 3, 0));
    }

    [Fact]
    public void Reveal_StopsAtMaxRadius()
    {
        var simulation = CreateSimulation();
        RegisterTerrains(simulation);
        InstallPhysicalBlueprint(simulation, generationRadius: 64, installedRadius: 3);
        simulation.World.SetMapRadius(3);
        simulation.SeedCell(1, 0, TundraTerrain, ownerEntityId: 0);
        simulation.MapFrontier.SetAgeConfiguration(
            ThawAgeId, ReclaimAgeId, DeepIceTerrain,
            maxRadius: 1,
            iceAdvanceBudgetPerPulse: 24, iceRetreatBudgetPerPulse: 24);
        _ = simulation.CreateEntity(KindId, 1, 1, 1, 0, 0, 0, 0, tick: 0);

        simulation.MapFrontier.Tick(simulation, tick: 10);

        Assert.False(simulation.World.CellIsKnown(PlaneCodes.Physical, 2, 0));
        Assert.True(simulation.World.CellIsKnown(PlaneCodes.Physical, 0, 0));
    }

    [Fact]
    public void Reveal_FiresOnArrival_NotJustOnThePulse()
    {
        var simulation = CreateSimulation();
        RegisterTerrains(simulation);
        InstallPhysicalBlueprint(simulation, generationRadius: 64, installedRadius: 2);
        simulation.World.SetMapRadius(6);
        simulation.SeedCell(0, 0, TundraTerrain, ownerEntityId: 0);
        ConfigureFrontier(simulation, ThawAgeId, ReclaimAgeId);
        var walker = simulation.CreateEntity(KindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);

        // No frontier pulse runs; arrival alone opens the surroundings.
        simulation.MapFrontier.RevealOnArrival(simulation, walker, tick: 1);

        Assert.True(simulation.World.CellIsKnown(PlaneCodes.Physical, 1, 0));
        Assert.True(simulation.World.CellIsKnown(PlaneCodes.Physical, -1, 1));
    }

    [Fact]
    public void Ice_AdvancesOverColdUnheldFrontierGround_DuringFreezeAges()
    {
        var simulation = CreateRevealedFrontierSimulation();
        OpenIceAge(simulation, ReclaimAgeId, priority: 20, tick: 5);

        simulation.MapFrontier.Tick(simulation, tick: 6);

        // Frontier-grown cold ground with no culture, no roads, no
        // occupants freezes over; its durable terrain survives underneath.
        Assert.Equal(DeepIceTerrain, simulation.World.CellTerrainCoverId(PlaneCodes.Physical, 2, 0));
        Assert.Equal(MountainTerrain, simulation.World.CellBaseTerrainId(PlaneCodes.Physical, 2, 0));
        Assert.True(simulation.MapFrontier.State.TotalReclaimed > 0);
    }

    [Fact]
    public void Ice_NeverCoversTheStartupHeart()
    {
        var simulation = CreateRevealedFrontierSimulation();
        OpenIceAge(simulation, ReclaimAgeId, priority: 20, tick: 5);

        for (var tick = 6; tick < 12; tick++)
        {
            simulation.MapFrontier.Tick(simulation, tick);
        }

        Assert.Equal(0, simulation.World.CellTerrainCoverId(PlaneCodes.Physical, 0, 0));
    }

    [Fact]
    public void Ice_SparesGroundACultureHolds()
    {
        var simulation = CreateRevealedFrontierSimulation();
        // Enough mass that resistance (8 + 4 x mass) beats the cold's
        // pressure at temperature zero (64 x scalar 1).
        _ = simulation.ModifyCellCultureInfluenceRaw(1, PlaneCodes.Physical, 2, 0, FixedScalar.FromSingle(20f), tick: 4);
        OpenIceAge(simulation, ReclaimAgeId, priority: 20, tick: 5);

        simulation.MapFrontier.Tick(simulation, tick: 6);

        Assert.Equal(0, simulation.World.CellTerrainCoverId(PlaneCodes.Physical, 2, 0));
    }

    [Fact]
    public void Ice_RetreatsInThawAges_AndTheCellStaysKnownThroughout()
    {
        var simulation = CreateRevealedFrontierSimulation();
        OpenIceAge(simulation, ReclaimAgeId, priority: 20, tick: 5);
        simulation.MapFrontier.Tick(simulation, tick: 6);
        Assert.Equal(DeepIceTerrain, simulation.World.CellTerrainCoverId(PlaneCodes.Physical, 2, 0));

        // The covered cell is still known ground: the ice closes, it does
        // not erase. Culture mass then melts it out in the thaw.
        Assert.True(simulation.World.CellIsKnown(PlaneCodes.Physical, 2, 0));
        _ = simulation.ModifyCellCultureInfluenceRaw(1, PlaneCodes.Physical, 2, 0, FixedScalar.FromSingle(20f), tick: 7);
        OpenIceAge(simulation, ThawAgeId, priority: 30, tick: 8);
        for (var tick = 9; tick < 13 &&
            simulation.World.CellTerrainCoverId(PlaneCodes.Physical, 2, 0) != 0; tick++)
        {
            simulation.MapFrontier.Tick(simulation, tick);
        }

        Assert.Equal(0, simulation.World.CellTerrainCoverId(PlaneCodes.Physical, 2, 0));
        Assert.Equal(MountainTerrain, simulation.World.CellTerrainId(PlaneCodes.Physical, 2, 0));
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
            frontierMassThreshold: FixedScalar.FromSingle(10f),
            budgetPerPulse: 48);
        _ = simulation.ModifyCellCultureInfluenceRaw(1, PlaneCodes.Semantic, 0, 0, FixedScalar.FromSingle(40f), tick: 11);

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
    public void MapFrontier_SnapshotPreservesFrontierHistoryAndTerrainCover()
    {
        var simulation = CreateRevealedFrontierSimulation();
        OpenIceAge(simulation, ReclaimAgeId, priority: 20, tick: 5);
        simulation.MapFrontier.Tick(simulation, tick: 6);
        var thawedCount = simulation.MapFrontier.State.ThawedCount;
        using var snapshot = new MemoryStream();

        KernelSnapshot.SaveKernelState(simulation, snapshot);
        snapshot.Position = 0;
        var loaded = KernelSnapshot.LoadKernelState(snapshot);

        Assert.Equal(thawedCount, loaded.MapFrontier.State.ThawedCount);
        Assert.Equal(MountainTerrain, loaded.World.CellBaseTerrainId(PlaneCodes.Physical, 2, 0));
        Assert.Equal(DeepIceTerrain, loaded.World.CellTerrainCoverId(PlaneCodes.Physical, 2, 0));
    }

    [Fact]
    public void Reveal_AppliesInitialSeaIceCoverAndLeavesItAlone()
    {
        var simulation = CreateSimulation();
        RegisterTerrains(simulation);
        InstallPhysicalBlueprintWithCover(simulation, coverQ: 1, coverR: 0);
        simulation.World.SetMapRadius(6);
        simulation.SeedCell(0, 0, TundraTerrain, ownerEntityId: 0);
        ConfigureFrontier(simulation, ThawAgeId, ReclaimAgeId);
        _ = simulation.CreateEntity(KindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);

        simulation.MapFrontier.Tick(simulation, tick: 12);

        // Reveal materializes the base terrain and applies the declared
        // initial cover, so the cell plays as frozen sea from its first
        // tick, and later reveal pulses leave the known cell alone.
        Assert.True(simulation.World.CellIsKnown(PlaneCodes.Physical, 1, 0));
        Assert.Equal(SeaIceTerrain, simulation.World.CellTerrainCoverId(PlaneCodes.Physical, 1, 0));
        simulation.MapFrontier.Tick(simulation, tick: 14);
        Assert.Equal(SeaIceTerrain, simulation.World.CellTerrainCoverId(PlaneCodes.Physical, 1, 0));
    }

    [Fact]
    public void SeedCell_AppliesInitialCoverFromLatentGeography()
    {
        var simulation = CreateSimulation();
        RegisterTerrains(simulation);
        InstallPhysicalBlueprintWithCover(simulation, coverQ: 0, coverR: 0);
        simulation.World.SetMapRadius(6);

        simulation.SeedCell(0, 0, MountainTerrain, ownerEntityId: 0);

        Assert.Equal(SeaIceTerrain, simulation.World.CellTerrainCoverId(PlaneCodes.Physical, 0, 0));
        Assert.Equal(SeaIceTerrain, simulation.World.CellTerrainId(PlaneCodes.Physical, 0, 0));
    }

    [Fact]
    public void SeaIce_FreezesFromShoreInFreezeAgesAndMeltsInThawAges()
    {
        var simulation = CreateSimulation();
        RegisterTerrains(simulation);
        InstallPhysicalBlueprint(simulation, generationRadius: 64, installedRadius: 2);
        simulation.World.SetMapRadius(6);
        // A shore at the origin and open water east of it. Water uses the
        // mountain terrain id registered as sea-ice eligible below.
        simulation.SeedCell(0, 0, TundraTerrain, ownerEntityId: 0);
        simulation.SeedCell(1, 0, MountainTerrain, ownerEntityId: 0);
        simulation.SeedCell(2, 0, MountainTerrain, ownerEntityId: 0);
        ConfigureFrontier(simulation, ThawAgeId, ReclaimAgeId);
        simulation.MapFrontier.SetSeaIceConfiguration(
            SeaIceTerrain, freezeBudgetPerPulse: 1, meltBudgetPerPulse: 8);
        simulation.MapFrontier.AddSeaIceEligibleTerrain(MountainTerrain);

        // A freeze age: the shore-adjacent water freezes first.
        OpenIceAge(simulation, ReclaimAgeId, priority: 20, tick: 1);
        simulation.MapFrontier.Tick(simulation, tick: 2);
        Assert.Equal(SeaIceTerrain, simulation.World.CellTerrainCoverId(PlaneCodes.Physical, 1, 0));

        // The next freeze pulse spreads the cover along the frozen edge.
        simulation.MapFrontier.Tick(simulation, tick: 3);
        Assert.Equal(SeaIceTerrain, simulation.World.CellTerrainCoverId(PlaneCodes.Physical, 2, 0));

        // A thaw age melts the cover back and the water reopens.
        OpenIceAge(simulation, ThawAgeId, priority: 30, tick: 4);
        simulation.MapFrontier.Tick(simulation, tick: 5);
        simulation.MapFrontier.Tick(simulation, tick: 6);
        Assert.Equal(0, simulation.World.CellTerrainCoverId(PlaneCodes.Physical, 1, 0));
        Assert.Equal(0, simulation.World.CellTerrainCoverId(PlaneCodes.Physical, 2, 0));
    }

    // A heart at the origin, an entity that walked the frontier open to
    // distance 2, and cold (temperature-zero) facts everywhere: the revealed
    // rim is exactly the ground the ice may later claim.
    private static Simulation CreateRevealedFrontierSimulation()
    {
        var simulation = CreateSimulation();
        RegisterTerrains(simulation);
        InstallPhysicalBlueprint(simulation, generationRadius: 64, installedRadius: 3);
        simulation.World.SetMapRadius(6);
        simulation.SeedCell(0, 0, TundraTerrain, ownerEntityId: 0);
        ConfigureFrontier(simulation, ThawAgeId, ReclaimAgeId);
        simulation.MapFrontier.RegisterKindRevealRadius(KindId, 2);
        _ = simulation.CreateEntity(KindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.MapFrontier.Tick(simulation, tick: 3);
        return simulation;
    }

    private static void RegisterTerrains(Simulation simulation)
    {
        simulation.RegisterTerrain(
            TundraTerrain, boundaryGroupId: 1, movementCost: FixedScalar.FromSingle(3f),
            influenceMultiplier: FixedScalar.FromSingle(1f), claimCostMultiplier: FixedScalar.FromSingle(1f), claimChanceMultiplier: FixedScalar.FromSingle(1f),
            roadBuildMultiplier: FixedScalar.FromSingle(4f), roadSpeedMultiplier: FixedScalar.FromSingle(2f));
        simulation.RegisterTerrain(
            DeepIceTerrain, boundaryGroupId: 1, movementCost: FixedScalar.FromSingle(9f),
            influenceMultiplier: FixedScalar.FromSingle(0.2f), claimCostMultiplier: FixedScalar.FromSingle(3f), claimChanceMultiplier: FixedScalar.FromSingle(0.05f),
            roadBuildMultiplier: FixedScalar.FromSingle(0f), roadSpeedMultiplier: FixedScalar.FromSingle(1f));
        simulation.RegisterTerrain(
            MountainTerrain, boundaryGroupId: 2, movementCost: FixedScalar.FromSingle(6f),
            influenceMultiplier: FixedScalar.FromSingle(0.8f), claimCostMultiplier: FixedScalar.FromSingle(1.5f), claimChanceMultiplier: FixedScalar.FromSingle(0.7f),
            roadBuildMultiplier: FixedScalar.FromSingle(6f), roadSpeedMultiplier: FixedScalar.FromSingle(2f));
        simulation.RegisterTerrain(
            SeaIceTerrain, boundaryGroupId: 1, movementCost: FixedScalar.FromSingle(5.5f),
            influenceMultiplier: FixedScalar.FromSingle(0.6f), claimCostMultiplier: FixedScalar.FromSingle(1.5f), claimChanceMultiplier: FixedScalar.FromSingle(0.6f),
            roadBuildMultiplier: FixedScalar.FromSingle(0.2f), roadSpeedMultiplier: FixedScalar.FromSingle(1f));
    }

    private static void OpenIceAge(Simulation simulation, int ageId, int priority = 10, int tick = 1)
    {
        // An unconditional Ice-track age so OpenAgeId(Ice) equals ageId.
        _ = simulation.WorldBeats.AddAge(ageId, WorldBeatTrackCodes.World, priority, minOpenTicks: 0, cooldownTicks: 0);
        simulation.WorldBeats.Tick(simulation, tick);
    }

    private static void ConfigureFrontier(Simulation simulation, int thawAgeId, int reclaimAgeId) =>
        simulation.MapFrontier.SetAgeConfiguration(
            thawAgeId, reclaimAgeId, DeepIceTerrain,
            maxRadius: 40,
            iceAdvanceBudgetPerPulse: 24, iceRetreatBudgetPerPulse: 24);

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
        ForEachHex(installedRadius, (q, r) =>
        {
            simulation.KernelData.PhysicalGeography.InstallCell(
                q,
                r,
                q == 0 && r == 0 ? TundraTerrain : MountainTerrain,
                superregionId: 12,
                continentId: 13,
                crossingWord: 0,
                fieldWord: 0,
                initialCoverTerrainId: 0,
                new byte[PhysicalHexFactCodes.Count]);
        });
    }

    private static void InstallPhysicalBlueprintWithCover(Simulation simulation, int coverQ, int coverR)
    {
        simulation.KernelData.PhysicalGeography.BeginInstall(64);
        ForEachHex(1, (q, r) =>
        {
            var covered = q == coverQ && r == coverR;
            simulation.KernelData.PhysicalGeography.InstallCell(
                q,
                r,
                covered || !(q == 0 && r == 0) ? MountainTerrain : TundraTerrain,
                superregionId: 12,
                continentId: 13,
                crossingWord: 0,
                fieldWord: 0,
                initialCoverTerrainId: covered ? SeaIceTerrain : 0,
                new byte[PhysicalHexFactCodes.Count]);
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
