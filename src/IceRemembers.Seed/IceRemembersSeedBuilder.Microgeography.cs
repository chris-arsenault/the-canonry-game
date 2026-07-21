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
        var generated = IceRemembersWorldGenerationOrchestrator.Generate(
            GenerationRadius,
            salt,
            IceRemembersPhysicalGeography.GenerationProfile,
            IceRemembersPhysicalGeography.MicrogeographyProfile,
            IceRemembersPhysicalGeography.WorldGenerationEpochs);
        var field = generated.Physical;
        var magic = generated.Magic;
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
                IceRemembersPhysicalGeography.GenerationProfile.CliffSlopeThreshold,
                IceRemembersPhysicalGeography.GenerationProfile.TrunkRiverOrderThreshold);
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
            var projection = IceRemembersHexProjection.Project(
                field,
                magic,
                hexIndex,
                aggregate);
            fieldGrid.SetCell(coord, projection.Terrain.ToDomainName());
            var subdivision = subdivisions.GetCell(coord);
            cells.Add(ToDomainCell(
                coord,
                subdivision,
                projection,
                microtriangles.Count,
                aggregate));
            AddDomainMicrotriangles(field, hexIndex, materialIndexes, magic, microtriangles);
        }

        // Discrete geography, identified here where triangles are still in
        // scope and handed on purely in hex terms.
        var (features, coverage) = BuildGeologicalFeatures(field);
        var blueprint = new DomainPhysicalGeographyBlueprint(
            GenerationRadius,
            magic.CompletedEpochCount,
            IceRemembersPhysicalGeography.MaterialPalette,
            IceRemembersPhysicalGeography.SurfacePalette,
            IceRemembersPhysicalGeography.FeaturePalette,
            CreateScalarFields(field, magic),
            cells,
            microtriangles,
            features,
            coverage);
        blueprint.Validate();
        return blueprint;
    }

    private static (List<DomainGeologicalFeature> Features,
        List<DomainGeologicalFeatureCoverage> Coverage) BuildGeologicalFeatures(
        PhysicalGeographyField field)
    {
        var identified = MapGeologicalFeatureIdentification.Identify(field);
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
            field.RiverSpineEdgeMasks[triangleIndex],
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
        PhysicalHexAggregate aggregate) =>
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
        IceRemembersMicrogeographyMap magic) =>
    [
        new("crust_province", ExtractConfiguredValues(field, field.CrustProvinceIds)),
        new("crust_type", ExtractConfiguredValues(field, field.CrustTypeCodes)),
        new("tectonic_boundary", ExtractConfiguredValues(field, field.TectonicBoundaryCodes)),
        new("tectonic_strain", ExtractConfiguredValues(field, field.TectonicStrain)),
        new("ocean_current_x", ExtractConfiguredValues(field, field.OceanCurrentX)),
        new("ocean_current_y", ExtractConfiguredValues(field, field.OceanCurrentY)),
        new("ocean_current_strength", ExtractConfiguredValues(field, field.OceanCurrentStrength)),
        new("ocean_current_heat", ExtractConfiguredValues(field, field.OceanCurrentHeat)),
        new("wave_exposure", ExtractConfiguredValues(field, field.WaveExposure)),
        new("rainfall", ExtractConfiguredValues(field, field.Rainfall)),
        new("snowfall", ExtractConfiguredValues(field, field.Snowfall)),
        new("sea_ice_suitability", ExtractConfiguredValues(field, field.SeaIceSuitability)),
        new("fine_elevation", ExtractConfiguredElevationValues(field, field.Elevation, fine: true)),
        new("conditioned_drainage_elevation", ExtractConfiguredElevationValues(
            field,
            field.ConditionedDrainageElevation,
            fine: true)),
        new("drainage_flat_rank", ExtractConfiguredValues(field, field.DrainageFlatRanks)),
        new("bedrock_elevation", ExtractConfiguredElevationValues(field, field.BedrockElevation)),
        new("sediment_thickness", ExtractConfiguredElevationValues(field, field.SedimentThickness)),
        new("fluvial_erosion_history", ExtractConfiguredElevationValues(
            field,
            field.CumulativeFluvialErosion)),
        new("glacial_erosion_history", ExtractConfiguredElevationValues(
            field,
            field.CumulativeGlacialErosion)),
        new("coastal_erosion_history", ExtractConfiguredElevationValues(
            field,
            field.CumulativeCoastalErosion)),
        new("deposition_history", ExtractConfiguredElevationValues(field, field.CumulativeDeposition)),
        new("maximum_ice_thickness", ExtractConfiguredValues(field, field.MaximumIceThickness)),
        new("maximum_ice_flux", ExtractConfiguredValues(field, field.MaximumIceFlux)),
        new("grounded_ice", ExtractConfiguredValues(field, field.GroundedIce)),
        new("maximum_grounded_ice_flux", ExtractConfiguredValues(
            field,
            field.MaximumGroundedIceFlux)),
        new("river_channel_strength", ExtractConfiguredValues(field, field.RiverChannelStrength)),
        new("river_channel_order", ExtractConfiguredValues(field, field.RiverChannelOrders)),
        new("floodplain_intensity", ExtractConfiguredValues(field, field.FloodplainIntensity)),
        new("delta_intensity", ExtractConfiguredValues(field, field.DeltaIntensity)),
        new("wave_exposure_history", ExtractConfiguredValues(field, field.WaveExposureHistory)),
        new("tectonic_activity_history", ExtractConfiguredValues(field, field.TectonicActivityHistory)),
        new("geothermal_history", ExtractConfiguredValues(field, field.GeothermalHistory)),
        .. CreateVolcanicScalarFields(field),
        new("external_coastal_forcing_history", ExtractConfiguredValues(field, field.ExternalCoastalForcingHistory)),
        new("fire_intensity", ExtractConfiguredValues(field, magic.FireIntensity)),
        new("aurora_intensity", ExtractConfiguredValues(field, magic.AuroraIntensity)),
        new("pressure_intensity", ExtractConfiguredValues(field, magic.PressureIntensity)),
        new("fire_history", ExtractConfiguredValues(field, magic.FireHistory)),
        new("aurora_history", ExtractConfiguredValues(field, magic.AuroraHistory)),
        new("pressure_history", ExtractConfiguredValues(field, magic.PressureHistory)),
        new("pressure_coastal_history", ExtractConfiguredValues(field, magic.PressureCoastalHistory)),
        new("fire_first_epoch", ExtractConfiguredValues(field, magic.FireFirstEpoch)),
        new("aurora_first_epoch", ExtractConfiguredValues(field, magic.AuroraFirstEpoch)),
        new("pressure_first_epoch", ExtractConfiguredValues(field, magic.PressureFirstEpoch)),
        new("pressure_coastal_first_epoch", ExtractConfiguredValues(field, magic.PressureCoastalFirstEpoch)),
    ];

    private static DomainPhysicalScalarField[] CreateVolcanicScalarFields(
        PhysicalGeographyField field) =>
    [
        new("mantle_plume_root", ExtractConfiguredValues(field, field.MantlePlumeRootStrength)),
        new("volcanic_activity", ExtractConfiguredValues(field, field.VolcanicActivity)),
        new("volcanic_construction_history", ExtractConfiguredElevationValues(
            field,
            field.VolcanicConstructionHistory)),
        new("volcanic_last_active_epoch", ExtractConfiguredValues(field, field.VolcanicLastActiveEpoch)),
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

    private static int[] ExtractConfiguredElevationValues(
        PhysicalGeographyField field,
        ReadOnlySpan<long> values,
        bool fine = false)
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
                var value = values[field.Grid.TriangleIndex(hexIndex, localTriangle)];
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
