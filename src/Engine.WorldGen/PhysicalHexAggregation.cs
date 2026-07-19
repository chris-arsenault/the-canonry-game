namespace Engine.WorldGen;

using Engine.Constants;

public static class PhysicalHexAggregator
{
    public static PhysicalHexAggregate Summarize(
        PhysicalGeographyField field,
        int hexIndex,
        ReadOnlySpan<int> materialIndexes,
        int materialCount,
        ReadOnlySpan<int> surfaceIndexes,
        int surfaceCount,
        ReadOnlySpan<ulong> significantFeatureFlags,
        ReadOnlySpan<int> fireIntensity,
        ReadOnlySpan<int> auroraIntensity,
        ReadOnlySpan<int> pressureIntensity,
        int cliffSlopeThreshold)
    {
        ArgumentNullException.ThrowIfNull(field);
        ValidateHexIndex(field, hexIndex);
        ValidatePaletteCount(materialCount, nameof(materialCount));
        ValidatePaletteCount(surfaceCount, nameof(surfaceCount));
        ArgumentOutOfRangeException.ThrowIfNegative(cliffSlopeThreshold);
        ValidateSpan(materialIndexes.Length, field.Grid.TriangleCount, nameof(materialIndexes));
        ValidateSpan(surfaceIndexes.Length, field.Grid.TriangleCount, nameof(surfaceIndexes));
        ValidateSpan(significantFeatureFlags.Length, field.Grid.TriangleCount, nameof(significantFeatureFlags));
        ValidateSpan(fireIntensity.Length, field.Grid.TriangleCount, nameof(fireIntensity));
        ValidateSpan(auroraIntensity.Length, field.Grid.TriangleCount, nameof(auroraIntensity));
        ValidateSpan(pressureIntensity.Length, field.Grid.TriangleCount, nameof(pressureIntensity));

        Span<int> materialCounts = materialCount <= 64
            ? stackalloc int[materialCount]
            : new int[materialCount];
        Span<int> surfaceCounts = surfaceCount <= 64
            ? stackalloc int[surfaceCount]
            : new int[surfaceCount];
        var builder = new AggregateBuilder();
        for (var localTriangle = 0; localTriangle < PhysicalGeographyCodes.MicrotrianglesPerHex; localTriangle++)
        {
            var triangleIndex = field.Grid.TriangleIndex(hexIndex, localTriangle);
            var material = materialIndexes[triangleIndex];
            var surface = surfaceIndexes[triangleIndex];
            if ((uint)material >= (uint)materialCount || (uint)surface >= (uint)surfaceCount)
            {
                throw new InvalidOperationException("Physical palette index is outside its palette.");
            }

            materialCounts[material]++;
            surfaceCounts[surface]++;
            builder.AddTriangle(
                field,
                hexIndex,
                triangleIndex,
                localTriangle,
                significantFeatureFlags[triangleIndex],
                fireIntensity[triangleIndex],
                auroraIntensity[triangleIndex],
                pressureIntensity[triangleIndex],
                cliffSlopeThreshold);
        }

        builder.DominantMaterialIndex = FindDominant(materialCounts);
        builder.PrimarySurfaceIndex = FindDominant(surfaceCounts);
        builder.MeasureRoughness(field, hexIndex);
        return builder.Build();
    }

    private static void ValidateHexIndex(PhysicalGeographyField field, int hexIndex)
    {
        if ((uint)hexIndex >= (uint)field.Grid.HexCount)
        {
            throw new ArgumentOutOfRangeException(nameof(hexIndex));
        }
    }

    private static void ValidatePaletteCount(int count, string parameterName)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void ValidateSpan(int length, int expectedLength, string parameterName)
    {
        if (length != expectedLength)
        {
            throw new ArgumentException("Physical field does not match the triangle grid.", parameterName);
        }
    }

    private static int FindDominant(ReadOnlySpan<int> counts)
    {
        var best = 0;
        for (var index = 1; index < counts.Length; index++)
        {
            if (counts[index] > counts[best])
            {
                best = index;
            }
        }

        return best;
    }

    private struct AggregateBuilder
    {
        private int _auroraTotal;
        private int _coastEdgeMask;
        private int _cliffEdgeMask;
        private int _elevationTotal;
        private int _fireTotal;
        private int _fractureEdgeMask;
        private int _geothermalTotal;
        private int _iceTotal;
        private int _lakeCoverage;
        private int _maxAurora;
        private int _maxElevation;
        private int _maxFire;
        private int _maxIce;
        private int _maxPressure;
        private int _minElevation;
        private int _moistureTotal;
        private int _oceanCoverage;
        private int _passEdgeMask;
        private int _peakCount;
        private int _permanentIceCoverage;
        private int _pressureTotal;
        private int _ridgeEdgeMask;
        private int _riverCoverage;
        private int _floodplainCoverage;
        private int _deltaCoverage;
        private int _wideRiverCoverage;
        private int _riverEdgeMask;
        private int _roughness;
        private ulong _significantFeatureFlags;
        private int _temperatureTotal;

