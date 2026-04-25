namespace Engine.Tests;

using Engine.Domain;
using Engine.Kinds;
using Engine.Pressure;

internal static class TestDefaultPressures
{
    public static PressureDefinition MilitaryTension()
    {
        var def = new PressureDefinition("military_tension", PressureScope.Regional,
            baseline: 0f, homeostasisRate: 0.05f, smoothingFactor: 0.2f,
            runtimeId: new PressureRuntimeId(1));
        def.Factors.Add(new EntityCountFactor("faction_count", TestKinds.Faction, coefficient: 8f));
        def.Factors.Add(new NegativeRelationshipFactor("rivalries", "rival_of", coefficient: 15f));
        def.Factors.Add(new PositiveRelationshipFactor("alliances", "allied_with", coefficient: -5f));
        return def;
    }

    public static PressureDefinition CulturalFriction()
    {
        var def = new PressureDefinition("cultural_friction", PressureScope.Regional,
            baseline: 0f, homeostasisRate: 0.06f, smoothingFactor: 0.2f,
            runtimeId: new PressureRuntimeId(2));
        def.Factors.Add(new CultureDiversityFactor("culture_diversity", coefficient: 10f));
        return def;
    }

    public static PressureDefinition EconomicStress()
    {
        var def = new PressureDefinition("economic_stress", PressureScope.Regional,
            baseline: 0f, homeostasisRate: 0.08f, smoothingFactor: 0.25f,
            runtimeId: new PressureRuntimeId(3));
        def.Factors.Add(new LowAttributeFactor("low_prosperity", TestKinds.Settlement,
            "security", threshold: 30f, coefficient: 5f));
        return def;
    }

    public static PressureDefinition DemographicPressure()
    {
        var def = new PressureDefinition("demographic_pressure", PressureScope.Regional,
            baseline: 0f, homeostasisRate: 0.1f, smoothingFactor: 0.3f,
            runtimeId: new PressureRuntimeId(4));
        def.Factors.Add(new EntityCountFactor("settlement_count", TestKinds.Settlement, coefficient: 5f));
        return def;
    }

    public static IEnumerable<PressureDefinition> All()
    {
        yield return MilitaryTension();
        yield return EconomicStress();
        yield return CulturalFriction();
        yield return DemographicPressure();
    }
}

internal sealed class EntityCountFactor(string name, EntityKind kind, float coefficient) : IContributingFactor
{
    public string Name => name;

    public float Evaluate(IPressureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.CountEntities(e => e.Kind == kind) * coefficient;
    }
}

internal sealed class NegativeRelationshipFactor(string name, string relationshipKind, float coefficient) : IContributingFactor
{
    public string Name => name;

    public float Evaluate(IPressureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.CountRelationships(r => r.Kind == relationshipKind && r.Polarity == Engine.Graph.RelationshipPolarity.Negative) * coefficient;
    }
}

internal sealed class PositiveRelationshipFactor(string name, string relationshipKind, float coefficient) : IContributingFactor
{
    public string Name => name;

    public float Evaluate(IPressureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.CountRelationships(r => r.Kind == relationshipKind && r.Polarity == Engine.Graph.RelationshipPolarity.Positive) * coefficient;
    }
}

internal sealed class LowAttributeFactor(string name, EntityKind kind, string attributeName, float threshold, float coefficient) : IContributingFactor
{
    public string Name => name;

    public float Evaluate(IPressureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.CountEntities(e =>
            e.Kind == kind &&
            e.Attributes.GetValue(attributeName) < threshold) * coefficient;
    }
}

internal sealed class CultureDiversityFactor(string name, float coefficient) : IContributingFactor
{
    public string Name => name;

    public float Evaluate(IPressureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var cultures = context.EntitiesInScope
            .Select(context.GetEntity)
            .Where(entity => entity is not null)
            .Select(entity => entity!.Culture)
            .Where(culture => !string.IsNullOrWhiteSpace(culture))
            .Distinct(StringComparer.Ordinal)
            .Count();
        return Math.Max(0, cultures - 1) * coefficient;
    }
}
