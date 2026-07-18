namespace IceRemembers.Seed;

using Engine.Constants;
using Engine.Domain.Startup;
using Engine.WorldGen;

using IceRemembers;

internal static partial class IceRemembersSeedBuilder
{
    private readonly record struct PhysicalGeographyBuild(
        HexGrid HeartGrid,
        MapSubdivisionMap HeartSubdivisions,
        DomainPhysicalGeographyBlueprint Blueprint);

    private static PhysicalGeographyBuild CreatePhysicalGeography(int salt)
    {
        var field = PhysicalGeographyGenerator.Generate(
            GenerationRadius,
            salt,
            IceRemembersPhysicalGeography.GenerationProfile);
        var magic = IceRemembersMicrogeographyGenerator.Generate(
            field,
            salt,
            IceRemembersPhysicalGeography.MicrogeographyProfile);
        var materialIndexes = ClassifyPhysicalMaterials(field, magic);
        var aggregates = AggregateConfiguredHexes(field, materialIndexes, magic);
        var subdivisions = CreatePhysicalSubdivisions(field, aggregates, salt);
        var catalog = IceRemembersTerrainCatalog.CreateWorldGenCatalog();
        var fieldGrid = new HexGrid(
            GenerationRadius,
            catalog,
            IceRemembersTerrain.Wastes.ToDomainName());
        var blueprint = BuildPhysicalBlueprint(
            field,
            materialIndexes,
            magic,
            aggregates,
            subdivisions,
            fieldGrid);
        var heart = CropPhysicalHeart(fieldGrid, subdivisions, catalog);
        return new PhysicalGeographyBuild(heart.Grid, heart.Subdivisions, blueprint);
    }

    private static int[] ClassifyPhysicalMaterials(
        PhysicalGeographyField field,
        IceRemembersMicrogeographyMap magic)
    {
        var materialIndexes = new int[field.Grid.TriangleCount];
        for (var configuredIndex = 0;
            configuredIndex < field.Grid.ConfiguredHexCount;
            configuredIndex++)
        {
            var hexIndex = field.Grid.ConfiguredHexIndexAt(configuredIndex);
            for (var localTriangle = 0;
                localTriangle < PhysicalGeographyCodes.MicrotrianglesPerHex;
                localTriangle++)
            {
                var triangleIndex = field.Grid.TriangleIndex(hexIndex, localTriangle);
                var surface = (IceRemembersPhysicalSurface)magic.SurfaceIndexes[triangleIndex];
                materialIndexes[triangleIndex] = surface switch
                {
                    IceRemembersPhysicalSurface.BasaltFlat => (int)IceRemembersPhysicalMaterial.Basalt,
                    IceRemembersPhysicalSurface.FirecoreVent or IceRemembersPhysicalSurface.ThermalVein =>
                        (int)IceRemembersPhysicalMaterial.GeothermalCrust,
                    _ => (int)IceRemembersPhysicalGeography.ClassifyMaterial(field, triangleIndex),
                };
            }
        }

        return materialIndexes;
    }

    private static PhysicalHexAggregate[] AggregateConfiguredHexes(
        PhysicalGeographyField field,
        ReadOnlySpan<int> materialIndexes,
        IceRemembersMicrogeographyMap magic)
    {
        var aggregates = new PhysicalHexAggregate[field.Grid.HexCount];
        for (var configuredIndex = 0;
            configuredIndex < field.Grid.ConfiguredHexCount;
            configuredIndex++)
        {
            var hexIndex = field.Grid.ConfiguredHexIndexAt(configuredIndex);
            aggregates[hexIndex] = PhysicalHexAggregator.Summarize(
                field,
                hexIndex,
                materialIndexes,
                IceRemembersPhysicalGeography.MaterialPalette.Count,
                magic.SurfaceIndexes,
                IceRemembersPhysicalGeography.SurfacePalette.Count,
                magic.SignificantFeatureFlags,
                magic.FireIntensity,
                magic.AuroraIntensity,
                magic.PressureIntensity,
                IceRemembersPhysicalGeography.GenerationProfile.CliffSlopeThreshold);
        }

        return aggregates;
    }

