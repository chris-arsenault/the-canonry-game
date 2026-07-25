namespace Engine.Kernel;

internal sealed class LocalityData
{
    private readonly FixedScalar[] _terrainBaseCapacities;
    private readonly int[] _kindLocalityRadii;
    private readonly FixedScalar[] _kindCapacityEmissions;
    private readonly FixedScalar[] _kindSupportEmissions;
    private readonly FixedScalar[] _kindThreatEmissions;
    private readonly FixedScalar[] _kindBaseAttractorMasses;
    private readonly FixedScalar[] _kindCoverageAttractorScales;
    private readonly FixedScalar[] _kindInfluenceAttractorScales;
    private readonly int[] _kindAttractorRadii;
    private readonly int[] _kindScalarStarts;
    private readonly int[] _kindScalarCounts;
    private readonly int[] _scalarAttributeIds;
    private readonly FixedScalar[] _scalarAttractorScales;
    private readonly FixedScalar[] _relationshipSupportEmissions;
    private readonly FixedScalar[] _relationshipThreatEmissions;
    private readonly int[] _relationshipKindIds;
    private int _scalarCount;
    private int _relationshipKindCount;

    public LocalityData(
        int terrainCapacity,
        int kindCapacity,
        int relationshipKindCapacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(terrainCapacity);
        NumericGuard.ThrowIfNegativeOrZero(kindCapacity);
        NumericGuard.ThrowIfNegativeOrZero(relationshipKindCapacity);
        _terrainBaseCapacities = new FixedScalar[terrainCapacity + 1];
        _kindLocalityRadii = new int[kindCapacity + 1];
        _kindCapacityEmissions = new FixedScalar[kindCapacity + 1];
        _kindSupportEmissions = new FixedScalar[kindCapacity + 1];
        _kindThreatEmissions = new FixedScalar[kindCapacity + 1];
        _kindBaseAttractorMasses = new FixedScalar[kindCapacity + 1];
        _kindCoverageAttractorScales = new FixedScalar[kindCapacity + 1];
        _kindInfluenceAttractorScales = new FixedScalar[kindCapacity + 1];
        _kindAttractorRadii = new int[kindCapacity + 1];
        _kindScalarStarts = new int[kindCapacity + 1];
        _kindScalarCounts = new int[kindCapacity + 1];
        _scalarAttributeIds = new int[(kindCapacity * 4) + 1];
        _scalarAttractorScales = new FixedScalar[(kindCapacity * 4) + 1];
        _relationshipSupportEmissions = new FixedScalar[relationshipKindCapacity + 1];
        _relationshipThreatEmissions = new FixedScalar[relationshipKindCapacity + 1];
        _relationshipKindIds = new int[relationshipKindCapacity + 1];
    }

    public FixedScalar SemanticBaseCapacity { get; set; } = 2;

    public int CrowdingRadius { get; set; } = 1;

    public int MaxKindLocalityRadius { get; private set; }

    public int MaxAttractorRadius { get; private set; }

    public void RegisterTerrainCapacity(int terrainId, FixedScalar baseCapacity)
    {
        ValidateIndex(terrainId, _terrainBaseCapacities.Length);
        _terrainBaseCapacities[terrainId] = baseCapacity < 0 ? 0 : baseCapacity;
    }

    // Physical carrying capacity derives from the hex's real facts through
    // one authored formula; terrain rows serve only domains (and the
    // semantic plane) that never configure it.

    public bool HasCapacityFormula { get; private set; }

    public FixedScalar CapacityColdFloor { get; private set; }

    public FixedScalar CapacityWarmPeak { get; private set; }

    public FixedScalar CapacityGeothermalBlend { get; private set; }

    public FixedScalar CapacityFreshwaterWeight { get; private set; }

    public FixedScalar CapacityFertileWeight { get; private set; }

    public FixedScalar CapacityReliefComfort { get; private set; }

    public FixedScalar CapacityReliefPenalty { get; private set; }

    public FixedScalar CapacityRoughnessPenalty { get; private set; }

    public FixedScalar CapacityLandScale { get; private set; }

    public FixedScalar CapacityMarineScale { get; private set; }

