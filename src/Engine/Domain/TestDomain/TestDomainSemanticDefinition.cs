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
            "Lifecycle anchor — artifact destroyed at 0. Maintained by custodian faction (support→condition). Channel sacrifices condition to raise power (intra-entity conversion, user's canonical example)."),
        Stock(EntityKind.Artifact, Attr.Power, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100",
            "free",
            "Channel converts condition into power. Devote converts NPC.Ideo·X into power (depletes NPC belief). Empower converts power into custodian faction.influence."),
        Stock(EntityKind.Artifact, Attr.Containment, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100",
            "free",
            "Enshrine converts settlement.prestige into containment. Depleted by heal events and passive emanation into region resonance."),
        Stock(EntityKind.Artifact, Attr.MysticResonance, SemanticStockClass.Plane, SemanticFeedbackClass.Unknown,
            "0..100 per bucket; X=Quiescent↔Resonant, Y=Corrupting↔Purifying",
            "plane (X/Y independent)",
            "Plane rows: 'Inflow'/'Outflow' = axis shift UP/DOWN. cleanse shifts Res·Y UP (mutual with NPC Ideo·Y rising). purify shifts Res·Y DOWN (toward Corrupting) — the purifier is tainted through use, releasing stability to the region. Res·X reserved extension space."),

        Stock(EntityKind.Faction, Attr.Cohesion, SemanticStockClass.Anchor, SemanticFeedbackClass.Anchor,
            "0..100",
            "anchor",
            "Lifecycle anchor — faction dissolves at 0. tax accrues cohesion at a rate modified by hex.wealth (hex not consumed). rally is plane→polar: depletes aligned NPC.Ideo·X to raise cohesion. radicalize is polar→plane: depletes cohesion, raises dominant ideology bucket."),
        Stock(EntityKind.Faction, Attr.Support, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100",
            "free",
            "Recruit converts settlement.residents into support (loyalty shift). Maintain spends support on custodian artifact upkeep. Patrol spends support to raise settlement security."),
        Stock(EntityKind.Faction, Attr.Influence, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100",
            "free",
            "Fueled by custodian artifact power (empower depletes artifact.power). Spent on attracting NPCs."),
        Stock(EntityKind.Faction, Attr.CivicIdeology, SemanticStockClass.Plane, SemanticFeedbackClass.Unknown,
            "0..100 per bucket; X=Collectivist↔Individualist, Y=Traditionalist↔Progressive",
            "plane (X/Y independent)",
            "Plane rows: 'Inflow'/'Outflow' = axis shift UP/DOWN. consecrate shifts faction Ideo·X UP (toward Individualist) while also shifting Region.Res·X UP (toward Resonant) — mutual UP on both sides. radicalize shifts Ideo·X DOWN (toward Collectivist), depleting cohesion. Indoctrinate shifts depend on partner state."),

        Stock(EntityKind.Npc, Attr.Vitality, SemanticStockClass.Anchor, SemanticFeedbackClass.Anchor,
            "0..100",
            "anchor",
            "Heart anchor — NPC dies at 0. npc_endure sacrifices vitality to refill satiety when hungry. Restored by artifact_heal (depletes containment) and region_shelter (depletes regional_safety)."),
        Stock(EntityKind.Npc, Attr.Satiety, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100",
            "free",
            "Hex.food is a non-depleting gate on eat (abundance signal, not stockpile)."),
        Stock(EntityKind.Npc, Attr.Ambition, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100",
            "free",
            "Low at spawn (20). Seeded by faction_attract. create_works spends ambition (burnout cost) to raise settlement.prestige."),
        Stock(EntityKind.Npc, Attr.CivicIdeology, SemanticStockClass.Plane, SemanticFeedbackClass.Unknown,
            "0..100 per bucket; X=Collectivist↔Individualist, Y=Traditionalist↔Progressive",
            "plane (X/Y independent)",
            "Plane rows: 'Inflow'/'Outflow' columns mean axis shift UP/DOWN, not stock gain/loss. attune and cleanse shift NPC values UP on their axes. rally and devote shift Ideo·X DOWN (toward Collectivist) as collectivist actions — these are plane→polar where the polar target (cohesion, power) rises. Propaganda/indoctrinate are context-dependent shifts toward partner state."),

        Stock(EntityKind.Region, Attr.RegionalSafety, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100",
            "free",
            "Aggregated upward from contained settlements. region_settle converts safety into hex.population (wilderness→populace). region_shelter converts safety into npc.vitality — sheltering NPCs attract corrupting essences, depleting safety."),
        Stock(EntityKind.Region, Attr.GeologicStability, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100",
            "free",
            "region_irrigate converts geologic_stability into hex.food. Only named inflow is artifact purification (high Res·Y); otherwise decays via engine_decay."),
        Stock(EntityKind.Region, Attr.MysticResonance, SemanticStockClass.Plane, SemanticFeedbackClass.Unknown,
            "0..100 per bucket; X=Quiescent↔Resonant, Y=Corrupting↔Purifying",
            "plane (X/Y independent)",
            "Plane rows: 'Inflow'/'Outflow' = axis shift UP/DOWN. Res·X shifts UP via consecrate and emanate (toward Resonant). Res·X shifts DOWN via attune (toward Quiescent) — attuning an NPC spends the land's resonance. Res·Y reserved extension space."),

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
        Stock(EntityKind.Settlement, Attr.CivicIdeology, SemanticStockClass.Plane, SemanticFeedbackClass.Unknown,
            "0..100 per bucket; X=Collectivist↔Individualist, Y=Traditionalist↔Progressive",
            "plane (X/Y independent)",
            "Plane rows: 'Inflow'/'Outflow' = axis shift UP/DOWN. Settlement participates only in shift verbs (propaganda with NPCs, indoctrinate with factions). Shift direction depends on partner state. Ideology modulates attract VOLUME (how many residents move)."),

        ResourceStock(Res.Food, "Ambient hex food abundance. Modifies NPC satiety intake; produced by region_irrigate. Not depleted by npc_eat — it is a non-consuming abundance signal."),
        ResourceStock(Res.Wealth, "Ambient hex wealth. Modifies faction_tax intake rate; produced by settlement_trade. Not depleted by tax — it is a non-consuming abundance signal."),
        ResourceStock(Res.Population, "Ambient hex population. Modifies settlement_grow intake rate; produced by region_settle (wilderness→populace). Not depleted by grow — it is a non-consuming abundance signal."),
    ];

    private static IReadOnlyList<SemanticVerbConfig> CreateVerbs() =>
    [
        EntityVerb(Verb.ArtifactChannel, EntityKind.Artifact),
        EntityVerb(Verb.ArtifactCleanse, EntityKind.Artifact),
        EntityVerb(Verb.ArtifactDevote, EntityKind.Artifact),
        EntityVerb(Verb.ArtifactEmanate, EntityKind.Artifact),
        EntityVerb(Verb.ArtifactEmpower, EntityKind.Artifact),
        EntityVerb(Verb.ArtifactEnshrine, EntityKind.Artifact),
        EntityVerb(Verb.ArtifactHeal, EntityKind.Artifact),
        EntityVerb(Verb.ArtifactPurify, EntityKind.Artifact),
        EngineVerb(Verb.EngineDecay),
        EntityVerb(Verb.FactionAttract, EntityKind.Faction),
        EntityVerb(Verb.FactionConsecrate, EntityKind.Faction),
        EntityVerb(Verb.FactionIndoctrinate, EntityKind.Faction),
        EntityVerb(Verb.FactionMaintain, EntityKind.Faction),
        EntityVerb(Verb.FactionPatrol, EntityKind.Faction, Proc.FactionPatrol),
        EntityVerb(Verb.FactionRadicalize, EntityKind.Faction),
        EntityVerb(Verb.FactionRally, EntityKind.Faction),
        EntityVerb(Verb.FactionRecruit, EntityKind.Faction),
        EntityVerb(Verb.FactionTax, EntityKind.Faction),
        EntityVerb(Verb.NpcCreateWorks, EntityKind.Npc, Proc.NpcCreateWorks),
        EntityVerb(Verb.NpcEat, EntityKind.Npc, Proc.NpcEat),
        EntityVerb(Verb.NpcEndure, EntityKind.Npc),
        EntityVerb(Verb.NpcGuard, EntityKind.Npc),
        EntityVerb(Verb.RegionAttune, EntityKind.Region),
        EntityVerb(Verb.RegionIrrigate, EntityKind.Region),
        EntityVerb(Verb.RegionSettle, EntityKind.Region),
        EntityVerb(Verb.RegionShelter, EntityKind.Region),
        EntityVerb(Verb.SettlementAttract, EntityKind.Settlement, Proc.SettlementAttract),
        EntityVerb(Verb.SettlementGarrison, EntityKind.Settlement),
        EntityVerb(Verb.SettlementGrow, EntityKind.Settlement, Proc.SettlementGrow),
        EntityVerb(Verb.SettlementPropaganda, EntityKind.Settlement, Proc.SettlementPropaganda),
        EntityVerb(Verb.SettlementTrade, EntityKind.Settlement),
    ];

    private static IReadOnlyList<SemanticFlowConfig> CreateFlows() =>
    [
        Inflow(EntityKind.Artifact, Attr.Condition, Verb.FactionMaintain, SemanticMechanism.Conversion, "target; conversion from faction.support", Target(EntityKind.Faction, Attr.Support)),
        Outflow(EntityKind.Artifact, Attr.Condition, Verb.ArtifactChannel, SemanticMechanism.Sacrifice, "self; sacrifice into power", Self(EntityKind.Artifact, Attr.Power)),
        Decay(EntityKind.Artifact, Attr.Condition),
        Inflow(EntityKind.Artifact, Attr.Power, Verb.ArtifactChannel, SemanticMechanism.Conversion, "self; conversion from condition", Self(EntityKind.Artifact, Attr.Condition)),
        Inflow(EntityKind.Artifact, Attr.Power, Verb.ArtifactDevote, SemanticMechanism.Conversion, "self; conversion from NPC.ideo_x", Target(EntityKind.Npc, Attr.CivicIdeology)),
        Outflow(EntityKind.Artifact, Attr.Power, Verb.ArtifactEmpower, SemanticMechanism.Conversion, "self; conversion to faction.influence", Target(EntityKind.Faction, Attr.Influence)),
        Decay(EntityKind.Artifact, Attr.Power),
        Inflow(EntityKind.Artifact, Attr.Containment, Verb.ArtifactEnshrine, SemanticMechanism.Conversion, "self; conversion from settlement.prestige", Target(EntityKind.Settlement, Attr.Prestige)),
        Outflow(EntityKind.Artifact, Attr.Containment, Verb.ArtifactHeal, SemanticMechanism.Conversion, "self; conversion to NPC.vitality", Target(EntityKind.Npc, Attr.Vitality)),
        Outflow(EntityKind.Artifact, Attr.Containment, Verb.ArtifactEmanate, SemanticMechanism.Emanation, "self; emanation to region.res_x", Target(EntityKind.Region, Attr.MysticResonance)),
        Decay(EntityKind.Artifact, Attr.Containment),
        Inflow(EntityKind.Artifact, Attr.MysticResonance, Verb.ArtifactCleanse, SemanticMechanism.Shift, "self; shifts Res·Y UP → Purifying; plane-plane mutual with NPC.Ideo·Y", SemanticPlaneAxis.Y, Target(EntityKind.Npc, Attr.CivicIdeology)),
        Outflow(EntityKind.Artifact, Attr.MysticResonance, Verb.ArtifactPurify, SemanticMechanism.Shift, "self; shifts Res·Y DOWN → Corrupting; plane→polar, raises region.geologic_stability", SemanticPlaneAxis.Y, Target(EntityKind.Region, Attr.GeologicStability)),

        Inflow(EntityKind.Faction, Attr.Cohesion, Verb.FactionTax, SemanticMechanism.Inflow, "self; inflow, rate modified by hex.wealth", Resource(Res.Wealth)),
        Inflow(EntityKind.Faction, Attr.Cohesion, Verb.FactionRally, SemanticMechanism.Conversion, "self; conversion from NPC.ideo_x", Target(EntityKind.Npc, Attr.CivicIdeology)),
        Outflow(EntityKind.Faction, Attr.Cohesion, Verb.FactionRadicalize, SemanticMechanism.Conversion, "self; conversion to ideology bucket", Self(EntityKind.Faction, Attr.CivicIdeology)),
        Decay(EntityKind.Faction, Attr.Cohesion),
        Inflow(EntityKind.Faction, Attr.Support, Verb.FactionRecruit, SemanticMechanism.Conversion, "self; conversion from settlement.residents", Target(EntityKind.Settlement, Attr.Residents)),
        Outflow(EntityKind.Faction, Attr.Support, Verb.FactionMaintain, SemanticMechanism.Conversion, "self; conversion to artifact.condition", Target(EntityKind.Artifact, Attr.Condition)),
        Outflow(EntityKind.Faction, Attr.Support, Verb.FactionPatrol, SemanticMechanism.Conversion, "self; conversion to settlement.security", Target(EntityKind.Settlement, Attr.Security)),
        Decay(EntityKind.Faction, Attr.Support),
        Inflow(EntityKind.Faction, Attr.Influence, Verb.ArtifactEmpower, SemanticMechanism.Conversion, "target; conversion from artifact.power", Target(EntityKind.Artifact, Attr.Power)),
        Outflow(EntityKind.Faction, Attr.Influence, Verb.FactionAttract, SemanticMechanism.Conversion, "self; conversion to NPC.ambition", Target(EntityKind.Npc, Attr.Ambition)),
        Decay(EntityKind.Faction, Attr.Influence),
        Inflow(EntityKind.Faction, Attr.CivicIdeology, Verb.FactionConsecrate, SemanticMechanism.Shift, "self; shifts Ideo·X UP → Individualist; plane-plane mutual with Region.Res·X", SemanticPlaneAxis.X, Target(EntityKind.Region, Attr.MysticResonance)),
        Inflow(EntityKind.Faction, Attr.CivicIdeology, Verb.FactionIndoctrinate, SemanticMechanism.Shift, "self/target; shift ↔ NPC/settlement", Target(EntityKind.Npc, Attr.CivicIdeology), Target(EntityKind.Settlement, Attr.CivicIdeology)),
        Outflow(EntityKind.Faction, Attr.CivicIdeology, Verb.FactionRadicalize, SemanticMechanism.Shift, "self; shifts Ideo·X DOWN → Collectivist; polar→plane, depletes cohesion", SemanticPlaneAxis.X, Self(EntityKind.Faction, Attr.Cohesion)),
        Outflow(EntityKind.Faction, Attr.CivicIdeology, Verb.FactionIndoctrinate, SemanticMechanism.Shift, "self/target; shift ↔ NPC/settlement", Target(EntityKind.Npc, Attr.CivicIdeology), Target(EntityKind.Settlement, Attr.CivicIdeology)),

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
        Inflow(EntityKind.Npc, Attr.CivicIdeology, Verb.RegionAttune, SemanticMechanism.Shift, "target; shifts Ideo·X UP → Individualist; plane-plane", SemanticPlaneAxis.X, Target(EntityKind.Region, Attr.MysticResonance)),
        Inflow(EntityKind.Npc, Attr.CivicIdeology, Verb.ArtifactCleanse, SemanticMechanism.Shift, "target; shifts Ideo·Y UP → Progressive; plane-plane mutual with Res·Y", SemanticPlaneAxis.Y, Target(EntityKind.Artifact, Attr.MysticResonance)),
        Inflow(EntityKind.Npc, Attr.CivicIdeology, Verb.SettlementPropaganda, SemanticMechanism.Shift, "target; shift ↔ NPC", Target(EntityKind.Settlement, Attr.CivicIdeology)),
        Inflow(EntityKind.Npc, Attr.CivicIdeology, Verb.FactionIndoctrinate, SemanticMechanism.Shift, "target; shift ↔ NPC", Target(EntityKind.Faction, Attr.CivicIdeology)),
        Outflow(EntityKind.Npc, Attr.CivicIdeology, Verb.FactionRally, SemanticMechanism.Shift, "target; shifts Ideo·X DOWN → Collectivist; plane→polar, raises faction.cohesion", SemanticPlaneAxis.X, Target(EntityKind.Faction, Attr.Cohesion)),
        Outflow(EntityKind.Npc, Attr.CivicIdeology, Verb.ArtifactDevote, SemanticMechanism.Shift, "target; shifts Ideo·X DOWN → Collectivist; plane→polar, raises artifact.power", SemanticPlaneAxis.X, Target(EntityKind.Artifact, Attr.Power)),
        Outflow(EntityKind.Npc, Attr.CivicIdeology, Verb.SettlementPropaganda, SemanticMechanism.Shift, "target; shift ↔ NPC", Target(EntityKind.Settlement, Attr.CivicIdeology)),
        Outflow(EntityKind.Npc, Attr.CivicIdeology, Verb.FactionIndoctrinate, SemanticMechanism.Shift, "target; shift ↔ NPC", Target(EntityKind.Faction, Attr.CivicIdeology)),

        Inflow(EntityKind.Region, Attr.RegionalSafety, Verb.SettlementGarrison, SemanticMechanism.Conversion, "target; conversion from settlement.security", Target(EntityKind.Settlement, Attr.Security)),
        Outflow(EntityKind.Region, Attr.RegionalSafety, Verb.RegionSettle, SemanticMechanism.Conversion, "self; conversion to hex.population", Resource(Res.Population)),
        Outflow(EntityKind.Region, Attr.RegionalSafety, Verb.RegionShelter, SemanticMechanism.Conversion, "self; conversion to npc.vitality", Target(EntityKind.Npc, Attr.Vitality)),
        Decay(EntityKind.Region, Attr.RegionalSafety),
        Inflow(EntityKind.Region, Attr.GeologicStability, Verb.ArtifactPurify, SemanticMechanism.Emanation, "target; emanation from artifact.res_y", Target(EntityKind.Artifact, Attr.MysticResonance)),
        Outflow(EntityKind.Region, Attr.GeologicStability, Verb.RegionIrrigate, SemanticMechanism.Conversion, "self; conversion to hex.food", Resource(Res.Food)),
        Decay(EntityKind.Region, Attr.GeologicStability),
        Inflow(EntityKind.Region, Attr.MysticResonance, Verb.FactionConsecrate, SemanticMechanism.Shift, "target; shifts Res·X UP → Resonant; plane-plane mutual with Faction.Ideo·X", SemanticPlaneAxis.X, Target(EntityKind.Faction, Attr.CivicIdeology)),
        Inflow(EntityKind.Region, Attr.MysticResonance, Verb.ArtifactEmanate, SemanticMechanism.Shift, "target; shifts Res·X UP → Resonant; polar→plane, depletes artifact.containment", SemanticPlaneAxis.X, Target(EntityKind.Artifact, Attr.Containment)),
        Outflow(EntityKind.Region, Attr.MysticResonance, Verb.RegionAttune, SemanticMechanism.Shift, "self; shifts Res·X DOWN → Quiescent; plane-plane, also shifts NPC.Ideo·X UP → Individualist", SemanticPlaneAxis.X, Target(EntityKind.Npc, Attr.CivicIdeology)),

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
        Inflow(EntityKind.Settlement, Attr.CivicIdeology, Verb.SettlementPropaganda, SemanticMechanism.Shift, "self/target; shift ↔ NPC", Target(EntityKind.Npc, Attr.CivicIdeology)),
        Inflow(EntityKind.Settlement, Attr.CivicIdeology, Verb.FactionIndoctrinate, SemanticMechanism.Shift, "target; shift ↔ faction", Target(EntityKind.Faction, Attr.CivicIdeology)),
        Outflow(EntityKind.Settlement, Attr.CivicIdeology, Verb.SettlementPropaganda, SemanticMechanism.Shift, "self/target; shift ↔ NPC", Target(EntityKind.Npc, Attr.CivicIdeology)),
        Outflow(EntityKind.Settlement, Attr.CivicIdeology, Verb.FactionIndoctrinate, SemanticMechanism.Shift, "target; shift ↔ faction", Target(EntityKind.Faction, Attr.CivicIdeology)),
    ];

    private static IReadOnlyList<SemanticModifierConfig> CreateModifiers() =>
    [
        Modifier(EntityKind.Settlement, Attr.CivicIdeology, Verb.SettlementAttract, SemanticModifierDirection.Positive, "volume modifier"),
    ];

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

        Read(Proc.SettlementPropaganda, EntityKind.Settlement, Attr.CivicIdeology),
        Read(Proc.SettlementAttract, EntityKind.Settlement, Attr.CivicIdeology),
        DirectWrite(Proc.SettlementPropaganda, EntityKind.Settlement, Attr.CivicIdeology),

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
