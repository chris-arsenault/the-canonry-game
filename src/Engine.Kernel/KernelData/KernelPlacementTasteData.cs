namespace Engine.Kernel;

/// <summary>
/// Signed placement taste weights over the hex-fact axes, authored per kind
/// and per culture and summed at scoring time. Taste is how identity reads
/// geography: a kind states its functional needs once, a culture states its
/// relationship to the world once, and every placement that kind or culture
/// makes follows from those statements instead of hand-ordered terrain
/// lists. Configured once at install.
/// </summary>
internal sealed class KernelPlacementTasteData
{
    private readonly FixedScalar[] _kindWeights;
    private readonly FixedScalar[] _cultureWeights;

    public KernelPlacementTasteData(int kindIdCapacity, int cultureIdCapacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(kindIdCapacity);
        NumericGuard.ThrowIfNegativeOrZero(cultureIdCapacity);
        _kindWeights = new FixedScalar[checked((kindIdCapacity + 1) * PlacementTasteAxisCodes.Count)];
        _cultureWeights = new FixedScalar[checked((cultureIdCapacity + 1) * PlacementTasteAxisCodes.Count)];
    }

    public void RegisterKindWeight(int kindId, int axisCode, FixedScalar weight) =>
        Register(_kindWeights, kindId, axisCode, weight);

    public void RegisterCultureWeight(int cultureId, int axisCode, FixedScalar weight) =>
        Register(_cultureWeights, cultureId, axisCode, weight);

    public FixedScalar KindWeight(int kindId, int axisCode) =>
        WeightAt(_kindWeights, kindId, axisCode);

    public FixedScalar CultureWeight(int cultureId, int axisCode) =>
        WeightAt(_cultureWeights, cultureId, axisCode);

    /// <summary>The summed taste of a kind placed under a culture (0 ids read as flat).</summary>
    public FixedScalar CombinedWeight(int kindId, int cultureId, int axisCode) =>
        WeightAt(_kindWeights, kindId, axisCode) + WeightAt(_cultureWeights, cultureId, axisCode);

    internal void WriteSnapshot(global::System.IO.BinaryWriter writer) =>
        KernelSnapshotBinary.WriteObject(writer, this);

    internal void ReadSnapshot(global::System.IO.BinaryReader reader) =>
        KernelSnapshotBinary.ReadObject(reader, this);

    private static void Register(FixedScalar[] weights, int id, int axisCode, FixedScalar weight)
    {
        NumericGuard.ThrowIfNegativeOrZero(id);
        if ((uint)axisCode >= PlacementTasteAxisCodes.Count ||
            (id * PlacementTasteAxisCodes.Count) + axisCode >= weights.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        weights[(id * PlacementTasteAxisCodes.Count) + axisCode] = weight;
    }

    private static FixedScalar WeightAt(FixedScalar[] weights, int id, int axisCode)
    {
        var index = (id * PlacementTasteAxisCodes.Count) + axisCode;
        return id > 0 && (uint)index < (uint)weights.Length ? weights[index] : 0;
    }
}
