namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

/// <summary>
/// Verb geography filters: a terrain-mode verb can require a minimum warmth
/// band or a site flag on its physical target cells.
/// </summary>
public sealed class LowAltitudeGeographyFilterTests
{
    [Fact]
    public void SetGeographyFilter_StoresAndValidatesItsBounds()
    {
        var simulation = CreateSimulation();
        var slot = RegisterTerrainVerb(simulation);

        simulation.LowAltitudeVerbs.SetGeographyFilter(
            slot, minWarmthBand: 2, siteFlags: PhysicalHexFieldCodes.PolynyaFlag);

        Assert.Equal(2, simulation.LowAltitudeVerbs.Data.MinWarmthBand(slot));
        Assert.Equal(
            PhysicalHexFieldCodes.PolynyaFlag,
            simulation.LowAltitudeVerbs.Data.SiteFlagsFilter(slot));
        Assert.Throws<global::System.InvalidOperationException>(() =>
            simulation.LowAltitudeVerbs.SetGeographyFilter(slot, minWarmthBand: 4, siteFlags: 0));
        Assert.Throws<global::System.InvalidOperationException>(() =>
            simulation.LowAltitudeVerbs.SetGeographyFilter(slot, minWarmthBand: 0, siteFlags: 1 << 13));
    }

    private static int RegisterTerrainVerb(Simulation simulation)
    {
        return simulation.LowAltitudeVerbs.RegisterVerb(
            kindId: 1,
            verbId: 700,
            planeCode: PlaneCodes.Physical,
            operationCode: ProgramOpcodeCodes.MoveActor,
            targetModeCode: TargetSelectorCodes.Terrain,
            targetKindId: 0,
            relationshipKindId: 0,
            relationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Either,
            minCommitmentTicks: 10,
            maxCommitmentTicks: 20,
            dwellTicks: 4,
            targetRadius: 1,
            candidateSampleCount: 8,
            weight: 1f);
    }

    private static Simulation CreateSimulation() => KernelTestSimulation.Create(new SimulationOptions
    {
        EntityCapacity = 8,
        RelationshipKindIdCapacity = 16,
        TerrainIdCapacity = 8,
        EventCapacity = 512,
    });
}
