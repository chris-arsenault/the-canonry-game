namespace Engine.Domain.Config;

using Engine.Graph;
using Engine.Kinds;

public static class DomainDisplayText
{
    public static string GetCultureDisplayName(this DomainConfig config, string cultureId)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrEmpty(cultureId);

        return config.Cultures.FirstOrDefault(c => string.Equals(c.Id, cultureId, StringComparison.Ordinal))?.DisplayName
            ?? cultureId;
    }

    public static string GetAttributeDisplayName(this DomainConfig config, Entity entity, string attributeName)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return config.GetAttributeDisplayName(entity.Kind, entity.Culture, attributeName);
    }

    public static string GetAttributeDisplayName(this DomainConfig config, EntityKind kind, string? cultureId, string attributeName)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrEmpty(attributeName);

        if (!config.Kinds.TryGetValue(kind, out var kindConfig))
        {
            return attributeName;
        }

        var health = kindConfig.Health;
        if (!string.Equals(health.Name, attributeName, StringComparison.Ordinal))
        {
            return attributeName;
        }

        if (!string.IsNullOrWhiteSpace(cultureId)
            && health.CultureDisplayNames is not null
            && health.CultureDisplayNames.TryGetValue(cultureId, out var cultureDisplayName))
        {
            return cultureDisplayName;
        }

        return health.DisplayName ?? health.Name;
    }

    public static string GetTraitDisplayName(this DomainConfig config, EntityKind kind, string traitName)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrEmpty(traitName);

        if (!config.Kinds.TryGetValue(kind, out var kindConfig))
        {
            return traitName;
        }

        var trait = kindConfig.Traits.FirstOrDefault(t => string.Equals(t.Name, traitName, StringComparison.Ordinal));
        return trait?.DisplayName ?? traitName;
    }
}
