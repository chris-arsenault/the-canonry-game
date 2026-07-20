namespace Engine.Host;

using Engine.Domain;
using Engine.Domain.Compilation.KernelData;

internal static class KernelDataInstaller
{
    public static void Install(KernelConfigurationContext context, CompiledKernelDataSet kernelData)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(kernelData);

        InstallEntityData(context, kernelData.Entities);
        InstallMovement(context, kernelData.Movement);
        InstallTerrain(context, kernelData.Terrain);
        InstallCultures(context, kernelData.Cultures);
    }

    private static void InstallCultures(KernelConfigurationContext context, CompiledCultureData data)
    {
        foreach (var culture in data.Cultures)
        {
            context.RegisterCultureProduction(
                culture.CultureId,
                culture.PhysicalInfluenceProductionMultiplier,
                culture.SemanticInfluenceProductionMultiplier);
        }
    }

    private static void InstallEntityData(KernelConfigurationContext context, CompiledEntityData data)
    {
        foreach (var attribute in data.Attributes)
        {
            context.RegisterAttribute(
                attribute.KindId,
                new AttributeRuntimeId(attribute.AttributeId),
                attribute.Minimum,
                attribute.Maximum,
                attribute.DefaultValue,
                attribute.DefaultVariance);
        }

        foreach (var trait in data.Traits)
        {
            context.RegisterTrait(
                trait.KindId,
                new TraitRuntimeId(trait.TraitId),
                trait.AssignmentChance);
        }
    }

    private static void InstallTerrain(KernelConfigurationContext context, CompiledTerrainData data)
    {
        foreach (var terrain in data.Terrains)
        {
            context.RegisterTerrain(
                new TerrainRuntimeId(terrain.TerrainId),
                terrain.BoundaryGroupId,
                terrain.MovementCost,
                terrain.InfluenceMultiplier,
                terrain.ClaimCostMultiplier,
                terrain.ClaimChanceMultiplier,
                terrain.RoadBuildMultiplier,
                terrain.RoadSpeedMultiplier);

            foreach (var profile in terrain.MovementProfiles)
            {
                context.RegisterTerrainMovementProfile(
                    profile.TerrainId,
                    profile.MovementProfileId,
                    profile.MovementCost);
            }

            foreach (var profile in terrain.KindProfiles)
            {
                context.RegisterTerrainKindProfile(
                    profile.TerrainId,
                    profile.KindId,
                    profile.FieldFlags,
                    profile.InfluenceMultiplier,
                    profile.ClaimCostMultiplier,
                    profile.ClaimChanceMultiplier);
            }
        }

        foreach (var crossing in data.CrossingProfiles)
        {
            context.RegisterCrossingProfile(
                crossing.CrossingClass,
                crossing.MovementProfileId,
                crossing.Factor,
                crossing.RoadNegated);
        }
    }

    private static void InstallMovement(KernelConfigurationContext context, CompiledMovementData data)
    {
        foreach (var profile in data.Defaults)
        {
            context.RegisterDefaultMovementProfile(
                profile.KindId,
                profile.PlaneCode,
                profile.MovementProfileId);
        }

        foreach (var profile in data.SubtypeRules)
        {
            context.RegisterSubtypeMovementProfile(
                profile.KindId,
                profile.SubtypeId,
                profile.PlaneCode,
                profile.MovementProfileId);
        }

        foreach (var profile in data.TraitRules)
        {
            context.RegisterTraitMovementProfile(
                profile.KindId,
                profile.TraitId,
                profile.PlaneCode,
                profile.MovementProfileId);
        }

        foreach (var structure in data.TravelStructures)
        {
            context.RegisterTravelStructure(
                structure.TravelStructureId,
                structure.PlaneCode);

            foreach (var profile in structure.MovementProfiles)
            {
                context.RegisterTravelStructureMovementProfile(
                    profile.TravelStructureId,
                    profile.MovementProfileId,
                    profile.MovementCost);
            }
        }
    }
}
