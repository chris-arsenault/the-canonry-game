namespace IceRemembers.Seed;

using Engine.Domain.Startup;
using Engine.Graph;
using Engine.Kinds;
using Engine.WorldGen;

using IceRemembers;

/// <summary>
/// Builds a seeded procedural map with named starter regions and anchors.
/// The same seed produces the same world; different seeds should produce
/// meaningfully different terrain and starting layouts.
/// </summary>
internal static partial class IceRemembersSeedBuilder
{
    private const int MaxAcceptedSeedAttempts = 64;
    private const int StartupMapRadius = 40;

    private static string C(IceRemembersCulture culture) => culture.ToDomainName();

    private static SeedEntity CreateEntity(
        SeedAssembly seed,
        EntityKind kind,
        string subtype,
        string name,
        string culture,
        (int Q, int R)? coordinates = null) =>
        seed.CreateEntity(
            kind,
            subtype,
            name,
            culture,
            coordinates);

    private static void CreateRelationship(
        SeedAssembly seed,
        string kind,
        RelationshipPolarity polarity,
        int sourceEntityId,
        int targetEntityId,
        float strength) =>
        seed.CreateRelationship(
            kind,
            polarity,
            sourceEntityId,
            targetEntityId,
            strength);

    /// <summary>
    /// Build the default startup seed from an accepted procedural seed. A fixed
    /// input seed always resolves to the same accepted terrain and seed roster.
    /// </summary>
    public static DomainStartupSeed BuildDefault(int? seed = null)
    {
        var baseSeed = seed ?? Random.Shared.Next();
        return BuildAcceptedStartupSeed(baseSeed).StartupSeed;
    }

    /// <summary>
    /// Build the Ice Remembers startup seed in the domain-stable shape consumed by Engine.Host.
    /// The output uses domain-stable startup data rather than kernel runtime IDs.
    /// </summary>
    public static DomainStartupSeed BuildStartupSeed(int? seed = null)
    {
        var baseSeed = seed ?? Random.Shared.Next();
        return BuildAcceptedStartupSeed(baseSeed).StartupSeed;
    }

    public static DomainStartupSeed BuildStartupSeed(WorldGenerationSeed seed)
    {
        var assembly = BuildEmptyAssembly(seed);
        CreateSemanticMap(assembly, seed.TerrainSeed ^ seed.EntitySeed);
        Populate(assembly, seed);
        return assembly.ToStartupSeed();
    }

    public static IceRemembersStartupSeedResult BuildAcceptedStartupSeed(int baseSeed)
    {
        return BuildAcceptedStartupSeed(
            baseSeed,
            BuildStartupSeed,
            IceRemembersSeedQualityValidator.Validate,
            MaxAcceptedSeedAttempts);
    }

    public static WorldGenerationSeed SelectAcceptedSeed(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        return SelectAcceptedSeed(random.Next());
    }

    public static WorldGenerationSeed SelectAcceptedSeed(int baseSeed)
        => BuildAcceptedStartupSeed(baseSeed).Seed;

    internal static IceRemembersStartupSeedResult BuildAcceptedStartupSeed(
        int baseSeed,
        Func<WorldGenerationSeed, DomainStartupSeed> buildSeed,
        Func<DomainStartupSeed, SeedWorldQualityReport> validateSeed,
        int maxAttempts)
    {
        ArgumentNullException.ThrowIfNull(buildSeed);
        ArgumentNullException.ThrowIfNull(validateSeed);

        IReadOnlyList<string> lastIssues = [];
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var seed = WorldGenerationSeed.FromBaseSeed(baseSeed, attempt);
            try
            {
                var startupSeed = buildSeed(seed);
                var report = validateSeed(startupSeed);
                if (report.IsValid)
                {
                    return new IceRemembersStartupSeedResult(seed, startupSeed);
                }

                lastIssues = report.Issues;
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                lastIssues =
                [
                    $"{ex.GetType().Name}: {ex.Message}",
                ];
            }
        }

        throw new InvalidOperationException(
            $"Could not build an accepted procedural seed for base seed {baseSeed}. " +
            $"Last issues: {string.Join("; ", lastIssues)}");
    }

    private static SeedAssembly BuildEmptyAssembly(WorldGenerationSeed seed)
    {
        var grid = new HexGrid(
            StartupMapRadius,
            IceRemembersTerrainCatalog.CreateWorldGenCatalog(),
            IceRemembersTerrain.Wastes.ToDomainName());
        var coverageMap = new MapCoverageMap();
        var subdivisionMap = CreateSubdivisions(grid, seed.TerrainSeed);
        PaintTerrain(grid, new Random(seed.TerrainSeed), subdivisionMap);
        return new SeedAssembly(grid, coverageMap, subdivisionMap, CreateSubdivisionDefinitions());
    }

    private static void Populate(SeedAssembly assembly, WorldGenerationSeed seed)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var regions = CreateRegionEntities(assembly);
        CreateEntities(assembly, assembly.Grid, assembly.CoverageMap, regions, new Random(seed.EntitySeed));
    }
}
