namespace Engine.Kernel;

internal sealed class RelationshipProgramRuntime :
    IKernelProgramRuntime<RelationshipProgramData, RelationshipProgramState>
{
    public RelationshipProgramRuntime(RelationshipProgramData data, RelationshipProgramState state)
        : this(data, state, new KernelProgramTuningOptions())
    {
    }

    public RelationshipProgramRuntime(
        RelationshipProgramData data,
        RelationshipProgramState state,
        KernelProgramTuningOptions tuning)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        global::System.ArgumentNullException.ThrowIfNull(state);
        global::System.ArgumentNullException.ThrowIfNull(tuning);
        Data = data;
        State = state;
        Tuning = tuning;
    }

    public RelationshipProgramData Data { get; }

    public RelationshipProgramState State { get; }

    public KernelProgramTuningOptions Tuning { get; }

    public int Count => Data.Count;

    public int RegisterRelationshipKind(int kindId, float cullThreshold, bool targetFollowsSourcePhysical = false) =>
        Data.RegisterRelationshipKind(kindId, cullThreshold, targetFollowsSourcePhysical);

    public int AddMaintenanceRule(
        int kindId,
        int conditionCode,
        int planeCode,
        int maxDistance,
        float threshold,
        float delta) =>
        Data.AddMaintenanceRule(kindId, conditionCode, planeCode, maxDistance, threshold, delta);

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        var world = simulation.World;
        for (var relationshipId = 1; relationshipId <= world.RelationshipHighWatermark; relationshipId++)
        {
            if (!world.RelationshipIsActive(relationshipId))
            {
                continue;
            }

            TickRelationship(simulation, world, relationshipId, tick);
        }
    }

    private void TickRelationship(Simulation simulation, WorldState world, int relationshipId, int tick)
    {
        var relationshipKindId = world.RelationshipKindId(relationshipId);
        UpdateContactMemory(world, relationshipId, relationshipKindId, tick);
        var sourceId = world.RelationshipSourceId(relationshipId);
        var targetId = world.RelationshipTargetId(relationshipId);
        var delta = 0f;
        for (var rule = 1; rule <= Data.MaintenanceRuleCount; rule++)
        {
            if (Data.MaintenanceKindId(rule) == relationshipKindId)
            {
                delta += ComputeRuleDelta(world, relationshipId, sourceId, targetId, rule);
            }
        }

        if (delta != 0f)
        {
            simulation.SetRelationshipStrength(relationshipId, world.RelationshipStrength(relationshipId) + delta, tick);
        }

        if (world.RelationshipStrength(relationshipId) <= Data.CullThreshold(world.RelationshipKindId(relationshipId)))
        {
            simulation.CullRelationship(relationshipId, tick);
            return;
        }

        if (Data.TargetFollowsSourcePhysical(relationshipKindId))
        {
            MoveTargetToSourcePhysical(simulation, world, sourceId, targetId, tick);
        }
    }

    private static void MoveTargetToSourcePhysical(Simulation simulation, WorldState world, int sourceId, int targetId, int tick)
    {
        if (!world.EntityIsActive(sourceId) || !world.EntityIsActive(targetId))
        {
            return;
        }

        var sourceQ = world.EntityPhysicalQ(sourceId);
        var sourceR = world.EntityPhysicalR(sourceId);
        if (world.EntityPhysicalQ(targetId) == sourceQ && world.EntityPhysicalR(targetId) == sourceR)
        {
            return;
        }

        simulation.Movement.Place(simulation, targetId, PlaneCodes.Physical, sourceQ, sourceR, tick, MovementCauseCodes.Follow);
    }

    private void UpdateContactMemory(WorldState world, int relationshipId, int relationshipKindId, int tick)
    {
        var sourceId = world.RelationshipSourceId(relationshipId);
        var targetId = world.RelationshipTargetId(relationshipId);
        var sawContactMemoryRule = false;
        for (var rule = 1; rule <= Data.MaintenanceRuleCount; rule++)
        {
            if (Data.MaintenanceKindId(rule) != relationshipKindId ||
                Data.MaintenanceConditionCode(rule) != RelationshipMaintenanceConditionCodes.ContactMemory)
            {
                continue;
            }

            sawContactMemoryRule = true;
            var planeCode = Data.MaintenancePlaneCode(rule);
            if (planeCode != PlaneCodes.None &&
                PlaneDistance(world, sourceId, targetId, planeCode) <= 1)
            {
                State.RecordContact(relationshipId, tick);
                return;
            }
        }

        if (!sawContactMemoryRule)
        {
            return;
        }

        State.DecayContact(relationshipId);
    }

    private float ComputeRuleDelta(
        WorldState world,
        int relationshipId,
        int sourceId,
        int targetId,
        int rule)
    {
        var evidence = Evidence(world, relationshipId, sourceId, targetId, rule);
        return evidence <= 0f ? 0f : Data.MaintenanceDelta(rule) * evidence;
    }

    private float Evidence(
        WorldState world,
        int relationshipId,
        int sourceId,
        int targetId,
        int rule)
    {
        var condition = Data.MaintenanceConditionCode(rule);
        if (condition == RelationshipMaintenanceConditionCodes.Always)
        {
            return 1f;
        }

        if (TryCultureEvidence(world, sourceId, targetId, condition, out var evidence) ||
            TryDistanceEvidence(world, sourceId, targetId, condition, rule, out evidence) ||
            TryInfluenceEvidence(world, sourceId, targetId, condition, rule, out evidence))
        {
            return evidence;
        }

        return condition == RelationshipMaintenanceConditionCodes.ContactMemory
            ? ContactEvidence(relationshipId, Data.MaintenanceThreshold(rule))
            : 0f;
    }

    private static bool TryCultureEvidence(
        WorldState world,
        int sourceId,
        int targetId,
        int condition,
        out float evidence)
    {
        if (condition == RelationshipMaintenanceConditionCodes.SameCulture)
        {
            evidence = world.EntityCultureId(sourceId) == world.EntityCultureId(targetId) ? 1f : 0f;
            return true;
        }

        if (condition == RelationshipMaintenanceConditionCodes.DifferentCulture)
        {
            evidence = world.EntityCultureId(sourceId) != world.EntityCultureId(targetId) ? 1f : 0f;
            return true;
        }

        evidence = 0f;
        return false;
    }

    private bool TryDistanceEvidence(
        WorldState world,
        int sourceId,
        int targetId,
        int condition,
        int rule,
        out float evidence)
    {
        if (condition == RelationshipMaintenanceConditionCodes.PhysicalDistanceWithin)
        {
            evidence = DistanceEvidence(PhysicalDistance(world, sourceId, targetId), Data.MaintenanceMaxDistance(rule));
            return true;
        }

        if (condition == RelationshipMaintenanceConditionCodes.SemanticDistanceWithin)
        {
            evidence = DistanceEvidence(SemanticDistance(world, sourceId, targetId), Data.MaintenanceMaxDistance(rule));
            return true;
        }

        return TryCoPresenceEvidence(world, sourceId, targetId, condition, out evidence);
    }

    private static bool TryCoPresenceEvidence(
        WorldState world,
        int sourceId,
        int targetId,
        int condition,
        out float evidence)
    {
        if (condition == RelationshipMaintenanceConditionCodes.PhysicalCoPresence)
        {
            evidence = PhysicalDistance(world, sourceId, targetId) == 0 ? 1f : 0f;
            return true;
        }

        if (condition == RelationshipMaintenanceConditionCodes.SemanticCoPresence)
        {
            evidence = SemanticDistance(world, sourceId, targetId) == 0 ? 1f : 0f;
            return true;
        }

        evidence = 0f;
        return false;
    }

    private bool TryInfluenceEvidence(
        WorldState world,
        int sourceId,
        int targetId,
        int condition,
        int rule,
        out float evidence)
    {
        if (condition == RelationshipMaintenanceConditionCodes.SourceInfluenceAtTarget)
        {
            var planeCode = InfluencePlaneCode(rule);
            if (planeCode == PlaneCodes.None)
            {
                evidence = 0f;
                return true;
            }

            evidence = InfluenceEvidence(
                world.GetCellInfluence(
                    sourceId,
                    planeCode,
                    CurrentQ(world, targetId, planeCode),
                    CurrentR(world, targetId, planeCode)),
                Data.MaintenanceThreshold(rule));
            return true;
        }

        if (condition == RelationshipMaintenanceConditionCodes.TargetInfluenceAtSource)
        {
            var planeCode = InfluencePlaneCode(rule);
            if (planeCode == PlaneCodes.None)
            {
                evidence = 0f;
                return true;
            }

            evidence = InfluenceEvidence(
                world.GetCellInfluence(
                    targetId,
                    planeCode,
                    CurrentQ(world, sourceId, planeCode),
                    CurrentR(world, sourceId, planeCode)),
                Data.MaintenanceThreshold(rule));
            return true;
        }

        evidence = 0f;
        return false;
    }

    private static float DistanceEvidence(int distance, int maxDistance)
    {
        if (maxDistance <= 0)
        {
            return distance == 0 ? 1f : 0f;
        }

        if (distance > maxDistance)
        {
            return 0f;
        }

        return 1f - (distance / (float)(maxDistance + 1));
    }

    private static float InfluenceEvidence(float influence, float threshold)
    {
        if (threshold <= 0f)
        {
            return influence > 0f ? 1f : 0f;
        }

        return Clamp01(influence / threshold);
    }

    private float ContactEvidence(int relationshipId, float threshold)
    {
        if (threshold <= 0f)
        {
            return State.ContactTicks(relationshipId) > 0 ? 1f : 0f;
        }

        return Clamp01(State.ContactTicks(relationshipId) / threshold);
    }

    private static int PhysicalDistance(WorldState world, int sourceId, int targetId) =>
        HexDistance(
            world.EntityPhysicalQ(sourceId),
            world.EntityPhysicalR(sourceId),
            world.EntityPhysicalQ(targetId),
            world.EntityPhysicalR(targetId));

    private static int SemanticDistance(WorldState world, int sourceId, int targetId)
    {
        var q = Abs(world.EntitySemanticQ(sourceId) - world.EntitySemanticQ(targetId));
        var r = Abs(world.EntitySemanticR(sourceId) - world.EntitySemanticR(targetId));
        var s = Abs(world.EntitySemanticS(sourceId) - world.EntitySemanticS(targetId));
        return Max(q, Max(r, s));
    }

    private int InfluencePlaneCode(int rule)
    {
        return Data.MaintenancePlaneCode(rule);
    }

    private static int PlaneDistance(WorldState world, int sourceId, int targetId, int planeCode) =>
        planeCode == PlaneCodes.Semantic
            ? SemanticDistance(world, sourceId, targetId)
            : PhysicalDistance(world, sourceId, targetId);

    private static int CurrentQ(WorldState world, int entityId, int planeCode) =>
        planeCode == PlaneCodes.Semantic ? world.EntitySemanticQ(entityId) : world.EntityPhysicalQ(entityId);

    private static int CurrentR(WorldState world, int entityId, int planeCode) =>
        planeCode == PlaneCodes.Semantic ? world.EntitySemanticR(entityId) : world.EntityPhysicalR(entityId);

    private static int HexDistance(int aq, int ar, int bq, int br)
    {
        var aS = -aq - ar;
        var bS = -bq - br;
        return (Abs(aq - bq) + Abs(ar - br) + Abs(aS - bS)) / 2;
    }

    private static int Abs(int value)
    {
        return value < 0 ? -value : value;
    }

    private static int Max(int left, int right) => left >= right ? left : right;

    private static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }

}
