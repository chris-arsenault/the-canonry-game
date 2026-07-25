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
            BaseRevealRadius = frontier.BaseRevealRadius,
            KindRevealRadii =
                [.. frontier.KindRevealRadii.Select(entry =>
                    (ids.KindIdOf(entry.Kind.Value).Value, entry.Radius))],
            FrostTemperatureCeiling = frontier.FrostTemperatureCeiling,
            IceThicknessPressureWeight = FixedScalar.FromSingle(frontier.IceThicknessPressureWeight),
            GeothermalRefugeWeight = FixedScalar.FromSingle(frontier.GeothermalRefugeWeight),
            FreezeAgePressureScalar = FixedScalar.FromSingle(frontier.FreezeAgePressureScalar),
            ThawAgePressureScalar = FixedScalar.FromSingle(frontier.ThawAgePressureScalar),
            BaseIceResistance = FixedScalar.FromSingle(frontier.BaseIceResistance),
            CultureMassResistanceScale = FixedScalar.FromSingle(frontier.CultureMassResistanceScale),
            RoadIceResistance = FixedScalar.FromSingle(frontier.RoadIceResistance),
            IceAdvanceBudgetPerPulse = frontier.IceAdvanceBudgetPerPulse,
            IceRetreatBudgetPerPulse = frontier.IceRetreatBudgetPerPulse,
            SemanticGrowthEnabled = frontier.SemanticGrowthEnabled,
            SemanticMaxRadius = frontier.SemanticMaxRadius,
            SemanticFrontierMassThreshold = KernelFixedPoint.CultureMassScalar(
                KernelFixedPoint.CultureMassUnits(frontier.SemanticFrontierMassThreshold)),
            SemanticBudgetPerPulse = frontier.SemanticBudgetPerPulse,
            SeaIceCoverTerrainId = frontier.SeaIceCoverTerrain is null
                ? 0
                : ids.TerrainIdOf(frontier.SeaIceCoverTerrain).Value,
            SeaIceEligibleTerrainIds =
                [.. frontier.SeaIceEligibleTerrains.Select(terrain => ids.TerrainIdOf(terrain).Value)],
            SeaIceFreezeBudgetPerPulse = frontier.SeaIceFreezeBudgetPerPulse,
            SeaIceMeltBudgetPerPulse = frontier.SeaIceMeltBudgetPerPulse,
        };
    }
}
