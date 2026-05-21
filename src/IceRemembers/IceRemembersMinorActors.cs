namespace IceRemembers;

using Engine.Constants;
using Engine.Domain.Config;

using K = IceRemembers.IceRemembersKinds;

public static partial class IceRemembersDefinition
{
    private static readonly Dictionary<EntityKind, int> CultureWorkerEmitCadences = new()
    {
        [K.Faction] = 65,
        [K.Settlement] = 70,
        [K.PointOfInterest] = 85,
        [K.Region] = 95,
        [K.Transport] = 80,
        [K.Occurrence] = 90,
    };

    private static readonly Dictionary<EntityKind, int> CultureWorkerTtls = new()
    {
        [K.Settlement] = 90,
        [K.Region] = 90,
        [K.Transport] = 75,
    };

    private static readonly Dictionary<EntityKind, float> CultureWorkerDeltas = new()
    {
        [K.Settlement] = 2.8f,
        [K.Faction] = 2.6f,
        [K.Region] = 2.4f,
        [K.PointOfInterest] = 2.2f,
        [K.Transport] = 1.8f,
        [K.Orca] = 1.7f,
        [K.Penguin] = 1.6f,
        [K.Artifact] = 1.6f,
        [K.Ability] = 1.5f,
        [K.Occurrence] = 1.7f,
    };

    private static List<MinorActorRuleConfig> CreateMinorActorRules()
    {
        var rules = new List<MinorActorRuleConfig>();
        foreach (var entity in IceRemembersEntities.All)
        {
            rules.Add(CultureWorker(
                entity.Kind,
                NonMobilePlaneCode(entity.PrimaryPlane),
                CultureWorkerEmitCadence(entity.Kind),
                CultureWorkerTtl(entity.Kind),
                CultureWorkerDelta(entity.Kind)));
        }

        rules.Add(RoadCrew(K.Transport));
        return rules;
    }

    private static MinorActorRuleConfig CultureWorker(
        EntityKind sourceKind,
        int planeCode,
        int emitCadence,
        int ttl,
        float workDelta) =>
        new()
        {
            SourceKind = sourceKind,
            PurposeCode = MinorActorPurposeCodes.CulturalLabor,
            ActorKindCode = MinorActorKindCodes.Worker,
            ActionCode = MinorActorActionCodes.CultureExpansion,
            PlaneCode = planeCode,
            TtlPulses = ttl,
            EmitCadencePulses = emitCadence,
            MaxActivePerSource = 1,
            SpawnRadius = 3,
            MoveChancePermille = 0,
            ActionCadencePulses = 1,
            WorkDelta = workDelta,
            Verbs = CultureWorkerVerbs(sourceKind, planeCode),
        };

    private static IReadOnlyList<MinorActorVerbConfig> CultureWorkerVerbs(EntityKind sourceKind, int planeCode) =>
    [
        ReinforceHomeMassVerb(sourceKind, planeCode),
        ExtendConnectedBorderVerb(sourceKind, planeCode),
        BridgeGapVerb(sourceKind, planeCode),
        ClearForeignTraceVerb(sourceKind, planeCode),
        SustainSourceLifecycleVerb(sourceKind, planeCode),
    ];

    private static MinorActorVerbConfig ReinforceHomeMassVerb(EntityKind sourceKind, int planeCode) =>
        CultureVerb(sourceKind, planeCode, "reinforce_home_mass", "Reinforce Home Mass",
            MinorActorVerbTargetingCodes.ReinforceHomeMass,
            radius: sourceKind == K.Faction ? 5 : 4,
            sampleCount: 96,
            weight: sourceKind == K.Faction ? 1.1f : 1.25f,
            workDeltaMultiplier: sourceKind == K.Settlement ? 1.2f : 1f);

    private static MinorActorVerbConfig ExtendConnectedBorderVerb(EntityKind sourceKind, int planeCode) =>
        CultureVerb(sourceKind, planeCode, "extend_connected_border", "Extend Connected Border",
            MinorActorVerbTargetingCodes.ExtendConnectedBorder,
            radius: sourceKind == K.Faction ? 6 : 5,
            sampleCount: 128,
            weight: sourceKind == K.Faction ? 1.3f : 0.95f,
            workDeltaMultiplier: 0.85f);

    private static MinorActorVerbConfig BridgeGapVerb(EntityKind sourceKind, int planeCode) =>
        CultureVerb(sourceKind, planeCode, "bridge_gap", "Bridge Gap",
            MinorActorVerbTargetingCodes.BridgeGap,
            radius: sourceKind == K.Faction ? 6 : 5,
            sampleCount: 96,
            weight: sourceKind == K.Faction ? 0.75f : 0.6f,
            workDeltaMultiplier: 0.7f);

