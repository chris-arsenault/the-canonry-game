namespace Engine.Kernel;

using Engine.Constants;

/// <summary>
/// Fixed primitive geography for every potential physical surface hex. The
/// blueprint is installed once; materialization only exposes a precomputed
/// coordinate to the live world.
/// </summary>
internal sealed class KernelPhysicalGeographyData
{
    private readonly int _radiusCapacity;
    private readonly int _diameter;
    private readonly bool[] _installedCells;
    private readonly int[] _baseTerrainIds;
    private readonly int[] _superregionIds;
    private readonly int[] _continentIds;
    private readonly int[] _dominantMaterialIds;
    private readonly int[] _meanElevations;
    private readonly int[] _minElevations;
    private readonly int[] _maxElevations;
    private readonly int[] _relief;
    private readonly int[] _roughness;
    private readonly int[] _meanTemperatures;
    private readonly int[] _meanMoisture;
    private readonly int[] _meanGeothermal;
    private readonly int[] _landCoverage;
    private readonly int[] _waterCoverage;
    private readonly int[] _oceanCoverage;
    private readonly int[] _lakeCoverage;
    private readonly int[] _permanentIceCoverage;
    private readonly int[] _riverCoverage;
    private readonly int[] _peakCounts;
    private readonly int[] _riverEdgeMasks;
    private readonly int[] _coastEdgeMasks;
    private readonly int[] _ridgeEdgeMasks;
    private readonly int[] _cliffEdgeMasks;
    private readonly int[] _passEdgeMasks;
    private readonly int[] _microtriangleMaterialIds;
    private readonly int[] _microtriangleLandformCodes;
    private readonly int[] _microtriangleElevations;
    private readonly int[] _microtriangleTemperatures;
    private readonly int[] _microtriangleMoisture;
    private readonly int[] _microtriangleGeothermal;
    private readonly int[] _microtriangleWaterKindCodes;
    private readonly int[] _microtriangleWaterDepths;
    private readonly int[] _microtriangleWaterBodyIds;
    private readonly int[] _microtriangleDrainageBasinIds;
    private readonly int[] _microtriangleFlowEdges;
    private readonly int[] _microtriangleFlowAccumulation;
    private readonly int[] _microtriangleRiverOrders;
    private readonly int[] _microtriangleFeatureFlags;
    private int _installedRadius;

    public KernelPhysicalGeographyData(int radiusCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(radiusCapacity);
        _radiusCapacity = radiusCapacity;
        _diameter = checked((radiusCapacity * 2) + 1);
        var cellCapacity = checked(_diameter * _diameter);
        var microtriangleCapacity = checked(cellCapacity * PhysicalGeographyCodes.MicrotrianglesPerHex);
        _installedCells = new bool[cellCapacity];
        (_baseTerrainIds, _superregionIds, _continentIds, _dominantMaterialIds) =
            CreateIntArrays4(cellCapacity);
        (_meanElevations, _minElevations, _maxElevations, _relief) =
            CreateIntArrays4(cellCapacity);
        _roughness = new int[cellCapacity];
        _meanTemperatures = new int[cellCapacity];
        _meanMoisture = new int[cellCapacity];
        _meanGeothermal = new int[cellCapacity];
        _landCoverage = new int[cellCapacity];
        _waterCoverage = new int[cellCapacity];
        _oceanCoverage = new int[cellCapacity];
        _lakeCoverage = new int[cellCapacity];
        _permanentIceCoverage = new int[cellCapacity];
        _riverCoverage = new int[cellCapacity];
        _peakCounts = new int[cellCapacity];
        _riverEdgeMasks = new int[cellCapacity];
        _coastEdgeMasks = new int[cellCapacity];
        _ridgeEdgeMasks = new int[cellCapacity];
        _cliffEdgeMasks = new int[cellCapacity];
        _passEdgeMasks = new int[cellCapacity];
        _microtriangleMaterialIds = new int[microtriangleCapacity];
        _microtriangleLandformCodes = new int[microtriangleCapacity];
        _microtriangleElevations = new int[microtriangleCapacity];
        _microtriangleTemperatures = new int[microtriangleCapacity];
        _microtriangleMoisture = new int[microtriangleCapacity];
        _microtriangleGeothermal = new int[microtriangleCapacity];
        _microtriangleWaterKindCodes = new int[microtriangleCapacity];
        _microtriangleWaterDepths = new int[microtriangleCapacity];
        _microtriangleWaterBodyIds = new int[microtriangleCapacity];
        _microtriangleDrainageBasinIds = new int[microtriangleCapacity];
        _microtriangleFlowEdges = new int[microtriangleCapacity];
        _microtriangleFlowAccumulation = new int[microtriangleCapacity];
        _microtriangleRiverOrders = new int[microtriangleCapacity];
        _microtriangleFeatureFlags = new int[microtriangleCapacity];
    }

