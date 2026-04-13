namespace Engine.Behaviors.Impulses;

#pragma warning disable CA1062 // Impulse parameters validated by UtilityEvaluator framework

using Engine.Graph;

/// <summary>
/// Degradation: artifact condition decreases over time.
/// Deterministic (not scored by utility AI -- artifacts don't choose).
/// Rate increases with age (handled by lifecycle system, but this adds
/// use-based degradation).
/// </summary>
public sealed class DegradationImpulse : IImpulse
{
    public string Name => "degradation";

    public float BaseDecay { get; set; } = 0.02f;

    public float Score(Entity entity, IWorldState worldState) => 1f; // Always runs

    public void Execute(Entity entity, IWorldState worldState)
    {
        // Additional condition loss beyond lifecycle drain
        worldState.Attributes.Modify(entity.Id, "condition", -BaseDecay);
    }
}

/// <summary>
/// Resonance accumulation: artifacts near significant events accumulate mystical resonance.
/// Increases when the holder has high activity (presence at settlements, involvement in relationships).
/// </summary>
public sealed class ResonanceAccumulationImpulse : IImpulse
{
    public string Name => "resonance_accumulation";

    public float BaseAccumulation { get; set; } = 0.01f;

    public float Score(Entity entity, IWorldState worldState) => 1f; // Always runs

    public void Execute(Entity entity, IWorldState worldState)
    {
        // Check if artifact has a holder (via incoming "holds" relationship)
        var holders = worldState.Graph.IncomingRelationships(entity.Id, "holds");
        var isHeld = holders.Any(r => r.IsActive);

        // Held artifacts accumulate resonance faster
        var rate = isHeld ? BaseAccumulation * 2f : BaseAccumulation;
        worldState.Attributes.Modify(entity.Id, "mystical_resonance", rate);
    }
}
