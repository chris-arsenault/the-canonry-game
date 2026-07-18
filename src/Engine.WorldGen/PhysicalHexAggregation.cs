namespace Engine.WorldGen;

using Engine.Constants;

public static class PhysicalHexAggregator
{
    public static PhysicalHexAggregate Summarize(
        PhysicalGeographyField field,
        int hexIndex,
        ReadOnlySpan<int> materialIndexes,
        int materialCount,
        int cliffSlopeThreshold)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(materialCount);
        ArgumentOutOfRangeException.ThrowIfNegative(cliffSlopeThreshold);
        if ((uint)hexIndex >= (uint)field.Grid.HexCount ||
            materialIndexes.Length != field.Grid.TriangleCount)
        {
            throw new ArgumentOutOfRangeException(nameof(hexIndex));
        }

        Span<int> materialCounts = materialCount <= 64
            ? stackalloc int[materialCount]
            : new int[materialCount];
        var builder = new AggregateBuilder();
        for (var localTriangle = 0;
            localTriangle < PhysicalGeographyCodes.MicrotrianglesPerHex;
            localTriangle++)
        {
            var triangleIndex = field.Grid.TriangleIndex(hexIndex, localTriangle);
            var materialIndex = materialIndexes[triangleIndex];
            if ((uint)materialIndex >= (uint)materialCount)
            {
                throw new InvalidOperationException("Physical material index is outside its palette.");
            }

            materialCounts[materialIndex]++;
            builder.AddTriangle(field, hexIndex, triangleIndex, localTriangle, cliffSlopeThreshold);
        }

