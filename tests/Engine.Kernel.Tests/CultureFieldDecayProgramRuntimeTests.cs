namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

public sealed class CultureFieldDecayProgramRuntimeTests
{
    private const int CultureId = 1;

    [Fact]
    public void Decay_ReducesLargeMassMultiplicativelyAtPulseCadence()
    {
        var simulation = CreateSimulation(decayPerPulse: 0.01f, minimumDecayPerPulse: 0.5f, cadenceMainTicks: 1);
        simulation.ModifyCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0, 1000f, tick: 0);

        AdvanceOnePulse(simulation);

        var raw = simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0);
        Assert.Equal(990f, raw, precision: 2);
    }

    [Fact]
    public void Decay_AppliesMinimumDecayToSmallMassAndCullsAtFloor()
    {
        var simulation = CreateSimulation(decayPerPulse: 0.005f, minimumDecayPerPulse: 1f, cadenceMainTicks: 1);
        simulation.ModifyCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0, 5f, tick: 0);

        AdvanceOnePulse(simulation);
        Assert.Equal(4f, simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0), precision: 2);

        for (var pulse = 0; pulse < 5; pulse++)
        {
            AdvanceOnePulse(simulation);
        }

        Assert.Equal(0f, simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0));
        Assert.Equal(0, CellCount(simulation));
    }

    [Fact]
    public void Decay_SustainedProductionOutpacesLoss()
    {
        var simulation = CreateSimulation(decayPerPulse: 0.01f, minimumDecayPerPulse: 0.5f, cadenceMainTicks: 1);
        simulation.ModifyCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0, 100f, tick: 0);

        for (var pulse = 0; pulse < 4; pulse++)
        {
            AdvanceOnePulse(simulation);
            simulation.ModifyCellCultureInfluenceRaw(
                CultureId,
                PlaneCodes.Physical,
                0,
                0,
                5f,
                simulation.CurrentTick);
        }

        var raw = simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0);
        Assert.True(raw > 100f);
    }

    [Fact]
    public void Decay_ZeroRatesLeaveFieldUntouched()
    {
        var simulation = CreateSimulation(decayPerPulse: 0f, minimumDecayPerPulse: 0f, cadenceMainTicks: 1);
        simulation.ModifyCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0, 250f, tick: 0);

        AdvanceOnePulse(simulation);
        AdvanceOnePulse(simulation);

        Assert.Equal(250f, simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0));
    }

    [Fact]
    public void Decay_DowngradesBoundaryStrengthBandWhenSupportThresholdCrossed()
    {
        var simulation = CreateSimulation(decayPerPulse: 0.005f, minimumDecayPerPulse: 2f, cadenceMainTicks: 1);
        simulation.ModifyCellCultureInfluenceRaw(
            CultureId,
            PlaneCodes.Physical,
            0,
            0,
            WorldState.CultureClaimedRawMass + 1f,
            tick: 0);
        var before = simulation.CultureBoundaries.CellAt(simulation.World, CultureId, PlaneCodes.Physical, 0, 0);
        Assert.Equal(CultureFieldStrengthBandCodes.Claimed, before.StrengthBandCode);

        AdvanceOnePulse(simulation);

        Assert.True(
            simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0) <
            WorldState.CultureClaimedRawMass);
        var after = simulation.CultureBoundaries.CellAt(simulation.World, CultureId, PlaneCodes.Physical, 0, 0);
        Assert.Equal(CultureFieldStrengthBandCodes.Trace, after.StrengthBandCode);
    }

    private static void AdvanceOnePulse(Simulation simulation)
    {
        var mainCadence = simulation.Options.KernelTuning.Programs.MinorActorMainTickRatio;
        var cadence = simulation.Options.KernelTuning.Programs.CultureField.DecayCadenceMainTicks * mainCadence;
        var currentTick = simulation.CurrentTick;
        var nextPulseTick = ((currentTick / cadence) + 1) * cadence;
        simulation.AdvanceTicks(nextPulseTick - currentTick);
    }

    private static int CellCount(Simulation simulation)
    {
        var count = 0;
        var cursor = simulation.World.RawCultureInfluenceCellsOf(CultureId, PlaneCodes.Physical);
        while (simulation.World.MoveNextRawCultureInfluenceCell(ref cursor, out _, out _, out var raw))
        {
            if (raw > 0f)
            {
                count++;
            }
        }

        return count;
    }

    private static Simulation CreateSimulation(
        float decayPerPulse,
        float minimumDecayPerPulse,
        int cadenceMainTicks) =>
        new(new SimulationOptions
        {
            EntityCapacity = 16,
            MapCellCapacity = 128,
            MapInfluenceCapacity = 128,
            EventCapacity = 4096,
            SystemCapacity = 32,
            KernelTuning = new KernelTuningOptions
            {
                Programs = new KernelProgramTuningOptions
                {
                    CultureField = new CultureFieldTuningOptions
                    {
                        DecayPerPulse = decayPerPulse,
                        MinimumDecayPerPulse = minimumDecayPerPulse,
                        CullFloorRawMass = 1f,
                        DecayCadenceMainTicks = cadenceMainTicks,
                    },
                },
            },
        });
}
