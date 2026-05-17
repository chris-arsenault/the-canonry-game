namespace Engine.Kernel;

using Engine.Constants;

internal sealed class CultureSlotProgramRuntime :
    IKernelProgramRuntime<CultureSlotProgramData, CultureSlotProgramState>
{
    public CultureSlotProgramRuntime(CultureSlotProgramData data, CultureSlotProgramState state)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        global::System.ArgumentNullException.ThrowIfNull(state);
        Data = data;
        State = state;
    }

    public CultureSlotProgramData Data { get; }

    public CultureSlotProgramState State { get; }

    public int RegisterSlot(
        int cultureId,
        int slotId,
        int polarityCode,
        float minimumScore,
        int minimumTenureTicks,
        float displacementMargin,
        float entityInfluenceMultiplierDelta,
        float cultureInfluenceMultiplierDelta) =>
        Data.RegisterSlot(
            cultureId,
            slotId,
            polarityCode,
            minimumScore,
            minimumTenureTicks,
            displacementMargin,
            entityInfluenceMultiplierDelta,
            cultureInfluenceMultiplierDelta);

    public void AddAllowedKind(int slot, int kindId) => Data.AddAllowedKind(slot, kindId);

    public void AddScoreRule(int slot, int signalCode, int relationshipKindId, float weight) =>
        Data.AddScoreRule(slot, signalCode, relationshipKindId, weight);

    public int OccupantEntityId(int cultureId, int slotId)
    {
        var slot = FindSlot(cultureId, slotId);
        return slot > 0 ? State.OccupantEntityId(slot) : 0;
    }

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        for (var slot = 1; slot <= Data.SlotCount; slot++)
        {
            TickSlot(simulation, slot, tick);
        }
    }

    public int SnapshotEventCount()
    {
        var count = 0;
        for (var slot = 1; slot <= Data.SlotCount; slot++)
        {
            if (State.OccupantEntityId(slot) > 0)
            {
                count++;
            }
        }

        return count;
    }

    public void EmitSnapshotEvents(Simulation simulation, int tick)
    {
        for (var slot = 1; slot <= Data.SlotCount; slot++)
        {
            var occupant = State.OccupantEntityId(slot);
            if (occupant <= 0)
            {
                continue;
            }

            simulation.EmitCultureSlotOccupancyChanged(
                tick,
                Data.CultureId(slot),
                Data.SlotId(slot),
                Data.PolarityCode(slot),
                previousEntityId: 0,
                currentEntityId: occupant,
                previousScore: 0f,
                currentScore: State.OccupantScore(slot));
        }
    }

    private void TickSlot(Simulation simulation, int slot, int tick)
    {
        var best = BestCandidate(simulation.World, slot);
        var currentEntityId = State.OccupantEntityId(slot);
        var currentScore = currentEntityId > 0 && simulation.World.EntityIsActive(currentEntityId)
            ? ScoreEntity(simulation.World, currentEntityId, slot)
            : 0f;

        if (best.EntityId <= 0 || best.Score < Data.MinimumScore(slot))
        {
            TryVacate(simulation, slot, currentEntityId, currentScore, tick);
            return;
        }

        if (currentEntityId == best.EntityId)
        {
            State.RefreshScore(slot, best.Score);
            return;
        }

        if (currentEntityId > 0 &&
            !CanDisplace(slot, currentScore, best.Score, tick))
        {
            State.RefreshScore(slot, currentScore);
            return;
        }

        ChangeOccupant(simulation, slot, currentEntityId, best.EntityId, currentScore, best.Score, tick);
    }

    private (int EntityId, float Score) BestCandidate(WorldState world, int slot)
    {
        var cultureId = Data.CultureId(slot);
        var bestEntityId = 0;
        var bestScore = 0f;
        for (var entityId = 1; entityId <= world.EntityHighWatermark; entityId++)
        {
            if (!CandidateAllowed(world, entityId, cultureId, slot))
            {
                continue;
            }

            var score = ScoreEntity(world, entityId, slot);
            if (score > bestScore ||
                (score == bestScore && bestEntityId > 0 && entityId < bestEntityId))
            {
                bestEntityId = entityId;
                bestScore = score;
            }
        }

        return (bestEntityId, bestScore);
    }

    private bool CandidateAllowed(WorldState world, int entityId, int cultureId, int slot)
    {
        return world.EntityIsActive(entityId) &&
            world.EntityCultureId(entityId) == cultureId &&
            Data.KindAllowed(slot, world.EntityKindId(entityId));
    }

    private float ScoreEntity(WorldState world, int entityId, int slot)
    {
        var score = 0f;
        for (var offset = 0; offset < Data.RuleCount(slot); offset++)
        {
            var rule = Data.RuleIndex(slot, offset);
            score += Data.RuleWeight(rule) * ScoreSignal(world, entityId, rule);
        }

        return score;
    }

    private float ScoreSignal(WorldState world, int entityId, int rule)
    {
        return Data.RuleSignalCode(rule) switch
        {
            CultureSlotScoreSignalCodes.ActiveEntity => 1f,
            CultureSlotScoreSignalCodes.PhysicalInfluence => SumInfluence(world, entityId, PlaneCodes.Physical),
            CultureSlotScoreSignalCodes.SemanticInfluence => SumInfluence(world, entityId, PlaneCodes.Semantic),
            CultureSlotScoreSignalCodes.PhysicalCoverage => world.CoverageOwnerCellCount(entityId, PlaneCodes.Physical),
            CultureSlotScoreSignalCodes.SemanticCoverage => world.CoverageOwnerCellCount(entityId, PlaneCodes.Semantic),
            CultureSlotScoreSignalCodes.OutgoingRelationshipStrength => SumRelationships(
                world,
                entityId,
                Data.RuleRelationshipKindId(rule),
                outgoing: true),
            CultureSlotScoreSignalCodes.IncomingRelationshipStrength => SumRelationships(
                world,
                entityId,
                Data.RuleRelationshipKindId(rule),
                outgoing: false),
            _ => 0f,
        };
    }

    private static float SumInfluence(WorldState world, int entityId, int planeCode)
    {
        var sum = 0f;
        var cursor = world.InfluenceCells(entityId, planeCode);
        while (world.MoveNextInfluenceCell(ref cursor, out _, out _, out var value))
        {
            sum += value;
        }

        return sum;
    }

    private static float SumRelationships(WorldState world, int entityId, int relationshipKindId, bool outgoing)
    {
        var sum = 0f;
        var cursor = outgoing
            ? world.OutgoingRelationshipsOfKind(entityId, relationshipKindId)
            : world.IncomingRelationshipsOfKind(entityId, relationshipKindId);
        while (world.MoveNextRelationship(ref cursor, out var relationshipId))
        {
            sum += world.RelationshipStrength(relationshipId);
        }

        return sum;
    }

    private bool CanDisplace(int slot, float currentScore, float challengerScore, int tick)
    {
        var tenure = tick - State.OccupiedAtTick(slot);
        if (tenure < Data.MinimumTenureTicks(slot))
        {
            return false;
        }

        var required = currentScore * (1f + Data.DisplacementMargin(slot));
        return challengerScore > required;
    }

    private void TryVacate(Simulation simulation, int slot, int currentEntityId, float currentScore, int tick)
    {
        if (currentEntityId <= 0)
        {
            return;
        }

        var tenure = tick - State.OccupiedAtTick(slot);
        if (tenure < Data.MinimumTenureTicks(slot))
        {
            State.RefreshScore(slot, currentScore);
            return;
        }

        ChangeOccupant(simulation, slot, currentEntityId, 0, currentScore, 0f, tick);
    }

    private void ChangeOccupant(
        Simulation simulation,
        int slot,
        int previousEntityId,
        int currentEntityId,
        float previousScore,
        float currentScore,
        int tick)
    {
        simulation.World.AdvanceWorldVersion();
        ApplyBenefitDelta(simulation, slot, previousEntityId, currentEntityId, tick);
        State.SetOccupant(slot, currentEntityId, currentScore, tick);
        simulation.EmitCultureSlotOccupancyChanged(
            tick,
            Data.CultureId(slot),
            Data.SlotId(slot),
            Data.PolarityCode(slot),
            previousEntityId,
            currentEntityId,
            previousScore,
            currentScore);
    }

    private void ApplyBenefitDelta(
        Simulation simulation,
        int slot,
        int previousEntityId,
        int currentEntityId,
        int tick)
    {
        var entityDelta = Data.EntityInfluenceMultiplierDelta(slot);
        if (previousEntityId > 0)
        {
            simulation.World.ModifyEntityCultureInfluenceMultiplierDelta(
                previousEntityId,
                -entityDelta,
                tick,
                simulation.ExecutionContext);
        }

        if (currentEntityId > 0)
        {
            simulation.World.ModifyEntityCultureInfluenceMultiplierDelta(
                currentEntityId,
                entityDelta,
                tick,
                simulation.ExecutionContext);
        }

        var wasOccupied = previousEntityId > 0;
        var isOccupied = currentEntityId > 0;
        if (wasOccupied == isOccupied)
        {
            return;
        }

        var cultureDelta = Data.CultureInfluenceMultiplierDelta(slot) * (isOccupied ? 1f : -1f);
        if (cultureDelta != 0f)
        {
            simulation.World.ModifyCultureInfluenceMultiplierDelta(
                Data.CultureId(slot),
                cultureDelta,
                tick,
                simulation.ExecutionContext);
        }
    }

    private int FindSlot(int cultureId, int slotId)
    {
        for (var slot = 1; slot <= Data.SlotCount; slot++)
        {
            if (Data.CultureId(slot) == cultureId && Data.SlotId(slot) == slotId)
            {
                return slot;
            }
        }

        return 0;
    }
}
