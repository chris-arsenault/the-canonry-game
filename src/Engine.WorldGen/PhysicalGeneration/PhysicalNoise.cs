namespace Engine.WorldGen;

public static class PhysicalNoise
{
    public static double UnitNoise(double x, double y, int salt, double scale, int octaves) =>
        (FractalNoise(x, y, salt, scale, octaves) + 1d) * 0.5d;

    public static double FractalNoise(double x, double y, int salt, double scale, int octaves)
    {
        var amplitude = 1d;
        var amplitudeTotal = 0d;
        var value = 0d;
        for (var octave = 0; octave < octaves; octave++)
        {
            value += ValueNoise(x / scale, y / scale, salt + (octave * 7919)) * amplitude;
            amplitudeTotal += amplitude;
            amplitude *= 0.5d;
            scale *= 0.5d;
        }

        return value / amplitudeTotal;
    }

    public static double HashSigned(int salt, int x, int y) => (HashUnit(salt, x, y) * 2d) - 1d;

    public static double HashUnit(int salt, int x, int y)
    {
        unchecked
        {
            var hash = salt;
            hash = (hash * 397) ^ x;
            hash = (hash * 397) ^ y;
            hash ^= hash >> 13;
            hash *= 16777619;
            return (hash & 0x7fffffff) / (double)int.MaxValue;
        }
    }

    private static double ValueNoise(double x, double y, int salt)
    {
        var x0 = (int)Math.Floor(x);
        var y0 = (int)Math.Floor(y);
        var tx = Smooth(x - x0);
        var ty = Smooth(y - y0);
        var a = Lerp(HashSigned(salt, x0, y0), HashSigned(salt, x0 + 1, y0), tx);
        var b = Lerp(HashSigned(salt, x0, y0 + 1), HashSigned(salt, x0 + 1, y0 + 1), tx);
        return Lerp(a, b, ty);
    }

    private static double Smooth(double value) => value * value * (3d - (2d * value));

    private static double Lerp(double first, double second, double amount) =>
        first + ((second - first) * amount);
}
