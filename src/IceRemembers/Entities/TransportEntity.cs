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

internal static class TransportEntity
{
    public static IceRemembersEntityDefinition Definition { get; } = new()
    {
        Kind = K.Transport,
        Category = EntityCategory.Asset,
        Subtypes = Subtypes("trade", "battleship", "scout", "citadel"),
        HealthAttribute = Attr.Integrity,
        HealthDisplayName = "Integrity",
        PrimaryPlane = PrimaryPlane.Physical,
        LifecycleBand = IceRemembersLifecycleBand.Standard,
        CreatesSemanticStock = false,
        Description = "A named, mobile, large named asset - trade fleet, battleship, scout, or citadel. Owned and commanded by Agency entities; significant in its own right rather than an inert tool.",
        Traits =
        [
            PositiveTrait(
                Trait.TransportProvenMake,
                "Proven Make",
                "Maintenance and support effects preserve integrity.",
                TraitMapDelta(K.Transport, Attr.Integrity, Effect.MapSupportAtLocation, 0.03f, 3f, PrimaryPlane.Physical)),
            NegativeTrait(
                Trait.TransportHiddenFault,
                "Hidden Fault",
                "Threat and crowding effects damage integrity faster.",
                TraitMapDelta(K.Transport, Attr.Integrity, Effect.MapThreatAtLocation, -0.035f, 3f, PrimaryPlane.Physical),
                TraitMapDelta(K.Transport, Attr.Integrity, Effect.MapCrowdingLoad, -0.025f, 0.5f, PrimaryPlane.Physical)),
        ],
        MapSignal = MapSignal(radius: 2, capacity: 0.5f, support: 0.6f, threat: 0.4f,
            baseMass: 8f, coverageScale: 0f, influenceScale: 0.05f, activityScale: 0.06f,
            healthScale: 0.06f, attractorRadius: 2),
        MovementProfiles = MovementProfiles(
            physicalDefault: Profile.Wake,
            semanticDefault: Profile.Commons),
        Placement = Placement(
            physicalDefault: Maritime(Terrain.Ocean, Terrain.Reef, Terrain.Shelf),
            semanticDefault: Semantic(Terrain.Breakaway, Terrain.OpenField, Terrain.Witness)),
    };
}
