namespace Engine.Kinds;

public static class EntityKindExtensions
{
    public static EntityCategory GetCategory(this EntityKind kind) => kind switch
    {
        EntityKind.Region => EntityCategory.Spatial,
        EntityKind.Settlement => EntityCategory.Spatial,
        EntityKind.PointOfInterest => EntityCategory.Spatial,
        EntityKind.Faction => EntityCategory.Agency,
        EntityKind.Npc => EntityCategory.Agency,
        EntityKind.Monster => EntityCategory.Agency,
        EntityKind.Artifact => EntityCategory.Asset,
        EntityKind.Transportation => EntityCategory.Asset,
        EntityKind.Ability => EntityCategory.Asset,
        EntityKind.Occurrence => EntityCategory.Record,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown entity kind"),
    };
}
