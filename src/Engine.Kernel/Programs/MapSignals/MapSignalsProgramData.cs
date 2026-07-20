namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class MapSignalsProgramData : IKernelProgramData
{
    private readonly float[] _terrainBaseCapacities;
    private readonly int[] _kindSignalRadii;
    private readonly float[] _kindCapacityEmissions;
    private readonly float[] _kindSupportEmissions;
    private readonly float[] _kindThreatEmissions;
    private readonly float[] _kindBaseAttractorMasses;
    private readonly float[] _kindCoverageAttractorScales;
    private readonly float[] _kindInfluenceAttractorScales;
    private readonly float[] _kindActivityAttractorScales;
    private readonly int[] _kindAttractorRadii;
    private readonly int[] _kindScalarStarts;
    private readonly int[] _kindScalarCounts;
    private readonly int[] _scalarAttributeIds;
    private readonly float[] _scalarAttractorScales;
    private readonly int[] _relationshipSignalRadii;
    private readonly float[] _relationshipSupportEmissions;
    private readonly float[] _relationshipThreatEmissions;
    private int _scalarCount;

    public MapSignalsProgramData(
        int terrainCapacity,
        int kindCapacity,
        int relationshipKindCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(terrainCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(kindCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(relationshipKindCapacity);
        _terrainBaseCapacities = new float[terrainCapacity + 1];
        _kindSignalRadii = new int[kindCapacity + 1];
        _kindCapacityEmissions = new float[kindCapacity + 1];
        _kindSupportEmissions = new float[kindCapacity + 1];
        _kindThreatEmissions = new float[kindCapacity + 1];
        _kindBaseAttractorMasses = new float[kindCapacity + 1];
        _kindCoverageAttractorScales = new float[kindCapacity + 1];
        _kindInfluenceAttractorScales = new float[kindCapacity + 1];
        _kindActivityAttractorScales = new float[kindCapacity + 1];
        _kindAttractorRadii = new int[kindCapacity + 1];
        _kindScalarStarts = new int[kindCapacity + 1];
        _kindScalarCounts = new int[kindCapacity + 1];
        _scalarAttributeIds = new int[(kindCapacity * 4) + 1];
        _scalarAttractorScales = new float[(kindCapacity * 4) + 1];
        _relationshipSignalRadii = new int[relationshipKindCapacity + 1];
        _relationshipSupportEmissions = new float[relationshipKindCapacity + 1];
        _relationshipThreatEmissions = new float[relationshipKindCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.MapSignals;

    public int SchemaVersion => 2;

    public float SemanticBaseCapacity { get; set; } = 2f;

    public int CrowdingRadius { get; set; } = 1;

    public void RegisterTerrainCapacity(int terrainId, float baseCapacity)
    {
        ValidateIndex(terrainId, _terrainBaseCapacities.Length);
        _terrainBaseCapacities[terrainId] = baseCapacity < 0f ? 0f : baseCapacity;
    }

    // Physical carrying capacity derives from the hex's real facts through
    // one authored formula; terrain rows serve only domains (and the
    // semantic plane) that never configure it.

    public bool HasCapacityFormula { get; private set; }

    public float CapacityColdFloor { get; private set; }

    public float CapacityWarmPeak { get; private set; }

    public float CapacityGeothermalBlend { get; private set; }

    public float CapacityFreshwaterWeight { get; private set; }

    public float CapacityFertileWeight { get; private set; }

    public float CapacityReliefComfort { get; private set; }

    public float CapacityReliefPenalty { get; private set; }

    public float CapacityRoughnessPenalty { get; private set; }

    public float CapacityLandScale { get; private set; }

    public float CapacityMarineScale { get; private set; }

    public void SetCapacityFormula(
        float coldFloor,
        float warmPeak,
        float geothermalBlend,
        float freshwaterWeight,
        float fertileWeight,
        float reliefComfort,
        float reliefPenalty,
        float roughnessPenalty,
        float landScale,
        float marineScale)
    {
        if (coldFloor < 0f || warmPeak <= coldFloor || landScale < 0f || marineScale < 0f)
        {
            throw new global::System.InvalidOperationException();
        }

        HasCapacityFormula = true;
        CapacityColdFloor = coldFloor;
        CapacityWarmPeak = warmPeak;
        CapacityGeothermalBlend = geothermalBlend;
        CapacityFreshwaterWeight = freshwaterWeight;
        CapacityFertileWeight = fertileWeight;
        CapacityReliefComfort = reliefComfort;
        CapacityReliefPenalty = reliefPenalty;
        CapacityRoughnessPenalty = roughnessPenalty;
        CapacityLandScale = landScale;
        CapacityMarineScale = marineScale;
    }

    public void RegisterKindSignal(
        int kindId,
        int signalRadius,
        float capacityEmission,
        float supportEmission,
        float threatEmission,
        float baseAttractorMass,
        float coverageAttractorScale,
        float influenceAttractorScale,
        float activityAttractorScale,
        int attractorRadius)
    {
        ValidateIndex(kindId, _kindSignalRadii.Length);
        _kindSignalRadii[kindId] = global::System.Math.Max(0, signalRadius);
        _kindCapacityEmissions[kindId] = capacityEmission;
        _kindSupportEmissions[kindId] = supportEmission;
        _kindThreatEmissions[kindId] = threatEmission;
        _kindBaseAttractorMasses[kindId] = baseAttractorMass;
        _kindCoverageAttractorScales[kindId] = coverageAttractorScale;
        _kindInfluenceAttractorScales[kindId] = influenceAttractorScale;
        _kindActivityAttractorScales[kindId] = activityAttractorScale;
        _kindAttractorRadii[kindId] = global::System.Math.Max(0, attractorRadius);
    }

    public void AddKindAttractorScalar(int kindId, int attributeId, float attractorScale)
    {
        ValidateIndex(kindId, _kindSignalRadii.Length);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(attributeId);
        if (_scalarCount + 1 >= _scalarAttributeIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        if (_kindScalarCounts[kindId] == 0)
        {
            _kindScalarStarts[kindId] = _scalarCount + 1;
        }

        _scalarCount++;
        _scalarAttributeIds[_scalarCount] = attributeId;
        _scalarAttractorScales[_scalarCount] = attractorScale;
        _kindScalarCounts[kindId]++;
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

    public int KindSignalRadius(int kindId) => ValueAt(_kindSignalRadii, kindId);

    public float KindCapacityEmission(int kindId) => ValueAt(_kindCapacityEmissions, kindId);

    public float KindSupportEmission(int kindId) => ValueAt(_kindSupportEmissions, kindId);

    public float KindThreatEmission(int kindId) => ValueAt(_kindThreatEmissions, kindId);

    public float KindBaseAttractorMass(int kindId) => ValueAt(_kindBaseAttractorMasses, kindId);

    public float KindCoverageAttractorScale(int kindId) => ValueAt(_kindCoverageAttractorScales, kindId);

    public float KindInfluenceAttractorScale(int kindId) => ValueAt(_kindInfluenceAttractorScales, kindId);

    public float KindActivityAttractorScale(int kindId) => ValueAt(_kindActivityAttractorScales, kindId);

    public int KindAttractorRadius(int kindId) => ValueAt(_kindAttractorRadii, kindId);

    public int KindScalarStart(int kindId) => ValueAt(_kindScalarStarts, kindId);

    public int KindScalarCount(int kindId) => ValueAt(_kindScalarCounts, kindId);

    public int ScalarAttributeId(int scalar) => ValueAt(_scalarAttributeIds, scalar);

    public float ScalarAttractorScale(int scalar) => ValueAt(_scalarAttractorScales, scalar);

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