    private static DomainPhysicalGeographyBlueprint BuildPhysicalBlueprint(
        PhysicalGeographyField field,
        ReadOnlySpan<int> materialIndexes,
        IceRemembersMicrogeographyMap magic,
        ReadOnlySpan<PhysicalHexAggregate> aggregates,
        MapSubdivisionMap subdivisions,
        HexGrid fieldGrid)
    {
        var cells = new List<DomainPhysicalGeographyCell>(field.Grid.ConfiguredHexCount);
        var microtriangles = new List<DomainPhysicalMicrotriangle>(checked(
            field.Grid.ConfiguredHexCount * PhysicalGeographyCodes.MicrotrianglesPerHex));
        for (var configuredIndex = 0;
            configuredIndex < field.Grid.ConfiguredHexCount;
            configuredIndex++)
        {
            var hexIndex = field.Grid.ConfiguredHexIndexAt(configuredIndex);
            var coord = field.Grid.HexCoordAt(hexIndex);
            var aggregate = aggregates[hexIndex];
            var terrain = IceRemembersPhysicalTerrainSynthesizer.Synthesize(
                field,
                hexIndex,
                materialIndexes,
                aggregate);
            fieldGrid.SetCell(coord, terrain.ToDomainName());
            var subdivision = subdivisions.GetCell(coord);
            cells.Add(ToDomainCell(
                coord,
                subdivision,
                terrain,
                microtriangles.Count,
                aggregate));
            AddDomainMicrotriangles(field, hexIndex, materialIndexes, magic, microtriangles);
        }

        var blueprint = new DomainPhysicalGeographyBlueprint(
            GenerationRadius,
            IceRemembersPhysicalGeography.MaterialPalette,
            IceRemembersPhysicalGeography.SurfacePalette,
            IceRemembersPhysicalGeography.FeaturePalette,
            CreateScalarFields(field, magic),
            cells,
            microtriangles);
        blueprint.Validate();
        return blueprint;
    }

    private static void AddDomainMicrotriangles(
        PhysicalGeographyField field,
        int hexIndex,
        ReadOnlySpan<int> materialIndexes,
        IceRemembersMicrogeographyMap magic,
        List<DomainPhysicalMicrotriangle> target)
    {
        for (var localTriangle = 0;
            localTriangle < PhysicalGeographyCodes.MicrotrianglesPerHex;
            localTriangle++)
        {
            var triangleIndex = field.Grid.TriangleIndex(hexIndex, localTriangle);
            target.Add(ToDomainMicrotriangle(
                field,
                magic,
                triangleIndex,
                materialIndexes[triangleIndex]));
        }
    }

    private static DomainPhysicalMicrotriangle ToDomainMicrotriangle(
        PhysicalGeographyField field,
        IceRemembersMicrogeographyMap magic,
        int triangleIndex,
        int materialIndex)
    {
        var flowEdge = field.FlowEdges[triangleIndex];
        var flowTarget = flowEdge < 0
            ? -1
            : field.Grid.NeighborTriangleIndex(triangleIndex, flowEdge);
        if (flowTarget >= 0 && !field.Grid.TriangleIsInsideConfiguredDisk(flowTarget))
        {
            flowEdge = PhysicalGeographyCodes.NoFlowEdge;
        }

        return new DomainPhysicalMicrotriangle(
            materialIndex,
            magic.BaseSurfaceIndexes[triangleIndex],
            magic.SurfaceIndexes[triangleIndex],
            magic.SignificantFeatureFlags[triangleIndex],
            field.LandformCodes[triangleIndex],
            field.Elevation[triangleIndex],
            field.Slope[triangleIndex],
            field.Temperature[triangleIndex],
            field.Moisture[triangleIndex],
            field.Geothermal[triangleIndex],
            field.LandmassIds[triangleIndex],
            field.LandmassClassCodes[triangleIndex],
            field.ContinentalGroupIds[triangleIndex],
            field.BathymetricZoneCodes[triangleIndex],
            field.WaterKindCodes[triangleIndex],
            field.WaterDepths[triangleIndex],
            field.WaterBodyIds[triangleIndex],
            field.DrainageBasinIds[triangleIndex],
            flowEdge,
            field.FlowAccumulation[triangleIndex],
            field.RiverOrders[triangleIndex],
            field.IceThickness[triangleIndex],
            field.IceFlux[triangleIndex],
            field.FractureConductance[triangleIndex],
            field.HeatSourceDensity[triangleIndex],
            field.FeatureFlags[triangleIndex]);
    }

