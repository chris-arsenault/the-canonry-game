namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Kinds;

using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;

public static partial class IceRemembersGeneratorDefinition
{
    private const float OrcaRouteWarsGetNamesMinimumWarStrength = 0.68f;
    private const float OrcaRouteWarsGetNamesWarStrengthSpend = -0.22f;
    private const float HauntedTransportsSpreadPlagueMinimumCorruptionStrength = 0.68f;
    private const float HauntedTransportsSpreadPlagueCorruptionStrengthSpend = -0.24f;

    private static SemanticGeneratorConfig PodCurrentsLaunchScouts() => new()
    {
        Id = "pod_currents_launch_scouts",
        Name = "Pod Currents Launch Scouts",
        Description = "An orca with a settled hunting ground can launch a carried skiff and a newly named shoal. The current expands route topology without adding another orca body.",
        CausalLimit = "Depends on an orca home-base edge and no incoming claimed-by edge on the orca; creates a claimed skiff lock on the parent orca.",
        Target = TargetWithOutgoingAndWithoutIncoming(K.Orca, Rel.HomeBase, Rel.ClaimedBy),
        Produces =
        [
            Required("$skiff", K.Transport, "scout", "$target", "$target"),
            Required("$shoal", K.PointOfInterest, "shoal", "$target", "$target"),
        ],
        Relationships =
        [
            R("$target", "$skiff", Rel.Holds, 1f),
            R("$skiff", "$target", Rel.ClaimedBy, 0.78f),
            R("$skiff", "$shoal", Rel.NativeTo, 0.84f),
            R("$skiff", "$shoal", Rel.ConduitOf, 0.74f),
            R("$shoal", "$target", Rel.ClaimedBy, 0.70f),
        ],
        Triggers = TriggerOn(K.Orca),
        PulseWeight = 4,
    };

    private static SemanticGeneratorConfig OrcaRouteWarsGetNames() => new()
    {
        Id = "orca_route_wars_get_names",
        Name = "Orca Route Wars Get Names",
        Description = "A strong orca-penguin route raid becomes a named war. A defender, a trade convoy, and sometimes an escort enter the record so trade transport is part of the war instead of background scenery.",
        CausalLimit = "Depends on a strong outgoing at-war relationship from an orca and no incoming memorializes lock on that orca; spends the selected war edge and creates the memorializing convoy lock.",
        Target = TargetWithStrongOutgoingAndWithoutIncoming(
            K.Orca,
            Rel.AtWarWith,
            OrcaRouteWarsGetNamesMinimumWarStrength,
            Rel.Memorializes),
        Produces =
        [
            Required("$war", K.Occurrence, "war", "$target", "$target"),
            Required("$defender", K.Penguin, "warden", "$target", "$target"),
            Required("$convoy", K.Transport, "trade", "$target", "$target"),
            Optional("$escort", K.Transport, "battleship", "$target", "$target", chance: 0.45f),
        ],
        Relationships =
        [
            R("$target", "$war", Rel.MemberOf, 0.74f),
            R("$defender", "$war", Rel.MemberOf, 0.78f),
            R("$war", "$target", Rel.AtWarWith, 0.64f),
            R("$war", "$defender", Rel.AtWarWith, 0.62f),
            R("$target", "$defender", Rel.AtWarWith, 0.72f),
            R("$defender", "$target", Rel.AtWarWith, 0.72f),
            R("$defender", "$convoy", Rel.Holds, 1f),
            R("$convoy", "$target", Rel.Memorializes, 0.84f),
            R("$convoy", "$war", Rel.Memorializes, 0.84f),
            R("$convoy", "$target", Rel.WardAgainst, 0.66f),
            R("$defender", "$escort", Rel.Holds, 1f),
            R("$escort", "$target", Rel.WardAgainst, 0.70f),
        ],
        Effects =
        [
            new AdjustTargetRelationshipStrengthEffect(
                L(Rel.AtWarWith),
                OrcaRouteWarsGetNamesWarStrengthSpend,
                GeneratorRelationshipInstanceSelection.TriggerRelationship,
                includeReciprocal: true),
        ],
        Triggers = TriggerOn(K.Orca),
        PulseWeight = 4,
    };

