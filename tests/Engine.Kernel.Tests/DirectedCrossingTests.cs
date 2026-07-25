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
        var flat = ReachAcrossLine(
            crossingClass: PhysicalCrossingCodes.Open,
            factor: FixedScalar.One);
        var ridged = ReachAcrossLine(
            crossingClass: PhysicalCrossingCodes.Ridge,
            factor: FixedScalar.FromInt(2));

        // One of the two steps crosses the ridge edge: 3 + 6 instead of 3 + 3.
        Assert.Equal(FixedScalar.FromInt(6), flat);
        Assert.Equal(FixedScalar.FromInt(9), ridged);
    }

    [Fact]
    public void CrossingsSurchargeButNeverBlock()
    {
        var cliffed = ReachAcrossLine(
            crossingClass: PhysicalCrossingCodes.Cliff,
            factor: FixedScalar.FromInt(4));
        Assert.False(FixedScalar.IsPositiveInfinity(cliffed));
        Assert.Equal(FixedScalar.FromInt(15), cliffed);
    }

    [Fact]
    public void RoadQualityNegatesRiverCrossingsButNotRidges()
    {
        var riverBefore = FixedScalar.Zero;
        var riverAfter = FixedScalar.Zero;
        MeasureRoadEffect(PhysicalCrossingCodes.River, roadNegated: true, ref riverBefore, ref riverAfter);
        Assert.True(
            riverAfter < riverBefore,
            $"expected roads to erase the river surcharge: before {riverBefore}, after {riverAfter}");

        var ridgeBefore = FixedScalar.Zero;
        var ridgeAfter = FixedScalar.Zero;
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
        var simulation = CreateCrossingSimulation(
            PhysicalCrossingCodes.Cliff,
            factor: FixedScalar.FromInt(4),
            roadNegated: false);
        var actor = simulation.CreateEntity(KindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);

        var result = simulation.Movement.Navigate(simulation, actor, PlaneCodes.Physical, 1, 0, tick: 1);

        Assert.True(result.Moved);
        Assert.Equal(1, result.ToQ);
    }

    private static void MeasureRoadEffect(
        int crossingClass,
        bool roadNegated,
        ref FixedScalar before,
        ref FixedScalar after)
    {
        var simulation = CreateCrossingSimulation(
            crossingClass,
            factor: FixedScalar.FromInt(3),
            roadNegated);
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

    private static FixedScalar ReachAcrossLine(int crossingClass, FixedScalar factor)
    {
        var simulation = CreateCrossingSimulation(crossingClass, factor, roadNegated: false);
        var actor = simulation.CreateEntity(KindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);
        var target = simulation.CreateEntity(KindId, 1, 1, 2, 0, 0, 0, 0, tick: 0);
        return simulation.TravelReachCost(actor, target, PlaneCodes.Physical, tick: 1);
    }

    // Three cells east in a line; the edge between (0,0) and (1,0) carries
    // the crossing class in both cells' words (reciprocity by construction).
    private static Simulation CreateCrossingSimulation(
        int crossingClass,
        FixedScalar factor,
        bool roadNegated)
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
            OpenTerrain, boundaryGroupId: 1, movementCost: FixedScalar.FromSingle(3f),
            influenceMultiplier: FixedScalar.FromSingle(1f), claimCostMultiplier: FixedScalar.FromSingle(1f), claimChanceMultiplier: FixedScalar.FromSingle(1f),
            roadBuildMultiplier: FixedScalar.FromSingle(20f), roadSpeedMultiplier: FixedScalar.FromSingle(2f));
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