    public void SetCapacityFormula(
        FixedScalar coldFloor,
        FixedScalar warmPeak,
        FixedScalar geothermalBlend,
        FixedScalar freshwaterWeight,
        FixedScalar fertileWeight,
        FixedScalar reliefComfort,
        FixedScalar reliefPenalty,
        FixedScalar roughnessPenalty,
        FixedScalar landScale,
        FixedScalar marineScale)
    {
        if (coldFloor < 0 || warmPeak <= coldFloor || landScale < 0 || marineScale < 0)
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

    public void RegisterKindLocality(
        int kindId,
        int localityRadius,
        FixedScalar capacityEmission,
        FixedScalar supportEmission,
        FixedScalar threatEmission,
        FixedScalar baseAttractorMass,
        FixedScalar coverageAttractorScale,
        FixedScalar influenceAttractorScale,
        int attractorRadius)
    {
        ValidateIndex(kindId, _kindLocalityRadii.Length);
        _kindLocalityRadii[kindId] = global::System.Math.Max(0, localityRadius);
        _kindCapacityEmissions[kindId] = capacityEmission;
        _kindSupportEmissions[kindId] = supportEmission;
        _kindThreatEmissions[kindId] = threatEmission;
        _kindBaseAttractorMasses[kindId] = baseAttractorMass;
        _kindCoverageAttractorScales[kindId] = coverageAttractorScale;
        _kindInfluenceAttractorScales[kindId] = influenceAttractorScale;
        _kindAttractorRadii[kindId] = global::System.Math.Max(0, attractorRadius);
        MaxKindLocalityRadius = global::System.Math.Max(MaxKindLocalityRadius, _kindLocalityRadii[kindId]);
        MaxAttractorRadius = global::System.Math.Max(MaxAttractorRadius, _kindAttractorRadii[kindId]);
    }

    public void AddKindAttractorScalar(int kindId, int attributeId, FixedScalar attractorScale)
    {
        ValidateIndex(kindId, _kindLocalityRadii.Length);
        NumericGuard.ThrowIfNegativeOrZero(attributeId);
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

    public void RegisterRelationshipLocality(
        int relationshipKindId,
        FixedScalar supportEmission,
        FixedScalar threatEmission)
    {
        ValidateIndex(relationshipKindId, _relationshipSupportEmissions.Length);
        _relationshipSupportEmissions[relationshipKindId] = supportEmission;
        _relationshipThreatEmissions[relationshipKindId] = threatEmission;
        if (supportEmission == 0 && threatEmission == 0)
        {
            return;
        }

        _relationshipKindCount++;
        _relationshipKindIds[_relationshipKindCount] = relationshipKindId;
    }

    public FixedScalar TerrainBaseCapacity(int terrainId)
    {
        if ((uint)terrainId >= (uint)_terrainBaseCapacities.Length)
        {
            return 0;
        }

        return _terrainBaseCapacities[terrainId];
    }

    public int KindLocalityRadius(int kindId) => ValueAt(_kindLocalityRadii, kindId);

    public FixedScalar KindCapacityEmission(int kindId) => ValueAt(_kindCapacityEmissions, kindId);

    public FixedScalar KindSupportEmission(int kindId) => ValueAt(_kindSupportEmissions, kindId);

    public FixedScalar KindThreatEmission(int kindId) => ValueAt(_kindThreatEmissions, kindId);

    public FixedScalar KindBaseAttractorMass(int kindId) => ValueAt(_kindBaseAttractorMasses, kindId);

    public FixedScalar KindCoverageAttractorScale(int kindId) => ValueAt(_kindCoverageAttractorScales, kindId);

    public FixedScalar KindInfluenceAttractorScale(int kindId) => ValueAt(_kindInfluenceAttractorScales, kindId);

    public int KindAttractorRadius(int kindId) => ValueAt(_kindAttractorRadii, kindId);

    public int KindScalarStart(int kindId) => ValueAt(_kindScalarStarts, kindId);

    public int KindScalarCount(int kindId) => ValueAt(_kindScalarCounts, kindId);

    public int ScalarAttributeId(int scalar) => ValueAt(_scalarAttributeIds, scalar);

    public FixedScalar ScalarAttractorScale(int scalar) => ValueAt(_scalarAttractorScales, scalar);

    public FixedScalar RelationshipSupportEmission(int relationshipKindId) => ValueAt(_relationshipSupportEmissions, relationshipKindId);

    public FixedScalar RelationshipThreatEmission(int relationshipKindId) => ValueAt(_relationshipThreatEmissions, relationshipKindId);

    public int RelationshipKindCount => _relationshipKindCount;

    public int RelationshipKindId(int index) => ValueAt(_relationshipKindIds, index);

    private static int ValueAt(int[] values, int index) =>
        (uint)index < (uint)values.Length ? values[index] : 0;

    private static FixedScalar ValueAt(FixedScalar[] values, int index) =>
        (uint)index < (uint)values.Length ? values[index] : 0;

    private static void ValidateIndex(int value, int exclusiveMaximum)
    {
        NumericGuard.ThrowIfNegativeOrZero(value);
        if ((uint)value >= (uint)exclusiveMaximum)
        {
            throw new global::System.InvalidOperationException();
        }
    }
}
