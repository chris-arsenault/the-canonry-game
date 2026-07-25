namespace Engine.Kernel.Tests;

using Engine.Kernel;

public sealed class CultureInfluenceTargetScorerTests
{
    [Fact]
    public void Reinforce_StrongCurrentCenterOutscoresEqualWeakCenter()
    {
        var strong = Score(
            LowAltitudeInfluenceTargetingCodes.Reinforce,
            ownRaw: WorldState.CultureControlledRawMass,
            connectedRawMass: 20,
            adjacentSupportedCount: 4,
            strongCenterSupport: 1);
        var weak = Score(
            LowAltitudeInfluenceTargetingCodes.Reinforce,
            ownRaw: WorldState.CultureControlledRawMass,
            connectedRawMass: 20,
            adjacentSupportedCount: 4,
            strongCenterSupport: 0);

        Assert.True(strong > weak);
        var expectedRatio = CultureInfluenceTargetScorer.StrongCenterBoost(FixedScalar.One);
        var roundingErrorUnits = FixedScalar.Abs((strong / weak) - expectedRatio).Units;
        Assert.InRange(roundingErrorUnits, 0, 10);
    }

    [Fact]
    public void Spread_PrefersConnectedSupportedFrontierOverWeakIsland()
    {
        var supported = Score(
            LowAltitudeInfluenceTargetingCodes.Spread,
            ownRaw: 0,
            connectedRawMass: 20,
            adjacentSupportedCount: 3);
        var island = Score(
            LowAltitudeInfluenceTargetingCodes.Spread,
            ownRaw: 0,
            connectedRawMass: 1,
            adjacentSupportedCount: 1);

        Assert.True(supported > island * 20);
    }

    [Fact]
    public void Reinforce_RepairsLocallyEnclosedHole()
    {
        var enclosed = Score(
            LowAltitudeInfluenceTargetingCodes.Reinforce,
            ownRaw: 0,
            connectedRawMass: 24,
            adjacentSupportedCount: 6);
        var unsupported = Score(
            LowAltitudeInfluenceTargetingCodes.Reinforce,
            ownRaw: 0,
            connectedRawMass: 0,
            adjacentSupportedCount: 0);

        Assert.True(enclosed > FixedScalar.Zero);
        Assert.Equal(FixedScalar.Zero, unsupported);
    }

    [Fact]
    public void Contest_RequiresOwnLocalSupport()
    {
        var unsupported = Score(
            LowAltitudeInfluenceTargetingCodes.Contest,
            ownRaw: 0,
            connectedRawMass: 0,
            adjacentSupportedCount: 0,
            foreignShare: 100,
            foreignRaw: 100);
        var border = Score(
            LowAltitudeInfluenceTargetingCodes.Contest,
            ownRaw: 12,
            connectedRawMass: 18,
            adjacentSupportedCount: 3,
            foreignShare: 40,
            foreignRaw: 20);

        Assert.Equal(FixedScalar.Zero, unsupported);
        Assert.True(border > FixedScalar.Zero);
    }

    [Fact]
    public void CultureLocality_ProvidesRadiusTwoSupportToScoring()
    {
        var simulation = new Simulation(new SimulationOptions { Seed = 77 });
        simulation.World.SetMapRadius(4);
        var entityId = simulation.CreateEntity(
            kindId: 1,
            subtypeId: 1,
            cultureId: 1,
            0,
            0,
            0,
            0,
            0,
            tick: 0);
        using (simulation.EnterExecutionContext(simulation.StartupSourceCodeId, programId: 0))
        {
            simulation.SetCellInfluence(entityId, PlaneCodes.Physical, 0, 0, 30, tick: 0);
            simulation.SetCellInfluence(entityId, PlaneCodes.Physical, 1, 0, 30, tick: 0);
            simulation.SetCellInfluence(entityId, PlaneCodes.Physical, 1, -1, 30, tick: 0);
            simulation.SetCellInfluence(entityId, PlaneCodes.Physical, 0, -1, 30, tick: 0);
        }

        var locality = simulation.CultureLocality.CellAt(
            simulation.World,
            1,
            PlaneCodes.Physical,
            0,
            1);

        Assert.True(locality.SupportedAdjacentCount >= 2);
        Assert.True(locality.ConnectedRawMass > locality.RawMass);
    }

    private static FixedScalar Score(
        int targetingCode,
        FixedScalar ownRaw,
        FixedScalar connectedRawMass,
        int adjacentSupportedCount,
        FixedScalar strongCenterSupport = default,
        FixedScalar foreignShare = default,
        FixedScalar foreignRaw = default)
    {
        var input = new CultureInfluenceTargetScoreInput(
            targetingCode,
            OwnShare: ownRaw > FixedScalar.Zero ? FixedScalar.FromInt(100) : FixedScalar.Zero,
            ownRaw,
            foreignShare,
            foreignRaw,
            connectedRawMass,
            adjacentSupportedCount,
            strongCenterSupport);
        return CultureInfluenceTargetScorer.Score(input);
    }
}
