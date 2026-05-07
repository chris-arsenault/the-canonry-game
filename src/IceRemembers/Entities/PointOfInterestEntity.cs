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

internal static class PointOfInterestEntity
{
    public static IceRemembersEntityDefinition Definition { get; } = new()
    {
        Kind = K.PointOfInterest,
        Category = EntityCategory.Spatial,
        Subtypes = Subtypes("vent", "peak", "bloom", "shoal"),
        HealthAttribute = Attr.Presence,
        HealthDisplayName = "Presence",
        PrimaryPlane = PrimaryPlane.Semantic,
        LifecycleBand = IceRemembersLifecycleBand.Enduring,
        Description = "A named natural feature with localized significance - a thermal vent, a peak, a krill bloom, a shoal. Smaller than a region, distinct from a settlement, never built.",
        Traits =
        [
            PositiveTrait(
                Trait.PointOfInterestGenerousBounty,
                "Generous Bounty",
                "Supportive map signals strengthen this place.",
                TraitMapDelta(K.PointOfInterest, Attr.Presence, Effect.MapSupportAtLocation, 0.03f, 3f, PrimaryPlane.Semantic)),
            NegativeTrait(
                Trait.PointOfInterestTreacherousApproach,
                "Treacherous Approach",
                "Threat signals erode this place more quickly.",
                TraitMapDelta(K.PointOfInterest, Attr.Presence, Effect.MapThreatAtLocation, -0.035f, 3f, PrimaryPlane.Semantic)),
        ],
        MapSignal = MapSignal(radius: 1, capacity: 0.6f, support: 0.5f, threat: 0f,
            baseMass: 4f, coverageScale: 0f, influenceScale: 0.03f, activityScale: 0.02f,
            healthScale: 0.04f, attractorRadius: 1),
        MovementProfiles = MovementProfiles(
            physicalDefault: Profile.Trail,
            semanticDefault: Profile.Commons,
            subtypeRules:
            [
                MovementSubtypeRule("shoal", physical: Profile.Shore),
                MovementSubtypeRule("bloom", physical: Profile.Shore, semantic: Profile.Rift),
            ]),
        Placement = Placement(
            physicalDefault: Land(Terrain.Tundra, Terrain.Crags, Terrain.Ice),
            semanticDefault: Semantic(Terrain.Witness, Terrain.OpenField, Terrain.Lineage),
            physicalRules:
            [
                SubtypeRule("vent", Land(Terrain.HotSprings, Terrain.Crags, Terrain.Wastes)),
                SubtypeRule("shoal", Maritime(Terrain.Reef, Terrain.Shelf, Terrain.Ocean)),
                SubtypeRule("bloom", Maritime(Terrain.Ocean, Terrain.Reef, Terrain.Wastes)),
            ],
            semanticRules:
            [
                SubtypeRule("vent", Semantic(Terrain.BeaconSpire, Terrain.Witness, Terrain.OpenField)),
                SubtypeRule("bloom", Semantic(Terrain.Severance, Terrain.Silence, Terrain.TabooRidge)),
                SubtypeRule("shoal", Semantic(Terrain.Breakaway, Terrain.OpenField, Terrain.Witness)),
            ]),
    };
}
