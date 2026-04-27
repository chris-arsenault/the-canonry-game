namespace IceRemembers.Seed;

using Engine.Domain.Config;
using Engine.Host;
using IceRemembers;

internal sealed class IceRemembersKernelDomainModule : IKernelDomainModule
{
    public const string DomainName = "ice-remembers";

    public static IceRemembersKernelDomainModule Instance { get; } = new();

    private IceRemembersKernelDomainModule()
    {
    }

    public string Name => DomainName;

    public DomainConfig CreateDomainConfig() => IceRemembersDefinition.Create();

    public DomainStartupSeedResult BuildStartupSeed(DomainStartupSeedRequest request)
    {
        var accepted = IceRemembersSeedBuilder.BuildAcceptedStartupSeed(request.BaseSeed);
        var acceptedSeed = accepted.Seed;
        var identity = new StartupSeedIdentity(
            acceptedSeed.BaseSeed,
            acceptedSeed.Attempt,
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["terrain"] = acceptedSeed.TerrainSeed,
                ["entity"] = acceptedSeed.EntitySeed,
            });

        return new DomainStartupSeedResult(accepted.StartupSeed, identity);
    }

    public void ConfigureProjectionSymbols(KernelSymbolContext symbols)
    {
        ArgumentNullException.ThrowIfNull(symbols);
        symbols.UseEntityNameSynthesizer(IceRemembersEntityNameSynthesizer.Instance);
    }
}

internal static class IceRemembersRuntime
{
    public const string DomainName = IceRemembersKernelDomainModule.DomainName;

    public static KernelRuntimeFactory CreateRuntimeFactory() =>
        KernelRuntimeFactory.ForDomains(IceRemembersKernelDomainModule.Instance);

    public static DomainConfig CreateDomainConfig() =>
        IceRemembersKernelDomainModule.Instance.CreateDomainConfig();
}
