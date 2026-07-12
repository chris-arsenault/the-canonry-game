namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Kinds;

using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;

/// <summary>
/// The prey-side response: culture-generic loops where being hunted feeds the
/// hunted culture back. Bereaved shores raise memorials (grief becomes
/// Witness), terrified shores raise wardens who push the hunt back, and the
/// predation web reaches every kind — monsters stalk the marches and pods
/// hunt the convoys, so settlements, transports, monsters, artifacts, and
/// factions all participate in the same ladder.
/// </summary>
public static partial class IceRemembersGeneratorDefinition
{
    private const float WardenPredationDamp = -0.10f;

    private static SemanticGeneratorConfig MartyrsAreWitnessed() => new()
    {
        Id = "martyrs_are_witnessed",
        Name = "Martyrs Are Witnessed",
        Description = "A shore that lost someone named does not forget. A memorial seal enters the record and the grief becomes something the ice can witness.",
        CausalLimit = "Depends on a bereaved settlement without a memorial; the memorializes edge is the repeat lock.",
        Target = TargetTaggedWithoutIncoming(K.Settlement, "bereaved", Rel.Memorializes),
        Produces =
        [
            Required("$memorial", K.Artifact, "seal", "$target", "$target"),
        ],
        Relationships =
        [
            R("$memorial", "$target", Rel.Memorializes, 0.90f),
        ],
        Triggers = TriggerOn(K.Settlement),
        PulseWeight = 5,
    };

    private static SemanticGeneratorConfig WardensRiseWhereTheHuntBites() => new()
    {
        Id = "wardens_rise_where_the_hunt_bites",
        Name = "Wardens Rise Where The Hunt Bites",
        Description = "Terror organizes. A warden rises from the terrified shore, takes the watch, and the hunt is pressed back — suffering converted into strength.",
        CausalLimit = "Depends on a terrified settlement; raises a same-culture warden and damps the settlement's incoming predation.",
        Target = TargetWithTags(K.Settlement, new HashSet<string>(StringComparer.Ordinal) { "terrified" }),
        Produces =
        [
            Required("$warden", K.Penguin, "warden", "$target", "$target"),
        ],
        Relationships =
        [
            R("$warden", "$target", Rel.HomeBase, 1f),
        ],
        Effects =
        [
            new AdjustTargetRelationshipStrengthEffect(
                L(Rel.PreysOn),
                WardenPredationDamp,
                GeneratorRelationshipInstanceSelection.AllTargetRelationships),
        ],
        Triggers = TriggerOn(K.Settlement),
        PulseWeight = 6,
    };

    private static SemanticGeneratorConfig MonstersStalkTheMarches() => new()
    {
        Id = "monsters_stalk_the_marches",
        Name = "Monsters Stalk The Marches",
        Description = "The hungry things do not care whose shore it is. A monster begins working a settlement's outskirts, and the same fear that follows the pods follows it.",
        CausalLimit = "Depends on a living monster; creates or renews a preys_on edge toward a settlement and marks it hunted.",
        Target = TargetWithoutOutgoing(K.Monster, Rel.PreysOn),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$prey",
                Kind = K.Settlement,
            },
        ],
        Produces = [],
        Relationships =
        [
            R("$target", "$prey", Rel.PreysOn, 0.50f, replaceExisting: true),
        ],
        Effects =
        [
            new SetBindingTagEffect("$prey", "hunted"),
        ],
        Triggers = TriggerOn(K.Monster),
        PulseWeight = 4,
    };

    private static SemanticGeneratorConfig PodsHuntTheConvoys() => new()
    {
        Id = "pods_hunt_the_convoys",
        Name = "Pods Hunt The Convoys",
        Description = "The open water belongs to the pods. A devoted faction turns on a trade vessel of another culture, and the route learns what the shores already know.",
        CausalLimit = "Depends on a faction with devotion flow; creates or renews a preys_on edge toward a transport of another culture.",
        Target = TargetWithOutgoing(K.Faction, Rel.OfferedTo),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$prey",
                Kind = K.Transport,
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
        Triggers = TriggerOn(K.Faction),
        PulseWeight = 4,
    };

    private static SemanticGeneratorTarget TargetTaggedWithoutIncoming(
        EntityKind kind,
        string requiredTag,
        Rel withoutIncoming) =>
        new()
        {
            Kind = kind,
            RequiredTags = new HashSet<string>(StringComparer.Ordinal) { requiredTag },
            WithoutIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(withoutIncoming) },
        };
}
