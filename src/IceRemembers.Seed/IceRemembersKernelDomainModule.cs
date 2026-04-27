namespace IceRemembers.Seed;

using Engine.Domain.Config;
using Engine.Host;
using IceRemembers;

public sealed class IceRemembersKernelDomainModule : IKernelDomainModule
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
        var acceptedSeed = IceRemembersSeedBuilder.SelectAcceptedSeed(request.BaseSeed);
        var startupSeed = IceRemembersSeedBuilder.BuildStartupSeed(acceptedSeed);
        var identity = new StartupSeedIdentity(
            acceptedSeed.BaseSeed,
            acceptedSeed.Attempt,
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["terrain"] = acceptedSeed.TerrainSeed,
                ["entity"] = acceptedSeed.EntitySeed,
            });

        return new DomainStartupSeedResult(startupSeed, identity);
    }

    public void ConfigureProjectionSymbols(KernelSymbolContext symbols)
    {
        ArgumentNullException.ThrowIfNull(symbols);
        symbols.UseEntityNameSynthesizer(IceRemembersEntityNameSynthesizer.Instance);
    }
}

public static class IceRemembersRuntime
{
    public const string DomainName = IceRemembersKernelDomainModule.DomainName;

    public static KernelRuntimeFactory CreateRuntimeFactory() =>
        KernelRuntimeFactory.ForDomains(IceRemembersKernelDomainModule.Instance);

    public static DomainConfig CreateDomainConfig() =>
        IceRemembersKernelDomainModule.Instance.CreateDomainConfig();
}
