namespace Engine.Cards;

#pragma warning disable CA1062 // Parameters validated by evaluator framework

using Engine.Graph;

/// <summary>
/// A data-defined scoring expression for utility AI. Evaluates to a float
/// based on entity attributes and world state. Used by processes and decisions
/// to compute their desirability score without hard-coded C# logic.
/// </summary>
public interface IScoreExpression
{
    float Evaluate(Entity entity, IMutationContext context);
}

/// <summary>Returns a constant value with optional variance (jitter ±variance around value).</summary>
public sealed class ConstantScore(float value, float variance = 0f) : IScoreExpression
{
    public float Evaluate(Entity entity, IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Math.Clamp(Variance.Jitter(value, variance, context.Random), 0f, 1f);
    }
}

/// <summary>Returns attribute / max, normalized to [0,1], with optional variance.</summary>
public sealed class AttributeRatioScore(string attributeName, float max, float variance = 0f) : IScoreExpression
{
    public float Evaluate(Entity entity, IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var value = context.GetAttribute(entity.Id, attributeName);
        var ratio = max > 0 ? value / max : 0f;
        return Math.Clamp(Variance.Jitter(ratio, variance, context.Random), 0f, 1f);
    }
}

/// <summary>Returns 1 - (attribute / max). High attribute = low score. With optional variance.</summary>
public sealed class InverseAttributeRatioScore(string attributeName, float max, float variance = 0f) : IScoreExpression
{
    public float Evaluate(Entity entity, IMutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var value = context.GetAttribute(entity.Id, attributeName);
        var ratio = max > 0 ? 1f - (value / max) : 1f;
        return Math.Clamp(Variance.Jitter(ratio, variance, context.Random), 0f, 1f);
    }
}

/// <summary>Weighted sum of multiple score expressions.</summary>
public sealed class WeightedSumScore(IReadOnlyList<(IScoreExpression Expression, float Weight)> terms) : IScoreExpression
{
    public float Evaluate(Entity entity, IMutationContext context)
    {
        var total = 0f;
        foreach (var (expr, weight) in terms)
        {
            total += expr.Evaluate(entity, context) * weight;
        }

        return Math.Clamp(total, 0f, 1f);
    }
}

/// <summary>Shared variance helpers for scoring and mutations.</summary>
internal static class Variance
{
    /// <summary>Additive jitter for scores: value + random(-variance, +variance).</summary>
    internal static float Jitter(float value, float variance, Random random)
    {
        if (variance <= 0f) return value;
        return value + ((float)(random.NextDouble() * 2.0 - 1.0) * variance);
    }

    /// <summary>Multiplicative variance for deltas: value * (1 + random(-variance, +variance)).</summary>
    internal static float Scale(float value, float variance, Random random)
    {
        if (variance <= 0f) return value;
        var factor = 1f + ((float)(random.NextDouble() * 2.0 - 1.0) * variance);
        return value * factor;
    }
}
