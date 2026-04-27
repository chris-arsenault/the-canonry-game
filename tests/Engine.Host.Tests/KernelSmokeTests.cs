namespace Engine.Host.Tests;

using Engine.Eventing;
using Engine.Host;
using IceRemembers;
using IceRemembers.Seed;

public sealed class KernelSmokeTests
{
    [Fact]
    public void InitialSeed_EntitiesStartWithDistinctPositionsOnBothPlanes()
    {
        using var runtime = CreateRuntime(seed: 42);
        var host = runtime.Host;
        var world = host.Kernel.World;
        var smokeKindIds = new HashSet<int>
        {
            host.DomainIds.KindIdOf(IceRemembersKinds.Penguin).Value,
            host.DomainIds.KindIdOf(IceRemembersKinds.Settlement).Value,
            host.DomainIds.KindIdOf(IceRemembersKinds.Faction).Value,
            host.DomainIds.KindIdOf(IceRemembersKinds.Artifact).Value,
        };
        var entityIds = ActiveEntities(host)
            .Where(entityId => smokeKindIds.Contains(world.EntityKindId(entityId)))
            .ToArray();

        var physical = entityIds
            .Select(entityId => (world.EntityPhysicalQ(entityId), world.EntityPhysicalR(entityId)))
            .ToArray();
        Assert.Equal(physical.Length, physical.Distinct().Count());

        var semantic = entityIds
            .Select(entityId => (
                world.EntitySemanticQ(entityId),
                world.EntitySemanticR(entityId),
                world.EntitySemanticS(entityId)))
            .ToArray();
        Assert.Equal(semantic.Length, semantic.Distinct().Count());
    }

    [Fact]
    public void Simulation_SeededKindsActAndFactionsMoveSemantically()
    {
        const int ticks = 150;
        using var runtime = CreateRuntime(seed: 42);
        var host = runtime.Host;
        using var cursor = host.Events.CreateCursor(KernelEventCursorStart.Latest);
        var world = host.Kernel.World;
        var initialEntityCount = world.EntityHighWatermark;
        var factionKindId = host.DomainIds.KindIdOf(IceRemembersKinds.Faction).Value;
        var startSemanticByFaction = ActiveEntities(host)
            .Where(entityId => world.EntityKindId(entityId) == factionKindId)
            .ToDictionary(
                entityId => entityId,
                entityId => (
                    world.EntitySemanticQ(entityId),
                    world.EntitySemanticR(entityId),
                    world.EntitySemanticS(entityId)));

        for (var i = 0; i < ticks; i++)
        {
            var advanced = host.Time.AdvanceSingleStep(1);
            Assert.Equal(1, advanced);
        }

        var actionsByKindId = new Dictionary<int, int>();
        while (host.Events.TryRead(cursor, out var sequenced))
        {
            if (sequenced.Event.EventType != KernelEventType.ProcessExecuted)
            {
                continue;
            }

            var executed = sequenced.Event.AsProcessExecuted();
            actionsByKindId.TryGetValue(executed.ActorKindId, out var count);
            actionsByKindId[executed.ActorKindId] = count + 1;
        }

        foreach (var kind in new[]
                 {
                     IceRemembersKinds.Penguin,
                     IceRemembersKinds.Faction,
                     IceRemembersKinds.Settlement,
                     IceRemembersKinds.Artifact,
                 })
        {
            var kindId = host.DomainIds.KindIdOf(kind).Value;
            Assert.True(
                actionsByKindId.TryGetValue(kindId, out var acted) && acted > 0,
                $"{kind.Value} performed no process actions over {ticks} ticks.");
        }

        var factionMovedSemantically = startSemanticByFaction.Any(pair =>
        {
            var entityId = pair.Key;
            var start = pair.Value;
            return (
                world.EntitySemanticQ(entityId),
                world.EntitySemanticR(entityId),
                world.EntitySemanticS(entityId)) != start;
        });
        Assert.True(factionMovedSemantically, "No faction changed semantic position.");
        Assert.True(world.EntityHighWatermark > initialEntityCount, "Generators did not grow the world.");
    }

    private static KernelRuntime CreateRuntime(int seed) =>
        IceRemembersRuntime.CreateRuntimeFactory().Start(new KernelStartRequest(
            IceRemembersRuntime.DomainName,
            StartupSeedPolicy.Requested(seed),
            KernelFunctionProfile.ManualStep));

    private static IEnumerable<int> ActiveEntities(KernelHost host)
    {
        var world = host.Kernel.World;
        for (var entityId = 1; entityId <= world.EntityHighWatermark; entityId++)
        {
            if (world.EntityIsActive(entityId))
            {
                yield return entityId;
            }
        }
    }
}
