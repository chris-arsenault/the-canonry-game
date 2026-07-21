namespace IceRemembers.Seed.Tests;

using Engine.Domain.Startup;
using Engine.WorldGen;

/// <summary>
/// Seed-time placement taste: the same kind and culture weights the kernel
/// scores at runtime, priced against blueprint hex facts before the kernel
/// exists. Identity reads geography at seed time too - no ordered terrain
/// list decides who lives where.
/// </summary>
public sealed class IceRemembersSeedTasteTests
{
    [Fact]
    public void OrcaTaste_PricesOpenSeaAboveLand()
    {
        var taste = new IceRemembersSeedTaste(
        [
            Cell(q: 0, r: 0, terrain: "ocean", landCoverage: 0, pressure: 400),
            Cell(q: 1, r: 0, terrain: "tundra", landCoverage: 24, temperature: 400),
        ]);

        var sea = taste.Score(IceRemembersKinds.Orca, Culture(IceRemembersCulture.WakeSingers), new HexCoord(0, 0));
        var land = taste.Score(IceRemembersKinds.Orca, Culture(IceRemembersCulture.WakeSingers), new HexCoord(1, 0));

        Assert.True(sea > land, $"wake-singer orcas should price the sea above land: {sea} vs {land}");
    }

    [Fact]
    public void SettlementTaste_PricesWarmRiverValleyAboveFrozenWaste()
    {
        var taste = new IceRemembersSeedTaste(
        [
            Cell(q: 0, r: 0, terrain: "verdant", landCoverage: 24, temperature: 520, riverCoverage: 6, floodplainCoverage: 4),
            Cell(q: 1, r: 0, terrain: "wastes", landCoverage: 24, temperature: 120, relief: 600, roughness: 400),
        ]);

        var valley = taste.Score(IceRemembersKinds.Settlement, culture: null, new HexCoord(0, 0));
        var waste = taste.Score(IceRemembersKinds.Settlement, culture: null, new HexCoord(1, 0));

        Assert.True(valley > waste, $"settlements should price the warm valley above frozen waste: {valley} vs {waste}");
    }

    [Fact]
    public void CultureTaste_ReordersTheSameKindsPreferences()
    {
        var taste = new IceRemembersSeedTaste(
        [
            Cell(q: 0, r: 0, terrain: "aurora_field", landCoverage: 24, temperature: 300, aurora: 700),
            Cell(q: 1, r: 0, terrain: "ice", landCoverage: 24, temperature: 300, ice: 700),
        ]);

        var stackAtLights = taste.Score(IceRemembersKinds.Penguin, Culture(IceRemembersCulture.AuroraStack), new HexCoord(0, 0));
        var stackOnIce = taste.Score(IceRemembersKinds.Penguin, Culture(IceRemembersCulture.AuroraStack), new HexCoord(1, 0));
        var shelfAtLights = taste.Score(IceRemembersKinds.Penguin, Culture(IceRemembersCulture.Nightshelf), new HexCoord(0, 0));
        var shelfOnIce = taste.Score(IceRemembersKinds.Penguin, Culture(IceRemembersCulture.Nightshelf), new HexCoord(1, 0));

        Assert.True(stackAtLights > stackOnIce, $"Aurora Stack should seek the lights: {stackAtLights} vs {stackOnIce}");
        Assert.True(shelfOnIce > shelfAtLights, $"Nightshelf should shun the lights for thick ice: {shelfOnIce} vs {shelfAtLights}");
    }

    [Fact]
    public void CoastTaste_DerivesFromTheLandWaterDivide()
    {
        var taste = new IceRemembersSeedTaste(
        [
            Cell(q: 0, r: 0, terrain: "tundra", landCoverage: 24, temperature: 300),
            Cell(q: 1, r: 0, terrain: "ocean", landCoverage: 0, temperature: 300),
            Cell(q: -2, r: 0, terrain: "tundra", landCoverage: 24, temperature: 300),
            Cell(q: -3, r: 0, terrain: "tundra", landCoverage: 24, temperature: 300),
        ]);

        var coastal = taste.Score(IceRemembersKinds.Settlement, culture: null, new HexCoord(0, 0));
        var interior = taste.Score(IceRemembersKinds.Settlement, culture: null, new HexCoord(-2, 0));

        Assert.True(coastal > interior, $"coastal ground should outprice the interior for settlements: {coastal} vs {interior}");
    }

    private static string Culture(IceRemembersCulture culture) => culture.ToDomainName();

    private static DomainPhysicalGeographyCell Cell(
        int q,
        int r,
        string terrain,
        int landCoverage,
        int temperature = 0,
        int riverCoverage = 0,
        int floodplainCoverage = 0,
        int relief = 0,
        int roughness = 0,
        int ice = 0,
        int aurora = 0,
        int pressure = 0) =>
        new(
            q,
            r,
            terrain,
            SuperregionId: 1,
            ContinentId: 1,
            CrossingWord: 0,
            FieldWord: 0,
            InitialCoverTerrain: null,
            MicrotriangleOffset: 0,
            DominantMaterialIndex: 0,
            PrimarySurfaceIndex: 0,
            SignificantFeatureFlags: 0,
            MeanElevation: 0,
            MinElevation: 0,
            MaxElevation: 0,
            Relief: relief,
            Roughness: roughness,
            MeanTemperature: temperature,
            MeanMoisture: 0,
            MeanGeothermal: 0,
            LandCoverage: landCoverage,
            WaterCoverage: 24 - landCoverage,
            OceanCoverage: 24 - landCoverage,
            LakeCoverage: 0,
            PermanentIceCoverage: 0,
            RiverCoverage: riverCoverage,
            FloodplainCoverage: floodplainCoverage,
            DeltaCoverage: 0,
            WideRiverCoverage: 0,
            PeakCount: 0,
            MeanIceThickness: ice,
            MaxIceThickness: ice,
            MeanFireIntensity: 0,
            MaxFireIntensity: 0,
            MeanAuroraIntensity: aurora,
            MaxAuroraIntensity: aurora,
            MeanPressureIntensity: pressure,
            MaxPressureIntensity: pressure,
            RiverEdgeMask: 0,
            TrunkRiverEdgeMask: 0,
            CoastEdgeMask: 0,
            RidgeEdgeMask: 0,
            CliffEdgeMask: 0,
            PassEdgeMask: 0,
            FractureEdgeMask: 0);
}
