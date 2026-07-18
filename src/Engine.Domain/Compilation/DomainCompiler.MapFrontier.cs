namespace Engine.Domain.Compilation;

using Engine.Domain.Compilation.KernelPrograms;
using Engine.Domain.Config;

public static partial class DomainCompiler
{
    private static MapFrontierProgram BuildMapFrontierProgram(DomainConfig config, DomainIdRegistry ids)
    {
        var frontier = config.MapFrontier;
        if (frontier is null)
        {
            return new MapFrontierProgram { Enabled = false };
        }

        return new MapFrontierProgram
        {
            Enabled = true,
            ThawAgeId = ids.GeneratorIdOf(frontier.ThawAge).Value,
            RefreezeAgeId = frontier.RefreezeAge is null ? 0 : ids.GeneratorIdOf(frontier.RefreezeAge).Value,
            AdditionalThawAgeIds = [.. frontier.AdditionalThawAges.Select(age => ids.GeneratorIdOf(age).Value)],
            AdditionalRefreezeAgeIds = [.. frontier.AdditionalRefreezeAges.Select(age => ids.GeneratorIdOf(age).Value)],
            FrozenTerrainId = ids.TerrainIdOf(frontier.FrozenTerrain).Value,
            MaxRadius = frontier.MaxRadius,
            FrontierMassThreshold = frontier.FrontierMassThreshold,
            ReclaimMassCeiling = frontier.ReclaimMassCeiling,
            ThawBudgetPerPulse = frontier.ThawBudgetPerPulse,
            RefreezeBudgetPerPulse = frontier.RefreezeBudgetPerPulse,
            SemanticGrowthEnabled = frontier.SemanticGrowthEnabled,
            SemanticMaxRadius = frontier.SemanticMaxRadius,
            SemanticFrontierMassThreshold = frontier.SemanticFrontierMassThreshold,
            SemanticBudgetPerPulse = frontier.SemanticBudgetPerPulse,
        };
    }
}
