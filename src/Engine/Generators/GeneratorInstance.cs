namespace Engine.Generators;

using Engine.Domain.Graph;
using Engine.Graph;

/// <summary>
/// Per-run mutable state for a generator definition.
/// Multiple cards may point at the same generator instance.
/// </summary>
public sealed class GeneratorInstance
{
    private const int RecentTargetLimit = 5;

    public GeneratorInstance(DefinitionGeneratorNode definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        GeneratorId = definition.Id;
        Definition = definition;
    }

    public string GeneratorId { get; }
    public DefinitionGeneratorNode Definition { get; }
    public int TotalRuns { get; private set; }
    public int FailedRuns { get; private set; }
    public int? LastFiredTick { get; private set; }
    public int? LastFailedTick { get; private set; }
    public Queue<EntityId> RecentTargets { get; } = new();

    public void RecordRun(int tick)
    {
        TotalRuns++;
        LastFiredTick = tick;
    }

    public void RecordRun(int tick, EntityId target)
    {
        RecordRun(tick);
        RememberTarget(target);
    }

    public void RecordFailure(int tick, EntityId target)
    {
        FailedRuns++;
        LastFailedTick = tick;
        RememberTarget(target);
    }

    public void RememberTarget(EntityId target)
    {
        RecentTargets.Enqueue(target);
        while (RecentTargets.Count > RecentTargetLimit)
        {
            RecentTargets.Dequeue();
        }
    }

    public void Reset()
    {
        TotalRuns = 0;
        FailedRuns = 0;
        LastFiredTick = null;
        LastFailedTick = null;
        RecentTargets.Clear();
    }
}
