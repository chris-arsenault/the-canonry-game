namespace Engine.WorldGen;

using Engine.Constants;

/// <summary>
/// Dense generation-time indexing for the canonical 24-triangle physical
/// geography graph. The work disk includes padding; configured hex indexes are
/// the cropped latent world exported to startup data.
/// </summary>
public sealed class PhysicalGeographyGrid
{
    private readonly Dictionary<HexCoord, int> _hexIndexes;
    private readonly HexCoord[] _hexCoords;
    private readonly int[] _hexDistances;
    private readonly int[] _configuredHexIndexes;
    private readonly int[] _triangleNeighbors;
    private readonly float[] _triangleX;
    private readonly float[] _triangleY;

    public PhysicalGeographyGrid(int radius, int padding)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radius);
        ArgumentOutOfRangeException.ThrowIfNegative(padding);
        Radius = radius;
        Padding = padding;
        WorkRadius = checked(radius + padding);
        _hexCoords = HexCoord.CoordsInRadius(new HexCoord(0, 0), WorkRadius).ToArray();
        _hexIndexes = new Dictionary<HexCoord, int>(_hexCoords.Length);
        _hexDistances = new int[_hexCoords.Length];
        var configured = new List<int>(HexCellCount(radius));
        for (var hexIndex = 0; hexIndex < _hexCoords.Length; hexIndex++)
        {
            var coord = _hexCoords[hexIndex];
            _hexIndexes.Add(coord, hexIndex);
            var distance = HexCoord.Distance(new HexCoord(0, 0), coord);
            _hexDistances[hexIndex] = distance;
            if (distance <= radius)
            {
                configured.Add(hexIndex);
            }
        }

        _configuredHexIndexes = configured.ToArray();
        var triangleCount = checked(_hexCoords.Length * PhysicalGeographyCodes.MicrotrianglesPerHex);
        _triangleNeighbors = new int[checked(triangleCount * 3)];
        Array.Fill(_triangleNeighbors, -1);
        _triangleX = new float[triangleCount];
        _triangleY = new float[triangleCount];
        BuildTriangleGeometry();
        BuildTriangleNeighbors();
    }

    public int Radius { get; }

    public int Padding { get; }

    public int WorkRadius { get; }

    public int HexCount => _hexCoords.Length;

    public int ConfiguredHexCount => _configuredHexIndexes.Length;

    public int TriangleCount => _triangleX.Length;

    public HexCoord HexCoordAt(int hexIndex) => _hexCoords[hexIndex];

    public int HexDistanceAt(int hexIndex) => _hexDistances[hexIndex];

    public int ConfiguredHexIndexAt(int configuredIndex) => _configuredHexIndexes[configuredIndex];

    public int TriangleIndex(int hexIndex, int localTriangleIndex) =>
        checked((hexIndex * PhysicalGeographyCodes.MicrotrianglesPerHex) + localTriangleIndex);

    public int TriangleHexIndex(int triangleIndex) =>
        triangleIndex / PhysicalGeographyCodes.MicrotrianglesPerHex;

    public int LocalTriangleIndex(int triangleIndex) =>
        triangleIndex % PhysicalGeographyCodes.MicrotrianglesPerHex;

    public int NeighborTriangleIndex(int triangleIndex, int edge) =>
        _triangleNeighbors[checked((triangleIndex * 3) + edge)];

    public float TriangleX(int triangleIndex) => _triangleX[triangleIndex];

    public float TriangleY(int triangleIndex) => _triangleY[triangleIndex];

    public bool TriangleIsInsideConfiguredDisk(int triangleIndex) =>
        _hexDistances[TriangleHexIndex(triangleIndex)] <= Radius;

    public bool HexIsOnWorkBoundary(int hexIndex) => _hexDistances[hexIndex] == WorkRadius;

    public bool TryGetHexIndex(HexCoord coord, out int hexIndex) =>
        _hexIndexes.TryGetValue(coord, out hexIndex);

    public int EdgeLeadingTo(int triangleIndex, int targetTriangleIndex)
    {
        for (var edge = 0; edge < 3; edge++)
        {
            if (NeighborTriangleIndex(triangleIndex, edge) == targetTriangleIndex)
            {
                return edge;
            }
        }

        return PhysicalGeographyCodes.NoFlowEdge;
    }

    private void BuildTriangleNeighbors()
    {
        for (var hexIndex = 0; hexIndex < _hexCoords.Length; hexIndex++)
        {
            var coord = _hexCoords[hexIndex];
            for (var localTriangle = 0; localTriangle < PhysicalGeographyCodes.MicrotrianglesPerHex; localTriangle++)
            {
                BuildTriangleNeighbors(hexIndex, coord, localTriangle);
            }
        }
    }

    private void BuildTriangleNeighbors(int hexIndex, HexCoord coord, int localTriangle)
    {
        var triangleIndex = TriangleIndex(hexIndex, localTriangle);
        for (var edge = 0; edge < 3; edge++)
        {
            PhysicalMicrotriangleTopology.ResolveNeighbor(
                coord.Q,
                coord.R,
                localTriangle,
                edge,
                out var neighborQ,
                out var neighborR,
                out var neighborLocalTriangle);
            if (_hexIndexes.TryGetValue(new HexCoord(neighborQ, neighborR), out var neighborHexIndex))
            {
                _triangleNeighbors[(triangleIndex * 3) + edge] =
                    TriangleIndex(neighborHexIndex, neighborLocalTriangle);
            }
        }
    }

    private void BuildTriangleGeometry()
    {
        for (var hexIndex = 0; hexIndex < _hexCoords.Length; hexIndex++)
        {
            var coord = _hexCoords[hexIndex];
            var centerX = Math.Sqrt(3d) * (coord.Q + (coord.R * 0.5d));
            var centerY = 1.5d * coord.R;
            for (var sector = 0; sector < PhysicalGeographyCodes.HexSectorCount; sector++)
            {
                WriteSectorCentroids(hexIndex, sector, centerX, centerY);
            }
        }
    }

    private void WriteSectorCentroids(int hexIndex, int sector, double centerX, double centerY)
    {
        var angleA = ((sector * 60d) - 30d) * (Math.PI / 180d);
        var angleB = (((sector + 1) * 60d) - 30d) * (Math.PI / 180d);
        var ax = Math.Cos(angleA);
        var ay = Math.Sin(angleA);
        var bx = Math.Cos(angleB);
        var by = Math.Sin(angleB);
        WriteCentroid(hexIndex, sector, 0, centerX, centerY, (ax + bx) / 6d, (ay + by) / 6d);
        WriteCentroid(hexIndex, sector, 1, centerX, centerY, ((ax * 2d) + (bx * 0.5d)) / 3d, ((ay * 2d) + (by * 0.5d)) / 3d);
        WriteCentroid(hexIndex, sector, 2, centerX, centerY, ((ax * 0.5d) + (bx * 2d)) / 3d, ((ay * 0.5d) + (by * 2d)) / 3d);
        WriteCentroid(hexIndex, sector, 3, centerX, centerY, (ax + bx) / 3d, (ay + by) / 3d);
    }

    private void WriteCentroid(
        int hexIndex,
        int sector,
        int localTriangle,
        double centerX,
        double centerY,
        double offsetX,
        double offsetY)
    {
        var triangleIndex = TriangleIndex(
            hexIndex,
            PhysicalMicrotriangleTopology.TriangleIndex(sector, localTriangle));
        _triangleX[triangleIndex] = (float)(centerX + offsetX);
        _triangleY[triangleIndex] = (float)(centerY + offsetY);
    }

    private static int HexCellCount(int radius) => checked((3 * radius * radius) + (3 * radius) + 1);
}

