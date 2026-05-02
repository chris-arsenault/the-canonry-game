namespace Engine.WorldGen;

public sealed record TerrainDefinition(
    string StableName,
    string DisplayName,
    string BoundaryGroup);

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
        if (string.IsNullOrWhiteSpace(terrain.StableName))
        {
            throw new ArgumentException("Terrain stable name cannot be blank.", nameof(terrain));
        }

        if (string.IsNullOrWhiteSpace(terrain.DisplayName))
        {
            throw new ArgumentException("Terrain display name cannot be blank.", nameof(terrain));
        }

        if (string.IsNullOrWhiteSpace(terrain.BoundaryGroup))
        {
            throw new ArgumentException("Terrain boundary group cannot be blank.", nameof(terrain));
        }

        if (!_terrains.TryAdd(terrain.StableName, terrain))
        {
            throw new InvalidOperationException($"Duplicate terrain '{terrain.StableName}'.");
        }
    }
}
