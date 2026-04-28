namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Kinds;
using Engine.WorldGen;

using Attr = IceRemembers.IceRemembersAttributeId;
using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;
using RelFact = IceRemembers.IceRemembersRelationshipFact;

public static class IceRemembersGeneratorDefinition
{
    public static IReadOnlyList<SemanticGeneratorConfig> Create() =>
    [
        PenguinCallsTheUnhuddled(),
        FactionSealsTheEdge(),
        RegionOpensTheOldHolding(),
        ArtifactCracksTheBerg(),
        SettlementNamesTheOffice(),
        SeaRaisesTheReefhold(),
        ShelfYieldsATome(),
    ];

    private static SemanticGeneratorConfig PenguinCallsTheUnhuddled() =>
        new()
        {
            Id = "penguin_calls_the_unhuddled",
            Name = "Penguin Calls the Unhuddled",
            Description = "A high-echo penguin survives long enough that the cold-lost gather into a named refuge.",
            Target = Target(K.Penguin),
            TargetSaturation = Saturation(
                Rel.MemberOf,
                RelFact.CalledUnhuddled,
                TargetSaturationDirection.OutgoingFromTarget),
            Produces = PenguinCallsTheUnhuddledProduced(),
            Relationships = PenguinCallsTheUnhuddledRelationships(),
            Triggers = [Trigger(K.Penguin, Attr.Echo)],
            PulseWeight = 2,
        };

