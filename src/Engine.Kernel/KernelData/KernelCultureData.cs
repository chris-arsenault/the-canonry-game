namespace Engine.Kernel;

internal sealed partial class KernelCultureData
{
    private readonly FixedScalar[] _physicalProductionMultipliers;
    private readonly FixedScalar[] _semanticProductionMultipliers;

    public KernelCultureData(int cultureIdCapacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(cultureIdCapacity);
        _physicalProductionMultipliers = new FixedScalar[cultureIdCapacity + 1];
        _semanticProductionMultipliers = new FixedScalar[cultureIdCapacity + 1];
        global::System.Array.Fill(_physicalProductionMultipliers, 1);
        global::System.Array.Fill(_semanticProductionMultipliers, 1);
    }

    public void RegisterCultureProduction(int cultureId, FixedScalar physicalMultiplier, FixedScalar semanticMultiplier)
    {
        if (cultureId <= 0 || cultureId >= _physicalProductionMultipliers.Length ||
            physicalMultiplier < 0 || semanticMultiplier < 0)
        {
            throw new global::System.InvalidOperationException();
        }

        _physicalProductionMultipliers[cultureId] = physicalMultiplier;
        _semanticProductionMultipliers[cultureId] = semanticMultiplier;
    }

    public FixedScalar ProductionMultiplier(int cultureId, int planeCode)
    {
        if ((uint)cultureId >= (uint)_physicalProductionMultipliers.Length)
        {
            return 1;
        }

        return planeCode == PlaneCodes.Semantic
            ? _semanticProductionMultipliers[cultureId]
            : _physicalProductionMultipliers[cultureId];
    }
}