    private static SemanticGeneratorConfig StolenArtifactsDrawHunters() => new()
    {
        Id = "stolen_artifacts_draw_hunters",
        Name = "Stolen Artifacts Draw Hunters",
        Description = "A monster carrying a stolen artifact draws a penguin hunter into the hunt. The theft becomes war topology instead of a static possession.",
        CausalLimit = "Depends on monster-held artifact topology and no incoming at-war lock on the monster; creates incoming at-war hunt edges on the monster.",
        Target = TargetWithOutgoingAndWithoutIncoming(K.Monster, Rel.Holds, Rel.AtWarWith),
        Produces =
        [
            Required("$hunter", K.Penguin, "warden", "$target", "$target"),
        ],
        Relationships =
        [
            R("$hunter", "$target", Rel.AtWarWith, 0.76f),
            R("$target", "$hunter", Rel.AtWarWith, 0.76f),
        ],
        Triggers = TriggerOn(K.Monster),
        PulseWeight = 6,
    };

    private static SemanticGeneratorConfig MonsterSlayingsBecomeFestivals() => new()
    {
        Id = "monster_slayings_become_festivals",
        Name = "Monster Slayings Become Festivals",
        Description = "A penguin who has slain a monster turns the cleansing into a festival, a rite, and a trade convoy. The loop keeps monster resolution connected to transport and exchange.",
        CausalLimit = "Depends on outgoing cleansed-by evidence from the penguin and no incoming memorializes lock; creates rite and convoy memorials back onto the penguin.",
        Target = TargetWithOutgoingAndWithoutIncoming(K.Penguin, Rel.CleansedBy, Rel.Memorializes),
        Produces =
        [
            Required("$festival", K.Occurrence, "festival", "$target", "$target"),
            Required("$rite", K.Ability, "rite", "$target", "$target"),
            Required("$convoy", K.Transport, "trade", "$target", "$target"),
        ],
        Relationships =
        [
            R("$target", "$festival", Rel.MemberOf, 0.80f),
            R("$target", "$rite", Rel.Holds, 1f),
            R("$target", "$convoy", Rel.Holds, 1f),
            R("$rite", "$target", Rel.Memorializes, 0.86f),
            R("$rite", "$festival", Rel.Memorializes, 0.84f),
            R("$convoy", "$target", Rel.Memorializes, 0.78f),
            R("$convoy", "$festival", Rel.Memorializes, 0.80f),
            R("$convoy", "$rite", Rel.ConduitOf, 0.68f),
        ],
        Triggers = TriggerOn(K.Penguin),
        PulseWeight = 12,
    };

    private static SemanticGeneratorConfig HauntedTransportsSpreadPlague() => new()
    {
        Id = "haunted_transports_spread_plague",
        Name = "Haunted Transports Spread Plague",
        Description = "A transport that carries a strong monster-haunting becomes an outbreak vector. A wraith and plague enter the record, and the transport is marked by the event.",
        CausalLimit = "Depends on strong outgoing corrupted-by topology from a transport and no outgoing memorializes lock; spends the selected corruption strength.",
        Target = TargetWithStrongOutgoingAndWithoutOutgoing(
            K.Transport,
            Rel.CorruptedBy,
            HauntedTransportsSpreadPlagueMinimumCorruptionStrength,
            Rel.Memorializes),
        Produces =
        [
            Required("$wraith", K.Monster, "wraith", "$target", "$target"),
            Required("$plague", K.Occurrence, "plague", "$target", "$target"),
        ],
        Relationships =
        [
            R("$target", "$wraith", Rel.CorruptedBy, 0.82f),
            R("$plague", "$wraith", Rel.CorruptedBy, 0.76f),
            R("$target", "$plague", Rel.Memorializes, 0.78f),
        ],
        Effects =
        [
            new AdjustTargetRelationshipStrengthEffect(
                L(Rel.CorruptedBy),
                HauntedTransportsSpreadPlagueCorruptionStrengthSpend,
                GeneratorRelationshipInstanceSelection.TriggerRelationship),
        ],
        Triggers = TriggerOn(K.Transport),
        PulseWeight = 3,
    };

    private static SemanticGeneratorTarget TargetWithStrongOutgoingAndWithoutOutgoing(
        EntityKind kind,
        Rel required,
        float minimumStrength,
        Rel without) =>
        new()
        {
            Kind = kind,
            RequiredOutgoingRelationshipRequirements =
            [
                new SemanticGeneratorRelationshipRequirement
                {
                    RelationshipKind = L(required),
                    MinimumStrength = minimumStrength,
                },
            ],
            WithoutOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(without) },
        };
}
