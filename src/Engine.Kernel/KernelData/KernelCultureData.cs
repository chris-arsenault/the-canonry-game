namespace Engine.Kernel;

internal sealed partial class KernelCultureData
{
    private readonly float[] _physicalProductionMultipliers;
    private readonly float[] _semanticProductionMultipliers;

    public KernelCultureData(int cultureIdCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cultureIdCapacity);
        _physicalProductionMultipliers = new float[cultureIdCapacity + 1];
        _semanticProductionMultipliers = new float[cultureIdCapacity + 1];
        global::System.Array.Fill(_physicalProductionMultipliers, 1f);
        global::System.Array.Fill(_semanticProductionMultipliers, 1f);
    }

    public void RegisterCultureProduction(int cultureId, float physicalMultiplier, float semanticMultiplier)
    {
        if (cultureId <= 0 || cultureId >= _physicalProductionMultipliers.Length ||
            physicalMultiplier < 0f || semanticMultiplier < 0f)
        {
            throw new global::System.InvalidOperationException();
        }

        _physicalProductionMultipliers[cultureId] = physicalMultiplier;
        _semanticProductionMultipliers[cultureId] = semanticMultiplier;
    }

    public float ProductionMultiplier(int cultureId, int planeCode)
    {
        if ((uint)cultureId >= (uint)_physicalProductionMultipliers.Length)
        {
            return 1f;
        }

        return planeCode == PlaneCodes.Semantic
            ? _semanticProductionMultipliers[cultureId]
            : _physicalProductionMultipliers[cultureId];
    }
}