/// <summary>
/// Whole-field generated physical facts before domain material classification
/// and hex terrain synthesis.
/// </summary>
public sealed class PhysicalGeographyField
{
    private readonly int[] _crustBasePotential;
    private readonly int[] _crustAssemblyIds;
    private readonly int[] _crustAssemblySeparation;
    private readonly int[] _crustMotionX;
    private readonly int[] _crustMotionY;
    private readonly int[] _crustProvinceIds;
    private readonly int[] _crustTypeCodes;
    private readonly int[] _bedrockElevation;
    private readonly int[] _continentalPotential;
    private readonly int[] _cumulativeCoastalErosion;
    private readonly int[] _cumulativeDeposition;
    private readonly int[] _cumulativeFluvialErosion;
    private readonly int[] _cumulativeGlacialErosion;
    private readonly int[] _drainageBasinIds;
    private readonly int[] _drainageElevations;
    private readonly int[] _drainageParents;
    private readonly int[] _drainageRanks;
    private readonly int[] _elevation;
    private readonly int[] _featureFlags;
    private readonly int[] _fractureConductance;
    private readonly int[] _flowAccumulation;
    private readonly int[] _flowEdges;
    private readonly int[] _flowTargets;
    private readonly int[] _geothermal;
    private readonly int[] _heatSourceDensity;
    private readonly int[] _iceFlux;
    private readonly int[] _iceFlowTargets;
    private readonly int[] _iceBalance;
    private readonly int[] _iceThickness;
    private readonly int[] _landmassClassCodes;
    private readonly int[] _landmassIds;
    private readonly int[] _lakeOutletTargets;
    private readonly int[] _landformCodes;
    private readonly int[] _maximumIceFlux;
    private readonly int[] _maximumIceThickness;
    private readonly int[] _moisture;
    private readonly int[] _oceanCurrentHeat;
    private readonly int[] _oceanCurrentStrength;
    private readonly int[] _oceanCurrentX;
    private readonly int[] _oceanCurrentY;
    private readonly int[] _rainfall;
    private readonly float[] _ridgeInfluence;
    private readonly int[] _riverOrders;
    private readonly int[] _runoff;
    private readonly int[] _seaIceSuitability;
    private readonly int[] _sedimentSupply;
    private readonly int[] _sedimentThickness;
    private readonly int[] _snowfall;
    private readonly int[] _slope;
    private readonly int[] _temperature;
    private readonly int[] _tectonicBoundaryCodes;
    private readonly int[] _tectonicStrain;
    private readonly int[] _tectonicActivityHistory;
    private readonly int[] _warmth;
    private readonly int[] _waveExposure;
    private readonly int[] _waveExposureHistory;
    private readonly int[] _continentalGroupIds;
    private readonly int[] _bathymetricZoneCodes;
    private readonly float[] _uplift;
    private readonly int[] _waterBodyIds;
    private readonly int[] _waterDepths;
    private readonly int[] _waterKindCodes;

