namespace Engine.Behaviors;

using Engine.Resources;

/// <summary>
/// A composable expression that evaluates to a float given entity state.
/// Expressions are config-DSL records; the kernel compiles them into primitive
/// <c>BehaviorProgramExpressionOp</c> sequences via pattern matching in
/// <c>DomainCompiler</c> and does not evaluate them at runtime.
/// </summary>
#pragma warning disable CA1040 // Marker interface for compiler pattern matching
public interface IExpression
{
}
#pragma warning restore CA1040

/// <summary>A constant value.</summary>
public sealed class Literal(float value) : IExpression
{
    public float Value { get; } = value;
}

/// <summary>Resolve a scalar attribute path, such as self.warmth or target.fame.</summary>
public sealed class AttributePath(string entityRef, string attributeName) : IExpression
{
    public string EntityRef { get; } = entityRef;
    public string AttributeName { get; } = attributeName;
}

/// <summary>Read a named context variable (set by CalculateMutation).</summary>
public sealed class VariableRef(string variableName) : IExpression
{
    public string VariableName { get; } = variableName;
}

/// <summary>Read an ambient resource value at self or target location.</summary>
public sealed class LocationResourcePath(string entityRef, ResourceType resourceType) : IExpression
{
    public string EntityRef { get; } = entityRef;
    public ResourceType ResourceType { get; } = resourceType;
}

// --- Arithmetic ---

public sealed class Add(IExpression left, IExpression right) : IExpression
{
    public IExpression Left { get; } = left;
    public IExpression Right { get; } = right;
}

public sealed class Subtract(IExpression left, IExpression right) : IExpression
{
    public IExpression Left { get; } = left;
    public IExpression Right { get; } = right;
}

public sealed class Multiply(IExpression left, IExpression right) : IExpression
{
    public IExpression Left { get; } = left;
    public IExpression Right { get; } = right;
}

public sealed class Divide(IExpression left, IExpression right) : IExpression
{
    public IExpression Left { get; } = left;
    public IExpression Right { get; } = right;
}

// --- Built-in functions ---

/// <summary>Hex distance between self and target entity coordinates.</summary>
public sealed class Distance : IExpression
{
}

public sealed class Min(IExpression a, IExpression b) : IExpression
{
    public IExpression A { get; } = a;
    public IExpression B { get; } = b;
}

public sealed class Max(IExpression a, IExpression b) : IExpression
{
    public IExpression A { get; } = a;
    public IExpression B { get; } = b;
}

public sealed class Clamp(IExpression value, IExpression min, IExpression max) : IExpression
{
    public IExpression Value { get; } = value;
    public IExpression Min { get; } = min;
    public IExpression Max { get; } = max;
}

public sealed class Abs(IExpression value) : IExpression
{
    public IExpression Value { get; } = value;
}

/// <summary>
/// Adds random jitter at runtime. Not supported by compiled kernel behavior programs;
/// the compiler raises an error if a Jitter expression is encountered.
/// </summary>
public sealed class Jitter(IExpression value, float variance) : IExpression
{
    public IExpression Value { get; } = value;
    public float Variance { get; } = variance;
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
