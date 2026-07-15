namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class MinorActorProgramState : IKernelProgramState
{
    private const int ActiveKey = 1;
    private const int ActorKindKey = 2;
    private const int ActionKey = 3;
    private const int CultureKey = 4;
    private const int SourceEntityKey = 5;
    private const int SourceRuleKey = 6;
    private const int PlaneKey = 7;
    private const int QKey = 8;
    private const int RKey = 9;
    private const int RemainingPulsesKey = 10;
    private const int CreatedAtTickKey = 11;
    private const int NextActionPulseKey = 12;
    private const int MoveChancePermilleKey = 13;
    private const int ActionCadencePulsesKey = 14;
    private const int PurposeCodeKey = 15;
    private const int SourceWorkTierKey = 1;
    private const int WorkDeltaKey = 1;
    private const int IntFieldsPerActor = 15;

    private readonly PrimitiveIntTable _actorInts;
    private readonly PrimitiveFloatTable _actorFloats;
    private readonly PrimitiveIntTable _sourceActiveCounts;
    private readonly PrimitiveIntTable _nextEmissionPulses;
    private readonly PrimitiveIntTable _sourceWorkTiers;

    public MinorActorProgramState(int actorCapacity, int sourceRuleCapacity, int sourceEntityCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(actorCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceRuleCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceEntityCapacity);
        ActorCapacity = actorCapacity;
        _actorInts = new PrimitiveIntTable(actorCapacity * IntFieldsPerActor);
        _actorFloats = new PrimitiveFloatTable(actorCapacity);
        _sourceActiveCounts = new PrimitiveIntTable(sourceRuleCapacity);
        _nextEmissionPulses = new PrimitiveIntTable(sourceRuleCapacity);
        _sourceWorkTiers = new PrimitiveIntTable(sourceEntityCapacity);
    }

    public int ProgramId => KernelProgramCodes.MinorActors;

    public int SchemaVersion => 3;

    public int ActorCapacity { get; }

    public int HighWatermark { get; private set; }

    public int ActiveCount { get; private set; }

    public int Pulse { get; private set; }

    public int TotalSpawned { get; private set; }

    public int TotalExpired { get; private set; }

    public int AdvancePulse()
    {
        Pulse++;
        return Pulse;
    }

    public int WorkTier(int sourceEntityId)
    {
        if (sourceEntityId <= 0)
        {
            return 0;
        }

        return _sourceWorkTiers.GetOrDefault(sourceEntityId, SourceWorkTierKey);
    }

    public int IncreaseWorkTier(int sourceEntityId, int maxTier)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceEntityId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(maxTier);
        var uncappedNextTier = WorkTier(sourceEntityId) + 1;
        var nextTier = maxTier > 0
            ? global::System.Math.Min(maxTier, uncappedNextTier)
            : uncappedNextTier;
        _sourceWorkTiers.Set(sourceEntityId, SourceWorkTierKey, nextTier);
        return nextTier;
    }

    public bool IsActive(int actorId) => GetInt(actorId, ActiveKey) != 0;

    public int ActorKindCode(int actorId) => GetInt(actorId, ActorKindKey);

    public int ActionCode(int actorId) => GetInt(actorId, ActionKey);

    public int CultureId(int actorId) => GetInt(actorId, CultureKey);

    public int SourceEntityId(int actorId) => GetInt(actorId, SourceEntityKey);

    public int SourceRuleId(int actorId) => GetInt(actorId, SourceRuleKey);

    public int PlaneCode(int actorId) => GetInt(actorId, PlaneKey);

    public int Q(int actorId) => GetInt(actorId, QKey);

    public int R(int actorId) => GetInt(actorId, RKey);

    public int RemainingPulses(int actorId) => GetInt(actorId, RemainingPulsesKey);

    public int CreatedAtTick(int actorId) => GetInt(actorId, CreatedAtTickKey);

    public int NextActionPulse(int actorId) => GetInt(actorId, NextActionPulseKey);

    public int MoveChancePermille(int actorId) => GetInt(actorId, MoveChancePermilleKey);

    public int ActionCadencePulses(int actorId) => GetInt(actorId, ActionCadencePulsesKey);

    public int PurposeCode(int actorId) => GetInt(actorId, PurposeCodeKey);

    public float WorkDelta(int actorId) => _actorFloats.GetOrDefault(actorId, WorkDeltaKey);

    public int SourceActiveCount(int sourceEntityId, int sourceRuleId)
    {
        if (sourceEntityId <= 0 || sourceRuleId <= 0)
        {
            return 0;
        }

        return _sourceActiveCounts.GetOrDefault(sourceEntityId, sourceRuleId);
    }

    public int NextEmissionPulse(int sourceEntityId, int sourceRuleId)
    {
        if (sourceEntityId <= 0 || sourceRuleId <= 0)
        {
            return 0;
        }

        return _nextEmissionPulses.GetOrDefault(sourceEntityId, sourceRuleId);
    }

    public void SetNextEmissionPulse(int sourceEntityId, int sourceRuleId, int pulse)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceEntityId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceRuleId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(pulse);
        _nextEmissionPulses.Set(sourceEntityId, sourceRuleId, pulse);
    }

    public int Spawn(
        int cultureId,
        int sourceEntityId,
        int sourceRuleId,
        int actorKindCode,
        int actionCode,
        int planeCode,
        int q,
        int r,
        int ttlPulses,
        int moveChancePermille,
        int actionCadencePulses,
        float workDelta,
        int tick,
        int pulse,
        int purposeCode = MinorActorPurposeCodes.CulturalLabor)
    {
        ValidateSpawn(
            cultureId,
            sourceEntityId,
            sourceRuleId,
            purposeCode,
            actorKindCode,
            actionCode,
            planeCode,
            ttlPulses,
            moveChancePermille,
            actionCadencePulses,
            workDelta,
            tick,
            pulse);

        var actorId = FindFreeActorSlot();
        SetInt(actorId, ActiveKey, 1);
        SetInt(actorId, ActorKindKey, actorKindCode);
        SetInt(actorId, ActionKey, actionCode);
        SetInt(actorId, CultureKey, cultureId);
        SetInt(actorId, SourceEntityKey, sourceEntityId);
        SetInt(actorId, SourceRuleKey, sourceRuleId);
        SetInt(actorId, PlaneKey, planeCode);
        SetInt(actorId, QKey, q);
        SetInt(actorId, RKey, r);
        SetInt(actorId, RemainingPulsesKey, ttlPulses);
        SetInt(actorId, CreatedAtTickKey, tick);
        SetInt(actorId, NextActionPulseKey, pulse);
        SetInt(actorId, MoveChancePermilleKey, moveChancePermille);
        SetInt(actorId, ActionCadencePulsesKey, actionCadencePulses);
        SetInt(actorId, PurposeCodeKey, purposeCode);
        _actorFloats.Set(actorId, WorkDeltaKey, workDelta);
        ActiveCount++;
        TotalSpawned++;
        if (sourceEntityId > 0 && sourceRuleId > 0)
        {
            _sourceActiveCounts.Add(sourceEntityId, sourceRuleId, 1);
        }

        return actorId;
    }

    public bool Deactivate(int actorId)
    {
        if (!IsActive(actorId))
        {
            return false;
        }

        var sourceEntityId = SourceEntityId(actorId);
        var sourceRuleId = SourceRuleId(actorId);
        SetInt(actorId, ActiveKey, 0);
        SetInt(actorId, RemainingPulsesKey, 0);
        ActiveCount--;
        TotalExpired++;
        if (sourceEntityId > 0 && sourceRuleId > 0)
        {
            _sourceActiveCounts.Add(sourceEntityId, sourceRuleId, -1);
        }

        return true;
    }

    public void SetPosition(int actorId, int q, int r)
    {
        EnsureActorId(actorId);
        SetInt(actorId, QKey, q);
        SetInt(actorId, RKey, r);
    }

    public void SetRemainingPulses(int actorId, int remainingPulses)
    {
        EnsureActorId(actorId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(remainingPulses);
        SetInt(actorId, RemainingPulsesKey, remainingPulses);
    }

    public void SetNextActionPulse(int actorId, int pulse)
    {
        EnsureActorId(actorId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(pulse);
        SetInt(actorId, NextActionPulseKey, pulse);
    }

    private int FindFreeActorSlot()
    {
        for (var actorId = 1; actorId <= HighWatermark; actorId++)
        {
            if (!IsActive(actorId))
            {
                return actorId;
            }
        }

        if (HighWatermark >= ActorCapacity)
        {
            throw new global::System.InvalidOperationException();
        }

        HighWatermark++;
        return HighWatermark;
    }

    private int GetInt(int actorId, int key)
    {
        EnsureActorId(actorId);
        return _actorInts.GetOrDefault(actorId, key);
    }

    private void SetInt(int actorId, int key, int value)
    {
        EnsureActorId(actorId);
        _actorInts.Set(actorId, key, value);
    }

    private void EnsureActorId(int actorId)
    {
        if ((uint)(actorId - 1) >= (uint)ActorCapacity)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(actorId));
        }
    }

    private static void ValidateSpawn(
        int cultureId,
        int sourceEntityId,
        int sourceRuleId,
        int purposeCode,
        int actorKindCode,
        int actionCode,
        int planeCode,
        int ttlPulses,
        int moveChancePermille,
        int actionCadencePulses,
        float workDelta,
        int tick,
        int pulse)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cultureId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(sourceEntityId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(sourceRuleId);
        if (purposeCode is not (
                MinorActorPurposeCodes.CulturalLabor or
                MinorActorPurposeCodes.RoadLabor or
                MinorActorPurposeCodes.CampaignLabor))
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(purposeCode));
        }

        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(actorKindCode);
        if (actionCode is not (
                MinorActorActionCodes.CultureExpansion or
                MinorActorActionCodes.RoadMaintenance or
                MinorActorActionCodes.CultureCleanup or
                MinorActorActionCodes.LifecycleMaintenance))
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(actionCode));
        }

        if (planeCode is not (Engine.Constants.PlaneCodes.Physical or Engine.Constants.PlaneCodes.Semantic))
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(planeCode));
        }

        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ttlPulses);
        global::System.ArgumentOutOfRangeException.ThrowIfGreaterThan(moveChancePermille, 1000);

        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(actionCadencePulses);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(workDelta);

        global::System.ArgumentOutOfRangeException.ThrowIfNegative(tick);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(pulse);
    }
}
