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

internal static class SettlementEntity
{
    public static IceRemembersEntityDefinition Definition { get; } = new()
    {
        Kind = K.Settlement,
        Category = EntityCategory.Spatial,
        Tier = EntityTier.Place,
        Subtypes = Subtypes("rookery", "colony", "holdfast", "sanctuary", "harbor", "reefhold"),
        HealthAttribute = Attr.TempHp,
        HealthDisplayName = "Temp HP",
        PrimaryPlane = PrimaryPlane.Semantic,
        // The kind band is the TOWN clock; stage multipliers stretch it —
        // city x3 (~Generational), capital x6 (~Monumental). Founding
        // fragility and earned permanence come from the stage ladder
        // (docs/design/entity-stages.md).
        LifecycleBand = IceRemembersLifecycleBand.Standard,
        InitialInfluencePlanes = [PrimaryPlane.Physical, PrimaryPlane.Semantic],
        SemanticStockNotes = "Temporary health. Dies at 0.",
        Traits =
        [
            PositiveTrait(
                Trait.SettlementProvidentHearth,
                "Provident Hearth",
                "Shelter and recovery effects are stronger for this settlement.",
                TraitMapDelta(K.Settlement, Attr.TempHp, Effect.MapSupportAtLocation, 0.03f, 4f, PrimaryPlane.Semantic)),
            NegativeTrait(
                Trait.SettlementOverextendedHalls,
                "Overextended Halls",
                "Crowding and raid load scale upward around this settlement.",
                TraitMapDelta(K.Settlement, Attr.TempHp, Effect.MapCrowdingLoad, -0.04f, 0.5f, PrimaryPlane.Semantic)),
        ],
        Locality = Locality(radius: 2, capacity: 3.0f, support: 2.5f, threat: 0f,
            baseMass: 12f, coverageScale: 0.35f, influenceScale: 0.12f,
            healthScale: 0.08f, attractorRadius: 2),
        MovementProfiles = MovementProfiles(
            physicalDefault: Profile.Trail,
            semanticDefault: Profile.Commons,
            subtypeRules:
            [
                MovementSubtypeRule("harbor", physical: Profile.Shore),
                MovementSubtypeRule("reefhold", physical: Profile.Shore),
                MovementSubtypeRule("holdfast", physical: Profile.Shore),
            ]),
        Placement = Placement(
            physicalDefault: Land(),
            semanticDefault: Semantic(Terrain.Lineage, Terrain.OpenField, Terrain.Witness),
            physicalRules:
            [
                SubtypeRule("harbor", Maritime(Terrain.Shelf, Terrain.Reef)),
                SubtypeRule("reefhold", Maritime(Terrain.Shelf, Terrain.Reef)),
                SubtypeRule("holdfast", Maritime(Terrain.Shelf)),
                SubtypeRule("sanctuary", Land(Terrain.HotSprings) with
                {
                    PreferredSites = Sites(IceRemembersPlacementSites.Thermal),
                }),
            ],
            semanticRules:
            [
                SubtypeRule("sanctuary", Semantic(Terrain.Witness, Terrain.BeaconSpire, Terrain.Lineage, Terrain.OpenField)),
                SubtypeRule("harbor", Semantic(Terrain.Breakaway, Terrain.Witness, Terrain.OpenField)),
                SubtypeRule("reefhold", Semantic(Terrain.Breakaway, Terrain.Witness, Terrain.OpenField)),
                CultureRule(Culture.AuroraStack.ToDomainName(), Semantic(Terrain.Witness, Terrain.Lineage, Terrain.BeaconSpire, Terrain.OpenField)),
                CultureRule(Culture.Nightshelf.ToDomainName(), Semantic(Terrain.Silence, Terrain.CavePocket, Terrain.Yoke, Terrain.OpenField)),
            ]),
        PlacementTaste = [Taste(Axis.Capacity, 1f), Taste(Axis.Coast, 0.3f)],
    };
}
