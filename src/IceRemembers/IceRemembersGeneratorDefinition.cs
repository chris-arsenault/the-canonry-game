namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Kinds;
using Engine.WorldGen;

using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;
using Terrain = IceRemembers.IceRemembersTerrain;

public static class IceRemembersGeneratorDefinition
{
    public static IReadOnlyList<SemanticGeneratorConfig> Create() =>
    [
        WarMakesWardens(),
        IceSettlesAccounts(),
        WhatTheSpringRemembers(),
        WarBreaksTheLand(),
        RoutesDrawReaders(),
        WhatTheArtifactKnows(),
    ];

    private static string L(Rel relationship) => relationship.ToDomainName();

    private static SemanticGeneratorConfig WarMakesWardens() => new()
    {
        Id = "war_makes_wardens",
        Name = "War Makes Wardens",
        Target = TargetWithOutgoing(K.Faction, Rel.AtWarWith),
        TargetSaturation = SaturationIncoming(Rel.ControlledBy, 1),
        Produces =
        [
            Required("$warden", K.Penguin, "warden", "$target", "$target"),
            Required("$holdfast", K.Settlement, "holdfast", "$target", "$target"),
            Optional("$weapon", K.Artifact, "weapon", "$holdfast", "$target", chance: 0.55f, on: true),
        ],
        Relationships =
        [
            R("$warden", "$target", Rel.MemberOf, 0.88f),
            R("$warden", "$holdfast", Rel.HomeBase, 0.90f),
            R("$holdfast", "$target", Rel.ControlledBy, 0.85f),
            R("$weapon", "$holdfast", Rel.NativeTo, 0.82f),
        ],
        Triggers = TriggerOn(K.Faction),
    };

