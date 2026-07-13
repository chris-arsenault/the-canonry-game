namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Kinds;

using static IceRemembers.IceRemembersEntityDsl;

using Attr = IceRemembers.IceRemembersAttributeId;
using Effect = Engine.Gameplay.AttributeEffectConditions;
using K = IceRemembers.IceRemembersKinds;
using Profile = IceRemembers.IceRemembersMovementProfile;
using Terrain = IceRemembers.IceRemembersTerrain;
using Trait = IceRemembers.IceRemembersTraitId;

internal static class CanonEntity
{
    public static IceRemembersEntityDefinition Definition { get; } = new()
    {
        Kind = K.Canon,
        Category = EntityCategory.Spatial,
        Tier = EntityTier.Place,
        Subtypes = Subtypes("custom", "taboo", "accord", "doctrine"),
        HealthAttribute = Attr.Observance,
        HealthDisplayName = "Observance",
        PrimaryPlane = PrimaryPlane.Semantic,
        LifecycleBand = IceRemembersLifecycleBand.Monumental,
        Description = "A named body of law, custom, or prohibition living on the semantic plane — an accord signed after a war, a taboo nobody wrote down, a doctrine sung from the deep. A canon's coverage is the region of idea-space it governs; entities whose semantic position falls inside are living under it. Observance is how firmly the world still keeps it.",
        Traits =
        [
            PositiveTrait(
                Trait.CanonDeepRooted,
                "Deep-Rooted",
                "The law is old enough that keeping it comes naturally; support effects run stronger under it.",
                TraitMapDelta(K.Canon, Attr.Observance, Effect.MapSupportAtLocation, 0.02f, 4f, PrimaryPlane.Semantic)),
            NegativeTrait(
                Trait.CanonHollowLetter,
                "Hollow Letter",
                "Kept in word but not in spirit; threat effects erode the observance faster.",
                TraitMapDelta(K.Canon, Attr.Observance, Effect.MapThreatAtLocation, -0.025f, 3f, PrimaryPlane.Semantic)),
        ],
        MapSignal = MapSignal(radius: 2, capacity: 1.2f, support: 0.2f, threat: 0.2f,
            baseMass: 4f, coverageScale: 0.08f, influenceScale: 0.03f, activityScale: 0.02f,
            healthScale: 0.03f, attractorRadius: 2),
        MovementProfiles = MovementProfiles(
            physicalDefault: Profile.Trail,
            semanticDefault: Profile.Commons),
        Placement = Placement(
            physicalDefault: Land(Terrain.Tundra, Terrain.Ice, Terrain.Shelf),
            semanticDefault: Semantic(Terrain.Lineage, Terrain.Witness, Terrain.TabooRidge)),
    };
}
