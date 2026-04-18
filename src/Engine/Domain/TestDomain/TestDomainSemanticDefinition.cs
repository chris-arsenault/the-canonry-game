namespace Engine.Domain.TestDomain;

using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Kinds;
using Engine.Wiring;
using Attr = Engine.Domain.TestDomain.TestDomainAttributeId;
using Proc = Engine.Domain.TestDomain.TestDomainProcessId;
using Rel = Engine.Domain.TestDomain.TestDomainRelationship;
using Res = Engine.Domain.TestDomain.TestDomainResource;
using Verb = Engine.Domain.TestDomain.TestDomainVerbId;

public static class TestDomainSemanticDefinition
{
    public static SemanticDomainConfig Create() => new()
    {
        Stocks = CreateStocks(),
        Verbs = CreateVerbs(),
        Flows = CreateFlows(),
        Modifiers = CreateModifiers(),
        Generators = TestDomainGeneratorDefinition.Create(),
    };

    private static string A(Attr attribute) => attribute.ToDomainName();
    private static string L(Rel relationship) => relationship.ToDomainName();
    private static string ProcId(Proc processId) => processId.ToProcessId();
    private static string R(Res resource) => resource.ToDomainName();
    private static string VerbId(Verb verbId) => verbId.ToVerbId();

    private static IReadOnlyList<SemanticStockConfig> CreateStocks() =>
    [
        // Artifact: H + C + D
        Stock(EntityKind.Artifact, Attr.Condition, SemanticStockClass.Anchor, SemanticFeedbackClass.Anchor,
            "0..100", "anchor", "Health (integrity). Dies at 0."),
        Stock(EntityKind.Artifact, Attr.Power, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Capacity — tangible working stock spent on outward actions."),
        Stock(EntityKind.Artifact, Attr.Aura, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Drive — diffuse sacred/cursed charge; bleeds into the world, accumulates from corrupted land, consecrates settlements."),

        // Faction: H + P + N
        Stock(EntityKind.Faction, Attr.Cohesion, SemanticStockClass.Anchor, SemanticFeedbackClass.Anchor,
            "0..100", "anchor", "Health (integrity). Dies at 0."),
        Stock(EntityKind.Faction, Attr.Influence, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Projection — symbolic/reputational output."),
        Stock(EntityKind.Faction, Attr.Provisions, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Need — stockpile of material resources; deficit drives seeking."),

        // NPC: H + C + N
        Stock(EntityKind.Npc, Attr.Vitality, SemanticStockClass.Anchor, SemanticFeedbackClass.Anchor,
            "0..100", "anchor", "Health (integrity). Dies at 0."),
        Stock(EntityKind.Npc, Attr.Motivation, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Capacity — what others can draw on from the NPC for outward tasks."),
        Stock(EntityKind.Npc, Attr.Satiety, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Need — hunger deficit drives seeking food."),

        // Region: H + C + N
        Stock(EntityKind.Region, Attr.GeologicStability, SemanticStockClass.Anchor, SemanticFeedbackClass.Anchor,
            "0..100", "anchor", "Health (integrity). Dies at 0."),
        Stock(EntityKind.Region, Attr.RegionalSafety, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Capacity — tangible protective emission."),
        Stock(EntityKind.Region, Attr.Pristineness, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Need — unspoiled quality; low values indicate degradation."),

        // Settlement: H + P + N
        Stock(EntityKind.Settlement, Attr.Residents, SemanticStockClass.Anchor, SemanticFeedbackClass.Anchor,
            "0..∞", "anchor", "Health (integrity). Dies at 0."),
        Stock(EntityKind.Settlement, Attr.Dominion, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Projection — broad sphere of influence: political, cultural, mercantile, military."),
        Stock(EntityKind.Settlement, Attr.Sanitation, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Need — public hygiene deficit drives corrective behaviors."),

        ResourceStock(Res.Food, "Ambient hex food abundance."),
        ResourceStock(Res.Wealth, "Ambient hex wealth."),
        ResourceStock(Res.Population, "Ambient hex population."),
    ];

    private static IReadOnlyList<SemanticVerbConfig> CreateVerbs() =>
    [
        EngineVerb(Verb.EngineDecay),

        // Self-sacrifice verbs (H → X within each kind).
        EntityVerb(Verb.ArtifactChannel, EntityKind.Artifact,
            notes: "The artifact burns its physical integrity to fuel power projection."),
        EntityVerb(Verb.FactionRadicalize, EntityKind.Faction,
            notes: "A faction spends internal unity on a zealous outward projection of influence."),
        EntityVerb(Verb.NpcEndure, EntityKind.Npc,
            notes: "An NPC pushes through hunger at the cost of their body."),
        EntityVerb(Verb.RegionCleanse, EntityKind.Region,
            notes: "Slow geological calm purifies the land — the region settles and cleans itself."),
        EntityVerb(Verb.SettlementOverreach, EntityKind.Settlement,
            notes: "The settlement overreaches — residents are spent on ambition that exceeds their capacity. Soldiers march too far, builders work to ruin, missionaries die in the wilderness; the city's reach grows at the cost of its people."),

        // Hex → Health inflow verbs (food / wealth / population feed each kind's lifecycle).
        EntityVerb(Verb.NpcEat, EntityKind.Npc, Proc.NpcEat,
            notes: "NPCs draw nourishment from the local hex's food abundance."),
        EntityVerb(Verb.ArtifactEndow, EntityKind.Artifact,
            notes: "Wealthy locales endow artifacts — patrons fund preservation and custodial care."),
        EntityVerb(Verb.FactionTax, EntityKind.Faction,
            notes: "Wealth circulating through faction territory binds its unity."),
        EntityVerb(Verb.RegionRoot, EntityKind.Region,
            notes: "Agricultural cultivation roots the region — soil management stabilizes the bedrock."),
        EntityVerb(Verb.SettlementGrow, EntityKind.Settlement, Proc.SettlementGrow,
            notes: "Settlements absorb population from their hex as immigrants join."),

        // Untouched → hex outflow verbs (each kind expands the local hex's possibility space at the cost of an attribute).
        EntityVerb(Verb.NpcInspire, EntityKind.Npc,
            notes: "Motivated NPCs draw migrants — role models and social magnets expand the hex's population capacity. Drive spends as they evangelize, lead, and rally."),
        EntityVerb(Verb.ArtifactVivify, EntityKind.Artifact,
            notes: "The artifact vivifies the land — its aura is spent as lifeforce, quickening soil and seed; the hex's food possibility grows as the aura drains."),
        EntityVerb(Verb.FactionDistribute, EntityKind.Faction,
            notes: "Faction stockpiles fed back into the local food supply — alms, charity, military rations. Provisions transfer directly into the hex."),
        EntityVerb(Verb.RegionMine, EntityKind.Region,
            notes: "The region is mined — its safe interior gets dug up for ore, lumber, gold. Regional safety drains as extraction takes hold; the hex's wealth possibility grows."),
        EntityVerb(Verb.SettlementIndustrialize, EntityKind.Settlement,
            notes: "The settlement industrializes — smokestacks, mills, slums. Sanitation drains as the settlement gets dirty and crowded; the hex's wealth possibility grows."),

        // RPS ring conversion verbs (cross-entity drain).
        EntityVerb(Verb.SettlementEmploy, EntityKind.Settlement,
            notes: "The settlement employs motivated NPCs in its administrative machinery — civic labor: builders, clerks, marshals, emissaries. NPC drive is spent and the city's projected reach grows."),
        EntityVerb(Verb.FactionStarve, EntityKind.Faction,
            notes: "The faction starves its followers — NPC bellies empty into faction stockpiles. Rationing, tithe-in-kind, requisitioned reserves: satiety converts into provisions."),
        EntityVerb(Verb.FactionExtort, EntityKind.Faction,
            notes: "The faction extorts the orderly city — settlement sanitation is skimmed off the top as kickbacks, palm-greasing, and 'arrangements,' converting civic order into projected influence."),
        EntityVerb(Verb.RegionReclaim, EntityKind.Region,
            notes: "The region reclaims its pristine character from encroaching settlement dominion — cycles of erosion and renewal thin the settlement's footprint and restore untouched land."),
        EntityVerb(Verb.RegionGarrison, EntityKind.Region,
            notes: "The region keeps its defenses funded by faction provisions — stockpiles converted into standing watch, supply drops, and kept roads."),
        EntityVerb(Verb.ArtifactEnthrall, EntityKind.Artifact,
            notes: "The artifact enthralls its faction — their veneration thickens its aura. Worship feeds the numinous; influence drains into the diffuse sacred/cursed charge around the thing."),
        EntityVerb(Verb.ArtifactCorrupt, EntityKind.Artifact,
            notes: "The artifact corrupts the land — it drinks the region's pristineness, turning unspoiled character into its own raw power. (Corruption is a specific condition in the world; cards will interact with it.)"),
        EntityVerb(Verb.NpcForage, EntityKind.Npc,
            notes: "NPCs forage safe regions — protected hexes yield game, water, and edible flora straight to belly-fill. Regional safety converts into NPC satiety."),
        EntityVerb(Verb.NpcWield, EntityKind.Npc,
            notes: "The NPC wields the artifact — holding the thing, they draw its working power into their own drive. The artifact's power converts into the wielder's motivation."),
        EntityVerb(Verb.SettlementSanctify, EntityKind.Settlement,
            notes: "The settlement sanctifies itself in the artifact's aura — wards, blessed wells, sanctified ground, intimidation of the unclean. The aura's diffuse sacred charge spends into civic order."),

        // Mobility verbs — enablement: each kind's act of repositioning into range of its targets.
        PositioningVerb(Verb.NpcTravel, EntityKind.Npc, SemanticVerbRole.Mobility,
            "The NPC travels — walking, riding, marching across the map hex grid toward a target."),
        PositioningVerb(Verb.ArtifactCompel, EntityKind.Artifact, SemanticVerbRole.Mobility,
            "The artifact compels a bearer — its will draws someone to carry it. Bearers do not choose freely; the thing moves through the hands it has claimed."),
        PositioningVerb(Verb.SettlementDebate, EntityKind.Settlement, SemanticVerbRole.Mobility,
            "The settlement debates its future — councils argue, guilds maneuver, factions within the city win and lose on the public floor. The settlement's civic identity shifts on the semantic plane as positions are settled."),
        PositioningVerb(Verb.FactionEvangelize, EntityKind.Faction, SemanticVerbRole.Mobility,
            "The faction evangelizes — preachers, pamphlets, oaths-at-the-gates. The faction moves on the autonomy/resonance/tradition plane by spreading its doctrine and drawing targets closer to its position (or repositioning itself toward them)."),
        PositioningVerb(Verb.RegionCommune, EntityKind.Region, SemanticVerbRole.Mobility,
            "The region communes — the land's spirit stirs, takes counsel with itself, shifts its animist alignment. This is the region as a living place moving on the semantic plane; not the people within it, but the genius loci."),

        // Drift verbs — downtime: random one-step movement on the assigned grid when nothing else is happening.
        PositioningVerb(Verb.NpcWander, EntityKind.Npc, SemanticVerbRole.Drift,
            "The NPC wanders — idle footsteps across the map hex grid when no goal demands them."),
        PositioningVerb(Verb.ArtifactCirculate, EntityKind.Artifact, SemanticVerbRole.Drift,
            "The artifact circulates — slow passage through hands over time: lost, found, stolen, inherited. No single will drives it; the thing moves because things of its weight always end up somewhere new."),
        PositioningVerb(Verb.SettlementGossip, EntityKind.Settlement, SemanticVerbRole.Drift,
            "The settlement gossips — taverns, markets, pulpits, alleys. Rumor and talk nudge civic identity slightly, slowly, without any formal decision. The city drifts on the semantic plane as mood does."),
        PositioningVerb(Verb.FactionQuarrel, EntityKind.Faction, SemanticVerbRole.Drift,
            "The faction quarrels — internal bickering, petty doctrinal drift, the slow slippage of alignment when no new oath holds the line. The faction wobbles on the semantic plane as its members argue among themselves."),
        PositioningVerb(Verb.RegionDream, EntityKind.Region, SemanticVerbRole.Drift,
            "The region dreams — the land's spirit drifts in reverie, its animist alignment shifting with the seasons, the weather, the weight of what has passed. The genius loci dreams when not actively communing."),
    ];

    private static IReadOnlyList<SemanticFlowConfig> CreateFlows() =>
    [
        // Engine decay — one per scalar stock.
        Decay(EntityKind.Artifact, Attr.Condition),
        Decay(EntityKind.Artifact, Attr.Power),
        Decay(EntityKind.Artifact, Attr.Aura),

        Decay(EntityKind.Faction, Attr.Cohesion),
        Decay(EntityKind.Faction, Attr.Influence),
        Decay(EntityKind.Faction, Attr.Provisions),

        Decay(EntityKind.Npc, Attr.Vitality),
        Decay(EntityKind.Npc, Attr.Motivation),
        Decay(EntityKind.Npc, Attr.Satiety),

        Decay(EntityKind.Region, Attr.GeologicStability),
        Decay(EntityKind.Region, Attr.RegionalSafety),
        Decay(EntityKind.Region, Attr.Pristineness),

        Decay(EntityKind.Settlement, Attr.Residents),
        Decay(EntityKind.Settlement, Attr.Dominion),
        Decay(EntityKind.Settlement, Attr.Sanitation),

        // Self-sacrifice (H → X within each kind).
        ..SelfSac(EntityKind.Artifact, Attr.Condition, Attr.Power, Verb.ArtifactChannel,
            "The artifact burns its physical integrity to fuel power projection."),
        ..SelfSac(EntityKind.Faction, Attr.Cohesion, Attr.Influence, Verb.FactionRadicalize,
            "A faction spends internal unity on a zealous outward projection of influence."),
        ..SelfSac(EntityKind.Npc, Attr.Vitality, Attr.Satiety, Verb.NpcEndure,
            "An NPC pushes through hunger at the cost of their body."),
        ..SelfSac(EntityKind.Region, Attr.GeologicStability, Attr.Pristineness, Verb.RegionCleanse,
            "Slow geological calm purifies the land — the region settles and cleans itself."),
        ..SelfSac(EntityKind.Settlement, Attr.Residents, Attr.Dominion, Verb.SettlementOverreach,
            "The settlement overreaches — residents are spent on ambition: soldiers march too far, builders work to ruin, missionaries die in the wilderness."),

        // Hex → Health inflows (every kind's lifecycle anchor feeds from a hex resource).
        HexToHealth(EntityKind.Npc, Attr.Vitality, Res.Food, Verb.NpcEat,
            "NPCs draw nourishment from the local hex's food abundance."),
        HexToHealth(EntityKind.Artifact, Attr.Condition, Res.Wealth, Verb.ArtifactEndow,
            "Wealthy locales endow artifacts — patrons fund preservation and custodial care."),
        HexToHealth(EntityKind.Faction, Attr.Cohesion, Res.Wealth, Verb.FactionTax,
            "Wealth circulating through faction territory binds its unity."),
        HexToHealth(EntityKind.Region, Attr.GeologicStability, Res.Food, Verb.RegionRoot,
            "Agricultural cultivation roots the region — soil management stabilizes the bedrock."),
        HexToHealth(EntityKind.Settlement, Attr.Residents, Res.Population, Verb.SettlementGrow,
            "Settlements absorb population from their hex as immigrants join."),

        // Untouched → hex outflows (each kind spends an attribute to expand the local hex's possibility space).
        UntouchedToHex(EntityKind.Npc, Attr.Motivation, Res.Population, Verb.NpcInspire,
            "Motivated NPCs draw migrants — role models, evangelists, social magnets. Drive spends as they rally; the hex's population capacity grows."),
        UntouchedToHex(EntityKind.Artifact, Attr.Aura, Res.Food, Verb.ArtifactVivify,
            "The artifact vivifies the land — aura spent as lifeforce, quickening soil and seed; the hex's food possibility grows."),
        UntouchedToHex(EntityKind.Faction, Attr.Provisions, Res.Food, Verb.FactionDistribute,
            "Faction stockpiles fed back into the local food supply — alms, charity, military rations."),
        UntouchedToHex(EntityKind.Region, Attr.RegionalSafety, Res.Wealth, Verb.RegionMine,
            "The region is mined — secure interior dug up for ore, lumber, gold. Safety drains as extraction takes hold; the hex's wealth possibility grows."),
        UntouchedToHex(EntityKind.Settlement, Attr.Sanitation, Res.Wealth, Verb.SettlementIndustrialize,
            "The settlement industrializes — smokestacks, mills, slums. Sanitation drains; the hex's wealth possibility grows."),

        // RPS +1 edges (K_N → K_{N+1}).
        ..Conversion(EntityKind.Npc, Attr.Motivation, EntityKind.Settlement, Attr.Dominion, Verb.SettlementEmploy,
            "The settlement employs motivated NPCs — their drive spends into civic projection: builders, clerks, marshals, emissaries."),
        ..Conversion(EntityKind.Settlement, Attr.Sanitation, EntityKind.Faction, Attr.Influence, Verb.FactionExtort,
            "The faction extorts the orderly city — settlement sanitation is skimmed off the top as kickbacks and palm-greasing, converting civic order into projected influence."),
        ..Conversion(EntityKind.Faction, Attr.Provisions, EntityKind.Region, Attr.RegionalSafety, Verb.RegionGarrison,
            "The region keeps its defenses funded by faction provisions — stockpiles become standing watch, supply drops, and kept roads."),
        ..Conversion(EntityKind.Region, Attr.Pristineness, EntityKind.Artifact, Attr.Power, Verb.ArtifactCorrupt,
            "The artifact corrupts the land — drinks unspoiled character, converting pristineness into its own raw power."),
        ..Conversion(EntityKind.Artifact, Attr.Power, EntityKind.Npc, Attr.Motivation, Verb.NpcWield,
            "The NPC wields the artifact — pulls its working power into their own drive; artifact power converts into motivation."),

        // RPS +2 edges (K_N → K_{N+2}).
        ..Conversion(EntityKind.Npc, Attr.Satiety, EntityKind.Faction, Attr.Provisions, Verb.FactionStarve,
            "The faction starves its followers — NPC bellies empty into faction stockpiles: rationing, tithe-in-kind, requisitioned reserves."),
        ..Conversion(EntityKind.Settlement, Attr.Dominion, EntityKind.Region, Attr.Pristineness, Verb.RegionReclaim,
            "The region reclaims its pristine character from encroaching settlement dominion — cycles of erosion and renewal thin the settlement's footprint."),
        ..Conversion(EntityKind.Faction, Attr.Influence, EntityKind.Artifact, Attr.Aura, Verb.ArtifactEnthrall,
            "The artifact enthralls its faction — veneration thickens its aura; influence drains into the diffuse sacred/cursed charge around the thing."),
        ..Conversion(EntityKind.Region, Attr.RegionalSafety, EntityKind.Npc, Attr.Satiety, Verb.NpcForage,
            "NPCs forage safe regions — protected hexes yield game, water, and edible flora; regional safety converts into satiety."),
        ..Conversion(EntityKind.Artifact, Attr.Aura, EntityKind.Settlement, Attr.Sanitation, Verb.SettlementSanctify,
            "The settlement sanctifies itself in the artifact's aura — wards, blessed wells, sanctified ground; aura spends into civic order."),
    ];

    private static IReadOnlyList<SemanticModifierConfig> CreateModifiers() => [];

    private static IReadOnlyList<SemanticProcessReferenceConfig> CreateProcessReferences() => [];

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
        Proc? implementedProcess = null,
        string? notes = null) =>
        new()
        {
            Id = VerbId(verb),
            OwnerType = SemanticOwnerType.Entity,
            OwnerKind = ownerKind,
            ImplementedProcessId = implementedProcess is Proc process ? ProcId(process) : null,
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

    private static SemanticProcessReferenceConfig Read(Proc process, EntityKind targetKind, Attr targetAttribute) =>
        ProcessReference(process, SemanticProcessReferenceKind.Reads, targetKind, targetAttribute);

    private static SemanticProcessReferenceConfig DirectWrite(Proc process, EntityKind targetKind, Attr targetAttribute) =>
        ProcessReference(process, SemanticProcessReferenceKind.DirectWrites, targetKind, targetAttribute);

    private static SemanticProcessReferenceConfig IndirectWrite(Proc process, EntityKind targetKind, Attr targetAttribute) =>
        ProcessReference(process, SemanticProcessReferenceKind.IndirectWrites, targetKind, targetAttribute);

    private static SemanticProcessReferenceConfig ProcessReference(
        Proc process,
        SemanticProcessReferenceKind kind,
        EntityKind targetKind,
        Attr targetAttribute) =>
        new()
        {
            ProcessId = ProcId(process),
            Kind = kind,
            TargetKind = targetKind,
            TargetAttribute = A(targetAttribute),
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
