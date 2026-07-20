namespace IceRemembers.Seed;

using Engine.Constants;
using Engine.Domain.Startup;
using Engine.Graph;
using Engine.Kinds;
using Engine.WorldGen;

/// <summary>
/// The accepted seed plus why the attempts before it were turned down.
/// Rejections used to be discarded unless every attempt failed, so a change
/// that degraded the world enough to burn several attempts showed up only as a
/// different attempt number.
/// </summary>
internal sealed record IceRemembersStartupSeedResult(
    WorldGenerationSeed Seed,
    DomainStartupSeed StartupSeed,
    IReadOnlyList<string> RejectedAttempts);

internal sealed class SeedAssembly
{
    private readonly List<SeedEntity> _entities = [];
    private readonly List<DomainSeedRelationship> _relationships = [];
    private readonly Dictionary<SemanticHexCoord, DomainSeedCell> _semanticCells = [];
    private DomainSemanticTerrainBlueprint _semanticTerrain = DomainSemanticTerrainBlueprint.Empty;

    public SeedAssembly(
        HexGrid grid,
        MapCoverageMap coverageMap,
        MapSubdivisionMap subdivisionMap,
        IReadOnlyList<DomainMapSubdivision> mapSubdivisions,
        DomainPhysicalGeographyBlueprint physicalGeography)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(coverageMap);
        ArgumentNullException.ThrowIfNull(subdivisionMap);
        ArgumentNullException.ThrowIfNull(mapSubdivisions);
        ArgumentNullException.ThrowIfNull(physicalGeography);
        Grid = grid;
        CoverageMap = coverageMap;
        SubdivisionMap = subdivisionMap;
        MapSubdivisions = mapSubdivisions;
        PhysicalGeography = physicalGeography;
    }

    public HexGrid Grid { get; }

    public MapCoverageMap CoverageMap { get; }

    public MapSubdivisionMap SubdivisionMap { get; }

    public IReadOnlyList<DomainMapSubdivision> MapSubdivisions { get; }

    public DomainPhysicalGeographyBlueprint PhysicalGeography { get; }

    public IReadOnlyList<SeedEntity> Entities => _entities;

    public IReadOnlyList<DomainSeedRelationship> Relationships => _relationships;

    public IReadOnlyDictionary<SemanticHexCoord, DomainSeedCell> SemanticCells => _semanticCells;

    public void SetSemanticTerrainBlueprint(DomainSemanticTerrainBlueprint blueprint)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        _semanticTerrain = blueprint;
    }

    public SeedEntity CreateEntity(
        EntityKind kind,
        string subtype,
        string name,
        string referenceName,
        string culture,
        (int Q, int R)? coordinates = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceName);

        var entity = new SeedEntity(
            _entities.Count,
            kind,
            subtype,
            name,
            referenceName,
            culture,
            coordinates is { } coord ? new HexCoord(coord.Q, coord.R) : new HexCoord(0, 0));
        _entities.Add(entity);
        return entity;
    }

    public SeedEntity Entity(int seedEntityId)
    {
        if (seedEntityId < 0 || seedEntityId >= _entities.Count)
        {
            throw new InvalidOperationException($"Unknown seed entity {seedEntityId}.");
        }

        return _entities[seedEntityId];
    }

    public void CreateRelationship(
        string kind,
        RelationshipPolarity polarity,
        int sourceEntityId,
        int targetEntityId,
        float strength)
    {
        _ = Entity(sourceEntityId);
        _ = Entity(targetEntityId);
        _relationships.Add(new DomainSeedRelationship(
            kind,
            polarity,
            sourceEntityId,
            targetEntityId,
            Math.Clamp(strength, 0f, 1f)));
    }

    public void SetSemanticCell(
        SemanticHexCoord coord,
        string terrain,
        int? ownerSeedEntityId,
        int superregionId,
        int continentId)
    {
        if (ownerSeedEntityId is { } owner)
        {
            _ = Entity(owner);
        }

        var resolvedOwner = ownerSeedEntityId;
        if (resolvedOwner is null && _semanticCells.TryGetValue(coord, out var existing))
        {
            resolvedOwner = existing.OwnerSeedEntityId;
        }

        _semanticCells[coord] = new DomainSeedCell(
            coord.Q,
            coord.R,
            terrain,
            resolvedOwner,
            superregionId,
            continentId,
            PlaneCode: PlaneCodes.Semantic);
    }

    public DomainStartupSeed ToStartupSeed()
    {
        var entities = CreateStartupEntities();
        var cells = CreateStartupCells();
        var relationships = CreateStartupRelationships();

        return new DomainStartupSeed(
            Grid.Radius,
            entities,
            cells,
            relationships,
            MapSubdivisions,
            Attributes: [],
            KnowledgeSites: CreateKnowledgeSites(),
            PhysicalGeography,
            _semanticTerrain);
    }

    // The six discipline spokes (unit hex directions from centre). A tech at
    // tier t, discipline d sits at ring (t*TierWidth + TierWidth/2) along spoke d.
    private static readonly (int Q, int R)[] DisciplineSpokes =
    [
        (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1),
    ];

    private static DomainKnowledgeSite[] CreateKnowledgeSites()
    {
        var techs = IceRemembersKnowledge.Techs;
        var sites = new DomainKnowledgeSite[techs.Count];
        for (var i = 0; i < techs.Count; i++)
        {
            var tech = techs[i];
            var ring = (tech.Tier * IceRemembersKnowledge.TierWidth) + (IceRemembersKnowledge.TierWidth / 2);
            var spoke = DisciplineSpokes[(int)tech.Discipline % DisciplineSpokes.Length];
            sites[i] = new DomainKnowledgeSite(tech.Id, ring * spoke.Q, ring * spoke.R, tech.Tier, tech.Grants);
        }

        return sites;
    }

    private DomainSeedEntity[] CreateStartupEntities() =>
        _entities
            .OrderBy(entity => entity.SeedEntityId)
            .Select(entity => new DomainSeedEntity(
                entity.SeedEntityId,
                entity.Kind.Value,
                entity.Subtype,
                entity.Culture,
                entity.Coordinates.Q,
                entity.Coordinates.R,
                entity.SemanticPosition.Q,
                entity.SemanticPosition.R,
                SemanticS(entity.SemanticPosition),
                entity.Name,
                entity.ReferenceName))
            .ToArray();

    private DomainSeedCell[] CreateStartupCells()
    {
        var physicalCells = Grid.AllCells
            .OrderBy(cell => cell.Coord.Q)
            .ThenBy(cell => cell.Coord.R)
            .Select(cell =>
            {
                var subdivision = SubdivisionMap.GetCell(cell.Coord);
                return new DomainSeedCell(
                    cell.Coord.Q,
                    cell.Coord.R,
                    cell.Terrain.StableName,
                    CoverageMap.GetCoverageForCell(cell.Coord)?.OwnerEntityId,
                    subdivision.SuperregionId,
                    subdivision.ContinentId);
            });
        var semanticCells = _semanticCells.Values
            .OrderBy(cell => cell.Q)
            .ThenBy(cell => cell.R);
        return physicalCells.Concat(semanticCells).ToArray();
    }

    private DomainSeedRelationship[] CreateStartupRelationships() =>
        _relationships
            .OrderBy(relationship => relationship.SourceSeedEntityId)
            .ThenBy(relationship => relationship.RelationshipKind, StringComparer.Ordinal)
            .ThenBy(relationship => relationship.TargetSeedEntityId)
            .ThenBy(relationship => relationship.Strength)
            .ToArray();

    private static int SemanticS(SemanticHexCoord coord) => -coord.Q - coord.R;
}

internal sealed class SeedEntity(
    int seedEntityId,
    EntityKind kind,
    string subtype,
    string name,
    string referenceName,
    string culture,
    HexCoord coordinates)
{
    public int SeedEntityId { get; } = seedEntityId;

    public EntityKind Kind { get; } = kind;

    public string Subtype { get; } = subtype;

    public string Name { get; } = name;

    public string ReferenceName { get; } = referenceName;

    public string Culture { get; } = culture;

    public HexCoord Coordinates { get; set; } = coordinates;

    public SemanticHexCoord SemanticPosition { get; set; } = SemanticHexCoord.Origin;
}
