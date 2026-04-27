namespace Engine.Kernel;

using Engine.Kernel.Programs;

public sealed partial class DegeneracyProgramData : IKernelProgramData
{
    internal readonly int[] DetectorIds;
    internal readonly int[] MetricCodes;
    internal readonly int[] ThresholdDirectionCodes;
    internal readonly int[] NoSubjectsSeverityCodes;
    internal readonly int[] ReasonCodeIds;
    internal readonly int[] PrimaryKindIds;
    internal readonly int[] SecondaryKindIds;
    internal readonly int[] AttributeIds;
    internal readonly int[] RelationshipKindIds;
    internal readonly int[] DominantRelationshipEndpointCodes;
    internal readonly float[] WarningThresholds;
    internal readonly float[] CriticalThresholds;
    internal int DetectorCount;

    public DegeneracyProgramData(int detectorCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(detectorCapacity);
        DetectorIds = new int[detectorCapacity + 1];
        MetricCodes = new int[detectorCapacity + 1];
        ThresholdDirectionCodes = new int[detectorCapacity + 1];
        NoSubjectsSeverityCodes = new int[detectorCapacity + 1];
        ReasonCodeIds = new int[detectorCapacity + 1];
        PrimaryKindIds = new int[detectorCapacity + 1];
        SecondaryKindIds = new int[detectorCapacity + 1];
        AttributeIds = new int[detectorCapacity + 1];
        RelationshipKindIds = new int[detectorCapacity + 1];
        DominantRelationshipEndpointCodes = new int[detectorCapacity + 1];
        WarningThresholds = new float[detectorCapacity + 1];
        CriticalThresholds = new float[detectorCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.Degeneracy;

    public int SchemaVersion => 1;
}
