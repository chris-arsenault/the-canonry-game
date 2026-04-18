namespace Engine.Tests.Domain;

using System.IO;
using Engine.Graph;
using Engine.Kinds;
using Engine.Messages;
using Engine.Observability;
using Engine.Simulation;

/// <summary>
/// Smoke test: after 150 ticks of the test sim, every entity kind should have
/// fired at least one process, and at least one faction should have moved on the
/// semantic plane (chasing NPC targets). Also verifies generator content grows
/// the world. Catches regressions where planner bugs or generator output stop
/// producing motion.
/// </summary>
public class MotionDebugTests
{
    [Fact]
    public void TestMap_AllEntities_StartWithDistinctPositionsOnBothPlanes()
    {
        var sim = SimulationRunner.CreateFromTestMap();
        var entities = sim.Graph.AllEntities
            .Select(kv => kv.Value)
            .Where(entity => entity.Kind is EntityKind.Npc or EntityKind.Settlement or EntityKind.Faction or EntityKind.Artifact)
            .ToList();

        var physical = entities.Select(e => e.Coordinates).ToList();
        Assert.Equal(
            physical.Count,
            physical.Distinct().Count());

        var semantic = entities.Select(e => e.SemanticPosition).ToList();
        Assert.Equal(
            semantic.Count,
            semantic.Distinct().Count());
    }

    [Fact]
    public void RunSim_EveryKindActs_AndFactionsChaseSemantically()
    {
        const int ticks = 150;
        var sim = SimulationRunner.CreateFromTestMap();
        var initialEntityCount = sim.Graph.EntityCount;

        var actionsByKind = new Dictionary<EntityKind, int>();
        sim.MessageBus.Subscribe(msg =>
        {
            if (msg is not ProcessExecutedMessage pem) return;
            actionsByKind.TryGetValue(pem.ActorKind, out var count);
            actionsByKind[pem.ActorKind] = count + 1;
        });

        var startSemanticByKind = sim.Graph.AllEntities
            .GroupBy(kv => kv.Value.Kind)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(kv => kv.Key, kv => kv.Value.SemanticPosition));

        for (var i = 0; i < ticks; i++)
        {
            sim.Tick();
        }

        foreach (var kind in new[] { EntityKind.Npc, EntityKind.Faction, EntityKind.Settlement, EntityKind.Artifact })
        {
            Assert.True(
                actionsByKind.TryGetValue(kind, out var acted) && acted > 0,
                $"{kind} performed no actions over {ticks} ticks — planner or generator regression.");
        }

        var factionMovedSemantically = sim.Graph
            .GetEntitiesByKind(EntityKind.Faction)
            .Any(id =>
            {
                var entity = sim.Graph.GetEntity(id);
                if (entity is null) return false;
                var start = startSemanticByKind[EntityKind.Faction][id];
                return entity.SemanticPosition != start;
            });
        Assert.True(factionMovedSemantically,
            "No faction changed its semantic position — mobility sub-goal routing may be broken.");
        Assert.True(
            sim.Graph.EntityCount > initialEntityCount,
            "Generators did not grow the world over 150 ticks.");
    }

    [Fact]
    public void RunLogger_EmitsPerTickBlocks_ForShortRun()
    {
        // Write to a temp file so we can inspect structure without stomping on
        // .artifacts logs from real runs.
        var logPath = Path.Combine(Path.GetTempPath(),
            $"canonry-runlogger-test-{System.Guid.NewGuid():N}.log");

        try
        {
            var sim = SimulationRunner.CreateFromTestMap();
            using (var logger = new RunLogger(logPath, sim))
            {
                for (var i = 0; i < 5; i++)
                {
                    sim.Tick();
                    logger.OnTickEnded();
                }
            }

            var lines = File.ReadAllLines(logPath);
            Assert.Contains(lines, l => l.StartsWith("# The Canonry sim run log", System.StringComparison.Ordinal));
            Assert.Contains(lines, l => l.StartsWith("=== initial state", System.StringComparison.Ordinal));
            Assert.Contains(lines, l => l.StartsWith("=== t=1 ===", System.StringComparison.Ordinal));
            Assert.Contains(lines, l => l.StartsWith("=== t=5 ===", System.StringComparison.Ordinal));
            Assert.Contains(lines, l => l.Contains(" -- t=5:", System.StringComparison.Ordinal));
        }
        finally
        {
            if (File.Exists(logPath)) File.Delete(logPath);
        }
    }
}
