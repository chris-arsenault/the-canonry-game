namespace IceRemembers.Seed;

using System.Text;

using Engine.Domain.Startup;
using Engine.Gameplay;
using Engine.Graph;
using Engine.Host;

internal static class IceRemembersStartupSeedCacheSerializer
{
    private const ulong Magic = 0x31444C524F574743UL;
    private const int FormatVersion = 2;
    private const int MaximumEntityCount = 1_000_000;
    private const int MaximumCellCount = 2_000_000;
    private const int MaximumRelationshipCount = 4_000_000;
    private const int MaximumSubdivisionCount = 1_000_000;
    private const int MaximumAttributeCount = 4_000_000;
    private const int MaximumKnowledgeSiteCount = 1_000_000;
    private const int MaximumGrantCount = 10_000;
    private const int MaximumPaletteCount = 10_000;
    private const int MaximumScalarFieldCount = 100;
    private const int MaximumPhysicalCellCount = 2_000_000;
    private const int MaximumMicrotriangleCount = 50_000_000;
    private const int MaximumComponentCount = 1_000;

    public static IceRemembersStartupSeedHeader ReadHeader(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        if (reader.ReadUInt64() != Magic || reader.ReadInt32() != FormatVersion)
        {
            throw new InvalidDataException("Startup seed header is not recognized.");
        }

        return new IceRemembersStartupSeedHeader(
            reader.ReadInt32(),
            reader.ReadInt32());
    }