    private static SemanticGeneratorConfig IceSettlesAccounts() => new()
    {
        Id = "ice_settles_accounts",
        Name = "Ice Settles Accounts",
        Target = TargetWithOutgoing(K.Faction, Rel.AtWarWith),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$offered",
                Kind = K.Penguin,
                Query = new SemanticEntityQuery { TargetCultureMatch = QueryTargetCultureMatch.SameAsTarget },
            },
        ],
        TargetSaturation = SaturationIncoming(Rel.Memorializes, 1),
        Produces =
        [
            Required("$seal", K.Artifact, "seal", "$target", "$target", on: true),
            Required("$witness", K.Penguin, "witness", "$target", "$target"),
            Optional("$exile", K.Penguin, "exile", "$target", "$target", chance: 0.40f),
        ],
        Effects =
        [
            new SetBindingHistoricalEffect("$offered"),
            new RemoveTargetRelationshipsEffect(L(Rel.AtWarWith)),
        ],
        Relationships =
        [
            R("$seal", "$target", Rel.Memorializes, 0.90f),
            R("$witness", "$target", Rel.MemberOf, 0.85f),
            R("$exile", "$target", Rel.BetrayedBy, 0.70f),
        ],
        Triggers = TriggerOn(K.Faction),
    };

    private static SemanticGeneratorConfig WhatTheSpringRemembers() => new()
    {
        Id = "what_the_spring_remembers",
        Name = "What the Spring Remembers",
        Target = TargetWithIncoming(K.Region, Rel.CleansedBy),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget { Ref = "$patron", Kind = K.Faction },
        ],
        TargetSaturation = SaturationIncoming(Rel.Anchored, 1),
        Produces =
        [
            Required("$relic", K.Artifact, "relic", "$target", "$patron", on: true),
            Required("$witness", K.Penguin, "witness", "$target", "$patron"),
            Optional("$sanctuary", K.Settlement, "sanctuary", "$target", "$patron", chance: 0.55f),
            Optional("$choir", K.Faction, "choir", "$target", "$patron", chance: 0.35f),
        ],
        Relationships =
        [
            R("$relic", "$target", Rel.Anchored, 0.90f),
            R("$witness", "$patron", Rel.MemberOf, 0.85f),
            R("$sanctuary", "$patron", Rel.ControlledBy, 0.80f),
            R("$sanctuary", "$target", Rel.FoundedFrom, 0.85f),
            R("$choir", "$patron", Rel.AlliedWith, 0.70f),
        ],
        Triggers = TriggerOn(K.Region),
    };

    private static SemanticGeneratorConfig WarBreaksTheLand() => new()
    {
        Id = "war_breaks_the_land",
        Name = "War Breaks the Land",
        Target = TargetWithOutgoing(K.Faction, Rel.AtWarWith),
        TargetSaturation = SaturationIncoming(Rel.CorruptedBy, 2),
        Produces =
        [
            ScarRegion("$scar", "$target"),
            Optional("$weapon", K.Artifact, "weapon", "$scar", "$target", chance: 0.35f, on: true),
            Optional("$compact", K.Faction, "compact", "$scar", "$target", chance: 0.40f),
        ],
        Relationships =
        [
            R("$scar", "$target", Rel.CorruptedBy, 0.82f),
            R("$weapon", "$scar", Rel.Anchored, 0.85f),
            R("$scar", "$compact", Rel.ClaimedBy, 0.78f),
            R("$compact", "$target", Rel.RivalOf, 0.75f),
        ],
        Triggers = TriggerOn(K.Faction),
    };

    private static SemanticGeneratorConfig RoutesDrawReaders() => new()
    {
        Id = "routes_draw_readers",
        Name = "Routes Draw Readers",
        Target = TargetWithOutgoing(K.Settlement, Rel.TradeRoute),
        TargetSaturation = SaturationIncoming(Rel.FoundedFrom, 1),
        Produces =
        [
            Required("$broker", K.Penguin, "broker", "$target", "$target"),
            HarborAcross("$harbor", "$target"),
            Optional("$order", K.Faction, "order", "$harbor", "$target", chance: 0.40f),
            Optional("$instrument", K.Artifact, "instrument", "$harbor", "$target", chance: 0.45f, on: true),
        ],
        Relationships =
        [
            R("$broker", "$harbor", Rel.HomeBase, 0.88f),
            R("$harbor", "$target", Rel.FoundedFrom, 0.90f),
            R("$harbor", "$target", Rel.TradeRoute, 0.72f),
            R("$harbor", "$order", Rel.ControlledBy, 0.82f),
            R("$broker", "$order", Rel.MemberOf, 0.80f),
            R("$instrument", "$harbor", Rel.NativeTo, 0.82f),
        ],
        Triggers = TriggerOn(K.Settlement),
    };

    private static SemanticGeneratorConfig WhatTheArtifactKnows() => new()
    {
        Id = "what_the_artifact_knows",
        Name = "What the Artifact Knows",
        Target = TargetWithOutgoing(K.Penguin, Rel.Holds),
        TargetSaturation = SaturationOutgoing(Rel.HomeBase, 1),
        Produces =
        [
            Required("$rookery", K.Settlement, "rookery", "$target", "$target"),
            Required("$seal", K.Artifact, "seal", "$rookery", "$target", on: true),
            Optional("$cell", K.Faction, "cell", "$target", "$target", chance: 0.45f),
        ],
        Relationships =
        [
            R("$target", "$rookery", Rel.HomeBase, 0.90f),
            R("$seal", "$rookery", Rel.Anchored, 0.88f),
            R("$rookery", "$cell", Rel.ControlledBy, 0.82f),
            R("$cell", "$target", Rel.LedBy, 0.85f),
        ],
        Triggers = TriggerOn(K.Penguin),
    };

    private static SemanticGeneratorTarget TargetWithOutgoing(EntityKind kind, Rel required) =>
        new()
        {
            Kind = kind,
            RequiredOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(required) },
        };

    private static SemanticGeneratorTarget TargetWithIncoming(EntityKind kind, Rel required) =>
        new()
        {
            Kind = kind,
            RequiredIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(required) },
        };

    private static SemanticTargetSaturation SaturationIncoming(Rel kind, int max) =>
        new()
        {
            RelationshipKind = L(kind),
            Direction = TargetSaturationDirection.IncomingToTarget,
            MaxExisting = max,
        };

    private static SemanticTargetSaturation SaturationOutgoing(Rel kind, int max) =>
        new()
        {
            RelationshipKind = L(kind),
            Direction = TargetSaturationDirection.OutgoingFromTarget,
            MaxExisting = max,
        };

    private static IReadOnlyList<SemanticGeneratorTrigger> TriggerOn(EntityKind kind) =>
    [
        new() { OnKind = kind, Direction = TriggerDirection.Any },
    ];

    private static SemanticProducedEntity Required(
        string @ref,
        EntityKind kind,
        string subtype,
        string placementRef,
        string cultureRef,
        bool on = false) =>
        new()
        {
            Ref = @ref,
            Kind = kind,
            Subtype = subtype,
            Role = subtype,
            PhysicalPlacement = on ? SemanticPlacement.On : SemanticPlacement.Near,
            SemanticPlacement = SemanticPlacement.Near,
            PlacementRef = placementRef,
            InheritCultureFrom = cultureRef,
        };

    private static SemanticProducedEntity Optional(
        string @ref,
        EntityKind kind,
        string subtype,
        string placementRef,
        string cultureRef,
        float chance,
        bool on = false) =>
        new()
        {
            Ref = @ref,
            Kind = kind,
            Subtype = subtype,
            Role = subtype,
            CreateChance = chance,
            PhysicalPlacement = on ? SemanticPlacement.On : SemanticPlacement.Near,
            SemanticPlacement = SemanticPlacement.Near,
            PlacementRef = placementRef,
            InheritCultureFrom = cultureRef,
        };

    private static SemanticProducedEntity HarborAcross(string @ref, string placementRef) =>
        new()
        {
            Ref = @ref,
            Kind = K.Settlement,
            Subtype = "harbor",
            Role = "harbor",
            PhysicalPlacement = SemanticPlacement.Across,
            SemanticPlacement = SemanticPlacement.Near,
            PlacementRef = placementRef,
            InheritCultureFrom = placementRef,
        };

    private static SemanticProducedEntity ScarRegion(string @ref, string cultureRef) =>
        new()
        {
            Ref = @ref,
            Kind = K.Region,
            Subtype = "scar",
            Role = "scar",
            PhysicalPlacement = SemanticPlacement.Near,
            SemanticPlacement = SemanticPlacement.Near,
            PlacementRef = "$target",
            InheritCultureFrom = cultureRef,
            Coverage = new SemanticRegionCoverage
            {
                Mode = CoveragePlacementMode.CreateNew,
                DesiredCellCount = 8,
                MinimumCellCount = 4,
                TransformTerrain = Terrain.Wastes.ToDomainName(),
            },
        };

    private static SemanticGeneratorRelationship R(
        string source,
        string destination,
        Rel kind,
        float strength) =>
        new()
        {
            SourceRef = source,
            DestinationRef = destination,
            Kind = L(kind),
            Strength = strength,
        };
}