    internal PhysicalGeographyField(PhysicalGeographyGrid grid)
    {
        Grid = grid;
        (_crustBasePotential, _crustAssemblyIds, _crustAssemblySeparation, _crustMotionX,
            _crustMotionY, _crustProvinceIds, _crustTypeCodes) =
            (new int[grid.TriangleCount], new int[grid.TriangleCount], new int[grid.TriangleCount],
                new int[grid.TriangleCount], new int[grid.TriangleCount], new int[grid.TriangleCount],
                new int[grid.TriangleCount]);
        (_continentalPotential, _elevation, _temperature) =
            (new int[grid.TriangleCount], new int[grid.TriangleCount], new int[grid.TriangleCount]);
        (_tectonicBoundaryCodes, _tectonicStrain) =
            (new int[grid.TriangleCount], new int[grid.TriangleCount]);
        _moisture = new int[grid.TriangleCount];
        (_oceanCurrentX, _oceanCurrentY, _oceanCurrentStrength, _oceanCurrentHeat,
            _waveExposure) =
            (new int[grid.TriangleCount], new int[grid.TriangleCount],
                new int[grid.TriangleCount], new int[grid.TriangleCount],
                new int[grid.TriangleCount]);
        (_rainfall, _snowfall, _seaIceSuitability) =
            (new int[grid.TriangleCount], new int[grid.TriangleCount],
                new int[grid.TriangleCount]);
        (_bedrockElevation, _sedimentThickness, _sedimentSupply, _cumulativeFluvialErosion,
            _cumulativeGlacialErosion, _cumulativeCoastalErosion, _cumulativeDeposition) =
            (NewField(grid), NewField(grid), NewField(grid), NewField(grid), NewField(grid),
                NewField(grid), NewField(grid));
        (_maximumIceThickness, _maximumIceFlux, _tectonicActivityHistory, _waveExposureHistory) =
            (NewField(grid), NewField(grid), NewField(grid), NewField(grid));
        (_geothermal, _slope, _landformCodes, _waterKindCodes, _waterDepths, _waterBodyIds) =
            (new int[grid.TriangleCount], new int[grid.TriangleCount], new int[grid.TriangleCount],
                new int[grid.TriangleCount], new int[grid.TriangleCount], new int[grid.TriangleCount]);
        _drainageBasinIds = new int[grid.TriangleCount];
        _flowEdges = new int[grid.TriangleCount];
        Array.Fill(_flowEdges, PhysicalGeographyCodes.NoFlowEdge);
        _flowAccumulation = new int[grid.TriangleCount];
        _riverOrders = new int[grid.TriangleCount];
        _featureFlags = new int[grid.TriangleCount];
        _landmassIds = new int[grid.TriangleCount];
        _landmassClassCodes = new int[grid.TriangleCount];
        _continentalGroupIds = new int[grid.TriangleCount];
        _bathymetricZoneCodes = new int[grid.TriangleCount];
        _iceThickness = new int[grid.TriangleCount];
        _iceFlux = new int[grid.TriangleCount];
        _iceFlowTargets = new int[grid.TriangleCount];
        Array.Fill(_iceFlowTargets, -1);
        _iceBalance = new int[grid.TriangleCount];
        _fractureConductance = new int[grid.TriangleCount];
        _heatSourceDensity = new int[grid.TriangleCount];
        _drainageElevations = new int[grid.TriangleCount];
        _drainageParents = new int[grid.TriangleCount];
        Array.Fill(_drainageParents, -1);
        _drainageRanks = new int[grid.TriangleCount];
        _flowTargets = new int[grid.TriangleCount];
        Array.Fill(_flowTargets, -1);
        _lakeOutletTargets = new int[grid.TriangleCount];
        Array.Fill(_lakeOutletTargets, -1);
        _ridgeInfluence = new float[grid.TriangleCount];
        _uplift = new float[grid.TriangleCount];
        _runoff = new int[grid.TriangleCount];
        _warmth = new int[grid.TriangleCount];
    }

