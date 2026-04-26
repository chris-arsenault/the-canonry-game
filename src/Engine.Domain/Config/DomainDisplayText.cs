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

        var attribute = kindConfig.Attributes.FirstOrDefault(a => string.Equals(a.Name, attributeName, StringComparison.Ordinal));
        if (attribute is null)
        {
            return attributeName;
        }

        if (!string.IsNullOrWhiteSpace(cultureId)
            && attribute.CultureDisplayNames is not null
            && attribute.CultureDisplayNames.TryGetValue(cultureId, out var cultureDisplayName))
        {
            return cultureDisplayName;
        }

        return attribute.DisplayName ?? attribute.Name;
    }
}
