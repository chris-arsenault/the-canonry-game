namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class MapSignalsProgramData : IKernelProgramData
{
    private readonly float[] _terrainBaseCapacities;
    private readonly int[] _kindHealthAttributeIds;
    private readonly int[] _kindSignalRadii;
    private readonly float[] _kindCapacityEmissions;
    private readonly float[] _kindSupportEmissions;
    private readonly float[] _kindThreatEmissions;
    private readonly float[] _kindBaseAttractorMasses;
    private readonly float[] _kindCoverageAttractorScales;
    private readonly float[] _kindInfluenceAttractorScales;
    private readonly float[] _kindActivityAttractorScales;
    private readonly float[] _kindHealthAttractorScales;
    private readonly int[] _kindAttractorRadii;
    private readonly int[] _relationshipSignalRadii;
    private readonly float[] _relationshipSupportEmissions;
    private readonly float[] _relationshipThreatEmissions;

    public MapSignalsProgramData(
        int terrainCapacity,
        int kindCapacity,
        int relationshipKindCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(terrainCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(kindCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(relationshipKindCapacity);
        _terrainBaseCapacities = new float[terrainCapacity + 1];
        _kindHealthAttributeIds = new int[kindCapacity + 1];
        _kindSignalRadii = new int[kindCapacity + 1];
        _kindCapacityEmissions = new float[kindCapacity + 1];
        _kindSupportEmissions = new float[kindCapacity + 1];
        _kindThreatEmissions = new float[kindCapacity + 1];
        _kindBaseAttractorMasses = new float[kindCapacity + 1];
        _kindCoverageAttractorScales = new float[kindCapacity + 1];
        _kindInfluenceAttractorScales = new float[kindCapacity + 1];
        _kindActivityAttractorScales = new float[kindCapacity + 1];
        _kindHealthAttractorScales = new float[kindCapacity + 1];
        _kindAttractorRadii = new int[kindCapacity + 1];
        _relationshipSignalRadii = new int[relationshipKindCapacity + 1];
        _relationshipSupportEmissions = new float[relationshipKindCapacity + 1];
        _relationshipThreatEmissions = new float[relationshipKindCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.MapSignals;

    public int SchemaVersion => 1;

    public float SemanticBaseCapacity { get; set; } = 2f;

    public int CrowdingRadius { get; set; } = 1;

    public void RegisterTerrainCapacity(int terrainId, float baseCapacity)
    {
        ValidateIndex(terrainId, _terrainBaseCapacities.Length);
        _terrainBaseCapacities[terrainId] = baseCapacity < 0f ? 0f : baseCapacity;
    }

    public void RegisterKindSignal(
        int kindId,
        int healthAttributeId,
        int signalRadius,
        float capacityEmission,
        float supportEmission,
        float threatEmission,
        float baseAttractorMass,
        float coverageAttractorScale,
        float influenceAttractorScale,
        float activityAttractorScale,
        float healthAttractorScale,
        int attractorRadius)
    {
        ValidateIndex(kindId, _kindSignalRadii.Length);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(healthAttributeId);
        _kindHealthAttributeIds[kindId] = healthAttributeId;
        _kindSignalRadii[kindId] = global::System.Math.Max(0, signalRadius);
        _kindCapacityEmissions[kindId] = capacityEmission;
        _kindSupportEmissions[kindId] = supportEmission;
        _kindThreatEmissions[kindId] = threatEmission;
        _kindBaseAttractorMasses[kindId] = baseAttractorMass;
        _kindCoverageAttractorScales[kindId] = coverageAttractorScale;
        _kindInfluenceAttractorScales[kindId] = influenceAttractorScale;
        _kindActivityAttractorScales[kindId] = activityAttractorScale;
        _kindHealthAttractorScales[kindId] = healthAttractorScale;
        _kindAttractorRadii[kindId] = global::System.Math.Max(0, attractorRadius);
    }

    public void RegisterRelationshipSignal(
        int relationshipKindId,
        int signalRadius,
        float supportEmission,
        float threatEmission)
    {
        ValidateIndex(relationshipKindId, _relationshipSignalRadii.Length);
        _relationshipSignalRadii[relationshipKindId] = global::System.Math.Max(0, signalRadius);
        _relationshipSupportEmissions[relationshipKindId] = supportEmission;
        _relationshipThreatEmissions[relationshipKindId] = threatEmission;
    }

    public float TerrainBaseCapacity(int terrainId)
    {
        if ((uint)terrainId >= (uint)_terrainBaseCapacities.Length)
        {
            return 0f;
        }

        return _terrainBaseCapacities[terrainId];
    }

    public int KindHealthAttributeId(int kindId) => ValueAt(_kindHealthAttributeIds, kindId);

    public int KindSignalRadius(int kindId) => ValueAt(_kindSignalRadii, kindId);

    public float KindCapacityEmission(int kindId) => ValueAt(_kindCapacityEmissions, kindId);

    public float KindSupportEmission(int kindId) => ValueAt(_kindSupportEmissions, kindId);

    public float KindThreatEmission(int kindId) => ValueAt(_kindThreatEmissions, kindId);

    public float KindBaseAttractorMass(int kindId) => ValueAt(_kindBaseAttractorMasses, kindId);

    public float KindCoverageAttractorScale(int kindId) => ValueAt(_kindCoverageAttractorScales, kindId);

    public float KindInfluenceAttractorScale(int kindId) => ValueAt(_kindInfluenceAttractorScales, kindId);

    public float KindActivityAttractorScale(int kindId) => ValueAt(_kindActivityAttractorScales, kindId);

    public float KindHealthAttractorScale(int kindId) => ValueAt(_kindHealthAttractorScales, kindId);

    public int KindAttractorRadius(int kindId) => ValueAt(_kindAttractorRadii, kindId);

    public int RelationshipSignalRadius(int relationshipKindId) => ValueAt(_relationshipSignalRadii, relationshipKindId);

    public float RelationshipSupportEmission(int relationshipKindId) => ValueAt(_relationshipSupportEmissions, relationshipKindId);

    public float RelationshipThreatEmission(int relationshipKindId) => ValueAt(_relationshipThreatEmissions, relationshipKindId);

    private static int ValueAt(int[] values, int index) =>
        (uint)index < (uint)values.Length ? values[index] : 0;

    private static float ValueAt(float[] values, int index) =>
        (uint)index < (uint)values.Length ? values[index] : 0f;

    private static void ValidateIndex(int value, int exclusiveMaximum)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
        if ((uint)value >= (uint)exclusiveMaximum)
        {
            throw new global::System.InvalidOperationException();
        }
    }
}
