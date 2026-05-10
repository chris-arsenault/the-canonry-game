namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Kinds;
using Engine.WorldGen;

using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;
using Terrain = IceRemembers.IceRemembersTerrain;

public static class IceRemembersGeneratorDefinition
{
    private const float RoutesDrawReadersMinimumTradeRouteStrength = 0.70f;
    private const float RoutesDrawReadersTradeRouteStrengthSpend = -0.30f;
    private const int WarBreaksTheLandWasteRadius = 5;
    private const int WarBreaksTheLandMaximumLocalWastes = 8;

    public static IReadOnlyList<SemanticGeneratorConfig> Create() =>
    [
        IceSettlesAccounts(),
        WhatTheSpringRemembers(),
        WarBreaksTheLand(),
        RoutesDrawReaders(),
        WhatTheArtifactKnows(),
    ];

    private static string L(Rel relationship) => relationship.ToDomainName();

    private static SemanticGeneratorConfig IceSettlesAccounts() => new()
    {
        Id = "ice_settles_accounts",
        Name = "Ice Settles Accounts",
        Description = "The reckoning. The ice calls a price for the war: a penguin of the faction's culture does not return, a memorial seal closes the account, and the war ends.",
        CausalLimit = "Depends on an at-war relationship and an offered penguin; marks the penguin historical and removes the selected at-war relationship.",
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
        Produces =
        [
            Required("$seal", K.Artifact, "seal", "$target", "$target", on: true),
            Required("$witness", K.Penguin, "witness", "$target", "$target"),
            Optional("$exile", K.Penguin, "exile", "$target", "$target", chance: 0.40f),
        ],
        Effects =
        [
            new SetBindingHistoricalEffect("$offered"),
            new RemoveTargetRelationshipsEffect(
                L(Rel.AtWarWith),
                GeneratorRelationshipInstanceSelection.TriggerRelationship,
                includeReciprocal: true),
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
        Description = "A cleansed hot spring draws a witness and a memorial relic. Where the warmth holds long enough, a sanctuary may form, and a choir may settle to sing what the ice forgot.",
        CausalLimit = "Depends on an incoming cleansed-by relationship and absence of memorialization; creates an anchored memorial relic on the region.",
        Target = TargetWithIncomingAndWithoutIncoming(K.Region, Rel.CleansedBy, Rel.Memorializes),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget { Ref = "$patron", Kind = K.Faction },
        ],
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
            R("$relic", "$target", Rel.Memorializes, 0.90f),
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
        Description = "Sustained war scars the outer face into wastes. Survivors sometimes form a compact in the ruin; weapons are left where the fighting cracked.",
        CausalLimit = "Depends on an at-war relationship and fewer than eight local waste cells; creates a scar region that turns finite nearby land into wastes.",
        Target = TargetWithOutgoingAndLocalTerrainBelow(
            K.Faction,
            Rel.AtWarWith,
            Terrain.Wastes,
            WarBreaksTheLandWasteRadius,
            WarBreaksTheLandMaximumLocalWastes),
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
        Description = "An active trade route attracts a broker and a waystation harbor. Where the wealth concentrates, a formal order may take charge of what passes through.",
        CausalLimit = "Depends on high trade-route strength and reduces the selected trade route strength.",
        Target = TargetWithStrongOutgoing(K.Settlement, Rel.TradeRoute, RoutesDrawReadersMinimumTradeRouteStrength),
        Produces =
        [
            Required("$broker", K.Penguin, "broker", "$target", "$target"),
            HarborAcross("$harbor", "$target"),
            Optional("$order", K.Faction, "order", "$harbor", "$target", chance: 0.40f),
            Optional("$instrument", K.Artifact, "instrument", "$harbor", "$target", chance: 0.45f, on: true),
        ],
        Relationships =
        [
            R("$broker", "$harbor", Rel.HomeBase, 1f),
            R("$harbor", "$target", Rel.FoundedFrom, 0.90f),
            R("$harbor", "$target", Rel.TradeRoute, IceRemembersRelationshipStrengths.InitialTradeRoute),
            R("$harbor", "$order", Rel.ControlledBy, 0.82f),
            R("$broker", "$order", Rel.MemberOf, 0.80f),
            R("$instrument", "$harbor", Rel.NativeTo, 0.82f),
        ],
        Effects =
        [
            new AdjustTargetRelationshipStrengthEffect(
                L(Rel.TradeRoute),
                RoutesDrawReadersTradeRouteStrengthSpend,
                GeneratorRelationshipInstanceSelection.TriggerRelationship,
                includeReciprocal: true),
        ],
        Triggers = TriggerOn(K.Settlement),
    };

