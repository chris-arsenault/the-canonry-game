namespace Engine.WorldGen.Tests.Map;

using Engine.Constants;

public sealed class PhysicalMacroLayerTests
{
    [Fact]
    public void ContinentalDomains_CreateMacroScaleContrastAndRejectUniformPotential()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<ContinentalDomainLayer>();
        Assert.True(result.Map.ContinentalPotential.ToArray().Max() -
            result.Map.ContinentalPotential.ToArray().Min() >= 600);

        result.Map.ContinentalPotential.Fill(500);
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void TectonicUplift_ConfinesRidgesToContinentalDomainsAndRejectsOceanRidge()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<TectonicUpliftLayer>();
        Assert.Contains(result.Map.RidgeInfluence.ToArray(), value => value > 0.25f);
        var deepOcean = FindTriangle(result.Map, index => result.Map.ContinentalPotential[index] < 250);
        result.Map.RidgeInfluence[deepOcean] = 0.5f;

        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void OceanConnectivity_FloodsOnlyBoundaryConnectedLowGroundAndRejectsSeaLevelContradiction()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<OceanConnectivityLayer>();
        var ocean = OceanTriangles(result.Map);
        Assert.NotEmpty(ocean);
        Assert.True(ReachesWorkBoundary(result.Map, ocean));
        Assert.All(ocean, index => Assert.True(result.Map.Elevation[index] < result.Map.EffectiveSeaLevel));

        result.Map.Elevation[ocean[0]] = result.Map.EffectiveSeaLevel;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void FluvialErosion_CutsAncestralDrainageWithoutRaisingLandAndRejectsInvalidRelief()
    {
        var start = PhysicalGeographyLayerTestHarness.GenerateThrough<OceanConnectivityLayer>();
        var before = start.Map.Elevation.ToArray();
        var layer = new FluvialErosionLayer();
        var pipeline = new PhysicalGeographyPipeline(
            PhysicalGeographyLayerTestHarness.Profile,
            [layer]);
        _ = pipeline.ApplyLayer(layer, start.Map, PhysicalGeographyLayerTestHarness.Seed);

        Assert.Contains(Enumerable.Range(0, before.Length), index =>
            start.Map.WaterKindCodes[index] != PhysicalWaterKindCodes.Ocean &&
            start.Map.Elevation[index] < before[index]);
        Assert.DoesNotContain(Enumerable.Range(0, before.Length), index =>
            start.Map.Elevation[index] > before[index]);

        start.Map.Elevation[0] = 2001;
        var context = new PhysicalGeographyGenerationContext(
            PhysicalGeographyLayerTestHarness.Seed,
            PhysicalGeographyLayerTestHarness.Profile,
            layer.Contract);
        Assert.Throws<InvalidOperationException>(() => layer.Validate(start.Map, context));
    }

    [Fact]
    public void Bathymetry_SeparatesShelfAndDeepOceanAndRejectsLandZoneForOcean()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<BathymetryLayer>();
        var ocean = OceanTriangles(result.Map);
        Assert.Contains(ocean, index =>
            result.Map.BathymetricZoneCodes[index] == PhysicalBathymetricZoneCodes.Shelf);
        Assert.Contains(ocean, index =>
            result.Map.BathymetricZoneCodes[index] != PhysicalBathymetricZoneCodes.Land);

        result.Map.BathymetricZoneCodes[ocean[0]] = PhysicalBathymetricZoneCodes.Land;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void Landmasses_LabelConnectedDryComponentsAndRejectMissingClassification()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<LandmassLayer>();
        var dry = FindTriangle(result.Map, index =>
            result.Map.WaterKindCodes[index] != PhysicalWaterKindCodes.Ocean);
        Assert.True(result.Map.LandmassIds[dry] > 0);
        Assert.True(result.Map.ContinentalGroupIds[dry] > 0);
        AssertAdjacentDryTrianglesShareLandmass(result.Map);

        result.Map.LandmassIds[dry] = 0;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void Fractures_CreateBranchingConductanceDistinctFromRidgesAndRejectFlatField()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<FractureNetworkLayer>();
        Assert.Contains(Enumerable.Range(0, result.Map.Grid.TriangleCount), index =>
            result.Map.FractureConductance[index] >= 560 && result.Map.RidgeInfluence[index] < 0.2f);

        result.Map.FractureConductance.Fill(
            PhysicalGeographyLayerTestHarness.Profile.FractureConductanceFloor);
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void Geothermal_ProducesVentsFromFracturesAndRejectsOutOfRangeIntensity()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<GeothermalLayer>();
        Assert.Contains(result.Map.FeatureFlags.ToArray(), flags =>
            (flags & PhysicalGeographyFeatureCodes.GeothermalVent) != 0);
        Assert.Contains(Enumerable.Range(0, result.Map.Grid.TriangleCount), index =>
            result.Map.Geothermal[index] >= 700 && result.Map.FractureConductance[index] >= 260);

        result.Map.Geothermal[0] = 1001;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    private static int FindTriangle(PhysicalGeographyField map, Func<int, bool> predicate) =>
        Enumerable.Range(0, map.Grid.TriangleCount).First(predicate);

    private static int[] OceanTriangles(PhysicalGeographyField map) =>
        Enumerable.Range(0, map.Grid.TriangleCount)
            .Where(index => map.WaterKindCodes[index] == PhysicalWaterKindCodes.Ocean)
            .ToArray();

    private static bool ReachesWorkBoundary(PhysicalGeographyField map, IReadOnlyCollection<int> ocean) =>
        ocean.Any(index => map.Grid.HexIsOnWorkBoundary(map.Grid.TriangleHexIndex(index)));

    private static void AssertAdjacentDryTrianglesShareLandmass(PhysicalGeographyField map)
    {
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = map.Grid.NeighborTriangleIndex(triangleIndex, edge);
                if (neighbor >= 0 &&
                    map.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.Ocean &&
                    map.WaterKindCodes[neighbor] != PhysicalWaterKindCodes.Ocean)
                {
                    Assert.Equal(map.LandmassIds[triangleIndex], map.LandmassIds[neighbor]);
                }
            }
        }
    }
}
