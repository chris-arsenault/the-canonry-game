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
    private const float KillsSungMinimumPredationStrength = 0.50f;
    private const float KillsSungPredationSpend = -0.08f;
    private const float GloryMinimumPredationStrength = 0.60f;
    private const float GloryPredationSpend = -0.20f;
    private const float ProvidesMinimumOfferingStrength = 0.50f;
    private const float ProvidesOfferingSpend = -0.30f;
    private const float NewSiteMinimumOfferingStrength = 0.75f;
    private const float NewSiteOfferingSpend = -0.40f;
    private const float AccordPredationDamp = -0.15f;

    private static SemanticGeneratorConfig KillsSungToTheDeep() => new()
    {
        Id = "kills_sung_to_the_deep",
        Name = "Kills Sung To The Deep",
        Description = "A hunting faction brings its kills to the sacred waters. The blood is drained, the trophy is given to the deep, and a little of the hunt's force ebbs into worship.",
        CausalLimit = "Depends on a strong outgoing predation relationship and a same-culture sacred site; spends part of the selected predation strength, which self-limits repeat firing.",
        Target = TargetWithStrongOutgoing(K.Faction, Rel.PreysOn, KillsSungMinimumPredationStrength),
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
            R("$target", "$site", Rel.OfferedTo, 0.70f, replaceExisting: true),
            R("$trophy", "$site", Rel.ConsignedTo, 0.80f),
        ],
        Effects =
        [
            new AdjustTargetRelationshipStrengthEffect(
                L(Rel.PreysOn),
                KillsSungPredationSpend,
                GeneratorRelationshipInstanceSelection.TriggerRelationship),
        ],
        Triggers = TriggerOn(K.Faction),
        FireWeight = 8,
    };

    private static SemanticGeneratorConfig EldersDescendToTheGod() => new()
    {
        Id = "elders_descend_to_the_god",
        Name = "Elders Descend To The God",
        Description = "On the old rhythm, an elder who can no longer hunt makes the Deep Descent. This is completion, not tragedy: a relic of the descent joins the deep, and the pod's devotion is renewed.",
        CausalLimit = "Targets a worshipped site with a living same-culture elder; marks the elder historical.",
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
        FireWeight = 2,
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
        FireWeight = 4,
    };

    private static SemanticGeneratorConfig VigilsAtTheCurrent() => new()
    {
        Id = "vigils_at_the_current",
        Name = "Vigils At The Current",
        Description = "The songs never fully stop. On the vigil rhythm a singer returns to the sacred waters and offers the long harmonics, and something below answers just enough.",
        CausalLimit = "Targets a worshipped site with a living same-culture singer; renews offering strength.",
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
            R("$singer", "$target", Rel.OfferedTo, 0.55f, replaceExisting: true),
        ],
        Triggers = TriggerOn(K.PointOfInterest),
        FireWeight = 5,
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
            OptionalWakeSingers("$elder", K.Orca, "elder", "$target", GeneratorCulturePlacement.CultureRoute, chance: 0.20f),
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
        FireWeight = 10,
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
            RequiredWakeSingers("$newsite", K.PointOfInterest, "vent", "$target", GeneratorCulturePlacement.CultureFrontier, frontier: true, physicalFrontier: true),
            ProducedCanon("$creed", "doctrine", "$target", culture: C(IceRemembersCulture.WakeSingers), inheritCultureFrom: null, chance: 0.60f),
        ],
        Relationships =
        [
            R("$newsite", "$target", Rel.FoundedFrom, 0.85f),
            R("$newsite", "$keeper", Rel.ClaimedBy, 0.70f),
            R("$keeper", "$newsite", Rel.OfferedTo, 0.50f),
            R("$creed", "$keeper", Rel.ClaimedBy, 0.60f),
        ],
        Effects =
        [
            new ClaimSemanticJurisdictionEffect("$creed", CanonJurisdictionRadius),
            new AdjustTargetRelationshipStrengthEffect(
                L(Rel.OfferedTo),
                NewSiteOfferingSpend,
                GeneratorRelationshipInstanceSelection.AllTargetRelationships),
        ],
        Triggers = TriggerOn(K.PointOfInterest),
        FireWeight = 3,
    };

    private static SemanticGeneratorConfig PodsMarkTheirPrey() => new()
    {
        Id = "pods_mark_their_prey",
        Name = "Pods Mark Their Prey",
        Description = "A devoted faction turns its hunger on the surface world. The pods mark a rich shore and begin taking from its unnamed masses — ordinary predation, not war.",
        CausalLimit = "Depends on a faction with devotion flow; creates or renews a preys_on edge toward a settlement of another culture.",
        Target = TargetWithOutgoing(K.Faction, Rel.OfferedTo),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$prey",
                Kind = K.Settlement,
                Query = new SemanticEntityQuery
                {
                    TargetCultureMatch = QueryTargetCultureMatch.DifferentFromTarget,
                },
            },
        ],
        Produces = [],
        Relationships =
        [
            R("$target", "$prey", Rel.PreysOn, 0.55f, replaceExisting: true),
        ],
        Effects =
        [
            new SetBindingTagEffect("$prey", "hunted"),
        ],
        Triggers = TriggerOn(K.Faction),
        FireWeight = 6,
    };

    private static SemanticGeneratorConfig AHeroFallsToTheDeep() => new()
    {
        Id = "a_hero_falls_to_the_deep",
        Name = "A Hero Falls To The Deep",
        Description = "Rarely, the hunt takes someone whose name mattered. The settlement is bereaved, the predation deepens, and the grievance is the kind wars are made of.",
        CausalLimit = "Depends on strong predation against a settlement with a living named penguin of its culture; marks the victim historical and sets the bereaved tag.",
        Target = TargetWithIncomingAndWithoutIncoming(K.Settlement, Rel.PreysOn, Rel.CleansedBy),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$victim",
                Kind = K.Penguin,
                Query = SameCultureAsTarget(),
            },
        ],
        Produces = [],
        Relationships = [],
        Effects =
        [
            new SetBindingHistoricalEffect("$victim"),
            new SetBindingTagEffect("$target", "bereaved"),
        ],
        Triggers = TriggerOn(K.Settlement),
        FireWeight = 2,
    };

    private static SemanticGeneratorConfig GloryToTheDeadGod() => new()
    {
        Id = "glory_to_the_dead_god",
        Name = "Glory To The Dead God",
        Description = "When the choir is fervent and the hunt runs hot, the orthodox get their way: the predation is consecrated into open war, and the war gets a name.",
        CausalLimit = "Depends on a fervent faction with very strong predation; converts predation strength into a named at-war relationship.",
        Target = TargetWithStrongOutgoingTagged(K.Faction, Rel.PreysOn, GloryMinimumPredationStrength, "fervent"),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$foe",
                Kind = K.Faction,
                Query = new SemanticEntityQuery
                {
                    TargetCultureMatch = QueryTargetCultureMatch.DifferentFromTarget,
                },
            },
        ],
        Produces =
        [
            Required("$war", K.Occurrence, "war", "$target", "$target"),
        ],
        Relationships =
        [
            R("$target", "$foe", Rel.AtWarWith, 0.72f),
            R("$war", "$target", Rel.AtWarWith, 0.60f),
        ],
        Effects =
        [
            new AdjustTargetRelationshipStrengthEffect(
                L(Rel.PreysOn),
                GloryPredationSpend,
                GeneratorRelationshipInstanceSelection.TriggerRelationship),
        ],
        Triggers = TriggerOn(K.Faction),
        FireWeight = 4,
    };

    private static SemanticGeneratorConfig CounterOffensive() => new()
    {
        Id = "counter_offensive",
        Name = "Counter-Offensive",
        Description = "A settlement that is both terrified and bereaved stops absorbing the hunt. Its protectors declare a named war on the choir, and the grievance is answered.",
        CausalLimit = "Depends on a terrified, bereaved settlement; creates a named at-war relationship from the defending faction and clears the bereaved tag as the repeat lock.",
        Target = TargetWithTags(K.Settlement, new HashSet<string>(StringComparer.Ordinal) { "terrified", "bereaved" }),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$defender",
                Kind = K.Faction,
                Query = SameCultureAsTarget(),
            },
            new SemanticGeneratorBindingTarget
            {
                Ref = "$raider",
                Kind = K.Faction,
                Query = new SemanticEntityQuery { Culture = C(IceRemembersCulture.WakeSingers) },
            },
        ],
        Produces =
        [
            Required("$war", K.Occurrence, "war", "$target", "$defender"),
        ],
        Relationships =
        [
            R("$defender", "$raider", Rel.AtWarWith, 0.70f),
            R("$war", "$defender", Rel.AtWarWith, 0.60f),
        ],
        Effects =
        [
            new ClearBindingTagEffect("$target", "bereaved"),
        ],
        Triggers = TriggerOn(K.Settlement),
        FireWeight = 5,
    };

    private static SemanticGeneratorConfig TheAccordConvenes() => new()
    {
        Id = "the_accord_convenes",
        Name = "The Accord Convenes",
        Description = "On the civic calendar the Flipper Accord tribunal takes up the worst of the grievances. A terrified settlement is heard, the raiders are named and warned, and the hunt is pressed back for a season.",
        CausalLimit = "Targets a terrified settlement; clears the terrified tag and damps its incoming predation edges.",
        Target = TargetWithTags(K.Settlement, new HashSet<string>(StringComparer.Ordinal) { "terrified" }),
        Relationships =
        [
            R("$ruling", "$target", Rel.Memorializes, 0.70f),
        ],
        Produces =
        [
            ProducedCanon("$ruling", "custom", "$target", culture: null, inheritCultureFrom: "$target", chance: 0.35f),
        ],
        Effects =
        [
            new ClearBindingTagEffect("$target", "terrified"),
            new ClaimSemanticJurisdictionEffect("$ruling", CanonJurisdictionRadius),
            new AdjustTargetRelationshipStrengthEffect(
                L(Rel.PreysOn),
                AccordPredationDamp,
                GeneratorRelationshipInstanceSelection.AllTargetRelationships),
        ],
        Triggers = TriggerOn(K.Settlement),
        FireWeight = 2,
    };

    private static SemanticGeneratorConfig TheHuntSettlesIn() => new()
    {
        Id = "the_hunt_settles_in",
        Name = "The Hunt Settles In",
        Description = "A hunted shore that stays hunted stops sleeping. The raids keep coming, the counts keep coming up short, and the settlement crosses from wariness into terror.",
        CausalLimit = "Depends on a hunted settlement still under active predation; steps the terror ladder from hunted to terrified.",
        Target = TargetWithIncomingTagged(K.Settlement, Rel.PreysOn, "hunted", "terrified"),
        Produces = [],
        Relationships = [],
        Effects =
        [
            new SetBindingTagEffect("$target", "terrified"),
        ],
        Triggers = TriggerOn(K.Settlement),
        FireWeight = 6,
    };

    private static SemanticGeneratorConfig TheFearFades() => new()
    {
        Id = "the_fear_fades",
        Name = "The Fear Fades",
        Description = "When the pods stop coming, the shore slowly remembers ordinary life. The watch relaxes, the terror lifts, and the hunt becomes a story.",
        CausalLimit = "Depends on a hunted settlement with no remaining predation; steps the terror ladder back down.",
        Target = TargetWithoutIncomingTagged(K.Settlement, Rel.PreysOn, "hunted"),
        Produces = [],
        Relationships = [],
        Effects =
        [
            new ClearBindingTagEffect("$target", "terrified"),
            new ClearBindingTagEffect("$target", "hunted"),
        ],
        Triggers = TriggerOn(K.Settlement),
        FireWeight = 3,
    };

    private static SemanticGeneratorTarget TargetWithIncomingTagged(
        EntityKind kind,
        Rel required,
        string requiredTag,
        string withoutTag) =>
        new()
        {
            Kind = kind,
            RequiredTags = new HashSet<string>(StringComparer.Ordinal) { requiredTag },
            WithoutTags = new HashSet<string>(StringComparer.Ordinal) { withoutTag },
            RequiredIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(required) },
        };

    private static SemanticGeneratorTarget TargetWithoutIncomingTagged(
        EntityKind kind,
        Rel withoutRelationship,
        string requiredTag) =>
        new()
        {
            Kind = kind,
            RequiredTags = new HashSet<string>(StringComparer.Ordinal) { requiredTag },
            WithoutIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(withoutRelationship) },
        };

    private static string C(IceRemembersCulture culture) => culture.ToDomainName();

    private static SemanticGeneratorTarget TargetWithTags(EntityKind kind, IReadOnlySet<string> requiredTags) =>
        new()
        {
            Kind = kind,
            RequiredTags = requiredTags,
        };

    private static SemanticGeneratorTarget TargetWithStrongOutgoingTagged(
        EntityKind kind,
        Rel required,
        float minimumStrength,
        string requiredTag) =>
        new()
        {
            Kind = kind,
            RequiredTags = new HashSet<string>(StringComparer.Ordinal) { requiredTag },
            RequiredOutgoingRelationshipRequirements =
            [
                new SemanticGeneratorRelationshipRequirement
                {
                    RelationshipKind = L(required),
                    MinimumStrength = minimumStrength,
                },
            ],
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
        GeneratorCulturePlacement culturePlacement,
        bool frontier = false,
        bool physicalFrontier = false)
    {
        var physicalProfile = IceRemembersPlacementTerrainProfiles.Physical(kind, subtype);
        var semanticProfile = IceRemembersPlacementTerrainProfiles.Semantic(kind, subtype);
        return new()
        {
            Ref = @ref,
            Kind = kind,
            Subtype = subtype,
            Role = subtype,
            PhysicalPlacement = physicalFrontier ? SemanticPlacement.Frontier : SemanticPlacement.Near,
            PhysicalPlacementPreferredTerrains = physicalProfile.PreferredTerrains,
            PhysicalPlacementAvoidedTerrains = physicalProfile.AvoidedTerrains,
            PhysicalPlacementFallbackTerrain = physicalProfile.FallbackTerrain,
            PhysicalPlacementPreferredSites = physicalProfile.PreferredSites,
            PhysicalPlacementWarmthAffinity = physicalProfile.WarmthAffinity,
            PhysicalPlacementFireAffinity = physicalProfile.FireAffinity,
            PhysicalPlacementAuroraAffinity = physicalProfile.AuroraAffinity,
            PhysicalPlacementPressureAffinity = physicalProfile.PressureAffinity,
            SemanticPlacement = frontier ? SemanticPlacement.Frontier : SemanticPlacement.Near,
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
            PhysicalPlacementPreferredSites = produced.PhysicalPlacementPreferredSites,
            PhysicalPlacementWarmthAffinity = produced.PhysicalPlacementWarmthAffinity,
            PhysicalPlacementFireAffinity = produced.PhysicalPlacementFireAffinity,
            PhysicalPlacementAuroraAffinity = produced.PhysicalPlacementAuroraAffinity,
            PhysicalPlacementPressureAffinity = produced.PhysicalPlacementPressureAffinity,
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
