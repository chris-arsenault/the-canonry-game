namespace Engine.Domain.Startup;

using Engine.Constants;

/// <summary>
/// Stable-name physical geography generated before kernel installation. The
/// radius-wide field exists from startup, while individual hexes are only
/// materialized when the growing map reveals them.
/// </summary>
public sealed record DomainPhysicalGeographyBlueprint(
    int Radius,
    int GenerationEpochCount,
    IReadOnlyList<string> MaterialPalette,
    IReadOnlyList<string> SurfacePalette,
    IReadOnlyList<string> FeaturePalette,
    IReadOnlyList<DomainPhysicalScalarField> ScalarFields,
    IReadOnlyList<DomainPhysicalGeographyCell> Cells,
    IReadOnlyList<DomainPhysicalMicrotriangle> Microtriangles,
    IReadOnlyList<DomainGeologicalFeature> GeologicalFeatures,
    IReadOnlyList<DomainGeologicalFeatureCoverage> GeologicalFeatureCoverage)
{
    public static DomainPhysicalGeographyBlueprint Empty { get; } =
        new(0, 0, [], [], [], [], [], [], [], []);

    public void Validate()
    {
        if (Radius < 0)
        {
            throw new InvalidOperationException("Physical geography radius cannot be negative.");
        }

        if (Radius == 0)
        {
            ValidateEmpty();
            return;
        }

        ValidateCoverage();
        ValidateCells();
        ValidateMicrotriangles();
        ValidateScalarFields();
    }

    private void ValidateEmpty()
    {
        if (GenerationEpochCount != 0 || MaterialPalette.Count != 0 || SurfacePalette.Count != 0 ||
            FeaturePalette.Count != 0 || ScalarFields.Count != 0 ||
            Cells.Count != 0 || Microtriangles.Count != 0)
        {
            throw new InvalidOperationException("Empty physical geography cannot contain data.");
        }
    }

    private void ValidateCoverage()
    {
        var expectedCells = checked((3 * Radius * Radius) + (3 * Radius) + 1);
        if (GenerationEpochCount <= 0 || MaterialPalette.Count == 0 ||
            SurfacePalette.Count == 0 || FeaturePalette.Count > 64 ||
            Cells.Count != expectedCells ||
            Microtriangles.Count != checked(expectedCells * PhysicalGeographyCodes.MicrotrianglesPerHex))
        {
            throw new InvalidOperationException("Physical geography does not cover its declared hex disk.");
        }
    }

    private void ValidateCells()
    {
        for (var cellIndex = 0; cellIndex < Cells.Count; cellIndex++)
        {
            var cell = Cells[cellIndex];
            if (!CellIdentityIsValid(cell, cellIndex) || !CellCoverageIsValid(cell))
            {
                throw new InvalidOperationException("Physical geography contains an invalid hex summary.");
            }
        }
    }

    private bool CellIdentityIsValid(DomainPhysicalGeographyCell cell, int cellIndex) =>
        cell.MicrotriangleOffset == cellIndex * PhysicalGeographyCodes.MicrotrianglesPerHex &&
        !string.IsNullOrWhiteSpace(cell.BaseTerrain) &&
        (uint)cell.DominantMaterialIndex < (uint)MaterialPalette.Count &&
        (uint)cell.PrimarySurfaceIndex < (uint)SurfacePalette.Count &&
        cell.SuperregionId > 0 && cell.ContinentId > 0 &&
        GameplayProjectionIsValid(cell);

    private static bool GameplayProjectionIsValid(DomainPhysicalGeographyCell cell) =>
        (cell.CrossingWord & ~PhysicalCrossingCodes.WordMask) == 0 &&
        (cell.FieldWord & ~PhysicalHexFieldCodes.WordMask) == 0 &&
        (cell.InitialCoverTerrain is null || !string.IsNullOrWhiteSpace(cell.InitialCoverTerrain));

    private static bool CellCoverageIsValid(DomainPhysicalGeographyCell cell) =>
        cell.RiverCoverage is >= 0 and <= PhysicalGeographyCodes.MicrotrianglesPerHex &&
        cell.FloodplainCoverage is >= 0 and <= PhysicalGeographyCodes.MicrotrianglesPerHex &&
        cell.DeltaCoverage is >= 0 and <= PhysicalGeographyCodes.MicrotrianglesPerHex &&
        cell.WideRiverCoverage is >= 0 and <= PhysicalGeographyCodes.MicrotrianglesPerHex &&
        cell.WideRiverCoverage <= cell.RiverCoverage;

    private void ValidateMicrotriangles()
    {
        foreach (var microtriangle in Microtriangles)
        {
            if (!MicrotriangleIsValid(microtriangle))
            {
                throw new InvalidOperationException("Physical geography contains an invalid microtriangle.");
            }
        }
    }

    private bool MicrotriangleIsValid(DomainPhysicalMicrotriangle microtriangle) =>
        (uint)microtriangle.MaterialIndex < (uint)MaterialPalette.Count &&
        (uint)microtriangle.BaseSurfaceIndex < (uint)SurfacePalette.Count &&
        (uint)microtriangle.SurfaceIndex < (uint)SurfacePalette.Count &&
        SurfaceCodesAreValid(microtriangle) &&
        GeographyCodesAreValid(microtriangle) &&
        HydrologyIsValid(microtriangle) &&
        FlowIsValid(microtriangle);

    private static bool SurfaceCodesAreValid(DomainPhysicalMicrotriangle microtriangle) =>
        microtriangle.LandformCode >= PhysicalLandformCodes.Min &&
        microtriangle.LandformCode <= PhysicalLandformCodes.Max &&
        (microtriangle.FeatureFlags & ~PhysicalGeographyFeatureCodes.All) == 0;

    private static bool GeographyCodesAreValid(DomainPhysicalMicrotriangle microtriangle) =>
        GeographicIdentityIsValid(microtriangle) && FrozenFieldValuesAreValid(microtriangle);

    private static bool GeographicIdentityIsValid(DomainPhysicalMicrotriangle microtriangle) =>
        microtriangle.Slope >= 0 &&
        microtriangle.LandmassId >= 0 &&
        microtriangle.LandmassClassCode >= PhysicalLandmassClassCodes.Min &&
        microtriangle.LandmassClassCode <= PhysicalLandmassClassCodes.Max &&
        microtriangle.ContinentalGroupId >= 0 &&
        microtriangle.BathymetricZoneCode >= PhysicalBathymetricZoneCodes.Min &&
        microtriangle.BathymetricZoneCode <= PhysicalBathymetricZoneCodes.Max;

    private static bool FrozenFieldValuesAreValid(DomainPhysicalMicrotriangle microtriangle) =>
        microtriangle.IceThickness >= 0 &&
        microtriangle.IceFlux >= 0 &&
        microtriangle.FractureConductance >= 0 &&
        microtriangle.HeatSourceDensity >= 0;

    private static bool HydrologyIsValid(DomainPhysicalMicrotriangle microtriangle) =>
        microtriangle.WaterKindCode >= PhysicalWaterKindCodes.Min &&
        microtriangle.WaterKindCode <= PhysicalWaterKindCodes.Max &&
        microtriangle.WaterDepth >= 0 &&
        microtriangle.WaterBodyId >= 0 &&
        microtriangle.DrainageBasinId >= 0;

    private static bool FlowIsValid(DomainPhysicalMicrotriangle microtriangle) =>
        microtriangle.FlowEdge >= PhysicalGeographyCodes.NoFlowEdge &&
        microtriangle.FlowEdge <= 2 &&
        microtriangle.FlowAccumulation >= 0 &&
        microtriangle.RiverOrder >= 0 && RiverGeometryIsValid(microtriangle);

    private static bool RiverGeometryIsValid(DomainPhysicalMicrotriangle microtriangle) =>
        microtriangle.RiverChannelStrength is >= 0 and <= 1000 &&
        microtriangle.RiverChannelOrder >= 0 &&
        microtriangle.FloodplainIntensity is >= 0 and <= 1000 &&
        microtriangle.DeltaIntensity is >= 0 and <= 1000 && RiverGeometryRelationshipsAreValid(microtriangle);

    private static bool RiverGeometryRelationshipsAreValid(DomainPhysicalMicrotriangle microtriangle) =>
        (microtriangle.RiverChannelStrength == 0) == (microtriangle.RiverChannelOrder == 0) &&
        (microtriangle.DeltaIntensity == 0 || microtriangle.RiverChannelStrength > 0) &&
        (microtriangle.FloodplainIntensity == 0 || microtriangle.RiverChannelStrength == 0);

    private void ValidateScalarFields()
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in ScalarFields)
        {
            if (string.IsNullOrWhiteSpace(field.StableName) ||
                !names.Add(field.StableName) ||
                field.Values.Count != Microtriangles.Count)
            {
                throw new InvalidOperationException("Physical geography contains an invalid scalar field.");
            }
        }
    }
}

