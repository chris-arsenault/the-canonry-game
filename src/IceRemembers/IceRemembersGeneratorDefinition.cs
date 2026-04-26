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
        new()
        {
            Id = "penguin_calls_the_unhuddled",
            Name = "Penguin Calls the Unhuddled",
            Description = "A high-echo penguin survives long enough that the cold-lost gather into a named refuge.",
            Target = new SemanticGeneratorTarget
            {
                Kind = K.Penguin,
            },
            TargetSaturation = new SemanticTargetSaturation
            {
                RelationshipKind = L(Rel.MemberOf),
                RequiredFact = LF(RelFact.CalledUnhuddled),
                Direction = TargetSaturationDirection.OutgoingFromTarget,
                MaxExisting = 0,
            },
            Produces =
            [
                new SemanticProducedEntity
                {
                    Ref = "$refuge",
                    Kind = K.Settlement,
                    Subtypes = new HashSet<string>(StringComparer.Ordinal) { "sanctuary", "rookery" },
                    Role = "unhuddled_refuge",
                    PhysicalPlacement = SemanticPlacement.Near,
                    SemanticPlacement = SemanticPlacement.Near,
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$compact",
                    Kind = K.Faction,
                    Subtypes = new HashSet<string>(StringComparer.Ordinal) { "compact", "cell" },
                    Role = "exile_compact",
                    PhysicalPlacement = SemanticPlacement.Near,
                    SemanticPlacement = SemanticPlacement.Near,
                    PlacementRef = "$refuge",
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$first_follower",
                    Kind = K.Penguin,
                    Subtype = "exile",
                    Role = "first_follower",
                    PhysicalPlacement = SemanticPlacement.Near,
                    SemanticPlacement = SemanticPlacement.Across,
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$third_follower",
                    Kind = K.Penguin,
                    Subtype = "exile",
                    Role = "third_follower",
                    CreateIfMissing = false,
                    Query = new SemanticEntityQuery
                    {
                        AdjacentToTarget = true,
                        WithoutIncomingRelationships = new HashSet<string>(StringComparer.Ordinal)
                        {
                            L(Rel.LedBy),
                        },
                    },
                    InheritCultureFrom = "$target",
                },
            ],
            Relationships =
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$refuge",
                    DestinationRef = "$compact",
                    Kind = L(Rel.ControlledBy),
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$target",
                    DestinationRef = "$compact",
                    Kind = L(Rel.MemberOf),
                    Facts = new HashSet<string>(StringComparer.Ordinal) { LF(RelFact.CalledUnhuddled) },
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$target",
                    DestinationRef = "$refuge",
                    Kind = L(Rel.HomeBase),
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$first_follower",
                    DestinationRef = "$compact",
                    Kind = L(Rel.MemberOf),
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$first_follower",
                    DestinationRef = "$refuge",
                    Kind = L(Rel.HomeBase),
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$third_follower",
                    DestinationRef = "$compact",
                    Kind = L(Rel.MemberOf),
                    ReplaceExistingOutgoingOfSameKind = true,
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$third_follower",
                    DestinationRef = "$refuge",
                    Kind = L(Rel.HomeBase),
                },
            ],
            Triggers =
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = K.Penguin,
                    AttributeName = A(Attr.Echo),
                    Direction = TriggerDirection.High,
                },
            ],
            PulseWeight = 2,
        },
        new()
        {
            Id = "faction_seals_the_edge",
            Name = "Faction Seals the Edge",
            Description = "A cohesive faction sacrifices a holdfast, a warden, and a seal to contain a thin place where corruption leaks through.",
            Target = new SemanticGeneratorTarget
            {
                Kind = K.Faction,
            },
            TargetSaturation = new SemanticTargetSaturation
            {
                RelationshipKind = L(Rel.ControlledBy),
                RequiredFact = LF(RelFact.SealedThinPlace),
                Direction = TargetSaturationDirection.IncomingToTarget,
                MaxExisting = 0,
            },
            SecondaryTargets =
            [
                new SemanticGeneratorBindingTarget
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
            ],
            Produces =
            [
                new SemanticProducedEntity
                {
                    Ref = "$holdfast",
                    Kind = K.Settlement,
                    Subtype = "holdfast",
                    Role = "edge_holdfast",
                    PhysicalPlacement = SemanticPlacement.InRegionLocality,
                    SemanticPlacement = SemanticPlacement.Near,
                    PlacementRef = "$leak_region",
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$warden",
                    Kind = K.Penguin,
                    Subtype = "warden",
                    Role = "edge_warden",
                    PhysicalPlacement = SemanticPlacement.On,
                    SemanticPlacement = SemanticPlacement.Near,
                    PlacementRef = "$holdfast",
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$seal",
                    Kind = K.Artifact,
                    Subtype = "seal",
                    Role = "edge_seal",
                    PhysicalPlacement = SemanticPlacement.On,
                    SemanticPlacement = SemanticPlacement.Near,
                    PlacementRef = "$holdfast",
                    InheritCultureFrom = "$target",
                },
            ],
            Relationships =
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$holdfast",
                    DestinationRef = "$target",
                    Kind = L(Rel.ControlledBy),
                    Facts = new HashSet<string>(StringComparer.Ordinal) { LF(RelFact.SealedThinPlace) },
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$warden",
                    DestinationRef = "$target",
                    Kind = L(Rel.MemberOf),
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$warden",
                    DestinationRef = "$holdfast",
                    Kind = L(Rel.HomeBase),
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$warden",
                    DestinationRef = "$seal",
                    Kind = L(Rel.Holds),
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$seal",
                    DestinationRef = "$holdfast",
                    Kind = L(Rel.Anchored),
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$seal",
                    DestinationRef = "$leak_region",
                    Kind = L(Rel.WardAgainst),
                },
            ],
            Triggers =
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = K.Faction,
                    AttributeName = A(Attr.Cohesion),
                    Direction = TriggerDirection.High,
                },
            ],
            PulseWeight = 3,
        },
        new()
        {
            Id = "region_opens_the_old_holding",
            Name = "Region Opens the Old Holding",
            Description = "A region yields up what it has held too long, naming a relic and its witness into the world.",
            Target = new SemanticGeneratorTarget
            {
                Kind = K.Region,
            },
            TargetSaturation = new SemanticTargetSaturation
            {
                RelationshipKind = L(Rel.NativeTo),
                RequiredFact = LF(RelFact.OpenedOldHolding),
                Direction = TargetSaturationDirection.IncomingToTarget,
                MaxExisting = 0,
            },
            Produces =
            [
                new SemanticProducedEntity
                {
                    Ref = "$relic",
                    Kind = K.Artifact,
                    Subtype = "relic",
                    Role = "old_relic",
                    PhysicalPlacement = SemanticPlacement.InRegionLocality,
                    SemanticPlacement = SemanticPlacement.Across,
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$witness",
                    Kind = K.Penguin,
                    Subtype = "witness",
                    Role = "witness",
                    PhysicalPlacement = SemanticPlacement.Near,
                    SemanticPlacement = SemanticPlacement.Near,
                    PlacementRef = "$relic",
                    InheritCultureFrom = "$target",
                },
            ],
            Relationships =
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$relic",
                    DestinationRef = "$target",
                    Kind = L(Rel.NativeTo),
                    Facts = new HashSet<string>(StringComparer.Ordinal) { LF(RelFact.OpenedOldHolding) },
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$witness",
                    DestinationRef = "$relic",
                    Kind = L(Rel.Holds),
                },
            ],
            Triggers =
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = K.Region,
                    AttributeName = A(Attr.Holding),
                    Direction = TriggerDirection.High,
                },
            ],
            PulseWeight = 1,
        },
        new()
        {
            Id = "artifact_cracks_the_berg",
            Name = "Artifact Cracks the Berg",
            Description = "A resonant artifact breaks the Berg's holding and names a scar-region into being.",
            Target = new SemanticGeneratorTarget
            {
                Kind = K.Artifact,
            },
            TargetSaturation = new SemanticTargetSaturation
            {
                RelationshipKind = L(Rel.CorruptedBy),
                RequiredFact = LF(RelFact.CrackedBerg),
                Direction = TargetSaturationDirection.IncomingToTarget,
                MaxExisting = 0,
            },
            Produces =
            [
                new SemanticProducedEntity
                {
                    Ref = "$scar",
                    Kind = K.Region,
                    Subtype = "scar",
                    Role = "cracked_berg",
                    PhysicalPlacement = SemanticPlacement.Across,
                    SemanticPlacement = SemanticPlacement.Across,
                    InheritCultureFrom = "$target",
                    Coverage = new SemanticRegionCoverage
                    {
                        Mode = CoveragePlacementMode.SplitFromExisting,
                        DesiredCellCount = 12,
                        MinimumCellCount = 8,
                        MinimumSourceRemainingCellCount = 24,
                        TransformTerrain = IceRemembersTerrain.Crags.ToDomainName(),
                    },
                },
                new SemanticProducedEntity
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
                new SemanticProducedEntity
                {
                    Ref = "$splinter",
                    Kind = K.Artifact,
                    Subtype = "relic",
                    Role = "splinter",
                    CreateChance = 0.35f,
                    PhysicalPlacement = SemanticPlacement.Near,
                    SemanticPlacement = SemanticPlacement.Across,
                    PlacementRef = "$scar",
                    InheritCultureFrom = "$target",
                },
            ],
            Relationships =
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$scar",
                    DestinationRef = "$choir",
                    Kind = L(Rel.ControlledBy),
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$target",
                    DestinationRef = "$scar",
                    Kind = L(Rel.Anchored),
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$scar",
                    DestinationRef = "$target",
                    Kind = L(Rel.CorruptedBy),
                    Facts = new HashSet<string>(StringComparer.Ordinal) { LF(RelFact.CrackedBerg) },
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$splinter",
                    DestinationRef = "$target",
                    Kind = L(Rel.PartOf),
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$splinter",
                    DestinationRef = "$scar",
                    Kind = L(Rel.NativeTo),
                },
            ],
            Triggers =
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = K.Artifact,
                    AttributeName = A(Attr.Resonance),
                    Direction = TriggerDirection.High,
                },
            ],
            PulseWeight = 1,
        },
        new()
        {
            Id = "settlement_names_the_office",
            Name = "Warden Bears the Office",
            Description = "A named warden or broker gathers authority around themselves until office hardens into an order and a seal.",
            Target = new SemanticGeneratorTarget
            {
                Kind = K.Penguin,
                Subtypes = new HashSet<string>(StringComparer.Ordinal) { "warden", "broker" },
            },
            TargetSaturation = new SemanticTargetSaturation
            {
                RelationshipKind = L(Rel.MemberOf),
                RequiredFact = LF(RelFact.NamedOffice),
                Direction = TargetSaturationDirection.OutgoingFromTarget,
                MaxExisting = 0,
            },
            Produces =
            [
                new SemanticProducedEntity
                {
                    Ref = "$office",
                    Kind = K.Faction,
                    Subtype = "order",
                    Role = "named_office",
                    PhysicalPlacement = SemanticPlacement.On,
                    SemanticPlacement = SemanticPlacement.Near,
                    PlacementRef = "$target",
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$seal",
                    Kind = K.Artifact,
                    Subtype = "seal",
                    Role = "office_seal",
                    PhysicalPlacement = SemanticPlacement.On,
                    SemanticPlacement = SemanticPlacement.Near,
                    PlacementRef = "$target",
                    InheritCultureFrom = "$target",
                },
            ],
            Relationships =
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$target",
                    DestinationRef = "$office",
                    Kind = L(Rel.MemberOf),
                    Facts = new HashSet<string>(StringComparer.Ordinal) { LF(RelFact.NamedOffice) },
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$target",
                    DestinationRef = "$seal",
                    Kind = L(Rel.Holds),
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$seal",
                    DestinationRef = "$office",
                    Kind = L(Rel.ClaimedBy),
                },
            ],
            Triggers =
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = K.Penguin,
                    AttributeName = A(Attr.Echo),
                    Direction = TriggerDirection.High,
                },
            ],
            PulseWeight = 2,
        },
        new()
        {
            Id = "sea_raises_the_reefhold",
            Name = "Sea Raises the Reefhold",
            Description = "A harbor settlement pushes work and watchfulness out onto dangerous water until a reefhold, sea-name, and working instrument take on lives of their own.",
            Target = new SemanticGeneratorTarget
            {
                Kind = K.Settlement,
                Subtypes = new HashSet<string>(StringComparer.Ordinal) { "harbor" },
            },
            TargetSaturation = new SemanticTargetSaturation
            {
                RelationshipKind = L(Rel.TradeRoute),
                RequiredFact = LF(RelFact.RaisedReefhold),
                Direction = TargetSaturationDirection.IncomingToTarget,
                MaxExisting = 0,
            },
            Produces =
            [
                new SemanticProducedEntity
                {
                    Ref = "$reefhold",
                    Kind = K.Settlement,
                    Subtype = "reefhold",
                    Role = "offshore_hold",
                    PhysicalPlacement = SemanticPlacement.Across,
                    SemanticPlacement = SemanticPlacement.Near,
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$sea",
                    Kind = K.Region,
                    Subtype = "sea",
                    Role = "named_water",
                    PhysicalPlacement = SemanticPlacement.Across,
                    SemanticPlacement = SemanticPlacement.Across,
                    InheritCultureFrom = "$target",
                    Query = new SemanticEntityQuery
                    {
                        Subtypes = new HashSet<string>(StringComparer.Ordinal) { "sea" },
                        AdjacentToTarget = true,
                    },
                    Coverage = new SemanticRegionCoverage
                    {
                        Mode = CoveragePlacementMode.CreateNew,
                        DesiredCellCount = 20,
                        MinimumCellCount = 12,
                        PreferredTerrains = new HashSet<string> { IceRemembersTerrain.Reef.ToDomainName(), IceRemembersTerrain.Ocean.ToDomainName(), IceRemembersTerrain.Shelf.ToDomainName() },
                        AllowedTerrains = new HashSet<string> { IceRemembersTerrain.Reef.ToDomainName(), IceRemembersTerrain.Ocean.ToDomainName(), IceRemembersTerrain.Shelf.ToDomainName() },
                        TransformTerrain = IceRemembersTerrain.Ocean.ToDomainName(),
                    },
                },
                new SemanticProducedEntity
                {
                    Ref = "$cell",
                    Kind = K.Faction,
                    Subtype = "cell",
                    Role = "reef_keepers",
                    CreateChance = 0.5f,
                    PhysicalPlacement = SemanticPlacement.On,
                    SemanticPlacement = SemanticPlacement.Near,
                    PlacementRef = "$reefhold",
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$instrument",
                    Kind = K.Artifact,
                    Subtype = "instrument",
                    Role = "sea_instrument",
                    PhysicalPlacement = SemanticPlacement.On,
                    SemanticPlacement = SemanticPlacement.Near,
                    PlacementRef = "$reefhold",
                    InheritCultureFrom = "$target",
                },
            ],
            Relationships =
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$reefhold",
                    DestinationRef = "$target",
                    Kind = L(Rel.TradeRoute),
                    Facts = new HashSet<string>(StringComparer.Ordinal) { LF(RelFact.RaisedReefhold) },
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$reefhold",
                    DestinationRef = "$cell",
                    Kind = L(Rel.ControlledBy),
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$instrument",
                    DestinationRef = "$reefhold",
                    Kind = L(Rel.Anchored),
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$instrument",
                    DestinationRef = "$sea",
                    Kind = L(Rel.NativeTo),
                },
            ],
            Triggers =
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = K.Settlement,
                    AttributeName = A(Attr.Shelter),
                    Direction = TriggerDirection.High,
                },
            ],
            PulseWeight = 1,
        },
        new()
        {
            Id = "the_shelf_yields_a_tome",
            Name = "The Shelf Yields a Tome",
            Description = "A shelf gives up a named text, and a rookery forms around the act of keeping and reading it.",
            Target = new SemanticGeneratorTarget
            {
                Kind = K.Region,
            },
            TargetSaturation = new SemanticTargetSaturation
            {
                RelationshipKind = L(Rel.FoundedFrom),
                RequiredFact = LF(RelFact.YieldedTome),
                Direction = TargetSaturationDirection.IncomingToTarget,
                MaxExisting = 0,
            },
            Produces =
            [
                new SemanticProducedEntity
                {
                    Ref = "$shelf",
                    Kind = K.Region,
                    Subtype = "shelf",
                    Role = "yielded_shelf",
                    PhysicalPlacement = SemanticPlacement.Across,
                    SemanticPlacement = SemanticPlacement.Across,
                    InheritCultureFrom = "$target",
                    Query = new SemanticEntityQuery
                    {
                        Subtypes = new HashSet<string>(StringComparer.Ordinal) { "shelf" },
                        AdjacentToTarget = true,
                    },
                    Coverage = new SemanticRegionCoverage
                    {
                        Mode = CoveragePlacementMode.CreateNew,
                        DesiredCellCount = 16,
                        MinimumCellCount = 10,
                        PreferredTerrains = new HashSet<string> { IceRemembersTerrain.Shelf.ToDomainName(), IceRemembersTerrain.Reef.ToDomainName(), IceRemembersTerrain.Ice.ToDomainName() },
                        AllowedTerrains = new HashSet<string> { IceRemembersTerrain.Shelf.ToDomainName(), IceRemembersTerrain.Reef.ToDomainName(), IceRemembersTerrain.Ice.ToDomainName() },
                    },
                },
                new SemanticProducedEntity
                {
                    Ref = "$tome",
                    Kind = K.Artifact,
                    Subtype = "tome",
                    Role = "yielded_tome",
                    PhysicalPlacement = SemanticPlacement.On,
                    SemanticPlacement = SemanticPlacement.Near,
                    PlacementRef = "$rookery",
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$rookery",
                    Kind = K.Settlement,
                    Subtype = "rookery",
                    Role = "copying_rookery",
                    PhysicalPlacement = SemanticPlacement.InRegionLocality,
                    SemanticPlacement = SemanticPlacement.Near,
                    PlacementRef = "$shelf",
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$witness",
                    Kind = K.Penguin,
                    Subtype = "witness",
                    Role = "first_reader",
                    CreateChance = 0.5f,
                    PhysicalPlacement = SemanticPlacement.On,
                    SemanticPlacement = SemanticPlacement.Near,
                    PlacementRef = "$rookery",
                    InheritCultureFrom = "$target",
                },
            ],
            Relationships =
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$shelf",
                    DestinationRef = "$target",
                    Kind = L(Rel.FoundedFrom),
                    Facts = new HashSet<string>(StringComparer.Ordinal) { LF(RelFact.YieldedTome) },
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$rookery",
                    DestinationRef = "$shelf",
                    Kind = L(Rel.FoundedFrom),
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$tome",
                    DestinationRef = "$shelf",
                    Kind = L(Rel.NativeTo),
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$tome",
                    DestinationRef = "$rookery",
                    Kind = L(Rel.Anchored),
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$witness",
                    DestinationRef = "$rookery",
                    Kind = L(Rel.HomeBase),
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$witness",
                    DestinationRef = "$tome",
                    Kind = L(Rel.Holds),
                },
            ],
            Triggers =
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = K.Region,
                    AttributeName = A(Attr.Holding),
                    Direction = TriggerDirection.High,
                },
            ],
            PulseWeight = 1,
        },
    ];

    private static string A(Attr attribute) => attribute.ToDomainName();
    private static string L(Rel relationship) => relationship.ToDomainName();
    private static string LF(RelFact fact) => fact.ToDomainName();
}