    private static MinorActorVerbConfig ClearForeignTraceVerb(EntityKind sourceKind, int planeCode) =>
        new()
        {
            Id = MinorActorVerbId(sourceKind, planeCode, "clear_foreign_trace"),
            DisplayName = "Clear Foreign Trace",
            ActionCode = MinorActorActionCodes.CultureCleanup,
            TargetingCode = MinorActorVerbTargetingCodes.ReduceOtherCulture,
            PlaneCode = planeCode,
            Radius = sourceKind == K.Faction ? 5 : 4,
            SampleCount = 128,
            Weight = sourceKind == K.Faction ? 1.0f : 0.85f,
            WorkDeltaMultiplier = 0.9f,
        };

    private static MinorActorVerbConfig SustainSourceLifecycleVerb(EntityKind sourceKind, int planeCode) =>
        new()
        {
            Id = MinorActorVerbId(sourceKind, planeCode, "sustain_source_lifecycle"),
            DisplayName = "Sustain the Holding",
            ActionCode = MinorActorActionCodes.LifecycleMaintenance,
            TargetingCode = MinorActorVerbTargetingCodes.MaintainSourceLifecycle,
            PlaneCode = planeCode,
            Radius = sourceKind == K.Faction ? 5 : 4,
            SampleCount = 96,
            Weight = sourceKind == K.Faction ? 1.2f : 1.05f,
            WorkDeltaMultiplier = 1f,
        };

    private static MinorActorVerbConfig CultureVerb(
        EntityKind sourceKind,
        int planeCode,
        string suffix,
        string displayName,
        int targetingCode,
        int radius,
        int sampleCount,
        float weight,
        float workDeltaMultiplier) =>
        new()
        {
            Id = MinorActorVerbId(sourceKind, planeCode, suffix),
            DisplayName = displayName,
            ActionCode = MinorActorActionCodes.CultureExpansion,
            TargetingCode = targetingCode,
            PlaneCode = planeCode,
            Radius = radius,
            SampleCount = sampleCount,
            Weight = weight,
            WorkDeltaMultiplier = workDeltaMultiplier,
        };

    private static MinorActorRuleConfig RoadCrew(EntityKind sourceKind) =>
        new()
        {
            SourceKind = sourceKind,
            PurposeCode = MinorActorPurposeCodes.RoadLabor,
            ActorKindCode = MinorActorKindCodes.Worker,
            ActionCode = MinorActorActionCodes.RoadMaintenance,
            PlaneCode = PlaneCodes.Physical,
            TtlPulses = 75,
            EmitCadencePulses = 80,
            MaxActivePerSource = 1,
            SpawnRadius = 3,
            MoveChancePermille = 0,
            ActionCadencePulses = 1,
            WorkDelta = 1.6f,
            Verbs =
            [
                new MinorActorVerbConfig
                {
                    Id = "minor_transport_maintain_road",
                    DisplayName = "Maintain Road",
                    ActionCode = MinorActorActionCodes.RoadMaintenance,
                    TargetingCode = MinorActorVerbTargetingCodes.MaintainRoad,
                    PlaneCode = PlaneCodes.Physical,
                    Radius = 5,
                    SampleCount = 128,
                    Weight = 1.35f,
                    WorkDeltaMultiplier = 1f,
                },
                new MinorActorVerbConfig
                {
                    Id = "minor_transport_extend_route_culture",
                    DisplayName = "Extend Route Culture",
                    ActionCode = MinorActorActionCodes.CultureExpansion,
                    TargetingCode = MinorActorVerbTargetingCodes.ExtendConnectedBorder,
                    PlaneCode = PlaneCodes.Physical,
                    Radius = 4,
                    SampleCount = 96,
                    Weight = 0.65f,
                    WorkDeltaMultiplier = 4.5f,
                },
            ],
        };

    private static string MinorActorVerbId(EntityKind sourceKind, int planeCode, string suffix) =>
        $"minor_{sourceKind.Value}_{(planeCode == PlaneCodes.Semantic ? "semantic" : "physical")}_{suffix}";

    private static int NonMobilePlaneCode(PrimaryPlane primaryPlane) =>
        primaryPlane == PrimaryPlane.Physical
            ? PlaneCodes.Semantic
            : PlaneCodes.Physical;

    private static int CultureWorkerEmitCadence(EntityKind sourceKind)
        => CultureWorkerEmitCadences.TryGetValue(sourceKind, out var cadence) ? cadence : 100;

    private static int CultureWorkerTtl(EntityKind sourceKind)
        => CultureWorkerTtls.TryGetValue(sourceKind, out var ttl) ? ttl : 80;

    private static float CultureWorkerDelta(EntityKind sourceKind)
        => CultureWorkerDeltas.TryGetValue(sourceKind, out var delta) ? delta : 1.2f;
}