    private static (int[] A, int[] B, int[] C, int[] D) CreateIntArrays4(int capacity) =>
        (new int[capacity], new int[capacity], new int[capacity], new int[capacity]);

    internal int Radius => _installedRadius;

    internal int RadiusCapacity => _radiusCapacity;

    internal void BeginInstall(int radius)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(radius);
        if (radius > _radiusCapacity || _installedRadius != 0)
        {
            throw new global::System.InvalidOperationException();
        }

        _installedRadius = radius;
    }

    internal void InstallCell(
        int q,
        int r,
        in PhysicalGeographyCellSummary summary,
        global::System.ReadOnlySpan<int> materialIds,
        global::System.ReadOnlySpan<int> landformCodes,
        global::System.ReadOnlySpan<int> elevations,
        global::System.ReadOnlySpan<int> temperatures,
        global::System.ReadOnlySpan<int> moisture,
        global::System.ReadOnlySpan<int> geothermal,
        global::System.ReadOnlySpan<int> waterKindCodes,
        global::System.ReadOnlySpan<int> waterDepths,
        global::System.ReadOnlySpan<int> waterBodyIds,
        global::System.ReadOnlySpan<int> drainageBasinIds,
        global::System.ReadOnlySpan<int> flowEdges,
        global::System.ReadOnlySpan<int> flowAccumulation,
        global::System.ReadOnlySpan<int> riverOrders,
        global::System.ReadOnlySpan<int> featureFlags)
    {
        if (!CanInstallCell(q, r, summary) ||
            !SurfaceSpansValid(materialIds, landformCodes, elevations, temperatures, moisture, geothermal) ||
            !HydrologySpansValid(
                waterKindCodes,
                waterDepths,
                waterBodyIds,
                drainageBasinIds,
                flowEdges,
                flowAccumulation,
                riverOrders,
                featureFlags))
        {
            throw new global::System.InvalidOperationException();
        }

        var cellIndex = CellIndex(q, r);
        if (_installedCells[cellIndex])
        {
            throw new global::System.InvalidOperationException();
        }

        InstallCellSummary(cellIndex, summary);
        InstallMicrotriangles(
            cellIndex,
            materialIds,
            landformCodes,
            elevations,
            temperatures,
            moisture,
            geothermal,
            waterKindCodes,
            waterDepths,
            waterBodyIds,
            drainageBasinIds,
            flowEdges,
            flowAccumulation,
            riverOrders,
            featureFlags);
    }

    private void InstallCellSummary(
        int cellIndex,
        in PhysicalGeographyCellSummary summary)
    {
        _installedCells[cellIndex] = true;
        _baseTerrainIds[cellIndex] = summary.BaseTerrainId;
        _superregionIds[cellIndex] = summary.SuperregionId;
        _continentIds[cellIndex] = summary.ContinentId;
        _dominantMaterialIds[cellIndex] = summary.DominantMaterialId;
        _meanElevations[cellIndex] = summary.MeanElevation;
        _minElevations[cellIndex] = summary.MinElevation;
        _maxElevations[cellIndex] = summary.MaxElevation;
        _relief[cellIndex] = summary.Relief;
        _roughness[cellIndex] = summary.Roughness;
        _meanTemperatures[cellIndex] = summary.MeanTemperature;
        _meanMoisture[cellIndex] = summary.MeanMoisture;
        _meanGeothermal[cellIndex] = summary.MeanGeothermal;
        _landCoverage[cellIndex] = summary.LandCoverage;
        _waterCoverage[cellIndex] = summary.WaterCoverage;
        _oceanCoverage[cellIndex] = summary.OceanCoverage;
        _lakeCoverage[cellIndex] = summary.LakeCoverage;
        _permanentIceCoverage[cellIndex] = summary.PermanentIceCoverage;
        _riverCoverage[cellIndex] = summary.RiverCoverage;
        _peakCounts[cellIndex] = summary.PeakCount;
        _riverEdgeMasks[cellIndex] = summary.RiverEdgeMask;
        _coastEdgeMasks[cellIndex] = summary.CoastEdgeMask;
        _ridgeEdgeMasks[cellIndex] = summary.RidgeEdgeMask;
        _cliffEdgeMasks[cellIndex] = summary.CliffEdgeMask;
        _passEdgeMasks[cellIndex] = summary.PassEdgeMask;
    }

    private void InstallMicrotriangles(
        int cellIndex,
        global::System.ReadOnlySpan<int> materialIds,
        global::System.ReadOnlySpan<int> landformCodes,
        global::System.ReadOnlySpan<int> elevations,
        global::System.ReadOnlySpan<int> temperatures,
        global::System.ReadOnlySpan<int> moisture,
        global::System.ReadOnlySpan<int> geothermal,
        global::System.ReadOnlySpan<int> waterKindCodes,
        global::System.ReadOnlySpan<int> waterDepths,
        global::System.ReadOnlySpan<int> waterBodyIds,
        global::System.ReadOnlySpan<int> drainageBasinIds,
        global::System.ReadOnlySpan<int> flowEdges,
        global::System.ReadOnlySpan<int> flowAccumulation,
        global::System.ReadOnlySpan<int> riverOrders,
        global::System.ReadOnlySpan<int> featureFlags)
    {
        var microtriangleOffset = checked(cellIndex * PhysicalGeographyCodes.MicrotrianglesPerHex);
        materialIds.CopyTo(new global::System.Span<int>(_microtriangleMaterialIds, microtriangleOffset, materialIds.Length));
        landformCodes.CopyTo(new global::System.Span<int>(_microtriangleLandformCodes, microtriangleOffset, landformCodes.Length));
        elevations.CopyTo(new global::System.Span<int>(_microtriangleElevations, microtriangleOffset, elevations.Length));
        temperatures.CopyTo(new global::System.Span<int>(_microtriangleTemperatures, microtriangleOffset, temperatures.Length));
        moisture.CopyTo(new global::System.Span<int>(_microtriangleMoisture, microtriangleOffset, moisture.Length));
        geothermal.CopyTo(new global::System.Span<int>(_microtriangleGeothermal, microtriangleOffset, geothermal.Length));
        waterKindCodes.CopyTo(new global::System.Span<int>(_microtriangleWaterKindCodes, microtriangleOffset, waterKindCodes.Length));
        waterDepths.CopyTo(new global::System.Span<int>(_microtriangleWaterDepths, microtriangleOffset, waterDepths.Length));
        waterBodyIds.CopyTo(new global::System.Span<int>(_microtriangleWaterBodyIds, microtriangleOffset, waterBodyIds.Length));
        drainageBasinIds.CopyTo(new global::System.Span<int>(_microtriangleDrainageBasinIds, microtriangleOffset, drainageBasinIds.Length));
        flowEdges.CopyTo(new global::System.Span<int>(_microtriangleFlowEdges, microtriangleOffset, flowEdges.Length));
        flowAccumulation.CopyTo(new global::System.Span<int>(_microtriangleFlowAccumulation, microtriangleOffset, flowAccumulation.Length));
        riverOrders.CopyTo(new global::System.Span<int>(_microtriangleRiverOrders, microtriangleOffset, riverOrders.Length));
        featureFlags.CopyTo(new global::System.Span<int>(_microtriangleFeatureFlags, microtriangleOffset, featureFlags.Length));
    }

    private bool CanInstallCell(
        int q,
        int r,
        in PhysicalGeographyCellSummary summary) =>
        _installedRadius > 0 &&
        HexDistanceFromOrigin(q, r) <= _installedRadius &&
        summary.BaseTerrainId > 0 &&
        summary.SuperregionId > 0 &&
        summary.ContinentId > 0 &&
        summary.DominantMaterialId > 0;

    private static bool SurfaceSpansValid(
        global::System.ReadOnlySpan<int> materialIds,
        global::System.ReadOnlySpan<int> landformCodes,
        global::System.ReadOnlySpan<int> elevations,
        global::System.ReadOnlySpan<int> temperatures,
        global::System.ReadOnlySpan<int> moisture,
        global::System.ReadOnlySpan<int> geothermal) =>
        IsMicrotriangleSpan(materialIds) &&
        IsMicrotriangleSpan(landformCodes) &&
        IsMicrotriangleSpan(elevations) &&
        IsMicrotriangleSpan(temperatures) &&
        IsMicrotriangleSpan(moisture) &&
        IsMicrotriangleSpan(geothermal);

    private static bool HydrologySpansValid(
        global::System.ReadOnlySpan<int> waterKindCodes,
        global::System.ReadOnlySpan<int> waterDepths,
        global::System.ReadOnlySpan<int> waterBodyIds,
        global::System.ReadOnlySpan<int> drainageBasinIds,
        global::System.ReadOnlySpan<int> flowEdges,
        global::System.ReadOnlySpan<int> flowAccumulation,
        global::System.ReadOnlySpan<int> riverOrders,
        global::System.ReadOnlySpan<int> featureFlags) =>
        IsMicrotriangleSpan(waterKindCodes) &&
        IsMicrotriangleSpan(waterDepths) &&
        IsMicrotriangleSpan(waterBodyIds) &&
        IsMicrotriangleSpan(drainageBasinIds) &&
        IsMicrotriangleSpan(flowEdges) &&
        IsMicrotriangleSpan(flowAccumulation) &&
        IsMicrotriangleSpan(riverOrders) &&
        IsMicrotriangleSpan(featureFlags);

    internal bool TryGetCell(
        int q,
        int r,
        out int baseTerrainId,
        out int superregionId,
        out int continentId)
    {
        if (!CoordinateWithinCapacity(q, r))
        {
            baseTerrainId = 0;
            superregionId = 0;
            continentId = 0;
            return false;
        }

        var cellIndex = CellIndex(q, r);
        if (!_installedCells[cellIndex])
        {
            baseTerrainId = 0;
            superregionId = 0;
            continentId = 0;
            return false;
        }

        baseTerrainId = _baseTerrainIds[cellIndex];
        superregionId = _superregionIds[cellIndex];
        continentId = _continentIds[cellIndex];
        return true;
    }

    internal int DominantMaterialId(int q, int r) => CellValue(_dominantMaterialIds, q, r);

    internal int MeanElevation(int q, int r) => CellValue(_meanElevations, q, r);

    internal int MinElevation(int q, int r) => CellValue(_minElevations, q, r);

    internal int MaxElevation(int q, int r) => CellValue(_maxElevations, q, r);

    internal int Relief(int q, int r) => CellValue(_relief, q, r);

    internal int Roughness(int q, int r) => CellValue(_roughness, q, r);

    internal int MeanTemperature(int q, int r) => CellValue(_meanTemperatures, q, r);

    internal int MeanMoisture(int q, int r) => CellValue(_meanMoisture, q, r);

    internal int MeanGeothermal(int q, int r) => CellValue(_meanGeothermal, q, r);

    internal int LandCoverage(int q, int r) => CellValue(_landCoverage, q, r);

    internal int WaterCoverage(int q, int r) => CellValue(_waterCoverage, q, r);

    internal int OceanCoverage(int q, int r) => CellValue(_oceanCoverage, q, r);

    internal int LakeCoverage(int q, int r) => CellValue(_lakeCoverage, q, r);

    internal int PermanentIceCoverage(int q, int r) => CellValue(_permanentIceCoverage, q, r);

    internal int RiverCoverage(int q, int r) => CellValue(_riverCoverage, q, r);

    internal int PeakCount(int q, int r) => CellValue(_peakCounts, q, r);

    internal int RiverEdgeMask(int q, int r) => CellValue(_riverEdgeMasks, q, r);

    internal int CoastEdgeMask(int q, int r) => CellValue(_coastEdgeMasks, q, r);

    internal int RidgeEdgeMask(int q, int r) => CellValue(_ridgeEdgeMasks, q, r);

    internal int CliffEdgeMask(int q, int r) => CellValue(_cliffEdgeMasks, q, r);

    internal int PassEdgeMask(int q, int r) => CellValue(_passEdgeMasks, q, r);

    internal int MicrotriangleMaterialId(int q, int r, int triangleIndex) =>
        MicrotriangleValue(_microtriangleMaterialIds, q, r, triangleIndex);

    internal int MicrotriangleElevation(int q, int r, int triangleIndex) =>
        MicrotriangleValue(_microtriangleElevations, q, r, triangleIndex);

    internal int MicrotriangleLandformCode(int q, int r, int triangleIndex) =>
        MicrotriangleValue(_microtriangleLandformCodes, q, r, triangleIndex);

    internal int MicrotriangleTemperature(int q, int r, int triangleIndex) =>
        MicrotriangleValue(_microtriangleTemperatures, q, r, triangleIndex);

    internal int MicrotriangleMoisture(int q, int r, int triangleIndex) =>
        MicrotriangleValue(_microtriangleMoisture, q, r, triangleIndex);

    internal int MicrotriangleGeothermal(int q, int r, int triangleIndex) =>
        MicrotriangleValue(_microtriangleGeothermal, q, r, triangleIndex);

    internal int MicrotriangleWaterKindCode(int q, int r, int triangleIndex) =>
        MicrotriangleValue(_microtriangleWaterKindCodes, q, r, triangleIndex);

    internal int MicrotriangleWaterDepth(int q, int r, int triangleIndex) =>
        MicrotriangleValue(_microtriangleWaterDepths, q, r, triangleIndex);

    internal int MicrotriangleWaterBodyId(int q, int r, int triangleIndex) =>
        MicrotriangleValue(_microtriangleWaterBodyIds, q, r, triangleIndex);

    internal int MicrotriangleDrainageBasinId(int q, int r, int triangleIndex) =>
        MicrotriangleValue(_microtriangleDrainageBasinIds, q, r, triangleIndex);

    internal int MicrotriangleFlowEdge(int q, int r, int triangleIndex) =>
        MicrotriangleValue(_microtriangleFlowEdges, q, r, triangleIndex);

    internal int MicrotriangleFlowAccumulation(int q, int r, int triangleIndex) =>
        MicrotriangleValue(_microtriangleFlowAccumulation, q, r, triangleIndex);

    internal int MicrotriangleRiverOrder(int q, int r, int triangleIndex) =>
        MicrotriangleValue(_microtriangleRiverOrders, q, r, triangleIndex);

    internal int MicrotriangleFeatureFlags(int q, int r, int triangleIndex) =>
        MicrotriangleValue(_microtriangleFeatureFlags, q, r, triangleIndex);

    internal void WriteSnapshot(global::System.IO.BinaryWriter writer) =>
        KernelSnapshotBinary.WriteObject(writer, this);

    internal void ReadSnapshot(global::System.IO.BinaryReader reader) =>
        KernelSnapshotBinary.ReadObject(reader, this);

    private int CellValue(int[] values, int q, int r)
    {
        var cellIndex = RequiredCellIndex(q, r);
        return values[cellIndex];
    }

    private int MicrotriangleValue(int[] values, int q, int r, int triangleIndex)
    {
        if ((uint)triangleIndex >= PhysicalGeographyCodes.MicrotrianglesPerHex)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(triangleIndex));
        }

        var cellIndex = RequiredCellIndex(q, r);
        return values[(cellIndex * PhysicalGeographyCodes.MicrotrianglesPerHex) + triangleIndex];
    }

    private static bool IsMicrotriangleSpan(global::System.ReadOnlySpan<int> values) =>
        values.Length == PhysicalGeographyCodes.MicrotrianglesPerHex;

    private int RequiredCellIndex(int q, int r)
    {
        if (!CoordinateWithinCapacity(q, r))
        {
            throw new global::System.InvalidOperationException();
        }

        var cellIndex = CellIndex(q, r);
        if (!_installedCells[cellIndex])
        {
            throw new global::System.InvalidOperationException();
        }

        return cellIndex;
    }

    private bool CoordinateWithinCapacity(int q, int r) =>
        q >= -_radiusCapacity && q <= _radiusCapacity &&
        r >= -_radiusCapacity && r <= _radiusCapacity;

    private int CellIndex(int q, int r) =>
        checked(((q + _radiusCapacity) * _diameter) + r + _radiusCapacity);

    private static int HexDistanceFromOrigin(int q, int r) =>
        (global::System.Math.Abs(q) + global::System.Math.Abs(r) + global::System.Math.Abs(q + r)) / 2;
}
