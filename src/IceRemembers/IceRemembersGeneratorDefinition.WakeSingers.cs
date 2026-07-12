namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Kinds;

using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;

/// <summary>
/// The Wake-Singers' devotion economy. Worship of the Dead God is carried
/// entirely in relationship strength and the spatial field: offering edges
/// (`offered_to`, `consigned_to`) accumulate on sacred sites and drift down
/// under maintenance, and the god answers by spending them — new pods calve
/// along the warm corridor, and heavily charged sites found daughter sites.
/// Blood is one offering channel among several: wars feed the deep, but so do
/// elder descents, consigned relics, and sung vigils, so a raid-suppressed run
/// keeps the same god and the same scoreboard at a quieter tempo.
/// </summary>
public static partial class IceRemembersGeneratorDefinition
{
    private const float KillsSungMinimumWarStrength = 0.50f;
    private const float KillsSungWarStrengthSpend = -0.08f;
    private const float ProvidesMinimumOfferingStrength = 0.80f;
    private const float ProvidesOfferingSpend = -0.25f;
    private const float NewSiteMinimumOfferingStrength = 0.95f;
    private const float NewSiteOfferingSpend = -0.40f;
    private const int VigilCadenceTicks = 600;
    private const int VigilCadenceJitterTicks = 120;
    private const int DescentCadenceTicks = 2000;
    private const int DescentCadenceJitterTicks = 400;

