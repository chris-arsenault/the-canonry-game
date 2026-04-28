namespace Engine.Host.Tests;

using Engine.Eventing;
using Engine.Host;
using Engine.Projection;

using IceRemembers;
using IceRemembers.Seed;

public sealed class KernelSmokeTests
{
    [Fact]
    public void InitialSeed_EntitiesStartWithDistinctPositionsOnBothPlanes()
    {
        using var runtime = CreateRuntime(seed: 42);
        var smokeKindIds = new HashSet<int>
        {
            runtime.DomainIds.KindIdOf(IceRemembersKinds.Penguin).Value,
            runtime.DomainIds.KindIdOf(IceRemembersKinds.Settlement).Value,
            runtime.DomainIds.KindIdOf(IceRemembersKinds.Faction).Value,
            runtime.DomainIds.KindIdOf(IceRemembersKinds.Artifact).Value,
        };
        var entities = ActiveEntities(runtime)
            .Where(entity => smokeKindIds.Contains(entity.KindId))
            .ToArray();

        var physical = entities
            .Select(entity => (entity.PhysicalQ, entity.PhysicalR))
            .ToArray();
        Assert.Equal(physical.Length, physical.Distinct().Count());

        var semantic = entities
            .Select(entity => (
                entity.SemanticQ,
                entity.SemanticR,
                entity.SemanticS))
            .ToArray();
        Assert.Equal(semantic.Length, semantic.Distinct().Count());
    }

    [Fact]
    public void Simulation_SeededKindsActAndFactionsMoveSemantically()
    {
        const int ticks = 150;
        using var runtime = CreateRuntime(seed: 42);
        var initialEntityCount = runtime.Ui.Entities.Count;
        var factionKindId = runtime.DomainIds.KindIdOf(IceRemembersKinds.Faction).Value;
        var startSemanticByFaction = ActiveEntities(runtime)
            .Where(entity => entity.KindId == factionKindId)
            .ToDictionary(
                entity => entity.EntityId,
                entity => (
                    entity.SemanticQ,
                    entity.SemanticR,
                    entity.SemanticS));

        for (var i = 0; i < ticks; i++)
        {
            var advanced = runtime.Time.AdvanceSingleStep(1);
            Assert.Equal(1, advanced);
        }

        _ = runtime.Pump();
        var actedKindIds = runtime.Ui.Entities.Values
            .Where(entity => entity.LastProcessExecution is not null)
            .Select(entity => entity.KindId)
            .ToHashSet();

        foreach (var kind in new[]
                 {
                     IceRemembersKinds.Penguin,
                     IceRemembersKinds.Faction,
                     IceRemembersKinds.Settlement,
                     IceRemembersKinds.Artifact,
                 })
        {
            var kindId = runtime.DomainIds.KindIdOf(kind).Value;
            Assert.True(
                actedKindIds.Contains(kindId),
                $"{kind.Value} performed no process actions over {ticks} ticks.");
        }

        var factionMovedSemantically = startSemanticByFaction.Any(pair =>
        {
            var entityId = pair.Key;
            var start = pair.Value;
            return runtime.Ui.Entities.TryGetValue(entityId, out var entity) &&
                (entity.SemanticQ, entity.SemanticR, entity.SemanticS) != start;
        });
        Assert.True(factionMovedSemantically, "No faction changed semantic position.");
        Assert.True(runtime.Ui.Entities.Count > initialEntityCount, "Generators did not grow the world.");
    }

    private static KernelUiRuntime CreateRuntime(int seed) =>
        IceRemembersRuntime.CreateRuntimeFactory().StartUiRuntime(new KernelStartRequest(
            IceRemembersRuntime.DomainName,
            StartupSeedPolicy.Requested(seed),
            KernelFunctionProfile.ManualStep));

    private static IEnumerable<UiEntityProjection> ActiveEntities(KernelUiRuntime runtime) =>
        runtime.Ui.Entities.Values.Where(entity => entity.StatusCode == EntityStatusCodes.Active);
}
