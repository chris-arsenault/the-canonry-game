namespace Engine.Kernel;

internal readonly struct KeyedRandom
{
    private readonly ulong _seed;

    public KeyedRandom(int seed)
    {
        _seed = unchecked((uint)seed);
    }

    public int Int(
        int exclusiveMaximum,
        long tick,
        int phaseCode,
        int programId,
        long logicalKey,
        int ruleId,
        int purposeCode,
        int attempt)
    {
        NumericGuard.ThrowIfNegativeOrZero(exclusiveMaximum);
        return (int)(Value(
            tick,
            phaseCode,
            programId,
            logicalKey,
            ruleId,
            purposeCode,
            attempt) % (uint)exclusiveMaximum);
    }

    public int Inclusive(
        int minimum,
        int maximum,
        long tick,
        int phaseCode,
        int programId,
        long logicalKey,
        int ruleId,
        int purposeCode,
        int attempt)
    {
        NumericGuard.ThrowIfLessThan(maximum, minimum);
        return minimum + Int(
            maximum - minimum + 1,
            tick,
            phaseCode,
            programId,
            logicalKey,
            ruleId,
            purposeCode,
            attempt);
    }

    public FixedScalar Scalar(
        long tick,
        int phaseCode,
        int programId,
        long logicalKey,
        int ruleId,
        int purposeCode,
        int attempt)
    {
        var sample = Value(
            tick,
            phaseCode,
            programId,
            logicalKey,
            ruleId,
            purposeCode,
            attempt) >> 40;
        var units = ((sample * (ulong)FixedScalar.Scale) + 8_388_608UL) >> 24;
        return FixedScalar.FromUnits((long)units);
    }

    private ulong Value(
        long tick,
        int phaseCode,
        int programId,
        long logicalKey,
        int ruleId,
        int purposeCode,
        int attempt)
    {
        unchecked
        {
            var value = _seed ^ 0xD1B54A32D192ED03UL;
            value ^= RotateLeft((ulong)tick * 0x9E3779B185EBCA87UL, 7);
            value ^= RotateLeft((uint)phaseCode * 0xC2B2AE3D27D4EB4FUL, 17);
            value ^= RotateLeft((uint)programId * 0x165667B19E3779F9UL, 29);
            value ^= RotateLeft((ulong)logicalKey * 0x85EBCA77C2B2AE63UL, 41);
            value ^= RotateLeft((uint)ruleId * 0x27D4EB2F165667C5UL, 11);
            value ^= RotateLeft((uint)purposeCode * 0x94D049BB133111EBUL, 23);
            value ^= RotateLeft((uint)attempt * 0xBF58476D1CE4E5B9UL, 37);
            return Mix(value);
        }
    }

    private static ulong RotateLeft(ulong value, int offset) =>
        (value << offset) | (value >> (64 - offset));

    private static ulong Mix(ulong value)
    {
        value += 0x9E3779B97F4A7C15UL;
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }
}
