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
        InstallTerrain(context, kernelData.Terrain);
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
                terrain.BoundaryGroupId);
        }
    }
}
