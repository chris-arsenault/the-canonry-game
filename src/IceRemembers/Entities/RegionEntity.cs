namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Gameplay;
using Engine.Kinds;

using static IceRemembers.IceRemembersEntityDsl;

using Attr = IceRemembers.IceRemembersAttributeId;
using Effect = Engine.Gameplay.AttributeEffectConditions;
using K = IceRemembers.IceRemembersKinds;
using Profile = IceRemembers.IceRemembersMovementProfile;
using Terrain = IceRemembers.IceRemembersTerrain;
using Trait = IceRemembers.IceRemembersTraitId;

internal static class RegionEntity
{
    public static IceRemembersEntityDefinition Definition { get; } = new()
    {
        Kind = K.Region,
        Category = EntityCategory.Spatial,
        Tier = EntityTier.Place,
        Subtypes = Subtypes("face", "shelf", "depths", "reach", "sea", "scar"),
        HealthAttribute = Attr.Stability,
        HealthDisplayName = "Stability",
        PrimaryPlane = PrimaryPlane.Semantic,
        LifecycleBand = IceRemembersLifecycleBand.WorldAnchor,
        Description = "A geographically connected area of the world. A region's semantic alignment is the spirit or animist ideals of the place itself - the land's own tendencies - distinct from the people, factions, or settlements that happen to occupy it.",
        Traits =
        [
            PositiveTrait(
                Trait.RegionShelteringGround,
                "Sheltering Ground",
                "Local support effects are stronger for this region.",
                TraitMapDelta(K.Region, Attr.Stability, Effect.MapSupportAtLocation, 0.025f, 4f, PrimaryPlane.Semantic)),
            NegativeTrait(
                Trait.RegionBrittleSeams,
                "Brittle Seams",
                "Stress and corruption effects bite harder around this region.",
                TraitMapDelta(K.Region, Attr.Stability, Effect.MapThreatAtLocation, -0.03f, 3f, PrimaryPlane.Semantic)),
        ],
        MapSignal = MapSignal(radius: 2, capacity: 1.5f, support: 0.3f, threat: 0.3f,
            baseMass: 6f, coverageScale: 0.10f, influenceScale: 0.03f, activityScale: 0.02f,
            healthScale: 0.03f, attractorRadius: 2),
        MovementProfiles = MovementProfiles(
            physicalDefault: Profile.Trail,
            semanticDefault: Profile.Commons,
            subtypeRules:
            [
                MovementSubtypeRule("shelf", physical: Profile.Shore),
                MovementSubtypeRule("sea", physical: Profile.Shore),
                MovementSubtypeRule("depths", physical: Profile.Shore, semantic: Profile.Veil),
                MovementSubtypeRule("scar", semantic: Profile.Rift),
            ]),
        Placement = Placement(
            physicalDefault: Land(),
            semanticDefault: Semantic(Terrain.OpenField, Terrain.Lineage, Terrain.Witness),
            physicalRules:
            [
                SubtypeRule("face", Land(Terrain.Ice)),
                SubtypeRule("reach", Land()),
                SubtypeRule("shelf", Maritime(Terrain.Shelf)),
                SubtypeRule("sea", Maritime()),
                SubtypeRule("depths", Maritime()),
                SubtypeRule("scar", Land(Terrain.Wastes)),
            ],
            semanticRules:
            [
                SubtypeRule("depths", Semantic(Terrain.Silence, Terrain.CavePocket, Terrain.TabooRidge)),
            ]),
    };
}
