namespace MonteCarloTool;

using Engine.Domain.Config;

internal static class MonteCarloKernelRunFactory
{
    public static MonteCarloKernelRun Create(DomainConfig domain, int seed)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return KernelNativeWorldBootstrap.CreateMonteCarloRun(domain, seed);
    }
}