        public int DominantMaterialIndex { get; set; }

        public int PrimarySurfaceIndex { get; set; }

        public void AddTriangle(
            PhysicalGeographyField field,
            int hexIndex,
            int triangleIndex,
            int localTriangle,
            ulong significantFeatureFlags,
            int fireIntensity,
            int auroraIntensity,
            int pressureIntensity,
            int cliffSlopeThreshold)
        {
            var elevation = field.Elevation[triangleIndex];
            var ice = field.IceThickness[triangleIndex];
            _elevationTotal += elevation;
            _temperatureTotal += field.Temperature[triangleIndex];
            _moistureTotal += field.Moisture[triangleIndex];
            _geothermalTotal += field.Geothermal[triangleIndex];
            _iceTotal += ice;
            _fireTotal += fireIntensity;
            _auroraTotal += auroraIntensity;
            _pressureTotal += pressureIntensity;
            _minElevation = localTriangle == 0 ? elevation : Math.Min(_minElevation, elevation);
            _maxElevation = localTriangle == 0 ? elevation : Math.Max(_maxElevation, elevation);
            _maxIce = Math.Max(_maxIce, ice);
            _maxFire = Math.Max(_maxFire, fireIntensity);
            _maxAurora = Math.Max(_maxAurora, auroraIntensity);
            _maxPressure = Math.Max(_maxPressure, pressureIntensity);
            _significantFeatureFlags |= significantFeatureFlags;
            CountFeatures(field, triangleIndex);
            AddOuterEdgeFlags(field, hexIndex, triangleIndex, localTriangle, cliffSlopeThreshold);
        }

        public void MeasureRoughness(PhysicalGeographyField field, int hexIndex)
        {
            var total = 0;
            var edges = 0;
            for (var localTriangle = 0; localTriangle < PhysicalGeographyCodes.MicrotrianglesPerHex; localTriangle++)
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
                PrimarySurfaceIndex,
                _significantFeatureFlags,
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
                _floodplainCoverage,
                _deltaCoverage,
                _wideRiverCoverage,
                _peakCount,
                _iceTotal / count,
                _maxIce,
                _fireTotal / count,
                _maxFire,
                _auroraTotal / count,
                _maxAurora,
                _pressureTotal / count,
                _maxPressure,
                _riverEdgeMask,
                _coastEdgeMask,
                _ridgeEdgeMask,
                _cliffEdgeMask,
                _passEdgeMask,
                _fractureEdgeMask);
        }

        private void CountFeatures(PhysicalGeographyField field, int triangleIndex)
        {
            _oceanCoverage += field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean ? 1 : 0;
            _lakeCoverage += field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Lake ? 1 : 0;
            _permanentIceCoverage += field.IceThickness[triangleIndex] >= 250 ? 1 : 0;
            _riverCoverage += field.RiverChannelStrength[triangleIndex] > 0 ? 1 : 0;
            _floodplainCoverage += field.FloodplainIntensity[triangleIndex] > 0 ? 1 : 0;
            _deltaCoverage += field.DeltaIntensity[triangleIndex] > 0 ? 1 : 0;
            _wideRiverCoverage += field.RiverChannelStrength[triangleIndex] > 0 &&
                field.RiverOrders[triangleIndex] == 0 ? 1 : 0;
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
            AddWaterEdgeFlags(field, triangleIndex, neighbor, edgeMask);
            AddReliefEdgeFlags(field, triangleIndex, neighbor, edgeMask, cliffSlopeThreshold);
            AddFractureEdgeFlag(field, triangleIndex, neighbor, edgeMask);
        }

        private void AddWaterEdgeFlags(
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

        private void AddFractureEdgeFlag(
            PhysicalGeographyField field,
            int triangleIndex,
            int neighbor,
            int edgeMask)
        {
            if (field.FractureConductance[triangleIndex] >= 560 &&
                field.FractureConductance[neighbor] >= 560)
            {
                _fractureEdgeMask |= edgeMask;
            }
        }

        private static bool IsRidge(int landformCode) =>
            landformCode is PhysicalLandformCodes.Ridge or PhysicalLandformCodes.Peak;
    }
}

public readonly record struct PhysicalHexAggregate(
    int DominantMaterialIndex,
    int PrimarySurfaceIndex,
    ulong SignificantFeatureFlags,
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
    int FloodplainCoverage,
    int DeltaCoverage,
    int WideRiverCoverage,
    int PeakCount,
    int MeanIceThickness,
    int MaxIceThickness,
    int MeanFireIntensity,
    int MaxFireIntensity,
    int MeanAuroraIntensity,
    int MaxAuroraIntensity,
    int MeanPressureIntensity,
    int MaxPressureIntensity,
    int RiverEdgeMask,
    int CoastEdgeMask,
    int RidgeEdgeMask,
    int CliffEdgeMask,
    int PassEdgeMask,
    int FractureEdgeMask);