    private static int[] NewField(PhysicalGeographyGrid grid) => new int[grid.TriangleCount];

    public PhysicalGeographyGrid Grid { get; }

    internal PhysicalGeographyFacts AvailableFacts { get; set; }

    internal int EffectiveSeaLevel { get; set; }

    public Span<int> CrustBasePotential => _crustBasePotential;

    public Span<int> CrustAssemblyIds => _crustAssemblyIds;

    public Span<int> CrustAssemblySeparation => _crustAssemblySeparation;

    public Span<int> CrustMotionX => _crustMotionX;

    public Span<int> CrustMotionY => _crustMotionY;

    public Span<int> CrustProvinceIds => _crustProvinceIds;

    public Span<int> CrustTypeCodes => _crustTypeCodes;

    internal Span<int> ContinentalPotential => _continentalPotential;

    public Span<int> TectonicBoundaryCodes => _tectonicBoundaryCodes;

    public Span<int> TectonicStrain => _tectonicStrain;

    public Span<int> Elevation => _elevation;

    public Span<int> Temperature => _temperature;

    public Span<int> Moisture => _moisture;

    public Span<int> OceanCurrentX => _oceanCurrentX;

    public Span<int> OceanCurrentY => _oceanCurrentY;

    public Span<int> OceanCurrentStrength => _oceanCurrentStrength;

    public Span<int> OceanCurrentHeat => _oceanCurrentHeat;

    public Span<int> WaveExposure => _waveExposure;

    public Span<int> Rainfall => _rainfall;

    public Span<int> Snowfall => _snowfall;

    public Span<int> SeaIceSuitability => _seaIceSuitability;

    public Span<int> BedrockElevation => _bedrockElevation;

    public Span<int> SedimentThickness => _sedimentThickness;

    public Span<int> SedimentSupply => _sedimentSupply;

    public Span<int> CumulativeFluvialErosion => _cumulativeFluvialErosion;

    public Span<int> CumulativeGlacialErosion => _cumulativeGlacialErosion;

    public Span<int> CumulativeCoastalErosion => _cumulativeCoastalErosion;

    public Span<int> CumulativeDeposition => _cumulativeDeposition;

    public Span<int> MaximumIceThickness => _maximumIceThickness;

    public Span<int> MaximumIceFlux => _maximumIceFlux;

    public Span<int> TectonicActivityHistory => _tectonicActivityHistory;

    public Span<int> WaveExposureHistory => _waveExposureHistory;

    public Span<int> Geothermal => _geothermal;

    public Span<int> Slope => _slope;

    public Span<int> LandformCodes => _landformCodes;

    public Span<int> WaterKindCodes => _waterKindCodes;

    public Span<int> WaterDepths => _waterDepths;

    public Span<int> WaterBodyIds => _waterBodyIds;

    public Span<int> DrainageBasinIds => _drainageBasinIds;

    public Span<int> FlowEdges => _flowEdges;

    public Span<int> FlowAccumulation => _flowAccumulation;

    public Span<int> RiverOrders => _riverOrders;

    public Span<int> FeatureFlags => _featureFlags;

    public Span<int> LandmassIds => _landmassIds;

    public Span<int> LandmassClassCodes => _landmassClassCodes;

    public Span<int> ContinentalGroupIds => _continentalGroupIds;

    public Span<int> BathymetricZoneCodes => _bathymetricZoneCodes;

    public Span<int> IceThickness => _iceThickness;

    public Span<int> IceFlux => _iceFlux;

    internal Span<int> IceFlowTargets => _iceFlowTargets;

    internal Span<int> IceBalance => _iceBalance;

    public Span<int> FractureConductance => _fractureConductance;

    public Span<int> HeatSourceDensity => _heatSourceDensity;

    internal Span<int> DrainageElevations => _drainageElevations;

    internal Span<int> DrainageParents => _drainageParents;

    internal Span<int> DrainageRanks => _drainageRanks;

    internal Span<int> FlowTargets => _flowTargets;

    internal Span<int> LakeOutletTargets => _lakeOutletTargets;

    public Span<float> RidgeInfluence => _ridgeInfluence;

    public Span<float> Uplift => _uplift;

    internal Span<int> Runoff => _runoff;

    internal Span<int> Warmth => _warmth;

    internal long SedimentProducedTotal { get; set; }

    internal long SedimentDepositedTotal { get; set; }

    internal long SedimentExportedTotal { get; set; }
}
