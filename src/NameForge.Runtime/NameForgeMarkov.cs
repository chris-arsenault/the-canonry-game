namespace NameForge.Runtime;

using System.Reflection;
using System.Text.Json;

public sealed class NameForgeMarkovModel
{
    public int Order { get; init; }
    public Dictionary<string, double> StartStates { get; init; } = new(StringComparer.Ordinal);
    public Dictionary<string, Dictionary<string, double>> Transitions { get; init; } = new(StringComparer.Ordinal);
}

public interface INameForgeMarkovModelProvider
{
    bool TryGetModel(string modelId, out NameForgeMarkovModel model);
}

public sealed class NameForgeEmbeddedMarkovModelProvider : INameForgeMarkovModelProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly Dictionary<string, NameForgeMarkovModel?> _cache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _resourceNames;

    public static NameForgeEmbeddedMarkovModelProvider Instance { get; } = new();

    private NameForgeEmbeddedMarkovModelProvider()
    {
        _resourceNames = Assembly.GetExecutingAssembly()
            .GetManifestResourceNames()
            .Where(static name => name.StartsWith("NameForge.Runtime.Data.Markov.Models.", StringComparison.Ordinal))
            .ToDictionary(ModelIdFromResourceName, StringComparer.Ordinal);
    }

    public bool TryGetModel(string modelId, out NameForgeMarkovModel model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);

        if (!_cache.TryGetValue(modelId, out var cached))
        {
            cached = LoadModel(modelId);
            _cache[modelId] = cached;
        }

        if (cached is null)
        {
            model = new NameForgeMarkovModel();
            return false;
        }

        model = cached;
        return true;
    }

    private NameForgeMarkovModel? LoadModel(string modelId)
    {
        if (!_resourceNames.TryGetValue(modelId, out var resourceName))
        {
            return null;
        }

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        return stream is null
            ? null
            : JsonSerializer.Deserialize<NameForgeMarkovModel>(stream, JsonOptions);
    }

    private static string ModelIdFromResourceName(string resourceName)
    {
        var prefixLength = "NameForge.Runtime.Data.Markov.Models.".Length;
        var modelWithExtension = resourceName[prefixLength..];
        return modelWithExtension[..^".json".Length];
    }
}

internal static class NameForgeMarkov
{
    public static string Generate(NameForgeMarkovModel model, NameForgeRandom rng, int minLength = 3, int maxLength = 12)
    {
        if (model.StartStates.Count == 0)
        {
            return string.Empty;
        }

        var state = WeightedRandom(model.StartStates, rng);
        var result = string.Empty;

        for (var i = 0; i < maxLength + model.Order; i++)
        {
            if (!model.Transitions.TryGetValue(state, out var nextProbabilities) || nextProbabilities.Count == 0)
            {
                break;
            }

            var next = WeightedRandom(nextProbabilities, rng);
            if (string.Equals(next, "$", StringComparison.Ordinal))
            {
                if (result.Length >= minLength)
                {
                    break;
                }

                continue;
            }

            result += next;
            state = state.Length <= 1 ? next : state[1..] + next;
        }

        return NameForgeText.Capitalize(result);
    }

    private static string WeightedRandom(IReadOnlyDictionary<string, double> probabilities, NameForgeRandom rng)
    {
        var roll = rng.NextDouble();
        var cumulative = 0d;
        foreach (var (item, probability) in probabilities)
        {
            cumulative += probability;
            if (roll <= cumulative)
            {
                return item;
            }
        }

        return probabilities.Keys.First();
    }
}