    public static void Write(
        Stream stream,
        int worldGenerationVersion,
        int baseSeed,
        DomainStartupSeedResult startupSeed)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(worldGenerationVersion);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baseSeed);
        ArgumentNullException.ThrowIfNull(startupSeed);
        if (startupSeed.Identity.BaseSeed != baseSeed)
        {
            throw new ArgumentException("Startup seed identity does not match the cache key.", nameof(startupSeed));
        }

        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write(Magic);
        writer.Write(FormatVersion);
        writer.Write(worldGenerationVersion);
        writer.Write(baseSeed);
        WriteIdentity(writer, startupSeed.Identity);
        WriteStartupSeed(writer, startupSeed.Seed);
    }

    public static DomainStartupSeedResult Read(
        Stream stream,
        int expectedWorldGenerationVersion,
        int expectedBaseSeed)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedWorldGenerationVersion);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedBaseSeed);

        try
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            if (reader.ReadUInt64() != Magic || reader.ReadInt32() != FormatVersion)
            {
                throw new InvalidDataException("Startup cache header is not recognized.");
            }

            var worldGenerationVersion = reader.ReadInt32();
            if (worldGenerationVersion != expectedWorldGenerationVersion)
            {
                throw new InvalidDataException(
                    $"Startup cache world-generation version {worldGenerationVersion} does not match {expectedWorldGenerationVersion}.");
            }

            var baseSeed = reader.ReadInt32();
            if (baseSeed != expectedBaseSeed)
            {
                throw new InvalidDataException(
                    $"Startup cache seed {baseSeed} does not match requested seed {expectedBaseSeed}.");
            }

            var identity = ReadIdentity(reader);
            if (identity.BaseSeed != expectedBaseSeed)
            {
                throw new InvalidDataException("Startup cache identity does not match its cache key.");
            }

            var seed = ReadStartupSeed(reader);
            if (stream.CanSeek && stream.Position != stream.Length)
            {
                throw new InvalidDataException("Startup cache contains trailing data.");
            }

            ValidateSeed(seed);
            return new DomainStartupSeedResult(seed, identity);
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or OverflowException)
        {
            throw new InvalidDataException("Startup cache data is incomplete or invalid.", exception);
        }
    }

    private static void WriteIdentity(BinaryWriter writer, StartupSeedIdentity identity)
    {
        writer.Write(identity.BaseSeed);
        writer.Write(identity.Attempt);
        writer.Write(identity.Components.Count);
        foreach (var (name, value) in identity.Components.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            writer.Write(name);
            writer.Write(value);
        }
    }

    private static StartupSeedIdentity ReadIdentity(BinaryReader reader)
    {
        var baseSeed = reader.ReadInt32();
        var attempt = reader.ReadInt32();
        var count = ReadCount(reader, MaximumComponentCount, "identity component");
        var components = new Dictionary<string, int>(count, StringComparer.Ordinal);
        for (var index = 0; index < count; index++)
        {
            if (!components.TryAdd(reader.ReadString(), reader.ReadInt32()))
            {
                throw new InvalidDataException("Startup cache contains a duplicate identity component.");
            }
        }

        return new StartupSeedIdentity(baseSeed, attempt, components);
    }

    private static void WriteStartupSeed(BinaryWriter writer, DomainStartupSeed seed)
    {
        writer.Write(seed.MapRadius);
        WriteList(writer, seed.Entities, WriteEntity);
        WriteList(writer, seed.Cells, WriteCell);
        WriteList(writer, seed.Relationships, WriteRelationship);
        WriteList(writer, seed.MapSubdivisions, WriteSubdivision);
        WriteList(writer, seed.Attributes, WriteAttribute);
        WriteList(writer, seed.KnowledgeSites, WriteKnowledgeSite);
        WritePhysicalGeography(writer, seed.PhysicalGeography);
        WriteSemanticTerrain(writer, seed.SemanticTerrain);
    }

    private static DomainStartupSeed ReadStartupSeed(BinaryReader reader)
    {
        var mapRadius = reader.ReadInt32();
        var entities = ReadArray(reader, MaximumEntityCount, "entity", ReadEntity);
        var cells = ReadArray(reader, MaximumCellCount, "startup cell", ReadCell);
        var relationships = ReadArray(reader, MaximumRelationshipCount, "relationship", ReadRelationship);
        var subdivisions = ReadArray(reader, MaximumSubdivisionCount, "map subdivision", ReadSubdivision);
        var attributes = ReadArray(reader, MaximumAttributeCount, "attribute", ReadAttribute);
        var knowledgeSites = ReadArray(reader, MaximumKnowledgeSiteCount, "knowledge site", ReadKnowledgeSite);
        var physicalGeography = ReadPhysicalGeography(reader);
        var semanticTerrain = ReadSemanticTerrain(reader);
        return new DomainStartupSeed(
            mapRadius,
            entities,
            cells,
            relationships,
            subdivisions,
            attributes,
            knowledgeSites,
            physicalGeography,
            semanticTerrain);
    }

    private static void WriteEntity(BinaryWriter writer, DomainSeedEntity entity)
    {
        writer.Write(entity.SeedEntityId);
        writer.Write(entity.Kind);
        writer.Write(entity.Subtype);
        writer.Write(entity.Culture);
        writer.Write(entity.PhysicalQ);
        writer.Write(entity.PhysicalR);
        writer.Write(entity.SemanticQ);
        writer.Write(entity.SemanticR);
        writer.Write(entity.SemanticS);
        WriteNullableString(writer, entity.DisplayName);
        WriteNullableString(writer, entity.ReferenceName);
    }

    private static DomainSeedEntity ReadEntity(BinaryReader reader) => new(
        reader.ReadInt32(),
        reader.ReadString(),
        reader.ReadString(),
        reader.ReadString(),
        reader.ReadInt32(),
        reader.ReadInt32(),
        reader.ReadInt32(),
        reader.ReadInt32(),
        reader.ReadInt32(),
        ReadNullableString(reader),
        ReadNullableString(reader));

    private static void WriteCell(BinaryWriter writer, DomainSeedCell cell)
    {
        writer.Write(cell.Q);
        writer.Write(cell.R);
        writer.Write(cell.Terrain);
        WriteNullableInt32(writer, cell.OwnerSeedEntityId);
        writer.Write(cell.SuperregionId);
        writer.Write(cell.ContinentId);
        writer.Write(cell.PlaneCode);
    }

    private static DomainSeedCell ReadCell(BinaryReader reader) => new(
        reader.ReadInt32(),
        reader.ReadInt32(),
        reader.ReadString(),
        ReadNullableInt32(reader),
        reader.ReadInt32(),
        reader.ReadInt32(),
        reader.ReadInt32());

    private static void WriteRelationship(BinaryWriter writer, DomainSeedRelationship relationship)
    {
        writer.Write(relationship.RelationshipKind);
        writer.Write((int)relationship.Polarity);
        writer.Write(relationship.SourceSeedEntityId);
        writer.Write(relationship.TargetSeedEntityId);
        writer.Write(relationship.Strength);
    }

    private static DomainSeedRelationship ReadRelationship(BinaryReader reader) => new(
        reader.ReadString(),
        (RelationshipPolarity)reader.ReadInt32(),
        reader.ReadInt32(),
        reader.ReadInt32(),
        reader.ReadSingle());

    private static void WriteSubdivision(BinaryWriter writer, DomainMapSubdivision subdivision)
    {
        writer.Write(subdivision.Id);
        writer.Write((int)subdivision.Level);
        writer.Write(subdivision.StableName);
        writer.Write(subdivision.DisplayName);
    }

    private static DomainMapSubdivision ReadSubdivision(BinaryReader reader) => new(
        reader.ReadInt32(),
        (DomainMapSubdivisionLevel)reader.ReadInt32(),
        reader.ReadString(),
        reader.ReadString());

    private static void WriteAttribute(BinaryWriter writer, DomainSeedAttributeValue attribute)
    {
        writer.Write(attribute.SeedEntityId);
        writer.Write(attribute.Attribute);
        writer.Write(attribute.Value);
    }

    private static DomainSeedAttributeValue ReadAttribute(BinaryReader reader) => new(
        reader.ReadInt32(),
        reader.ReadString(),
        reader.ReadSingle());

    private static void WriteKnowledgeSite(BinaryWriter writer, DomainKnowledgeSite site)
    {
        writer.Write(site.TechId);
        writer.Write(site.Q);
        writer.Write(site.R);
        writer.Write(site.Tier);
        WriteList(writer, site.Grants, WriteKnowledgeGrant);
    }

    private static DomainKnowledgeSite ReadKnowledgeSite(BinaryReader reader) => new(
        reader.ReadString(),
        reader.ReadInt32(),
        reader.ReadInt32(),
        reader.ReadInt32(),
        ReadArray(reader, MaximumGrantCount, "knowledge grant", ReadKnowledgeGrant));

    private static void WriteKnowledgeGrant(BinaryWriter writer, KnowledgeGrantConfig grant)
    {
        writer.Write((int)grant.Domain);
        writer.Write(grant.Multiplier);
        WriteNullableString(writer, grant.Target);
        writer.Write(grant.TargetCode);
    }

    private static KnowledgeGrantConfig ReadKnowledgeGrant(BinaryReader reader) => new(
        (KnowledgeGrantDomain)reader.ReadInt32(),
        reader.ReadSingle(),
        ReadNullableString(reader),
        reader.ReadInt32());

    private static void WritePhysicalGeography(
        BinaryWriter writer,
        DomainPhysicalGeographyBlueprint? geography)
    {
        writer.Write(geography is not null);
        if (geography is null)
        {
            return;
        }

        writer.Write(geography.Radius);
        writer.Write(geography.GenerationEpochCount);
        WriteList(writer, geography.MaterialPalette, static (output, material) => output.Write(material));
        WriteList(writer, geography.SurfacePalette, static (output, surface) => output.Write(surface));
        WriteList(writer, geography.FeaturePalette, static (output, feature) => output.Write(feature));
        WriteList(writer, geography.ScalarFields, WritePhysicalScalarField);
        WriteList(writer, geography.Cells, WritePhysicalCell);
        WriteList(writer, geography.Microtriangles, WriteMicrotriangle);
    }

    private static DomainPhysicalGeographyBlueprint? ReadPhysicalGeography(BinaryReader reader)
    {
        if (!reader.ReadBoolean())
        {
            return null;
        }

        var radius = reader.ReadInt32();
        var generationEpochCount = reader.ReadInt32();
        var materialPalette = ReadArray(reader, MaximumPaletteCount, "material", static input => input.ReadString());
        var surfacePalette = ReadArray(reader, MaximumPaletteCount, "surface", static input => input.ReadString());
        var featurePalette = ReadArray(reader, MaximumPaletteCount, "physical feature", static input => input.ReadString());
        var scalarFields = ReadArray(reader, MaximumScalarFieldCount, "physical scalar field", ReadPhysicalScalarField);
        var cells = ReadArray(reader, MaximumPhysicalCellCount, "physical cell", ReadPhysicalCell);
        var microtriangles = ReadArray(
            reader,
            MaximumMicrotriangleCount,
            "physical microtriangle",
            ReadMicrotriangle);
        return new DomainPhysicalGeographyBlueprint(
            radius,
            generationEpochCount,
            materialPalette,
            surfacePalette,
            featurePalette,
            scalarFields,
            cells,
            microtriangles);
    }

    private static void WritePhysicalScalarField(BinaryWriter writer, DomainPhysicalScalarField field)
    {
        writer.Write(field.StableName);
        WriteList(writer, field.Values, static (output, value) => output.Write(value));
    }

    private static DomainPhysicalScalarField ReadPhysicalScalarField(BinaryReader reader) =>
        new(
            reader.ReadString(),
            ReadArray(reader, MaximumMicrotriangleCount, "physical scalar value", static input => input.ReadInt32()));

    private static void WritePhysicalCell(BinaryWriter writer, DomainPhysicalGeographyCell cell)
    {
        writer.Write(cell.Q);
        writer.Write(cell.R);
        writer.Write(cell.BaseTerrain);
        writer.Write(cell.SuperregionId);
        writer.Write(cell.ContinentId);
        writer.Write(cell.MicrotriangleOffset);
        writer.Write(cell.DominantMaterialIndex);
        writer.Write(cell.PrimarySurfaceIndex);
        writer.Write(cell.SignificantFeatureFlags);
        writer.Write(cell.MeanElevation);
        writer.Write(cell.MinElevation);
        writer.Write(cell.MaxElevation);
        writer.Write(cell.Relief);
        writer.Write(cell.Roughness);
        writer.Write(cell.MeanTemperature);
        writer.Write(cell.MeanMoisture);
        writer.Write(cell.MeanGeothermal);
        WritePhysicalCellCoverage(writer, cell);
        writer.Write(cell.PeakCount);
        writer.Write(cell.MeanIceThickness);
        writer.Write(cell.MaxIceThickness);
        writer.Write(cell.MeanFireIntensity);
        writer.Write(cell.MaxFireIntensity);
        writer.Write(cell.MeanAuroraIntensity);
        writer.Write(cell.MaxAuroraIntensity);
        writer.Write(cell.MeanPressureIntensity);
        writer.Write(cell.MaxPressureIntensity);
        writer.Write(cell.RiverEdgeMask);
        writer.Write(cell.CoastEdgeMask);
        writer.Write(cell.RidgeEdgeMask);
        writer.Write(cell.CliffEdgeMask);
        writer.Write(cell.PassEdgeMask);
        writer.Write(cell.FractureEdgeMask);
    }

    private static void WritePhysicalCellCoverage(BinaryWriter writer, DomainPhysicalGeographyCell cell)
    {
        writer.Write(cell.LandCoverage);
        writer.Write(cell.WaterCoverage);
        writer.Write(cell.OceanCoverage);
        writer.Write(cell.LakeCoverage);
        writer.Write(cell.PermanentIceCoverage);
        writer.Write(cell.RiverCoverage);
        writer.Write(cell.FloodplainCoverage);
        writer.Write(cell.DeltaCoverage);
        writer.Write(cell.WideRiverCoverage);
    }

    private static DomainPhysicalGeographyCell ReadPhysicalCell(BinaryReader reader) => new(
        Q: reader.ReadInt32(),
        R: reader.ReadInt32(),
        BaseTerrain: reader.ReadString(),
        SuperregionId: reader.ReadInt32(),
        ContinentId: reader.ReadInt32(),
        MicrotriangleOffset: reader.ReadInt32(),
        DominantMaterialIndex: reader.ReadInt32(),
        PrimarySurfaceIndex: reader.ReadInt32(),
        SignificantFeatureFlags: reader.ReadUInt64(),
        MeanElevation: reader.ReadInt32(),
        MinElevation: reader.ReadInt32(),
        MaxElevation: reader.ReadInt32(),
        Relief: reader.ReadInt32(),
        Roughness: reader.ReadInt32(),
        MeanTemperature: reader.ReadInt32(),
        MeanMoisture: reader.ReadInt32(),
        MeanGeothermal: reader.ReadInt32(),
        LandCoverage: reader.ReadInt32(),
        WaterCoverage: reader.ReadInt32(),
        OceanCoverage: reader.ReadInt32(),
        LakeCoverage: reader.ReadInt32(),
        PermanentIceCoverage: reader.ReadInt32(),
        RiverCoverage: reader.ReadInt32(),
        FloodplainCoverage: reader.ReadInt32(),
        DeltaCoverage: reader.ReadInt32(),
        WideRiverCoverage: reader.ReadInt32(),
        PeakCount: reader.ReadInt32(),
        MeanIceThickness: reader.ReadInt32(),
        MaxIceThickness: reader.ReadInt32(),
        MeanFireIntensity: reader.ReadInt32(),
        MaxFireIntensity: reader.ReadInt32(),
        MeanAuroraIntensity: reader.ReadInt32(),
        MaxAuroraIntensity: reader.ReadInt32(),
        MeanPressureIntensity: reader.ReadInt32(),
        MaxPressureIntensity: reader.ReadInt32(),
        RiverEdgeMask: reader.ReadInt32(),
        CoastEdgeMask: reader.ReadInt32(),
        RidgeEdgeMask: reader.ReadInt32(),
        CliffEdgeMask: reader.ReadInt32(),
        PassEdgeMask: reader.ReadInt32(),
        FractureEdgeMask: reader.ReadInt32());

    private static void WriteMicrotriangle(BinaryWriter writer, DomainPhysicalMicrotriangle triangle)
    {
        writer.Write(triangle.MaterialIndex);
        writer.Write(triangle.BaseSurfaceIndex);
        writer.Write(triangle.SurfaceIndex);
        writer.Write(triangle.SignificantFeatureFlags);
        writer.Write(triangle.LandformCode);
        writer.Write(triangle.Elevation);
        writer.Write(triangle.Slope);
        writer.Write(triangle.Temperature);
        writer.Write(triangle.Moisture);
        writer.Write(triangle.Geothermal);
        writer.Write(triangle.LandmassId);
        writer.Write(triangle.LandmassClassCode);
        writer.Write(triangle.ContinentalGroupId);
        writer.Write(triangle.BathymetricZoneCode);
        writer.Write(triangle.WaterKindCode);
        writer.Write(triangle.WaterDepth);
        writer.Write(triangle.WaterBodyId);
        writer.Write(triangle.DrainageBasinId);
        writer.Write(triangle.FlowEdge);
        writer.Write(triangle.FlowAccumulation);
        writer.Write(triangle.RiverOrder);
        writer.Write(triangle.RiverChannelStrength);
        writer.Write(triangle.RiverChannelOrder);
        writer.Write(triangle.FloodplainIntensity);
        writer.Write(triangle.DeltaIntensity);
        writer.Write(triangle.IceThickness);
        writer.Write(triangle.IceFlux);
        writer.Write(triangle.FractureConductance);
        writer.Write(triangle.HeatSourceDensity);
        writer.Write(triangle.FeatureFlags);
    }

    private static DomainPhysicalMicrotriangle ReadMicrotriangle(BinaryReader reader) => new(
        MaterialIndex: reader.ReadInt32(),
        BaseSurfaceIndex: reader.ReadInt32(),
        SurfaceIndex: reader.ReadInt32(),
        SignificantFeatureFlags: reader.ReadUInt64(),
        LandformCode: reader.ReadInt32(),
        Elevation: reader.ReadInt32(),
        Slope: reader.ReadInt32(),
        Temperature: reader.ReadInt32(),
        Moisture: reader.ReadInt32(),
        Geothermal: reader.ReadInt32(),
        LandmassId: reader.ReadInt32(),
        LandmassClassCode: reader.ReadInt32(),
        ContinentalGroupId: reader.ReadInt32(),
        BathymetricZoneCode: reader.ReadInt32(),
        WaterKindCode: reader.ReadInt32(),
        WaterDepth: reader.ReadInt32(),
        WaterBodyId: reader.ReadInt32(),
        DrainageBasinId: reader.ReadInt32(),
        FlowEdge: reader.ReadInt32(),
        FlowAccumulation: reader.ReadInt32(),
        RiverOrder: reader.ReadInt32(),
        RiverChannelStrength: reader.ReadInt32(),
        RiverChannelOrder: reader.ReadInt32(),
        FloodplainIntensity: reader.ReadInt32(),
        DeltaIntensity: reader.ReadInt32(),
        IceThickness: reader.ReadInt32(),
        IceFlux: reader.ReadInt32(),
        FractureConductance: reader.ReadInt32(),
        HeatSourceDensity: reader.ReadInt32(),
        FeatureFlags: reader.ReadInt32());

    private static void WriteSemanticTerrain(
        BinaryWriter writer,
        DomainSemanticTerrainBlueprint? terrain)
    {
        writer.Write(terrain is not null);
        if (terrain is null)
        {
            return;
        }

        writer.Write(terrain.Radius);
        WriteList(writer, terrain.Cells, WriteSemanticCell);
    }

    private static DomainSemanticTerrainBlueprint? ReadSemanticTerrain(BinaryReader reader)
    {
        if (!reader.ReadBoolean())
        {
            return null;
        }

        var radius = reader.ReadInt32();
        var cells = ReadArray(reader, MaximumCellCount, "semantic terrain cell", ReadSemanticCell);
        return new DomainSemanticTerrainBlueprint(radius, cells);
    }

    private static void WriteSemanticCell(BinaryWriter writer, DomainSemanticTerrainCell cell)
    {
        writer.Write(cell.Q);
        writer.Write(cell.R);
        writer.Write(cell.Terrain);
        writer.Write(cell.SuperregionId);
        writer.Write(cell.ContinentId);
    }

    private static DomainSemanticTerrainCell ReadSemanticCell(BinaryReader reader) => new(
        reader.ReadInt32(),
        reader.ReadInt32(),
        reader.ReadString(),
        reader.ReadInt32(),
        reader.ReadInt32());

    private static void ValidateSeed(DomainStartupSeed seed)
    {
        if (seed.MapRadius < 0)
        {
            throw new InvalidDataException("Startup cache contains a negative map radius.");
        }

        seed.PhysicalGeography?.Validate();
        seed.SemanticTerrain?.Validate();
    }

    private static void WriteList<T>(
        BinaryWriter writer,
        IReadOnlyList<T> items,
        Action<BinaryWriter, T> writeItem)
    {
        writer.Write(items.Count);
        foreach (var item in items)
        {
            writeItem(writer, item);
        }
    }

    private static T[] ReadArray<T>(
        BinaryReader reader,
        int maximumCount,
        string label,
        Func<BinaryReader, T> readItem)
    {
        var count = ReadCount(reader, maximumCount, label);
        var items = new T[count];
        for (var index = 0; index < count; index++)
        {
            items[index] = readItem(reader);
        }

        return items;
    }

    private static int ReadCount(BinaryReader reader, int maximumCount, string label)
    {
        var count = reader.ReadInt32();
        if (count < 0 || count > maximumCount)
        {
            throw new InvalidDataException($"Startup cache {label} count {count} is invalid.");
        }

        return count;
    }

    private static void WriteNullableInt32(BinaryWriter writer, int? value)
    {
        writer.Write(value.HasValue);
        if (value.HasValue)
        {
            writer.Write(value.Value);
        }
    }

    private static int? ReadNullableInt32(BinaryReader reader) =>
        reader.ReadBoolean() ? reader.ReadInt32() : null;

    private static void WriteNullableString(BinaryWriter writer, string? value)
    {
        writer.Write(value is not null);
        if (value is not null)
        {
            writer.Write(value);
        }
    }

    private static string? ReadNullableString(BinaryReader reader) =>
        reader.ReadBoolean() ? reader.ReadString() : null;
}

internal readonly record struct IceRemembersStartupSeedHeader(
    int WorldGenerationVersion,
    int BaseSeed);
