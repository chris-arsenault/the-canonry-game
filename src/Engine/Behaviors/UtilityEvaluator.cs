namespace Engine.Behaviors;

using Engine.Graph;

/// <summary>
/// Evaluates all available impulses for an entity and selects one using weighted random.
/// Higher-scoring impulses are more likely to be selected, but all viable impulses have
/// a chance (preventing deterministic behavior).
/// </summary>
public sealed class UtilityEvaluator
{
    private readonly ImpulseRegistry _registry;
    private readonly Random _random;

    /// <summary>Minimum score for an impulse to be considered viable.</summary>
    public float MinimumViableScore { get; set; } = 0.01f;

    /// <summary>Maximum number of top candidates to consider for weighted random.</summary>
    public int TopCandidateCount { get; set; } = 5;

    public UtilityEvaluator(ImpulseRegistry registry, Random? random = null)
    {
        _registry = registry;
        _random = random ?? Random.Shared;
    }

    /// <summary>
    /// Evaluate all impulses for an entity and select one via weighted random.
    /// Returns null if no impulse scores above the minimum threshold (entity does nothing).
    /// </summary>
    public EvaluationResult? Evaluate(Entity entity, IWorldState worldState)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(worldState);

        var impulses = _registry.GetImpulses(entity.Kind);
        if (impulses.Count == 0)
        {
            return null;
        }

        // Score all impulses
        var scored = new List<(IImpulse Impulse, float Score)>();
        foreach (var impulse in impulses)
        {
            var score = impulse.Score(entity, worldState);
            if (score >= MinimumViableScore)
            {
                scored.Add((impulse, score));
            }
        }

        if (scored.Count == 0)
        {
            return null;
        }

        // Take top N candidates
        var candidates = scored
            .OrderByDescending(s => s.Score)
            .Take(TopCandidateCount)
            .ToList();

        // Weighted random selection
        var totalWeight = candidates.Sum(c => c.Score);
        var roll = (float)(_random.NextDouble() * totalWeight);
        var cumulative = 0f;

        foreach (var (impulse, score) in candidates)
        {
            cumulative += score;
            if (roll <= cumulative)
            {
                return new EvaluationResult(impulse, score, candidates.Select(c => (c.Impulse.Name, c.Score)).ToList());
            }
        }

        // Fallback to last candidate (floating point edge case)
        var last = candidates[^1];
        return new EvaluationResult(last.Impulse, last.Score, candidates.Select(c => (c.Impulse.Name, c.Score)).ToList());
    }

    /// <summary>
    /// Evaluate and execute the selected impulse in one step.
    /// Returns the evaluation result, or null if the entity did nothing.
    /// </summary>
    public EvaluationResult? EvaluateAndExecute(Entity entity, IWorldState worldState)
    {
        var result = Evaluate(entity, worldState);
        result?.SelectedImpulse.Execute(entity, worldState);
        return result;
    }
}

/// <summary>
/// Result of a utility evaluation: which impulse was selected, its score,
/// and the full candidate list (for debugging/display).
/// </summary>
public sealed class EvaluationResult
{
    public IImpulse SelectedImpulse { get; }
    public float SelectedScore { get; }

    /// <summary>All candidates considered, with their scores. For debugging.</summary>
    public IReadOnlyList<(string Name, float Score)> Candidates { get; }

    public EvaluationResult(
        IImpulse selectedImpulse,
        float selectedScore,
        IReadOnlyList<(string Name, float Score)> candidates)
    {
        SelectedImpulse = selectedImpulse;
        SelectedScore = selectedScore;
        Candidates = candidates;
    }
}
