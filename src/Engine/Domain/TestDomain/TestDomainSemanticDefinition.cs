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
        ProcessReferences = CreateProcessReferences(),
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
        Stock(EntityKind.Artifact, Attr.Containment, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Drive — inner sacred charge; fuels the artifact's outward effects."),

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
        EntityVerb(Verb.SettlementMobilize, EntityKind.Settlement,
            notes: "The settlement conscripts its residents into extending reach — soldiers, emissaries, missionaries."),

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

        // Untouched → hex outflow verbs (each kind returns something to its local hex).
        EntityVerb(Verb.NpcInspire, EntityKind.Npc,
            notes: "Motivated NPCs draw migrants — role models and social magnets seed the hex's population."),
        EntityVerb(Verb.ArtifactEmanate, EntityKind.Artifact,
            notes: "The artifact's contained spiritual charge bleeds into the land, fertilizing it."),
        EntityVerb(Verb.FactionDistribute, EntityKind.Faction,
            notes: "Faction stockpiles fed back into the local food supply — alms, charity, military rations."),
        EntityVerb(Verb.RegionSettle, EntityKind.Region,
            notes: "Safe regions draw migrants — fearless settlers follow the protective aura."),
        EntityVerb(Verb.SettlementMarket, EntityKind.Settlement,
            notes: "Clean settlements anchor commerce — sanitation enables markets, storage, and disease-free trade."),
    ];

    private static IReadOnlyList<SemanticFlowConfig> CreateFlows() =>
    [
        // Engine decay — one per scalar stock.
        Decay(EntityKind.Artifact, Attr.Condition),
        Decay(EntityKind.Artifact, Attr.Power),
        Decay(EntityKind.Artifact, Attr.Containment),

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
        ..SelfSac(EntityKind.Settlement, Attr.Residents, Attr.Dominion, Verb.SettlementMobilize,
            "The settlement conscripts its residents into extending reach — soldiers, emissaries, missionaries."),

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

        // Untouched → hex outflows (each kind returns something to its local hex).
        UntouchedToHex(EntityKind.Npc, Attr.Motivation, Res.Population, Verb.NpcInspire,
            "Motivated NPCs draw migrants — role models and social magnets seed the hex's population."),
        UntouchedToHex(EntityKind.Artifact, Attr.Containment, Res.Food, Verb.ArtifactEmanate,
            "The artifact's contained spiritual charge bleeds into the land, fertilizing it."),
        UntouchedToHex(EntityKind.Faction, Attr.Provisions, Res.Food, Verb.FactionDistribute,
            "Faction stockpiles fed back into the local food supply — alms, charity, military rations."),
        UntouchedToHex(EntityKind.Region, Attr.RegionalSafety, Res.Population, Verb.RegionSettle,
            "Safe regions draw migrants — fearless settlers follow the protective aura."),
        UntouchedToHex(EntityKind.Settlement, Attr.Sanitation, Res.Wealth, Verb.SettlementMarket,
            "Clean settlements anchor commerce — sanitation enables markets, storage, and disease-free trade."),
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
