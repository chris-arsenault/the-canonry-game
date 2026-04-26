namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Kinds;
using Engine.Wiring;
using Attr = IceRemembers.IceRemembersAttributeId;
using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;
using Res = IceRemembers.IceRemembersResource;
using Verb = IceRemembers.IceRemembersVerbId;

public static class IceRemembersSemanticDefinition
{
    public static SemanticDomainConfig Create() => new()
    {
        RingOrder =
        [
            K.Penguin,
            K.Settlement,
            K.Faction,
            K.Region,
            K.Artifact,
        ],
        Stocks = CreateStocks(),
        Verbs = CreateVerbs(),
        Flows = CreateFlows(),
        Modifiers = CreateModifiers(),
        Generators = IceRemembersGeneratorDefinition.Create(),
    };

    private static string A(Attr attribute) => attribute.ToDomainName();
    private static string L(Rel relationship) => relationship.ToDomainName();
    private static string R(Res resource) => resource.ToDomainName();
    private static string VerbId(Verb verbId) => verbId.ToVerbId();

    private static IReadOnlyList<SemanticStockConfig> CreateStocks() =>
    [
        // Artifact: H + C + D
        Stock(K.Artifact, Attr.Condition, SemanticStockClass.Anchor, SemanticFeedbackClass.Anchor,
            "0..100", "anchor", "Health (integrity). Dies at 0."),
        Stock(K.Artifact, Attr.Power, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Capacity — tangible working stock spent on outward actions."),
        Stock(K.Artifact, Attr.Resonance, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Drive — diffuse resonance; bleeds into the world, accumulates from compromised land, and consecrates settlements."),

        // Faction: H + P + N
        Stock(K.Faction, Attr.Cohesion, SemanticStockClass.Anchor, SemanticFeedbackClass.Anchor,
            "0..100", "anchor", "Health (integrity). Dies at 0."),
        Stock(K.Faction, Attr.Reach, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Projection — factional reach: symbolic, reputational, political, and logistical."),
        Stock(K.Faction, Attr.Provisions, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Need — stockpile of material resources; deficit drives seeking."),

        // NPC: H + C + N
        Stock(K.Penguin, Attr.Vitality, SemanticStockClass.Anchor, SemanticFeedbackClass.Anchor,
            "0..100", "anchor", "Health (integrity). Dies at 0."),
        Stock(K.Penguin, Attr.Echo, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Capacity — what others can draw on from the NPC for outward tasks."),
        Stock(K.Penguin, Attr.Warmth, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Need — warmth deficit drives seeking food, shelter, and survivable company."),

        // Region: H + C + N
        Stock(K.Region, Attr.Stability, SemanticStockClass.Anchor, SemanticFeedbackClass.Anchor,
            "0..100", "anchor", "Health (integrity). Dies at 0."),
        Stock(K.Region, Attr.Holding, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Capacity — the land's holding: how well the world keeps its cover, thresholds, and protections intact."),
        Stock(K.Region, Attr.Pristiness, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Need — unspoiled quality; low values mark breached, opened, or compromised ground."),

        // Settlement: H + P + N
        Stock(K.Settlement, Attr.Residents, SemanticStockClass.Anchor, SemanticFeedbackClass.Anchor,
            "0..∞", "anchor", "Health (integrity). Dies at 0."),
        Stock(K.Settlement, Attr.Dominion, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Projection — broad civic and territorial reach: political, cultural, mercantile, military."),
        Stock(K.Settlement, Attr.Shelter, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Need — shelter, warmth, and civic habitability; deficit drives corrective behaviors."),

        ResourceStock(Res.Food, "Ambient hex food abundance."),
        ResourceStock(Res.Wealth, "Ambient hex wealth."),
        ResourceStock(Res.Population, "Ambient hex population."),
    ];

    private static IReadOnlyList<SemanticVerbConfig> CreateVerbs() =>
    [
        EngineVerb(Verb.EngineDecay),

        // Self-sacrifice verbs (H → X within each kind).
        EntityVerb(Verb.ArtifactChannel, K.Artifact,
            notes: "The artifact burns its physical integrity to fuel power projection."),
        EntityVerb(Verb.FactionAgitate, K.Faction,
            notes: "A faction spends internal unity to agitate outward, pushing its reach at the cost of cohesion."),
        EntityVerb(Verb.PenguinEndure, K.Penguin,
            notes: "A penguin keeps their warmth through the long dark at the cost of their body."),
        EntityVerb(Verb.RegionCleanse, K.Region,
            notes: "Slow geological calm purifies the land — the region settles and cleans itself."),
        EntityVerb(Verb.SettlementOverreach, K.Settlement,
            notes: "The settlement overreaches — residents are spent on dominion that exceeds their capacity. Soldiers march too far, builders work to ruin, missionaries die in the wilderness; the city's reach grows at the cost of its people."),

        // Hex → Health inflow verbs (food / wealth / population feed each kind's lifecycle).
        EntityVerb(Verb.PenguinWarm, K.Penguin,
            notes: "Penguins draw warmth from the local hex's food abundance, shelter, and survivable company."),
        EntityVerb(Verb.ArtifactRepair, K.Artifact,
            notes: "Wealth repairs artifacts — patrons fund preservation, custodial care, and replacement of worn parts."),
        EntityVerb(Verb.FactionOrganize, K.Faction,
            notes: "Wealth organizes the faction — offices, meeting halls, couriers, and enforcement bind cohesion."),
        EntityVerb(Verb.RegionRoot, K.Region,
            notes: "Agricultural cultivation roots the region — soil management stabilizes the bedrock."),
        EntityVerb(Verb.SettlementGrow, K.Settlement,
            notes: "Settlements absorb population from their hex as immigrants join."),

        // Untouched → hex outflow verbs (each kind expands the local hex's possibility space at the cost of an attribute).
        EntityVerb(Verb.PenguinWitness, K.Penguin,
            notes: "Echo-rich penguins become socially legible — singers, survivors, and remembered names expand the hex's population capacity as their echo spends."),
        EntityVerb(Verb.ArtifactVivify, K.Artifact,
            notes: "The artifact vivifies the land — its resonance is spent as lifeforce, quickening soil and seed; the hex's food possibility grows as the resonance drains."),
        EntityVerb(Verb.FactionDistribute, K.Faction,
            notes: "Faction stockpiles fed back into the local food supply — alms, charity, military rations. Provisions transfer directly into the hex."),
        EntityVerb(Verb.RegionBreach, K.Region,
            notes: "The region is breached — its held interior is opened for ore, lumber, fire-cores, and hidden wealth. Holding drains as extraction takes hold; the hex's wealth possibility grows."),
        EntityVerb(Verb.SettlementStripRookeries, K.Settlement,
            notes: "The settlement strips rookeries — nesting space, shelter, and civic habitability are converted into saleable material and harsh output; the hex's wealth possibility grows."),

        // RPS ring conversion verbs (cross-entity drain).
        EntityVerb(Verb.SettlementClaimsEcho, K.Settlement,
            notes: "The settlement claims an NPC's echo — a remembered name is absorbed into civic legitimacy, and the city's dominion grows."),
        EntityVerb(Verb.FactionStarve, K.Faction,
            notes: "The faction starves its followers — warmth empties into faction stockpiles. Rationing, tithe-in-kind, requisitioned reserves: warmth converts into provisions."),
        EntityVerb(Verb.FactionExtort, K.Faction,
            notes: "The faction extorts the orderly city — settlement shelter is skimmed off the top as kickbacks, palm-greasing, and 'arrangements,' converting civic order into projected reach."),
        EntityVerb(Verb.RegionReclaim, K.Region,
            notes: "The region reclaims its pristine character from encroaching settlement dominion — cycles of erosion and renewal thin the settlement's footprint and restore untouched land."),
        EntityVerb(Verb.RegionFortify, K.Region,
            notes: "The region fortifies itself from faction provisions — stockpiles become standing watch, supply drops, kept roads, and maintained thresholds."),
        EntityVerb(Verb.ArtifactBind, K.Artifact,
            notes: "The artifact binds factional reach into its resonance — offices, vows, routes, and attention are knotted around the thing."),
        EntityVerb(Verb.ArtifactCorrupt, K.Artifact,
            notes: "The artifact corrupts the land — it drinks the region's pristiness, turning unspoiled character into its own raw power."),
        EntityVerb(Verb.PenguinForage, K.Penguin,
            notes: "Penguins forage held regions — protected hexes yield game, water, and edible flora straight to belly-fill. Holding converts into warmth."),
        EntityVerb(Verb.PenguinWield, K.Penguin,
            notes: "The penguin wields the artifact — holding the thing, they draw its working power into their own echo."),
        EntityVerb(Verb.SettlementSanctify, K.Settlement,
            notes: "The settlement sanctifies itself in the artifact's resonance — wards, blessed wells, sanctified ground, intimidation of the unclean. Resonance spends into civic shelter."),

        // Mobility verbs — enablement: each kind's act of repositioning into range of its targets.
        PositioningVerb(Verb.PenguinTravel, K.Penguin, SemanticVerbRole.Mobility,
            "The penguin travels — walking, riding, marching across the map hex grid toward a target."),
        PositioningVerb(Verb.ArtifactCompel, K.Artifact, SemanticVerbRole.Mobility,
            "The artifact compels a bearer — its will draws someone to carry it. Bearers do not choose freely; the thing moves through the hands it has claimed."),
        PositioningVerb(Verb.SettlementDeliberate, K.Settlement, SemanticVerbRole.Mobility,
            "The settlement deliberates — councils argue, guilds maneuver, factions within the city win and lose on the public floor. The settlement's civic identity shifts on the semantic plane as positions are settled."),
        PositioningVerb(Verb.FactionPreach, K.Faction, SemanticVerbRole.Mobility,
            "The faction preaches — speakers, pamphlets, oaths-at-the-gates. The faction moves on the autonomy/resonance/tradition plane by spreading doctrine and drawing targets closer to its position (or repositioning itself toward them)."),
        PositioningVerb(Verb.RegionCommune, K.Region, SemanticVerbRole.Mobility,
            "The region communes — the land's spirit stirs, takes counsel with itself, shifts its animist alignment. This is the region as a living place moving on the semantic plane; not the people within it, but the genius loci."),

        // Drift verbs — downtime: random one-step movement on the assigned grid when nothing else is happening.
        PositioningVerb(Verb.PenguinWander, K.Penguin, SemanticVerbRole.Drift,
            "The penguin wanders — idle footsteps across the map hex grid when no goal demands them."),
        PositioningVerb(Verb.ArtifactCirculate, K.Artifact, SemanticVerbRole.Drift,
            "The artifact circulates — slow passage through hands over time: lost, found, stolen, inherited. No single will drives it; the thing moves because things of its weight always end up somewhere new."),
        PositioningVerb(Verb.SettlementGossip, K.Settlement, SemanticVerbRole.Drift,
            "The settlement gossips — taverns, markets, pulpits, alleys. Rumor and talk nudge civic identity slightly, slowly, without any formal decision. The city drifts on the semantic plane as mood does."),
        PositioningVerb(Verb.FactionQuarrel, K.Faction, SemanticVerbRole.Drift,
            "The faction quarrels — internal bickering, petty doctrinal drift, the slow slippage of alignment when no new oath holds the line. The faction wobbles on the semantic plane as its members argue among themselves."),
        PositioningVerb(Verb.RegionDream, K.Region, SemanticVerbRole.Drift,
            "The region dreams — the land's spirit drifts in reverie, its animist alignment shifting with the seasons, the weather, the weight of what has passed. The genius loci dreams when not actively communing."),
    ];

    private static IReadOnlyList<SemanticFlowConfig> CreateFlows() =>
    [
        // Engine decay — one per scalar stock.
        Decay(K.Artifact, Attr.Condition),
        Decay(K.Artifact, Attr.Power),
        Decay(K.Artifact, Attr.Resonance),

        Decay(K.Faction, Attr.Cohesion),
        Decay(K.Faction, Attr.Reach),
        Decay(K.Faction, Attr.Provisions),

        Decay(K.Penguin, Attr.Vitality),
        Decay(K.Penguin, Attr.Echo),
        Decay(K.Penguin, Attr.Warmth),

        Decay(K.Region, Attr.Stability),
        Decay(K.Region, Attr.Holding),
        Decay(K.Region, Attr.Pristiness),

        Decay(K.Settlement, Attr.Residents),
        Decay(K.Settlement, Attr.Dominion),
        Decay(K.Settlement, Attr.Shelter),

        // Self-sacrifice (H → X within each kind).
        ..SelfSac(K.Artifact, Attr.Condition, Attr.Power, Verb.ArtifactChannel,
            "The artifact burns its physical integrity to fuel power projection."),
        ..SelfSac(K.Faction, Attr.Cohesion, Attr.Reach, Verb.FactionAgitate,
            "A faction spends internal unity to agitate outward, pushing its reach at the cost of cohesion."),
        ..SelfSac(K.Penguin, Attr.Vitality, Attr.Warmth, Verb.PenguinEndure,
            "A penguin keeps their warmth through the long dark at the cost of their body."),
        ..SelfSac(K.Region, Attr.Stability, Attr.Pristiness, Verb.RegionCleanse,
            "Slow geological calm purifies the land — the region settles and cleans itself."),
        ..SelfSac(K.Settlement, Attr.Residents, Attr.Dominion, Verb.SettlementOverreach,
            "The settlement overreaches — residents are spent on dominion: soldiers march too far, builders work to ruin, missionaries die in the wilderness."),

        // Hex → Health inflows (every kind's lifecycle anchor feeds from a hex resource).
        HexToHealth(K.Penguin, Attr.Vitality, Res.Food, Verb.PenguinWarm,
            "Penguins draw warmth from the local hex's food abundance, shelter, and survivable company."),
        HexToHealth(K.Artifact, Attr.Condition, Res.Wealth, Verb.ArtifactRepair,
            "Wealth repairs artifacts — patrons fund preservation, custodial care, and replacement of worn parts."),
        HexToHealth(K.Faction, Attr.Cohesion, Res.Wealth, Verb.FactionOrganize,
            "Wealth organizes the faction — offices, meeting halls, couriers, and enforcement bind cohesion."),
        HexToHealth(K.Region, Attr.Stability, Res.Food, Verb.RegionRoot,
            "Agricultural cultivation roots the region — soil management stabilizes the bedrock."),
        HexToHealth(K.Settlement, Attr.Residents, Res.Population, Verb.SettlementGrow,
            "Settlements absorb population from their hex as immigrants join."),

        // Untouched → hex outflows (each kind spends an attribute to expand the local hex's possibility space).
        UntouchedToHex(K.Penguin, Attr.Echo, Res.Population, Verb.PenguinWitness,
            "Echo-rich penguins draw migrants — witnesses, singers, and social magnets. Echo spends as they rally; the hex's population capacity grows."),
        UntouchedToHex(K.Artifact, Attr.Resonance, Res.Food, Verb.ArtifactVivify,
            "The artifact vivifies the land — resonance spent as lifeforce, quickening soil and seed; the hex's food possibility grows."),
        UntouchedToHex(K.Faction, Attr.Provisions, Res.Food, Verb.FactionDistribute,
            "Faction stockpiles fed back into the local food supply — alms, charity, military rations."),
        UntouchedToHex(K.Region, Attr.Holding, Res.Wealth, Verb.RegionBreach,
            "The region is breached — held interior opened for ore, lumber, fire-cores, and hidden wealth. Holding drains as extraction takes hold; the hex's wealth possibility grows."),
        UntouchedToHex(K.Settlement, Attr.Shelter, Res.Wealth, Verb.SettlementStripRookeries,
            "The settlement strips rookeries — nesting space and shelter are converted into saleable material and harsh output; the hex's wealth possibility grows."),

        // RPS +1 edges (K_N → K_{N+1}).
        ..Conversion(K.Penguin, Attr.Echo, K.Settlement, Attr.Dominion, Verb.SettlementClaimsEcho,
            "The settlement claims an NPC's echo — a remembered name is absorbed into civic legitimacy, and the city's dominion grows."),
        ..Conversion(K.Settlement, Attr.Shelter, K.Faction, Attr.Reach, Verb.FactionExtort,
            "The faction extorts the orderly city — settlement shelter is skimmed off the top as kickbacks and palm-greasing, converting civic order into projected reach."),
        ..Conversion(K.Faction, Attr.Provisions, K.Region, Attr.Holding, Verb.RegionFortify,
            "The region fortifies itself from faction provisions — stockpiles become standing watch, supply drops, kept roads, and maintained thresholds."),
        ..Conversion(K.Region, Attr.Pristiness, K.Artifact, Attr.Power, Verb.ArtifactCorrupt,
            "The artifact corrupts the land — drinks unspoiled character, converting pristiness into its own raw power."),
        ..Conversion(K.Artifact, Attr.Power, K.Penguin, Attr.Echo, Verb.PenguinWield,
            "The penguin wields the artifact — pulls its working power into their own echo."),

        // RPS +2 edges (K_N → K_{N+2}).
        ..Conversion(K.Penguin, Attr.Warmth, K.Faction, Attr.Provisions, Verb.FactionStarve,
            "The faction starves its followers — NPC bellies empty into faction stockpiles: rationing, tithe-in-kind, requisitioned reserves."),
        ..Conversion(K.Settlement, Attr.Dominion, K.Region, Attr.Pristiness, Verb.RegionReclaim,
            "The region reclaims its pristine character from encroaching settlement dominion — cycles of erosion and renewal thin the settlement's footprint."),
        ..Conversion(K.Faction, Attr.Reach, K.Artifact, Attr.Resonance, Verb.ArtifactBind,
            "The artifact binds factional reach into its resonance — offices, vows, routes, and attention are knotted around the thing."),
        ..Conversion(K.Region, Attr.Holding, K.Penguin, Attr.Warmth, Verb.PenguinForage,
            "Penguins forage held regions — protected hexes yield game, water, and edible flora; holding converts into warmth."),
        ..Conversion(K.Artifact, Attr.Resonance, K.Settlement, Attr.Shelter, Verb.SettlementSanctify,
            "The settlement sanctifies itself in the artifact's resonance — wards, blessed wells, sanctified ground; resonance spends into civic shelter."),
    ];

    private static IReadOnlyList<SemanticModifierConfig> CreateModifiers() => [];

    private static SemanticAttributeStockConfig Stock(
        EntityKind kind,
        Attr attribute,
        SemanticStockClass stockClass,
        SemanticFeedbackClass feedbackClass,
        string boundsLabel,
        string conservationLabel,
        string notes) =>
        new()
        {
            Kind = kind,
            AttributeName = A(attribute),
            StockClass = stockClass,
            FeedbackClass = feedbackClass,
            RequiredFlowCoverage = SemanticFlowCoverageRequirement.Both,
            BoundsLabel = boundsLabel,
            ConservationLabel = conservationLabel,
            Notes = notes,
        };

    private static SemanticResourceStockConfig ResourceStock(Res resource, string notes) =>
        new()
        {
            ResourceName = R(resource),
            StockClass = SemanticStockClass.Free,
            FeedbackClass = SemanticFeedbackClass.Unknown,
            RequiredFlowCoverage = SemanticFlowCoverageRequirement.None,
            Notes = notes,
        };

    private static SemanticVerbConfig EntityVerb(
        Verb verb,
        EntityKind ownerKind,
        string? notes = null) =>
        new()
        {
            Id = VerbId(verb),
            OwnerType = SemanticOwnerType.Entity,
            OwnerKind = ownerKind,
            Notes = notes,
        };

    // Mobility/drift verbs are positioning-only: flowless, tagged with their role so
    // the invariant can check that each kind has exactly one of each.
    private static SemanticVerbConfig PositioningVerb(
        Verb verb,
        EntityKind ownerKind,
        SemanticVerbRole role,
        string notes) =>
        new()
        {
            Id = VerbId(verb),
            OwnerType = SemanticOwnerType.Entity,
            OwnerKind = ownerKind,
            Role = role,
            Notes = notes,
            IsFlowless = true,
        };

    private static SemanticVerbConfig EngineVerb(Verb verb, string? notes = null) =>
        new()
        {
            Id = VerbId(verb),
            OwnerType = SemanticOwnerType.Engine,
            Notes = notes,
        };

    private static SemanticFlowConfig Inflow(
        EntityKind kind,
        Attr attribute,
        Verb verb,
        SemanticMechanism mechanism,
        string signatureLabel,
        params AttributeRef[] sourceRefs) =>
        Flow(kind, attribute, verb, SemanticFlowRole.Inflow, mechanism, signatureLabel, SemanticPlaneAxis.None, sourceRefs);

    private static SemanticFlowConfig Inflow(
        EntityKind kind,
        Attr attribute,
        Verb verb,
        SemanticMechanism mechanism,
        string signatureLabel,
        SemanticPlaneAxis planeAxis,
        params AttributeRef[] sourceRefs) =>
        Flow(kind, attribute, verb, SemanticFlowRole.Inflow, mechanism, signatureLabel, planeAxis, sourceRefs);

    private static SemanticFlowConfig Outflow(
        EntityKind kind,
        Attr attribute,
        Verb verb,
        SemanticMechanism mechanism,
        string signatureLabel,
        params AttributeRef[] sourceRefs) =>
        Flow(kind, attribute, verb, SemanticFlowRole.Outflow, mechanism, signatureLabel, SemanticPlaneAxis.None, sourceRefs);

    private static SemanticFlowConfig Outflow(
        EntityKind kind,
        Attr attribute,
        Verb verb,
        SemanticMechanism mechanism,
        string signatureLabel,
        SemanticPlaneAxis planeAxis,
        params AttributeRef[] sourceRefs) =>
        Flow(kind, attribute, verb, SemanticFlowRole.Outflow, mechanism, signatureLabel, planeAxis, sourceRefs);

    private static SemanticFlowConfig Decay(EntityKind kind, Attr attribute) =>
        new()
        {
            VerbId = VerbId(Verb.EngineDecay),
            Role = SemanticFlowRole.Outflow,
            TargetKind = kind,
            TargetAttribute = A(attribute),
            Mechanism = SemanticMechanism.Decay,
        };

    // Hex → Health inflow. Source ref is a hex resource; target is the kind's Health.
    private static SemanticFlowConfig HexToHealth(
        EntityKind kind,
        Attr health,
        Res resource,
        Verb verb,
        string reasoning) =>
        new()
        {
            VerbId = VerbId(verb),
            Role = SemanticFlowRole.Inflow,
            TargetKind = kind,
            TargetAttribute = A(health),
            Mechanism = SemanticMechanism.Inflow,
            SignatureLabel = $"self; inflow from hex.{R(resource)}",
            FormLabel = $"self; inflow from hex.{R(resource)}",
            Reasoning = reasoning,
            SourceRefs = [Resource(resource)],
        };

    // Untouched → hex outflow. Target stock is the kind's Untouched attribute;
    // source ref is a hex resource (the destination of the mass).
    private static SemanticFlowConfig UntouchedToHex(
        EntityKind kind,
        Attr untouched,
        Res resource,
        Verb verb,
        string reasoning) =>
        new()
        {
            VerbId = VerbId(verb),
            Role = SemanticFlowRole.Outflow,
            TargetKind = kind,
            TargetAttribute = A(untouched),
            Mechanism = SemanticMechanism.Conversion,
            SignatureLabel = $"self; outflow to hex.{R(resource)}",
            FormLabel = $"self; outflow to hex.{R(resource)}",
            Reasoning = reasoning,
            SourceRefs = [Resource(resource)],
        };

    // Emits both halves of a cross-entity conversion flow: source kind's stock
    // depletes, target kind's stock grows. Outflow on source attribute (target ref
    // = destination), inflow on target attribute (source ref = origin). Both
    // halves share the same narrative reasoning.
    private static IEnumerable<SemanticFlowConfig> Conversion(
        EntityKind sourceKind,
        Attr sourceAttr,
        EntityKind targetKind,
        Attr targetAttr,
        Verb verb,
        string reasoning)
    {
        yield return new SemanticFlowConfig
        {
            VerbId = VerbId(verb),
            Role = SemanticFlowRole.Outflow,
            TargetKind = sourceKind,
            TargetAttribute = A(sourceAttr),
            Mechanism = SemanticMechanism.Conversion,
            SignatureLabel = $"target; conversion to {targetKind}.{A(targetAttr)}",
            FormLabel = $"target; conversion to {targetKind}.{A(targetAttr)}",
            Reasoning = reasoning,
            SourceRefs = [Target(targetKind, targetAttr)],
        };
        yield return new SemanticFlowConfig
        {
            VerbId = VerbId(verb),
            Role = SemanticFlowRole.Inflow,
            TargetKind = targetKind,
            TargetAttribute = A(targetAttr),
            Mechanism = SemanticMechanism.Conversion,
            SignatureLabel = $"self; conversion from {sourceKind}.{A(sourceAttr)}",
            FormLabel = $"self; conversion from {sourceKind}.{A(sourceAttr)}",
            Reasoning = reasoning,
            SourceRefs = [Target(sourceKind, sourceAttr)],
        };
    }

    // Emits both halves of a self-sacrifice flow (Health → X): an outflow on H
    // with source ref Self(kind, target), and an inflow on target with source ref
    // Self(kind, Health). Both halves carry the same narrative reasoning.
    private static IEnumerable<SemanticFlowConfig> SelfSac(
        EntityKind kind,
        Attr source,
        Attr target,
        Verb verb,
        string reasoning)
    {
        yield return new SemanticFlowConfig
        {
            VerbId = VerbId(verb),
            Role = SemanticFlowRole.Outflow,
            TargetKind = kind,
            TargetAttribute = A(source),
            Mechanism = SemanticMechanism.Sacrifice,
            SignatureLabel = $"self; sacrifice into {A(target)}",
            FormLabel = $"self; sacrifice into {A(target)}",
            Reasoning = reasoning,
            SourceRefs = [Self(kind, target)],
        };
        yield return new SemanticFlowConfig
        {
            VerbId = VerbId(verb),
            Role = SemanticFlowRole.Inflow,
            TargetKind = kind,
            TargetAttribute = A(target),
            Mechanism = SemanticMechanism.Sacrifice,
            SignatureLabel = $"self; sacrifice from {A(source)}",
            FormLabel = $"self; sacrifice from {A(source)}",
            Reasoning = reasoning,
            SourceRefs = [Self(kind, source)],
        };
    }

    private static SemanticFlowConfig Flow(
        EntityKind kind,
        Attr attribute,
        Verb verb,
        SemanticFlowRole role,
        SemanticMechanism mechanism,
        string signatureLabel,
        SemanticPlaneAxis planeAxis,
        params AttributeRef[] sourceRefs) =>
        new()
        {
            VerbId = VerbId(verb),
            Role = role,
            TargetKind = kind,
            TargetAttribute = A(attribute),
            PlaneAxis = planeAxis,
            Mechanism = mechanism,
            SignatureLabel = signatureLabel,
            FormLabel = signatureLabel,
            SourceRefs = sourceRefs,
        };

    private static SemanticModifierConfig Modifier(
        EntityKind sourceKind,
        Attr sourceAttribute,
        Verb targetVerb,
        SemanticModifierDirection direction,
        string formLabel) =>
        new()
        {
            SourceKind = sourceKind,
            SourceAttribute = A(sourceAttribute),
            TargetVerbId = VerbId(targetVerb),
            Direction = direction,
            FormLabel = formLabel,
        };


    private static AttributeRef Resource(Res resource) =>
        AttributeRef.Resource(R(resource));

    private static AttributeRef Self(EntityKind kind, Attr attribute) =>
        AttributeRef.Self(A(attribute), kind);

    private static AttributeRef Target(EntityKind kind, Attr attribute) =>
        AttributeRef.Target(A(attribute), kind);

    private static AttributeRef Related(Rel relationship, Attr attribute, EntityKind targetKind) =>
        AttributeRef.Related(L(relationship), A(attribute), targetKind);
}
