namespace Engine.Kernel;

using Engine.Constants;

internal sealed class CultureSlotProgramRuntime :
    IKernelProgramRuntime<CultureSlotProgramData, CultureSlotProgramState>
{
    private const int AnyPolarityFilterCode = -1;
    private const float CultureSupportScoreScale = 30f;
    private const float InfluenceProductionScoreScale = 100f;

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

    public float OccupancyEvidence(
        int entityId,
        int cultureId,
        int slotId,
        int polarityCode,
        float threshold)
    {
        if (entityId <= 0)
        {
            return 0f;
        }

        var best = 0f;
        for (var slot = 1; slot <= Data.SlotCount; slot++)
        {
            if (!SlotMatchesOccupancyFilter(slot, entityId, cultureId, slotId, polarityCode))
            {
                continue;
            }

            var evidence = KernelQuery.ThresholdEvidence(State.OccupantScore(slot), threshold);
            if (evidence >= 1f)
            {
                return 1f;
            }

            if (evidence > best)
            {
                best = evidence;
            }
        }

        return best;
    }

    public void OccupancyEvidenceByPolarity(
        int entityId,
        int cultureId,
        float threshold,
        out float positive,
        out float neutral,
        out float negative)
    {
        positive = 0f;
        neutral = 0f;
        negative = 0f;
        if (entityId <= 0)
        {
            return;
        }

        for (var slot = 1; slot <= Data.SlotCount; slot++)
        {
            if (!SlotMatchesOccupancyFilter(slot, entityId, cultureId, slotId: 0, polarityCode: AnyPolarityFilterCode))
            {
                continue;
            }

            var evidence = KernelQuery.ThresholdEvidence(State.OccupantScore(slot), threshold);
            switch (Data.PolarityCode(slot))
            {
                case RelationshipPolarityCodes.Positive:
                    positive = global::System.MathF.Max(positive, evidence);
                    break;
                case RelationshipPolarityCodes.Neutral:
                    neutral = global::System.MathF.Max(neutral, evidence);
                    break;
                case RelationshipPolarityCodes.Negative:
                    negative = global::System.MathF.Max(negative, evidence);
                    break;
            }
        }
    }

    public int UnoccupiedSlotFitCount(int cultureId, int kindId)
    {
        if (cultureId <= 0 || kindId <= 0)
        {
            return 0;
        }

        var count = 0;
        for (var slot = 1; slot <= Data.SlotCount; slot++)
        {
            if (Data.CultureId(slot) == cultureId &&
                State.OccupantEntityId(slot) == 0 &&
                Data.KindAllowed(slot, kindId))
            {
                count++;
            }
        }

        return count;
    }

    private bool SlotMatchesOccupancyFilter(
        int slot,
        int entityId,
        int cultureId,
        int slotId,
        int polarityCode)
    {
        if (State.OccupantEntityId(slot) != entityId)
        {
            return false;
        }

        if (cultureId > 0 && Data.CultureId(slot) != cultureId)
        {
            return false;
        }

        if (slotId > 0 && Data.SlotId(slot) != slotId)
        {
            return false;
        }

        return polarityCode < 0 || Data.PolarityCode(slot) == polarityCode;
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

        if (best.EntityId <= 0 || best.Score < Data.MinimumScore(slot))
        {
            var vacatingScore = CurrentOccupantScore(simulation.World, currentEntityId, slot);
            TryVacate(simulation, slot, currentEntityId, vacatingScore, tick);
            return;
        }

        if (currentEntityId == best.EntityId)
        {
            State.RefreshScore(slot, best.Score);
            return;
        }

        var currentScore = CurrentOccupantScore(simulation.World, currentEntityId, slot);
        if (currentEntityId > 0 &&
            !CanDisplace(slot, currentScore, best.Score, tick))
        {
            State.RefreshScore(slot, currentScore);
            return;
        }

        ChangeOccupant(simulation, slot, currentEntityId, best.EntityId, currentScore, best.Score, tick);
    }

    private float CurrentOccupantScore(WorldState world, int currentEntityId, int slot)
    {
        return currentEntityId > 0 && world.EntityIsActive(currentEntityId)
            ? ScoreEntity(world, currentEntityId, slot)
            : 0f;
    }

    private (int EntityId, float Score) BestCandidate(WorldState world, int slot)
    {
        var cultureId = Data.CultureId(slot);
        var bestEntityId = 0;
        var bestScore = 0f;
        var allowedKindCount = Data.AllowedKindCount(slot);
        if (allowedKindCount == 0)
        {
            for (var entityId = world.FirstEntityOfCulture(cultureId);
                 entityId > 0;
                 entityId = world.NextEntityOfCulture(entityId))
            {
                ConsiderCandidate(world, slot, entityId, cultureId, ref bestEntityId, ref bestScore);
            }

            return (bestEntityId, bestScore);
        }

        for (var offset = 0; offset < allowedKindCount; offset++)
        {
            var kindId = Data.AllowedKindId(slot, offset);
            for (var entityId = world.FirstEntityOfKind(kindId);
                 entityId > 0;
                 entityId = world.NextEntityOfKind(entityId))
            {
                ConsiderCandidate(world, slot, entityId, cultureId, ref bestEntityId, ref bestScore);
            }
        }

        return (bestEntityId, bestScore);
    }

    private void ConsiderCandidate(
        WorldState world,
        int slot,
        int entityId,
        int cultureId,
        ref int bestEntityId,
        ref float bestScore)
    {
        if (!CandidateAllowed(world, entityId, cultureId))
        {
            return;
        }

        var score = ScoreEntity(world, entityId, slot);
        if (score > bestScore ||
            (score == bestScore && bestEntityId > 0 && entityId < bestEntityId))
        {
            bestEntityId = entityId;
            bestScore = score;
        }
    }

    private static bool CandidateAllowed(WorldState world, int entityId, int cultureId)
    {
        return world.EntityIsActive(entityId) &&
            world.EntityCultureId(entityId) == cultureId;
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
        var signalCode = Data.RuleSignalCode(rule);
        return signalCode switch
        {
            CultureSlotScoreSignalCodes.ActiveEntity => 1f,
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
            _ => ScorePlaneSignal(world, entityId, signalCode),
        };
    }

    private static float ScorePlaneSignal(WorldState world, int entityId, int signalCode)
    {
        return signalCode switch
        {
            CultureSlotScoreSignalCodes.PhysicalInfluence => SumInfluence(world, entityId, PlaneCodes.Physical),
            CultureSlotScoreSignalCodes.SemanticInfluence => SumInfluence(world, entityId, PlaneCodes.Semantic),
            CultureSlotScoreSignalCodes.PhysicalCoverage => world.CoverageOwnerCellCount(entityId, PlaneCodes.Physical),
            CultureSlotScoreSignalCodes.SemanticCoverage => world.CoverageOwnerCellCount(entityId, PlaneCodes.Semantic),
            CultureSlotScoreSignalCodes.PhysicalCultureSupport => CultureSupportScore(world, entityId, PlaneCodes.Physical),
            CultureSlotScoreSignalCodes.SemanticCultureSupport => CultureSupportScore(world, entityId, PlaneCodes.Semantic),
            CultureSlotScoreSignalCodes.PhysicalInfluenceProduction => InfluenceProductionScore(world, entityId, PlaneCodes.Physical),
            CultureSlotScoreSignalCodes.SemanticInfluenceProduction => InfluenceProductionScore(world, entityId, PlaneCodes.Semantic),
            _ => 0f,
        };
    }

    private static float CultureSupportScore(WorldState world, int entityId, int planeCode)
    {
        var cultureId = world.EntityCultureId(entityId);
        if (cultureId <= 0)
        {
            return 0f;
        }

        var q = planeCode == PlaneCodes.Semantic ? world.EntitySemanticQ(entityId) : world.EntityPhysicalQ(entityId);
        var r = planeCode == PlaneCodes.Semantic ? world.EntitySemanticR(entityId) : world.EntityPhysicalR(entityId);
        var field = world.CultureFieldAt(cultureId, planeCode, q, r);
        return global::System.MathF.Min(10f, field.ConnectedRawMass / CultureSupportScoreScale);
    }

    private static float InfluenceProductionScore(WorldState world, int entityId, int planeCode)
    {
        return global::System.MathF.Min(
            10f,
            world.EntityInfluenceProduction(entityId, planeCode) / InfluenceProductionScoreScale);
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