    private static DomainPhysicalGeographyCell ToDomainCell(
        HexCoord coord,
        MapSubdivisionCell subdivision,
        IceRemembersTerrain terrain,
        int microtriangleOffset,
        PhysicalHexAggregate aggregate) =>
        new(
            coord.Q,
            coord.R,
            terrain.ToDomainName(),
            subdivision.SuperregionId,
            subdivision.ContinentId,
            microtriangleOffset,
            aggregate.DominantMaterialIndex,
            aggregate.PrimarySurfaceIndex,
            aggregate.SignificantFeatureFlags,
            aggregate.MeanElevation,
            aggregate.MinElevation,
            aggregate.MaxElevation,
            aggregate.Relief,
            aggregate.Roughness,
            aggregate.MeanTemperature,
            aggregate.MeanMoisture,
            aggregate.MeanGeothermal,
            aggregate.LandCoverage,
            aggregate.WaterCoverage,
            aggregate.OceanCoverage,
            aggregate.LakeCoverage,
            aggregate.PermanentIceCoverage,
            aggregate.RiverCoverage,
            aggregate.PeakCount,
            aggregate.MeanIceThickness,
            aggregate.MaxIceThickness,
            aggregate.MeanFireIntensity,
            aggregate.MaxFireIntensity,
            aggregate.MeanAuroraIntensity,
            aggregate.MaxAuroraIntensity,
            aggregate.MeanPressureIntensity,
            aggregate.MaxPressureIntensity,
            aggregate.RiverEdgeMask,
            aggregate.CoastEdgeMask,
            aggregate.RidgeEdgeMask,
            aggregate.CliffEdgeMask,
            aggregate.PassEdgeMask,
            aggregate.FractureEdgeMask);

    private static DomainPhysicalScalarField[] CreateScalarFields(
        PhysicalGeographyField field,
        IceRemembersMicrogeographyMap magic) =>
    [
        new("crust_province", ExtractConfiguredValues(field, field.CrustProvinceIds)),
        new("crust_type", ExtractConfiguredValues(field, field.CrustTypeCodes)),
        new("tectonic_boundary", ExtractConfiguredValues(field, field.TectonicBoundaryCodes)),
        new("tectonic_strain", ExtractConfiguredValues(field, field.TectonicStrain)),
        new("fire_intensity", ExtractConfiguredValues(field, magic.FireIntensity)),
        new("aurora_intensity", ExtractConfiguredValues(field, magic.AuroraIntensity)),
        new("pressure_intensity", ExtractConfiguredValues(field, magic.PressureIntensity)),
    ];

    private static int[] ExtractConfiguredValues(
        PhysicalGeographyField field,
        ReadOnlySpan<int> values)
    {
        var result = new int[checked(
            field.Grid.ConfiguredHexCount * PhysicalGeographyCodes.MicrotrianglesPerHex)];
        var offset = 0;
        for (var configuredIndex = 0; configuredIndex < field.Grid.ConfiguredHexCount; configuredIndex++)
        {
            var hexIndex = field.Grid.ConfiguredHexIndexAt(configuredIndex);
            for (var localTriangle = 0;
                localTriangle < PhysicalGeographyCodes.MicrotrianglesPerHex;
                localTriangle++)
            {
                result[offset++] = values[field.Grid.TriangleIndex(hexIndex, localTriangle)];
            }
        }

        return result;
    }

    private static PhysicalHeart CropPhysicalHeart(
        HexGrid fieldGrid,
        MapSubdivisionMap fieldSubdivisions,
        TerrainCatalog catalog)
    {
        var heartGrid = new HexGrid(
            PhysicalHeartRadius,
            catalog,
            IceRemembersTerrain.Wastes.ToDomainName());
        var heartSubdivisions = new MapSubdivisionMap();
        foreach (var heartCell in heartGrid.AllCells)
        {
            var source = fieldGrid.GetCell(heartCell.Coord) ?? throw new InvalidOperationException();
            heartGrid.SetCell(heartCell.Coord, source.Terrain.StableName);
            var subdivision = fieldSubdivisions.GetCell(heartCell.Coord);
            heartSubdivisions.AssignCell(
                heartCell.Coord,
                subdivision.SuperregionId,
                subdivision.ContinentId);
        }

        return new PhysicalHeart(heartGrid, heartSubdivisions);
    }

    private readonly record struct PhysicalHeart(
        HexGrid Grid,
        MapSubdivisionMap Subdivisions);
}
