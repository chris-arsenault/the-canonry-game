namespace Engine.Host.Tests;

using Engine.Host;

using IceRemembers;
using IceRemembers.Seed;

public sealed class CulturePowerTests
{
    private const int Ticks = 400;

    [Fact]
    public void ComputeCulturePower_ReportsAllCulturesWithSharesThatPartitionEachAxis()
    {
        using var runtime = CreateRuntime(seed: 42);
        for (var i = 0; i < Ticks; i++)
        {
            _ = runtime.Time.AdvanceSingleStep(1);
        }

        _ = runtime.Pump();
        var powers = runtime.CulturePowers();

        Assert.Equal(runtime.CompiledDomain!.Config.Cultures.Count, powers.Count);
        Assert.Equal(powers.Count, powers.Select(power => power.CultureId).Distinct().Count());
        foreach (var power in powers)
        {
            Assert.InRange(power.WitnessShare, 0f, 100.5f);
            Assert.InRange(power.ShadowShare, 0f, 100.5f);
            Assert.InRange(power.WakeShare, 0f, 100.5f);
        }

        // Physical mass always exists after seeding, so Witness partitions the world.
        Assert.Equal(100f, powers.Sum(power => power.WitnessShare), precision: 1);

        // Every axis mean is built from components dropped world-wide when empty,
        // so each axis either partitions to 100% or is entirely absent.
        AssertPartitionsOrEmpty(powers.Sum(power => power.ShadowShare));
        AssertPartitionsOrEmpty(powers.Sum(power => power.WakeShare));
    }

    [Fact]
    public void ComputeCulturePower_SeedCustodyFeedsTheShadowLedger()
    {
        using var runtime = CreateRuntime(seed: 42);
        _ = runtime.Pump();
        var powers = runtime.CulturePowers();

        // The startup seed places held artifacts and practiced abilities, so
        // custody is a live Shadow component before any tick runs.
        Assert.True(powers.Sum(power => power.CustodyStrength) > 0f, "No custody strength at seed.");
        Assert.Equal(100f, powers.Sum(power => power.ShadowShare), precision: 1);
    }

    private static void AssertPartitionsOrEmpty(float shareSum)
    {
        var partitions = Math.Abs(shareSum - 100f) < 0.5f;
        var empty = shareSum < 0.5f;
        Assert.True(partitions || empty, $"Axis share sum {shareSum} neither ~100 nor ~0.");
    }

    private static KernelUiRuntime CreateRuntime(int seed) =>
        IceRemembersRuntime.CreateRuntimeFactory().StartUiRuntime(new KernelStartRequest(
            IceRemembersRuntime.DomainName,
            StartupSeedPolicy.Requested(seed),
            KernelFunctionProfile.ManualStep));
}
