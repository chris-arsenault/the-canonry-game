namespace Engine.Host;

using Engine.Domain.Compilation.KernelPrograms;

internal static partial class KernelProgramInstaller
{
    private static void InstallMapFrontier(KernelConfigurationContext context, MapFrontierProgram program)
    {
        if (!program.Enabled)
        {
            return;
        }

        context.SetMapFrontierConfiguration(
            program.ThawAgeId,
            program.RefreezeAgeId,
            program.FrozenTerrainId,
            program.MaxRadius,
            program.RevealBudgetPerPulse,
            program.IceAdvanceBudgetPerPulse,
            program.IceRetreatBudgetPerPulse);
        context.SetMapFrontierIcePressureConfiguration(
            program.FrostTemperatureCeiling,
            program.IceThicknessPressureWeight,
            program.GeothermalRefugeWeight,
            program.FreezeAgePressureScalar,
            program.ThawAgePressureScalar,
            program.BaseIceResistance,
            program.CultureMassResistanceScale,
            program.RoadIceResistance);
        context.SetMapFrontierBaseRevealRadius(program.BaseRevealRadius);
        foreach (var (kindId, radius) in program.KindRevealRadii)
        {
            context.RegisterMapFrontierKindRevealRadius(kindId, radius);
        }
        context.SetMapFrontierSemanticConfiguration(
            program.SemanticGrowthEnabled,
            program.SemanticMaxRadius,
            program.SemanticFrontierMassThreshold,
            program.SemanticBudgetPerPulse);
        context.SetMapFrontierSeaIceConfiguration(
            program.SeaIceCoverTerrainId,
            program.SeaIceFreezeBudgetPerPulse,
            program.SeaIceMeltBudgetPerPulse);
        foreach (var terrainId in program.SeaIceEligibleTerrainIds)
        {
            context.AddMapFrontierSeaIceEligibleTerrain(terrainId);
        }
        var driverCount = System.Math.Max(program.AdditionalThawAgeIds.Count, program.AdditionalRefreezeAgeIds.Count);
        for (var i = 0; i < driverCount; i++)
        {
            context.AddMapFrontierDriverAge(
                i < program.AdditionalThawAgeIds.Count ? program.AdditionalThawAgeIds[i] : 0,
                i < program.AdditionalRefreezeAgeIds.Count ? program.AdditionalRefreezeAgeIds[i] : 0);
        }
    }
}
