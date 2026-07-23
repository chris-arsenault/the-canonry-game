namespace IceRemembers.Seed;

using Engine.Constants;
using Engine.Domain.Spatial;
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
        var generationSize = PhysicalGenerationSize.FromKernelRadius(GenerationRadius);
        var generated = IceRemembersWorldGenerationOrchestrator.Generate(
            generationSize.TriangleCount,
            salt,
            IceRemembersPhysicalGeography.GenerationProfile,
            IceRemembersPhysicalGeography.MicrogeographyProfile,
            IceRemembersPhysicalGeography.WorldGenerationEpochs);
        var field = generated.Physical;
        var magic = generated.Magic;
        var geometry = new PhysicalKernelMapGeometry(GenerationRadius, field.Grid);
        var materialIndexes = ClassifyPhysicalMaterials(field, magic);
        var aggregates = AggregateProjectedCells(field, geometry, materialIndexes, magic);
        var subdivisions = CreatePhysicalSubdivisions(geometry, aggregates, salt);
        var catalog = IceRemembersTerrainCatalog.CreateWorldGenCatalog();
        var fieldGrid = new HexGrid(
            GenerationRadius,
            catalog,
            IceRemembersTerrain.Wastes.ToDomainName());
        var blueprint = BuildPhysicalBlueprint(
            field,
            geometry,
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
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            var surface = (IceRemembersPhysicalSurface)magic.SurfaceIndexes[triangleIndex];
            materialIndexes[triangleIndex] = surface switch
            {
                IceRemembersPhysicalSurface.BasaltFlat => (int)IceRemembersPhysicalMaterial.Basalt,
                IceRemembersPhysicalSurface.FirecoreVent or IceRemembersPhysicalSurface.ThermalVein =>
                    (int)IceRemembersPhysicalMaterial.GeothermalCrust,
                _ => (int)IceRemembersPhysicalGeography.ClassifyMaterial(field, triangleIndex),
            };
        }

        return materialIndexes;
    }

    private static PhysicalKernelCellAggregate[] AggregateProjectedCells(
        PhysicalGeographyField field,
        PhysicalKernelMapGeometry geometry,
        ReadOnlySpan<int> materialIndexes,
        IceRemembersMicrogeographyMap magic)
    {
        const int trunkRiverOrderThreshold = 4;
        var aggregates = new PhysicalKernelCellAggregate[geometry.CellCount];
        for (var cellIndex = 0; cellIndex < geometry.CellCount; cellIndex++)
        {
            aggregates[cellIndex] = PhysicalKernelCellAggregator.Summarize(
                field,
                geometry,
                cellIndex,
                materialIndexes,
                IceRemembersPhysicalGeography.MaterialPalette.Count,
                magic.SurfaceIndexes,
                IceRemembersPhysicalGeography.SurfacePalette.Count,
                magic.SignificantFeatureFlags,
                magic.FireIntensity,
                magic.AuroraIntensity,
                magic.PressureIntensity,
                IceRemembersPhysicalGeography.GenerationProfile.CliffSlopeThreshold,
                trunkRiverOrderThreshold);
        }

        return aggregates;
    }

    private static DomainPhysicalGeographyBlueprint BuildPhysicalBlueprint(
        PhysicalGeographyField field,
        PhysicalKernelMapGeometry geometry,
        ReadOnlySpan<int> materialIndexes,
        IceRemembersMicrogeographyMap magic,
        ReadOnlySpan<PhysicalKernelCellAggregate> aggregates,
        MapSubdivisionMap subdivisions,
        HexGrid fieldGrid)
    {
        var cells = new List<DomainPhysicalGeographyCell>(geometry.CellCount);
        var microtriangles = new List<DomainPhysicalMicrotriangle>(checked(
            geometry.CellCount * PhysicalGeographyCodes.MicrotrianglesPerHex));
        for (var cellIndex = 0; cellIndex < geometry.CellCount; cellIndex++)
        {
            var coord = geometry.CoordAt(cellIndex);
            var aggregate = aggregates[cellIndex];
            var projection = IceRemembersHexProjection.Project(
                field,
                magic,
                geometry,
                cellIndex,
                aggregate);
            fieldGrid.SetCell(coord, projection.Terrain.ToDomainName());
            var subdivision = subdivisions.GetCell(coord);
            cells.Add(ToDomainCell(
                coord,
                subdivision,
                projection,
                microtriangles.Count,
                aggregate));
            AddDomainMicrotriangles(
                field,
                geometry,
                cellIndex,
                materialIndexes,
                magic,
                microtriangles);
        }

        // Discrete geography, identified here where triangles are still in
        // scope and handed on purely in hex terms.
        var (features, coverage) = BuildGeologicalFeatures(field, geometry);
        var blueprint = new DomainPhysicalGeographyBlueprint(
            GenerationRadius,
            magic.CompletedEpochCount,
            IceRemembersPhysicalGeography.MaterialPalette,
            IceRemembersPhysicalGeography.SurfacePalette,
            IceRemembersPhysicalGeography.FeaturePalette,
            CreateScalarFields(field, geometry, magic),
            cells,
            microtriangles,
            features,
            coverage);
        blueprint.Validate();
        return blueprint;
    }

    private static (List<DomainGeologicalFeature> Features,
        List<DomainGeologicalFeatureCoverage> Coverage) BuildGeologicalFeatures(
        PhysicalGeographyField field,
        PhysicalKernelMapGeometry geometry)
    {
        var identified = ProjectedGeologicalFeatureIdentification.Identify(field, geometry);
        var features = new List<DomainGeologicalFeature>(identified.Features.Count);
        var coverage = new List<DomainGeologicalFeatureCoverage>();
        foreach (var feature in identified.Features.Values.OrderBy(entry => entry.Id))
        {
            features.Add(new DomainGeologicalFeature(
                feature.Id,
                (int)feature.Kind,
                feature.Anchor.Q,
                feature.Anchor.R,
                feature.Magnitude,
                feature.HexCount));
        }

        foreach (var coord in identified.CoveredCoords())
        {
            foreach (var id in identified.At(coord))
            {
                coverage.Add(new DomainGeologicalFeatureCoverage(id, coord.Q, coord.R));
            }
        }

        return (features, coverage);
    }

    private static void AddDomainMicrotriangles(
        PhysicalGeographyField field,
        PhysicalKernelMapGeometry geometry,
        int cellIndex,
        ReadOnlySpan<int> materialIndexes,
        IceRemembersMicrogeographyMap magic,
        List<DomainPhysicalMicrotriangle> target)
    {
        for (var localTriangle = 0;
            localTriangle < PhysicalGeographyCodes.MicrotrianglesPerHex;
            localTriangle++)
        {
            var triangleIndex = geometry.TriangleIndex(cellIndex, localTriangle);
            target.Add(ToDomainMicrotriangle(
                field,
                magic,
                geometry,
                cellIndex,
                localTriangle,
                triangleIndex,
                materialIndexes[triangleIndex]));
        }
    }

    private static DomainPhysicalMicrotriangle ToDomainMicrotriangle(
        PhysicalGeographyField field,
        IceRemembersMicrogeographyMap magic,
        PhysicalKernelMapGeometry geometry,
        int cellIndex,
        int localTriangle,
        int triangleIndex,
        int materialIndex)
    {
        var flowEdge = field.FlowEdges[triangleIndex];
        var flowTarget = flowEdge < 0
            ? -1
            : field.Grid.NeighborTriangleIndex(triangleIndex, flowEdge);
        if (flowTarget >= 0 &&
            !geometry.TryLocateTriangle(flowTarget, out _, out _))
        {
            flowEdge = PhysicalGeographyCodes.NoFlowEdge;
        }
        else if (flowEdge >= 0)
        {
            flowEdge = geometry.ProjectionEdgeForGenerationEdge(
                cellIndex,
                localTriangle,
                flowEdge);
        }

        return new DomainPhysicalMicrotriangle(
            materialIndex,
            magic.BaseSurfaceIndexes[triangleIndex],
            magic.SurfaceIndexes[triangleIndex],
            magic.SignificantFeatureFlags[triangleIndex],
            field.LandformCodes[triangleIndex],
            PhysicalElevation.ToUnits(field.Elevation[triangleIndex]),
            PhysicalElevation.ToUnits(field.Slope[triangleIndex]),
            field.Temperature[triangleIndex],
            field.Moisture[triangleIndex],
            field.Geothermal[triangleIndex],
            field.LandmassIds[triangleIndex],
            field.LandmassClassCodes[triangleIndex],
            field.ContinentalGroupIds[triangleIndex],
            field.BathymetricZoneCodes[triangleIndex],
            field.WaterKindCodes[triangleIndex],
            PhysicalElevation.ToUnits(field.WaterDepths[triangleIndex]),
            field.WaterBodyIds[triangleIndex],
            field.DrainageBasinIds[triangleIndex],
            flowEdge,
            field.FlowAccumulation[triangleIndex],
            field.RiverOrders[triangleIndex],
            geometry.TranslateGenerationEdgeMask(
                cellIndex,
                localTriangle,
                field.RiverSpineEdgeMasks[triangleIndex]),
            field.RiverChannelStrength[triangleIndex],
            field.RiverChannelOrders[triangleIndex],
            field.FloodplainIntensity[triangleIndex],
            field.DeltaIntensity[triangleIndex],
            field.IceThickness[triangleIndex],
            field.IceFlux[triangleIndex],
            field.FractureConductance[triangleIndex],
            field.HeatSourceDensity[triangleIndex],
            field.FeatureFlags[triangleIndex]);
    }

    private static DomainPhysicalGeographyCell ToDomainCell(
        HexCoord coord,
        MapSubdivisionCell subdivision,
        IceRemembersHexProjection.Result projection,
        int microtriangleOffset,
        PhysicalKernelCellAggregate aggregate) =>
        new(
            coord.Q,
            coord.R,
            projection.Terrain.ToDomainName(),
            subdivision.SuperregionId,
            subdivision.ContinentId,
            projection.CrossingWord,
            projection.FieldWord,
            projection.InitialCover?.ToDomainName(),
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
            aggregate.FloodplainCoverage,
            aggregate.DeltaCoverage,
            aggregate.WideRiverCoverage,
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
            aggregate.TrunkRiverEdgeMask,
            aggregate.CoastEdgeMask,
            aggregate.RidgeEdgeMask,
            aggregate.CliffEdgeMask,
            aggregate.PassEdgeMask,
            aggregate.FractureEdgeMask);

    private static DomainPhysicalScalarField[] CreateScalarFields(
        PhysicalGeographyField field,
        PhysicalKernelMapGeometry geometry,
        IceRemembersMicrogeographyMap magic) =>
    [
        new("crust_province", ExtractProjectedValues(geometry, field.CrustProvinceIds)),
        new("crust_type", ExtractProjectedValues(geometry, field.CrustTypeCodes)),
        new("tectonic_boundary", ExtractProjectedValues(geometry, field.TectonicBoundaryCodes)),
        new("tectonic_strain", ExtractProjectedValues(geometry, field.TectonicStrain)),
        new("ocean_current_x", ExtractProjectedValues(geometry, field.OceanCurrentX)),
        new("ocean_current_y", ExtractProjectedValues(geometry, field.OceanCurrentY)),
        new("ocean_current_strength", ExtractProjectedValues(geometry, field.OceanCurrentStrength)),
        new("ocean_current_heat", ExtractProjectedValues(geometry, field.OceanCurrentHeat)),
        new("wave_exposure", ExtractProjectedValues(geometry, field.WaveExposure)),
        new("rainfall", ExtractProjectedValues(geometry, field.Rainfall)),
        new("snowfall", ExtractProjectedValues(geometry, field.Snowfall)),
        new("sea_ice_suitability", ExtractProjectedValues(geometry, field.SeaIceSuitability)),
        new("fine_elevation", ExtractProjectedElevationValues(geometry, field.Elevation, fine: true)),
        new("conditioned_drainage_elevation", ExtractProjectedElevationValues(
            geometry,
            field.ConditionedDrainageElevation,
            fine: true)),
        new("drainage_flat_rank", ExtractProjectedValues(geometry, field.DrainageFlatRanks)),
        new("bedrock_elevation", ExtractProjectedElevationValues(geometry, field.BedrockElevation)),
        new("sediment_thickness", ExtractProjectedElevationValues(geometry, field.SedimentThickness)),
        new("fluvial_erosion_history", ExtractProjectedElevationValues(
            geometry,
            field.CumulativeFluvialErosion)),
        new("glacial_erosion_history", ExtractProjectedElevationValues(
            geometry,
            field.CumulativeGlacialErosion)),
        new("coastal_erosion_history", ExtractProjectedElevationValues(
            geometry,
            field.CumulativeCoastalErosion)),
        new("deposition_history", ExtractProjectedElevationValues(geometry, field.CumulativeDeposition)),
        new("maximum_ice_thickness", ExtractProjectedValues(geometry, field.MaximumIceThickness)),
        new("maximum_ice_flux", ExtractProjectedValues(geometry, field.MaximumIceFlux)),
        new("grounded_ice", ExtractProjectedValues(geometry, field.GroundedIce)),
        new("maximum_grounded_ice_flux", ExtractProjectedValues(
            geometry,
            field.MaximumGroundedIceFlux)),
        new("river_channel_strength", ExtractProjectedValues(geometry, field.RiverChannelStrength)),
        new("river_channel_order", ExtractProjectedValues(geometry, field.RiverChannelOrders)),
        new("floodplain_intensity", ExtractProjectedValues(geometry, field.FloodplainIntensity)),
        new("delta_intensity", ExtractProjectedValues(geometry, field.DeltaIntensity)),
        new("wave_exposure_history", ExtractProjectedValues(geometry, field.WaveExposureHistory)),
        new("tectonic_activity_history", ExtractProjectedValues(geometry, field.TectonicActivityHistory)),
        new("geothermal_history", ExtractProjectedValues(geometry, field.GeothermalHistory)),
        .. CreateVolcanicScalarFields(geometry, field),
        new("external_coastal_forcing_history", ExtractProjectedValues(geometry, field.ExternalCoastalForcingHistory)),
        new("fire_intensity", ExtractProjectedValues(geometry, magic.FireIntensity)),
        new("aurora_intensity", ExtractProjectedValues(geometry, magic.AuroraIntensity)),
        new("pressure_intensity", ExtractProjectedValues(geometry, magic.PressureIntensity)),
        new("fire_history", ExtractProjectedValues(geometry, magic.FireHistory)),
        new("aurora_history", ExtractProjectedValues(geometry, magic.AuroraHistory)),
        new("pressure_history", ExtractProjectedValues(geometry, magic.PressureHistory)),
        new("pressure_coastal_history", ExtractProjectedValues(geometry, magic.PressureCoastalHistory)),
        new("fire_first_epoch", ExtractProjectedValues(geometry, magic.FireFirstEpoch)),
        new("aurora_first_epoch", ExtractProjectedValues(geometry, magic.AuroraFirstEpoch)),
        new("pressure_first_epoch", ExtractProjectedValues(geometry, magic.PressureFirstEpoch)),
        new("pressure_coastal_first_epoch", ExtractProjectedValues(geometry, magic.PressureCoastalFirstEpoch)),
    ];

    private static DomainPhysicalScalarField[] CreateVolcanicScalarFields(
        PhysicalKernelMapGeometry geometry,
        PhysicalGeographyField field) =>
    [
        new("mantle_plume_root", ExtractProjectedValues(geometry, field.MantlePlumeRootStrength)),
        new("volcanic_activity", ExtractProjectedValues(geometry, field.VolcanicActivity)),
        new("volcanic_construction_history", ExtractProjectedElevationValues(
            geometry,
            field.VolcanicConstructionHistory)),
        new("volcanic_last_active_epoch", ExtractProjectedValues(geometry, field.VolcanicLastActiveEpoch)),
    ];

    private static int[] ExtractProjectedValues(
        PhysicalKernelMapGeometry geometry,
        ReadOnlySpan<int> values)
    {
        var result = new int[geometry.ProjectedTriangleCount];
        var offset = 0;
        for (var cellIndex = 0; cellIndex < geometry.CellCount; cellIndex++)
        {
            for (var localTriangle = 0;
                localTriangle < PhysicalGeographyCodes.MicrotrianglesPerHex;
                localTriangle++)
            {
                result[offset++] = values[geometry.TriangleIndex(cellIndex, localTriangle)];
            }
        }

        return result;
    }

    private static int[] ExtractProjectedElevationValues(
        PhysicalKernelMapGeometry geometry,
        ReadOnlySpan<long> values,
        bool fine = false)
    {
        var result = new int[geometry.ProjectedTriangleCount];
        var offset = 0;
        for (var cellIndex = 0; cellIndex < geometry.CellCount; cellIndex++)
        {
            for (var localTriangle = 0;
                localTriangle < PhysicalGeographyCodes.MicrotrianglesPerHex;
                localTriangle++)
            {
                var value = values[geometry.TriangleIndex(cellIndex, localTriangle)];
                result[offset++] = fine
                    ? PhysicalElevation.ToDiagnosticValue(value)
                    : PhysicalElevation.ToUnits(value);
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
