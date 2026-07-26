namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Kinds;

using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;

/// <summary>
/// Canons born from play: wars end in accords, the Conclave decrees taboos,
/// charged sites found doctrines, the tribunal legislates customs — each
/// culture writes its norms into idea-space, and the breach states those
/// jurisdictions create are enforced back onto the graph (the Accord ends
/// wars; the Silence smothers devotion).
/// </summary>
public static partial class IceRemembersGeneratorDefinition
{
    private const int CanonJurisdictionRadius = 2;
    private const float AccordWarDamp = -0.20f;
    private const float SilenceDevotionDamp = -0.15f;

    private static SemanticGeneratorConfig TheAccordHolds() => new()
    {
        Id = "the_accord_holds",
        Name = "The Accord Holds",
        Description = "A faction at war while standing in accord ground is an oathbreaker, and accords are the kind of law with teeth. The censure enters the record and the war is pressed back toward peace.",
        CausalLimit = "Depends on an oathbreaker-tagged faction; damps its war edges and memorializes the censure. The tag clears itself when the breach ends.",
        Target = TargetWithTags(K.Faction, new HashSet<string>(StringComparer.Ordinal) { "oathbreaker" }),
        Produces =
        [
            Required("$censure", K.Artifact, "seal", "$target", "$target"),
        ],
        Relationships =
        [
            R("$censure", "$target", Rel.Memorializes, 0.80f),
        ],
        Effects =
        [
            new AdjustTargetRelationshipStrengthEffect(
                L(Rel.AtWarWith),
                AccordWarDamp,
                GeneratorRelationshipInstanceSelection.AllTargetRelationships),
        ],
        Triggers = TriggerOn(K.Faction),
        FireWeight = 5,
    };

    private static SemanticGeneratorConfig TheSilenceAnswers() => new()
    {
        Id = "the_silence_answers",
        Name = "The Silence Answers",
        Description = "Worship carried into taboo ground does not go unnoticed. The songs falter, the offerings thin, and the profaner learns why the law was never written down.",
        CausalLimit = "Depends on a profaner-tagged faction; damps its devotion edges. The tag clears itself when the breach ends.",
        Target = TargetWithTags(K.Faction, new HashSet<string>(StringComparer.Ordinal) { "profaner" }),
        Produces = [],
        Relationships = [],
        Effects =
        [
            new AdjustTargetRelationshipStrengthEffect(
                L(Rel.OfferedTo),
                SilenceDevotionDamp,
                GeneratorRelationshipInstanceSelection.AllTargetRelationships),
        ],
        Triggers = TriggerOn(K.Faction),
        FireWeight = 4,
    };

    /// <summary>A canon produced by a generator, claiming jurisdiction on creation.</summary>
    private static SemanticProducedEntity ProducedCanon(
        string @ref,
        string subtype,
        string placementRef,
        string? culture,
        string? inheritCultureFrom,
        float chance = 1f)
    {
        var physicalProfile = IceRemembersPlacementTerrainProfiles.Physical(K.Canon, subtype);
        var semanticProfile = IceRemembersPlacementTerrainProfiles.Semantic(K.Canon, subtype);
        return new()
        {
            Ref = @ref,
            Kind = K.Canon,
            Subtype = subtype,
            Role = subtype,
            CreateChance = chance,
            PhysicalPlacement = SemanticPlacement.Near,
            PhysicalPlacementPreferredTerrains = physicalProfile.PreferredTerrains,
            PhysicalPlacementAvoidedTerrains = physicalProfile.AvoidedTerrains,
            PhysicalPlacementFallbackTerrain = physicalProfile.FallbackTerrain,
            PhysicalPlacementPreferredSites = physicalProfile.PreferredSites,
            PhysicalPlacementWarmthAffinity = physicalProfile.WarmthAffinity,
            PhysicalPlacementFireAffinity = physicalProfile.FireAffinity,
            PhysicalPlacementAuroraAffinity = physicalProfile.AuroraAffinity,
            PhysicalPlacementPressureAffinity = physicalProfile.PressureAffinity,
            SemanticPlacement = SemanticPlacement.Near,
            SemanticPlacementPreferredTerrains = semanticProfile.PreferredTerrains,
            SemanticPlacementAvoidedTerrains = semanticProfile.AvoidedTerrains,
            SemanticPlacementFallbackTerrain = semanticProfile.FallbackTerrain,
            PlacementRef = placementRef,
            Culture = culture,
            InheritCultureFrom = inheritCultureFrom,
            CulturePlacement = GeneratorCulturePlacement.PreferInheritedCulture,
        };
    }
}
