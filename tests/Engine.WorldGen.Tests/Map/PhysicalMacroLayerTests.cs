namespace Engine.WorldGen.Tests.Map;

using Engine.Constants;

public sealed class PhysicalMacroLayerTests
{
    [Fact]
    public void TectonicProvinces_CreateTypedMovingCrustAndRejectUnknownBoundary()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<TectonicProvinceLayer>();
        Assert.True(result.Map.CrustProvinceIds.ToArray().Distinct().Count() >= 4);
        Assert.Contains(result.Map.CrustTypeCodes.ToArray(), code =>
            code == PhysicalCrustTypeCodes.Continental);
        Assert.Contains(result.Map.CrustTypeCodes.ToArray(), code =>
            code == PhysicalCrustTypeCodes.Oceanic);
        Assert.Contains(result.Map.TectonicBoundaryCodes.ToArray(), code =>
            code != PhysicalTectonicBoundaryCodes.None);

        result.Map.TectonicBoundaryCodes[0] = PhysicalTectonicBoundaryCodes.Max + 1;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void TectonicProvinces_KeepHeartLandWhileDistributingAssembliesAcrossWorkDomain()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<TectonicProvinceLayer>();
        var continental = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .Where(index => result.Map.CrustTypeCodes[index] == PhysicalCrustTypeCodes.Continental)
            .ToArray();
        var closest = continental.MinBy(index => DistanceSquaredFromOrigin(result.Map, index));
        var farthest = continental.MaxBy(index => DistanceSquaredFromOrigin(result.Map, index));

        Assert.True(DistanceSquaredFromOrigin(result.Map, closest) < 16d);
        Assert.True(DistanceSquaredFromOrigin(result.Map, farthest) > 100d);
        Assert.Equal(
            PhysicalGeographyLayerTestHarness.Profile.ContinentalAssemblyCount,
            continental.Select(index => result.Map.CrustAssemblyIds[index]).Distinct().Count());
    }

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
    public void TectonicUplift_FollowsConvergentBoundariesAndRejectsInvalidInfluence()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<TectonicUpliftLayer>();
        Assert.Contains(Enumerable.Range(0, result.Map.Grid.TriangleCount), index =>
            result.Map.TectonicBoundaryCodes[index] == PhysicalTectonicBoundaryCodes.Convergent &&
            result.Map.RidgeInfluence[index] > 0.15f);
        result.Map.RidgeInfluence[0] = 1.1f;

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
            TriangleIsDry(result.Map, index));
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
            (result.Map.FeatureFlags[index] & PhysicalGeographyFeatureCodes.GeothermalVent) != 0 &&
            (result.Map.TectonicStrain[index] >= 180 ||
                result.Map.FractureConductance[index] >= 560 ||
                (result.Map.FeatureFlags[index] & PhysicalGeographyFeatureCodes.MantlePlume) != 0));

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
                if (neighbor >= 0 && TriangleIsDry(map, triangleIndex) && TriangleIsDry(map, neighbor))
                {
                    Assert.Equal(map.LandmassIds[triangleIndex], map.LandmassIds[neighbor]);
                }
            }
        }
    }

    private static bool TriangleIsDry(PhysicalGeographyField map, int triangleIndex) =>
        map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.None &&
        map.Elevation[triangleIndex] >= map.EffectiveSeaLevel;

    private static double DistanceSquaredFromOrigin(PhysicalGeographyField map, int triangleIndex)
    {
        var x = map.Grid.TriangleX(triangleIndex);
        var y = map.Grid.TriangleY(triangleIndex);
        return (x * x) + (y * y);
    }
}
