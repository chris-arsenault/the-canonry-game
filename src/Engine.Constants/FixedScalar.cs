namespace Engine.Constants;

/// <summary>
/// Deterministic signed fixed-point scalar for dimensionless runtime values:
/// probabilities, weights, scores, ratios, heat, and path costs. Semantic
/// stocks such as attributes, culture, relationships, and roads retain their
/// narrower field-specific integer contracts.
/// </summary>
public readonly struct FixedScalar :
    global::System.IComparable,
    global::System.IComparable<FixedScalar>,
    global::System.IEquatable<FixedScalar>
{
    public const long Scale = 1_000_000L;
    public static readonly FixedScalar Zero = new(0L);
    public static readonly FixedScalar One = new(Scale);
    public static readonly FixedScalar Epsilon = new(1L);
    public static readonly FixedScalar PositiveInfinity = new(long.MaxValue);
    public static readonly FixedScalar MaxValue = new(long.MaxValue - 1);
    public static readonly FixedScalar MinValue = new(long.MinValue + 1);
    private static readonly FixedScalar Ln2 = new(693_147L);

    private FixedScalar(long units)
    {
        Units = units;
    }

    public long Units { get; }

    public static FixedScalar FromUnits(long units) => new(units);

    public static FixedScalar FromInt(int value) => new(checked(value * Scale));

    public static FixedScalar FromRatio(long numerator, long denominator)
    {
        if (denominator == 0)
        {
            throw new global::System.DivideByZeroException();
        }

        return FromWide(DivideAwayFromZero((global::System.Int128)numerator * Scale, denominator));
    }

    public static FixedScalar FromSingle(float value)
    {
        if (!float.IsFinite(value))
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(value));
        }

        var scaled = global::System.Math.Round(
            (double)value * Scale,
            global::System.MidpointRounding.AwayFromZero);
        if (scaled <= long.MinValue || scaled >= long.MaxValue)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(value));
        }

        return new FixedScalar((long)scaled);
    }

    public static FixedScalar FromDouble(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(value));
        }

        var scaled = global::System.Math.Round(
            value * Scale,
            global::System.MidpointRounding.AwayFromZero);
        if (scaled <= long.MinValue || scaled >= long.MaxValue)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(value));
        }

        return new FixedScalar((long)scaled);
    }

    public float ToSingle() => (float)Units / Scale;

    public double ToDouble() => (double)Units / Scale;

    public int RoundToInt32()
    {
        var rounded = DivideAwayFromZero(Units, Scale);
        return checked((int)rounded);
    }

    public int TruncateToInt32() => checked((int)(Units / Scale));

    public static FixedScalar Abs(FixedScalar value)
    {
        if (value.Units == long.MinValue)
        {
            return MaxValue;
        }

        return value.Units < 0 ? new FixedScalar(-value.Units) : value;
    }

    public static FixedScalar Min(FixedScalar left, FixedScalar right) =>
        left.Units <= right.Units ? left : right;

    public static FixedScalar Max(FixedScalar left, FixedScalar right) =>
        left.Units >= right.Units ? left : right;

    public static FixedScalar Clamp(FixedScalar value, FixedScalar minimum, FixedScalar maximum)
    {
        if (minimum > maximum)
        {
            throw new global::System.ArgumentException(
                "Minimum must not exceed maximum.",
                nameof(minimum));
        }

        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : value;
    }

    public static FixedScalar Round(FixedScalar value) =>
        FromInt(value.RoundToInt32());

    public static FixedScalar Round(
        FixedScalar value,
        global::System.MidpointRounding mode)
    {
        if (mode != global::System.MidpointRounding.AwayFromZero)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(mode));
        }

        return Round(value);
    }

    public static FixedScalar Floor(FixedScalar value)
    {
        var quotient = value.Units / Scale;
        if (value.Units < 0 && value.Units % Scale != 0)
        {
            quotient--;
        }

        return FromWide((global::System.Int128)quotient * Scale);
    }

    public static FixedScalar Ceiling(FixedScalar value)
    {
        var quotient = value.Units / Scale;
        if (value.Units > 0 && value.Units % Scale != 0)
        {
            quotient++;
        }

        return FromWide((global::System.Int128)quotient * Scale);
    }

    public static FixedScalar Exp(FixedScalar value)
    {
        if (value <= FromInt(-20))
        {
            return Zero;
        }

        if (value >= FromInt(20))
        {
            return MaxValue;
        }

        var exponent = Floor(value / Ln2).TruncateToInt32();
        var remainder = value - (Ln2 * exponent);
        var term = One;
        var sum = One;
        for (var divisor = 1; divisor <= 14; divisor++)
        {
            term = (term * remainder) / divisor;
            sum += term;
        }

        if (exponent >= 0)
        {
            return exponent >= 62
                ? MaxValue
                : FromWide((global::System.Int128)sum.Units << exponent);
        }

        return new FixedScalar(sum.Units >> -exponent);
    }

    public static FixedScalar Pow(FixedScalar value, FixedScalar exponent)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfLessThan(value, Zero);

        if (value == Zero)
        {
            return exponent == Zero ? One : Zero;
        }

        return Exp(Log(value) * exponent);
    }

    public static bool IsInfinity(FixedScalar value) => value == PositiveInfinity;

    public static bool IsPositiveInfinity(FixedScalar value) => value == PositiveInfinity;

    public static bool IsFinite(FixedScalar value) => value != PositiveInfinity;

    public static bool IsNaN(FixedScalar value)
    {
        _ = value;
        return false;
    }

    public int CompareTo(FixedScalar other) => Units.CompareTo(other.Units);

    int global::System.IComparable.CompareTo(object? obj) =>
        obj is FixedScalar other
            ? CompareTo(other)
            : throw new global::System.ArgumentException(
                "Object must be a FixedScalar.",
                nameof(obj));

    public bool Equals(FixedScalar other) => Units == other.Units;

    public override bool Equals(object? obj) => obj is FixedScalar other && Equals(other);

    public override int GetHashCode() => Units.GetHashCode();

    public static implicit operator FixedScalar(int value) => FromInt(value);

    public static explicit operator int(FixedScalar value) => value.TruncateToInt32();

    public static FixedScalar FromInt32(int value) => FromInt(value);

    public int ToInt32() => TruncateToInt32();

    public static FixedScalar Add(FixedScalar left, FixedScalar right) => left + right;

    public static FixedScalar Subtract(FixedScalar left, FixedScalar right) => left - right;

    public static FixedScalar Negate(FixedScalar value) => -value;

    public static FixedScalar Multiply(FixedScalar left, FixedScalar right) => left * right;

    public static FixedScalar Multiply(FixedScalar left, int right) => left * right;

    public static FixedScalar Multiply(int left, FixedScalar right) => left * right;

    public static FixedScalar Multiply(FixedScalar left, long right) => left * right;

    public static FixedScalar Multiply(long left, FixedScalar right) => left * right;

    public static FixedScalar Divide(FixedScalar left, FixedScalar right) => left / right;

    public static FixedScalar Divide(FixedScalar left, int right) => left / right;

    public static FixedScalar Divide(FixedScalar left, long right) => left / right;

    public static FixedScalar Remainder(FixedScalar left, FixedScalar right) => left % right;

    public static FixedScalar operator +(FixedScalar left, FixedScalar right) =>
        new(SaturatingAdd(left.Units, right.Units));

    public static FixedScalar operator -(FixedScalar left, FixedScalar right) =>
        new(SaturatingAdd(left.Units, SaturatingNegate(right.Units)));

    public static FixedScalar operator -(FixedScalar value) =>
        new(SaturatingNegate(value.Units));

    public static FixedScalar operator *(FixedScalar left, FixedScalar right) =>
        FromWide(DivideAwayFromZero(
            (global::System.Int128)left.Units * right.Units,
            Scale));

    public static FixedScalar operator *(FixedScalar left, int right) =>
        FromWide((global::System.Int128)left.Units * right);

    public static FixedScalar operator *(int left, FixedScalar right) => right * left;

    public static FixedScalar operator *(FixedScalar left, long right) =>
        FromWide((global::System.Int128)left.Units * right);

    public static FixedScalar operator *(long left, FixedScalar right) => right * left;

    public static FixedScalar operator /(FixedScalar left, FixedScalar right)
    {
        if (right.Units == 0)
        {
            throw new global::System.DivideByZeroException();
        }

        return FromWide(DivideAwayFromZero(
            (global::System.Int128)left.Units * Scale,
            right.Units));
    }

    public static FixedScalar operator /(FixedScalar left, int right)
    {
        if (right == 0)
        {
            throw new global::System.DivideByZeroException();
        }

        return FromWide(DivideAwayFromZero(left.Units, right));
    }

    public static FixedScalar operator /(FixedScalar left, long right)
    {
        if (right == 0L)
        {
            throw new global::System.DivideByZeroException();
        }

        return FromWide(DivideAwayFromZero(left.Units, right));
    }

    public static FixedScalar operator %(FixedScalar left, FixedScalar right)
    {
        if (right.Units == 0)
        {
            throw new global::System.DivideByZeroException();
        }

        return new FixedScalar(left.Units % right.Units);
    }

    public static bool operator ==(FixedScalar left, FixedScalar right) =>
        left.Units == right.Units;

    public static bool operator !=(FixedScalar left, FixedScalar right) =>
        left.Units != right.Units;

    public static bool operator <(FixedScalar left, FixedScalar right) =>
        left.Units < right.Units;

    public static bool operator <=(FixedScalar left, FixedScalar right) =>
        left.Units <= right.Units;

    public static bool operator >(FixedScalar left, FixedScalar right) =>
        left.Units > right.Units;

    public static bool operator >=(FixedScalar left, FixedScalar right) =>
        left.Units >= right.Units;

    private static FixedScalar Log(FixedScalar value)
    {
        var units = value.Units;
        var exponent = 0;
        while (units >= 2 * Scale)
        {
            units >>= 1;
            exponent++;
        }

        while (units < Scale)
        {
            units <<= 1;
            exponent--;
        }

        var normalized = new FixedScalar(units);
        var z = (normalized - One) / (normalized + One);
        var zSquared = z * z;
        var power = z;
        var sum = z;
        for (var divisor = 3; divisor <= 29; divisor += 2)
        {
            power *= zSquared;
            sum += power / divisor;
        }

        return (sum * 2) + (Ln2 * exponent);
    }

    private static FixedScalar FromWide(global::System.Int128 units)
    {
        if (units >= long.MaxValue)
        {
            return MaxValue;
        }

        return units <= long.MinValue ? MinValue : new FixedScalar((long)units);
    }

    private static long SaturatingAdd(long left, long right)
    {
        var sum = (global::System.Int128)left + right;
        if (sum >= long.MaxValue)
        {
            return MaxValue.Units;
        }

        return sum <= long.MinValue ? MinValue.Units : (long)sum;
    }

    private static long SaturatingNegate(long value) =>
        value == long.MinValue ? MaxValue.Units : -value;

    private static global::System.Int128 DivideAwayFromZero(
        global::System.Int128 numerator,
        global::System.Int128 denominator)
    {
        var quotient = numerator / denominator;
        var remainder = numerator % denominator;
        if (remainder == 0)
        {
            return quotient;
        }

        var absoluteRemainder = global::System.Int128.Abs(remainder);
        var absoluteDenominator = global::System.Int128.Abs(denominator);
        if (absoluteRemainder * 2 < absoluteDenominator)
        {
            return quotient;
        }

        return quotient + global::System.Int128.Sign(numerator) *
            global::System.Int128.Sign(denominator);
    }
}
