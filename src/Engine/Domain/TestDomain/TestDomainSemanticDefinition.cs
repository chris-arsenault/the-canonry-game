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
    ];

    private static IReadOnlyList<SemanticFlowConfig> CreateFlows() =>
    [
        // Clean slate: the only authored flows are engine_decay outflows on every scalar stock.
        // Entity-level verbs and their inflow/outflow wiring will be re-authored per design.
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
