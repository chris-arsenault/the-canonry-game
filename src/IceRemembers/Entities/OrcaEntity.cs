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
        // The Wake-Singers' identity rests on orca memory (the Oldest Voice
        // slot); whales carry the long song. At Standard (~80y) an orca got
        // ~2.4 succession windows per life and pods lost the replacement
        // coin-flip on harsh seeds; Enduring (~100y) keeps elders singing
        // long enough for the line to hold.
        LifecycleBand = IceRemembersLifecycleBand.Enduring,
        Description = "A named orca, member of a matriarchal pod. Mobile actor on the physical plane, primarily aligned with the Wake-Singers but capable of independent allegiance.",
        Traits =
        [
            PositiveTrait(
                Trait.OrcaSureWake,
                "Sure Wake",
                "Supportive relationships restore endurance more efficiently.",
                TraitRelationshipDelta(K.Orca, Attr.Endurance, Rel.AlliedWith, EffectRelationshipDirection.Either, 0.03f, 2, PrimaryPlane.Physical)),
            NegativeTrait(
                Trait.OrcaBloodFrenzy,
                "Blood Frenzy",
                "Conflict stress burns endurance faster.",
                TraitRelationshipDelta(K.Orca, Attr.Endurance, Rel.RivalOf, EffectRelationshipDirection.Either, -0.035f, 2, PrimaryPlane.Physical)),
        ],
        Locality = Locality(radius: 2, capacity: 0.4f, support: 0.3f, threat: 0.6f,
            baseMass: 5f, coverageScale: 0f, influenceScale: 0.03f,
            healthScale: 0.04f, attractorRadius: 2),
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
