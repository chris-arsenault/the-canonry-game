namespace Engine.WorldGen.Tests.Map;

using Engine.Constants;

public sealed class PhysicalMacroLayerTests
{
    [Fact]
    public void CrustIsostasy_SeparatesOceanicAndContinentalFlotationWithoutATypeStep()
    {
        Assert.InRange(
            PhysicalCrust.HeightUnits(PhysicalCrust.OceanicThicknessUnits, 0d),
            -201d,
            -199d);
        Assert.Equal(
            200d,
            PhysicalCrust.HeightUnits(PhysicalCrust.ContinentalThicknessUnits, 1d));
        Assert.Equal(3000d, PhysicalCrust.HeightUnits(8000d, 1d));
        Assert.Equal(
            0d,
            PhysicalCrust.HeightUnits(
                (PhysicalCrust.OceanicThicknessUnits +
                    PhysicalCrust.ContinentalThicknessUnits) / 2d,
                0.5d));

        var oceanwardFraction = 0.49d;
        var landwardFraction = 0.51d;
        var oceanwardThickness = PhysicalCrust.OceanicThicknessUnits +
            ((PhysicalCrust.ContinentalThicknessUnits - PhysicalCrust.OceanicThicknessUnits) *
                oceanwardFraction);
        var landwardThickness = PhysicalCrust.OceanicThicknessUnits +
            ((PhysicalCrust.ContinentalThicknessUnits - PhysicalCrust.OceanicThicknessUnits) *
                landwardFraction);
        var typeBoundaryStep = PhysicalCrust.HeightUnits(landwardThickness, landwardFraction) -
            PhysicalCrust.HeightUnits(oceanwardThickness, oceanwardFraction);

        Assert.InRange(typeBoundaryStep, 0d, 100d);
    }

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
        Assert.Contains(result.Map.CrustContinentalFraction.ToArray(), value =>
            value is > 0 and < 1000);
        Assert.All(
            Enumerable.Range(0, result.Map.Grid.TriangleCount)
                .Where(index => result.Map.TectonicBoundaryCodes[index] !=
                    PhysicalTectonicBoundaryCodes.None),
            index => Assert.InRange(
                Math.Sqrt(
                    Math.Pow(result.Map.TectonicTangentX[index] / 1000d, 2d) +
                    Math.Pow(result.Map.TectonicTangentY[index] / 1000d, 2d)),
                0.99d,
                1.01d));

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
    public void ContinentalSutures_UseTransportedMaterialAssemblies()
    {
        var map = new PhysicalGeographyField(new PhysicalGeographyGrid(9, 9));
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            map.CrustContinentalFraction[triangleIndex] = 1000;
            map.CrustAssemblyIds[triangleIndex] = map.Grid.TriangleX(triangleIndex) < 0d ? 1 : 2;
            map.RockResistance[triangleIndex] = PhysicalRock.BaselineResistance;
        }

        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (Enumerable.Range(0, 3)
                .Select(edge => map.Grid.NeighborTriangleIndex(triangleIndex, edge))
                .Any(neighbor => neighbor >= 0 &&
                    map.CrustAssemblyIds[neighbor] != map.CrustAssemblyIds[triangleIndex]))
            {
                map.TectonicBoundaryCodes[triangleIndex] =
                    PhysicalTectonicBoundaryCodes.Convergent;
                map.TectonicStrain[triangleIndex] = 900;
            }
        }

        var layer = new ContinentalSutureLayer();
        var context = new PhysicalGeographyGenerationContext(
            PhysicalGeographyLayerTestHarness.Seed,
            PhysicalGeographyLayerTestHarness.Profile,
            layer.Contract);

        layer.Apply(map, context);
        layer.Validate(map, context);

        var sutures = Enumerable.Range(0, map.Grid.TriangleCount)
            .Where(index => map.CrustSutureStrength[index] > 0)
            .ToArray();
        Assert.NotEmpty(sutures);
        Assert.All(sutures, index => Assert.Equal((1 << 16) | 2, map.CrustSuturePairIds[index]));
        Assert.All(sutures, index => Assert.True(map.CrustCollisionIntensity[index] > 0));
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
    public void TectonicUplift_FollowsMaterialContinentalSuturesAndRejectsInvalidInfluence()
    {
        var start = PhysicalGeographyLayerTestHarness.GenerateThrough<ContinentalDomainLayer>();
        var suture = Enumerable.Range(0, start.Map.Grid.TriangleCount)
            .First(start.Map.Grid.TriangleIsInsideContentDomain);
        start.Map.CrustSuturePairIds[suture] = (1 << 16) | 2;
        start.Map.CrustSutureStrength[suture] = 800;
        var layer = new TectonicUpliftLayer();
        var pipeline = new PhysicalGeographyPipeline(
            PhysicalGeographyLayerTestHarness.Profile,
            [layer]);

        _ = pipeline.ApplyLayer(layer, start.Map, PhysicalGeographyLayerTestHarness.Seed);

        Assert.True(start.Map.RidgeInfluence[suture] > 0.7f);
        start.Map.RidgeInfluence[0] = 1.1f;
        var context = new PhysicalGeographyGenerationContext(
            PhysicalGeographyLayerTestHarness.Seed,
            PhysicalGeographyLayerTestHarness.Profile,
            layer.Contract);

        Assert.Throws<InvalidOperationException>(() =>
            layer.Validate(start.Map, context));
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
    public void OceanConnectivity_CalibratesItsDatumOnceInsteadOfChasingTerrainEdits()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<OceanConnectivityLayer>();
        var baseSeaLevel = result.Map.BaseSeaLevel;
        var effectiveSeaLevel = result.Map.EffectiveSeaLevel;
        var dry = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .First(index => result.Map.WaterKindCodes[index] == PhysicalWaterKindCodes.None);
        result.Map.BedrockElevation[dry] += PhysicalElevation.FromUnits(25);
        LandscapeSurface.Rebuild(result.Map, dry);

        result.Layer.Apply(result.Map, result.Context);

        Assert.True(result.Map.SeaLevelInitialized);
        Assert.Equal(baseSeaLevel, result.Map.BaseSeaLevel);
        Assert.Equal(effectiveSeaLevel, result.Map.EffectiveSeaLevel);
    }

    [Fact]
    public void ContinentalMargins_LeaveLandCrustAloneAndReachEveryConnectedOceanTriangle()
    {
        var start = PhysicalGeographyLayerTestHarness.GenerateThrough<OceanConnectivityLayer>();
        var thicknessBefore = start.Map.CrustThickness.ToArray();
        var layer = new ContinentalMarginEvolutionLayer();
        var pipeline = new PhysicalGeographyPipeline(
            PhysicalGeographyLayerTestHarness.Profile,
            [layer]);

        _ = pipeline.ApplyLayer(layer, start.Map, PhysicalGeographyLayerTestHarness.Seed);

        Assert.All(
            Enumerable.Range(0, start.Map.Grid.TriangleCount)
                .Where(index => start.Map.WaterKindCodes[index] == PhysicalWaterKindCodes.None),
            index => Assert.Equal(thicknessBefore[index], start.Map.CrustThickness[index]));
        Assert.All(
            Enumerable.Range(0, start.Map.Grid.TriangleCount)
                .Where(index => start.Map.WaterKindCodes[index] == PhysicalWaterKindCodes.Ocean),
            index => Assert.True(start.Map.OceanMarginDistance[index] > 0));
    }

    [Fact]
    public void OrogenicConstruction_BuildsRootedResistantSurfaceRelief()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<OrogenicConstructionLayer>();
        var constructed = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .Where(index => result.Map.SurfaceRockLoad[index] > 0 &&
                result.Map.OrogenicComponentIds[index] >= 0)
            .ToArray();

        Assert.NotEmpty(constructed);
        Assert.Contains(constructed, index =>
            result.Map.RockResistance[index] > PhysicalRock.BaselineResistance);
        Assert.Contains(constructed, index => result.Map.OrogenicConstruction[index] > 0);
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

    [Fact]
    public void MantlePlumeRoots_AreStableMidplateSourcesAndRejectInvalidStrength()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<MantlePlumeRootLayer>();
        var roots = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .Where(index => result.Map.MantlePlumeRootStrength[index] > 0)
            .ToArray();

        Assert.NotEmpty(roots);
        Assert.All(roots, index =>
            Assert.True(result.Map.Grid.TriangleIsInsideContentDomain(index)));
        result.Map.MantlePlumeRootStrength[roots[0]] = 1001;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void VolcanicConstruction_BuildsReliefWithPhysicalProvenanceAndRejectsInvalidState()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<MantlePlumeRootLayer>();
        var thicknessBefore = result.Map.CrustThickness.ToArray();
        var surfaceLoadBefore = result.Map.SurfaceRockLoad.ToArray();
        var layer = new VolcanicConstructionLayer();
        var pipeline = new PhysicalGeographyPipeline(
            PhysicalGeographyLayerTestHarness.Profile,
            [layer]);
        _ = pipeline.ApplyLayer(
            layer,
            result.Map,
            PhysicalGeographyLayerTestHarness.Seed);
        var constructed = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .First(index => result.Map.VolcanicActivity[index] > 0);

        Assert.True(result.Map.VolcanicConstructionHistory[constructed] > 0);
        Assert.Contains(
            Enumerable.Range(0, result.Map.Grid.TriangleCount),
            index => result.Map.VolcanicConstructionHistory[index] > 0 &&
                result.Map.SurfaceRockLoad[index] > surfaceLoadBefore[index]);
        Assert.All(
            Enumerable.Range(0, result.Map.Grid.TriangleCount),
            index => Assert.Equal(thicknessBefore[index], result.Map.CrustThickness[index]));
        Assert.Contains(result.Map.FeatureFlags.ToArray(), flags =>
            (flags & PhysicalGeographyFeatureCodes.VolcanicCone) != 0);
        Assert.Contains(result.Map.FeatureFlags.ToArray(), flags =>
            (flags & PhysicalGeographyFeatureCodes.LavaPlain) != 0);

        var history = result.Map.VolcanicConstructionHistory[constructed];
        result.Map.VolcanicConstructionHistory[constructed] = -1;
        Assert.Throws<InvalidOperationException>(() => layer.Validate(
            result.Map,
            new PhysicalGeographyGenerationContext(
                PhysicalGeographyLayerTestHarness.Seed,
                PhysicalGeographyLayerTestHarness.Profile,
                layer.Contract)));

        result.Map.VolcanicConstructionHistory[constructed] = history;
        result.Map.Elevation[constructed] = PhysicalElevation.FromUnits(2001);
        Assert.Throws<InvalidOperationException>(() => layer.Validate(
            result.Map,
            new PhysicalGeographyGenerationContext(
                PhysicalGeographyLayerTestHarness.Seed,
                PhysicalGeographyLayerTestHarness.Profile,
                layer.Contract)));
    }

    private static int FindTriangle(PhysicalGeographyField map, Func<int, bool> predicate) =>
        Enumerable.Range(0, map.Grid.TriangleCount).First(predicate);

    private static int[] OceanTriangles(PhysicalGeographyField map) =>
        Enumerable.Range(0, map.Grid.TriangleCount)
            .Where(index => map.WaterKindCodes[index] == PhysicalWaterKindCodes.Ocean)
            .ToArray();

    private static bool ReachesWorkBoundary(PhysicalGeographyField map, IReadOnlyCollection<int> ocean) =>
        ocean.Any(map.Grid.TriangleIsOnWorkBoundary);

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
