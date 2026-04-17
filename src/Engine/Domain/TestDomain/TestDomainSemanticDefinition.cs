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
        Stock(EntityKind.Artifact, Attr.Condition, SemanticStockClass.Anchor, SemanticFeedbackClass.Anchor,
            "0..100",
            "anchor",
            "Lifecycle anchor — artifact destroyed at 0. Maintained by custodian faction (support→condition). Channel sacrifices condition into power."),
        Stock(EntityKind.Artifact, Attr.Power, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100",
            "free",
            "Channel converts condition into power. Empower converts power into custodian faction.influence."),
        Stock(EntityKind.Artifact, Attr.Containment, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100",
            "free",
            "Enshrine converts settlement.prestige into containment. Depleted by heal events."),

        Stock(EntityKind.Faction, Attr.Cohesion, SemanticStockClass.Anchor, SemanticFeedbackClass.Anchor,
            "0..100",
            "anchor",
            "Lifecycle anchor — faction dissolves at 0. Tax accrues cohesion at a rate modified by hex.wealth (hex not consumed)."),
        Stock(EntityKind.Faction, Attr.Support, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100",
            "free",
            "Recruit converts settlement.residents into support (loyalty shift). Maintain spends support on custodian artifact upkeep. Patrol spends support to raise settlement security."),
        Stock(EntityKind.Faction, Attr.Influence, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100",
            "free",
            "Fueled by custodian artifact power (empower depletes artifact.power). Spent on attracting NPCs."),

        Stock(EntityKind.Npc, Attr.Vitality, SemanticStockClass.Anchor, SemanticFeedbackClass.Anchor,
            "0..100",
            "anchor",
            "Heart anchor — NPC dies at 0. npc_endure sacrifices vitality to refill satiety when hungry. Restored by artifact_heal (depletes containment) and region_shelter (depletes regional_safety)."),
        Stock(EntityKind.Npc, Attr.Satiety, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100",
            "free",
            "Refilled by npc_eat (modified by hex.food) or npc_endure sacrifice. Spent on npc_guard toward settlement.security."),
        Stock(EntityKind.Npc, Attr.Ambition, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100",
            "free",
            "Low at spawn (20). Seeded by faction_attract. create_works spends ambition (burnout cost) to raise settlement.prestige."),

        Stock(EntityKind.Region, Attr.RegionalSafety, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100",
            "free",
            "Aggregated upward from contained settlements. region_settle converts safety into hex.population (wilderness→populace). region_shelter converts safety into npc.vitality — sheltering NPCs attract corrupting essences, depleting safety."),
        StockOutflowOnly(EntityKind.Region, Attr.GeologicStability, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100",
            "free",
            "TODO: no authored inflow. region_irrigate converts geologic_stability into hex.food, but nothing currently replenishes it. Coverage temporarily declared Outflow-only until an inflow verb (likely a region self→self sacrifice) is designed."),

        Stock(EntityKind.Settlement, Attr.Residents, SemanticStockClass.Anchor, SemanticFeedbackClass.Anchor,
            "0..∞",
            "anchor",
            "Lifecycle anchor — settlement dies at 0. grow produces residents at a rate modified by hex.population abundance (hex not consumed). attract redistributes residents between settlements. faction_recruit converts residents into faction.support (loyalty shift)."),
        Stock(EntityKind.Settlement, Attr.Security, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100",
            "free",
            "Externally supplied by NPC guards (depletes satiety) and faction patrols (depletes support). Depleted outward via settlement_garrison — security flows from settlement into containing region.regional_safety."),
        Stock(EntityKind.Settlement, Attr.Prestige, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100",
            "free",
            "Earned via NPC deeds (create_works depletes ambition). Spent via artifact_enshrine (conversion to artifact.containment) and settlement_trade (conversion to hex.wealth)."),

        ResourceStock(Res.Food, "Ambient hex food abundance. Modifies NPC satiety intake; produced by region_irrigate. Not depleted by npc_eat — it is a non-consuming abundance signal."),
        ResourceStock(Res.Wealth, "Ambient hex wealth. Modifies faction_tax intake rate; produced by settlement_trade. Not depleted by tax — it is a non-consuming abundance signal."),
        ResourceStock(Res.Population, "Ambient hex population. Modifies settlement_grow intake rate; produced by region_settle (wilderness→populace). Not depleted by grow — it is a non-consuming abundance signal."),
    ];

    private static IReadOnlyList<SemanticVerbConfig> CreateVerbs() =>
    [
        EntityVerb(Verb.ArtifactChannel, EntityKind.Artifact),
        EntityVerb(Verb.ArtifactEmpower, EntityKind.Artifact),
        EntityVerb(Verb.ArtifactEnshrine, EntityKind.Artifact),
        EntityVerb(Verb.ArtifactHeal, EntityKind.Artifact),
        EngineVerb(Verb.EngineDecay),
        EntityVerb(Verb.FactionAttract, EntityKind.Faction),
        EntityVerb(Verb.FactionMaintain, EntityKind.Faction),
        EntityVerb(Verb.FactionPatrol, EntityKind.Faction, Proc.FactionPatrol),
        EntityVerb(Verb.FactionRecruit, EntityKind.Faction),
        EntityVerb(Verb.FactionTax, EntityKind.Faction),
        EntityVerb(Verb.NpcCreateWorks, EntityKind.Npc, Proc.NpcCreateWorks),
        EntityVerb(Verb.NpcEat, EntityKind.Npc, Proc.NpcEat),
        EntityVerb(Verb.NpcEndure, EntityKind.Npc),
        EntityVerb(Verb.NpcGuard, EntityKind.Npc),
        EntityVerb(Verb.RegionIrrigate, EntityKind.Region),
        EntityVerb(Verb.RegionSettle, EntityKind.Region),
        EntityVerb(Verb.RegionShelter, EntityKind.Region),
        EntityVerb(Verb.SettlementAttract, EntityKind.Settlement, Proc.SettlementAttract),
        EntityVerb(Verb.SettlementGarrison, EntityKind.Settlement),
        EntityVerb(Verb.SettlementGrow, EntityKind.Settlement, Proc.SettlementGrow),
        EntityVerb(Verb.SettlementTrade, EntityKind.Settlement),
    ];

    private static IReadOnlyList<SemanticFlowConfig> CreateFlows() =>
    [
        Inflow(EntityKind.Artifact, Attr.Condition, Verb.FactionMaintain, SemanticMechanism.Conversion, "target; conversion from faction.support", Target(EntityKind.Faction, Attr.Support)),
        Outflow(EntityKind.Artifact, Attr.Condition, Verb.ArtifactChannel, SemanticMechanism.Sacrifice, "self; sacrifice into power", Self(EntityKind.Artifact, Attr.Power)),
        Decay(EntityKind.Artifact, Attr.Condition),
        Inflow(EntityKind.Artifact, Attr.Power, Verb.ArtifactChannel, SemanticMechanism.Conversion, "self; conversion from condition", Self(EntityKind.Artifact, Attr.Condition)),
        Outflow(EntityKind.Artifact, Attr.Power, Verb.ArtifactEmpower, SemanticMechanism.Conversion, "self; conversion to faction.influence", Target(EntityKind.Faction, Attr.Influence)),
        Decay(EntityKind.Artifact, Attr.Power),
        Inflow(EntityKind.Artifact, Attr.Containment, Verb.ArtifactEnshrine, SemanticMechanism.Conversion, "self; conversion from settlement.prestige", Target(EntityKind.Settlement, Attr.Prestige)),
        Outflow(EntityKind.Artifact, Attr.Containment, Verb.ArtifactHeal, SemanticMechanism.Conversion, "self; conversion to NPC.vitality", Target(EntityKind.Npc, Attr.Vitality)),
        Decay(EntityKind.Artifact, Attr.Containment),

        Inflow(EntityKind.Faction, Attr.Cohesion, Verb.FactionTax, SemanticMechanism.Inflow, "self; inflow, rate modified by hex.wealth", Resource(Res.Wealth)),
        Decay(EntityKind.Faction, Attr.Cohesion),
        Inflow(EntityKind.Faction, Attr.Support, Verb.FactionRecruit, SemanticMechanism.Conversion, "self; conversion from settlement.residents", Target(EntityKind.Settlement, Attr.Residents)),
        Outflow(EntityKind.Faction, Attr.Support, Verb.FactionMaintain, SemanticMechanism.Conversion, "self; conversion to artifact.condition", Target(EntityKind.Artifact, Attr.Condition)),
        Outflow(EntityKind.Faction, Attr.Support, Verb.FactionPatrol, SemanticMechanism.Conversion, "self; conversion to settlement.security", Target(EntityKind.Settlement, Attr.Security)),
        Decay(EntityKind.Faction, Attr.Support),
        Inflow(EntityKind.Faction, Attr.Influence, Verb.ArtifactEmpower, SemanticMechanism.Conversion, "target; conversion from artifact.power", Target(EntityKind.Artifact, Attr.Power)),
        Outflow(EntityKind.Faction, Attr.Influence, Verb.FactionAttract, SemanticMechanism.Conversion, "self; conversion to NPC.ambition", Target(EntityKind.Npc, Attr.Ambition)),
        Decay(EntityKind.Faction, Attr.Influence),

        Inflow(EntityKind.Npc, Attr.Vitality, Verb.ArtifactHeal, SemanticMechanism.Conversion, "target; conversion", Target(EntityKind.Artifact, Attr.Containment)),
        Inflow(EntityKind.Npc, Attr.Vitality, Verb.RegionShelter, SemanticMechanism.Conversion, "target; conversion from region.regional_safety", Target(EntityKind.Region, Attr.RegionalSafety)),
        Outflow(EntityKind.Npc, Attr.Vitality, Verb.NpcEndure, SemanticMechanism.Sacrifice, "self; sacrifice to satiety", Self(EntityKind.Npc, Attr.Satiety)),
        Decay(EntityKind.Npc, Attr.Vitality),
        Inflow(EntityKind.Npc, Attr.Satiety, Verb.NpcEat, SemanticMechanism.Inflow, "self; inflow, rate modified by hex.food", Resource(Res.Food)),
        Inflow(EntityKind.Npc, Attr.Satiety, Verb.NpcEndure, SemanticMechanism.Sacrifice, "self; sacrifice from vitality", Self(EntityKind.Npc, Attr.Vitality)),
        Outflow(EntityKind.Npc, Attr.Satiety, Verb.NpcGuard, SemanticMechanism.Conversion, "self; conversion to settlement.security", Target(EntityKind.Settlement, Attr.Security)),
        Decay(EntityKind.Npc, Attr.Satiety),
        Inflow(EntityKind.Npc, Attr.Ambition, Verb.FactionAttract, SemanticMechanism.Conversion, "target; conversion from faction.influence", Target(EntityKind.Faction, Attr.Influence)),
        Outflow(EntityKind.Npc, Attr.Ambition, Verb.NpcCreateWorks, SemanticMechanism.Conversion, "self; conversion to settlement.prestige", Target(EntityKind.Settlement, Attr.Prestige)),
        Decay(EntityKind.Npc, Attr.Ambition),

        Inflow(EntityKind.Region, Attr.RegionalSafety, Verb.SettlementGarrison, SemanticMechanism.Conversion, "target; conversion from settlement.security", Target(EntityKind.Settlement, Attr.Security)),
        Outflow(EntityKind.Region, Attr.RegionalSafety, Verb.RegionSettle, SemanticMechanism.Conversion, "self; conversion to hex.population", Resource(Res.Population)),
        Outflow(EntityKind.Region, Attr.RegionalSafety, Verb.RegionShelter, SemanticMechanism.Conversion, "self; conversion to npc.vitality", Target(EntityKind.Npc, Attr.Vitality)),
        Decay(EntityKind.Region, Attr.RegionalSafety),
        Outflow(EntityKind.Region, Attr.GeologicStability, Verb.RegionIrrigate, SemanticMechanism.Conversion, "self; conversion to hex.food", Resource(Res.Food)),
        Decay(EntityKind.Region, Attr.GeologicStability),

        Inflow(EntityKind.Settlement, Attr.Residents, Verb.SettlementGrow, SemanticMechanism.Inflow, "self; inflow, rate modified by hex.population", Resource(Res.Population)),
        Inflow(EntityKind.Settlement, Attr.Residents, Verb.SettlementAttract, SemanticMechanism.Conversion, "self; conversion from target settlement", Target(EntityKind.Settlement, Attr.Residents)),
        Outflow(EntityKind.Settlement, Attr.Residents, Verb.SettlementAttract, SemanticMechanism.Conversion, "target; conversion to self settlement", Self(EntityKind.Settlement, Attr.Residents)),
        Outflow(EntityKind.Settlement, Attr.Residents, Verb.FactionRecruit, SemanticMechanism.Conversion, "target; conversion to faction.support", Target(EntityKind.Faction, Attr.Support)),
        Decay(EntityKind.Settlement, Attr.Residents),
        Inflow(EntityKind.Settlement, Attr.Security, Verb.NpcGuard, SemanticMechanism.Conversion, "target; conversion from NPC.satiety", Self(EntityKind.Npc, Attr.Satiety)),
        Inflow(EntityKind.Settlement, Attr.Security, Verb.FactionPatrol, SemanticMechanism.Conversion, "target; conversion from faction.support", Self(EntityKind.Faction, Attr.Support)),
        Outflow(EntityKind.Settlement, Attr.Security, Verb.SettlementGarrison, SemanticMechanism.Conversion, "self; conversion to region.regional_safety", Related(Rel.Contains, Attr.RegionalSafety, EntityKind.Region)),
        Decay(EntityKind.Settlement, Attr.Security),
        Inflow(EntityKind.Settlement, Attr.Prestige, Verb.NpcCreateWorks, SemanticMechanism.Conversion, "target; conversion from NPC.ambition", Self(EntityKind.Npc, Attr.Ambition)),
        Outflow(EntityKind.Settlement, Attr.Prestige, Verb.ArtifactEnshrine, SemanticMechanism.Conversion, "self; conversion to artifact.containment", Target(EntityKind.Artifact, Attr.Containment)),
        Outflow(EntityKind.Settlement, Attr.Prestige, Verb.SettlementTrade, SemanticMechanism.Conversion, "self; conversion to hex.wealth", Resource(Res.Wealth)),
        Decay(EntityKind.Settlement, Attr.Prestige),
    ];

    private static IReadOnlyList<SemanticModifierConfig> CreateModifiers() => [];

    private static IReadOnlyList<SemanticProcessReferenceConfig> CreateProcessReferences() =>
    [
        Read(Proc.SettlementAttract, EntityKind.Settlement, Attr.Residents),
        Read(Proc.NpcEat, EntityKind.Settlement, Attr.Residents),
        Read(Proc.NpcCreateWorks, EntityKind.Settlement, Attr.Residents),
        Read(Proc.FactionPatrol, EntityKind.Settlement, Attr.Residents),
        DirectWrite(Proc.SettlementGrow, EntityKind.Settlement, Attr.Residents),
        DirectWrite(Proc.SettlementAttract, EntityKind.Settlement, Attr.Residents),

        Read(Proc.NpcCreateWorks, EntityKind.Settlement, Attr.Security),
        Read(Proc.FactionPatrol, EntityKind.Settlement, Attr.Security),
        DirectWrite(Proc.FactionPatrol, EntityKind.Settlement, Attr.Security),

        Read(Proc.SettlementAttract, EntityKind.Settlement, Attr.Prestige),
        Read(Proc.NpcEat, EntityKind.Settlement, Attr.Prestige),
        Read(Proc.NpcCreateWorks, EntityKind.Settlement, Attr.Prestige),
        Read(Proc.FactionPatrol, EntityKind.Settlement, Attr.Prestige),
        DirectWrite(Proc.NpcEat, EntityKind.Settlement, Attr.Prestige),
        DirectWrite(Proc.NpcCreateWorks, EntityKind.Settlement, Attr.Prestige),

        Read(Proc.FactionPatrol, EntityKind.Faction, Attr.Support),
        DirectWrite(Proc.FactionPatrol, EntityKind.Faction, Attr.Support),

        Read(Proc.NpcEat, EntityKind.Npc, Attr.Satiety),
        Read(Proc.NpcWander, EntityKind.Npc, Attr.Satiety),
        DirectWrite(Proc.NpcEat, EntityKind.Npc, Attr.Satiety),
    ];

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
            RequiredFlowCoverage = stockClass == SemanticStockClass.Plane
                ? SemanticFlowCoverageRequirement.None
                : SemanticFlowCoverageRequirement.Both,
            BoundsLabel = boundsLabel,
            ConservationLabel = conservationLabel,
            Notes = notes,
        };

    // TODO: remove once every scalar stock has a real authored inflow.
    private static SemanticAttributeStockConfig StockOutflowOnly(
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
            RequiredFlowCoverage = SemanticFlowCoverageRequirement.Outflow,
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
