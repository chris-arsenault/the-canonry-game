namespace Engine.Behaviors.Impulses;

#pragma warning disable CA1062 // Impulse parameters validated by UtilityEvaluator framework

using Engine.Graph;

/// <summary>
/// Population attraction: prosperous settlements attract population, struggling ones lose it.
/// Deterministic (not scored by utility AI -- settlements don't choose).
/// </summary>
public sealed class PopulationGrowthImpulse : IImpulse
{
    public string Name => "population_growth";

    /// <summary>Growth rate per tick when conditions are favorable.</summary>
    public float GrowthRate { get; set; } = 0.5f;

    /// <summary>Decline rate per tick when conditions are unfavorable.</summary>
    public float DeclineRate { get; set; } = 0.3f;

    /// <summary>Prosperity threshold below which population declines.</summary>
    public float ProsperityThreshold { get; set; } = 30f;

    public float Score(Entity entity, IWorldState worldState) => 1f; // Always runs

    public void Execute(Entity entity, IWorldState worldState)
    {
        var prosperity = worldState.Attributes.Get(entity.Id, "prosperity");
        var population = worldState.Attributes.Get(entity.Id, "population");

        if (population <= 0)
        {
            return;
        }

        var delta = prosperity > ProsperityThreshold
            ? GrowthRate * (prosperity / 100f)
            : -DeclineRate * ((ProsperityThreshold - prosperity) / ProsperityThreshold);

        worldState.Attributes.Modify(entity.Id, "population", delta);
    }
}

/// <summary>
/// Development: settlements with sustained prosperity develop over time.
/// </summary>
public sealed class DevelopmentImpulse : IImpulse
{
    public string Name => "development";

    public float GrowthRate { get; set; } = 0.1f;
    public float DeclineRate { get; set; } = 0.05f;
    public float ProsperityThreshold { get; set; } = 40f;

    public float Score(Entity entity, IWorldState worldState) => 1f;

    public void Execute(Entity entity, IWorldState worldState)
    {
        var prosperity = worldState.Attributes.Get(entity.Id, "prosperity");

        var delta = prosperity > ProsperityThreshold ? GrowthRate : -DeclineRate;
        worldState.Attributes.Modify(entity.Id, "development", delta);
    }
}
