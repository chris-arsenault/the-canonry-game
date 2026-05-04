namespace Engine.Graph;

using Engine.Domain.Config;
using Engine.Kinds;

/// <summary>
/// A lightweight behavior program commitment to a locality on one plane for a short horizon.
/// The evaluator may keep this across ticks so arrival can pay off with multiple
/// actions instead of immediately dissolving back into one-tick selection.
/// </summary>
public sealed class LocalityVisitIntent
{
    private static int _nextId;

    public LocalityVisitId VisitId { get; } = new(System.Threading.Interlocked.Increment(ref _nextId));
    public LocalityRuntimeKey LocalityKey { get; init; }
    public PrimaryPlane Plane { get; init; }
    public int Q { get; init; }
    public int R { get; init; }
    public int? S { get; init; }
    public EntityId AnchorEntityId { get; init; }
    public int CreatedTick { get; init; }
    public LocalitySelectionReason SelectionReason { get; set; }
    public float BundleScore { get; set; }
    public int GoalCount { get; set; }
    public int DistinctTargetCount { get; set; }
    public int DistinctEffectCount { get; set; }
    public int DistanceAtSelection { get; set; }
    public bool Arrived { get; set; }
    public int ActionsRemaining { get; set; }
    public int RealizedActions { get; set; }
}