    private static SemanticGeneratorConfig WhatTheArtifactKnows() => new()
    {
        Id = "what_the_artifact_knows",
        Name = "What the Artifact Knows",
        Description = "A penguin who has carried an artifact long enough recognises a place to stop. They put down a rookery, seal the artifact into the ground, and a small cell may form around them.",
        CausalLimit = "Depends on a held artifact and absence of a home base; creates a home-base relationship for the penguin.",
        Target = TargetWithOutgoingAndWithoutOutgoing(K.Penguin, Rel.Holds, Rel.HomeBase),
        Produces =
        [
            Required("$rookery", K.Settlement, "rookery", "$target", "$target"),
            Required("$seal", K.Artifact, "seal", "$rookery", "$target", on: true),
            Optional("$cell", K.Faction, "cell", "$target", "$target", chance: 0.45f),
        ],
        Relationships =
        [
            R("$target", "$rookery", Rel.HomeBase, 1f),
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

    private static SemanticGeneratorTarget TargetWithStrongOutgoing(
        EntityKind kind,
        Rel required,
        float minimumStrength) =>
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
        };

    private static SemanticGeneratorTarget TargetWithOutgoingAndLocalTerrainBelow(
        EntityKind kind,
        Rel required,
        Terrain terrain,
        int radius,
        int maximumExclusive) =>
        new()
        {
            Kind = kind,
            RequiredOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(required) },
            TerrainCountRequirements =
            [
                new SemanticGeneratorTerrainCountRequirement
                {
                    TerrainName = terrain.ToDomainName(),
                    Radius = radius,
                    MaximumExclusive = maximumExclusive,
                },
            ],
        };

