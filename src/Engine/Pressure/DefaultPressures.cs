namespace Engine.Pressure;

using Engine.Graph;
using Engine.Kinds;

/// <summary>
/// MVP pressure definitions: military tension, economic stress, cultural friction, demographic pressure.
/// Contributing factor coefficients are tuning targets.
/// </summary>
public static class DefaultPressures
{
    public static PressureDefinition MilitaryTension()
    {
        var def = new PressureDefinition("military_tension", PressureScope.Regional,
            baseline: 0f, homeostasisRate: 0.05f, smoothingFactor: 0.2f);

        // More factions in a region = more tension
        def.Factors.Add(new EntityCountFactor("faction_count", EntityKind.Faction, coefficient: 8f));

        // Negative relationships increase tension
        def.Factors.Add(new NegativeRelationshipFactor("rivalries", "rival_of", coefficient: 15f));

        // Positive relationships decrease tension
        def.Factors.Add(new PositiveRelationshipFactor("alliances", "allied_with", coefficient: -5f));

        return def;
    }

    public static PressureDefinition EconomicStress()
    {
        var def = new PressureDefinition("economic_stress", PressureScope.Regional,
            baseline: 0f, homeostasisRate: 0.08f, smoothingFactor: 0.25f);

        // Large settlements with low security = stress
        def.Factors.Add(new LowAttributeFactor("low_prosperity", EntityKind.Settlement,
            "security", threshold: 30f, coefficient: 5f));

        return def;
    }

    public static PressureDefinition CulturalFriction()
    {
        var def = new PressureDefinition("cultural_friction", PressureScope.Regional,
            baseline: 0f, homeostasisRate: 0.06f, smoothingFactor: 0.2f);

        // More distinct cultures = more friction
        def.Factors.Add(new CultureDiversityFactor("culture_diversity", coefficient: 10f));

        return def;
    }

    public static PressureDefinition DemographicPressure()
    {
        var def = new PressureDefinition("demographic_pressure", PressureScope.Regional,
            baseline: 0f, homeostasisRate: 0.1f, smoothingFactor: 0.3f);

        // More settlements = more demographic pressure
        def.Factors.Add(new EntityCountFactor("settlement_count", EntityKind.Settlement, coefficient: 5f));

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

// --- Concrete contributing factors ---

/// <summary>Counts entities of a kind in scope, multiplied by coefficient.</summary>
public sealed class EntityCountFactor(string name, EntityKind kind, float coefficient) : IContributingFactor
{
    public string Name => name;

    public float Evaluate(IPressureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.CountEntities(e => e.Kind == kind) * coefficient;
    }
}

/// <summary>Counts negative-polarity relationships of a specific kind, multiplied by coefficient.</summary>
public sealed class NegativeRelationshipFactor(string name, string relationshipKind, float coefficient) : IContributingFactor
{
    public string Name => name;

    public float Evaluate(IPressureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.CountRelationships(r => r.Kind == relationshipKind && r.IsActive) * coefficient;
    }
}

/// <summary>Counts positive-polarity relationships, multiplied by coefficient (typically negative to reduce pressure).</summary>
public sealed class PositiveRelationshipFactor(string name, string relationshipKind, float coefficient) : IContributingFactor
{
    public string Name => name;

    public float Evaluate(IPressureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.CountRelationships(r => r.Kind == relationshipKind && r.IsActive) * coefficient;
    }
}

/// <summary>Counts entities below an attribute threshold, multiplied by coefficient.</summary>
public sealed class LowAttributeFactor(
    string name, EntityKind kind, string attributeName, float threshold, float coefficient) : IContributingFactor
{
    public string Name => name;

    public float Evaluate(IPressureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var count = 0;
        foreach (var entityId in context.EntitiesInScope)
        {
            var entity = context.GetEntity(entityId);
            if (entity?.Kind == kind && context.GetAttribute(entityId, attributeName) < threshold)
            {
                count++;
            }
        }

        return count * coefficient;
    }
}

/// <summary>Counts distinct cultures among entities in scope, multiplied by coefficient.</summary>
public sealed class CultureDiversityFactor(string name, float coefficient) : IContributingFactor
{
    public string Name => name;

    public float Evaluate(IPressureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var cultures = new HashSet<string>();
        foreach (var entityId in context.EntitiesInScope)
        {
            var entity = context.GetEntity(entityId);
            if (entity is not null && !string.IsNullOrEmpty(entity.Culture))
            {
                cultures.Add(entity.Culture);
            }
        }

        // Diversity = distinct cultures - 1 (a single culture = 0 friction)
        return Math.Max(0, cultures.Count - 1) * coefficient;
    }
}
