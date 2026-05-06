namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Gameplay;
using Engine.Kinds;

using static IceRemembers.IceRemembersEntityDsl;

using Attr = IceRemembers.IceRemembersAttributeId;
using Effect = Engine.Gameplay.AttributeEffectConditions;
using K = IceRemembers.IceRemembersKinds;
using Terrain = IceRemembers.IceRemembersTerrain;
using Trait = IceRemembers.IceRemembersTraitId;

internal static class MonsterEntity
{
    public static IceRemembersEntityDefinition Definition { get; } = new()
    {
        Kind = K.Monster,
        Category = EntityCategory.Agency,
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
        Placement = Placement(
            physicalDefault: Land(Terrain.Wastes, Terrain.Crags, Terrain.Ice),
            semanticDefault: Semantic(Terrain.Silence, Terrain.Severance, Terrain.CavePocket)),
    };
}
