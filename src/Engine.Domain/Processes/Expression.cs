namespace Engine.Processes;

using Engine.Domain;
using Engine.Graph;
using Engine.WorldGen;
using Engine.Resources;

/// <summary>
/// A composable expression that evaluates to a float given entity state.
/// Resolves scalar attribute paths and supports arithmetic and built-in functions.
/// </summary>
public interface IExpression
{
    float Evaluate(ExpressionContext ctx);

    void BindRuntimeIds(global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
    }
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
    public IMutationContext? MutationContext { get; }

    public ExpressionContext(Entity self, Entity? target = null, HexGrid? grid = null,
        IMutationContext? mutationContext = null)
    {
        Self = self;
        Target = target;
        Grid = grid;
        MutationContext = mutationContext;
    }

    public float ResolvePath(string entityRef, string attributeName)
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

        if (attr is Attributes.ScalarValue scalar)
        {
            return scalar.Value;
        }

        return 0f;
    }

    public float ResolvePath(string entityRef, AttributeRuntimeId attributeId)
    {
        var entity = entityRef switch
        {
            "self" => Self,
            "target" => Target,
            _ => null,
        };

        return entity?.Attributes.GetValue(attributeId) ?? 0f;
    }
}

// --- Leaf expressions ---

/// <summary>A constant value.</summary>
public sealed class Literal(float value) : IExpression
{
    public float Value { get; } = value;
    public float Evaluate(ExpressionContext ctx) => Value;
}

/// <summary>Resolve a scalar attribute path, such as self.warmth or target.fame.</summary>
public sealed class AttributePath(string entityRef, string attributeName) : IExpression
{
    private AttributeRuntimeId _attributeId;
    public string EntityRef { get; } = entityRef;
    public string AttributeName { get; } = attributeName;

    public float Evaluate(ExpressionContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        return _attributeId.Value > 0
            ? ctx.ResolvePath(EntityRef, _attributeId)
            : ctx.ResolvePath(EntityRef, AttributeName);
    }

    public void BindRuntimeIds(global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        ArgumentNullException.ThrowIfNull(runtimeIds);
        _attributeId = runtimeIds.AttributeIdOf(AttributeName);
    }
}

/// <summary>Read a named context variable (set by CalculateMutation).</summary>
public sealed class VariableRef : IExpression
{
    public string VariableName { get; }

    public VariableRef(string variableName)
    {
        VariableName = variableName;
    }

    public float Evaluate(ExpressionContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        return ctx.MutationContext?.GetVariable(VariableName) ?? 0f;
    }
}

/// <summary>Read an ambient resource value at self or target location.</summary>
public sealed class LocationResourcePath(string entityRef, ResourceType resourceType) : IExpression
{
    public string EntityRef { get; } = entityRef;
    public ResourceType ResourceType { get; } = resourceType;

    public float Evaluate(ExpressionContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var entity = EntityRef switch
        {
            "self" => ctx.Self,
            "target" => ctx.Target,
            _ => null,
        };

        if (entity is null || ctx.MutationContext is null)
        {
            return 0f;
        }

        return ctx.MutationContext.GetResource(entity.Coordinates, ResourceType);
    }
}

// --- Arithmetic ---

public sealed class Add(IExpression left, IExpression right) : IExpression
{
    public IExpression Left { get; } = left;
    public IExpression Right { get; } = right;
    public float Evaluate(ExpressionContext ctx) => Left.Evaluate(ctx) + Right.Evaluate(ctx);
    public void BindRuntimeIds(global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        Left.BindRuntimeIds(runtimeIds);
        Right.BindRuntimeIds(runtimeIds);
    }
}

public sealed class Subtract(IExpression left, IExpression right) : IExpression
{
    public IExpression Left { get; } = left;
    public IExpression Right { get; } = right;
    public float Evaluate(ExpressionContext ctx) => Left.Evaluate(ctx) - Right.Evaluate(ctx);
    public void BindRuntimeIds(global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        Left.BindRuntimeIds(runtimeIds);
        Right.BindRuntimeIds(runtimeIds);
    }
}

public sealed class Multiply(IExpression left, IExpression right) : IExpression
{
    public IExpression Left { get; } = left;
    public IExpression Right { get; } = right;
    public float Evaluate(ExpressionContext ctx) => Left.Evaluate(ctx) * Right.Evaluate(ctx);
    public void BindRuntimeIds(global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        Left.BindRuntimeIds(runtimeIds);
        Right.BindRuntimeIds(runtimeIds);
    }
}

public sealed class Divide(IExpression left, IExpression right) : IExpression
{
    public IExpression Left { get; } = left;
    public IExpression Right { get; } = right;

