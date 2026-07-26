namespace Engine.Domain.Compilation;

using Engine.Constants;
using Engine.Domain.Compilation.KernelPrograms;
using Engine.Domain.Config;

public static partial class DomainCompiler
{
    private static LifecycleProgram BuildLifecycleProgram(
        DomainConfig config,
        DomainIdRegistry ids)
    {
        var lifecycleRules = new List<LifecycleRule>();

        foreach (var (kind, lifecycle) in config.Lifecycles)
        {
            var kindId = ids.KindIdOf(kind).Value;
            var healthAttributeId = ids.AttributeIdOf(lifecycle.Health).Value;
            var kindConfig = config.Kinds[kind];
            if (!string.Equals(kindConfig.Health.Name, lifecycle.Health, StringComparison.Ordinal) ||
                lifecycle.TargetTicks <= 0 ||
                lifecycle.FlatDrainShare < 0f ||
                lifecycle.FlatDrainShare > 1f)
            {
                throw new InvalidOperationException(
                    $"Invalid lifecycle configuration for '{kind}'.");
            }

            var nominalHealthUnits = KernelFixedPoint.AttributeUnits(
                kindConfig.Health.Default - kindConfig.Health.Min);
            if (nominalHealthUnits <= 0)
            {
                throw new InvalidOperationException(
                    $"Lifecycle health budget must be positive for '{kind}'.");
            }

            lifecycleRules.Add(new LifecycleRule(
                kindId,
                healthAttributeId,
                lifecycle.TargetTicks,
                nominalHealthUnits,
                FixedScalar.FromSingle(lifecycle.FlatDrainShare),
                FixedScalar.FromUnits(200_000L)));
        }

        return new LifecycleProgram
        {
            Rules = lifecycleRules,
        };
    }
}
