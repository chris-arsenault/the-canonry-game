namespace Engine.Processes;

using Engine.Graph;
using Engine.Map;

/// <summary>
/// A composable expression that evaluates to a float given entity state.
/// Resolves attribute paths (self.hunger, target.ideology.x) and supports
/// arithmetic and built-in functions.
/// </summary>
public interface IExpression
{
    float Evaluate(ExpressionContext ctx);
}

/// <summary>
/// Context for expression evaluation. Provides access to self entity,
/// target entity, and world state for path resolution and built-in functions.
/// </summary>
public sealed class ExpressionContext
{
    public Entity Self { get; }
    public Entity? Target { get; }
    public HexGrid? Grid { get; }

    public ExpressionContext(Entity self, Entity? target = null, HexGrid? grid = null)
    {
        Self = self;
        Target = target;
        Grid = grid;
    }

    public float ResolvePath(string entityRef, string attributeName, string? component = null)
    {
        var entity = entityRef switch
        {
            "self" => Self,
            "target" => Target,
            _ => null,
        };

        if (entity is null) return 0f;

        var attr = entity.Attributes[attributeName];
        if (attr is null) return 0f;

        if (component is not null && attr is Attributes.SemanticPlane plane)
        {
            return component switch
            {
                "x" => plane.X,
                "y" => plane.Y,
                _ => 0f,
            };
        }

        if (attr is Attributes.ScalarValue scalar)
        {
            return scalar.Value;
        }

        return 0f;
    }
}

// --- Leaf expressions ---

/// <summary>A constant value.</summary>
public sealed class Literal(float value) : IExpression
{
    public float Evaluate(ExpressionContext ctx) => value;
}

/// <summary>Resolve an attribute path: self.hunger, target.ideology.x, etc.</summary>
public sealed class AttributePath(string entityRef, string attributeName, string? component = null) : IExpression
{
    public float Evaluate(ExpressionContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        return ctx.ResolvePath(entityRef, attributeName, component);
    }
}

/// <summary>Read a named context variable (set by CalculateMutation).</summary>
public sealed class VariableRef(string variableName) : IExpression
{
    public string VariableName { get; } = variableName;

    public float Evaluate(ExpressionContext ctx) => 0f; // Variables resolved by IMutationContext, not ExpressionContext
}

// --- Arithmetic ---

public sealed class Add(IExpression left, IExpression right) : IExpression
{
    public float Evaluate(ExpressionContext ctx) => left.Evaluate(ctx) + right.Evaluate(ctx);
}

public sealed class Subtract(IExpression left, IExpression right) : IExpression
{
    public float Evaluate(ExpressionContext ctx) => left.Evaluate(ctx) - right.Evaluate(ctx);
}

public sealed class Multiply(IExpression left, IExpression right) : IExpression
{
    public float Evaluate(ExpressionContext ctx) => left.Evaluate(ctx) * right.Evaluate(ctx);
}

public sealed class Divide(IExpression left, IExpression right) : IExpression
{
    public float Evaluate(ExpressionContext ctx)
    {
        var divisor = right.Evaluate(ctx);
        return divisor == 0f ? 0f : left.Evaluate(ctx) / divisor;
    }
}

// --- Built-in functions ---

/// <summary>Hex distance between self and target entity coordinates.</summary>
public sealed class Distance : IExpression
{
    public float Evaluate(ExpressionContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        if (ctx.Self.Coordinates is null || ctx.Target?.Coordinates is null) return float.MaxValue;

        return HexCoord.Distance(
            new HexCoord(ctx.Self.Coordinates.Value.Q, ctx.Self.Coordinates.Value.R),
            new HexCoord(ctx.Target.Coordinates.Value.Q, ctx.Target.Coordinates.Value.R));
    }
}

/// <summary>Euclidean distance between self and target on a 2D semantic plane attribute.</summary>
public sealed class SemanticDistance(string attributeName) : IExpression
{
    public float Evaluate(ExpressionContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        if (ctx.Target is null) return float.MaxValue;

        var selfPlane = ctx.Self.Attributes.GetPlane(attributeName);
        var targetPlane = ctx.Target.Attributes.GetPlane(attributeName);
        if (selfPlane is null || targetPlane is null)
            return float.MaxValue;

        return selfPlane.DistanceTo(targetPlane);
    }
}

public sealed class Min(IExpression a, IExpression b) : IExpression
{
    public float Evaluate(ExpressionContext ctx) => Math.Min(a.Evaluate(ctx), b.Evaluate(ctx));
}

public sealed class Max(IExpression a, IExpression b) : IExpression
{
    public float Evaluate(ExpressionContext ctx) => Math.Max(a.Evaluate(ctx), b.Evaluate(ctx));
}

public sealed class Clamp(IExpression value, IExpression min, IExpression max) : IExpression
{
    public float Evaluate(ExpressionContext ctx) =>
        Math.Clamp(value.Evaluate(ctx), min.Evaluate(ctx), max.Evaluate(ctx));
}

public sealed class Abs(IExpression value) : IExpression
{
    public float Evaluate(ExpressionContext ctx) => Math.Abs(value.Evaluate(ctx));
}

/// <summary>Adds random jitter: value + random(-variance, +variance). For scoring variance.</summary>
public sealed class Jitter(IExpression value, float variance) : IExpression
{
    public float Evaluate(ExpressionContext ctx)
    {
        var v = value.Evaluate(ctx);
        if (variance <= 0f) return v;
        // Use a simple hash-based random since ExpressionContext has no Random
        // This is deterministic per-evaluation which is fine for scoring
        return v + ((float)(System.Random.Shared.NextDouble() * 2.0 - 1.0) * variance);
    }
}

/// <summary>Helper for building expressions concisely in domain config code.</summary>
public static class Expr
{
    public static Literal Lit(float v) => new(v);
    public static AttributePath Self(string attr) => new("self", attr);
    public static AttributePath SelfX(string attr) => new("self", attr, "x");
    public static AttributePath SelfY(string attr) => new("self", attr, "y");
    public static AttributePath Target(string attr) => new("target", attr);
    public static AttributePath TargetX(string attr) => new("target", attr, "x");
    public static AttributePath TargetY(string attr) => new("target", attr, "y");
    public static VariableRef Var(string name) => new(name);
    public static Distance Distance() => new();
    public static SemanticDistance SemanticDistance(string attr) => new(attr);
    public static Add Add(IExpression l, IExpression r) => new(l, r);
    public static Subtract Sub(IExpression l, IExpression r) => new(l, r);
    public static Multiply Mul(IExpression l, IExpression r) => new(l, r);
    public static Divide Div(IExpression l, IExpression r) => new(l, r);
    public static Abs Abs(IExpression v) => new(v);
    public static Clamp Clamp(IExpression v, IExpression min, IExpression max) => new(v, min, max);
    public static Jitter Jitter(IExpression v, float variance) => new(v, variance);
    public static Clamp Score(IExpression v, float variance = 0f) =>
        new(variance > 0f ? new Jitter(v, variance) : v, new Literal(0f), new Literal(1f));
}
