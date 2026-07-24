namespace Engine.Kernel;

internal sealed partial class Simulation
{
    internal void RegisterStageProperties(
        int kindId,
        int stageIndex,
        float bandMultiplier,
        int workerBonus,
        float ambientEmission,
        int districtCapacity) =>
        World.RegisterStageProperties(kindId, stageIndex, bandMultiplier, workerBonus, ambientEmission, districtCapacity);
}

/// <summary>
/// Entity stage catalogs: per-(kind, stage) properties for the named
/// maturity ladder (docs/design/entity-stages.md). The entity's stage index
/// lives in the minor-actor progression store; these registries hold what
/// each stage means. Kinds without registered stages keep legacy behavior.
/// </summary>
internal sealed partial class WorldState
{
    private int _stageRuleCount;
    private int[] _stageKindIds = [];
    private int[] _stageIndices = [];
    private float[] _stageBandMultipliers = [];
    private int[] _stageWorkerBonuses = [];
    private float[] _stageAmbientEmissions = [];
    private int[] _stageDistrictCapacities = [];

    private void InitializeStageStorage()
    {
        _stageKindIds = new int[129];
        _stageIndices = new int[129];
        _stageBandMultipliers = new float[129];
        _stageWorkerBonuses = new int[129];
        _stageAmbientEmissions = new float[129];
        _stageDistrictCapacities = new int[129];
    }

    internal void RegisterStageProperties(
        int kindId,
        int stageIndex,
        float bandMultiplier,
        int workerBonus,
        float ambientEmission,
        int districtCapacity)
    {
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
    }

    internal bool KindHasStages(int kindId)
    {
        for (var rule = 1; rule <= _stageRuleCount; rule++)
        {
            if (_stageKindIds[rule] == kindId)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Resolves the registered rule slot for the kind at the
    /// entity's stage, clamped to the kind's highest registered stage.
    /// Returns 0 when the kind has no catalog.</summary>
    internal int StageRuleFor(int kindId, int stageIndex)
    {
        var best = 0;
        var bestIndex = -1;
        for (var rule = 1; rule <= _stageRuleCount; rule++)
        {
            if (_stageKindIds[rule] != kindId || _stageIndices[rule] > stageIndex)
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

    internal float StageBandMultiplier(int stageRule) => stageRule == 0 ? 1f : _stageBandMultipliers[stageRule];

    internal int StageWorkerBonus(int stageRule) => _stageWorkerBonuses[stageRule];

    internal float StageAmbientEmission(int stageRule) => stageRule == 0 ? 0f : _stageAmbientEmissions[stageRule];

    /// <summary>0 means uncapped (kinds without catalogs).</summary>
    internal int StageDistrictCapacity(int stageRule) => stageRule == 0 ? 0 : _stageDistrictCapacities[stageRule];
}
