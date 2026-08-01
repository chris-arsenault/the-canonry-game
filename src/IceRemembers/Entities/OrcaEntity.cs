namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Gameplay;
using Engine.Kinds;

using static IceRemembers.IceRemembersEntityDsl;

using Attr = IceRemembers.IceRemembersAttributeId;
using Axis = Engine.Domain.Config.PlacementTasteAxisNames;
using K = IceRemembers.IceRemembersKinds;
using Profile = IceRemembers.IceRemembersMovementProfile;
using Rel = IceRemembers.IceRemembersRelationship;
using Terrain = IceRemembers.IceRemembersTerrain;
using Trait = IceRemembers.IceRemembersTraitId;

internal static class OrcaEntity
{
    public static IceRemembersEntityDefinition Definition { get; } = new()
    {
        Kind = K.Orca,
        Category = EntityCategory.Agency,
        Tier = EntityTier.Life,
        Subtypes = Subtypes("matriarch", "bull", "scout", "elder"),
        HealthAttribute = Attr.Endurance,
        HealthDisplayName = "Endurance",
        PrimaryPlane = PrimaryPlane.Physical,
        // Actor kinds share one band (authorship parity, actor-parity
        // audit 2026-08-01). The Enduring promotion was one of five stacked
        // orca-extinction fixes; the homing and succession machinery that
        // actually carries pod survival stays.
        LifecycleBand = IceRemembersLifecycleBand.Standard,
        Description = "A named orca, member of a matriarchal pod. Mobile actor on the physical plane, primarily aligned with the Wake-Singers but capable of independent allegiance.",
        Traits =
        [
            .. ActorBondTraits(
                K.Orca, Attr.Endurance,
                Trait.OrcaSureWake, "Sure Wake",
                Trait.OrcaBloodFrenzy, "Blood Frenzy",
                "endurance"),
        ],
        Locality = ActorLocality(),
        MovementProfiles = MovementProfiles(
            physicalDefault: Profile.Wake,
            semanticDefault: Profile.Commons),
        Placement = Placement(
            // Orcas follow the deep pressure - the god's wake is their sea.
            physicalDefault: Maritime(),
            semanticDefault: Semantic(Terrain.Breakaway, Terrain.Lineage, Terrain.OpenField)),
        PlacementTaste = [Taste(Axis.Water, 0.8f), Taste(Axis.Pressure, 0.5f)],
    };
}
