namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

/// <summary>
/// The full exploration chain in miniature: a fog-adjacent movement verb
/// targets the edge of the known world, committed travel walks there step
/// by step, and arrival materializes the latent ground. If any link breaks
/// - targeting, commitment, stepping, or the arrival reveal - the world
/// stops growing, so this pins the whole journey.
/// </summary>
public sealed class ExplorationJourneyTests
{
    private const int OpenTerrain = 1;
    private const int KindId = 1;
    private const int ScoutVerbId = 700;

    [Fact]
    public void ScoutVerb_WalksToTheFogAndArrivalRevealsNewGround()
    {
        var simulation = CreateSimulation();
        simulation.RegisterTerrain(
            OpenTerrain, boundaryGroupId: 1, movementCost: FixedScalar.FromSingle(3f),
            influenceMultiplier: FixedScalar.FromSingle(1f), claimCostMultiplier: FixedScalar.FromSingle(1f), claimChanceMultiplier: FixedScalar.FromSingle(1f),
            roadBuildMultiplier: FixedScalar.FromSingle(1f), roadSpeedMultiplier: FixedScalar.FromSingle(1f));
        InstallLatentDisk(simulation, latentRadius: 6, knownRadius: 2);
        simulation.World.SetMapRadius(8);
        var slot = simulation.LowAltitudeVerbs.RegisterVerb(
            kindId: KindId,
            verbId: ScoutVerbId,
            planeCode: PlaneCodes.Physical,
            operationCode: ProgramOpcodeCodes.MoveActor,
            targetModeCode: TargetSelectorCodes.Terrain,
            targetKindId: 0,
            relationshipKindId: 0,
            relationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Either,
            minCommitmentTicks: 60,
            maxCommitmentTicks: 120,
            dwellTicks: 4,
            targetRadius: 1,
            candidateSampleCount: 16,
            weight: FixedScalar.FromSingle(2f));
        simulation.LowAltitudeVerbs.SetGeographyFilter(slot, minTemperature: 0, siteFlags: 0, fogAdjacent: 1);
        simulation.LowAltitudeVerbs.AddPrimitiveStep(slot, ProgramOpcodeCodes.MoveActor, 0, FixedScalar.FromSingle(0f));
        var scout = simulation.CreateEntity(KindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);
        var knownAtStart = CountKnown(simulation, radius: 6);

        simulation.AdvanceTicks(400);

        var q = simulation.World.EntityPhysicalQ(scout);
        var r = simulation.World.EntityPhysicalR(scout);
        var knownAtEnd = CountKnown(simulation, radius: 6);
        Assert.True(
            knownAtEnd > knownAtStart,
            $"the scout should have opened new ground: {knownAtStart} known before, {knownAtEnd} after; scout ended at ({q},{r})");
    }

    private static void InstallLatentDisk(Simulation simulation, int latentRadius, int knownRadius)
    {
        simulation.KernelData.PhysicalGeography.BeginInstall(8);
        ForEachHex(latentRadius, (q, r) =>
        {
            simulation.KernelData.PhysicalGeography.InstallCell(
                q, r, OpenTerrain, superregionId: 1, continentId: 1,
                crossingWord: 0, fieldWord: 0, initialCoverTerrainId: 0,
                new byte[PhysicalHexFactCodes.Count]);
        });
        ForEachHex(knownRadius, (q, r) => simulation.SeedCell(q, r, OpenTerrain, ownerEntityId: 0));
    }

    private static int CountKnown(Simulation simulation, int radius)
    {
        var count = 0;
        ForEachHex(radius, (q, r) =>
        {
            if (simulation.World.CellIsKnown(PlaneCodes.Physical, q, r))
            {
                count++;
            }
        });
        return count;
    }

    private static void ForEachHex(int radius, Action<int, int> visit)
    {
        for (var q = -radius; q <= radius; q++)
        {
            var minR = Math.Max(-radius, -q - radius);
            var maxR = Math.Min(radius, -q + radius);
            for (var r = minR; r <= maxR; r++)
            {
                visit(q, r);
            }
        }
    }

    private static Simulation CreateSimulation() => KernelTestSimulation.Create(new SimulationOptions
    {
        EntityCapacity = 8,
        RelationshipKindIdCapacity = 16,
        TerrainIdCapacity = 8,
        EventCapacity = 8192,
        MapCellCapacity = 1024,
        MapGenerationRadiusCapacity = 8,
    });
}