    private static SemanticGeneratorConfig KillsSungToTheDeep() => new()
    {
        Id = "kills_sung_to_the_deep",
        Name = "Kills Sung To The Deep",
        Description = "A faction at war brings its kills to the sacred waters. The blood is drained, the trophy is given to the deep, and a little of the war's force ebbs into worship.",
        CausalLimit = "Depends on a strong at-war relationship and a same-culture sacred site; spends part of the selected war strength, which self-limits repeat firing.",
        Target = TargetWithStrongOutgoing(K.Faction, Rel.AtWarWith, KillsSungMinimumWarStrength),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$site",
                Kind = K.PointOfInterest,
                Query = SameCultureAsTarget(),
            },
        ],
        Produces =
        [
            Required("$trophy", K.Artifact, "relic", "$site", "$target"),
        ],
        Relationships =
        [
            R("$target", "$site", Rel.OfferedTo, 0.70f),
            R("$trophy", "$site", Rel.ConsignedTo, 0.80f),
        ],
        Effects =
        [
            new AdjustTargetRelationshipStrengthEffect(
                L(Rel.AtWarWith),
                KillsSungWarStrengthSpend,
                GeneratorRelationshipInstanceSelection.TriggerRelationship,
                includeReciprocal: true),
        ],
        Triggers = TriggerOn(K.Faction),
        PulseWeight = 8,
    };

    private static SemanticGeneratorConfig EldersDescendToTheGod() => new()
    {
        Id = "elders_descend_to_the_god",
        Name = "Elders Descend To The God",
        Description = "On the old rhythm, an elder who can no longer hunt makes the Deep Descent. This is completion, not tragedy: a relic of the descent joins the deep, and the pod's devotion is renewed.",
        CausalLimit = "Fires on an authored cadence against a worshipped site with a living same-culture elder; marks the elder historical.",
        Target = TargetWithIncomingAndWithoutIncoming(K.PointOfInterest, Rel.OfferedTo, Rel.CleansedBy),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$elder",
                Kind = K.Orca,
                Query = new SemanticEntityQuery
                {
                    Subtypes = new HashSet<string>(StringComparer.Ordinal) { "elder" },
                    TargetCultureMatch = QueryTargetCultureMatch.SameAsTarget,
                },
            },
        ],
        Produces =
        [
            RequiredWakeSingers("$descent_relic", K.Artifact, "relic", "$target", GeneratorCulturePlacement.PreferInheritedCulture),
        ],
        Relationships =
        [
            R("$descent_relic", "$target", Rel.ConsignedTo, 0.90f),
            R("$descent_relic", "$elder", Rel.Memorializes, 0.80f),
        ],
        Effects =
        [
            new SetBindingHistoricalEffect("$elder"),
        ],
        Triggers = TriggerOn(K.PointOfInterest),
        PulseWeight = 1,
        CadenceTicks = DescentCadenceTicks,
        CadenceJitterTicks = DescentCadenceJitterTicks,
    };

    private static SemanticGeneratorConfig RelicsAreGivenToTheDeep() => new()
    {
        Id = "relics_are_given_to_the_deep",
        Name = "Relics Are Given To The Deep",
        Description = "A held relic is carried to the sacred waters and given away. The god's tools return to the god, and the holder's standing returns as devotion.",
        CausalLimit = "Depends on a held artifact without a consignment and a same-culture sacred site; the consigned_to edge is the repeat lock.",
        Target = TargetWithIncomingAndWithoutOutgoing(K.Artifact, Rel.Holds, Rel.ConsignedTo),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$site",
                Kind = K.PointOfInterest,
                Query = SameCultureAsTarget(),
            },
        ],
        Produces = [],
        Relationships =
        [
            R("$target", "$site", Rel.ConsignedTo, 0.80f),
        ],
        Triggers = TriggerOn(K.Artifact),
        PulseWeight = 4,
    };

    private static SemanticGeneratorConfig VigilsAtTheCurrent() => new()
    {
        Id = "vigils_at_the_current",
        Name = "Vigils At The Current",
        Description = "The songs never fully stop. On the vigil rhythm a singer returns to the sacred waters and offers the long harmonics, and something below answers just enough.",
        CausalLimit = "Fires on an authored cadence against a worshipped site with a living same-culture singer; renews offering strength.",
        Target = TargetWithIncomingAndWithoutIncoming(K.PointOfInterest, Rel.OfferedTo, Rel.CleansedBy),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$singer",
                Kind = K.Orca,
                Query = SameCultureAsTarget(),
            },
        ],
        Produces = [],
        Relationships =
        [
            R("$singer", "$target", Rel.OfferedTo, 0.55f),
        ],
        Triggers = TriggerOn(K.PointOfInterest),
        PulseWeight = 1,
        CadenceTicks = VigilCadenceTicks,
        CadenceJitterTicks = VigilCadenceJitterTicks,
    };

    private static SemanticGeneratorConfig TheCurrentProvides() => new()
    {
        Id = "the_current_provides",
        Name = "The Current Provides",
        Description = "A site charged with enough devotion answers. The warm water widens, prey runs thick, and a new pod calves along the corridor — the offerings are spent into life.",
        CausalLimit = "Depends on strong incoming offering strength; spends all offering edges on the site when it fires.",
        Target = TargetWithStrongIncomingAndWithoutIncoming(
            K.PointOfInterest,
            Rel.OfferedTo,
            ProvidesMinimumOfferingStrength,
            Rel.CleansedBy),
        Produces =
        [
            RequiredWakeSingers("$calf", K.Orca, "scout", "$target", GeneratorCulturePlacement.CultureRoute),
            OptionalWakeSingers("$elder", K.Orca, "elder", "$target", GeneratorCulturePlacement.CultureRoute, chance: 0.30f),
        ],
        Relationships =
        [
            R("$calf", "$target", Rel.HomeBase, 1f),
            R("$elder", "$target", Rel.HomeBase, 1f),
        ],
        Effects =
        [
            new AdjustTargetRelationshipStrengthEffect(
                L(Rel.OfferedTo),
                ProvidesOfferingSpend,
                GeneratorRelationshipInstanceSelection.AllTargetRelationships),
        ],
        Triggers = TriggerOn(K.PointOfInterest),
        PulseWeight = 12,
    };

    private static SemanticGeneratorConfig PilgrimsFoundNewCurrents() => new()
    {
        Id = "pilgrims_found_new_currents",
        Name = "Pilgrims Found New Currents",
        Description = "When a site's devotion overflows, the current branches. Pilgrims consecrate a new warm vent along the corridor and the worship divides between the old water and the new.",
        CausalLimit = "Depends on very strong incoming offering strength and no founding lock; creates incoming founded-from on the parent site and spends its offerings.",
        Target = TargetWithStrongIncomingAndWithoutIncoming(
            K.PointOfInterest,
            Rel.OfferedTo,
            NewSiteMinimumOfferingStrength,
            Rel.FoundedFrom),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$keeper",
                Kind = K.Faction,
                Query = SameCultureAsTarget(),
            },
        ],
        Produces =
        [
            RequiredWakeSingers("$newsite", K.PointOfInterest, "vent", "$target", GeneratorCulturePlacement.CultureRoute),
        ],
        Relationships =
        [
            R("$newsite", "$target", Rel.FoundedFrom, 0.85f),
            R("$newsite", "$keeper", Rel.ClaimedBy, 0.70f),
            R("$keeper", "$newsite", Rel.OfferedTo, 0.50f),
        ],
        Effects =
        [
            new AdjustTargetRelationshipStrengthEffect(
                L(Rel.OfferedTo),
                NewSiteOfferingSpend,
                GeneratorRelationshipInstanceSelection.AllTargetRelationships),
        ],
        Triggers = TriggerOn(K.PointOfInterest),
        PulseWeight = 3,
    };

    /// <summary>
    /// A produced entity with authored Wake-Singer culture. Calves and daughter
    /// sites use CultureRoute placement so the god only provides where the
    /// corridor's field is already warm — the spatial half of the loop.
    /// </summary>
    private static SemanticProducedEntity RequiredWakeSingers(
        string @ref,
        EntityKind kind,
        string subtype,
        string placementRef,
        GeneratorCulturePlacement culturePlacement)
    {
        var physicalProfile = IceRemembersPlacementTerrainProfiles.Physical(kind, subtype);
        var semanticProfile = IceRemembersPlacementTerrainProfiles.Semantic(kind, subtype);
        return new()
        {
            Ref = @ref,
            Kind = kind,
            Subtype = subtype,
            Role = subtype,
            PhysicalPlacement = SemanticPlacement.Near,
            PhysicalPlacementPreferredTerrains = physicalProfile.PreferredTerrains,
            PhysicalPlacementAvoidedTerrains = physicalProfile.AvoidedTerrains,
            PhysicalPlacementFallbackTerrain = physicalProfile.FallbackTerrain,
            SemanticPlacement = SemanticPlacement.Near,
            SemanticPlacementPreferredTerrains = semanticProfile.PreferredTerrains,
            SemanticPlacementAvoidedTerrains = semanticProfile.AvoidedTerrains,
            SemanticPlacementFallbackTerrain = semanticProfile.FallbackTerrain,
            PlacementRef = placementRef,
            Culture = IceRemembersCulture.WakeSingers.ToDomainName(),
            CulturePlacement = culturePlacement,
        };
    }

    private static SemanticProducedEntity OptionalWakeSingers(
        string @ref,
        EntityKind kind,
        string subtype,
        string placementRef,
        GeneratorCulturePlacement culturePlacement,
        float chance)
    {
        var produced = RequiredWakeSingers(@ref, kind, subtype, placementRef, culturePlacement);
        return new()
        {
            Ref = produced.Ref,
            Kind = produced.Kind,
            Subtype = produced.Subtype,
            Role = produced.Role,
            CreateChance = chance,
            PhysicalPlacement = produced.PhysicalPlacement,
            PhysicalPlacementPreferredTerrains = produced.PhysicalPlacementPreferredTerrains,
            PhysicalPlacementAvoidedTerrains = produced.PhysicalPlacementAvoidedTerrains,
            PhysicalPlacementFallbackTerrain = produced.PhysicalPlacementFallbackTerrain,
            SemanticPlacement = produced.SemanticPlacement,
            SemanticPlacementPreferredTerrains = produced.SemanticPlacementPreferredTerrains,
            SemanticPlacementAvoidedTerrains = produced.SemanticPlacementAvoidedTerrains,
            SemanticPlacementFallbackTerrain = produced.SemanticPlacementFallbackTerrain,
            PlacementRef = produced.PlacementRef,
            Culture = produced.Culture,
            CulturePlacement = produced.CulturePlacement,
        };
    }
}
