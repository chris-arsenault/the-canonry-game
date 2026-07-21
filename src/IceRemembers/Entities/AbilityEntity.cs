namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Gameplay;
using Engine.Kinds;

using static IceRemembers.IceRemembersEntityDsl;

using Attr = IceRemembers.IceRemembersAttributeId;
using Culture = IceRemembers.IceRemembersCulture;
using Effect = Engine.Gameplay.AttributeEffectConditions;
using K = IceRemembers.IceRemembersKinds;
using Profile = IceRemembers.IceRemembersMovementProfile;
using Rel = IceRemembers.IceRemembersRelationship;
using Terrain = IceRemembers.IceRemembersTerrain;
using Trait = IceRemembers.IceRemembersTraitId;

internal static class AbilityEntity
{
    public static IceRemembersEntityDefinition Definition { get; } = new()
    {
        Kind = K.Ability,
        Category = EntityCategory.Asset,
        Tier = EntityTier.Life,
        Subtypes = Subtypes("technique", "rite", "craft"),
        HealthAttribute = Attr.Clarity,
        HealthDisplayName = "Clarity",
        PrimaryPlane = PrimaryPlane.Semantic,
        LifecycleBand = IceRemembersLifecycleBand.Generational,
        Description = "A combat technique, magical rite, or piece of technology. Represented as a transferable physical carrier - a book, a marking, a tool - but lives on the semantic plane and may be held by multiple bearers at once.",
        Traits =
        [
            PositiveTrait(
                Trait.AbilityPracticedForm,
                "Practiced Form",
                "Bearers recover vitality from a clear, practiced ability.",
                TraitRelationshipDelta(K.Penguin, Attr.Vitality, Rel.Holds, EffectRelationshipDirection.Outgoing, 0.02f, 0, PrimaryPlane.Physical, TraitEffectBearer.RelatedEntity, minStrength: 0.35f)),
            NegativeTrait(
                Trait.AbilityBrittleLineage,
                "Brittle Lineage",
                "Threat and repeated stress erode this ability's clarity.",
                TraitMapDelta(K.Ability, Attr.Clarity, Effect.MapThreatAtLocation, -0.025f, 3f, PrimaryPlane.Semantic)),
        ],
        MapSignal = MapSignal(radius: 1, capacity: 0.2f, support: 0.3f, threat: 0f,
            baseMass: 3f, coverageScale: 0f, influenceScale: 0.02f, activityScale: 0.02f,
            healthScale: 0.03f, attractorRadius: 1),
        MovementProfiles = MovementProfiles(
            physicalDefault: Profile.Trail,
            semanticDefault: Profile.Commons),
        Placement = Placement(
            physicalDefault: Land(),
            semanticDefault: Semantic(Terrain.Lineage, Terrain.Witness, Terrain.OpenField),
            semanticRules:
            [
                CultureRule(Culture.AuroraStack.ToDomainName(), Semantic(Terrain.Witness, Terrain.Lineage, Terrain.BeaconSpire)),
                CultureRule(Culture.Nightshelf.ToDomainName(), Semantic(Terrain.Silence, Terrain.CavePocket, Terrain.Yoke)),
                CultureRule(Culture.WakeSingers.ToDomainName(), Semantic(Terrain.Lineage, Terrain.Breakaway, Terrain.Witness)),
            ]),
    };
}
