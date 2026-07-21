namespace Engine.WorldGen;

using System.Runtime.CompilerServices;

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
    private readonly float[] _triangleEdgeUnitX;
    private readonly float[] _triangleEdgeUnitY;
    private readonly float[] _triangleEdgeScale;
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
        _triangleEdgeUnitX = new float[_triangleNeighbors.Length];
        _triangleEdgeUnitY = new float[_triangleNeighbors.Length];
        _triangleEdgeScale = new float[_triangleNeighbors.Length];
        _triangleX = new float[triangleCount];
        _triangleY = new float[triangleCount];
        BuildTriangleGeometry();
        BuildTriangleNeighbors();
        BuildTriangleEdgeGeometry();
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal float TriangleEdgeUnitX(int triangleIndex, int edge) =>
        _triangleEdgeUnitX[(triangleIndex * 3) + edge];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal float TriangleEdgeUnitY(int triangleIndex, int edge) =>
        _triangleEdgeUnitY[(triangleIndex * 3) + edge];

    /// <summary>
    /// Mean-edge-length divided by this edge's centroid distance. Multiply an
    /// elevation drop across the edge by this to get a length-fair slope.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal float TriangleEdgeSlopeScale(int triangleIndex, int edge) =>
        _triangleEdgeScale[(triangleIndex * 3) + edge];

    public float TriangleX(int triangleIndex) => _triangleX[triangleIndex];

    public float TriangleY(int triangleIndex) => _triangleY[triangleIndex];

    public bool TriangleIsInsideConfiguredDisk(int triangleIndex) =>
        _hexDistances[TriangleHexIndex(triangleIndex)] <= Radius;

    public bool HexIsOnWorkBoundary(int hexIndex) => _hexDistances[hexIndex] == WorkRadius;

    public bool TryGetHexIndex(HexCoord coord, out int hexIndex) =>
        _hexIndexes.TryGetValue(coord, out hexIndex);

    /// <summary>
    /// Nearest triangle to a world position. Advection samples in world space
    /// rather than index space: an index-space shift would smear history along
    /// lattice axes and reintroduce the hex-aligned artifact class.
    /// </summary>
    public bool TryFindNearestTriangle(double x, double y, out int triangleIndex)
    {
        triangleIndex = -1;
        var approximateR = y / 1.5d;
        var approximateQ = (x / Math.Sqrt(3d)) - (approximateR / 2d);
        var best = double.MaxValue;
        for (var dq = -1; dq <= 1; dq++)
        {
            for (var dr = -1; dr <= 1; dr++)
            {
                var coord = RoundAxial(approximateQ + dq, approximateR + dr);
                if (!_hexIndexes.TryGetValue(coord, out var hexIndex))
                {
                    continue;
                }

                for (var local = 0; local < PhysicalGeographyCodes.MicrotrianglesPerHex; local++)
                {
                    var candidate = TriangleIndex(hexIndex, local);
                    var dx = _triangleX[candidate] - x;
                    var dy = _triangleY[candidate] - y;
                    var distance = (dx * dx) + (dy * dy);
                    if (distance < best)
                    {
                        best = distance;
                        triangleIndex = candidate;
                    }
                }
            }
        }

        return triangleIndex >= 0;
    }

    private static HexCoord RoundAxial(double q, double r)
    {
        var s = -q - r;
        var roundedQ = (int)Math.Round(q, MidpointRounding.AwayFromZero);
        var roundedR = (int)Math.Round(r, MidpointRounding.AwayFromZero);
        var roundedS = (int)Math.Round(s, MidpointRounding.AwayFromZero);
        var differenceQ = Math.Abs(roundedQ - q);
        var differenceR = Math.Abs(roundedR - r);
        var differenceS = Math.Abs(roundedS - s);
        if (differenceQ > differenceR && differenceQ > differenceS)
        {
            roundedQ = -roundedR - roundedS;
        }
        else if (differenceR > differenceS)
        {
            roundedR = -roundedQ - roundedS;
        }

        return new HexCoord(roundedQ, roundedR);
    }

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

    private void BuildTriangleEdgeGeometry()
    {
        var lengthTotal = 0d;
        var lengthCount = 0L;
        var lengths = new double[_triangleNeighbors.Length];
        for (var triangleIndex = 0; triangleIndex < TriangleCount; triangleIndex++)
        {
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = NeighborTriangleIndex(triangleIndex, edge);
                if (neighbor < 0)
                {
                    continue;
                }

                var dx = (double)_triangleX[neighbor] - _triangleX[triangleIndex];
                var dy = (double)_triangleY[neighbor] - _triangleY[triangleIndex];
                var length = Math.Max(0.001d, Math.Sqrt((dx * dx) + (dy * dy)));
                var offset = (triangleIndex * 3) + edge;
                _triangleEdgeUnitX[offset] = (float)(dx / length);
                _triangleEdgeUnitY[offset] = (float)(dy / length);
                lengths[offset] = length;
                lengthTotal += length;
                lengthCount++;
            }
        }

        // Centroid steps differ in world length by edge type (cross-hex steps
        // are not the same distance as intra-hex ones). Every drop comparison
        // multiplies by this scale so it compares slope, not raw drop —
        // otherwise long-edge directions systematically look steeper and
        // drainage gutters onto the hex lattice.
        var meanLength = lengthTotal / Math.Max(1, lengthCount);
        for (var offset = 0; offset < lengths.Length; offset++)
        {
            _triangleEdgeScale[offset] = lengths[offset] <= 0d
                ? 1f
                : (float)(meanLength / lengths[offset]);
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
    private readonly int[] _crustCollisionIntensity;
    private readonly int[] _crustMotionX;
    private readonly int[] _crustMotionY;
    private readonly int[] _crustProvinceIds;
    private readonly int[] _crustTypeCodes;
    private readonly int[] _advectionSourceIndex;
    private readonly int[] _marginCodes;
    private readonly int[] _crustAge;
    private readonly long[] _bedrockElevation;
    private readonly long[] _crustThickness;
    private readonly int[] _continentalPotential;
    private readonly long[] _cumulativeCoastalErosion;
    private readonly long[] _cumulativeDeposition;
    private readonly long[] _cumulativeFluvialErosion;
    private readonly long[] _cumulativeGlacialErosion;
    private readonly long[] _cumulativeGlacialDeposition;
    private readonly int[] _drainageBasinIds;
    private readonly int[] _drainageGuidanceTargets;
    private readonly long[] _drainageElevations;
    private readonly int[] _drainageParents;
    private readonly int[] _drainageRanks;
    private readonly long[] _elevation;
    private readonly int[] _featureFlags;
    private readonly int[] _fractureConductance;
    private readonly int[] _flowAccumulation;
    private readonly int[] _flowEdges;
    private readonly int[] _flowTargets;
    private readonly int[] _geothermal;
    private readonly int[] _geothermalHistory;
    private readonly int[] _groundedIce;
    private readonly long[] _impactExcavation;
    private readonly long[] _impactRimUplift;
    private readonly long[] _impactEjectaThickness;
    private readonly int[] _impactHeat;
    private readonly int[] _glacialMeltwater;
    private readonly int[] _heatSourceDensity;
    private readonly int[] _mantlePlumeRootStrength;
    private readonly int[] _volcanicActivity;
    private readonly long[] _volcanicConstructionHistory;
    private readonly int[] _volcanicLastActiveEpoch;
    private readonly int[] _iceFlux;
    private readonly int[] _iceFlowTargets;
    private readonly int[] _iceBalance;
    private readonly int[] _iceThickness;
    private readonly int[] _landmassClassCodes;
    private readonly int[] _landmassIds;
    private readonly int[] _lakeOutletTargets;
    private readonly int[] _landformCodes;
    private readonly int[] _maximumIceFlux;
    private readonly int[] _maximumGroundedIceFlux;
    private readonly int[] _maximumIceThickness;
    private readonly int[] _moisture;
    private readonly int[] _oceanCurrentHeat;
    private readonly int[] _oceanCurrentStrength;
    private readonly int[] _oceanCurrentX;
    private readonly int[] _oceanCurrentY;
    private readonly int[] _oceanDistance;
    private readonly int[] _rainfall;
    private readonly float[] _ridgeInfluence;
    private readonly int[] _riverOrders;
    private readonly int[] _riverChannelStrength;
    private readonly int[] _riverChannelOrders;
    private readonly int[] _floodplainIntensity;
    private readonly int[] _deltaIntensity;
    private readonly int[] _riverSpineEdgeMasks;
    private readonly int[] _runoff;
    private readonly int[] _seaIceSuitability;
    private readonly long[] _sedimentSupply;
    private readonly long[] _sedimentThickness;
    private readonly int[] _snowfall;
    private readonly long[] _slope;
    private readonly int[] _temperature;
    private readonly int[] _windX;
    private readonly int[] _windY;
    private readonly int[] _upwindTargets;
    private readonly int[] _moistureBarriers;
    private readonly int[] _precipitationBarriers;
    private readonly int[] _upwindOceanSources;
    private readonly int[] _tectonicBoundaryCodes;
    private readonly int[] _initialTectonicBoundaryCodes;
    private readonly int[] _initialTectonicStrain;
    private readonly int[] _tectonicStrain;
    private readonly int[] _tectonicActivityHistory;
    private readonly int[] _warmth;
    private readonly int[] _waveExposure;
    private readonly int[] _waveExposureHistory;
    private readonly int[] _externalHeatForcing;
    private readonly int[] _externalCoastalForcing;
    private readonly int[] _externalCoastalForcingHistory;
    private readonly int[] _continentalGroupIds;
    private readonly int[] _bathymetricZoneCodes;
    private readonly float[] _uplift;
    private readonly int[] _waterBodyIds;
    private readonly long[] _waterDepths;
    private readonly int[] _waterKindCodes;
    private readonly long[] _waterSurfaceElevations;

    private static (int[] Source, int[] Margin, int[] Age, int[] Potential) NewCrustFrameFields(
        PhysicalGeographyGrid grid) =>
        (new int[grid.TriangleCount], new int[grid.TriangleCount],
            new int[grid.TriangleCount], new int[grid.TriangleCount]);

    internal PhysicalGeographyField(PhysicalGeographyGrid grid)
    {
        Grid = grid;
        (_crustBasePotential, _crustAssemblyIds, _crustAssemblySeparation, _crustMotionX,
            _crustMotionY, _crustProvinceIds, _crustTypeCodes) =
            (new int[grid.TriangleCount], new int[grid.TriangleCount], new int[grid.TriangleCount],
                new int[grid.TriangleCount], new int[grid.TriangleCount], new int[grid.TriangleCount],
                new int[grid.TriangleCount]);
        (_advectionSourceIndex, _marginCodes, _crustAge, _continentalPotential) =
            NewCrustFrameFields(grid);
        _elevation = NewElevationField(grid);
        _temperature = new int[grid.TriangleCount];
        (_windX, _windY, _upwindTargets, _moistureBarriers,
            _precipitationBarriers, _upwindOceanSources) = NewAtmosphericFields(grid);
        (_tectonicBoundaryCodes, _tectonicStrain, _initialTectonicBoundaryCodes,
            _initialTectonicStrain) =
            (new int[grid.TriangleCount], new int[grid.TriangleCount],
                new int[grid.TriangleCount], new int[grid.TriangleCount]);
        (_oceanCurrentX, _oceanCurrentY, _oceanCurrentStrength, _oceanCurrentHeat,
            _waveExposure, _oceanDistance) = NewOceanFields(grid);
        (_rainfall, _snowfall, _seaIceSuitability) = NewTriple(grid);
        (_bedrockElevation, _sedimentThickness, _sedimentSupply, _cumulativeFluvialErosion,
            _cumulativeGlacialErosion, _cumulativeGlacialDeposition,
            _cumulativeCoastalErosion, _cumulativeDeposition, _crustThickness) = NewLandscapeHistoryFields(grid);

        (_maximumIceThickness, _maximumIceFlux, _tectonicActivityHistory, _waveExposureHistory,
            _geothermalHistory) =
            (NewField(grid), NewField(grid), NewField(grid), NewField(grid), NewField(grid));
        (_impactExcavation, _impactRimUplift, _impactEjectaThickness, _impactHeat, _glacialMeltwater) =
            NewImpactFields(grid);
        (_externalHeatForcing, _externalCoastalForcing, _externalCoastalForcingHistory) =
            (NewField(grid), NewField(grid), NewField(grid));
        (_moisture, _geothermal, _crustCollisionIntensity) = (new int[grid.TriangleCount], NewField(grid), NewField(grid));
        (_mantlePlumeRootStrength, _volcanicActivity, _volcanicConstructionHistory,
            _volcanicLastActiveEpoch) = NewVolcanicFields(grid);
        _slope = NewElevationField(grid);
        _landformCodes = NewField(grid);
        _waterKindCodes = NewField(grid);
        _waterDepths = NewElevationField(grid);
        _waterSurfaceElevations = NewElevationField(grid);
        Array.Fill(_waterSurfaceElevations, long.MinValue);
        _waterBodyIds = NewField(grid);
        (_drainageBasinIds, _flowEdges, _flowAccumulation, _riverOrders) =
            (NewField(grid), NewField(grid), NewField(grid), NewField(grid));
        Array.Fill(_flowEdges, PhysicalGeographyCodes.NoFlowEdge);
        (_riverChannelStrength, _riverChannelOrders, _floodplainIntensity, _deltaIntensity,
            _riverSpineEdgeMasks) =
            (NewField(grid), NewField(grid), NewField(grid), NewField(grid), NewField(grid));
        (_featureFlags, _landmassIds, _landmassClassCodes, _continentalGroupIds,
            _bathymetricZoneCodes) =
            (NewField(grid), NewField(grid), NewField(grid), NewField(grid), NewField(grid));
        (_iceThickness, _iceFlux, _iceFlowTargets, _iceBalance, _groundedIce,
            _maximumGroundedIceFlux) = NewIceFields(grid);
        _fractureConductance = new int[grid.TriangleCount];
        _heatSourceDensity = new int[grid.TriangleCount];
        (_drainageElevations, _drainageParents, _drainageRanks, _drainageGuidanceTargets,
            _flowTargets, _lakeOutletTargets) = NewDrainageFields(grid);
        _ridgeInfluence = new float[grid.TriangleCount];
        _uplift = new float[grid.TriangleCount];
        _runoff = new int[grid.TriangleCount];
        _warmth = new int[grid.TriangleCount];
    }

    private static int[] NewField(PhysicalGeographyGrid grid) => new int[grid.TriangleCount];

    private static (int[] Roots, int[] Activity, long[] History, int[] LastActiveEpoch)
        NewVolcanicFields(PhysicalGeographyGrid grid)
    {
        var lastActiveEpoch = NewField(grid);
        Array.Fill(lastActiveEpoch, -1);
        return (NewField(grid), NewField(grid), NewElevationField(grid), lastActiveEpoch);
    }

    private static long[] NewElevationField(PhysicalGeographyGrid grid) => new long[grid.TriangleCount];

    private static (
        long[] Excavation,
        long[] RimUplift,
        long[] Ejecta,
        int[] Heat,
        int[] Meltwater) NewImpactFields(PhysicalGeographyGrid grid) =>
        (NewElevationField(grid), NewElevationField(grid), NewElevationField(grid),
            NewField(grid), NewField(grid));

    private static (
        int[] Thickness,
        int[] Flux,
        int[] Targets,
        int[] Balance,
        int[] Grounded,
        int[] MaximumGroundedFlux) NewIceFields(
        PhysicalGeographyGrid grid)
    {
        var targets = NewField(grid);
        Array.Fill(targets, -1);
        return (NewField(grid), NewField(grid), targets, NewField(grid), NewField(grid), NewField(grid));
    }

    private static (
        long[] Elevations,
        int[] Parents,
        int[] Ranks,
        int[] GuidanceTargets,
        int[] FlowTargets,
        int[] LakeOutlets) NewDrainageFields(PhysicalGeographyGrid grid)
    {
        var parents = NewField(grid);
        var guidanceTargets = NewField(grid);
        var flowTargets = NewField(grid);
        var lakeOutlets = NewField(grid);
        Array.Fill(parents, -1);
        Array.Fill(guidanceTargets, -1);
        Array.Fill(flowTargets, -1);
        Array.Fill(lakeOutlets, -1);
        return (NewElevationField(grid), parents, NewField(grid), guidanceTargets, flowTargets, lakeOutlets);
    }

    private static (int[] First, int[] Second, int[] Third) NewTriple(
        PhysicalGeographyGrid grid) =>
        (NewField(grid), NewField(grid), NewField(grid));

    private static (
        long[] Bedrock,
        long[] Sediment,
        long[] Supply,
        long[] FluvialErosion,
        long[] GlacialErosion,
        long[] GlacialDeposition,
        long[] CoastalErosion,
        long[] Deposition,
        long[] CrustThickness) NewLandscapeHistoryFields(PhysicalGeographyGrid grid) =>
        (NewElevationField(grid), NewElevationField(grid), NewElevationField(grid),
            NewElevationField(grid), NewElevationField(grid), NewElevationField(grid),
            NewElevationField(grid), NewElevationField(grid), NewElevationField(grid));

    private static (
        int[] CurrentX,
        int[] CurrentY,
        int[] CurrentStrength,
        int[] CurrentHeat,
        int[] WaveExposure,
        int[] OceanDistance) NewOceanFields(PhysicalGeographyGrid grid)
    {
        var oceanDistance = NewField(grid);
        Array.Fill(oceanDistance, -1);
        return (
            NewField(grid),
            NewField(grid),
            NewField(grid),
            NewField(grid),
            NewField(grid),
            oceanDistance);
    }

    private static (
        int[] WindX,
        int[] WindY,
        int[] UpwindTargets,
        int[] MoistureBarriers,
        int[] PrecipitationBarriers,
        int[] UpwindOceanSources) NewAtmosphericFields(
        PhysicalGeographyGrid grid)
    {
        var upwindTargets = NewField(grid);
        var upwindOceanSources = NewField(grid);
        Array.Fill(upwindTargets, -1);
        Array.Fill(upwindOceanSources, -1);
        return (
            NewField(grid),
            NewField(grid),
            upwindTargets,
            NewField(grid),
            NewField(grid),
            upwindOceanSources);
    }

    public PhysicalGeographyGrid Grid { get; }

    internal PhysicalGeographyFacts AvailableFacts { get; set; }

    internal long EffectiveSeaLevel { get; set; }

    public Span<int> CrustBasePotential => _crustBasePotential;

    public Span<int> CrustAssemblyIds => _crustAssemblyIds;

    public Span<int> CrustAssemblySeparation => _crustAssemblySeparation;

    /// <summary>
    /// Per-mille intensity of continent-against-continent collision. Ocean
    /// closing against a continent subducts and builds a coastal range;
    /// continent meeting continent has nowhere to go and stacks crust, which
    /// is what puts a Himalaya deep inside a landmass instead of another
    /// Andes along its shore.
    /// </summary>
    public Span<int> CrustCollisionIntensity => _crustCollisionIntensity;

    public Span<int> CrustMotionX => _crustMotionX;

    public Span<int> CrustMotionY => _crustMotionY;

    public Span<int> CrustProvinceIds => _crustProvinceIds;

    public Span<int> CrustTypeCodes => _crustTypeCodes;

    /// <summary>
    /// What the nearest plate boundary is doing to this crust. Coastal layers
    /// consume this so coasts differ in kind, not merely in raggedness.
    /// </summary>
    /// <summary>
    /// For each triangle, the triangle its crust arrived from during the last
    /// plate-drift step, or -1 where new crust was created at a rift. Published
    /// so domain layers can advect their own persistent history without
    /// Engine.WorldGen knowing what that history is.
    /// </summary>
    public Span<int> AdvectionSourceIndex => _advectionSourceIndex;

    public Span<int> MarginCodes => _marginCodes;

    /// <summary>
    /// Relative crust age in per-mille of the oldest crust, rising away from
    /// spreading boundaries. Oceanic depth follows it, which is what gives
    /// passive margins their broad shallow shelves.
    /// </summary>
    public Span<int> CrustAge => _crustAge;

    internal Span<int> ContinentalPotential => _continentalPotential;

    public Span<int> TectonicBoundaryCodes => _tectonicBoundaryCodes;

    public Span<int> TectonicStrain => _tectonicStrain;

    internal Span<int> InitialTectonicBoundaryCodes => _initialTectonicBoundaryCodes;

    internal Span<int> InitialTectonicStrain => _initialTectonicStrain;

    public Span<long> Elevation => _elevation;

    public Span<int> Temperature => _temperature;

    internal Span<int> WindX => _windX;

    internal Span<int> WindY => _windY;

    internal Span<int> UpwindTargets => _upwindTargets;

    internal Span<int> MoistureBarriers => _moistureBarriers;

    internal Span<int> PrecipitationBarriers => _precipitationBarriers;

    internal Span<int> UpwindOceanSources => _upwindOceanSources;

    public Span<int> Moisture => _moisture;

    public Span<int> OceanCurrentX => _oceanCurrentX;

    public Span<int> OceanCurrentY => _oceanCurrentY;

    public Span<int> OceanCurrentStrength => _oceanCurrentStrength;

    public Span<int> OceanCurrentHeat => _oceanCurrentHeat;

    internal Span<int> OceanDistance => _oceanDistance;

    public Span<int> WaveExposure => _waveExposure;

    public Span<int> Rainfall => _rainfall;

    public Span<int> Snowfall => _snowfall;

    public Span<int> SeaIceSuitability => _seaIceSuitability;

    public Span<long> BedrockElevation => _bedrockElevation;

    /// <summary>
    /// Crustal thickness, in the same fixed-point units as elevation. This is
    /// the quantity geology actually changes: collision thickens it, rifting
    /// thins it, volcanism adds to it, erosion removes it. Surface height is
    /// derived from it by isostasy rather than painted from a continental
    /// mask, because a mask must saturate to define "definitely land" and so
    /// flattens exactly the interior it defines. Thickness has no such
    /// ceiling - two collisions stack where one collided once.
    /// </summary>
    public Span<long> CrustThickness => _crustThickness;

    public Span<long> SedimentThickness => _sedimentThickness;

    public Span<long> SedimentSupply => _sedimentSupply;

    public Span<long> CumulativeFluvialErosion => _cumulativeFluvialErosion;

    public Span<long> CumulativeGlacialErosion => _cumulativeGlacialErosion;

    public Span<long> CumulativeGlacialDeposition => _cumulativeGlacialDeposition;

    public Span<long> CumulativeCoastalErosion => _cumulativeCoastalErosion;

    public Span<long> CumulativeDeposition => _cumulativeDeposition;

    public Span<int> MaximumIceThickness => _maximumIceThickness;

    public Span<int> MaximumIceFlux => _maximumIceFlux;

    public Span<int> MaximumGroundedIceFlux => _maximumGroundedIceFlux;

    public Span<int> TectonicActivityHistory => _tectonicActivityHistory;

    public Span<int> WaveExposureHistory => _waveExposureHistory;

    public Span<int> GeothermalHistory => _geothermalHistory;

    public Span<long> ImpactExcavation => _impactExcavation;

    public Span<long> ImpactRimUplift => _impactRimUplift;

    public Span<long> ImpactEjectaThickness => _impactEjectaThickness;

    public Span<int> ImpactHeat => _impactHeat;

    public Span<int> GlacialMeltwater => _glacialMeltwater;

    public Span<int> ExternalHeatForcing => _externalHeatForcing;

    public Span<int> ExternalCoastalForcing => _externalCoastalForcing;

    public Span<int> ExternalCoastalForcingHistory => _externalCoastalForcingHistory;

    public Span<int> Geothermal => _geothermal;

    public Span<long> Slope => _slope;

    public Span<int> LandformCodes => _landformCodes;

    public Span<int> WaterKindCodes => _waterKindCodes;

    public ReadOnlySpan<long> WaterDepths => _waterDepths;

    public ReadOnlySpan<long> WaterSurfaceElevations => _waterSurfaceElevations;

    internal Span<long> MutableWaterDepths => _waterDepths;

    public Span<int> WaterBodyIds => _waterBodyIds;

    public Span<int> DrainageBasinIds => _drainageBasinIds;

    public Span<int> FlowEdges => _flowEdges;

    public Span<int> FlowAccumulation => _flowAccumulation;

    public Span<int> RiverOrders => _riverOrders;

    public Span<int> RiverChannelStrength => _riverChannelStrength;

    public Span<int> RiverChannelOrders => _riverChannelOrders;

    public Span<int> FloodplainIntensity => _floodplainIntensity;

    public Span<int> DeltaIntensity => _deltaIntensity;

    public Span<int> RiverSpineEdgeMasks => _riverSpineEdgeMasks;

    public Span<int> FeatureFlags => _featureFlags;

    public Span<int> LandmassIds => _landmassIds;

    public Span<int> LandmassClassCodes => _landmassClassCodes;

    public Span<int> ContinentalGroupIds => _continentalGroupIds;

    public Span<int> BathymetricZoneCodes => _bathymetricZoneCodes;

    public Span<int> IceThickness => _iceThickness;

    public Span<int> IceFlux => _iceFlux;

    public Span<int> GroundedIce => _groundedIce;

    internal Span<int> IceFlowTargets => _iceFlowTargets;

    internal Span<int> IceBalance => _iceBalance;

    public Span<int> FractureConductance => _fractureConductance;

    public Span<int> HeatSourceDensity => _heatSourceDensity;

    public Span<int> MantlePlumeRootStrength => _mantlePlumeRootStrength;

    public Span<int> VolcanicActivity => _volcanicActivity;

    public Span<long> VolcanicConstructionHistory => _volcanicConstructionHistory;

    public Span<int> VolcanicLastActiveEpoch => _volcanicLastActiveEpoch;

    internal Span<long> DrainageElevations => _drainageElevations;

    public ReadOnlySpan<long> ConditionedDrainageElevation => _drainageElevations;

    internal Span<int> DrainageParents => _drainageParents;

    internal Span<int> DrainageRanks => _drainageRanks;

    public ReadOnlySpan<int> DrainageFlatRanks => _drainageRanks;

    internal Span<int> DrainageGuidanceTargets => _drainageGuidanceTargets;

    internal Span<int> FlowTargets => _flowTargets;

    internal Span<int> LakeOutletTargets => _lakeOutletTargets;

    public Span<float> RidgeInfluence => _ridgeInfluence;

    public Span<float> Uplift => _uplift;

    internal Span<int> Runoff => _runoff;

    internal Span<int> Warmth => _warmth;

    internal void ClearWaterSurfaces()
    {
        _waterDepths.AsSpan().Clear();
        Array.Fill(_waterSurfaceElevations, long.MinValue);
    }

    internal void ClearWaterSurface(int triangleIndex)
    {
        _waterDepths[triangleIndex] = 0;
        _waterSurfaceElevations[triangleIndex] = long.MinValue;
    }

    internal void SetWaterSurface(int triangleIndex, long surfaceElevation)
    {
        var depth = surfaceElevation - _elevation[triangleIndex];
        if (depth <= 0)
        {
            throw new InvalidOperationException("Water surface must be above its physical floor.");
        }

        _waterSurfaceElevations[triangleIndex] = surfaceElevation;
        _waterDepths[triangleIndex] = depth;
    }

    internal void ReconcileWaterAfterFloorChange(int triangleIndex)
    {
        var surface = _waterSurfaceElevations[triangleIndex];
        if (surface == long.MinValue)
        {
            return;
        }

        if (surface > _elevation[triangleIndex])
        {
            _waterDepths[triangleIndex] = surface - _elevation[triangleIndex];
            return;
        }

        _waterKindCodes[triangleIndex] = PhysicalWaterKindCodes.None;
        _waterBodyIds[triangleIndex] = 0;
        _lakeOutletTargets[triangleIndex] = -1;
        ClearWaterSurface(triangleIndex);
    }

    internal long SedimentProducedTotal { get; set; }

    internal long SedimentDepositedTotal { get; set; }

    internal long SedimentExportedTotal { get; set; }

    public int CompletedEpochCount { get; internal set; }

    /// <summary>
    /// Total distance, in world units, that plates have drifted since world
    /// creation. Province sites are placed at their seed positions offset by
    /// this, so each epoch is a static snapshot of a moved plate arrangement.
    /// </summary>
    public double AccumulatedPlateDrift { get; internal set; }

    /// <summary>
    /// Cumulative drainage-guidance selections whose top candidate scores tied
    /// within epsilon, leaving the hashed tie-break to decide. With real
    /// relief everywhere this should stay near zero of
    /// <see cref="DrainageDecisionTotal"/>.
    /// </summary>
    public long DrainageTieDecisionTotal { get; internal set; }

    public long DrainageDecisionTotal { get; internal set; }

    /// <summary>
    /// Cumulative priority-flood fills where the neighbor floor sat at or
    /// below the flood level (flat or depression fill), against all fills.
    /// </summary>
    public long FloodFlatFillTotal { get; internal set; }

    public long FloodFillTotal { get; internal set; }
}
