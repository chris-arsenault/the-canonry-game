namespace Engine.Kernel;

using Engine.Constants;

internal sealed class CultureSlotProgramRuntime :
    IKernelProgramRuntime<CultureSlotProgramData, CultureSlotProgramState>
{
    private const int AnyPolarityFilterCode = -1;
    private static readonly FixedScalar CultureSupportScoreScale = 30;
    private static readonly FixedScalar InfluenceProductionScoreScale = 100;
    private readonly long[] _seenCultureWorkRevisions;
    private readonly int[] _nextDueTicks;
    private readonly int[] _publishedOccupantEntityIds;
    private readonly FixedScalar[] _publishedOccupantScores;

    public CultureSlotProgramRuntime(CultureSlotProgramData data, CultureSlotProgramState state)
        : this(data, state, new KernelProgramTuningOptions())
    {
    }

    public CultureSlotProgramRuntime(
        CultureSlotProgramData data,
        CultureSlotProgramState state,
        KernelProgramTuningOptions tuning)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        global::System.ArgumentNullException.ThrowIfNull(state);
        global::System.ArgumentNullException.ThrowIfNull(tuning);
        Data = data;
        State = state;
        Tuning = tuning;
        _seenCultureWorkRevisions = new long[data.SlotCapacity + 1];
        _nextDueTicks = new int[data.SlotCapacity + 1];
        _publishedOccupantEntityIds =
            new int[data.SlotCapacity + 1];
        _publishedOccupantScores =
            new FixedScalar[data.SlotCapacity + 1];
        global::System.Array.Fill(_seenCultureWorkRevisions, -1);
        global::System.Array.Fill(_nextDueTicks, int.MaxValue);
    }

    public CultureSlotProgramData Data { get; }

    public CultureSlotProgramState State { get; }

    public KernelProgramTuningOptions Tuning { get; }

    public int RegisterSlot(
        int cultureId,
        int slotId,
        int polarityCode,
        FixedScalar minimumScore,
        int minimumTenureTicks,
        FixedScalar displacementMargin,
        FixedScalar entityInfluenceMultiplierDelta,
        FixedScalar cultureInfluenceMultiplierDelta) =>
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

    public void AddScoreRule(int slot, int signalCode, int relationshipKindId, FixedScalar weight) =>
        Data.AddScoreRule(slot, signalCode, relationshipKindId, weight);

    public int OccupantEntityId(int cultureId, int slotId)
    {
        var slot = FindSlot(cultureId, slotId);
        return slot > 0 ? ObservedOccupantEntityId(slot) : 0;
    }

    internal void CapturePublishedView()
    {
        for (var slot = 1; slot <= Data.SlotCount; slot++)
        {
            _publishedOccupantEntityIds[slot] =
                State.OccupantEntityId(slot);
            _publishedOccupantScores[slot] =
                State.OccupantScore(slot);
        }
    }

    public FixedScalar OccupancyEvidence(
        int entityId,
        int cultureId,
        int slotId,
        int polarityCode,
        FixedScalar threshold)
    {
        if (entityId <= 0)
        {
            return 0;
        }

        var best = FixedScalar.Zero;
        for (var slot = 1; slot <= Data.SlotCount; slot++)
        {
            if (!SlotMatchesOccupancyFilter(slot, entityId, cultureId, slotId, polarityCode))
            {
                continue;
            }

            var evidence = KernelQuery.ThresholdEvidence(
                ObservedOccupantScore(slot),
                threshold);
            if (evidence >= 1)
            {
                return 1;
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
        FixedScalar threshold,
        out FixedScalar positive,
        out FixedScalar neutral,
        out FixedScalar negative)
    {
        positive = 0;
        neutral = 0;
        negative = 0;
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

            var evidence = KernelQuery.ThresholdEvidence(
                ObservedOccupantScore(slot),
                threshold);
            switch (Data.PolarityCode(slot))
            {
                case RelationshipPolarityCodes.Positive:
                    positive = FixedScalar.Max(positive, evidence);
                    break;
                case RelationshipPolarityCodes.Neutral:
                    neutral = FixedScalar.Max(neutral, evidence);
                    break;
                case RelationshipPolarityCodes.Negative:
                    negative = FixedScalar.Max(negative, evidence);
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
                ObservedOccupantEntityId(slot) == 0 &&
                Data.KindAllowed(slot, kindId))
            {
                count++;
            }
        }

        return count;
    }

    public int CausalSpendLimitMultiplier(int entityId)
    {
        if (entityId <= 0)
        {
            return 1;
        }

        for (var slot = 1; slot <= Data.SlotCount; slot++)
        {
            if (ObservedOccupantEntityId(slot) == entityId)
            {
                return 2;
            }
        }

        return 1;
    }

    private bool SlotMatchesOccupancyFilter(
        int slot,
        int entityId,
        int cultureId,
        int slotId,
        int polarityCode)
    {
        if (ObservedOccupantEntityId(slot) != entityId)
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

    private int ObservedOccupantEntityId(int slot) =>
        KernelWorkerThread.OwnerIndex >= 0
            ? _publishedOccupantEntityIds[slot]
            : State.OccupantEntityId(slot);

    private FixedScalar ObservedOccupantScore(int slot) =>
        KernelWorkerThread.OwnerIndex >= 0
            ? _publishedOccupantScores[slot]
            : State.OccupantScore(slot);

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        for (var slot = 1; slot <= Data.SlotCount; slot++)
        {
            var cultureId = Data.CultureId(slot);
            var revision = simulation.World.CultureWorkRevision(cultureId);
            if (_seenCultureWorkRevisions[slot] == revision &&
                tick < _nextDueTicks[slot])
            {
                continue;
            }

            _nextDueTicks[slot] = int.MaxValue;
            TickSlot(simulation, slot, tick);
            _seenCultureWorkRevisions[slot] =
                simulation.World.CultureWorkRevision(cultureId);
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
                previousScore: 0,
                currentScore: State.OccupantScore(slot));
        }
    }

    internal void NoteEntityRetired(Simulation simulation, int entityId, int tick)
    {
        for (var slot = 1; slot <= Data.SlotCount; slot++)
        {
            if (State.OccupantEntityId(slot) != entityId)
            {
                continue;
            }

            ChangeOccupant(
                simulation,
                slot,
                entityId,
                currentEntityId: 0,
                State.OccupantScore(slot),
                currentScore: 0,
                tick);
        }
    }

    private void TickSlot(Simulation simulation, int slot, int tick)
    {
        var best = BestCandidate(simulation, slot);
        var currentEntityId = State.OccupantEntityId(slot);

        if (best.EntityId <= 0 || best.Score < Data.MinimumScore(slot))
        {
            var vacatingScore = CurrentOccupantScore(simulation, currentEntityId, slot);
            TryVacate(simulation, slot, currentEntityId, vacatingScore, tick);
            return;
        }

        if (currentEntityId == best.EntityId)
        {
            State.RefreshScore(slot, best.Score);
            return;
        }

        var currentScore = CurrentOccupantScore(simulation, currentEntityId, slot);
        if (currentEntityId > 0)
        {
            var tenureDueTick = State.OccupiedAtTick(slot) + Data.MinimumTenureTicks(slot);
            if (tick < tenureDueTick)
            {
                _nextDueTicks[slot] = tenureDueTick;
                State.RefreshScore(slot, currentScore);
                return;
            }

            if (!CanDisplace(slot, currentScore, best.Score))
            {
                State.RefreshScore(slot, currentScore);
                return;
            }
        }

        ChangeOccupant(simulation, slot, currentEntityId, best.EntityId, currentScore, best.Score, tick);
    }

    private FixedScalar CurrentOccupantScore(Simulation simulation, int currentEntityId, int slot)
    {
        var world = simulation.World;
        return currentEntityId > 0 && world.EntityIsActive(currentEntityId)
            ? ScoreEntity(simulation, currentEntityId, slot)
            : 0;
    }

    private (int EntityId, FixedScalar Score) BestCandidate(Simulation simulation, int slot)
    {
        var world = simulation.World;
        var cultureId = Data.CultureId(slot);
        var bestEntityId = 0;
        var bestScore = FixedScalar.Zero;
        var allowedKindCount = Data.AllowedKindCount(slot);
        if (allowedKindCount == 0)
        {
            for (var entityId = world.FirstEntityOfCulture(cultureId);
                 entityId > 0;
                 entityId = world.NextEntityOfCulture(entityId))
            {
                ConsiderCandidate(simulation, slot, entityId, cultureId, ref bestEntityId, ref bestScore);
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
                ConsiderCandidate(simulation, slot, entityId, cultureId, ref bestEntityId, ref bestScore);
            }
        }

        return (bestEntityId, bestScore);
    }

    private void ConsiderCandidate(
        Simulation simulation,
        int slot,
        int entityId,
        int cultureId,
        ref int bestEntityId,
        ref FixedScalar bestScore)
    {
        var world = simulation.World;
        if (!CandidateAllowed(world, entityId, cultureId))
        {
            return;
        }

        var score = ScoreEntity(simulation, entityId, slot);
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

    private FixedScalar ScoreEntity(Simulation simulation, int entityId, int slot)
    {
        var score = FixedScalar.Zero;
        for (var offset = 0; offset < Data.RuleCount(slot); offset++)
        {
            var rule = Data.RuleIndex(slot, offset);
            score += Data.RuleWeight(rule) * ScoreSignal(simulation, entityId, rule);
        }

        return score;
    }

    private FixedScalar ScoreSignal(Simulation simulation, int entityId, int rule)
    {
        var signalCode = Data.RuleSignalCode(rule);
        var world = simulation.World;
        return signalCode switch
        {
            CultureSlotScoreSignalCodes.ActiveEntity => 1,
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
            _ => ScorePlaneSignal(simulation, entityId, signalCode),
        };
    }

    private static FixedScalar ScorePlaneSignal(Simulation simulation, int entityId, int signalCode)
    {
        var world = simulation.World;
        return signalCode switch
        {
            CultureSlotScoreSignalCodes.PhysicalInfluence => CultureSupportScore(simulation, entityId, PlaneCodes.Physical),
            CultureSlotScoreSignalCodes.SemanticInfluence => CultureSupportScore(simulation, entityId, PlaneCodes.Semantic),
            CultureSlotScoreSignalCodes.PhysicalCoverage => world.CoverageOwnerCellCount(entityId, PlaneCodes.Physical),
            CultureSlotScoreSignalCodes.SemanticCoverage => world.CoverageOwnerCellCount(entityId, PlaneCodes.Semantic),
            CultureSlotScoreSignalCodes.PhysicalCultureSupport => CultureSupportScore(simulation, entityId, PlaneCodes.Physical),
            CultureSlotScoreSignalCodes.SemanticCultureSupport => CultureSupportScore(simulation, entityId, PlaneCodes.Semantic),
            CultureSlotScoreSignalCodes.PhysicalInfluenceProduction => InfluenceProductionScore(world, entityId, PlaneCodes.Physical),
            CultureSlotScoreSignalCodes.SemanticInfluenceProduction => InfluenceProductionScore(world, entityId, PlaneCodes.Semantic),
            _ => 0,
        };
    }

    private static FixedScalar CultureSupportScore(Simulation simulation, int entityId, int planeCode)
    {
        var world = simulation.World;
        var cultureId = world.EntityCultureId(entityId);
        if (cultureId <= 0)
        {
            return 0;
        }

        var q = planeCode == PlaneCodes.Semantic ? world.EntitySemanticQ(entityId) : world.EntityPhysicalQ(entityId);
        var r = planeCode == PlaneCodes.Semantic ? world.EntitySemanticR(entityId) : world.EntityPhysicalR(entityId);
        var boundary = simulation.CultureLocality.CellAt(world, cultureId, planeCode, q, r);
        return FixedScalar.Min(10, boundary.ConnectedRawMass / CultureSupportScoreScale);
    }

    private static FixedScalar InfluenceProductionScore(WorldState world, int entityId, int planeCode)
    {
        return FixedScalar.Min(
            10,
            world.EntityInfluenceProduction(entityId, planeCode) / InfluenceProductionScoreScale);
    }

    private static FixedScalar SumRelationships(WorldState world, int entityId, int relationshipKindId, bool outgoing)
    {
        var sum = FixedScalar.Zero;
        var cursor = outgoing
            ? world.OutgoingRelationshipsOfKind(entityId, relationshipKindId)
            : world.IncomingRelationshipsOfKind(entityId, relationshipKindId);
        while (world.MoveNextRelationship(ref cursor, out var relationshipId))
        {
            sum += world.RelationshipStrength(relationshipId);
        }

        return sum;
    }

    private bool CanDisplace(int slot, FixedScalar currentScore, FixedScalar challengerScore)
    {
        var required = currentScore * (1 + Data.DisplacementMargin(slot));
        return challengerScore > required;
    }

    private void TryVacate(Simulation simulation, int slot, int currentEntityId, FixedScalar currentScore, int tick)
    {
        if (currentEntityId <= 0)
        {
            return;
        }

        var tenureDueTick = State.OccupiedAtTick(slot) + Data.MinimumTenureTicks(slot);
        if (tick < tenureDueTick)
        {
            _nextDueTicks[slot] = tenureDueTick;
            State.RefreshScore(slot, currentScore);
            return;
        }

        ChangeOccupant(simulation, slot, currentEntityId, 0, currentScore, 0, tick);
    }

    private void ChangeOccupant(
        Simulation simulation,
        int slot,
        int previousEntityId,
        int currentEntityId,
        FixedScalar previousScore,
        FixedScalar currentScore,
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
            simulation.ModifyEntityCultureInfluenceMultiplier(
                previousEntityId,
                -entityDelta,
                tick);
        }

        if (currentEntityId > 0)
        {
            simulation.ModifyEntityCultureInfluenceMultiplier(
                currentEntityId,
                entityDelta,
                tick);
        }

        var wasOccupied = previousEntityId > 0;
        var isOccupied = currentEntityId > 0;
        if (wasOccupied == isOccupied)
        {
            return;
        }

        var cultureDelta = Data.CultureInfluenceMultiplierDelta(slot) * (isOccupied ? 1 : -1);
        if (cultureDelta != 0)
        {
            simulation.ModifyCultureInfluenceMultiplier(
                Data.CultureId(slot),
                cultureDelta,
                tick);
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
