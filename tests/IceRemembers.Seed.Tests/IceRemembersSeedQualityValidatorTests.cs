namespace IceRemembers.Seed.Tests;

using Engine.Domain.Startup;

/// <summary>
/// Validator teeth: a seed whose placement broke the land/water partition,
/// clumped a kind onto one spot, or produced an unlivable heart fails
/// loudly with a named issue instead of shipping a broken world.
/// </summary>
public sealed class IceRemembersSeedQualityValidatorTests
{
    [Fact]
    public void PenguinOnOcean_FailsThePartitionCheck()
    {
        var seed = Seed(
            cells: [Cell(0, 0, "ocean"), Cell(1, 0, "tundra")],
            entities:
            [
                Entity(1, "penguin", "witness", "aurora_stack", 0, 0),
                Entity(2, "settlement", "rookery", "aurora_stack", 1, 0),
            ]);

        var report = IceRemembersSeedQualityValidator.Validate(seed);

        Assert.Contains(report.Issues, issue => issue.Contains("avoided terrain 'ocean'", StringComparison.Ordinal));
    }

    [Fact]
    public void OrcaAtSea_PassesThePartitionCheck()
    {
        var seed = Seed(
            cells: [Cell(0, 0, "ocean"), Cell(1, 0, "tundra")],
            entities:
            [
                Entity(1, "orca", "matriarch", "wake_singers", 0, 0),
                Entity(2, "penguin", "witness", "aurora_stack", 1, 0),
            ]);

        var report = IceRemembersSeedQualityValidator.Validate(seed);

        Assert.DoesNotContain(report.Issues, issue => issue.Contains("avoided terrain", StringComparison.Ordinal));
    }

    [Fact]
    public void ClumpedKind_FailsTheDispersionCheck()
    {
        var seed = Seed(
            cells:
            [
                Cell(0, 0, "tundra"), Cell(1, 0, "tundra"), Cell(0, 1, "tundra"), Cell(1, -1, "tundra"),
            ],
            entities:
            [
                Entity(1, "penguin", "witness", "aurora_stack", 0, 0),
                Entity(2, "penguin", "broker", "nightshelf", 1, 0),
                Entity(3, "penguin", "warden", "aurora_stack", 0, 1),
                Entity(4, "penguin", "exile", "nightshelf", 1, -1),
            ]);

        var report = IceRemembersSeedQualityValidator.Validate(seed);

        Assert.Contains(report.Issues, issue => issue.Contains("'penguin' is clustered", StringComparison.Ordinal));
    }

    [Fact]
    public void SpreadKind_PassesTheDispersionCheck()
    {
        var seed = Seed(
            cells:
            [
                Cell(0, 0, "tundra"), Cell(4, 0, "tundra"), Cell(0, 4, "tundra"), Cell(-4, 0, "tundra"),
            ],
            entities:
            [
                Entity(1, "penguin", "witness", "aurora_stack", 0, 0),
                Entity(2, "penguin", "broker", "nightshelf", 4, 0),
                Entity(3, "penguin", "warden", "aurora_stack", 0, 4),
                Entity(4, "penguin", "exile", "nightshelf", -4, 0),
            ]);

        var report = IceRemembersSeedQualityValidator.Validate(seed);

        Assert.DoesNotContain(report.Issues, issue => issue.Contains("clustered", StringComparison.Ordinal));
    }

    [Fact]
    public void FrozenHeart_FailsTheViabilityCheck()
    {
        // Every blueprint cell is dead cold: capacity derives to zero, so
        // the heart cannot carry life no matter what the terrain is called.
        var blueprintCells = new List<DomainPhysicalGeographyCell>();
        var seedCells = new List<DomainSeedCell>();
        for (var q = 0; q < 50; q++)
        {
            blueprintCells.Add(BlueprintCell(q, 0, temperature: 0));
            seedCells.Add(Cell(q, 0, "ice"));
        }

        var seed = Seed(
            cells: seedCells,
            entities: [Entity(1, "penguin", "witness", "aurora_stack", 0, 0)],
            blueprint: Blueprint(blueprintCells));

        var report = IceRemembersSeedQualityValidator.Validate(seed);

        Assert.Contains(report.Issues, issue => issue.Contains("habitable cells", StringComparison.Ordinal));
    }

