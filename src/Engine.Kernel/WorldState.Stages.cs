namespace Engine.Kernel;

internal sealed partial class Simulation
{
    internal void RegisterStageProperties(
        int kindId,
        int stageIndex,
        FixedScalar bandMultiplier,
        int workerBonus,
        FixedScalar ambientEmission,
        int districtCapacity) =>
        World.RegisterStageProperties(kindId, stageIndex, bandMultiplier, workerBonus, ambientEmission, districtCapacity);
}

/// <summary>
/// Entity stage catalogs: per-(kind, stage) properties for the named
/// maturity ladder (docs/design/entity-stages.md). Entity stage is named-entity
/// state owned by the world; these registries hold what each stage means.
/// </summary>
internal sealed partial class WorldState
{
    private int _stageRuleCount;
    private int[] _stageKindIds = [];
    private int[] _stageIndices = [];
    private FixedScalar[] _stageBandMultipliers = [];
    private int[] _stageWorkerBonuses = [];
    private FixedScalar[] _stageAmbientEmissions = [];
    private int[] _stageDistrictCapacities = [];
    private int[] _firstStageRuleByKind = [];
    private int[] _lastStageRuleByKind = [];
    private int[] _nextStageRuleByKind = [];

    private void InitializeStageStorage(SimulationOptions options)
    {
        _stageKindIds = new int[129];
        _stageIndices = new int[129];
        _stageBandMultipliers = new FixedScalar[129];
        _stageWorkerBonuses = new int[129];
        _stageAmbientEmissions = new FixedScalar[129];
        _stageDistrictCapacities = new int[129];
        _firstStageRuleByKind = new int[options.KindIdCapacity + 1];
        _lastStageRuleByKind = new int[options.KindIdCapacity + 1];
        _nextStageRuleByKind = new int[129];
    }

    internal void RegisterStageProperties(
        int kindId,
        int stageIndex,
        FixedScalar bandMultiplier,
        int workerBonus,
        FixedScalar ambientEmission,
        int districtCapacity)
    {
        if ((uint)kindId == 0u || kindId >= _firstStageRuleByKind.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(kindId));
        }
        if (_stageRuleCount + 1 >= _stageKindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _stageRuleCount++;
        _stageKindIds[_stageRuleCount] = kindId;
        _stageIndices[_stageRuleCount] = stageIndex;
        _stageBandMultipliers[_stageRuleCount] = bandMultiplier;
        _stageWorkerBonuses[_stageRuleCount] = workerBonus;
        _stageAmbientEmissions[_stageRuleCount] = ambientEmission;
        _stageDistrictCapacities[_stageRuleCount] = districtCapacity;
        var previous = _lastStageRuleByKind[kindId];
        if (previous == 0)
        {
            _firstStageRuleByKind[kindId] = _stageRuleCount;
        }
        else
        {
            _nextStageRuleByKind[previous] = _stageRuleCount;
        }

        _lastStageRuleByKind[kindId] = _stageRuleCount;
    }

    internal bool KindHasStages(int kindId) =>
        (uint)kindId < (uint)_firstStageRuleByKind.Length &&
        _firstStageRuleByKind[kindId] != 0;

    /// <summary>Resolves the registered rule slot for the kind at the
    /// entity's stage, clamped to the kind's highest registered stage.
    /// Returns 0 when the kind has no catalog.</summary>
    internal int StageRuleFor(int kindId, int stageIndex)
    {
        var best = 0;
        var bestIndex = -1;
        if ((uint)kindId >= (uint)_firstStageRuleByKind.Length)
        {
            return 0;
        }

        for (var rule = _firstStageRuleByKind[kindId];
             rule > 0;
             rule = _nextStageRuleByKind[rule])
        {
            if (_stageIndices[rule] > stageIndex)
            {
                continue;
            }

            if (_stageIndices[rule] > bestIndex)
            {
                bestIndex = _stageIndices[rule];
                best = rule;
            }
        }

        return best;
    }

    internal FixedScalar StageBandMultiplier(int stageRule) => stageRule == 0 ? 1 : _stageBandMultipliers[stageRule];

    internal int StageWorkerBonus(int stageRule) => _stageWorkerBonuses[stageRule];

    internal FixedScalar StageAmbientEmission(int stageRule) => stageRule == 0 ? 0 : _stageAmbientEmissions[stageRule];

    /// <summary>0 means uncapped (kinds without catalogs).</summary>
    internal int StageDistrictCapacity(int stageRule) => stageRule == 0 ? 0 : _stageDistrictCapacities[stageRule];

    internal int EntityWorkTier(int entityId)
    {
        EnsureEntity(entityId);
        return _entityWorkTiers[entityId];
    }

    internal int IncreaseEntityWorkTier(int entityId, int maxTier)
    {
        EnsureEntity(entityId);
        NumericGuard.ThrowIfNegative(maxTier);
        var uncapped = _entityWorkTiers[entityId] + 1;
        var next = maxTier > 0 ? global::System.Math.Min(maxTier, uncapped) : uncapped;
        _entityWorkTiers[entityId] = next;
        MarkEntityCultureWorkDirty(entityId);
        return next;
    }

    internal int DecreaseEntityWorkTier(int entityId)
    {
        EnsureEntity(entityId);
        var next = global::System.Math.Max(0, _entityWorkTiers[entityId] - 1);
        _entityWorkTiers[entityId] = next;
        MarkEntityCultureWorkDirty(entityId);
        return next;
    }
}
