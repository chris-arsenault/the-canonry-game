namespace Engine.Kernel.Driver;

internal sealed class RealtimeTickAccumulator
{
    private readonly int _maxTicksPerSlice;
    private double _accumulator;

    public RealtimeTickAccumulator(int maxTicksPerSlice)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxTicksPerSlice);
        _maxTicksPerSlice = maxTicksPerSlice;
    }

    public int OverrunCount { get; private set; }

    public int ConsumeTicks(TimeSpan elapsed, double ticksPerSecond)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ticksPerSecond);

        _accumulator += elapsed.TotalSeconds * ticksPerSecond;
        var ticks = (int)_accumulator;
        if (ticks <= 0)
        {
            return 0;
        }

        if (ticks > _maxTicksPerSlice)
        {
            OverrunCount++;
            _accumulator = 0d;
            return _maxTicksPerSlice;
        }

        _accumulator -= ticks;
        return ticks;
    }
}
