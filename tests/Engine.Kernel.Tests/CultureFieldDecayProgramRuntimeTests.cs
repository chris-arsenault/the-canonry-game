namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

public sealed class CultureFieldDecayProgramRuntimeTests
{
    private const int CultureId = 1;

    [Fact]
    public void Decay_ReducesLargeMassMultiplicativelyAtPulseCadence()
    {
        var simulation = CreateSimulation(decayPerPulse: FixedScalar.FromSingle(0.01f), minimumDecayPerPulse: FixedScalar.FromSingle(0.5f), cadencePulses: 1);
        simulation.ModifyCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(1000f), tick: 0);

        AdvanceOnePulse(simulation);

        var raw = simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0);
        Assert.Equal(FixedScalar.FromSingle(990f), raw);
    }

    [Fact]
    public void Decay_AppliesMinimumDecayToSmallMassAndCullsAtFloor()
    {
        var simulation = CreateSimulation(decayPerPulse: FixedScalar.FromSingle(0.005f), minimumDecayPerPulse: FixedScalar.FromSingle(1f), cadencePulses: 1);
        simulation.ModifyCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(5f), tick: 0);

        AdvanceOnePulse(simulation);
        Assert.Equal(FixedScalar.FromSingle(4f), simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0));

        for (var pulse = 0; pulse < 5; pulse++)
        {
            AdvanceOnePulse(simulation);
        }

        Assert.Equal(FixedScalar.FromSingle(0f), simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0));
        Assert.Equal(0, CellCount(simulation));
    }

    [Fact]
    public void Decay_SustainedProductionOutpacesLoss()
    {
        var simulation = CreateSimulation(decayPerPulse: FixedScalar.FromSingle(0.01f), minimumDecayPerPulse: FixedScalar.FromSingle(0.5f), cadencePulses: 1);
        simulation.ModifyCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(100f), tick: 0);

        for (var pulse = 0; pulse < 4; pulse++)
        {
            AdvanceOnePulse(simulation);
            simulation.ModifyCellCultureInfluenceRaw(
                CultureId,
                PlaneCodes.Physical,
                0,
                0,
                FixedScalar.FromSingle(5f),
                simulation.CurrentTick);
        }

        var raw = simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0);
        Assert.True(raw > FixedScalar.FromSingle(100f));
    }

    [Fact]
    public void Decay_ZeroRatesLeaveFieldUntouched()
    {
        var simulation = CreateSimulation(decayPerPulse: FixedScalar.FromSingle(0f), minimumDecayPerPulse: FixedScalar.FromSingle(0f), cadencePulses: 1);
        simulation.ModifyCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(250f), tick: 0);

        AdvanceOnePulse(simulation);
        AdvanceOnePulse(simulation);

        Assert.Equal(FixedScalar.FromSingle(250f), simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0));
    }

    [Fact]
    public void Decay_DowngradesLocalityStrengthBandWhenSupportThresholdCrossed()
    {
        var simulation = CreateSimulation(decayPerPulse: FixedScalar.FromSingle(0.005f), minimumDecayPerPulse: FixedScalar.FromSingle(2f), cadencePulses: 1);
        simulation.ModifyCellCultureInfluenceRaw(
            CultureId,
            PlaneCodes.Physical,
            0,
            0,
            WorldState.CultureClaimedRawMass + FixedScalar.FromSingle(1f),
            tick: 0);
        var before = simulation.CultureLocality.CellAt(simulation.World, CultureId, PlaneCodes.Physical, 0, 0);
        Assert.Equal(CultureFieldStrengthBandCodes.Claimed, before.StrengthBandCode);

        AdvanceOnePulse(simulation);

        Assert.True(
            simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0) <
            WorldState.CultureClaimedRawMass);
        var after = simulation.CultureLocality.CellAt(simulation.World, CultureId, PlaneCodes.Physical, 0, 0);
        Assert.Equal(CultureFieldStrengthBandCodes.Trace, after.StrengthBandCode);
    }

    private static void AdvanceOnePulse(Simulation simulation)
    {
        var cadence = simulation.Options.KernelTuning.Programs.CultureField.DecayCadencePulses;
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
            if (raw > FixedScalar.FromSingle(0f))
            {
                count++;
            }
        }

        return count;
    }

    private static Simulation CreateSimulation(
        FixedScalar decayPerPulse,
        FixedScalar minimumDecayPerPulse,
        int cadencePulses) =>
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
                        CullFloorRawMass = FixedScalar.FromSingle(1f),
                        DecayCadencePulses = cadencePulses,
                    },
                },
            },
        });
}
