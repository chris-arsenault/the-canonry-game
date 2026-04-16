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
            "Channel converts condition into power. Devote converts NPC.Ideo·X into power (depletes NPC belief). Empower is a non-depleting gate — power projects into custodian faction.influence without draining."),
        Stock(EntityKind.Artifact, Attr.Containment, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100",
            "free",
            "Enshrine refills containment via custodian settlement prestige (gate, non-depleting on settlement). Depleted by heal events and passive emanation into region resonance."),
        Stock(EntityKind.Artifact, Attr.MysticResonance, SemanticStockClass.Plane, SemanticFeedbackClass.Drift,
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
            "Recruit gates on settlement.residents without depleting them (dual allegiance). Maintain spends support on custodian artifact upkeep."),
        Stock(EntityKind.Faction, Attr.Influence, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100",
            "free",
            "Gated by custodian artifact power (non-depleting — artifact keeps its power while projecting influence). Spent on attracting NPCs and patrolling settlements."),
        Stock(EntityKind.Faction, Attr.CivicIdeology, SemanticStockClass.Plane, SemanticFeedbackClass.Drift,
            "0..100 per bucket; X=Collectivist↔Individualist, Y=Traditionalist↔Progressive",
            "plane (X/Y independent)",
            "Plane rows: 'Inflow'/'Outflow' = axis shift UP/DOWN. consecrate shifts faction Ideo·X UP (toward Individualist) while also shifting Region.Res·X UP (toward Resonant) — mutual UP on both sides. radicalize shifts Ideo·X DOWN (toward Collectivist), depleting cohesion. Indoctrinate drifts depend on partner state."),

        Stock(EntityKind.Npc, Attr.Vitality, SemanticStockClass.Anchor, SemanticFeedbackClass.Anchor,
            "0..100",
            "anchor",
            "Heart anchor — NPC dies at 0. npc_endure sacrifices vitality to refill satiety when hungry. Restored by artifact_heal and region_shelter."),
        Stock(EntityKind.Npc, Attr.Satiety, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100",
            "free",
            "Hex.food is a non-depleting gate on eat (abundance signal, not stockpile). create_works reads satiety as gate (threshold to fire) — ambition is the real cost (burnout)."),
        Stock(EntityKind.Npc, Attr.Ambition, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100",
            "free",
            "Low at spawn (20). Seeded by faction_attract. create_works spends ambition (burnout cost) to raise settlement.prestige; gated by satiety but doesn't consume it."),
        Stock(EntityKind.Npc, Attr.CivicIdeology, SemanticStockClass.Plane, SemanticFeedbackClass.Drift,
            "0..100 per bucket; X=Collectivist↔Individualist, Y=Traditionalist↔Progressive",
            "plane (X/Y independent)",
            "Plane rows: 'Inflow'/'Outflow' columns mean axis shift UP/DOWN, not stock gain/loss. attune and cleanse shift NPC values UP on their axes. rally and devote shift Ideo·X DOWN (toward Collectivist) as collectivist actions — these are plane→polar where the polar target (cohesion, power) rises. Propaganda/indoctrinate are context-dependent drifts toward partner state."),

        Stock(EntityKind.Region, Attr.RegionalSafety, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100",
            "free",
            "Aggregated upward from contained settlements. region_settle converts safety into hex.population (wilderness→populace). region_shelter gates on safety (non-depleting) to restore NPC vitality."),
        Stock(EntityKind.Region, Attr.GeologicStability, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100",
            "free",
            "region_irrigate converts geologic_stability into hex.food. Only named inflow is artifact purification (high Res·Y); otherwise drifts down via engine_decay."),
        Stock(EntityKind.Region, Attr.MysticResonance, SemanticStockClass.Plane, SemanticFeedbackClass.Drift,
            "0..100 per bucket; X=Quiescent↔Resonant, Y=Corrupting↔Purifying",
            "plane (X/Y independent)",
            "Plane rows: 'Inflow'/'Outflow' = axis shift UP/DOWN. Res·X shifts UP via consecrate and emanate (toward Resonant). Res·X shifts DOWN via attune (toward Quiescent) — attuning an NPC spends the land's resonance. Res·Y reserved extension space."),

        Stock(EntityKind.Settlement, Attr.Residents, SemanticStockClass.Anchor, SemanticFeedbackClass.Anchor,
            "0..∞",
            "anchor",
            "Lifecycle anchor — settlement dies at 0. grow produces residents at a rate modified by hex.population abundance (hex not consumed, not gated). attract redistributes residents between settlements. faction_recruit reads residents without depleting."),
        Stock(EntityKind.Settlement, Attr.Security, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100",
            "free",
            "Externally supplied by NPC guards (depletes satiety) and faction patrols (depletes influence). Depleted outward via settlement_garrison — security flows from settlement into containing region.regional_safety."),
        Stock(EntityKind.Settlement, Attr.Prestige, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100",
            "free",
            "Earned via NPC deeds (create_works depletes ambition). Gates four downstream verbs: trade (→hex.wealth), attract and grow (resident targeting), artifact_enshrine (→artifact.containment)."),
        Stock(EntityKind.Settlement, Attr.CivicIdeology, SemanticStockClass.Plane, SemanticFeedbackClass.Drift,
            "0..100 per bucket; X=Collectivist↔Individualist, Y=Traditionalist↔Progressive",
            "plane (X/Y independent)",
            "Plane rows: 'Inflow'/'Outflow' = axis shift UP/DOWN. Settlement participates only in drift verbs (propaganda with NPCs, indoctrinate with factions). Shift direction depends on partner state. Ideology modulates attract VOLUME (how many residents move)."),
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
        EntityVerb(Verb.RegionFormMilitia, EntityKind.Region, Proc.RegionFormMilitia),
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
        Inflow(EntityKind.Artifact, Attr.Power, Verb.ArtifactChannel, SemanticMechanism.Conversion, "self; conversion from condition", Self(EntityKind.Artifact, Attr.Condition)),
        Inflow(EntityKind.Artifact, Attr.Power, Verb.ArtifactDevote, SemanticMechanism.Conversion, "self; conversion from NPC.ideo_x", Target(EntityKind.Npc, Attr.CivicIdeology)),
        Outflow(EntityKind.Artifact, Attr.Power, Verb.ArtifactEmpower, SemanticMechanism.Gate, "self; gate to faction.influence", Target(EntityKind.Faction, Attr.Influence)),
        Inflow(EntityKind.Artifact, Attr.Containment, Verb.ArtifactEnshrine, SemanticMechanism.Gate, "self; gate from settlement.prestige", Target(EntityKind.Settlement, Attr.Prestige)),
        Outflow(EntityKind.Artifact, Attr.Containment, Verb.ArtifactHeal, SemanticMechanism.Conversion, "self; conversion to NPC.vitality", Target(EntityKind.Npc, Attr.Vitality)),
        Outflow(EntityKind.Artifact, Attr.Containment, Verb.ArtifactEmanate, SemanticMechanism.Emanation, "self; emanation to region.res_x", Target(EntityKind.Region, Attr.MysticResonance)),
        Inflow(EntityKind.Artifact, Attr.MysticResonance, Verb.ArtifactCleanse, SemanticMechanism.Shift, "self; shifts Res·Y UP → Purifying; plane-plane mutual with NPC.Ideo·Y", Target(EntityKind.Npc, Attr.CivicIdeology)),
        Outflow(EntityKind.Artifact, Attr.MysticResonance, Verb.ArtifactPurify, SemanticMechanism.Shift, "self; shifts Res·Y DOWN → Corrupting; plane→polar, raises region.geologic_stability", Target(EntityKind.Region, Attr.GeologicStability)),

        Inflow(EntityKind.Faction, Attr.Cohesion, Verb.FactionTax, SemanticMechanism.Inflow, "self; inflow, rate modified by hex.wealth", Resource(Res.Wealth)),
        Inflow(EntityKind.Faction, Attr.Cohesion, Verb.FactionRally, SemanticMechanism.Conversion, "self; conversion from NPC.ideo_x", Target(EntityKind.Npc, Attr.CivicIdeology)),
        Outflow(EntityKind.Faction, Attr.Cohesion, Verb.FactionRadicalize, SemanticMechanism.Conversion, "self; conversion to ideology bucket", Self(EntityKind.Faction, Attr.CivicIdeology)),
        Decay(EntityKind.Faction, Attr.Cohesion),
        Inflow(EntityKind.Faction, Attr.Support, Verb.FactionRecruit, SemanticMechanism.Gate, "self; gate from settlement.residents", Target(EntityKind.Settlement, Attr.Residents)),
        Outflow(EntityKind.Faction, Attr.Support, Verb.FactionMaintain, SemanticMechanism.Conversion, "self; conversion to artifact.condition", Target(EntityKind.Artifact, Attr.Condition)),
        Decay(EntityKind.Faction, Attr.Support),
        Inflow(EntityKind.Faction, Attr.Influence, Verb.ArtifactEmpower, SemanticMechanism.Gate, "target; gate from artifact.power", Target(EntityKind.Artifact, Attr.Power)),
        Outflow(EntityKind.Faction, Attr.Influence, Verb.FactionAttract, SemanticMechanism.Conversion, "self; conversion to NPC.ambition", Target(EntityKind.Npc, Attr.Ambition)),
        Outflow(EntityKind.Faction, Attr.Influence, Verb.FactionPatrol, SemanticMechanism.Conversion, "self; conversion to settlement.security", Target(EntityKind.Settlement, Attr.Security)),
        Decay(EntityKind.Faction, Attr.Influence),
        Inflow(EntityKind.Faction, Attr.CivicIdeology, Verb.FactionConsecrate, SemanticMechanism.Shift, "self; shifts Ideo·X UP → Individualist; plane-plane mutual with Region.Res·X", Target(EntityKind.Region, Attr.MysticResonance)),
        Inflow(EntityKind.Faction, Attr.CivicIdeology, Verb.FactionIndoctrinate, SemanticMechanism.Drift, "self/target; drift ↔ NPC/settlement", Target(EntityKind.Npc, Attr.CivicIdeology), Target(EntityKind.Settlement, Attr.CivicIdeology)),
        Outflow(EntityKind.Faction, Attr.CivicIdeology, Verb.FactionRadicalize, SemanticMechanism.Shift, "self; shifts Ideo·X DOWN → Collectivist; polar→plane, depletes cohesion", Self(EntityKind.Faction, Attr.Cohesion)),
        Outflow(EntityKind.Faction, Attr.CivicIdeology, Verb.FactionIndoctrinate, SemanticMechanism.Drift, "self/target; drift ↔ NPC/settlement", Target(EntityKind.Npc, Attr.CivicIdeology), Target(EntityKind.Settlement, Attr.CivicIdeology)),

        Inflow(EntityKind.Npc, Attr.Vitality, Verb.ArtifactHeal, SemanticMechanism.Conversion, "target; conversion", Target(EntityKind.Artifact, Attr.Containment)),
        Inflow(EntityKind.Npc, Attr.Vitality, Verb.RegionShelter, SemanticMechanism.Gate, "target; gate", Target(EntityKind.Region, Attr.RegionalSafety)),
        Outflow(EntityKind.Npc, Attr.Vitality, Verb.NpcEndure, SemanticMechanism.Sacrifice, "self; sacrifice to satiety", Self(EntityKind.Npc, Attr.Satiety)),
        Decay(EntityKind.Npc, Attr.Vitality),
        Inflow(EntityKind.Npc, Attr.Satiety, Verb.NpcEat, SemanticMechanism.Inflow, "self; inflow, rate modified by hex.food", Resource(Res.Food)),
        Inflow(EntityKind.Npc, Attr.Satiety, Verb.NpcEndure, SemanticMechanism.Sacrifice, "self; sacrifice from vitality", Self(EntityKind.Npc, Attr.Vitality)),
        Outflow(EntityKind.Npc, Attr.Satiety, Verb.NpcGuard, SemanticMechanism.Conversion, "self; conversion to settlement.security", Target(EntityKind.Settlement, Attr.Security)),
        Decay(EntityKind.Npc, Attr.Satiety),
        Inflow(EntityKind.Npc, Attr.Ambition, Verb.FactionAttract, SemanticMechanism.Conversion, "target; conversion from faction.influence", Target(EntityKind.Faction, Attr.Influence)),
        Outflow(EntityKind.Npc, Attr.Ambition, Verb.NpcCreateWorks, SemanticMechanism.Conversion, "self; conversion to settlement.prestige", Target(EntityKind.Settlement, Attr.Prestige)),
        Inflow(EntityKind.Npc, Attr.CivicIdeology, Verb.RegionAttune, SemanticMechanism.Shift, "target; shifts Ideo·X UP → Individualist; plane-plane", Target(EntityKind.Region, Attr.MysticResonance)),
        Inflow(EntityKind.Npc, Attr.CivicIdeology, Verb.ArtifactCleanse, SemanticMechanism.Shift, "target; shifts Ideo·Y UP → Progressive; plane-plane mutual with Res·Y", Target(EntityKind.Artifact, Attr.MysticResonance)),
        Inflow(EntityKind.Npc, Attr.CivicIdeology, Verb.SettlementPropaganda, SemanticMechanism.Drift, "target; drift ↔ NPC", Target(EntityKind.Settlement, Attr.CivicIdeology)),
        Inflow(EntityKind.Npc, Attr.CivicIdeology, Verb.FactionIndoctrinate, SemanticMechanism.Drift, "target; drift ↔ NPC", Target(EntityKind.Faction, Attr.CivicIdeology)),
        Outflow(EntityKind.Npc, Attr.CivicIdeology, Verb.FactionRally, SemanticMechanism.Shift, "target; shifts Ideo·X DOWN → Collectivist; plane→polar, raises faction.cohesion", Target(EntityKind.Faction, Attr.Cohesion)),
        Outflow(EntityKind.Npc, Attr.CivicIdeology, Verb.ArtifactDevote, SemanticMechanism.Shift, "target; shifts Ideo·X DOWN → Collectivist; plane→polar, raises artifact.power", Target(EntityKind.Artifact, Attr.Power)),
        Outflow(EntityKind.Npc, Attr.CivicIdeology, Verb.SettlementPropaganda, SemanticMechanism.Drift, "target; drift ↔ NPC", Target(EntityKind.Settlement, Attr.CivicIdeology)),
        Outflow(EntityKind.Npc, Attr.CivicIdeology, Verb.FactionIndoctrinate, SemanticMechanism.Drift, "target; drift ↔ NPC", Target(EntityKind.Faction, Attr.CivicIdeology)),

        Inflow(EntityKind.Region, Attr.RegionalSafety, Verb.SettlementGarrison, SemanticMechanism.Conversion, "target; conversion from settlement.security", Target(EntityKind.Settlement, Attr.Security)),
        Outflow(EntityKind.Region, Attr.RegionalSafety, Verb.RegionSettle, SemanticMechanism.Conversion, "self; conversion to hex.population", Resource(Res.Population)),
        Decay(EntityKind.Region, Attr.RegionalSafety),
        Inflow(EntityKind.Region, Attr.GeologicStability, Verb.ArtifactPurify, SemanticMechanism.Emanation, "target; emanation from artifact.res_y", Target(EntityKind.Artifact, Attr.MysticResonance)),
        Outflow(EntityKind.Region, Attr.GeologicStability, Verb.RegionIrrigate, SemanticMechanism.Conversion, "self; conversion to hex.food", Resource(Res.Food)),
        Decay(EntityKind.Region, Attr.GeologicStability),
        Inflow(EntityKind.Region, Attr.MysticResonance, Verb.FactionConsecrate, SemanticMechanism.Shift, "target; shifts Res·X UP → Resonant; plane-plane mutual with Faction.Ideo·X", Target(EntityKind.Faction, Attr.CivicIdeology)),
        Inflow(EntityKind.Region, Attr.MysticResonance, Verb.ArtifactEmanate, SemanticMechanism.Shift, "target; shifts Res·X UP → Resonant; polar→plane, depletes artifact.containment", Target(EntityKind.Artifact, Attr.Containment)),
        Outflow(EntityKind.Region, Attr.MysticResonance, Verb.RegionAttune, SemanticMechanism.Shift, "self; shifts Res·X DOWN → Quiescent; plane-plane, also shifts NPC.Ideo·X UP → Individualist", Target(EntityKind.Npc, Attr.CivicIdeology)),

        Inflow(EntityKind.Settlement, Attr.Residents, Verb.SettlementGrow, SemanticMechanism.Inflow, "self; inflow, rate modified by hex.population", Resource(Res.Population)),
        Inflow(EntityKind.Settlement, Attr.Residents, Verb.SettlementAttract, SemanticMechanism.Conversion, "self; conversion from target settlement", Target(EntityKind.Settlement, Attr.Residents)),
        Outflow(EntityKind.Settlement, Attr.Residents, Verb.SettlementAttract, SemanticMechanism.Conversion, "target; conversion to self settlement", Self(EntityKind.Settlement, Attr.Residents)),
        Decay(EntityKind.Settlement, Attr.Residents),
        Inflow(EntityKind.Settlement, Attr.Security, Verb.NpcGuard, SemanticMechanism.Conversion, "target; conversion from NPC.satiety", Self(EntityKind.Npc, Attr.Satiety)),
        Inflow(EntityKind.Settlement, Attr.Security, Verb.FactionPatrol, SemanticMechanism.Conversion, "target; conversion from faction.influence", Self(EntityKind.Faction, Attr.Influence)),
        Outflow(EntityKind.Settlement, Attr.Security, Verb.SettlementGarrison, SemanticMechanism.Conversion, "self; conversion to region.regional_safety", Related(Rel.Contains, Attr.RegionalSafety, EntityKind.Region)),
        Decay(EntityKind.Settlement, Attr.Security),
        Inflow(EntityKind.Settlement, Attr.Prestige, Verb.NpcCreateWorks, SemanticMechanism.Conversion, "target; conversion from NPC.ambition", Self(EntityKind.Npc, Attr.Ambition)),
        Decay(EntityKind.Settlement, Attr.Prestige),
        Inflow(EntityKind.Settlement, Attr.CivicIdeology, Verb.SettlementPropaganda, SemanticMechanism.Drift, "self/target; drift ↔ NPC", Target(EntityKind.Npc, Attr.CivicIdeology)),
        Inflow(EntityKind.Settlement, Attr.CivicIdeology, Verb.FactionIndoctrinate, SemanticMechanism.Drift, "target; drift ↔ faction", Target(EntityKind.Faction, Attr.CivicIdeology)),
        Outflow(EntityKind.Settlement, Attr.CivicIdeology, Verb.SettlementPropaganda, SemanticMechanism.Drift, "self/target; drift ↔ NPC", Target(EntityKind.Npc, Attr.CivicIdeology)),
        Outflow(EntityKind.Settlement, Attr.CivicIdeology, Verb.FactionIndoctrinate, SemanticMechanism.Drift, "target; drift ↔ faction", Target(EntityKind.Faction, Attr.CivicIdeology)),
    ];

    private static IReadOnlyList<SemanticModifierConfig> CreateModifiers() =>
    [
        Modifier(EntityKind.Npc, Attr.Satiety, Verb.NpcCreateWorks, SemanticModifierDirection.Positive, "gate"),
        Modifier(EntityKind.Region, Attr.RegionalSafety, Verb.RegionShelter, SemanticModifierDirection.Positive, "gate"),
        Modifier(EntityKind.Settlement, Attr.Residents, Verb.FactionRecruit, SemanticModifierDirection.Positive, "gate"),
        Modifier(EntityKind.Settlement, Attr.Prestige, Verb.SettlementTrade, SemanticModifierDirection.Positive, "gate"),
        Modifier(EntityKind.Settlement, Attr.Prestige, Verb.SettlementAttract, SemanticModifierDirection.Positive, "gate"),
        Modifier(EntityKind.Settlement, Attr.Prestige, Verb.SettlementGrow, SemanticModifierDirection.Positive, "gate"),
        Modifier(EntityKind.Settlement, Attr.Prestige, Verb.ArtifactEnshrine, SemanticModifierDirection.Positive, "gate"),
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
        IndirectWrite(Proc.RegionFormMilitia, EntityKind.Settlement, Attr.Security),

        Read(Proc.SettlementAttract, EntityKind.Settlement, Attr.Prestige),
        Read(Proc.NpcEat, EntityKind.Settlement, Attr.Prestige),
        Read(Proc.NpcCreateWorks, EntityKind.Settlement, Attr.Prestige),
        Read(Proc.FactionPatrol, EntityKind.Settlement, Attr.Prestige),
        DirectWrite(Proc.NpcEat, EntityKind.Settlement, Attr.Prestige),
        DirectWrite(Proc.NpcCreateWorks, EntityKind.Settlement, Attr.Prestige),

        Read(Proc.SettlementPropaganda, EntityKind.Settlement, Attr.CivicIdeology),
        Read(Proc.SettlementAttract, EntityKind.Settlement, Attr.CivicIdeology),
        DirectWrite(Proc.SettlementPropaganda, EntityKind.Settlement, Attr.CivicIdeology),

        Read(Proc.RegionFormMilitia, EntityKind.Region, Attr.RegionalSafety),
        DirectWrite(Proc.RegionFormMilitia, EntityKind.Region, Attr.RegionalSafety),
        IndirectWrite(Proc.FactionPatrol, EntityKind.Region, Attr.RegionalSafety),

        Read(Proc.RegionFormMilitia, EntityKind.Region, Attr.GeologicStability),

        Read(Proc.FactionPatrol, EntityKind.Faction, Attr.Support),
        DirectWrite(Proc.FactionPatrol, EntityKind.Faction, Attr.Support),
        IndirectWrite(Proc.NpcCreateWorks, EntityKind.Faction, Attr.Support),

        Read(Proc.FactionPatrol, EntityKind.Faction, Attr.Influence),
        DirectWrite(Proc.FactionPatrol, EntityKind.Faction, Attr.Influence),

        Read(Proc.NpcEat, EntityKind.Npc, Attr.Satiety),
        Read(Proc.NpcWander, EntityKind.Npc, Attr.Satiety),
        Read(Proc.NpcCreateWorks, EntityKind.Npc, Attr.Satiety),
        DirectWrite(Proc.NpcEat, EntityKind.Npc, Attr.Satiety),
        DirectWrite(Proc.NpcCreateWorks, EntityKind.Npc, Attr.Satiety),
    ];

    private static SemanticStockConfig Stock(
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
            BoundsLabel = boundsLabel,
            ConservationLabel = conservationLabel,
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
        Flow(kind, attribute, verb, SemanticFlowRole.Inflow, mechanism, signatureLabel, sourceRefs);

    private static SemanticFlowConfig Outflow(
        EntityKind kind,
        Attr attribute,
        Verb verb,
        SemanticMechanism mechanism,
        string signatureLabel,
        params AttributeRef[] sourceRefs) =>
        Flow(kind, attribute, verb, SemanticFlowRole.Outflow, mechanism, signatureLabel, sourceRefs);

    private static SemanticFlowConfig Decay(EntityKind kind, Attr attribute) =>
        new()
        {
            VerbId = VerbId(Verb.EngineDecay),
            Role = SemanticFlowRole.Outflow,
            TargetKind = kind,
            TargetAttribute = A(attribute),
            Mechanism = SemanticMechanism.Drift,
        };

    private static SemanticFlowConfig Flow(
        EntityKind kind,
        Attr attribute,
        Verb verb,
        SemanticFlowRole role,
        SemanticMechanism mechanism,
        string signatureLabel,
        params AttributeRef[] sourceRefs) =>
        new()
        {
            VerbId = VerbId(verb),
            Role = role,
            TargetKind = kind,
            TargetAttribute = A(attribute),
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
