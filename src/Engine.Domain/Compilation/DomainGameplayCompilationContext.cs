namespace Engine.Domain.Compilation;

using Engine.Constants;
using Engine.Domain.Config;
using Engine.Gameplay;

internal sealed class DomainGameplayCompilationContext : IGameplayCompilationContext
{
    private readonly DomainConfig _config;
    private readonly DomainIdRegistry _ids;

    public DomainGameplayCompilationContext(DomainConfig config, DomainIdRegistry ids)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(ids);
        _config = config;
        _ids = ids;
    }

    public int ResolveKindId(string kind, string context) =>
        _ids.KindIdOf(ResolveKind(kind, context)).Value;

    public int PrimaryPlaneCodeForKind(string kind, string context) =>
        _config.Kinds[ResolveKind(kind, context)].PrimaryPlane == PrimaryPlane.Semantic
            ? PlaneCodes.Semantic
            : PlaneCodes.Physical;

    public int ResolveVerbId(string verb) => _ids.VerbIdOf(verb).Value;

    public int ResolveTerrainId(string terrain) => _ids.TerrainIdOf(terrain).Value;

    public int ResolveRelationshipKindId(string relationship) =>
        _ids.RelationshipKindIdOf(relationship).Value;

    public int ResolveAttributeId(string attribute) => _ids.AttributeIdOf(attribute).Value;

    public bool TerrainExists(string terrain) =>
        _config.Terrains.Any(candidate => string.Equals(candidate.Name, terrain, StringComparison.Ordinal));

    public bool RelationshipExists(string relationship) =>
        _config.Relationships.Any(candidate => string.Equals(candidate.Name, relationship, StringComparison.Ordinal));

    public bool KindHasAttribute(string kind, string attribute)
    {
        var kindId = ResolveKind(kind, "Gameplay kind");
        return _config.Kinds[kindId].Attributes.Any(candidate =>
            string.Equals(candidate.Name, attribute, StringComparison.Ordinal));
    }

    public IReadOnlyList<GeneratorAffordanceDefinition> GeneratorAffordances()
    {
        var affordances = new List<GeneratorAffordanceDefinition>();
        var seen = new HashSet<GeneratorAffordanceDefinition>();
        if (_config.Semantics is null)
        {
            return [];
        }

        foreach (var generator in _config.Semantics.Generators)
        {
            var targetKind = generator.Target.Kind.Value;
            AddAffordance(affordances, seen, targetKind, targetKind, targetKind);
            foreach (var binding in generator.SecondaryTargets)
            {
                var bindingKind = binding.Kind.Value;
                AddAffordance(affordances, seen, targetKind, bindingKind, bindingKind);
                AddAffordance(affordances, seen, bindingKind, targetKind, bindingKind);
                AddAffordance(affordances, seen, bindingKind, bindingKind, bindingKind);
            }

            foreach (var produced in generator.Produces)
            {
                var producedKind = produced.Kind.Value;
                AddAffordance(affordances, seen, targetKind, producedKind, producedKind);
                AddAffordance(affordances, seen, producedKind, targetKind, producedKind);
                AddAffordance(affordances, seen, producedKind, producedKind, producedKind);
            }
        }

        return affordances;
    }

    private static void AddAffordance(
        List<GeneratorAffordanceDefinition> affordances,
        HashSet<GeneratorAffordanceDefinition> seen,
        string actorKind,
        string candidateKind,
        string bindingKind)
    {
        var affordance = new GeneratorAffordanceDefinition(actorKind, candidateKind, bindingKind);
        if (seen.Add(affordance))
        {
            affordances.Add(affordance);
        }
    }

    private EntityKind ResolveKind(string kindName, string context)
    {
        foreach (var kind in _config.Kinds.Keys)
        {
            if (string.Equals(kind.Value, kindName, StringComparison.Ordinal))
            {
                return kind;
            }
        }

        throw new InvalidOperationException($"{context} references unknown kind '{kindName}'.");
    }
}
