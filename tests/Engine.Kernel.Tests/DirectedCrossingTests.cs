namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

/// <summary>
/// Directional crossing surcharges: rivers, ridges, and cliffs make a step
/// between two cells cost more without ever blocking it, rivers are negated
/// by road quality, and travel reach pays the same factors walking does.
/// </summary>
public sealed class DirectedCrossingTests
{
    private const int OpenTerrain = 1;
    private const int KindId = 1;
    private const int DefaultProfile = 1;

    [Fact]
    public void TravelReach_PaysTheCrossingSurchargeOnTheCrossedEdge()
    {
        var flat = ReachAcrossLine(crossingClass: PhysicalCrossingCodes.Open, factor: 1f);
        var ridged = ReachAcrossLine(crossingClass: PhysicalCrossingCodes.Ridge, factor: 2f);

        // One of the two steps crosses the ridge edge: 3 + 6 instead of 3 + 3.
        Assert.Equal(6f, flat, precision: 2);
        Assert.Equal(9f, ridged, precision: 2);
    }

    [Fact]
    public void CrossingsSurchargeButNeverBlock()
    {
        var cliffed = ReachAcrossLine(crossingClass: PhysicalCrossingCodes.Cliff, factor: 4f);
        Assert.False(float.IsPositiveInfinity(cliffed));
        Assert.Equal(15f, cliffed, precision: 2);
    }

    [Fact]
    public void RoadQualityNegatesRiverCrossingsButNotRidges()
    {
        var riverBefore = 0f;
        var riverAfter = 0f;
        MeasureRoadEffect(PhysicalCrossingCodes.River, roadNegated: true, ref riverBefore, ref riverAfter);
        Assert.True(
            riverAfter < riverBefore,
            $"expected roads to erase the river surcharge: before {riverBefore}, after {riverAfter}");

        var ridgeBefore = 0f;
        var ridgeAfter = 0f;
        MeasureRoadEffect(PhysicalCrossingCodes.Ridge, roadNegated: false, ref ridgeBefore, ref ridgeAfter);

        // Roads discount the surface cost on both lines equally; the ridge
        // surcharge itself must survive, so the ridge line stays costlier
        // than its own surface discount alone would leave it.
        var ridgeSurchargeSurvives = ridgeAfter / ridgeBefore >= riverAfter / riverBefore;
        Assert.True(
            ridgeSurchargeSurvives,
            $"ridge {ridgeBefore}->{ridgeAfter}, river {riverBefore}->{riverAfter}");
    }

    [Fact]
    public void WanderAndNavigationRemainPossibleAcrossCrossings()
    {
        var simulation = CreateCrossingSimulation(PhysicalCrossingCodes.Cliff, factor: 4f, roadNegated: false);
        var actor = simulation.CreateEntity(KindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);

        var result = simulation.Movement.Navigate(simulation, actor, PlaneCodes.Physical, 1, 0, tick: 1);

        Assert.True(result.Moved);
        Assert.Equal(1, result.ToQ);
    }

    private static void MeasureRoadEffect(
        int crossingClass,
        bool roadNegated,
        ref float before,
        ref float after)
    {
        var simulation = CreateCrossingSimulation(crossingClass, factor: 3f, roadNegated);
        var actor = simulation.CreateEntity(KindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);
        var target = simulation.CreateEntity(KindId, 1, 1, 2, 0, 0, 0, 0, tick: 0);
        before = simulation.TravelReachCost(actor, target, PlaneCodes.Physical, tick: 1);
        for (var pass = 0; pass < 8; pass++)
        {
            _ = simulation.MoveEntityPhysical(actor, 1, 0, tick: 1, MovementCauseCodes.Travel);
            _ = simulation.MoveEntityPhysical(actor, 2, 0, tick: 1, MovementCauseCodes.Travel);
            _ = simulation.MoveEntityPhysical(actor, 0, 0, tick: 1, MovementCauseCodes.Travel);
        }

        after = simulation.TravelReachCost(actor, target, PlaneCodes.Physical, tick: 1);
    }

    private static float ReachAcrossLine(int crossingClass, float factor)
    {
        var simulation = CreateCrossingSimulation(crossingClass, factor, roadNegated: false);
        var actor = simulation.CreateEntity(KindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);
        var target = simulation.CreateEntity(KindId, 1, 1, 2, 0, 0, 0, 0, tick: 0);
        return simulation.TravelReachCost(actor, target, PlaneCodes.Physical, tick: 1);
    }

    // Three cells east in a line; the edge between (0,0) and (1,0) carries
    // the crossing class in both cells' words (reciprocity by construction).
    private static Simulation CreateCrossingSimulation(int crossingClass, float factor, bool roadNegated)
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions
        {
            EntityCapacity = 8,
            RelationshipKindIdCapacity = 16,
            TerrainIdCapacity = 8,
            EventCapacity = 4096,
            MapGenerationRadiusCapacity = 8,
        });
        simulation.RegisterTerrain(
            OpenTerrain, boundaryGroupId: 1, movementCost: 3f,
            influenceMultiplier: 1f, claimCostMultiplier: 1f, claimChanceMultiplier: 1f,
            roadBuildMultiplier: 20f, roadSpeedMultiplier: 2f);
        if (crossingClass != PhysicalCrossingCodes.Open)
        {
            simulation.RegisterCrossingProfile(crossingClass, DefaultProfile, factor, roadNegated);
        }

        simulation.KernelData.PhysicalGeography.BeginInstall(4);
        for (var q = 0; q <= 2; q++)
        {
            var word = 0;
            if (crossingClass != PhysicalCrossingCodes.Open)
            {
                if (q == 0)
                {
                    word = PhysicalCrossingCodes.WithClass(word, direction: 0, crossingClass);
                }

                if (q == 1)
                {
                    word = PhysicalCrossingCodes.WithClass(word, direction: 3, crossingClass);
                }
            }

            simulation.KernelData.PhysicalGeography.InstallCell(
                q, 0, OpenTerrain, superregionId: 1, continentId: 1,
                crossingWord: word, fieldWord: 0, initialCoverTerrainId: 0,
                new byte[PhysicalHexFactCodes.Count]);
        }

        simulation.World.SetMapRadius(4);
        for (var q = 0; q <= 2; q++)
        {
            simulation.SeedCell(q, 0, OpenTerrain, ownerEntityId: 0);
        }

        return simulation;
    }
}
