namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Gameplay;
using Engine.Kinds;

using static IceRemembers.IceRemembersEntityDsl;

using Attr = IceRemembers.IceRemembersAttributeId;
using GambitId = IceRemembers.IceRemembersGambitId;
using K = IceRemembers.IceRemembersKinds;
using Profile = IceRemembers.IceRemembersMovementProfile;
using Rel = IceRemembers.IceRemembersRelationship;
using Terrain = IceRemembers.IceRemembersTerrain;
using Trait = IceRemembers.IceRemembersTraitId;

internal static class ArtifactEntity
{
    public static IceRemembersEntityDefinition Definition { get; } = new()
    {
        Kind = K.Artifact,
        Category = EntityCategory.Asset,
        Tier = EntityTier.Life,
        Subtypes = Subtypes("relic", "instrument", "weapon", "tome", "seal"),
        HealthAttribute = Attr.Condition,
        HealthDisplayName = "Condition",
        PrimaryPlane = PrimaryPlane.Physical,
        LifecycleBand = IceRemembersLifecycleBand.Enduring,
        Traits =
        [
            PositiveTrait(
                Trait.ArtifactRestorativeAura,
                "Restorative Aura",
                "Wielders, anchored regions, and cleansing gambits receive restorative support.",
                TraitRelationshipDelta(K.Penguin, Attr.Vitality, Rel.Holds, EffectRelationshipDirection.Outgoing, 0.04f, 0, PrimaryPlane.Physical, TraitEffectBearer.RelatedEntity, minStrength: 0.45f),
                TraitRelationshipDelta(K.Region, Attr.Stability, Rel.Anchored, EffectRelationshipDirection.Incoming, 0.03f, 1, PrimaryPlane.Physical, TraitEffectBearer.RelatedEntity, minStrength: 0.45f),
                TraitGambit(GambitId.ArtifactCleansesScar, scoreDelta: 0.10f, successDelta: 0.10f)),
            NegativeTrait(
                Trait.ArtifactVolatileResonance,
                "Volatile Resonance",
                "Wielders and anchored regions risk backlash while cleansing becomes tempting.",
                TraitRelationshipDelta(K.Penguin, Attr.Vitality, Rel.Holds, EffectRelationshipDirection.Outgoing, -0.035f, 0, PrimaryPlane.Physical, TraitEffectBearer.RelatedEntity, minStrength: 0.45f),
                TraitRelationshipDelta(K.Region, Attr.Stability, Rel.Anchored, EffectRelationshipDirection.Incoming, -0.025f, 1, PrimaryPlane.Physical, TraitEffectBearer.RelatedEntity, minStrength: 0.45f),
                TraitGambit(GambitId.ArtifactCleansesScar, scoreDelta: 0.15f, successDelta: -0.08f)),
        ],
        MapSignal = MapSignal(radius: 1, capacity: 0.3f, support: 0.4f, threat: 0.2f,
            baseMass: 10f, coverageScale: 0f, influenceScale: 0.04f, activityScale: 0.04f,
            healthScale: 0.08f, attractorRadius: 2),
        MovementProfiles = MovementProfiles(
            physicalDefault: Profile.Trail,
            semanticDefault: Profile.Veil,
            subtypeRules:
            [
                MovementSubtypeRule("weapon", semantic: Profile.Rift),
                MovementSubtypeRule("seal", semantic: Profile.Rift),
                MovementSubtypeRule("instrument", semantic: Profile.Commons),
                MovementSubtypeRule("relic", semantic: Profile.Commons),
                MovementSubtypeRule("tome", semantic: Profile.Veil),
            ]),
        Placement = Placement(
            physicalDefault: Land(Terrain.Crags, Terrain.Wastes, Terrain.Tundra),
            semanticDefault: Semantic(Terrain.Silence, Terrain.CavePocket, Terrain.OpenField),
            semanticRules:
            [
                SubtypeRule("weapon", Semantic(Terrain.Severance, Terrain.TabooRidge, Terrain.Yoke)),
                SubtypeRule("seal", Semantic(Terrain.TabooRidge, Terrain.Yoke, Terrain.Silence)),
                SubtypeRule("instrument", Semantic(Terrain.BeaconSpire, Terrain.Witness, Terrain.Breakaway)),
                SubtypeRule("relic", Semantic(Terrain.Lineage, Terrain.Witness, Terrain.CavePocket)),
                SubtypeRule("tome", Semantic(Terrain.Lineage, Terrain.Silence, Terrain.Witness)),
            ]),
    };
}
