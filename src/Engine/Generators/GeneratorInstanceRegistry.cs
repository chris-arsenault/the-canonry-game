namespace Engine.Generators;

using Engine.Domain.Graph;

/// <summary>
/// Run-scoped generator state keyed by generator id.
/// </summary>
public sealed class GeneratorInstanceRegistry
{
    private readonly Dictionary<string, GeneratorInstance> _instances = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, GeneratorInstance> ById => _instances;
    public int Count => _instances.Count;

    public GeneratorInstance GetOrCreate(DefinitionGeneratorNode definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (_instances.TryGetValue(definition.Id, out var existing))
        {
            return existing;
        }

        var instance = new GeneratorInstance(definition);
        _instances.Add(instance.GeneratorId, instance);
        return instance;
    }

    public bool TryGet(string generatorId, out GeneratorInstance? instance)
    {
        ArgumentException.ThrowIfNullOrEmpty(generatorId);
        return _instances.TryGetValue(generatorId, out instance);
    }

    public void Reset()
    {
        foreach (var instance in _instances.Values)
        {
            instance.Reset();
        }
    }
}
