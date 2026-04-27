namespace Engine.Host.Tests;

using Engine.Domain.Config;
using Engine.Domain.Startup;
using Engine.Host;
using IceRemembers;

public sealed class KernelRuntimeFactoryTests
{
    [Fact]
    public void Start_ReusesCompiledDomainAcrossDifferentRequestedSeeds()
    {
        var domain = new CountingDomainModule();
        var factory = KernelRuntimeFactory.ForDomains(domain);

        using var first = factory.Start(Request(domain.Name, seed: 100));
        using var second = factory.Start(Request(domain.Name, seed: 101));

        Assert.Equal(1, domain.CreateDomainConfigCalls);
        Assert.Equal(2, domain.BuildStartupSeedCalls);
        Assert.Equal(100, first.SeedIdentity.BaseSeed);
        Assert.Equal(101, second.SeedIdentity.BaseSeed);
    }

    [Fact]
    public void Start_DoesNotShareRuntimeDomainIdsAcrossCachedCompiledDomainStarts()
    {
        var domain = new CountingDomainModule();
        var factory = KernelRuntimeFactory.ForDomains(domain);

        using var first = factory.Start(Request(domain.Name, seed: 100));
        var settlementId = first.Host.DomainIds.KindIdOf(IceRemembersKinds.Settlement);
        first.Host.DomainIds.RegisterKind("runtime_only_kind");

        using var second = factory.Start(Request(domain.Name, seed: 101));

        Assert.Equal(settlementId, second.Host.DomainIds.KindIdOf(IceRemembersKinds.Settlement));
        Assert.Throws<KeyNotFoundException>(() => second.Host.DomainIds.KindIdOf("runtime_only_kind"));
    }

    private static KernelStartRequest Request(string domainName, int seed) =>
        new(domainName, StartupSeedPolicy.Requested(seed), KernelFunctionProfile.Batch);

    private sealed class CountingDomainModule : IKernelDomainModule
    {
        public string Name => "counting-domain";

        public int CreateDomainConfigCalls { get; private set; }

        public int BuildStartupSeedCalls { get; private set; }

        public DomainConfig CreateDomainConfig()
        {
            CreateDomainConfigCalls++;
            return IceRemembersDefinition.Create();
        }

        public DomainStartupSeedResult BuildStartupSeed(DomainStartupSeedRequest request)
        {
            BuildStartupSeedCalls++;
            return new DomainStartupSeedResult(
                DomainStartupSeed.Empty(mapRadius: 1),
                new StartupSeedIdentity(request.BaseSeed, attempt: 0));
        }

        public void ConfigureProjectionSymbols(KernelSymbolContext symbols)
        {
            ArgumentNullException.ThrowIfNull(symbols);
        }
    }
}