        builder.DominantMaterialIndex = FindDominantMaterial(materialCounts);
        builder.MeasureRoughness(field, hexIndex);
        return builder.Build();
    }

    private static int FindDominantMaterial(ReadOnlySpan<int> materialCounts)
    {
        var bestIndex = 0;
        var bestCount = materialCounts[0];
        for (var materialIndex = 1; materialIndex < materialCounts.Length; materialIndex++)
        {
            if (materialCounts[materialIndex] > bestCount)
            {
                bestIndex = materialIndex;
                bestCount = materialCounts[materialIndex];
            }
        }

        return bestIndex;
    }

    private struct AggregateBuilder
    {
        private int _coastEdgeMask;
        private int _cliffEdgeMask;
        private int _elevationTotal;
        private int _geothermalTotal;
        private int _lakeCoverage;
        private int _maxElevation;
        private int _minElevation;
        private int _moistureTotal;
        private int _oceanCoverage;
        private int _passEdgeMask;
        private int _peakCount;
        private int _permanentIceCoverage;
        private int _ridgeEdgeMask;
        private int _riverCoverage;
        private int _riverEdgeMask;
        private int _roughness;
        private int _temperatureTotal;

        public int DominantMaterialIndex { get; set; }

        public void AddTriangle(
            PhysicalGeographyField field,
            int hexIndex,
            int triangleIndex,
            int localTriangle,
            int cliffSlopeThreshold)
        {
            var elevation = field.Elevation[triangleIndex];
            _elevationTotal += elevation;
            _temperatureTotal += field.Temperature[triangleIndex];
            _moistureTotal += field.Moisture[triangleIndex];
            _geothermalTotal += field.Geothermal[triangleIndex];
            _minElevation = localTriangle == 0 ? elevation : Math.Min(_minElevation, elevation);
            _maxElevation = localTriangle == 0 ? elevation : Math.Max(_maxElevation, elevation);
            CountFeatures(field, triangleIndex);
            AddOuterEdgeFlags(field, hexIndex, triangleIndex, localTriangle, cliffSlopeThreshold);
        }

        public void MeasureRoughness(PhysicalGeographyField field, int hexIndex)
        {
            var total = 0;
            var edges = 0;
            for (var localTriangle = 0;
                localTriangle < PhysicalGeographyCodes.MicrotrianglesPerHex;
                localTriangle++)
            {
                var triangleIndex = field.Grid.TriangleIndex(hexIndex, localTriangle);
                for (var edge = 0; edge < 3; edge++)
                {
                    var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
                    if (neighbor > triangleIndex && field.Grid.TriangleHexIndex(neighbor) == hexIndex)
                    {
                        total += Math.Abs(field.Elevation[triangleIndex] - field.Elevation[neighbor]);
                        edges++;
                    }
                }
            }

            _roughness = edges == 0 ? 0 : total / edges;
        }

        public readonly PhysicalHexAggregate Build()
        {
            const int count = PhysicalGeographyCodes.MicrotrianglesPerHex;
            var waterCoverage = _oceanCoverage + _lakeCoverage;
            return new PhysicalHexAggregate(
                DominantMaterialIndex,
                _elevationTotal / count,
                _minElevation,
                _maxElevation,
                _maxElevation - _minElevation,
                _roughness,
                _temperatureTotal / count,
                _moistureTotal / count,
                _geothermalTotal / count,
                count - waterCoverage,
                waterCoverage,
                _oceanCoverage,
                _lakeCoverage,
                _permanentIceCoverage,
                _riverCoverage,
                _peakCount,
                _riverEdgeMask,
                _coastEdgeMask,
                _ridgeEdgeMask,
                _cliffEdgeMask,
                _passEdgeMask);
        }

        private void CountFeatures(PhysicalGeographyField field, int triangleIndex)
        {
            _oceanCoverage +=
                field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean ? 1 : 0;
            _lakeCoverage +=
                field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Lake ? 1 : 0;
            _permanentIceCoverage += IsPermanentIce(field.FeatureFlags[triangleIndex]) ? 1 : 0;
            _riverCoverage += field.RiverOrders[triangleIndex] > 0 ? 1 : 0;
            _peakCount += field.LandformCodes[triangleIndex] == PhysicalLandformCodes.Peak ? 1 : 0;
        }

        private void AddOuterEdgeFlags(
            PhysicalGeographyField field,
            int hexIndex,
            int triangleIndex,
            int localTriangle,
            int cliffSlopeThreshold)
        {
            var sectorLocal = localTriangle % PhysicalGeographyCodes.MicrotrianglesPerSector;
            if (sectorLocal is not (1 or 2))
            {
                return;
            }

            const int outerEdge = 2;
            var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, outerEdge);
            if (neighbor < 0 || field.Grid.TriangleHexIndex(neighbor) == hexIndex)
            {
                return;
            }

            var edgeMask = 1 << (localTriangle / PhysicalGeographyCodes.MicrotrianglesPerSector);
            AddWaterAndRiverEdgeFlags(field, triangleIndex, neighbor, edgeMask);
            AddReliefEdgeFlags(field, triangleIndex, neighbor, edgeMask, cliffSlopeThreshold);
        }

        private void AddWaterAndRiverEdgeFlags(
            PhysicalGeographyField field,
            int triangleIndex,
            int neighbor,
            int edgeMask)
        {
            var ocean = field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean;
            var neighborOcean = field.WaterKindCodes[neighbor] == PhysicalWaterKindCodes.Ocean;
            if (ocean != neighborOcean)
            {
                _coastEdgeMask |= edgeMask;
            }

            if ((field.FlowTargets[triangleIndex] == neighbor && field.RiverOrders[triangleIndex] > 0) ||
                (field.FlowTargets[neighbor] == triangleIndex && field.RiverOrders[neighbor] > 0))
            {
                _riverEdgeMask |= edgeMask;
            }
        }

        private void AddReliefEdgeFlags(
            PhysicalGeographyField field,
            int triangleIndex,
            int neighbor,
            int edgeMask,
            int cliffSlopeThreshold)
        {
            if (IsRidge(field.LandformCodes[triangleIndex]) || IsRidge(field.LandformCodes[neighbor]))
            {
                _ridgeEdgeMask |= edgeMask;
            }

            if (Math.Abs(field.Elevation[triangleIndex] - field.Elevation[neighbor]) >= cliffSlopeThreshold)
            {
                _cliffEdgeMask |= edgeMask;
            }

            if (field.LandformCodes[triangleIndex] == PhysicalLandformCodes.Pass ||
                field.LandformCodes[neighbor] == PhysicalLandformCodes.Pass)
            {
                _passEdgeMask |= edgeMask;
            }
        }

        private static bool IsPermanentIce(int featureFlags) =>
            (featureFlags &
                (PhysicalGeographyFeatureCodes.PermanentSeaIce |
                    PhysicalGeographyFeatureCodes.PermanentIceMargin)) != 0;

        private static bool IsRidge(int landformCode) =>
            landformCode is PhysicalLandformCodes.Ridge or PhysicalLandformCodes.Peak;
    }
}

public readonly record struct PhysicalHexAggregate(
    int DominantMaterialIndex,
    int MeanElevation,
    int MinElevation,
    int MaxElevation,
    int Relief,
    int Roughness,
    int MeanTemperature,
    int MeanMoisture,
    int MeanGeothermal,
    int LandCoverage,
    int WaterCoverage,
    int OceanCoverage,
    int LakeCoverage,
    int PermanentIceCoverage,
    int RiverCoverage,
    int PeakCount,
    int RiverEdgeMask,
    int CoastEdgeMask,
    int RidgeEdgeMask,
    int CliffEdgeMask,
    int PassEdgeMask);
