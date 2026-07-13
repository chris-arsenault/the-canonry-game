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

internal static class OccurrenceEntity
{
    public static IceRemembersEntityDefinition Definition { get; } = new()
    {
        Kind = K.Occurrence,
        Category = EntityCategory.Agency,
        Tier = EntityTier.None,
        Subtypes = Subtypes("war", "trade_conference", "plague", "festival"),
        HealthAttribute = Attr.Momentum,
        HealthDisplayName = "Momentum",
        PrimaryPlane = PrimaryPlane.Semantic,
        LifecycleBand = IceRemembersLifecycleBand.Brief,
        CreatesSemanticStock = false,
        Description = "A named, active, major event - war, trade conference, plague, festival. Born by generators when relationship and locality gates cross thresholds; resolves when momentum reaches zero or a gambit ends it. Lives on the semantic plane (its primary movement is through cultural/conflict space) while anchored to the world via relationships.",
        Traits =
        [
            PositiveTrait(
                Trait.OccurrenceGatheringForce,
                "Gathering Force",
                "Local support sustains and accelerates the event's momentum.",
                TraitMapDelta(K.Occurrence, Attr.Momentum, Effect.MapSupportAtLocation, 0.03f, 3f, PrimaryPlane.Semantic)),
            NegativeTrait(
                Trait.OccurrenceGrindingAttrition,
                "Grinding Attrition",
                "Local threat and crowding burn momentum away.",
                TraitMapDelta(K.Occurrence, Attr.Momentum, Effect.MapThreatAtLocation, -0.035f, 3f, PrimaryPlane.Semantic),
                TraitMapDelta(K.Occurrence, Attr.Momentum, Effect.MapCrowdingLoad, -0.025f, 0.5f, PrimaryPlane.Semantic)),
        ],
        MapSignal = MapSignal(radius: 2, capacity: 0.4f, support: 0.5f, threat: 0.5f,
            baseMass: 5f, coverageScale: 0f, influenceScale: 0.04f, activityScale: 0.05f,
            healthScale: 0.04f, attractorRadius: 2),
        MovementProfiles = MovementProfiles(
            physicalDefault: Profile.Trail,
            semanticDefault: Profile.Rift,
            subtypeRules:
            [
                MovementSubtypeRule("trade_conference", semantic: Profile.Commons),
                MovementSubtypeRule("festival", semantic: Profile.Commons),
                MovementSubtypeRule("plague", semantic: Profile.Veil),
                MovementSubtypeRule("war", semantic: Profile.Rift),
            ]),
        Placement = Placement(
            physicalDefault: Land(Terrain.Tundra, Terrain.Ice, Terrain.Shelf),
            semanticDefault: Semantic(Terrain.Witness, Terrain.Severance, Terrain.OpenField)),
    };
}
