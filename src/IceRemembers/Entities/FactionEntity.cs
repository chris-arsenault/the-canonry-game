namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Gameplay;
using Engine.Kinds;

using static IceRemembers.IceRemembersEntityDsl;

using Attr = IceRemembers.IceRemembersAttributeId;
using Culture = IceRemembers.IceRemembersCulture;
using GambitId = IceRemembers.IceRemembersGambitId;
using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;
using Terrain = IceRemembers.IceRemembersTerrain;
using Trait = IceRemembers.IceRemembersTraitId;

internal static class FactionEntity
{
    public static IceRemembersEntityDefinition Definition { get; } = new()
    {
        Kind = K.Faction,
        Category = EntityCategory.Agency,
        Subtypes = Subtypes("order", "compact", "cell", "choir"),
        HealthAttribute = Attr.Cohesion,
        HealthDisplayName = "Cohesion",
        PrimaryPlane = PrimaryPlane.Semantic,
        LifecycleBand = IceRemembersLifecycleBand.Standard,
        InitialInfluencePlanes = [PrimaryPlane.Physical, PrimaryPlane.Semantic],
        Traits =
        [
            PositiveTrait(
                Trait.FactionSteadyCadre,
                "Steady Cadre",
                "Faction gambits are more reliable and allied support coheres faster.",
                TraitRelationshipDelta(K.Faction, Attr.Cohesion, Rel.AlliedWith, EffectRelationshipDirection.Either, 0.025f, 1, PrimaryPlane.Semantic),
                TraitGambit(GambitId.FactionDeclaresTerritorialWar, scoreDelta: -0.05f, successDelta: 0.08f),
                TraitGambit(GambitId.FactionSuesForPeace, scoreDelta: 0.08f, successDelta: 0.08f)),
            NegativeTrait(
                Trait.FactionFractiousCouncil,
                "Fractious Council",
                "Conflict gates favor this faction but execution is less reliable.",
                TraitRelationshipDelta(K.Faction, Attr.Cohesion, Rel.RivalOf, EffectRelationshipDirection.Either, -0.025f, 1, PrimaryPlane.Semantic),
                TraitGambit(GambitId.FactionDeclaresTerritorialWar, scoreDelta: 0.18f, successDelta: -0.05f),
                TraitGambit(GambitId.FactionSuesForPeace, scoreDelta: -0.12f, successDelta: -0.04f)),
        ],
        MapSignal = MapSignal(radius: 2, capacity: 1.0f, support: 1.2f, threat: 0f,
            baseMass: 8f, coverageScale: 0.2f, influenceScale: 0.10f, activityScale: 0.04f,
            healthScale: 0.06f, attractorRadius: 2),
        Placement = Placement(
            physicalDefault: Land(Terrain.Tundra, Terrain.Ice, Terrain.Shelf),
            semanticDefault: Semantic(Terrain.Yoke, Terrain.Lineage, Terrain.OpenField),
            physicalRules:
            [
                SubtypeRule("choir", Maritime(Terrain.Ocean, Terrain.Reef, Terrain.Shelf)),
                CultureRule(Culture.WakeSingers.ToDomainName(), Maritime(Terrain.Ocean, Terrain.Reef, Terrain.Shelf)),
                SubtypeRule("order", Land(Terrain.Shelf, Terrain.Ice, Terrain.Tundra)),
                CultureRule(Culture.Nightshelf.ToDomainName(), Land(Terrain.Shelf, Terrain.Ice, Terrain.Tundra)),
            ],
            semanticRules:
            [
                new IceRemembersTerrainProfileRule(
                    "order",
                    Culture.Nightshelf.ToDomainName(),
                    Semantic(Terrain.Silence, Terrain.Yoke, Terrain.CavePocket)),
                SubtypeRule("order", Semantic(Terrain.Yoke, Terrain.Witness, Terrain.Lineage)),
                SubtypeRule("choir", Semantic(Terrain.Lineage, Terrain.Witness, Terrain.OpenField)),
                SubtypeRule("cell", Semantic(Terrain.CavePocket, Terrain.Silence, Terrain.Breakaway)),
                SubtypeRule("compact", Semantic(Terrain.Yoke, Terrain.Lineage, Terrain.Witness)),
            ]),
    };
}
