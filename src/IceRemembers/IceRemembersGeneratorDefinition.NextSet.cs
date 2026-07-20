namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Kinds;
using Engine.WorldGen;

using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;

public static partial class IceRemembersGeneratorDefinition
{
    private const float CleansedSpringsFoundSanctuariesMinimumCleansingStrength = 0.70f;
    private const float WardedMonstersFoundOrdersMinimumWardStrength = 0.70f;

    private static SemanticGeneratorConfig CleansedSpringsFoundSanctuaries() => new()
    {
        Id = "cleansed_springs_found_sanctuaries",
        Name = "Cleansed Springs Found Sanctuaries",
        Description = "A cleansing that holds long enough stops being only recovery. A sanctuary, choir, and rite settle around the warmed scar, making the repair politically durable.",
        CausalLimit = "Depends on strong incoming cleansed-by topology and no founded settlement on the region; creates incoming founded-from as the repeat lock.",
        Target = TargetWithStrongIncomingAndWithoutIncoming(
            K.Region,
            Rel.CleansedBy,
            CleansedSpringsFoundSanctuariesMinimumCleansingStrength,
            Rel.FoundedFrom),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$patron",
                Kind = K.Faction,
                Query = SameCultureAsTarget(samePhysicalComponent: true),
            },
        ],
        Produces =
        [
            Required("$sanctuary", K.Settlement, "sanctuary", "$target", "$patron"),
            Required("$choir", K.Faction, "choir", "$sanctuary", "$patron"),
            Required("$rite", K.Ability, "rite", "$target", "$patron"),
        ],
        Relationships =
        [
            R("$sanctuary", "$target", Rel.FoundedFrom, 0.90f),
            R("$sanctuary", "$choir", Rel.ControlledBy, 0.82f),
            R("$choir", "$sanctuary", Rel.HomeBase, 1f),
            R("$choir", "$patron", Rel.AlliedWith, 0.72f),
            R("$rite", "$target", Rel.Anchored, 0.86f),
            R("$rite", "$sanctuary", Rel.Memorializes, 0.82f),
            R("$rite", "$choir", Rel.PracticedBy, 0.78f),
        ],
        Triggers = TriggerOn(K.Region),
        PulseWeight = 8,
    };

    private static SemanticGeneratorConfig SanctuariesAnchorColonies() => new()
    {
        Id = "sanctuaries_anchor_colonies",
        Name = "Sanctuaries Anchor Colonies",
        Description = "A sanctuary with civic authority becomes a population anchor. A colony grows from it, and an order launches a chartered transport for the route between refuge and settlement.",
        CausalLimit = "Depends on a controlled sanctuary with no daughter settlement; creates incoming founded-from from the colony to the sanctuary.",
        Target = TargetSubtypeWithOutgoingAndWithoutIncoming(K.Settlement, "sanctuary", Rel.ControlledBy, Rel.FoundedFrom),
        Produces =
        [
            // The colony is the outward settler - it pushes the culture's
            // idea-frontier. Its order and charter stay with it (near the colony).
            Required("$colony", K.Settlement, "colony", "$target", "$target", frontier: true),
            Required("$order", K.Faction, "order", "$colony", "$target"),
            Required("$charter", K.Transport, "trade", "$colony", "$target"),
        ],
        Relationships =
        [
            R("$colony", "$target", Rel.FoundedFrom, 0.90f),
            R("$colony", "$order", Rel.ControlledBy, 0.82f),
            R("$order", "$colony", Rel.HomeBase, 1f),
            R("$order", "$target", Rel.AlliedWith, 0.72f),
            R("$colony", "$target", Rel.TradeRoute, IceRemembersRelationshipStrengths.InitialTradeRoute),
            R("$target", "$colony", Rel.TradeRoute, IceRemembersRelationshipStrengths.InitialTradeRoute),
            R("$order", "$charter", Rel.Holds, 1f),
            R("$charter", "$colony", Rel.NativeTo, 0.84f),
            R("$charter", "$target", Rel.ConduitOf, 0.72f),
        ],
        Triggers = TriggerOn(K.Settlement),
    };

    private static SemanticGeneratorConfig ConduitPointsFoundReefholds() => new()
    {
        Id = "conduit_points_found_reefholds",
        Name = "Conduit Points Found Reefholds",
        Description = "A named place in the conduit graph becomes a workable offshore settlement. The route gains a reefhold, a port bond, and an instrument that keeps the passage legible.",
        CausalLimit = "Depends on incoming conduit topology and no founded settlement at the point; creates incoming founded-from from the reefhold.",
        Target = TargetSubtypesWithIncomingAndWithoutIncoming(
            K.PointOfInterest,
            ["vent", "peak", "bloom", "shoal"],
            Rel.ConduitOf,
            Rel.FoundedFrom),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$port",
                Kind = K.Settlement,
                Query = SameCultureAsTarget(samePhysicalComponent: true),
            },
        ],
        Produces =
        [
            Required("$reefhold", K.Settlement, "reefhold", "$target", "$port", frontier: true),
            Required("$instrument", K.Artifact, "instrument", "$reefhold", "$port"),
            Required("$broker", K.Penguin, "broker", "$reefhold", "$port"),
        ],
        Relationships =
        [
            R("$reefhold", "$target", Rel.FoundedFrom, 0.88f),
            R("$reefhold", "$port", Rel.TradeRoute, IceRemembersRelationshipStrengths.InitialTradeRoute),
            R("$port", "$reefhold", Rel.TradeRoute, IceRemembersRelationshipStrengths.InitialTradeRoute),
            R("$instrument", "$reefhold", Rel.Anchored, 0.82f),
            R("$instrument", "$target", Rel.ConduitOf, 0.78f),
            R("$broker", "$reefhold", Rel.HomeBase, 1f),
            R("$broker", "$instrument", Rel.Holds, 1f),
        ],
        Triggers = TriggerOn(K.PointOfInterest),
        PulseWeight = 3,
    };

    private static SemanticGeneratorConfig DeepConduitsNameDepths() => new()
    {
        Id = "deep_conduits_name_depths",
        Name = "Deep Conduits Name Depths",
        Description = "Once a conduit point has founded a reefhold, the passage below it can no longer remain implied. A depths region and a seal enter the record.",
        CausalLimit = "Depends on incoming conduit topology and an existing founded settlement at the point; creates outgoing founded-from to the new depths region.",
        Target = TargetSubtypesWithIncomingRelationshipsAndWithoutOutgoing(
            K.PointOfInterest,
            ["vent", "peak", "bloom", "shoal"],
            [Rel.ConduitOf, Rel.FoundedFrom],
            Rel.FoundedFrom),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$patron",
                Kind = K.Faction,
                Query = SameCultureAsTarget(samePhysicalComponent: true),
            },
        ],
        Produces =
        [
            DepthsRegion("$depths", "$target", "$patron"),
            Required("$seal", K.Artifact, "seal", "$depths", "$patron"),
        ],
        Relationships =
        [
            R("$target", "$depths", Rel.FoundedFrom, 0.86f),
            R("$seal", "$depths", Rel.Anchored, 0.84f),
            R("$depths", "$target", Rel.Memorializes, 0.78f),
        ],
        Triggers = TriggerOn(K.PointOfInterest),
        PulseWeight = 6,
    };

    private static SemanticGeneratorConfig WardedMonstersFoundOrders() => new()
    {
        Id = "warded_monsters_found_orders",
        Name = "Warded Monsters Found Orders",
        Description = "A ward that holds around a monster becomes institution instead of emergency. An order and a battleship enter the world to keep the binding legible.",
        CausalLimit = "Depends on strong incoming ward-against topology and no incoming monster memorial; creates incoming memorializes as the repeat lock.",
        Target = TargetWithStrongIncomingAndWithoutIncoming(
            K.Monster,
            Rel.WardAgainst,
            WardedMonstersFoundOrdersMinimumWardStrength,
            Rel.Memorializes),
        Produces =
        [
            Required("$order", K.Faction, "order", "$target", "$target"),
            Required("$battleship", K.Transport, "battleship", "$target", "$target"),
        ],
        Relationships =
        [
            R("$order", "$battleship", Rel.Holds, 1f),
            R("$battleship", "$target", Rel.WardAgainst, 0.82f),
            R("$battleship", "$target", Rel.Memorializes, 0.84f),
        ],
        Triggers = TriggerOn(K.Monster),
    };

    private static SemanticProducedEntity DepthsRegion(
        string @ref,
        string placementRef,
        string cultureRef)
    {
        var physicalProfile = IceRemembersPlacementTerrainProfiles.Physical(K.Region, "depths");
        var semanticProfile = IceRemembersPlacementTerrainProfiles.Semantic(K.Region, "depths");
        return new()
        {
            Ref = @ref,
            Kind = K.Region,
            Subtype = "depths",
            Role = "depths",
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
            InheritCultureFrom = cultureRef,
            CulturePlacement = GeneratorCulturePlacement.CultureInterior,
            Coverage = new SemanticRegionCoverage
            {
                Mode = CoveragePlacementMode.CreateNew,
                DesiredCellCount = 10,
                MinimumCellCount = 3,
                PreferredTerrains = physicalProfile.PreferredTerrains,
            },
        };
    }

    private static SemanticGeneratorTarget TargetSubtypeWithOutgoingAndWithoutIncoming(
        EntityKind kind,
        string subtype,
        Rel required,
        Rel without) =>
        new()
        {
            Kind = kind,
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { subtype },
            RequiredOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(required) },
            WithoutIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(without) },
        };

    private static SemanticGeneratorTarget TargetSubtypesWithIncomingAndWithoutIncoming(
        EntityKind kind,
        IReadOnlyList<string> subtypes,
        Rel required,
        Rel without) =>
        new()
        {
            Kind = kind,
            Subtypes = new HashSet<string>(subtypes, StringComparer.Ordinal),
            RequiredIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(required) },
            WithoutIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(without) },
        };

    private static SemanticGeneratorTarget TargetSubtypesWithIncomingRelationshipsAndWithoutOutgoing(
        EntityKind kind,
        IReadOnlyList<string> subtypes,
        IReadOnlyList<Rel> required,
        Rel without) =>
        new()
        {
            Kind = kind,
            Subtypes = new HashSet<string>(subtypes, StringComparer.Ordinal),
            RequiredIncomingRelationships = new HashSet<string>(
                required.Select(relationship => L(relationship)),
                StringComparer.Ordinal),
            WithoutOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(without) },
        };
}