    [Fact]
    public void WarmHeart_PassesTheViabilityCheck()
    {
        var blueprintCells = new List<DomainPhysicalGeographyCell>();
        var seedCells = new List<DomainSeedCell>();
        for (var q = 0; q < 50; q++)
        {
            blueprintCells.Add(BlueprintCell(q, 0, temperature: 600));
            seedCells.Add(Cell(q, 0, "tundra"));
        }

        var seed = Seed(
            cells: seedCells,
            entities: [Entity(1, "penguin", "witness", "aurora_stack", 0, 0)],
            blueprint: Blueprint(blueprintCells));

        var report = IceRemembersSeedQualityValidator.Validate(seed);

        Assert.DoesNotContain(report.Issues, issue => issue.Contains("habitable cells", StringComparison.Ordinal));
    }

    private static DomainStartupSeed Seed(
        IReadOnlyList<DomainSeedCell> cells,
        IReadOnlyList<DomainSeedEntity> entities,
        DomainPhysicalGeographyBlueprint? blueprint = null) =>
        new(
            MapRadius: 8,
            Entities: entities,
            Cells: cells,
            Relationships: [],
            MapSubdivisions: [],
            Attributes: [],
            KnowledgeSites: [],
            PhysicalGeography: blueprint);

    private static DomainSeedCell Cell(int q, int r, string terrain) => new(q, r, terrain);

    private static DomainSeedEntity Entity(
        int id,
        string kind,
        string subtype,
        string culture,
        int q,
        int r) =>
        new(id, kind, subtype, culture, q, r, SemanticQ: id * 3, SemanticR: 0, SemanticS: -id * 3);

    private static DomainPhysicalGeographyBlueprint Blueprint(
        IReadOnlyList<DomainPhysicalGeographyCell> cells) =>
        new(
            Radius: 8,
            GenerationEpochCount: 1,
            MaterialPalette: ["stone"],
            SurfacePalette: ["ice"],
            FeaturePalette: [],
            ScalarFields: [],
            Cells: cells,
            Microtriangles: [],
            GeologicalFeatures: [],
            GeologicalFeatureCoverage: []);

    private static DomainPhysicalGeographyCell BlueprintCell(int q, int r, int temperature) =>
        new(
            q,
            r,
            "ice",
            SuperregionId: 1,
            ContinentId: 1,
            CrossingWord: 0,
            FieldWord: 0,
            InitialCoverTerrain: null,
            MicrotriangleOffset: 0,
            DominantMaterialIndex: 0,
            PrimarySurfaceIndex: 0,
            SignificantFeatureFlags: 0,
            MeanElevation: 100,
            MinElevation: 0,
            MaxElevation: 200,
            Relief: 0,
            Roughness: 0,
            MeanTemperature: temperature,
            MeanMoisture: 0,
            MeanGeothermal: 0,
            LandCoverage: 24,
            WaterCoverage: 0,
            OceanCoverage: 0,
            LakeCoverage: 0,
            PermanentIceCoverage: 0,
            RiverCoverage: 0,
            FloodplainCoverage: 0,
            DeltaCoverage: 0,
            WideRiverCoverage: 0,
            PeakCount: 0,
            MeanIceThickness: 0,
            MaxIceThickness: 0,
            MeanFireIntensity: 0,
            MaxFireIntensity: 0,
            MeanAuroraIntensity: 0,
            MaxAuroraIntensity: 0,
            MeanPressureIntensity: 0,
            MaxPressureIntensity: 0,
            RiverEdgeMask: 0,
            TrunkRiverEdgeMask: 0,
            CoastEdgeMask: 0,
            RidgeEdgeMask: 0,
            CliffEdgeMask: 0,
            PassEdgeMask: 0,
            FractureEdgeMask: 0);
}
