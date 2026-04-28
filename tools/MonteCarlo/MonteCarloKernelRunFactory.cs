namespace MonteCarloTool;

using Engine.Host;

using IceRemembers.Seed;

internal static class MonteCarloKernelRunFactory
{
    private static readonly KernelRuntimeFactory RuntimeFactory = IceRemembersRuntime.CreateRuntimeFactory();

    public static MonteCarloKernelRun Create(int seed, bool traceEnabled = false)
    {
        var runtime = RuntimeFactory.Start(new KernelStartRequest(
            IceRemembersRuntime.DomainName,
            StartupSeedPolicy.Requested(seed),
            KernelFunctionProfile.Batch,
            traceEnabled));

        return new MonteCarloKernelRun(
            runtime,
            runtime.Symbols,
            ToMonteCarloSeed(runtime.SeedIdentity));
    }

    private static MonteCarloWorldSeed ToMonteCarloSeed(StartupSeedIdentity identity)
    {
        _ = identity.TryGetComponent("terrain", out var terrainSeed);
        _ = identity.TryGetComponent("entity", out var entitySeed);
        return new MonteCarloWorldSeed(
            identity.BaseSeed,
            identity.Attempt,
            terrainSeed,
            entitySeed);
    }
}
