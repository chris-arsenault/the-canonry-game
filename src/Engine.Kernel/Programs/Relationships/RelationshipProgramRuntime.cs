namespace Engine.Kernel;

using Engine.Constants;

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

    public int RegisterRelationshipKind(
        int kindId,
        FixedScalar cullThreshold,
        bool targetFollowsSourcePhysical = false,
        bool isExclusive = false,
        bool isCultureAnchor = false) =>
        Data.RegisterRelationshipKind(kindId, cullThreshold, targetFollowsSourcePhysical, isExclusive, isCultureAnchor);

    public bool KindIsExclusive(int kindId) => Data.KindIsExclusive(kindId);

    public bool KindIsCultureAnchor(int kindId) => Data.IsCultureAnchor(kindId);

    public int AddMaintenanceRule(
        int kindId,
        int conditionCode,
        int planeCode,
        int maxDistance,
        FixedScalar threshold,
        FixedScalar delta) =>
        Data.AddMaintenanceRule(kindId, conditionCode, planeCode, maxDistance, threshold, delta);

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        var world = simulation.World;
        for (var relationshipId = world.FirstActiveRelationship(); relationshipId > 0;)
        {
            var nextRelationshipId = world.NextActiveRelationship(relationshipId);
            TickRelationship(simulation, world, relationshipId, tick);
            relationshipId = nextRelationshipId;
        }
    }

    public void NoteRelationshipCreated(int relationshipId) => State.Reset(relationshipId);

    public void NoteRelationshipCulled(int relationshipId) => State.Reset(relationshipId);

    private void TickRelationship(Simulation simulation, WorldState world, int relationshipId, int tick)
    {
        var relationshipKindId = world.RelationshipKindId(relationshipId);
        UpdateContactMemory(world, relationshipId, relationshipKindId, tick);
        var sourceId = world.RelationshipSourceId(relationshipId);
        var targetId = world.RelationshipTargetId(relationshipId);
        var sourceModifier = simulation.Knowledge.Modifier(
            world.EntityCultureId(sourceId),
            KnowledgeGrantDomainCodes.RelationshipMaintenance,
            relationshipKindId);
        var targetModifier = simulation.Knowledge.Modifier(
            world.EntityCultureId(targetId),
            KnowledgeGrantDomainCodes.RelationshipMaintenance,
            relationshipKindId);
        var modifier = FixedScalar.Max(sourceModifier, targetModifier);
        long deltaUnits = 0;
        for (var rule = Data.FirstMaintenanceRule(relationshipKindId);
             rule > 0;
             rule = Data.NextMaintenanceRule(rule))
        {
            var ruleDelta = ComputeRuleDelta(world, relationshipId, sourceId, targetId, rule);
            var adjustedDelta = ruleDelta > 0 ? ruleDelta * modifier : ruleDelta / modifier;
            deltaUnits = checked(
                deltaUnits + KernelFixedPoint.RelationshipStrengthDeltaUnits(adjustedDelta));
        }

        if (deltaUnits != 0)
        {
            simulation.ModifyRelationshipStrengthUnits(
                relationshipId,
                KernelFixedPoint.ClampToInt(
                    deltaUnits,
                    -KernelFixedPoint.RelationshipStrengthMaximumUnits,
                    KernelFixedPoint.RelationshipStrengthMaximumUnits),
                tick);
        }

        if (world.RelationshipStrength(relationshipId) <= Data.CullThreshold(world.RelationshipKindId(relationshipId)))
        {
            simulation.CullRelationship(relationshipId, tick);
        }
    }

    /// <summary>
    /// True when an active incoming follow-configured relationship governs
    /// this entity's physical position (it is being carried), so other
    /// programs can suspend their own movement of it.
    /// </summary>
    public bool TryResolveFollowSource(WorldState world, int entityId, out int sourceEntityId)
    {
        global::System.ArgumentNullException.ThrowIfNull(world);
        for (var index = 1; index <= Data.FollowKindCount; index++)
        {
            var cursor = world.IncomingRelationshipsOfKind(entityId, Data.FollowKindId(index));
            while (world.MoveNextRelationship(ref cursor, out var relationshipId))
            {
                var sourceId = world.RelationshipSourceId(relationshipId);
                if (world.EntityIsActive(sourceId))
                {
                    sourceEntityId = sourceId;
                    return true;
                }
            }
        }

        sourceEntityId = 0;
        return false;
    }

    private void UpdateContactMemory(WorldState world, int relationshipId, int relationshipKindId, int tick)
    {
        var sourceId = world.RelationshipSourceId(relationshipId);
        var targetId = world.RelationshipTargetId(relationshipId);
        var sawContactMemoryRule = false;
        for (var rule = Data.FirstMaintenanceRule(relationshipKindId);
             rule > 0;
             rule = Data.NextMaintenanceRule(rule))
        {
            if (Data.MaintenanceConditionCode(rule) != RelationshipMaintenanceConditionCodes.ContactMemory)
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

    private FixedScalar ComputeRuleDelta(
        WorldState world,
        int relationshipId,
        int sourceId,
        int targetId,
        int rule)
    {
        var evidence = Evidence(world, relationshipId, sourceId, targetId, rule);
        return evidence <= 0 ? 0 : Data.MaintenanceDelta(rule) * evidence;
    }

    private FixedScalar Evidence(
        WorldState world,
        int relationshipId,
        int sourceId,
        int targetId,
        int rule)
    {
        var condition = Data.MaintenanceConditionCode(rule);
        if (condition == RelationshipMaintenanceConditionCodes.Always)
        {
            return 1;
        }

        if (TryCultureEvidence(world, sourceId, targetId, condition, out var evidence) ||
            TryDistanceEvidence(world, sourceId, targetId, condition, rule, out evidence) ||
            TryInfluenceEvidence(world, sourceId, targetId, condition, rule, out evidence))
        {
            return evidence;
        }

        return condition == RelationshipMaintenanceConditionCodes.ContactMemory
            ? ContactEvidence(relationshipId, Data.MaintenanceThreshold(rule))
            : 0;
    }

    private static bool TryCultureEvidence(
        WorldState world,
        int sourceId,
        int targetId,
        int condition,
        out FixedScalar evidence)
    {
        if (condition == RelationshipMaintenanceConditionCodes.SameCulture)
        {
            evidence = world.EntityCultureId(sourceId) == world.EntityCultureId(targetId) ? 1 : 0;
            return true;
        }

        if (condition == RelationshipMaintenanceConditionCodes.DifferentCulture)
        {
            evidence = world.EntityCultureId(sourceId) != world.EntityCultureId(targetId) ? 1 : 0;
            return true;
        }

        evidence = 0;
        return false;
    }

    private bool TryDistanceEvidence(
        WorldState world,
        int sourceId,
        int targetId,
        int condition,
        int rule,
        out FixedScalar evidence)
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
        out FixedScalar evidence)
    {
        if (condition == RelationshipMaintenanceConditionCodes.PhysicalCoPresence)
        {
            evidence = PhysicalDistance(world, sourceId, targetId) == 0 ? 1 : 0;
            return true;
        }

        if (condition == RelationshipMaintenanceConditionCodes.SemanticCoPresence)
        {
            evidence = SemanticDistance(world, sourceId, targetId) == 0 ? 1 : 0;
            return true;
        }

        evidence = 0;
        return false;
    }

    private bool TryInfluenceEvidence(
        WorldState world,
        int sourceId,
        int targetId,
        int condition,
        int rule,
        out FixedScalar evidence)
    {
        if (condition == RelationshipMaintenanceConditionCodes.SourceInfluenceAtTarget)
        {
            var planeCode = InfluencePlaneCode(rule);
            if (planeCode == PlaneCodes.None)
            {
                evidence = 0;
                return true;
            }

            var sourceCultureId = world.EntityCultureId(sourceId);
            evidence = InfluenceEvidence(
                sourceCultureId <= 0
                    ? 0
                    : world.GetCellCultureInfluenceRaw(
                        sourceCultureId,
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
                evidence = 0;
                return true;
            }

            var targetCultureId = world.EntityCultureId(targetId);
            evidence = InfluenceEvidence(
                targetCultureId <= 0
                    ? 0
                    : world.GetCellCultureInfluenceRaw(
                        targetCultureId,
                        planeCode,
                        CurrentQ(world, sourceId, planeCode),
                        CurrentR(world, sourceId, planeCode)),
                Data.MaintenanceThreshold(rule));
            return true;
        }

        evidence = 0;
        return false;
    }

    private static FixedScalar DistanceEvidence(int distance, int maxDistance)
    {
        if (maxDistance <= 0)
        {
            return distance == 0 ? 1 : 0;
        }

        if (distance > maxDistance)
        {
            return 0;
        }

        return 1 - (distance / (FixedScalar)(maxDistance + 1));
    }

    private static FixedScalar InfluenceEvidence(FixedScalar influence, FixedScalar threshold)
    {
        if (threshold <= 0)
        {
            return influence > 0 ? 1 : 0;
        }

        return Clamp01(influence / threshold);
    }

    private FixedScalar ContactEvidence(int relationshipId, FixedScalar threshold)
    {
        if (threshold <= 0)
        {
            return State.ContactTicks(relationshipId) > 0 ? 1 : 0;
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

    private static FixedScalar Clamp01(FixedScalar value)
    {
        if (value < 0)
        {
            return 0;
        }

        return value > 1 ? 1 : value;
    }

}