    private static SemanticGeneratorTarget TargetWithOutgoingAndWithoutOutgoing(
        EntityKind kind,
        Rel required,
        Rel without) =>
        new()
        {
            Kind = kind,
            RequiredOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(required) },
            WithoutOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(without) },
        };

    private static SemanticGeneratorTarget TargetWithIncomingAndWithoutIncoming(
        EntityKind kind,
        Rel required,
        Rel without) =>
        new()
        {
            Kind = kind,
            RequiredIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(required) },
            WithoutIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(without) },
        };

    private static IReadOnlyList<SemanticGeneratorTrigger> TriggerOn(EntityKind kind) =>
    [
        new() { OnKind = kind },
    ];

    private static SemanticProducedEntity Required(
        string @ref,
        EntityKind kind,
        string subtype,
        string placementRef,
        string cultureRef,
        bool on = false)
    {
        var physicalProfile = IceRemembersPlacementTerrainProfiles.Physical(kind, subtype);
        var semanticProfile = IceRemembersPlacementTerrainProfiles.Semantic(kind, subtype);
        return new()
        {
            Ref = @ref,
            Kind = kind,
            Subtype = subtype,
            Role = subtype,
            PhysicalPlacement = on ? SemanticPlacement.On : SemanticPlacement.Near,
            PhysicalPlacementPreferredTerrains = physicalProfile.PreferredTerrains,
            PhysicalPlacementAvoidedTerrains = physicalProfile.AvoidedTerrains,
            PhysicalPlacementFallbackTerrain = physicalProfile.FallbackTerrain,
            SemanticPlacement = SemanticPlacement.Near,
            SemanticPlacementPreferredTerrains = semanticProfile.PreferredTerrains,
            SemanticPlacementAvoidedTerrains = semanticProfile.AvoidedTerrains,
            SemanticPlacementFallbackTerrain = semanticProfile.FallbackTerrain,
            PlacementRef = placementRef,
            InheritCultureFrom = cultureRef,
        };
    }

    private static SemanticProducedEntity Optional(
        string @ref,
        EntityKind kind,
        string subtype,
        string placementRef,
        string cultureRef,
        float chance,
        bool on = false)
    {
        var physicalProfile = IceRemembersPlacementTerrainProfiles.Physical(kind, subtype);
        var semanticProfile = IceRemembersPlacementTerrainProfiles.Semantic(kind, subtype);
        return new()
        {
            Ref = @ref,
            Kind = kind,
            Subtype = subtype,
            Role = subtype,
            CreateChance = chance,
            PhysicalPlacement = on ? SemanticPlacement.On : SemanticPlacement.Near,
            PhysicalPlacementPreferredTerrains = physicalProfile.PreferredTerrains,
            PhysicalPlacementAvoidedTerrains = physicalProfile.AvoidedTerrains,
            PhysicalPlacementFallbackTerrain = physicalProfile.FallbackTerrain,
            SemanticPlacement = SemanticPlacement.Near,
            SemanticPlacementPreferredTerrains = semanticProfile.PreferredTerrains,
            SemanticPlacementAvoidedTerrains = semanticProfile.AvoidedTerrains,
            SemanticPlacementFallbackTerrain = semanticProfile.FallbackTerrain,
            PlacementRef = placementRef,
            InheritCultureFrom = cultureRef,
        };
    }

    private static SemanticProducedEntity HarborAcross(string @ref, string placementRef)
    {
        var physicalProfile = IceRemembersPlacementTerrainProfiles.Physical(K.Settlement, "harbor");
        var semanticProfile = IceRemembersPlacementTerrainProfiles.Semantic(K.Settlement, "harbor");
        return new()
        {
            Ref = @ref,
            Kind = K.Settlement,
            Subtype = "harbor",
            Role = "harbor",
            PhysicalPlacement = SemanticPlacement.Across,
            PhysicalPlacementPreferredTerrains = physicalProfile.PreferredTerrains,
            PhysicalPlacementAvoidedTerrains = physicalProfile.AvoidedTerrains,
            PhysicalPlacementFallbackTerrain = physicalProfile.FallbackTerrain,
            SemanticPlacement = SemanticPlacement.Near,
            SemanticPlacementPreferredTerrains = semanticProfile.PreferredTerrains,
            SemanticPlacementAvoidedTerrains = semanticProfile.AvoidedTerrains,
            SemanticPlacementFallbackTerrain = semanticProfile.FallbackTerrain,
            PlacementRef = placementRef,
            InheritCultureFrom = placementRef,
        };
    }

    private static SemanticProducedEntity ScarRegion(string @ref, string cultureRef)
    {
        var physicalProfile = IceRemembersPlacementTerrainProfiles.Physical(K.Region, "scar");
        var semanticProfile = IceRemembersPlacementTerrainProfiles.Semantic(K.Region, "scar");
        return new()
        {
            Ref = @ref,
            Kind = K.Region,
            Subtype = "scar",
            Role = "scar",
            PhysicalPlacement = SemanticPlacement.Near,
            PhysicalPlacementPreferredTerrains = physicalProfile.PreferredTerrains,
            PhysicalPlacementAvoidedTerrains = physicalProfile.AvoidedTerrains,
            PhysicalPlacementFallbackTerrain = physicalProfile.FallbackTerrain,
            SemanticPlacement = SemanticPlacement.Near,
            SemanticPlacementPreferredTerrains = semanticProfile.PreferredTerrains,
            SemanticPlacementAvoidedTerrains = semanticProfile.AvoidedTerrains,
            SemanticPlacementFallbackTerrain = semanticProfile.FallbackTerrain,
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
    }

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
