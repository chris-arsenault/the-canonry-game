namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Gameplay;
using Engine.Kinds;

using static IceRemembers.IceRemembersEntityDsl;

using Attr = IceRemembers.IceRemembersAttributeId;
using Axis = Engine.Domain.Config.PlacementTasteAxisNames;
using Culture = IceRemembers.IceRemembersCulture;
using Effect = Engine.Gameplay.AttributeEffectConditions;
using K = IceRemembers.IceRemembersKinds;
using Profile = IceRemembers.IceRemembersMovementProfile;
using Terrain = IceRemembers.IceRemembersTerrain;
using Trait = IceRemembers.IceRemembersTraitId;

internal static class MonsterEntity
{
    public static IceRemembersEntityDefinition Definition { get; } = new()
    {
        Kind = K.Monster,
        Category = EntityCategory.Agency,
        Tier = EntityTier.Life,
        Subtypes = Subtypes("leviathan", "wraith", "beast", "revenant"),
        HealthAttribute = Attr.Containment,
        HealthDisplayName = "Containment",
        PrimaryPlane = PrimaryPlane.Physical,
        LifecycleBand = IceRemembersLifecycleBand.Enduring,
        Description = "A bound or partially bound peril. Containment is its health-like integrity; static traits describe whether the binding is stable or the hunger is stirring. The status of a monster (sealed, unbound, risen) is tracked separately from its subtype.",
        Traits =
        [
            PositiveTrait(
                Trait.MonsterWellBound,
                "Well Bound",
                "Supportive locality effects reinforce containment.",
                TraitMapDelta(K.Monster, Attr.Containment, Effect.MapSupportAtLocation, 0.03f, 3f, PrimaryPlane.Physical)),
            NegativeTrait(
                Trait.MonsterHungryStirring,
                "Hungry Stirring",
                "Threat stress erodes containment faster.",
                TraitMapDelta(K.Monster, Attr.Containment, Effect.MapThreatAtLocation, -0.04f, 3f, PrimaryPlane.Physical)),
        ],
        MapSignal = MapSignal(radius: 2, capacity: 0.3f, support: 0f, threat: 1.0f,
            baseMass: 6f, coverageScale: 0f, influenceScale: 0.03f, activityScale: 0.04f,
            healthScale: 0.05f, attractorRadius: 2),
        MovementProfiles = MovementProfiles(
            physicalDefault: Profile.Trail,
            semanticDefault: Profile.Veil),
        Placement = Placement(
            // Monsters haunt the fire and the anomalies, far from the rich
            // ground where the living crowd. What the wake stirs up is a
            // creature of the deep, not the shore.
            physicalDefault: Land() with
            {
                PreferredSites = Sites(IceRemembersPlacementSites.Anomaly),
            },
            physicalRules:
            [
                // First match wins: what the Wake-Singers make stays in the
                // deep regardless of shape. After that, the depth answers as
                // a thing of the depth - leviathans rise in open water under
                // pressure, beasts stalk the harvest edge where reef meets
                // shelf, and wraiths keep to the cold ground the ice never
                // quite released.
                CultureRule(Culture.WakeSingers.ToDomainName(), Maritime() with
                {
                    PreferredSites = Sites(IceRemembersPlacementSites.Anomaly),
                }),
                SubtypeRule("leviathan", Maritime() with
                {
                    PreferredSites = Sites(IceRemembersPlacementSites.Anomaly),
                    PressureAffinity = 0.5f,
                }),
                SubtypeRule("beast", Amphibious(Terrain.Reef, Terrain.Shelf)),
                SubtypeRule("wraith", Land(Terrain.Ice, Terrain.Tundra, Terrain.AuroraField) with
                {
                    PreferredSites = Sites(IceRemembersPlacementSites.Anomaly),
                }),
            ],
            semanticDefault: Semantic(Terrain.Silence, Terrain.Severance, Terrain.CavePocket)),
        PlacementTaste = [Taste(Axis.Fire, 0.5f), Taste(Axis.Capacity, -0.6f)],
    };
}
