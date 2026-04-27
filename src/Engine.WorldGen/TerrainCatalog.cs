namespace Engine.WorldGen;

public sealed record TerrainDefinition(
    string StableName,
    string DisplayName,
    float MovementCost,
    float HabitabilityFactor,
    string BoundaryGroup,
    IReadOnlyDictionary<string, AmbientValueRange> AmbientResources)
{
    public TerrainDefinition(
        string stableName,
        string displayName,
        float movementCost,
        float habitabilityFactor,
        string boundaryGroup)
        : this(
            stableName,
            displayName,
            movementCost,
            habitabilityFactor,
            boundaryGroup,
            new Dictionary<string, AmbientValueRange>(StringComparer.Ordinal))
    {
    }
}

public sealed class TerrainCatalog
{
    private readonly Dictionary<string, TerrainDefinition> _terrains = new(StringComparer.Ordinal);

    public TerrainCatalog(IEnumerable<TerrainDefinition> terrains)
    {
        ArgumentNullException.ThrowIfNull(terrains);
        foreach (var terrain in terrains)
        {
            Register(terrain);
        }
    }

    public IReadOnlyCollection<TerrainDefinition> Terrains => _terrains.Values;

    public TerrainDefinition Resolve(string stableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stableName);
        if (_terrains.TryGetValue(stableName, out var terrain))
        {
            return terrain;
        }

        throw new KeyNotFoundException($"Unknown terrain '{stableName}'.");
    }

    private void Register(TerrainDefinition terrain)
    {
        ArgumentNullException.ThrowIfNull(terrain);
        ArgumentException.ThrowIfNullOrWhiteSpace(terrain.StableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(terrain.DisplayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(terrain.BoundaryGroup);
        if (!_terrains.TryAdd(terrain.StableName, terrain))
        {
            throw new InvalidOperationException($"Duplicate terrain '{terrain.StableName}'.");
        }
    }
}
