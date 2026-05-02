namespace TheCanonryGame.Game.Tests;

using Engine.Host;

public sealed class GameSeedPolicyTests
{
    [Fact]
    public void Random_CreatesRandomStartupSeedPolicyWithoutRequestedSeed()
    {
        var policy = GameSeedPolicy.Random();

        var startupPolicy = policy.ToStartupSeedPolicy();

        Assert.Equal(GameSeedPolicyKind.Random, policy.Kind);
        Assert.Equal(0, policy.RequestedSeed);
        Assert.Equal(StartupSeedPolicyKind.Random, startupPolicy.Kind);
        Assert.Equal(0, startupPolicy.RequestedSeed);
    }

    [Fact]
    public void Requested_CreatesRequestedStartupSeedPolicyWithSameSeed()
    {
        var policy = GameSeedPolicy.Requested(12345);

        var startupPolicy = policy.ToStartupSeedPolicy();

        Assert.Equal(GameSeedPolicyKind.Requested, policy.Kind);
        Assert.Equal(12345, policy.RequestedSeed);
        Assert.Equal(StartupSeedPolicyKind.Requested, startupPolicy.Kind);
        Assert.Equal(12345, startupPolicy.RequestedSeed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Requested_RejectsNonPositiveSeeds(int seed)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GameSeedPolicy.Requested(seed));
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(2, 5, 3)]
    [InlineData(5, 5, 0)]
    [InlineData(7, 3, 0)]
    public void GameRuntimePulse_ReportsOnlyForwardProgress(int previousTick, int currentTick, int expectedProcessed)
    {
        var pulse = new GameRuntimePulse(previousTick, currentTick);

        Assert.Equal(expectedProcessed, pulse.TicksProcessed);
    }
}
