namespace Engine.Host.Tests;

using Engine.Host;

using IceRemembers;
using IceRemembers.Seed;

// Slow: seeds a full world; excluded from the fast loop.
[Trait("Category", "Slow")]
public sealed class KnowledgeOverviewTests
{
    [Fact]
    public void KnowledgeOverview_ReportsLiveClaimsAndDeduplicatedEffectiveModifiers()
    {
        using var runtime = IceRemembersRuntime.CreateRuntimeFactory().StartUiRuntime(new KernelStartRequest(
            IceRemembersRuntime.DomainName,
            StartupSeedPolicy.Requested(42),
            KernelFunctionProfile.ManualStep));

        var initial = runtime.KnowledgeOverview();
        _ = runtime.Time.AdvanceSingleStep(80);
        _ = runtime.Pump();
        var current = runtime.KnowledgeOverview();

        Assert.True(current.Revision > initial.Revision);
        Assert.Equal(runtime.CompiledDomain!.Config.Cultures.Count, current.Cultures.Count);
        Assert.Contains(current.Cultures, static culture => culture.ClaimedTechs > 0);
        foreach (var culture in current.Cultures)
        {
            Assert.Equal(culture.ClaimedTechs, culture.Claims.Count);
            Assert.Equal(
                culture.Modifiers.Count,
                culture.Modifiers
                    .Select(static modifier => (modifier.Domain, modifier.TargetId))
                    .Distinct()
                    .Count());
            Assert.All(culture.Modifiers, static modifier =>
                Assert.NotEqual(1f, modifier.Multiplier));
        }
    }
}
