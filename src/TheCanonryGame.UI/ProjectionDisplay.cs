using Engine.Projection;

namespace TheCanonryGame.UI;

public static class ProjectionDisplay
{
    public static string EntityName(IProjectionSymbolCatalog? symbols, UiEntityProjection entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return symbols?.ResolveEntityName(
            new ProjectionEntityNameKey(
                entity.EntityId,
                entity.KindId,
                entity.SubtypeId,
                entity.CultureId,
                entity.NameSeed),
            $"E{entity.EntityId}") ?? $"E{entity.EntityId}";
    }

    public static string EntityName(
        IProjectionSymbolCatalog? symbols,
        UiProjectionState? projection,
        int entityId)
    {
        if (projection?.Entities.TryGetValue(entityId, out var entity) == true)
        {
            return EntityName(symbols, entity);
        }

        return $"E{entityId}";
    }

    public static string DisplayName(
        IProjectionSymbolCatalog? symbols,
        ProjectionSymbolKind kind,
        int id,
        string fallback) =>
        id > 0
            ? symbols?.ResolveDisplayName(kind, id, fallback) ?? fallback
            : "none";

    public static string StableName(
        IProjectionSymbolCatalog? symbols,
        ProjectionSymbolKind kind,
        int id,
        string fallback)
    {
        if (symbols?.TryResolve(kind, id, out var symbol) == true)
        {
            return symbol.StableName;
        }

        return fallback;
    }

    public static string? NullableStableName(IProjectionSymbolCatalog? symbols, ProjectionSymbolKind kind, int id)
    {
        if (id <= 0)
        {
            return null;
        }

        return symbols?.TryResolve(kind, id, out var symbol) == true
            ? symbol.StableName
            : null;
    }
}