    private static IReadOnlyList<SemanticProducedEntity> PenguinCallsTheUnhuddledProduced() =>
    [
        new()
        {
            Ref = "$refuge",
            Kind = K.Settlement,
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { "sanctuary", "rookery" },
            Role = "unhuddled_refuge",
            InheritCultureFrom = "$target",
        },
        new()
        {
            Ref = "$compact",
            Kind = K.Faction,
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { "compact", "cell" },
            Role = "exile_compact",
            PlacementRef = "$refuge",
            InheritCultureFrom = "$target",
        },
        new()
        {
            Ref = "$first_follower",
            Kind = K.Penguin,
            Subtype = "exile",
            Role = "first_follower",
            SemanticPlacement = SemanticPlacement.Across,
            InheritCultureFrom = "$target",
        },
        new()
        {
            Ref = "$third_follower",
            Kind = K.Penguin,
            Subtype = "exile",
            Role = "third_follower",
            CreateIfMissing = false,
            Query = new SemanticEntityQuery
            {
                AdjacentToTarget = true,
                WithoutIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(Rel.LedBy) },
            },
            InheritCultureFrom = "$target",
        },
    ];

    private static IReadOnlyList<SemanticGeneratorRelationship> PenguinCallsTheUnhuddledRelationships() =>
    [
        Relationship("$refuge", "$compact", Rel.ControlledBy),
        RelationshipWithFact("$target", "$compact", Rel.MemberOf, RelFact.CalledUnhuddled),
        Relationship("$target", "$refuge", Rel.HomeBase),
        Relationship("$first_follower", "$compact", Rel.MemberOf),
        Relationship("$first_follower", "$refuge", Rel.HomeBase),
        ReplaceRelationship("$third_follower", "$compact", Rel.MemberOf),
        Relationship("$third_follower", "$refuge", Rel.HomeBase),
    ];

    private static SemanticGeneratorConfig FactionSealsTheEdge() =>
        new()
        {
            Id = "faction_seals_the_edge",
            Name = "Faction Seals the Edge",
            Description = "A cohesive faction sacrifices a holdfast, a warden, and a seal to contain a thin place where corruption leaks through.",
            Target = Target(K.Faction),
            TargetSaturation = Saturation(
                Rel.ControlledBy,
                RelFact.SealedThinPlace,
                TargetSaturationDirection.IncomingToTarget),
            SecondaryTargets = FactionSealsTheEdgeTargets(),
            Produces = FactionSealsTheEdgeProduced(),
            Relationships = FactionSealsTheEdgeRelationships(),
            Triggers = [Trigger(K.Faction, Attr.Cohesion)],
            PulseWeight = 3,
        };

    private static IReadOnlyList<SemanticGeneratorBindingTarget> FactionSealsTheEdgeTargets() =>
    [
        new()
        {
            Ref = "$leak_region",
            Kind = K.Region,
            Query = new SemanticEntityQuery
            {
                AttributeConditions =
                [
                    new SemanticQueryAttributeCondition
                    {
                        AttributeName = A(Attr.Pristiness),
                        Direction = TriggerDirection.Low,
                    },
                ],
            },
        },
    ];

    private static IReadOnlyList<SemanticProducedEntity> FactionSealsTheEdgeProduced() =>
    [
        new()
        {
            Ref = "$holdfast",
            Kind = K.Settlement,
            Subtype = "holdfast",
            Role = "edge_holdfast",
            PhysicalPlacement = SemanticPlacement.InRegionLocality,
            PlacementRef = "$leak_region",
            InheritCultureFrom = "$target",
        },
        new()
        {
            Ref = "$warden",
            Kind = K.Penguin,
            Subtype = "warden",
            Role = "edge_warden",
            PhysicalPlacement = SemanticPlacement.On,
            PlacementRef = "$holdfast",
            InheritCultureFrom = "$target",
        },
        new()
        {
            Ref = "$seal",
            Kind = K.Artifact,
            Subtype = "seal",
            Role = "edge_seal",
            PhysicalPlacement = SemanticPlacement.On,
            PlacementRef = "$holdfast",
            InheritCultureFrom = "$target",
        },
    ];

    private static IReadOnlyList<SemanticGeneratorRelationship> FactionSealsTheEdgeRelationships() =>
    [
        RelationshipWithFact("$holdfast", "$target", Rel.ControlledBy, RelFact.SealedThinPlace),
        Relationship("$warden", "$target", Rel.MemberOf),
        Relationship("$warden", "$holdfast", Rel.HomeBase),
        Relationship("$warden", "$seal", Rel.Holds),
        Relationship("$seal", "$holdfast", Rel.Anchored),
        Relationship("$seal", "$leak_region", Rel.WardAgainst),
    ];

    private static SemanticGeneratorConfig RegionOpensTheOldHolding() =>
        new()
        {
            Id = "region_opens_the_old_holding",
            Name = "Region Opens the Old Holding",
            Description = "A region yields up what it has held too long, naming a relic and its witness into the world.",
            Target = Target(K.Region),
            TargetSaturation = Saturation(
                Rel.NativeTo,
                RelFact.OpenedOldHolding,
                TargetSaturationDirection.IncomingToTarget),
            Produces = RegionOpensTheOldHoldingProduced(),
            Relationships = RegionOpensTheOldHoldingRelationships(),
            Triggers = [Trigger(K.Region, Attr.Holding)],
        };

    private static IReadOnlyList<SemanticProducedEntity> RegionOpensTheOldHoldingProduced() =>
    [
        new()
        {
            Ref = "$relic",
            Kind = K.Artifact,
            Subtype = "relic",
            Role = "old_relic",
            PhysicalPlacement = SemanticPlacement.InRegionLocality,
            SemanticPlacement = SemanticPlacement.Across,
            InheritCultureFrom = "$target",
        },
        new()
        {
            Ref = "$witness",
            Kind = K.Penguin,
            Subtype = "witness",
            Role = "witness",
            PlacementRef = "$relic",
            InheritCultureFrom = "$target",
        },
    ];

    private static IReadOnlyList<SemanticGeneratorRelationship> RegionOpensTheOldHoldingRelationships() =>
    [
        RelationshipWithFact("$relic", "$target", Rel.NativeTo, RelFact.OpenedOldHolding),
        Relationship("$witness", "$relic", Rel.Holds),
    ];

    private static SemanticGeneratorConfig ArtifactCracksTheBerg() =>
        new()
        {
            Id = "artifact_cracks_the_berg",
            Name = "Artifact Cracks the Berg",
            Description = "A resonant artifact breaks the Berg's holding and names a scar-region into being.",
            Target = Target(K.Artifact),
            TargetSaturation = Saturation(
                Rel.CorruptedBy,
                RelFact.CrackedBerg,
                TargetSaturationDirection.IncomingToTarget),
            Produces = ArtifactCracksTheBergProduced(),
            Relationships = ArtifactCracksTheBergRelationships(),
            Triggers = [Trigger(K.Artifact, Attr.Resonance)],
        };

    private static IReadOnlyList<SemanticProducedEntity> ArtifactCracksTheBergProduced() =>
    [
        new()
        {
            Ref = "$scar",
            Kind = K.Region,
            Subtype = "scar",
            Role = "cracked_berg",
            PhysicalPlacement = SemanticPlacement.Across,
            SemanticPlacement = SemanticPlacement.Across,
            InheritCultureFrom = "$target",
            Coverage = CrackedBergCoverage(),
        },
        new()
        {
            Ref = "$choir",
            Kind = K.Faction,
            Subtype = "choir",
            Role = "scar_choir",
            PhysicalPlacement = SemanticPlacement.Near,
            SemanticPlacement = SemanticPlacement.Across,
            PlacementRef = "$scar",
            InheritCultureFrom = "$target",
        },
        new()
        {
            Ref = "$splinter",
            Kind = K.Artifact,
            Subtype = "relic",
            Role = "splinter",
            CreateChance = 0.35f,
            SemanticPlacement = SemanticPlacement.Across,
            PlacementRef = "$scar",
            InheritCultureFrom = "$target",
        },
    ];

    private static SemanticRegionCoverage CrackedBergCoverage() =>
        new()
        {
            Mode = CoveragePlacementMode.SplitFromExisting,
            DesiredCellCount = 12,
            MinimumCellCount = 8,
            MinimumSourceRemainingCellCount = 24,
            TransformTerrain = IceRemembersTerrain.Crags.ToDomainName(),
        };

    private static IReadOnlyList<SemanticGeneratorRelationship> ArtifactCracksTheBergRelationships() =>
    [
        Relationship("$scar", "$choir", Rel.ControlledBy),
        Relationship("$target", "$scar", Rel.Anchored),
        RelationshipWithFact("$scar", "$target", Rel.CorruptedBy, RelFact.CrackedBerg),
        Relationship("$splinter", "$target", Rel.PartOf),
        Relationship("$splinter", "$scar", Rel.NativeTo),
    ];

    private static SemanticGeneratorConfig SettlementNamesTheOffice() =>
        new()
        {
            Id = "settlement_names_the_office",
            Name = "Warden Bears the Office",
            Description = "A named warden or broker gathers authority around themselves until office hardens into an order and a seal.",
            Target = Target(K.Penguin, new HashSet<string>(StringComparer.Ordinal) { "warden", "broker" }),
            TargetSaturation = Saturation(
                Rel.MemberOf,
                RelFact.NamedOffice,
                TargetSaturationDirection.OutgoingFromTarget),
            Produces = SettlementNamesTheOfficeProduced(),
            Relationships = SettlementNamesTheOfficeRelationships(),
            Triggers = [Trigger(K.Penguin, Attr.Echo)],
            PulseWeight = 2,
        };

    private static IReadOnlyList<SemanticProducedEntity> SettlementNamesTheOfficeProduced() =>
    [
        new()
        {
            Ref = "$office",
            Kind = K.Faction,
            Subtype = "order",
            Role = "named_office",
            PhysicalPlacement = SemanticPlacement.On,
            PlacementRef = "$target",
            InheritCultureFrom = "$target",
        },
        new()
        {
            Ref = "$seal",
            Kind = K.Artifact,
            Subtype = "seal",
            Role = "office_seal",
            PhysicalPlacement = SemanticPlacement.On,
            PlacementRef = "$target",
            InheritCultureFrom = "$target",
        },
    ];

    private static IReadOnlyList<SemanticGeneratorRelationship> SettlementNamesTheOfficeRelationships() =>
    [
        RelationshipWithFact("$target", "$office", Rel.MemberOf, RelFact.NamedOffice),
        Relationship("$target", "$seal", Rel.Holds),
        Relationship("$seal", "$office", Rel.ClaimedBy),
    ];

    private static SemanticGeneratorConfig SeaRaisesTheReefhold() =>
        new()
        {
            Id = "sea_raises_the_reefhold",
            Name = "Sea Raises the Reefhold",
            Description = "A harbor settlement pushes work and watchfulness out onto dangerous water until a reefhold, sea-name, and working instrument take on lives of their own.",
            Target = Target(K.Settlement, new HashSet<string>(StringComparer.Ordinal) { "harbor" }),
            TargetSaturation = Saturation(
                Rel.TradeRoute,
                RelFact.RaisedReefhold,
                TargetSaturationDirection.IncomingToTarget),
            Produces = SeaRaisesTheReefholdProduced(),
            Relationships = SeaRaisesTheReefholdRelationships(),
            Triggers = [Trigger(K.Settlement, Attr.Shelter)],
        };

    private static IReadOnlyList<SemanticProducedEntity> SeaRaisesTheReefholdProduced() =>
    [
        new()
        {
            Ref = "$reefhold",
            Kind = K.Settlement,
            Subtype = "reefhold",
            Role = "offshore_hold",
            PhysicalPlacement = SemanticPlacement.Across,
            InheritCultureFrom = "$target",
        },
        new()
        {
            Ref = "$sea",
            Kind = K.Region,
            Subtype = "sea",
            Role = "named_water",
            PhysicalPlacement = SemanticPlacement.Across,
            SemanticPlacement = SemanticPlacement.Across,
            InheritCultureFrom = "$target",
            Query = SeaRegionQuery(),
            Coverage = SeaRegionCoverage(),
        },
        new()
        {
            Ref = "$cell",
            Kind = K.Faction,
            Subtype = "cell",
            Role = "reef_keepers",
            CreateChance = 0.5f,
            PhysicalPlacement = SemanticPlacement.On,
            PlacementRef = "$reefhold",
            InheritCultureFrom = "$target",
        },
        new()
        {
            Ref = "$instrument",
            Kind = K.Artifact,
            Subtype = "instrument",
            Role = "sea_instrument",
            PhysicalPlacement = SemanticPlacement.On,
            PlacementRef = "$reefhold",
            InheritCultureFrom = "$target",
        },
    ];

    private static SemanticEntityQuery SeaRegionQuery() =>
        new()
        {
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { "sea" },
            AdjacentToTarget = true,
        };

    private static SemanticRegionCoverage SeaRegionCoverage() =>
        new()
        {
            Mode = CoveragePlacementMode.CreateNew,
            DesiredCellCount = 20,
            MinimumCellCount = 12,
            PreferredTerrains = ReefOceanShelfTerrains(),
            AllowedTerrains = ReefOceanShelfTerrains(),
            TransformTerrain = IceRemembersTerrain.Ocean.ToDomainName(),
        };

    private static IReadOnlyList<SemanticGeneratorRelationship> SeaRaisesTheReefholdRelationships() =>
    [
        RelationshipWithFact("$reefhold", "$target", Rel.TradeRoute, RelFact.RaisedReefhold),
        Relationship("$reefhold", "$cell", Rel.ControlledBy),
        Relationship("$instrument", "$reefhold", Rel.Anchored),
        Relationship("$instrument", "$sea", Rel.NativeTo),
    ];

    private static SemanticGeneratorConfig ShelfYieldsATome() =>
        new()
        {
            Id = "the_shelf_yields_a_tome",
            Name = "The Shelf Yields a Tome",
            Description = "A shelf gives up a named text, and a rookery forms around the act of keeping and reading it.",
            Target = Target(K.Region),
            TargetSaturation = Saturation(
                Rel.FoundedFrom,
                RelFact.YieldedTome,
                TargetSaturationDirection.IncomingToTarget),
            Produces = ShelfYieldsATomeProduced(),
            Relationships = ShelfYieldsATomeRelationships(),
            Triggers = [Trigger(K.Region, Attr.Holding)],
        };

    private static IReadOnlyList<SemanticProducedEntity> ShelfYieldsATomeProduced() =>
    [
        new()
        {
            Ref = "$shelf",
            Kind = K.Region,
            Subtype = "shelf",
            Role = "yielded_shelf",
            PhysicalPlacement = SemanticPlacement.Across,
            SemanticPlacement = SemanticPlacement.Across,
            InheritCultureFrom = "$target",
            Query = ShelfRegionQuery(),
            Coverage = ShelfRegionCoverage(),
        },
        new()
        {
            Ref = "$tome",
            Kind = K.Artifact,
            Subtype = "tome",
            Role = "yielded_tome",
            PhysicalPlacement = SemanticPlacement.On,
            PlacementRef = "$rookery",
            InheritCultureFrom = "$target",
        },
        new()
        {
            Ref = "$rookery",
            Kind = K.Settlement,
            Subtype = "rookery",
            Role = "copying_rookery",
            PhysicalPlacement = SemanticPlacement.InRegionLocality,
            PlacementRef = "$shelf",
            InheritCultureFrom = "$target",
        },
        new()
        {
            Ref = "$witness",
            Kind = K.Penguin,
            Subtype = "witness",
            Role = "first_reader",
            CreateChance = 0.5f,
            PhysicalPlacement = SemanticPlacement.On,
            PlacementRef = "$rookery",
            InheritCultureFrom = "$target",
        },
    ];

    private static SemanticEntityQuery ShelfRegionQuery() =>
        new()
        {
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { "shelf" },
            AdjacentToTarget = true,
        };

    private static SemanticRegionCoverage ShelfRegionCoverage() =>
        new()
        {
            Mode = CoveragePlacementMode.CreateNew,
            DesiredCellCount = 16,
            MinimumCellCount = 10,
            PreferredTerrains = ShelfReefIceTerrains(),
            AllowedTerrains = ShelfReefIceTerrains(),
        };

    private static IReadOnlyList<SemanticGeneratorRelationship> ShelfYieldsATomeRelationships() =>
    [
        RelationshipWithFact("$shelf", "$target", Rel.FoundedFrom, RelFact.YieldedTome),
        Relationship("$rookery", "$shelf", Rel.FoundedFrom),
        Relationship("$tome", "$shelf", Rel.NativeTo),
        Relationship("$tome", "$rookery", Rel.Anchored),
        Relationship("$witness", "$rookery", Rel.HomeBase),
        Relationship("$witness", "$tome", Rel.Holds),
    ];

    private static HashSet<string> ReefOceanShelfTerrains() =>
        new(StringComparer.Ordinal)
        {
            IceRemembersTerrain.Reef.ToDomainName(),
            IceRemembersTerrain.Ocean.ToDomainName(),
            IceRemembersTerrain.Shelf.ToDomainName(),
        };

    private static HashSet<string> ShelfReefIceTerrains() =>
        new(StringComparer.Ordinal)
        {
            IceRemembersTerrain.Shelf.ToDomainName(),
            IceRemembersTerrain.Reef.ToDomainName(),
            IceRemembersTerrain.Ice.ToDomainName(),
        };

    private static SemanticGeneratorTarget Target(EntityKind kind) =>
        new()
        {
            Kind = kind,
        };

    private static SemanticGeneratorTarget Target(EntityKind kind, IReadOnlySet<string> subtypes) =>
        new()
        {
            Kind = kind,
            Subtypes = subtypes,
        };

    private static SemanticTargetSaturation Saturation(
        Rel relationship,
        RelFact fact,
        TargetSaturationDirection direction) =>
        new()
        {
            RelationshipKind = L(relationship),
            RequiredFact = LF(fact),
            Direction = direction,
            MaxExisting = 0,
        };

    private static SemanticGeneratorTrigger Trigger(
        EntityKind kind,
        Attr attribute,
        TriggerDirection direction = TriggerDirection.High) =>
        new()
        {
            OnKind = kind,
            AttributeName = A(attribute),
            Direction = direction,
        };

    private static SemanticGeneratorRelationship Relationship(
        string sourceRef,
        string destinationRef,
        Rel relationship) =>
        new()
        {
            SourceRef = sourceRef,
            DestinationRef = destinationRef,
            Kind = L(relationship),
        };

    private static SemanticGeneratorRelationship RelationshipWithFact(
        string sourceRef,
        string destinationRef,
        Rel relationship,
        RelFact fact) =>
        new()
        {
            SourceRef = sourceRef,
            DestinationRef = destinationRef,
            Kind = L(relationship),
            Facts = new HashSet<string>(StringComparer.Ordinal) { LF(fact) },
        };

    private static SemanticGeneratorRelationship ReplaceRelationship(
        string sourceRef,
        string destinationRef,
        Rel relationship) =>
        new()
        {
            SourceRef = sourceRef,
            DestinationRef = destinationRef,
            Kind = L(relationship),
            ReplaceExistingOutgoingOfSameKind = true,
        };

    private static string A(Attr attribute) => attribute.ToDomainName();
    private static string L(Rel relationship) => relationship.ToDomainName();
    private static string LF(RelFact fact) => fact.ToDomainName();
}