/// <summary>
/// The one quantization from a hex summary's real quantities to the
/// kernel's byte fact columns, shared by kernel installation and seed-time
/// scoring so both read identical facts. Nothing here invents a scalar the
/// generator did not compute.
/// </summary>
public static class DomainPhysicalGeographyFacts
{
    public static void Quantize(in DomainPhysicalGeographyCell cell, byte[] facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        facts[PhysicalHexFactCodes.Temperature] = PhysicalHexFactCodes.FromIntensity(cell.MeanTemperature);
        facts[PhysicalHexFactCodes.Moisture] = PhysicalHexFactCodes.FromIntensity(cell.MeanMoisture);
        facts[PhysicalHexFactCodes.Geothermal] = PhysicalHexFactCodes.FromIntensity(cell.MeanGeothermal);
        facts[PhysicalHexFactCodes.Elevation] = PhysicalHexFactCodes.FromElevation(cell.MeanElevation);
        facts[PhysicalHexFactCodes.Relief] = PhysicalHexFactCodes.FromReliefUnits(cell.Relief);
        facts[PhysicalHexFactCodes.Roughness] = PhysicalHexFactCodes.FromReliefUnits(cell.Roughness);
        facts[PhysicalHexFactCodes.LandFraction] = PhysicalHexFactCodes.FromCoverage(cell.LandCoverage);
        facts[PhysicalHexFactCodes.RiverCoverage] = PhysicalHexFactCodes.FromCoverage(cell.RiverCoverage);
        facts[PhysicalHexFactCodes.FloodplainCoverage] =
            PhysicalHexFactCodes.FromCoverage(cell.FloodplainCoverage + cell.DeltaCoverage);
        facts[PhysicalHexFactCodes.IceThickness] = PhysicalHexFactCodes.FromIntensity(cell.MeanIceThickness);
        facts[PhysicalHexFactCodes.FireMean] = PhysicalHexFactCodes.FromIntensity(cell.MeanFireIntensity);
        facts[PhysicalHexFactCodes.FireMax] = PhysicalHexFactCodes.FromIntensity(cell.MaxFireIntensity);
        facts[PhysicalHexFactCodes.AuroraMean] = PhysicalHexFactCodes.FromIntensity(cell.MeanAuroraIntensity);
        facts[PhysicalHexFactCodes.AuroraMax] = PhysicalHexFactCodes.FromIntensity(cell.MaxAuroraIntensity);
        facts[PhysicalHexFactCodes.PressureMean] = PhysicalHexFactCodes.FromIntensity(cell.MeanPressureIntensity);
        facts[PhysicalHexFactCodes.PressureMax] = PhysicalHexFactCodes.FromIntensity(cell.MaxPressureIntensity);
    }

}