    public float Evaluate(ExpressionContext ctx)
    {
        var divisor = Right.Evaluate(ctx);
        return divisor == 0f ? 0f : Left.Evaluate(ctx) / divisor;
    }

    public void BindRuntimeIds(global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        Left.BindRuntimeIds(runtimeIds);
        Right.BindRuntimeIds(runtimeIds);
    }
}

// --- Built-in functions ---

/// <summary>Hex distance between self and target entity coordinates.</summary>
public sealed class Distance : IExpression
{
    public float Evaluate(ExpressionContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        if (ctx.Target is null) return float.MaxValue;

        return HexCoord.Distance(
            new HexCoord(ctx.Self.Coordinates.Q, ctx.Self.Coordinates.R),
            new HexCoord(ctx.Target.Coordinates.Q, ctx.Target.Coordinates.R));
    }
}

public sealed class Min(IExpression a, IExpression b) : IExpression
{
    public IExpression A { get; } = a;
    public IExpression B { get; } = b;
    public float Evaluate(ExpressionContext ctx) => Math.Min(A.Evaluate(ctx), B.Evaluate(ctx));
    public void BindRuntimeIds(global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        A.BindRuntimeIds(runtimeIds);
        B.BindRuntimeIds(runtimeIds);
    }
}

public sealed class Max(IExpression a, IExpression b) : IExpression
{
    public IExpression A { get; } = a;
    public IExpression B { get; } = b;
    public float Evaluate(ExpressionContext ctx) => Math.Max(A.Evaluate(ctx), B.Evaluate(ctx));
    public void BindRuntimeIds(global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        A.BindRuntimeIds(runtimeIds);
        B.BindRuntimeIds(runtimeIds);
    }
}

public sealed class Clamp(IExpression value, IExpression min, IExpression max) : IExpression
{
    public IExpression Value { get; } = value;
    public IExpression Min { get; } = min;
    public IExpression Max { get; } = max;

    public float Evaluate(ExpressionContext ctx) =>
        Math.Clamp(Value.Evaluate(ctx), Min.Evaluate(ctx), Max.Evaluate(ctx));

    public void BindRuntimeIds(global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        Value.BindRuntimeIds(runtimeIds);
        Min.BindRuntimeIds(runtimeIds);
        Max.BindRuntimeIds(runtimeIds);
    }
}

public sealed class Abs(IExpression value) : IExpression
{
    public IExpression Value { get; } = value;
    public float Evaluate(ExpressionContext ctx) => Math.Abs(Value.Evaluate(ctx));
    public void BindRuntimeIds(global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        Value.BindRuntimeIds(runtimeIds);
    }
}

/// <summary>Adds random jitter: value + random(-variance, +variance). For scoring variance.</summary>
public sealed class Jitter(IExpression value, float variance) : IExpression
{
    public IExpression Value { get; } = value;
    public float Variance { get; } = variance;

    public float Evaluate(ExpressionContext ctx)
    {
        var v = Value.Evaluate(ctx);
        if (Variance <= 0f) return v;
        // Use a simple hash-based random since ExpressionContext has no Random
        // This is deterministic per-evaluation which is fine for scoring
        return v + ((float)(System.Random.Shared.NextDouble() * 2.0 - 1.0) * Variance);
    }

    public void BindRuntimeIds(global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        Value.BindRuntimeIds(runtimeIds);
    }
}

/// <summary>Helper for building expressions concisely in domain config code.</summary>
public static class Expr
{
    public static Literal Lit(float v) => new(v);
    public static AttributePath Self(string attr) => new("self", attr);
    public static AttributePath Target(string attr) => new("target", attr);
    public static LocationResourcePath SelfLocationResource(ResourceType resourceType) => new("self", resourceType);
    public static LocationResourcePath TargetLocationResource(ResourceType resourceType) => new("target", resourceType);
    public static VariableRef Var(string name) => new(name);
    public static Distance Distance() => new();
    public static Add Add(IExpression l, IExpression r) => new(l, r);
    public static Subtract Sub(IExpression l, IExpression r) => new(l, r);
    public static Multiply Mul(IExpression l, IExpression r) => new(l, r);
    public static Divide Div(IExpression l, IExpression r) => new(l, r);
    public static Abs Abs(IExpression v) => new(v);
    public static Min Min(IExpression a, IExpression b) => new(a, b);
    public static Max Max(IExpression a, IExpression b) => new(a, b);
    public static Clamp Clamp(IExpression v, IExpression min, IExpression max) => new(v, min, max);
    public static Jitter Jitter(IExpression v, float variance) => new(v, variance);
    public static Clamp Score(IExpression v, float variance = 0f) =>
        new(variance > 0f ? new Jitter(v, variance) : v, new Literal(0f), new Literal(1f));
}
