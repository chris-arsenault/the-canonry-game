namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

public sealed class KnowledgeProgramRuntimeTests
{
    private const int CultureId = 1;

    [Fact]
    public void ClaimsAggregateGlobalAndTargetedGrantsAndLossRemovesOnlyLostSite()
    {
        var simulation = CreateSimulation();
        var foundation = simulation.RegisterKnowledgeSite(0, 0, tier: 0);
        simulation.RegisterKnowledgeGrant(
            foundation,
            KnowledgeGrantDomainCodes.VerbWeight,
            target: 0,
            multiplier: 1.10f);
        var advanced = simulation.RegisterKnowledgeSite(6, 0, tier: 4);
        simulation.RegisterKnowledgeGrant(
            advanced,
            KnowledgeGrantDomainCodes.VerbWeight,
            target: 17,
            multiplier: 1.20f);

        Claim(simulation, 0, 0);
        Claim(simulation, 6, 0);
        simulation.Knowledge.Tick(simulation, tick: 1);

        Assert.Equal(2, simulation.Knowledge.ClaimedTechs(CultureId));
        Assert.Equal(4, simulation.Knowledge.MaxTier(CultureId));
        Assert.True(simulation.Knowledge.CultureClaimsSite(CultureId, foundation));
        Assert.True(simulation.Knowledge.CultureClaimsSite(CultureId, advanced));
        Assert.Equal(
            1.32f,
            simulation.Knowledge.Modifier(CultureId, KnowledgeGrantDomainCodes.VerbWeight, target: 17),
            precision: 3);

        _ = simulation.ModifyCellCultureInfluenceRaw(
            CultureId,
            PlaneCodes.Semantic,
            6,
            0,
            -WorldState.CultureClaimedRawMass,
            tick: 2);
        simulation.Knowledge.Tick(simulation, tick: 2);

        Assert.Equal(1, simulation.Knowledge.ClaimedTechs(CultureId));
        Assert.Equal(0, simulation.Knowledge.MaxTier(CultureId));
        Assert.True(simulation.Knowledge.CultureClaimsSite(CultureId, foundation));
        Assert.False(simulation.Knowledge.CultureClaimsSite(CultureId, advanced));
        Assert.Equal(
            1.10f,
            simulation.Knowledge.Modifier(CultureId, KnowledgeGrantDomainCodes.VerbWeight, target: 17),
            precision: 3);
    }

    [Fact]
    public void CultureProductionGrantRoutesOnlyToItsConfiguredPlane()
    {
        var simulation = CreateSimulation();
        var site = simulation.RegisterKnowledgeSite(0, 0, tier: 4);
        simulation.RegisterKnowledgeGrant(
            site,
            KnowledgeGrantDomainCodes.CultureProduction,
            PlaneCodes.Semantic,
            multiplier: 1.50f);
        Claim(simulation, 0, 0);
        simulation.Knowledge.Tick(simulation, tick: 1);

        Assert.Equal(10f, simulation.CultureProductionScaled(CultureId, PlaneCodes.Physical, 10f));
        Assert.Equal(15f, simulation.CultureProductionScaled(CultureId, PlaneCodes.Semantic, 10f));
        Assert.Equal(-10f, simulation.CultureProductionScaled(CultureId, PlaneCodes.Semantic, -10f));
    }

    private static Simulation CreateSimulation()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions
        {
            MapCellCapacity = 512,
            MapInfluenceCapacity = 512,
            EventCapacity = 1024,
            SystemCapacity = 32,
            KnowledgeSiteCapacity = 8,
            KnowledgeGrantCapacity = 16,
            KnowledgeModifierCapacity = 32,
        });
        KernelTestSimulation.SeedSemanticCells(simulation, radius: 8);
        return simulation;
    }

    private static void Claim(Simulation simulation, int q, int r) =>
        _ = simulation.ModifyCellCultureInfluenceRaw(
            CultureId,
            PlaneCodes.Semantic,
            q,
            r,
            WorldState.CultureClaimedRawMass,
            tick: 0);
}