public readonly record struct DomainPhysicalGeographyCell(
    int Q,
    int R,
    string BaseTerrain,
    int SuperregionId,
    int ContinentId,
    int CrossingWord,
    int FieldWord,
    string? InitialCoverTerrain,
    int MicrotriangleOffset,
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
    int TrunkRiverEdgeMask,
    int CoastEdgeMask,
    int RidgeEdgeMask,
    int CliffEdgeMask,
    int PassEdgeMask,
    int FractureEdgeMask);

public readonly record struct DomainPhysicalMicrotriangle(
    int MaterialIndex,
    int BaseSurfaceIndex,
    int SurfaceIndex,
    ulong SignificantFeatureFlags,
    int LandformCode,
    int Elevation,
    int Slope,
    int Temperature,
    int Moisture,
    int Geothermal,
    int LandmassId,
    int LandmassClassCode,
    int ContinentalGroupId,
    int BathymetricZoneCode,
    int WaterKindCode,
    int WaterDepth,
    int WaterBodyId,
    int DrainageBasinId,
    int FlowEdge,
    int FlowAccumulation,
    int RiverOrder,
    int RiverSpineEdgeMask,
    int RiverChannelStrength,
    int RiverChannelOrder,
    int FloodplainIntensity,
    int DeltaIntensity,
    int IceThickness,
    int IceFlux,
    int FractureConductance,
    int HeatSourceDensity,
    int FeatureFlags);

public sealed record DomainPhysicalScalarField(
    string StableName,
    IReadOnlyList<int> Values);

/// <summary>
/// A piece of geography discrete enough to name: a river system, a lake, a
/// range. Identified during generation, where triangles are in scope, and
/// carried onward purely in hex terms so gameplay can read it without ever
/// seeing a triangle.
/// </summary>
public readonly record struct DomainGeologicalFeature(
    int Id,
    int Kind,
    int AnchorQ,
    int AnchorR,
    int Magnitude,
    int HexCount);

/// <summary>One hex that a feature covers.</summary>
public readonly record struct DomainGeologicalFeatureCoverage(int FeatureId, int Q, int R);
