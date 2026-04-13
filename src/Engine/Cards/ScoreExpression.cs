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
    float Evaluate(Entity entity, ICardContext context);
}

/// <summary>Returns a constant value.</summary>
public sealed class ConstantScore(float value) : IScoreExpression
{
    public float Evaluate(Entity entity, ICardContext context) => value;
}

/// <summary>Returns attribute / max, normalized to [0,1].</summary>
public sealed class AttributeRatioScore(string attributeName, float max) : IScoreExpression
{
    public float Evaluate(Entity entity, ICardContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var value = context.GetAttribute(entity.Id, attributeName);
        return max > 0 ? Math.Clamp(value / max, 0f, 1f) : 0f;
    }
}

/// <summary>Returns 1 - (attribute / max). High attribute = low score.</summary>
public sealed class InverseAttributeRatioScore(string attributeName, float max) : IScoreExpression
{
    public float Evaluate(Entity entity, ICardContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var value = context.GetAttribute(entity.Id, attributeName);
        return max > 0 ? Math.Clamp(1f - (value / max), 0f, 1f) : 1f;
    }
}

/// <summary>Weighted sum of multiple score expressions.</summary>
public sealed class WeightedSumScore(IReadOnlyList<(IScoreExpression Expression, float Weight)> terms) : IScoreExpression
{
    public float Evaluate(Entity entity, ICardContext context)
    {
        var total = 0f;
        foreach (var (expr, weight) in terms)
        {
            total += expr.Evaluate(entity, context) * weight;
        }

        return Math.Clamp(total, 0f, 1f);
    }
}
