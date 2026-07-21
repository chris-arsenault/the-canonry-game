namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Gameplay;
using Engine.Kinds;

using static IceRemembers.IceRemembersEntityDsl;

using Attr = IceRemembers.IceRemembersAttributeId;
using Axis = Engine.Domain.Config.PlacementTasteAxisNames;
using K = IceRemembers.IceRemembersKinds;
using Profile = IceRemembers.IceRemembersMovementProfile;
using Terrain = IceRemembers.IceRemembersTerrain;
using Trait = IceRemembers.IceRemembersTraitId;
using Verb = IceRemembers.IceRemembersVerbId;

internal static class PenguinEntity
{
    public static IceRemembersEntityDefinition Definition { get; } = new()
    {
        Kind = K.Penguin,
        Category = EntityCategory.Agency,
        Tier = EntityTier.Life,
        Subtypes = Subtypes("warden", "broker", "witness", "exile"),
        HealthAttribute = Attr.Vitality,
        HealthDisplayName = "Vitality",
        PrimaryPlane = PrimaryPlane.Physical,
        LifecycleBand = IceRemembersLifecycleBand.Brief,
        Traits =
        [
            PositiveTrait(
                Trait.PenguinRangingHeart,
                "Ranging Heart",
                "Low-altitude behaviour favours wilderness verbs over settlements.",
                TraitVerbWeight(Verb.PenguinExplore, weightDelta: 1.0f),
                TraitVerbWeight(Verb.PenguinGoFishing, weightDelta: 0.4f),
                TraitVerbWeight(Verb.PenguinVisitRookery, weightDelta: -0.5f)),
            PositiveTrait(
                Trait.PenguinSureSwimmer,
                "Sure Swimmer",
                "Can follow shore and water routes on the physical plane."),
            NegativeTrait(
                Trait.PenguinHearthbound,
                "Hearthbound",
                "Low-altitude behaviour favours home settlements and tracks home-base ties.",
                TraitVerbWeight(Verb.PenguinExplore, weightDelta: -0.7f),
                TraitVerbWeight(Verb.PenguinVisitRookery, weightDelta: 0.6f, fitnessWeightDelta: 0.4f)),
        ],
        MapSignal = MapSignal(radius: 1, capacity: 0.2f, support: 0.2f, threat: 0f,
            baseMass: 2f, coverageScale: 0f, influenceScale: 0.02f, activityScale: 0.02f,
            healthScale: 0.02f, attractorRadius: 1),
        MovementProfiles = MovementProfiles(
            physicalDefault: Profile.Trail,
            semanticDefault: Profile.Commons,
            subtypeRules:
            [
                MovementSubtypeRule("warden", semantic: Profile.Mandate),
                MovementSubtypeRule("exile", semantic: Profile.Rift),
            ],
            traitRules:
            [
                MovementTraitRule(Trait.PenguinSureSwimmer, physical: Profile.Shore),
            ]),
        Placement = Placement(
            physicalDefault: Land(),
            semanticDefault: Semantic(Terrain.Lineage, Terrain.OpenField, Terrain.Witness),
            semanticRules:
            [
                SubtypeRule("witness", Semantic(Terrain.Witness, Terrain.Lineage, Terrain.BeaconSpire)),
                SubtypeRule("broker", Semantic(Terrain.Breakaway, Terrain.OpenField, Terrain.Witness)),
                SubtypeRule("warden", Semantic(Terrain.Yoke, Terrain.Lineage, Terrain.TabooRidge)),
                SubtypeRule("exile", Semantic(Terrain.Severance, Terrain.Breakaway, Terrain.Silence)),
            ]),
        PlacementTaste = [Taste(Axis.Capacity, 0.8f)],
    };
}
