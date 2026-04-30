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
    public void Simulation_LowAltitudePhysicsMovesEntitiesAndOldWorkProgramsStayDisabled()
    {
        const int ticks = 150;
        using var runtime = CreateRuntime(seed: 42);
        var initialEntityCount = runtime.Ui.Entities.Count;
        var lowAltitudeVerbKindIds = new HashSet<int>
        {
            runtime.DomainIds.KindIdOf(IceRemembersKinds.Penguin).Value,
            runtime.DomainIds.KindIdOf(IceRemembersKinds.Artifact).Value,
        };
        var startPhysicalByEntity = ActiveEntities(runtime)
            .Where(entity => lowAltitudeVerbKindIds.Contains(entity.KindId))
            .ToDictionary(
                entity => entity.EntityId,
                entity => (
                    entity.PhysicalQ,
                    entity.PhysicalR));

        for (var i = 0; i < ticks; i++)
        {
            var advanced = runtime.Time.AdvanceSingleStep(1);
            Assert.Equal(1, advanced);
        }

        _ = runtime.Pump();
        var physicalEntityMoved = startPhysicalByEntity.Any(pair =>
        {
            var entityId = pair.Key;
            var start = pair.Value;
            return runtime.Ui.Entities.TryGetValue(entityId, out var entity) &&
                (entity.PhysicalQ, entity.PhysicalR) != start;
        });
        Assert.True(physicalEntityMoved, "No entity changed physical position.");

        Assert.DoesNotContain(runtime.Ui.Entities.Values, entity => entity.LastProcessExecution is not null);
        Assert.DoesNotContain(runtime.Ui.Entities.Values, entity => entity.LastGambitExecution is not null);
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
