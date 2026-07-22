namespace Engine.Kernel;

/// <summary>
/// The world's named geography, installed once from the blueprint: discrete
/// features (river systems, lakes, and later peaks, ranges, craters) with a
/// kind, an anchor hex, a magnitude, and a hex extent. Generators place
/// discoveries at revealed anchors; the entity standing on the anchor is
/// the durable claim, so no separate claim state exists here.
/// </summary>
internal sealed class KernelGeologicalFeatureData
{
    private readonly int[] _kinds;
    private readonly int[] _anchorQ;
    private readonly int[] _anchorR;
    private readonly int[] _magnitudes;
    private readonly int[] _hexCounts;

    public KernelGeologicalFeatureData(int capacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        _kinds = new int[capacity];
        _anchorQ = new int[capacity];
        _anchorR = new int[capacity];
        _magnitudes = new int[capacity];
        _hexCounts = new int[capacity];
    }

    public int Count { get; private set; }

    public int Capacity => _kinds.Length;

    public void Install(int kind, int anchorQ, int anchorR, int magnitude, int hexCount)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(kind);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(magnitude);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(hexCount);
        if (Count >= _kinds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _kinds[Count] = kind;
        _anchorQ[Count] = anchorQ;
        _anchorR[Count] = anchorR;
        _magnitudes[Count] = magnitude;
        _hexCounts[Count] = hexCount;
        Count++;
    }

    public int Kind(int index) => _kinds[index];

    public int AnchorQ(int index) => _anchorQ[index];

    public int AnchorR(int index) => _anchorR[index];

    public int Magnitude(int index) => _magnitudes[index];

    public int HexCount(int index) => _hexCounts[index];
}
